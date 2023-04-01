using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: ForLoop50

		public class ForLoop50 : AbstractInstruction, IJumpingInstruction, IAssigning {
			public ForLoop50() {
				Mode = EncodedMode.AsBx;
			}

			#region IJumpingInstruction Members

			public int GetJumpLocation(LubFunction function) {
				return function.InstructionIndex + 1 + Registers[1];
			}

			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				List<LubReferenceType> locals = function.LocalVariables.Skip(Registers[0]).Take(3).ToList();

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendFormat("for {0} = {1}, {2}, {3} do",
				                     locals[0].Key,
				                     locals[0].Value,
				                     locals[1].Value,
				                     locals[2].Value
					);
				builder.AppendLine();

				builder.AppendIndent(function.FunctionLevel + 1);

				builder.Append("goto " + function.Label + "_[" + (function.InstructionIndex + 1 + Registers[1]) + "]");
				builder.AppendLine();

				builder.AppendIndent(function.FunctionLevel);
				builder.Append("end");
				builder.AppendLine();

				builder.AppendIndent(function.FunctionLevel - 1);
				builder.Append("goto " + function.Label + "_[" + (function.InstructionIndex + 1) + "]");
				builder.AppendLine();

				int gotoLocation = function.InstructionIndex + 1;

				if (!function.BlockDelimiters.ContainsKey(gotoLocation)) {
					function.BlockDelimiters.Add(gotoLocation, new BlockDelimiter { Label = function.Label + "_[" + gotoLocation + "]", BlockStart = gotoLocation });
				}

				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: ForLoop51

		public class ForLoop51 : AbstractInstruction, IJumpingInstruction, IAssigning {
			public ForLoop51() {
				Mode = EncodedMode.AsBx;
			}

			#region IJumpingInstruction Members

			public int GetJumpLocation(LubFunction function) {
				return function.InstructionIndex + 1 + Registers[1];
			}

			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				List<LubReferenceType> locals = function.LocalVariables.Skip(Registers[0]).Take(4).ToList();

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendFormat("for {0} = {1}, {2}, {3} do",
				                     locals[3].Key,
				                     locals[0].Value,
				                     locals[1].Value,
				                     locals[2].Value
					);
				builder.AppendLine();

				builder.AppendIndent(function.FunctionLevel + 1);

				builder.Append("goto " + function.Label + "_[" + (function.InstructionIndex + 1 + Registers[1]) + "]");
				builder.AppendLine();

				builder.AppendIndent(function.FunctionLevel);
				builder.Append("end");
				builder.AppendLine();

				builder.AppendIndent(function.FunctionLevel - 1);
				builder.Append("goto " + function.Label + "_[" + (function.InstructionIndex + 1) + "]");
				builder.AppendLine();

				int gotoLocation = function.InstructionIndex + 1;

				if (!function.BlockDelimiters.ContainsKey(gotoLocation)) {
					function.BlockDelimiters.Add(gotoLocation, new BlockDelimiter { Label = function.Label + "_[" + gotoLocation + "]", BlockStart = gotoLocation });
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
				return function.InstructionIndex + Registers[1] + 1;
			}

			#endregion

			protected override void _execute(LubFunction function) {
				//function.Stack[Registers[0]] = function.Stack[Registers[0] - 2];
				Append = true;
				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + Registers[1] + 1) + "]");
				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: TableForLoop50

		public class TableForLoop50 : ConditionalInstruction {
			public TableForLoop50() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				List<LubReferenceType> locals = function.LocalVariables.Skip(Registers[0] + 2).Take(2).ToList();

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("for " + Methods.Aggregate(locals.Where(p => !p.Key.ToString().StartsWith("(")).Select(p => p.Key).ToList(), ", ") + " in " + function.Stack[Registers[0]] + " do");

				AbstractInstruction ins = function.Instructions[function.InstructionIndex + 1];

				builder.AppendIndent(function.FunctionLevel + 1);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + 2 + ins.Registers[0]) + "]");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("end");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + 2) + "]");

				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: TableForLoop51

		public class TableForLoop51 : ConditionalInstruction {
			public TableForLoop51() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				List<LubReferenceType> locals = function.LocalVariables.Skip(Registers[0] + 3).Take(2).ToList();

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("for " + Methods.Aggregate(locals.Where(p => !p.Key.ToString().StartsWith("(")).Select(p => p.Key).ToList(), ", ") + " in " + function.Stack[Registers[0]] + " do");

				AbstractInstruction ins = function.Instructions[function.InstructionIndex + 1];

				builder.AppendIndent(function.FunctionLevel + 1);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + 2 + ins.Registers[0]) + "]");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("end");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + 2) + "]");

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
				return function.InstructionIndex + Registers[1] + 1;
			}

			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + Registers[1] + 1) + "]");
				LuaCode = builder.ToString();
			}
		}

		#endregion
	}
}