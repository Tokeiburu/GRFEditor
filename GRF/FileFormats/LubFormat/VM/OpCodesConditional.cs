using System.Text;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: ConditionalInstruction
		public abstract class ConditionalInstruction : AbstractInstruction, IJumpingInstruction {
			public string PositiveOutput { get; protected set; }
			public string NegativeOutput { get; protected set; }
			public int WhileLoop_PC_End = -1;
			public int WhileLoop_PC_Start = -1;

			#region IJumpingInstruction Members
			public virtual int GetJumpLocation(LubFunction function) {
				return function.PC + 2;
			}
			#endregion

			public virtual void Output(LubFunction function) {
				if (_logicalConditionAnalysis(function))
					return;

				AbstractInstruction jmp = function.Instructions[function.PC + 1];
				StringBuilder builder = new StringBuilder();

				if (WhileLoop_PC_End > -1) {
					builder.AppendIndent(function.BaseIndent);
					builder.Append(Lub.String_LoopPcBounds);
					builder.AppendLine(WhileLoop_PC_Start + " " + (WhileLoop_PC_End - 1));

					builder.AppendIndent(function.BaseIndent);
					builder.Append("while ");

					if (IsConstant(Registers[1], function) && !IsConstant(Registers[2], function)) {
						builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
						if (function._decompiler.Header.Version >= 5.1)
							builder.Append(RelationalStatement.InternalSwapOperands(Registers[0] == 0 ? PositiveOutput : NegativeOutput));
						else
							builder.Append(RelationalStatement.InternalSwapOperands(Registers[0] == 0 ? NegativeOutput : PositiveOutput));
						builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
					}
					else {
						builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
						if (function._decompiler.Header.Version >= 5.1)
							builder.Append(Registers[0] == 0 ? PositiveOutput : NegativeOutput);
						else
							builder.Append(Registers[0] == 0 ? NegativeOutput : PositiveOutput);
						builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
					}

					builder.AppendLine(" do");

					builder.AppendIndent(function.BaseIndent + 1);
					builder.Append("goto ");
					builder.Append(function.Label);
					builder.Append("_[");
					if (function._decompiler.Header.Version >= 5.1)
						builder.Append(function.PC + 2);
					else
						builder.Append(WhileLoop_PC_Start);
					builder.AppendLine("]");

					builder.AppendIndent(function.BaseIndent);
					builder.AppendLine("end");

					builder.AppendIndent(function.BaseIndent);
					builder.Append("goto ");
					builder.Append(function.Label);
					builder.Append("_[");
					if (function._decompiler.Header.Version >= 5.1)
						builder.Append(jmp.Registers[0] + function.PC + 2);
					else
						builder.Append(WhileLoop_PC_End);
					builder.AppendLine("]");

					LuaCode = builder.ToString();
					return;
				}

				builder.AppendIndent(function.BaseIndent);
				builder.Append("if ");

				if (IsConstant(Registers[1], function) && !IsConstant(Registers[2], function)) {
					builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
					builder.Append(RelationalStatement.InternalSwapOperands(Registers[0] == 0 ? PositiveOutput : NegativeOutput));
					builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
				}
				else {
					builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
					builder.Append(Registers[0] == 0 ? PositiveOutput : NegativeOutput);
					builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
				}

				builder.AppendLine(" then");
				_writeBody(builder, function, function.PC + 2, jmp.Registers[0] + function.PC + 2);

				LuaCode = builder.ToString();
			}

			protected void _writeBody(StringBuilder builder, LubFunction function, int gotoIf, int gotoElse) {
				builder.AppendIndent(function.BaseIndent + 1);
				builder.Append("goto ");
				builder.Append(function.Label);
				builder.Append("_[");
				builder.Append(gotoIf);
				builder.AppendLine("]");

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("else");

				builder.AppendIndent(function.BaseIndent + 1);
				builder.Append("goto ");
				builder.Append(function.Label);
				builder.Append("_[");
				builder.Append(gotoElse);
				builder.AppendLine("]");

				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("end");
			}

			public string GetPositiveCondition(LubFunction function) {
				if (this is Test) {
					StringBuilder builder = new StringBuilder();
					builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					return builder.ToString();
				}
				if (this is TestSet) {
					StringBuilder builder = new StringBuilder();
					builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
					return builder.ToString();
				}
				else {
					StringBuilder builder = new StringBuilder();

					if (IsConstant(Registers[1], function) && !IsConstant(Registers[2], function)) {
						builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
						builder.Append(RelationalStatement.InternalSwapOperands(PositiveOutput));
						builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
					}
					else {
						builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
						builder.Append(PositiveOutput);
						builder.Append(GetKey(RegOrKOutput(Registers[2], function)));
					}

					return builder.ToString();
				}
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
				if (_logicalConditionAnalysis(function))
					return;

				StringBuilder builder = new StringBuilder();
				AbstractInstruction jmp = function.Instructions[function.PC + 1];

				if (WhileLoop_PC_End > -1) {
					builder.AppendIndent(function.BaseIndent);
					builder.Append(Lub.String_LoopPcBounds);
					builder.AppendLine(WhileLoop_PC_Start + " " + (WhileLoop_PC_End - 1));

					builder.AppendIndent(function.BaseIndent);
					builder.Append("while ");

					if (Registers[2] == 0) {
						builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					}
					else {
						builder.Append("not(");
						builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
						builder.Append(")");
					}

					builder.AppendLine(" do");

					builder.AppendIndent(function.BaseIndent + 1);
					builder.Append("goto ");
					builder.Append(function.Label);
					builder.Append("_[");
					builder.Append(function.PC + 2);
					builder.AppendLine("]");

					builder.AppendIndent(function.BaseIndent);
					builder.AppendLine("end");

					builder.AppendIndent(function.BaseIndent);
					builder.Append("goto ");
					builder.Append(function.Label);
					builder.Append("_[");
					builder.Append(jmp.Registers[0] + function.PC + 2);
					builder.AppendLine("]");

					LuaCode = builder.ToString();
					return;
				}

				// This call is always positive (== true) to avoid dealing with annoying items
				builder.AppendIndent(function.BaseIndent);
				builder.Append("if ");

				if (Registers[2] == 0) {
					builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					builder.Append(" == true");
				}
				else {
					builder.Append("not(");
					builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					builder.Append(")");
				}

				builder.AppendLine(" then");
				builder.AppendIndent(function.BaseIndent + 1);
				builder.AppendLine("goto " + function.Label + "_[" + (function.PC + 2) + "]");
				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("else");
				builder.AppendIndent(function.BaseIndent + 1);
				builder.AppendLine("goto " + function.Label + "_[" + (jmp.Registers[0] + function.PC + 2) + "]");
				builder.AppendIndent(function.BaseIndent);
				builder.AppendLine("end");
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
				if (_logicalConditionAnalysis(function))
					return;

				StringBuilder builder = new StringBuilder();
				AbstractInstruction jmp = function.Instructions[function.PC + 1];

				if (WhileLoop_PC_End > -1) {
					builder.AppendIndent(function.BaseIndent);
					builder.Append(Lub.String_LoopPcBounds);
					builder.AppendLine(WhileLoop_PC_Start + " " + (WhileLoop_PC_End - 1));

					builder.AppendIndent(function.BaseIndent);
					builder.Append("while ");

					if (Registers[2] == 0) {
						builder.Append("not(");
						builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
						builder.Append(")");
					}
					else {
						builder.Append(GetKey(RegOrKOutput(Registers[0], function)));
					}

					builder.AppendLine(" do");

					builder.AppendIndent(function.BaseIndent + 1);
					builder.Append("goto ");
					builder.Append(function.Label);
					builder.Append("_[");
					builder.Append(jmp.Registers[0] + function.PC + 2);
					builder.AppendLine("]");

					builder.AppendIndent(function.BaseIndent);
					builder.AppendLine("end");

					builder.AppendIndent(function.BaseIndent);
					builder.Append("goto ");
					builder.Append(function.Label);
					builder.Append("_[");
					builder.Append(function.PC + 2);
					builder.AppendLine("]");

					LuaCode = builder.ToString();
					return;
				}

				builder.AppendIndent(function.BaseIndent);
				builder.Append("if ");

				if (Registers[2] == 0) {
					builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
					builder.Append(" == true");
				}
				else {
					builder.Append("not(");
					builder.Append(GetKey(RegOrKOutput(Registers[1], function)));
					builder.Append(")");
				}

				builder.AppendLine(" then");

				_writeBody(builder, function, function.PC + 2, jmp.Registers[0] + function.PC + 2);

				function.Stack[Registers[0]] = GetKey(RegOrKOutput(Registers[1], function));
				LuaCode = builder.ToString();
			}
		}
		#endregion
	}
}