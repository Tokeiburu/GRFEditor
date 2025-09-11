using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Core;
using GRF.FileFormats.LubFormat.Core.CodeReconstructor;
using GRF.FileFormats.LubFormat.VM;
using GRF.GrfSystem;
using Utilities;

namespace GRF.FileFormats.LubFormat {
	public static class OpCodesToLua {
		public static string WriteRawFragmentOutput(List<CodeFragment> fragments) {
			StringBuilder b = new StringBuilder();

			for (int i = 0; i < fragments.Count; i++) {
				foreach (var line in fragments[i].Content.Lines) {
					b.AppendLine(line);
				}
			}

			return b.ToString();
		}

		public static string Analyse(LubFunction function, string code, Lub decompiler, int level) {
			try {
				List<string> lines = code.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();

				List<CodeFragment> fragments = CodeLogic.Analyse(lines, function.FunctionLevel, function);

				_execute(fragments, p => p.RemoveEmpty());
				_execute(fragments, p => p.MergeIfConditions2());
				fragments.ForEach(p => p.RemoveElse());
				fragments.ForEach(p => p.RemoveReturnElseBranches());

				// Destructive beyond this point
				fragments.OrderByDescending(p => p.Uid).ToList().ForEach(p => p.MergeElseIf());
				_cleanup(fragments);
				if (fragments.Count > 0) fragments[0].AnalyseLogicalExecutionLoops(fragments);
				fragments.ForEach(p => p.RemoveLogicalExecution());
				fragments.ForEach(p => p.ExtractExecution());
				fragments.ForEach(p => p.RemoveLogicalReturnExecution());

				StringBuilder builder = new StringBuilder();
				fragments[0].Print(builder, function, null, level);
				if (function.FunctionLevel > 0)
					_appendEnd(builder);

				if (function.Label == 0)
					Z.F();

				return builder.ToString();
			}
			catch {
				LubErrorHandler.Handle("Failed to reconstruct the code for the function " + decompiler.FunctionDecompiledCount + ".", LubSourceError.CodeReconstructor);
				return "";
			}
		}

		private static void _execute(List<CodeFragment> fragments, Func<CodeFragment, bool> func) {
			for (int i = 0; i < fragments.Count; i++) {
				while (i > 0 && func(fragments[i])) {
					fragments.RemoveAt(i);
					i--;
				}
			}
		}

		private static void _appendEnd(StringBuilder builder) {
			int indent = 0;
			for (int i = builder.Length - 1; i >= 0; i--) {
				if (builder[i] == '\t') {
					indent++;
					i--;

					for (; i >= 0; i--) {
						if (builder[i] == '\t')
							indent++;
						else
							break;
					}

					break;
				}
			}

			builder.Append(LineHelper.GenerateIndent(indent - 1) + "end");
		}

		private static void _cleanup(List<CodeFragment> fragments) {
			for (int i = fragments.Count - 1; i >= 1; i--) {
				if (fragments[i].ParentReferences.Count == 0)
					fragments.RemoveAt(i);
			}
		}

		public static string DecompileFunction(Lub decompiler, LubFunction function, int addIndent) {
			StringBuilder builder = new StringBuilder();

			if (function.FunctionLevel > 0) {
				var parameters_ = function.Debug_LocalVariables.Take(function.NumberOfParameters).Select(p => p.Key).ToList();

				if (parameters_.Count > 0 && parameters_[0].Value == "self")
					parameters_ = parameters_.Skip(1).ToList();

				string parameters = Methods.Aggregate(parameters_, ", ");

				if ((function.IsVarArg & (VarArgType.VARARG_HASARG | VarArgType.VARARG_ISVARARG | VarArgType.VARARG_NEEDSARG)) == (VarArgType.VARARG_HASARG | VarArgType.VARARG_ISVARARG | VarArgType.VARARG_NEEDSARG)) {
					parameters += parameters == "" ? "..." : ", ...";
				}

				builder.AppendLine("function(" + parameters + ")");
			}

			function.InitFunctionStack();

			_decodeInstructions(builder, function);

			if (function.FunctionLevel > 0) {
				_appendEnd(builder);
			}

			if (Settings.LubDecompilerSettings.UseCodeReconstructor)
				return Analyse(function, builder.ToString(), decompiler, addIndent);

			return builder.ToString();
		}

