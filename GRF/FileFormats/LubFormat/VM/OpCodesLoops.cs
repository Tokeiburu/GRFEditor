using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: ForLoop50
		public class ForLoop50 : AbstractInstruction, IJumpingInstruction, ILoopInstruction {
			public ForLoop50() {
				Mode = EncodedMode.AsBx;
			}

			#region IJumpingInstruction Members
			public int GetJumpLocation(LubFunction function) {
				return function.PC + 1 + Registers[1];
			}
			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				var forIndex = GetKey(function.Stack[Registers[0] + 0]);
				var forLimit = GetKey(function.Stack[Registers[0] + 1]);
				var forStep = GetKey(function.Stack[Registers[0] + 2]);
				ILubObject forValue = null;

				// Find the value of iterator, quite the nightmareu huh
				try {
					if (function.Stack.GetStashedSize > 0)
						return;

					function.Stack.Push(false);
					function.PushInstances(false);
					function.InitFunctionStack();

					var m = function.PC + Registers[1] - 1;

					for (int pc = 0; pc < m; pc++) {
						function.Instructions[pc].Execute(function);
					}

					forValue = function.Stack[Registers[0] + 0];
				}
				finally {
					function.Stack.Pop();
					function.PopInstances();
				}

				builder.AppendIndent(function.BaseIndent);
				builder.Append(Lub.String_LoopPcBounds);
				builder.AppendLine((function.PC + 1 + Registers[1]) + " " + function.PC);
				builder.AppendIndent(function.BaseIndent);

				if (forStep.ToString() == "1") {
					builder.AppendFormat("for {0} = {1}, {2} do",
						forIndex,
						forValue,
						forLimit
						);
				}
				else {
					builder.AppendFormat("for {0} = {1}, {2}, {3} do",
						forIndex,
						forValue,
						forLimit,
						forStep
						);
				}

				builder.AppendLine();

				builder.AppendIndent(function.BaseIndent + 1);

				builder.Append("goto " + function.Label + "_[" + (function.PC + 1 + Registers[1]) + "]");
				builder.AppendLine();

				builder.AppendIndent(function.BaseIndent);
				builder.Append("end");
				builder.AppendLine();

				builder.AppendIndent(function.BaseIndent);
				builder.Append("goto " + function.Label + "_[" + (function.PC + 1) + "]");
				builder.AppendLine();

				int gotoLocation = function.PC + 1;

				if (!function.BlockDelimiters.ContainsKey(gotoLocation)) {
					function.BlockDelimiters.Add(gotoLocation, new LubFunction.BlockDelimiter { Label = function.Label + "_[" + gotoLocation + "]", BlockStart = gotoLocation });
				}

				LuaCode = builder.ToString();
			}
		}
		#endregion

		#region Nested type: ForLoop51
		public class ForLoop51 : AbstractInstruction, IJumpingInstruction, ILoopInstruction {
			public ForLoop51() {
				Mode = EncodedMode.AsBx;
			}

			#region IJumpingInstruction Members
			public int GetJumpLocation(LubFunction function) {
				return function.PC + 1 + Registers[1];
			}
			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				var forIndex = GetKey(function.Stack[Registers[0] + 0]);
				var forLimit = GetKey(function.Stack[Registers[0] + 1]);
				var forStep = GetKey(function.Stack[Registers[0] + 2]);

				builder.AppendIndent(function.BaseIndent);
				builder.Append(Lub.String_LoopPcBounds);
				builder.AppendLine((function.PC + 1 + Registers[1]) + " " + function.PC);
				builder.AppendIndent(function.BaseIndent);

				if (forStep.ToString() == "1") {
					builder.AppendFormat("for {0} = {1}, {2} do",
						GetLocalName(Registers[0] + 3, function.PC, function).Key,
						forIndex,
						forLimit
						);
				}
				else {
					builder.AppendFormat("for {0} = {1}, {2}, {3} do",
						GetLocalName(Registers[0] + 3, function.PC, function).Key,
						forIndex,
						forLimit,
						forStep
						);
				}

				builder.AppendLine();

				builder.AppendIndent(function.BaseIndent + 1);
				builder.Append("goto " + function.Label + "_[" + (function.PC + 1 + Registers[1]) + "]");
				builder.AppendLine();

				builder.AppendIndent(function.BaseIndent);
				builder.Append("end");
				builder.AppendLine();

				builder.AppendIndent(function.BaseIndent);
				builder.Append("goto " + function.Label + "_[" + (function.PC + 1) + "]");
				builder.AppendLine();

				int gotoLocation = function.PC + 1;

				if (!function.BlockDelimiters.ContainsKey(gotoLocation)) {
					function.BlockDelimiters.Add(gotoLocation, new LubFunction.BlockDelimiter { Label = function.Label + "_[" + gotoLocation + "]", BlockStart = gotoLocation });
				}

				LuaCode = builder.ToString();
			}
		}
		#endregion

		#region Nested type: ForPrep
		public class ForPrep : AbstractInstruction, IJumpingInstruction {
			public ForPrep() {
				Mode = EncodedMode.AsBx;
			}

			#region IJumpingInstruction Members
			public int GetJumpLocation(LubFunction function) {
				return function.PC + Registers[1] + 1;
			}
			#endregion

			protected override void _execute(LubFunction function) {
				//function.Stack[Registers[0]] = function.Stack[Registers[0] - 2];
				Append = true;
				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("goto " + function.Label + "_[" + (function.PC + Registers[1] + 1) + "]");
				LuaCode = builder.ToString();
			}
		}
		#endregion

		#region Nested type: TableForLoop50
		public class TableForLoop50 : ConditionalInstruction, ILoopInstruction {
			public TableForLoop50() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				List<LubReferenceType> locals = new List<LubReferenceType>();

				// Fetch variable name from debug data
				locals.Add(GetLocalName(Registers[0] + 2, function.PC, function));
				locals.Add(GetLocalName(Registers[0] + 3, function.PC, function));

				function.Stack[Registers[0] + 2] = locals[0];
				function.Stack[Registers[0] + 3] = locals[1];

				var ins_jmp = function.Instructions[function.PC + 1];
				var targetJmp = ins_jmp.Registers[0] + function.PC + 2;

				builder.AppendIndent(function.BaseIndent);
				builder.Append(Lub.String_LoopPcBounds);
				builder.AppendLine(targetJmp + " " + function.PC);

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("for " + Methods.Aggregate(locals.Where(p => !p.Key.ToString().StartsWith("(")).Select(p => p.Key).ToList(), ", ") + " in " + GetKey(function.Stack[Registers[0]]) + " do");

				builder.AppendIndent(function.BaseIndent + 1);
				builder.AppendLine("goto " + function.Label + "_[" + targetJmp + "]");

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("end");

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("goto " + function.Label + "_[" + (function.PC + 2) + "]");

				LuaCode = builder.ToString();
			}
		}
		#endregion

		#region Nested type: TableForLoop51
		public class TableForLoop51 : ConditionalInstruction, ILoopInstruction {
			public TableForLoop51() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				List<LubReferenceType> locals = new List<LubReferenceType>();

				// Fetch variable name from debug data
				locals.Add(GetLocalName(Registers[0] + 3, function.PC, function));
				locals.Add(GetLocalName(Registers[0] + 4, function.PC, function));

				function.Stack[Registers[0] + 3] = locals[0];
				function.Stack[Registers[0] + 4] = locals[1];

				if (Registers[2] != 2) {
					Console.WriteLine("Loop has more than 2 results to return...? Inspect this properly.");
					Z.F();
				}

				var ins_jmp = function.Instructions[function.PC + 1];
				var targetJmp = ins_jmp.Registers[0] + function.PC + 2;

				builder.AppendIndent(function.BaseIndent);
				builder.Append(Lub.String_LoopPcBounds);
				builder.AppendLine(targetJmp + " " + function.PC);

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("for " + Methods.Aggregate(locals.Select(p => p.Key).ToList(), ", ") + " in " + GetKey(function.Stack[Registers[0]]) + " do");

				builder.AppendIndent(function.BaseIndent + 1);
				builder.AppendLine("goto " + function.Label + "_[" + targetJmp + "]");

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("end");

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("goto " + function.Label + "_[" + (function.PC + 2) + "]");

				LuaCode = builder.ToString();
			}
		}
		#endregion

		#region Nested type: TableForPrep
		public class TableForPrep : AbstractInstruction, IJumpingInstruction {
			public TableForPrep() {
				Mode = EncodedMode.AsBx;
			}

			#region IJumpingInstruction Members
			public int GetJumpLocation(LubFunction function) {
				return function.PC + Registers[1] + 1;
			}
			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("goto " + function.Label + "_[" + (function.PC + Registers[1] + 1) + "]");
				LuaCode = builder.ToString();
			}
		}
		#endregion
	}
}