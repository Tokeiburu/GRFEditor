using GRF.FileFormats.LubFormat.Types;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: Add

		public class Add : MathInstruction {
			public Add() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), " + ", GetKey(RegOrK(Registers[2], function)));
			}
		}

		#endregion

		#region Nested type: Div

		public class Div : MathInstruction {
			public Div() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), " / ", GetKey(RegOrK(Registers[2], function)));
			}
		}

		#endregion

		#region Nested type: Len

		public class Len : MathInstruction {
			public Len() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubOutput("#" + GetKey(RegOutput(Registers[1], function)));
			}
		}

		#endregion

		#region Nested type: MathInstruction

		public abstract class MathInstruction : AbstractInstruction {
		}

		#endregion

		#region Nested type: Mod

		public class Mod : MathInstruction {
			public Mod() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), " % ", GetKey(RegOrK(Registers[2], function)));
			}
		}

		#endregion

		#region Nested type: Mul

		public class Mul : MathInstruction {
			public Mul() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), " * ", GetKey(RegOrK(Registers[2], function)));
			}
		}

		#endregion

		#region Nested type: Pow

		public class Pow : AbstractInstruction {
			public Pow() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), " ^ ", GetKey(RegOrK(Registers[2], function)));
			}
		}

		#endregion

		#region Nested type: Sub

		public class Sub : AbstractInstruction {
			public Sub() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), " - ", GetKey(RegOrK(Registers[2], function)));
			}
		}

		#endregion

		#region Nested type: UnaryMinus

		public class UnaryMinus : MathInstruction {
			public UnaryMinus() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = new LubMathOutput(GetKey(RegOrK(Registers[1], function)), "- ");
			}
		}

		#endregion
	}
}