using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: Call

		public class Call : AbstractInstruction, IAssigning {
			public Call() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				int stackPointer = function.Stack.Pointer;

				ILubObject functionToCall = function.Stack[Registers[0]];
				StringBuilder builder = new StringBuilder();

				if (Registers[2] == 1) {
					Append = true;
					builder.AppendIndent(function.FunctionLevel);
				}

				// Alright, this is a bit tricky because we have to peek the operands, which
				// we never do so there's no easy way to do that.
				// If there are multiple return values, the decompiler will keep going until
				// all the values get assigned.

				if (Registers[2] != 1) {
					if (Registers[2] > 2) {
						if (function.Instructions.Skip(function.InstructionIndex + 1).Take(Registers[2] - 1).All(p => p is SetGlobal || p is Move)) {
							builder.AppendIndent(function.FunctionLevel);
							Append = true;
							ForceAssigning(builder, function);

							// Return values are either setglobal or move
							InstructionJump = Registers[2] - 1;

							for (int i = Registers[2] - 2; i >= 0; i--) {
								AbstractInstruction op = function.Instructions[function.InstructionIndex + i + 1];

								if (op is SetGlobal) {
									builder.Append(function.Constants[op.Registers[1]]);
								}
								else if (op is Move) {
									builder.Append(GetKey(function.Stack[op.Registers[0]]));
								}

								if (i > 0) {
									builder.Append(", ");
								}
							}

							builder.Append(" = ");
						}
					}
				}

				builder.Append(GetKey(functionToCall));
				AppendParameters(builder, Registers, stackPointer, function);

				string value = builder.ToString();

				function.Stack[Registers[0]] = new LubOutput(value);

				if (Append) {
					builder.AppendLine();
				}

				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: Close

		public class Close : AbstractInstruction {
			public Close() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = function.Functions[Registers[1]];
			}
		}

		#endregion

		#region Nested type: Closure

		public class Closure : AbstractInstruction {
			public Closure() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = function.Functions[Registers[1]];
			}
		}

		#endregion

		#region Nested type: Return

		public class Return : AbstractInstruction, IReturnInstruction, IAssigning {
			public Return() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				int stackPointer = function.Stack.Pointer;

				Append = true;

				if (function.FunctionLevel == 0) {
					return;
				}

				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.Append("return");

				if (Registers[1] != 1)
					builder.Append(" ");

				//AppendParameters(builder, Registers, stackPointer, function);
				if (Registers[1] == 1) {
				}
				else if (Registers[1] > 1) {
					for (int i = 0; i < Registers[1] - 1; i++) {
						ILubObject obj = function.Stack[Registers[0] + i];

						if (obj is LubFunction) {
							LubFunction objFunction = (LubFunction) obj;
							objFunction.Print(builder, function.FunctionLevel + 1);
						}
						else {
							builder.Append(GetKey(RegOutput(Registers[0] + i, function)));
						}

						if (i < Registers[1] - 2) {
							builder.Append(", ");
						}
					}
				}
				else {
					for (int i = Registers[0]; i <= stackPointer; i++) {
						builder.Append(GetKey(RegOutput(i, function)));

						if (i < stackPointer) {
							builder.Append(", ");
						}
					}
				}

				builder.AppendLine();
				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: TailCall

		public class TailCall : AbstractInstruction, IReturnInstruction {
			public TailCall() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				int stackPointer = function.Stack.Pointer;

				Append = true;

				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.Append("return ");

				ILubObject functionToCall = function.Stack[Registers[0]];
				builder.Append(GetKey(functionToCall));

				if (Registers[2] == 1) {
					// Will never happen... ?
					builder.AppendLine("()");
					LuaCode = builder.ToString();
					return;
				}

				builder.Append("(");

				if (Registers[1] > 1) {
					for (int i = 1; i < Registers[1]; i++) {
						ILubObject obj = function.Stack[Registers[0] + i];

						if (obj is LubFunction) {
							LubFunction objFunction = (LubFunction) obj;
							objFunction.Print(builder, function.FunctionLevel + 1);
						}
						else {
							builder.Append(GetKey(RegOutput(Registers[0] + i, function)));
						}

						if (i < Registers[1] - 1) {
							builder.Append(", ");
						}
					}

					//for (int i = 0; i < Registers[1] - 1; i++) {
					//    builder.Append(GetKey(RegOutput(Registers[0] + i + 1, function)));

					//    if (i < Registers[1] - 2) {
					//        builder.Append(", ");
					//    }
					//}
				}
				else if (Registers[1] == 0) {
					for (int i = Registers[0]; i <= stackPointer; i++) {
						builder.Append(GetKey(RegOutput(i, function)));

						if (i < stackPointer) {
							builder.Append(", ");
						}
					}
				}

				builder.AppendLine(")");
				LuaCode = builder.ToString();
			}
		}

		#endregion
	}
}