		private static void _decodeInstructions(StringBuilder builder, LubFunction function) {
			bool isReturned = false;

			_generateBlocksAndWhileLoops(function);

			function.PC = 0;
			for (; function.PC < function.Instructions.Count; function.PC++) {
				int pc = function.PC;

				if (function.Label == 14 && pc == 81) {
					Z.F();
				}
				if (function.Label == 1 && pc == 24) {
					Z.F();
				}

				OpCodes.AbstractInstruction ins = function.Instructions[pc];

				try {
					// Alawys attempt to instantiate local variables
					OpCodes.LocalVarInstantiation(builder, function);
					OpCodes.VarAssign(builder, function);

					if (function.BlockDelimiters.ContainsKey(pc)) {
						builder.Append("::");
						builder.Append(function.BlockDelimiters[pc].Label);
						builder.AppendLine("::");
						isReturned = false;
					}

					ins.Execute(function);

					if (ins is OpCodes.ConditionalInstruction) {
						builder.Append(ins.LuaCode);
						function.PC++;
						continue;
					}

					if (isReturned)
						continue;

					if (ins.Append) {
						builder.Append(ins.LuaCode);
					}

					if (ins is OpCodes.IReturnInstruction) {
						isReturned = true;
					}
				}
				catch {
					LubErrorHandler.Handle("Failed to decode the instruction {" + ins + "} for the function " + function.Label + ", at " + function.PC + ".", LubSourceError.CodeDecompiler);
				}
			}
		}

		private static void _generateBlocksAndWhileLoops(LubFunction function) {
			function.BlockDelimiters = new Dictionary<int, LubFunction.BlockDelimiter>();

			function.PC = 0;
			// We start by registering each jumps from the instructions
			// This will simplify labels
			for (; function.PC < function.Instructions.Count; function.PC++) {
				OpCodes.AbstractInstruction ins = function.Instructions[function.PC];

				if (ins is OpCodes.IJumpingInstruction) {
					int gotoLocation = ((OpCodes.IJumpingInstruction)ins).GetJumpLocation(function);
					var block = new LubFunction.BlockDelimiter();
					block.BlockStart = gotoLocation;
					block.Label = function.Label + "_[" + gotoLocation + "]";

					// Detect while loop from the jmp instruction, this is much easier...
					if (gotoLocation < function.PC) {
						if ((function.PC <= 0 || !(function.Instructions[function.PC - 1] is OpCodes.ILoopInstruction)) && ins is OpCodes.Jmp) {
							if (function._decompiler.Header.Version >= 5.1) {
								for (int pc2 = gotoLocation; pc2 < function.PC; pc2++) {
									var ins_cond = function.Instructions[pc2] as OpCodes.ConditionalInstruction;

									if (ins_cond != null) {
										ins_cond.WhileLoop_PC_Start = gotoLocation;
										ins_cond.WhileLoop_PC_End = function.PC + 1;
										break;
									}
								}
							}
							else {
								var ins_jmp2 = function.Instructions[gotoLocation - 1] as OpCodes.Jmp;

								if (ins_jmp2 != null) {
									var gotoLocation2 = ins_jmp2.Registers[0] + gotoLocation;

									for (int pc2 = gotoLocation2; pc2 < function.PC; pc2++) {
										var ins_cond = function.Instructions[pc2] as OpCodes.ConditionalInstruction;

										if (ins_cond != null) {
											ins_cond.WhileLoop_PC_Start = gotoLocation;
											ins_cond.WhileLoop_PC_End = function.PC + 1;
											break;
										}
									}
								}
							}
						}
					}

					if (!function.BlockDelimiters.ContainsKey(block.BlockStart)) {
						function.BlockDelimiters[block.BlockStart] = block;
					}
				}
			}
		}
	}
}