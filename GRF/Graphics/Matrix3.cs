namespace GRF.Graphics {
	public class Matrix3 {
		private readonly float[] _values = new float[9];

		public Matrix3() {
		}

		public Matrix3(Matrix3 m) {
			for (int i = 0; i < 9; i++) {
				_values[i] = m[i];
			}
		}

		public Matrix3(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33) {
			this[0, 0] = m11;
			this[0, 1] = m12;
			this[0, 2] = m13;

			this[1, 0] = m21;
			this[1, 1] = m22;
			this[1, 2] = m23;

			this[2, 0] = m31;
			this[2, 1] = m32;
			this[2, 2] = m33;
		}

		public float[] Values {
			get { return _values; }
		}

		public float this[int rowIndex, int columnIndex] {
			get { return _values[3 * rowIndex + columnIndex]; }
			set {
				_values[3 * rowIndex + columnIndex] = value;
			}
		}

		public float this[int index] {
			get { return _values[index]; }
			set {
				_values[index] = value;
			}
		}

		public bool IsIdentity() {
			return
				_values[0] == 1 && _values[1] == 0 && _values[2] == 0 &&
				_values[3] == 0 && _values[4] == 1 && _values[5] == 0 &&
				_values[6] == 0 && _values[7] == 0 && _values[8] == 1;
		}

		public static Matrix3 Identity {
			get {
				return new Matrix3(
					1, 0, 0,
					0, 1, 0,
					0, 0, 1);
			}
		}
	}
}
