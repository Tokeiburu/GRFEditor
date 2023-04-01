using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Core;
using GRF.FileFormats.LubFormat.Core.CodeReconstructor;
using GRF.FileFormats.LubFormat.Core.CodeReconstructor2;
using GRF.FileFormats.LubFormat.VM;
using GRF.System;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat {
	public static class OpCodesToLua {
		private static int _debugBreakCount = 40;
		private static int _debugBreakIndex = 20;

		public static string Analyse(string code, Lub decompiler, int level) {
			try {
				//code = "function(msg)\r\n	local imagetag_front_startpos = string.find(msg, a)\r\n	if nil ~= imagetag_front_startpos then\r\n		goto 18_[7]\r\n	else\r\n		goto 18_[9]\r\n	end\r\n::18_[7]::\r\n	if nil == msg then\r\n		goto 18_[9]\r\n	else\r\n		goto 18_[10]\r\n	end\r\n::18_[9]::\r\n	return msg\r\n::18_[10]::\r\n	local imagetag_front = string.sub(msg, imagetag_front_startpos, msg)\r\n	local imagetag_rear_startpos = string.find(msg, b, msg + 1)\r\n	if nil ~= imagetag_rear_startpos then\r\n		goto 18_[24]\r\n	else\r\n		goto 18_[26]\r\n	end\r\n::18_[24]::\r\n	if nil == msg then\r\n		goto 18_[26]\r\n	else\r\n		goto 18_[27]\r\n	end\r\n::18_[26]::\r\n	return msg\r\n::18_[27]::\r\n	local imagetag_rear = string.sub(msg, imagetag_rear_startpos, msg)\r\n	local name = string.sub(msg, msg + 1, imagetag_rear_startpos - 1)\r\n	local num_startpos = string.find(imagetag_front, c)\r\n	local num_endpos = string.sub(msg, imagetag_front_startpos, msg)\r\n	if nil ~= num_startpos then\r\n		goto 18_[46]\r\n	else\r\n		goto 18_[48]\r\n	end\r\n::18_[46]::\r\n	if nil == num_endpos then\r\n		goto 18_[48]\r\n	else\r\n		goto 18_[49]\r\n	end\r\n::18_[48]::\r\n	return msg\r\n::18_[49]::\r\n	local itidstr = string.sub(imagetag_front, num_startpos + 1, num_endpos - 1)\r\n	local tagstr = string.format(<ITEM>%s<INFO>%s</INFO></ITEM>, name, itidstr)\r\n	local final = \"\"\r\n	if 1 < imagetag_front_startpos then\r\n		goto 18_[64]\r\n	else\r\n		goto 18_[72]\r\n	end\r\n::18_[64]::\r\n	final = final .. string.sub(msg, 1, imagetag_front_startpos - 1)\r\n::18_[72]::\r\n	final = final .. tagstr\r\n	if msg < #msg then\r\n		goto 18_[78]\r\n	else\r\n		goto 18_[85]\r\n	end\r\n::18_[78]::\r\n	final = final .. string.sub(msg, msg + 1)\r\n::18_[85]::\r\n	return QuestTable.func_quest_lower_exchange_imagetag(final)\r\nend";
				List<string> lines = code.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();

				List<CodeFragment> fragments = CodeLogic.Analyse(lines, level);

				fragments.ForEach(p => p.RemoveEmpty());
				fragments.ForEach(p => p.RemoveElse());
				fragments.ForEach(p => p.RemoveIf());
				fragments.ForEach(p => p.MergeIfConditions());
				fragments.ForEach(p => p.RemoveReturnElseBranches());
				if (fragments.Count > 0) fragments[0].SetWhileLoop();
				fragments.ForEach(p => p.RemoveElseAfterLoop());
				
				// Destructive beyond this point
				fragments.ForEach(p => p.MergeElseIf());
				if (fragments.Count > 0) fragments[0].AnalyseLogicalExecutionLoops();
				fragments.ForEach(p => p.RemoveLogicalExecution());
				fragments.ForEach(p => p.ExtractExecution());
				if (fragments.Count > 0) fragments.ForEach(p => p.RemoveLogicalReturnExecution(fragments[0]));


				StringBuilder builder = new StringBuilder();
				fragments[0].Print(builder, 0);

				if (level > 0) {
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

				return builder.ToString();
			}
			catch {
				LubErrorHandler.Handle("Failed to reconstruct the code for the function " + decompiler.FunctionDecompiledCount + ".", LubSourceError.CodeReconstructor);
				return "";
			}
		}

		public static string DecompileFunction(Lub decompiler, LubFunction function) {
			StringBuilder builder = new StringBuilder();

			if (function.FunctionLevel > 0) {
				builder.AppendLine("function(" + Methods.Aggregate(function.LocalVariables.Take(function.NumberOfParameters).Select(p => p.Key).ToList(), ", ") + ")");
			}

			function.InitFunctionStack();

			_writeBlock(builder, 0, function);

			if (function.FunctionLevel > 0) {
				builder.AppendIndent(function.FunctionLevel - 1);
				builder.Append("end");
			}

			if (Settings.LubDecompilerSettings.UseCodeReconstructor)
				return Analyse(builder.ToString(), decompiler, function.FunctionLevel);

			return builder.ToString();
		}

		private static void _writeBlock(StringBuilder builder, int codeIndex, LubFunction function) {
			OpCodes.AbstractInstruction.DebugStack = function.Stack;
			OpCodes.AbstractInstruction.DebugFunction = function;

			bool isReturned = false;
			function.BlockDelimiters = new Dictionary<int, BlockDelimiter>();

			// We start by registering each jumps from the instructions
			// This will simplify delayed labels
			for (int index = codeIndex; index < function.Instructions.Count; index++) {
				function.InstructionIndex = index;
				OpCodes.AbstractInstruction ins = function.Instructions[index];

				if (ins is OpCodes.IJumpingInstruction) {
					int gotoLocation = ((OpCodes.IJumpingInstruction) ins).GetJumpLocation(function);
					BlockDelimiter block = new BlockDelimiter();
					block.BlockStart = gotoLocation;
					block.Label = function.Label + "_[" + gotoLocation + "]";

					if (!function.BlockDelimiters.ContainsKey(block.BlockStart)) {
						function.BlockDelimiters[block.BlockStart] = block;
					}
				}
			}

			//OpCodes.ForceInstantiation2(builder, function);

			for (int index = codeIndex, count = function.Instructions.Count; index < count; index++) {
				function.InstructionIndex = index;

				if (index == _debugBreakIndex && count == _debugBreakCount) {
					Z.F();
				}

				OpCodes.AbstractInstruction ins = function.Instructions[index];

				try {
					if (function.BlockDelimiters.ContainsKey(index)) {
						// We dump local variables before
						// changing the scope
						OpCodes.ForceAssigning(builder, function);
						builder.AppendIndent(function.FunctionLevel - 1);
						builder.Append("::");
						builder.Append(function.BlockDelimiters[index].Label);
						builder.AppendLine("::");
						isReturned = false;
					}

					if (ins is OpCodes.IAssigning) {
						OpCodes.ForceInstantiation(builder, function);
						OpCodes.ForceAssigning(builder, function);
					}

					ins.Execute(function);

					if (ins is OpCodes.ConditionalInstruction) {
						builder.Append(ins.LuaCode);
						index++;
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

					OpCodes.ForceInstantiation(builder, function);

					index += ins.InstructionJump;
				}
				catch {
					LubErrorHandler.Handle("Failed to decode the instruction {" + ins + "} for the function " + function.Label + ", at " + function.InstructionIndex + ".", LubSourceError.CodeDecompiler);
				}
			}
		}
	}
}