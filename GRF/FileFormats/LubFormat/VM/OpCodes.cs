using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: AbstractInstruction

		public abstract class AbstractInstruction {
			protected EncodedMode Mode;
			protected LubValueType _dump1;
			public List<int> Registers { get; set; }

			public bool Append { get; set; }
			public string LuaCode { get; set; }
			public int InstructionJump { get; set; }

			public static LubStack DebugStack { get; set; }
			public static LubFunction DebugFunction { get; set; }

			public void Load(int instruction, Lub decompiler) {
				_loadRegisters(instruction, Mode, decompiler);
			}

			protected virtual void _loadRegisters(int instruction, EncodedMode mode, Lub decompiler) {
				Registers = decompiler.Header.OperandCodeReader.GetResiters(instruction, mode);
			}

			public void Execute(LubFunction function) {
				try {
					_execute(function);
				}
				catch {
					LubErrorHandler.Handle("Failed to decode the instruction {" + this + "} for the function " + function.Label + ", at " + function.InstructionIndex + ".", LubSourceError.CodeDecompiler);
					//_execute(function);
				}
			}

			protected abstract void _execute(LubFunction function);

			public string Dump() {
				string toReturn = Mode.ToString();

				switch (Mode) {
					case EncodedMode.ABC:
						return toReturn + String.Format(" - {0}, {1}, {2}", Registers[0], Registers[1], Registers[2]);
					case EncodedMode.ABx:
						return toReturn + String.Format(" - {0}, {1}", Registers[0], Registers[1]);
					case EncodedMode.AsBx:
						return toReturn + String.Format(" - {0}, {1}", Registers[0], Registers[1]);
					case EncodedMode.SBx:
						return toReturn + String.Format(" - {0}", Registers[1]);
					default:
						return toReturn;
				}
			}

			public override string ToString() {
				try {
					string[] toRet = new string[] { "", "", "" };

					switch (Mode) {
						case EncodedMode.ABC:
							toRet[0] = Registers[0].ToString(CultureInfo.InvariantCulture);
							toRet[1] = Registers[1].ToString(CultureInfo.InvariantCulture);
							toRet[2] = Registers[2].ToString(CultureInfo.InvariantCulture);
							break;
						case EncodedMode.ABx:
						case EncodedMode.AsBx:
							toRet[0] = Registers[0].ToString(CultureInfo.InvariantCulture);
							toRet[1] = Registers[1].ToString(CultureInfo.InvariantCulture);
							break;
						case EncodedMode.SBx:
							toRet[1] = Registers[1].ToString(CultureInfo.InvariantCulture);
							break;
					}

					return String.Format("{0,-20} {1,-5}{2,-5}{3,-5}; ", Methods.TypeToString(this), toRet[0], toRet[1], toRet[2]);
				}
				catch {
					return base.ToString();
				}
			}
		}

		#endregion

		#region Nested type: Concat

		public class Concat : AbstractInstruction {
			public Concat() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				int from = Registers[1];
				int count = Registers[2] - from + 1;
				string value = "";

				for (int i = from, end = count + from; i < end; i++) {
					value += GetKey(RegOutput(i, function));

					if (i < end - 1) {
						value += " .. ";
					}
				}

				function.Stack[Registers[0]] = new LubOutput(value);
			}
		}

		#endregion

		#region Nested type: IAssigning

		public interface IAssigning {
		}

		#endregion

		#region Nested type: IJumpingInstruction

		public interface IJumpingInstruction {
			int GetJumpLocation(LubFunction function);
		}

		#endregion

		#region Nested type: IReturnInstruction

		public interface IReturnInstruction {
		}

		#endregion

		#region Nested type: Jmp

		public class Jmp : AbstractInstruction, IJumpingInstruction {
			public Jmp() {
				Mode = EncodedMode.SBx;
			}

			#region IJumpingInstruction Members

			public int GetJumpLocation(LubFunction function) {
				return function.InstructionIndex + Registers[0] + 1;
			}

			#endregion

			protected override void _execute(LubFunction function) {
				Append = true;

				StringBuilder builder = new StringBuilder();
				ForceAssigning(builder, function);
				builder.AppendIndent(function.FunctionLevel);
				builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + Registers[0] + 1) + "]");

				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: LoadBool

		public class LoadBool : AbstractInstruction {
			public LoadBool() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubBoolean(Registers[1] != 0);

				if (Registers[2] != 0) {
					Append = true;
					StringBuilder builder = new StringBuilder();
					ForceAssigning(builder, function);
					builder.AppendIndent(function.FunctionLevel);
					builder.AppendLine("goto " + function.Label + "_[" + (function.InstructionIndex + 2) + "]");
					LuaCode = builder.ToString();
				}
			}
		}

		#endregion

		#region Nested type: LoadK

		public class LoadK : AbstractInstruction {
			public LoadK() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				function.Constants[Registers[1]].Source = LubSourceType.Constant;
				function.Stack[Registers[0]] = function.Constants[Registers[1]];
			}
		}

		#endregion

		#region Nested type: LoadNil

		public class LoadNil : AbstractInstruction {
			public LoadNil() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				Append = true;

				StringBuilder builder = new StringBuilder();

				for (int i = Registers[0]; i <= Registers[1]; i++) {
					function.Stack[i] = new LubNull();
				}

				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: Move

		public class Move : AbstractInstruction, IAssigning {
			public Move() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = function.Stack[Registers[1]];
			}
		}

		#endregion

		#region Nested type: Not

		public class Not : AbstractInstruction {
			public Not() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubOutput(" not " + GetKey(RegOrK(Registers[1], function)));
			}
		}

		#endregion

		#region Nested type: Self

		public class Self : AbstractInstruction {
			public Self() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				string table = GetKey(function.Stack[Registers[1]]).ToString();

				ILubObject acces = GetKey(RegOrK(Registers[2], function));
				LubValueType accessor = (LubValueType) acces;

				table = table + ":" + accessor;
				function.Stack[Registers[0]] = new LubOutput(table);
			}
		}

		#endregion

		#region Nested type: VarArg

		public class VarArg : AbstractInstruction {
			public VarArg() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				LubErrorHandler.Handle("Non defined behavior for VarArg.", LubSourceError.CodeDecompiler);
			}
		}

		#endregion
	}
}