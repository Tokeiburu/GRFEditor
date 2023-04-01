using System.Collections.Generic;
using System.Text;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: ConditionalInstruction

		public abstract class ConditionalInstruction : AbstractInstruction, IJumpingInstruction, IAssigning {
			public string PositiveOutput { get; protected set; }
			public string NegativeOutput { get; protected set; }
			public List<BlockDelimiter> CodesToAdd { get; set; }

			#region IJumpingInstruction Members

			public virtual int GetJumpLocation(LubFunction function) {
				return function.InstructionIndex + 2;
			}

			#endregion

			public virtual void Output(LubFunction function) {
				AbstractInstruction jmp = function.Instructions[function.InstructionIndex + 1];

				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.Append("if " + GetKey(RegOrKOutput(Registers[1], function)));
				builder.Append(Registers[0] == 0 ? PositiveOutput : NegativeOutput);
				builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
				builder.AppendLine(" then");

				_writeBody(builder, function, function.InstructionIndex + 2, jmp.Registers[0] + function.InstructionIndex + 2);

				LuaCode = builder.ToString();
			}

			protected void _writeBody(StringBuilder builder, LubFunction function, int gotoIf, int gotoElse) {
				builder.AppendIndent(function.FunctionLevel + 1);
				builder.Append("goto ");
				builder.Append(function.Label);
				builder.Append("_[");
				builder.Append(gotoIf);
				builder.AppendLine("]");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("else");

				builder.AppendIndent(function.FunctionLevel + 1);
				builder.Append("goto ");
				builder.Append(function.Label);
				builder.Append("_[");
				builder.Append(gotoElse);
				builder.AppendLine("]");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("end");
			}
		}

		#endregion

		#region Nested type: Eq

		public class Eq : ConditionalInstruction {
			public Eq() {
				Mode = EncodedMode.ABC;
				PositiveOutput = " == ";
				NegativeOutput = " ~= ";
			}

			protected override void _execute(LubFunction function) {
				Output(function);
			}
		}

		#endregion

		#region Nested type: Le

		public class Le : ConditionalInstruction {
			public Le() {
				Mode = EncodedMode.ABC;
				PositiveOutput = " <= ";
				NegativeOutput = " > ";
			}

			protected override void _execute(LubFunction function) {
				Output(function);
			}
		}

		#endregion

		#region Nested type: Lt

		public class Lt : ConditionalInstruction {
			public Lt() {
				Mode = EncodedMode.ABC;
				PositiveOutput = " < ";
				NegativeOutput = " >= ";
			}

			protected override void _execute(LubFunction function) {
				Output(function);
			}
		}

		#endregion

		#region Nested type: Test

		public class Test : ConditionalInstruction {
			public Test() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				// Test can also be used for while loops, but
				// this will be detected by the CodeLogic
				Output(function);
			}

			public override void Output(LubFunction function) {
				// This call is always positive (== true) to avoid dealing with annoying items
				AbstractInstruction jmp = function.Instructions[function.InstructionIndex + 1];

				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.Append("if ");

				if (Registers[2] == 0) {
					// We can simply the condition
					builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					builder.Append(" == true");
				}
				else {
					builder.Append("not ");
					builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					builder.Append(" == true");
					//builder.Append("");
				}

				builder.AppendLine(" then");

				builder.AppendIndent(function.FunctionLevel + 1);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + 2) + "]");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("end");

				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("goto " + function.Label + "_[" + (jmp.Registers[0] + function.InstructionIndex + 2) + "]");

				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: TestSet

		public class TestSet : ConditionalInstruction, IJumpingInstruction {
			public TestSet() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				// Test can also be used for while loops, but
				// this will be detected by the CodeLogic
				Output(function);
			}

			public override void Output(LubFunction function) {
				AbstractInstruction jmp = function.Instructions[function.InstructionIndex + 1];

				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.Append("if " + GetKey(RegOrKOutput(Registers[1], function)));
				builder.Append(Registers[2] == 1 ? " == false" : " == true");
				builder.AppendLine(" then");

				_writeBody(builder, function, function.InstructionIndex + 2, jmp.Registers[0] + function.InstructionIndex + 2);

				LuaCode = builder.ToString();
			}
		}

		#endregion
	}
}