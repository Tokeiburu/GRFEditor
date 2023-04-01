using System.Globalization;
using System.Text;

namespace GRF.FileFormats.LubFormat.Types {
	public class LubMathOutput : LubOutput {
		private readonly string _value;

		public LubMathOutput(ILubObject left, string op, ILubObject right) : base(left + op + right) {
			if (left is LubMathOutput && right is LubMathOutput) {
				_value = "(" + left + ")" + op + "(" + right + ")";
			}
			else if (left is LubMathOutput) {
				_value = "(" + left + ")" + op + right;
			}
			else if (right is LubMathOutput) {
				_value = left + op + "(" + right + ")";
			}
			else {
				_value = left + op + right;
			}
		}

		public LubMathOutput(ILubObject left, string op) : base(op + left) {
			if (left is LubMathOutput) {
				_value = op + "(" + left + ")";
			}
			else {
				_value = op + left;
			}
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value);
		}

		public override int GetLength() {
			return Length ?? _value.Length;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value;
		}
	}

	public class LubOutput : LubValueType {
		private readonly string _value;

		public LubOutput(string value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value);
		}

		public override int GetLength() {
			return Length ?? _value.Length;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value;
		}
	}

	public class LubString : LubValueType {
		private readonly string _value;

		internal string Value {
			get { return _value; }
		}

		public LubString(string value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value);
		}

		public override int GetLength() {
			return Length ?? _value.Length;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value;
		}
	}

	public class LubNull : LubValueType {
		public override void Print(StringBuilder builder, int level) {
			builder.Append("nil");
		}

		public override int GetLength() {
			return 3;
		}

		public override int GetHashCode() {
			return 0;
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return "nil";
		}
	}

	public class LubBoolean : LubValueType {
		private readonly bool _value;

		public LubBoolean(bool value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value ? "true" : "false");
		}

		public override int GetLength() {
			return 5;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value ? "true" : "false";
		}
	}

	public class LubNumber : LubValueType {
		private readonly double _value;

		public LubNumber(double value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value.ToString(CultureInfo.InvariantCulture));
		}

		public override int GetLength() {
			return Length ?? (
				                 (_value < -10) ? 3 :
					                                    (_value < 0) ? 2 :
						                                                     (_value < 10) ? 1 :
							                                                                       (_value < 100) ? 2 :
								                                                                                          (_value < 1000) ? 3 :
									                                                                                                              (_value < 10000) ? 4 :
										                                                                                                                                   (_value < 100000) ? 5 :
											                                                                                                                                                         (_value < 1000000) ? 6 :
												                                                                                                                                                                                (_value < 10000000) ? 7 :
													                                                                                                                                                                                                        (_value < 100000000) ? 8 : 9);
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}