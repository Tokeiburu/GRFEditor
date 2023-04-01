using System;
using System.Collections.Generic;
using GRF.FileFormats.RsmFormat;

namespace GRF.Graphics {
	public class Matrix4 {
		protected bool Equals(Matrix4 other) {
			return Equals(_values, other._values);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Matrix4)obj);
		}

		public override int GetHashCode() {
			return (_values != null ? _values.GetHashCode() : 0);
		}

		private readonly float[] _values = new float[16];
		public Vertex Offset {
			get { return new Vertex(this[12], this[13], this[14]); }
			set {
				this[12] = value.X;
				this[13] = value.Y;
				this[14] = value.Z;
			}
		}

		public Vertex ScaleOffset {
			get { return new Vertex(this[0], this[5], this[10]); }
			set {
				this[0] = value.X;
				this[5] = value.Y;
				this[10] = value.Z;
			}
		}

		public float X { get { return this[12]; } }
		public float Y { get { return this[13]; } }
		public float Z { get { return this[14]; } }

		public Matrix4() {

		}

		public Matrix4(Matrix3 mat) {
			this[15] = 1;
			this[14] = 0;
			this[13] = 0;
			this[12] = 0;

			this[11] = 0;
			this[10] = mat[8];
			this[9] = mat[7];
			this[8] = mat[6];

			this[7] = 0;
			this[6] = mat[5];
			this[5] = mat[4];
			this[4] = mat[3];

			this[3] = 0;
			this[2] = mat[2];
			this[1] = mat[1];
			this[0] = mat[0];
		}

		public Matrix4(IList<float> values) {
			_values[0] = values[0];
			_values[1] = values[1];
			_values[2] = values[2];
			_values[3] = values[3];
			_values[4] = values[4];
			_values[5] = values[5];
			_values[6] = values[6];
			_values[7] = values[7];
			_values[8] = values[8];
			_values[9] = values[9];
			_values[10] = values[10];
			_values[11] = values[11];
			_values[12] = values[12];
			_values[13] = values[13];
			_values[14] = values[14];
			_values[15] = values[15];
		}

		public Matrix4(Matrix4 matrix) : this(matrix.Values) { }

		public Matrix4(
			float m11, float m12, float m13, float m14,
			float m21, float m22, float m23, float m24,
			float m31, float m32, float m33, float m34,
			float m41, float m42, float m43, float m44
			) {
			_values[0] = m11; _values[1] = m12; _values[2] = m13; _values[3] = m14;
			_values[4] = m21; _values[5] = m22; _values[6] = m23; _values[7] = m24;
			_values[8] = m31; _values[9] = m32; _values[10] = m33; _values[11] = m34;
			_values[12] = m41; _values[13] = m42; _values[14] = m43; _values[15] = m44;
		}

		public static Matrix4 Identity {
			get {
				return new Matrix4(
					1, 0, 0, 0,
					0, 1, 0, 0,
					0, 0, 1, 0,
					0, 0, 0, 1);
			}
		}

		private static readonly Matrix4 _bufferedIdentity = Identity;
		public static Matrix4 BufferedIdentity {
			get { return _bufferedIdentity; }
		}

		public float[] Values {
			get { return _values; }
		}

		public float this[int rowIndex, int columnIndex] {
			get { return _values[4 * rowIndex + columnIndex]; }
			set {
				_values[4 * rowIndex + columnIndex] = value;
			}
		}

		public float this[int index] {
			get { return _values[index]; }
			set {
				_values[index] = value;
			}
		}

		public void SelfTranslate(Vertex vertex) {
			SelfTranslate(vertex.X, vertex.Y, vertex.Z);
		}

		public void SelfTranslate(float x, float y, float z) {
			if (x == 0 && y == 0 && z == 0)
				return;
			//this[12] += x * this[15];
			//this[13] += y * this[15];
			//this[14] += z * this[15];

			// Fix : 2015-04-24
			// What... the hell is this?
			this[12] = this[0] * x + this[4] * y + this[8] * z + this[12];
			this[13] = this[1] * x + this[5] * y + this[9] * z + this[13];
			this[14] = this[2] * x + this[6] * y + this[10] * z + this[14];
			this[15] = this[3] * x + this[7] * y + this[11] * z + this[15];
		}

		public static Matrix4 Translate(Matrix4 mat, Vertex vertex) {
			mat.SelfTranslate(vertex);
			return mat;
		}

		public static Matrix4 Translate(Matrix4 mat, float x, float y, float z) {
			mat.SelfTranslate(x, y, z);
			return mat;
		}

		public static Matrix4 RotateZ(Matrix4 a, float radian) {
			if (radian == 0)
				return a;

			Matrix4 output = new Matrix4();

			float s = (float)Math.Sin(radian),
				  c = (float)Math.Cos(radian),
				  a00 = a[0],
				  a01 = a[1],
				  a02 = a[2],
				  a03 = a[3],
				  a10 = a[4],
				  a11 = a[5],
				  a12 = a[6],
				  a13 = a[7];

			if (a != output) {
				output[8] = a[8];
				output[9] = a[9];
				output[10] = a[10];
				output[11] = a[11];
				output[12] = a[12];
				output[13] = a[13];
				output[14] = a[14];
				output[15] = a[15];
			}

			output[0] = a00 * c + a10 * s;
			output[1] = a01 * c + a11 * s;
			output[2] = a02 * c + a12 * s;
			output[3] = a03 * c + a13 * s;
			output[4] = a10 * c - a00 * s;
			output[5] = a11 * c - a01 * s;
			output[6] = a12 * c - a02 * s;
			output[7] = a13 * c - a03 * s;
			return output;
		}

		public static Matrix4 RotateY(Matrix4 a, float radian) {
			if (radian == 0)
				return a;

			Matrix4 output = new Matrix4();
			float s = (float)Math.Sin(radian),
				  c = (float)Math.Cos(radian),
				  a00 = a[0],
				  a01 = a[1],
				  a02 = a[2],
				  a03 = a[3],
				  a20 = a[8],
				  a21 = a[9],
				  a22 = a[10],
				  a23 = a[11];

			if (a != output) {
				output[4] = a[4];
				output[5] = a[5];
				output[6] = a[6];
				output[7] = a[7];
				output[12] = a[12];
				output[13] = a[13];
				output[14] = a[14];
				output[15] = a[15];
			}

			output[0] = a00 * c - a20 * s;
			output[1] = a01 * c - a21 * s;
			output[2] = a02 * c - a22 * s;
			output[3] = a03 * c - a23 * s;
			output[8] = a00 * s + a20 * c;
			output[9] = a01 * s + a21 * c;
			output[10] = a02 * s + a22 * c;
			output[11] = a03 * s + a23 * c;
			return output;
		}

		public static Matrix4 RotateX(Matrix4 a, float radian) {
			if (radian == 0)
				return a;

			Matrix4 output = new Matrix4();
			float s = (float)Math.Sin(radian),
				  c = (float)Math.Cos(radian),
				  a10 = a[4],
				  a11 = a[5],
				  a12 = a[6],
				  a13 = a[7],
				  a20 = a[8],
				  a21 = a[9],
				  a22 = a[10],
				  a23 = a[11];

			if (a != output) {
				output[0] = a[0];
				output[1] = a[1];
				output[2] = a[2];
				output[3] = a[3];
				output[12] = a[12];
				output[13] = a[13];
				output[14] = a[14];
				output[15] = a[15];
			}

			output[4] = a10 * c + a20 * s;
			output[5] = a11 * c + a21 * s;
			output[6] = a12 * c + a22 * s;
			output[7] = a13 * c + a23 * s;
			output[8] = a20 * c - a10 * s;
			output[9] = a21 * c - a11 * s;
			output[10] = a22 * c - a12 * s;
			output[11] = a23 * c - a13 * s;
			return output;
		}

		public static Matrix4 Scale(Matrix4 matrix, Vertex scale) {
			if (scale.X == 1 && scale.Y == 1 && scale.Z == 1)
				return matrix;

			Matrix4 matrixScale = Identity;
			matrixScale[0] = scale.X;
			matrixScale[5] = scale.Y;
			matrixScale[10] = scale.Z;
			return Multiply(matrixScale, matrix);
		}

		public static Matrix4 Scale(Matrix4 matrix, float scale) {
			if (scale == 1)
				return matrix;

			Matrix4 matrixScale = Identity;
			matrixScale[0] = scale;
			matrixScale[5] = scale;
			matrixScale[10] = scale;
			return Multiply(matrixScale, matrix);
		}

		public static Matrix4 Rotate(Matrix4 matrix, Vertex axis, float angle) {
			Matrix4 matrixRotate = Identity;
			matrixRotate[0] = (float)(Math.Cos(angle) + axis.X * axis.X * (1 - Math.Cos(angle)));
			matrixRotate[1] = (float)(axis.X * axis.Y * (1 - Math.Cos(angle)) - axis.Z * Math.Sin(angle));
			matrixRotate[2] = (float)(axis.X * axis.Z * (1 - Math.Cos(angle)) + axis.Y * Math.Sin(angle));

			matrixRotate[4] = (float)(axis.Y * axis.X * (1 - Math.Cos(angle)) + axis.Z * Math.Sin(angle));
			matrixRotate[5] = (float)(Math.Cos(angle) + axis.Y * axis.Y * (1 - Math.Cos(angle)));
			matrixRotate[6] = (float)(axis.Y * axis.Z * (1 - Math.Cos(angle)) + axis.X * Math.Sin(angle));

			matrixRotate[8] = (float)(axis.Z * axis.X * (1 - Math.Cos(angle)) - axis.Y * Math.Sin(angle));
			matrixRotate[9] = (float)(axis.Y * axis.X * (1 - Math.Cos(angle)) + axis.X * Math.Sin(angle));
			matrixRotate[10] = (float)(Math.Cos(angle) + axis.Z * axis.Z * (1 - Math.Cos(angle)));
			return Multiply(matrixRotate, matrix);
		}

		public static Matrix4 Rotate3(Matrix4 a, Vertex axis, float rad) {
			var x = axis[0];
			var y = axis[1];
			var z = axis[2];
			var len = (float)Math.Sqrt(x * x + y * y + z * z);
			float
				s, c, t,
				a00, a01, a02, a03,
				a10, a11, a12, a13,
				a20, a21, a22, a23,
				b00, b01, b02,
				b10, b11, b12,
				b20, b21, b22;

			Matrix4 toRet = new Matrix4();

			if (Math.Abs(len) < 0.001) {
				return a;
			}

			len = 1 / len;
			x *= len;
			y *= len;
			z *= len;

			s = (float)Math.Sin(rad);
			c = (float)Math.Cos(rad);
			t = 1 - c;

			a00 = a[0];
			a01 = a[1];
			a02 = a[2];
			a03 = a[3];
			a10 = a[4];
			a11 = a[5];
			a12 = a[6];
			a13 = a[7];
			a20 = a[8];
			a21 = a[9];
			a22 = a[10];
			a23 = a[11];

			// Construct the elements of the rotation matrix
			b00 = x * x * t + c;
			b01 = y * x * t + z * s;
			b02 = z * x * t - y * s;
			b10 = x * y * t - z * s;
			b11 = y * y * t + c;
			b12 = z * y * t + x * s;
			b20 = x * z * t + y * s;
			b21 = y * z * t - x * s;
			b22 = z * z * t + c;

			// Perform rotation-specific matrix multiplication
			toRet[0] = a00 * b00 + a10 * b01 + a20 * b02;
			toRet[1] = a01 * b00 + a11 * b01 + a21 * b02;
			toRet[2] = a02 * b00 + a12 * b01 + a22 * b02;
			toRet[3] = a03 * b00 + a13 * b01 + a23 * b02;
			toRet[4] = a00 * b10 + a10 * b11 + a20 * b12;
			toRet[5] = a01 * b10 + a11 * b11 + a21 * b12;
			toRet[6] = a02 * b10 + a12 * b11 + a22 * b12;
			toRet[7] = a03 * b10 + a13 * b11 + a23 * b12;
			toRet[8] = a00 * b20 + a10 * b21 + a20 * b22;
			toRet[9] = a01 * b20 + a11 * b21 + a21 * b22;
			toRet[10] = a02 * b20 + a12 * b21 + a22 * b22;
			toRet[11] = a03 * b20 + a13 * b21 + a23 * b22;

			if (a != toRet) { // If the source and destination differ, copy the unchanged last row
				toRet[12] = a[12];
				toRet[13] = a[13];
				toRet[14] = a[14];
				toRet[15] = a[15];
			}
			return toRet;
		}

		public static Matrix4 Rotate2(Matrix4 matrix, Vertex axis, float angle) {
			if (angle == 0)
				return matrix;

			Matrix4 matrixRotate = Identity;

			float c = (float)Math.Cos(angle);
			float s = (float)Math.Sin(angle);
			float t = 1.0f - c;

			var normalizedAxis = Vertex.Normalize(axis);
			float x = normalizedAxis.X;
			float y = normalizedAxis.Y;
			float z = normalizedAxis.Z;

			matrixRotate[0] = 1 + t * (x * x - 1);
			matrixRotate[1] = z * s + t * x * y;
			matrixRotate[2] = -y * s + t * x * z;
			matrixRotate[3] = 0.0f;

			matrixRotate[4] = -z * s + t * x * y;
			matrixRotate[5] = 1 + t * (y * y - 1);
			matrixRotate[6] = x * s + t * y * z;

			matrixRotate[8] = y * s + t * x * z;
			matrixRotate[9] = -x * s + t * y * z;
			matrixRotate[10] = 1 + t * (z * z - 1);

			return Multiply(matrixRotate, matrix);
		}

		public static Matrix4 Rotation(Vertex axis, float angle) {
			return Rotate2(Identity, axis, angle);
		}

		public static Vertex Multiply(Matrix4 matrix, Vertex vertex) {
			float x = vertex.X;
			float y = vertex.Y;
			float z = vertex.Z;

			return new Vertex(
				matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12],
				matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13],
				matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14]
				);
		}

		public static Vertex MultiplyNoOffsets(Matrix4 matrix, Vertex vertex) {
			float x = vertex.X;
			float y = vertex.Y;
			float z = vertex.Z;

			return new Vertex(
				matrix[0] * x + matrix[4] * y + matrix[8] * z,
				matrix[1] * x + matrix[5] * y + matrix[9] * z,
				matrix[2] * x + matrix[6] * y + matrix[10] * z
				);
		}

		public static Matrix4 Multiply(Matrix4 matrix1, Matrix4 matrix2) {
			Matrix4 output = new Matrix4();

			float a00 = matrix1[0], a01 = matrix1[1], a02 = matrix1[2], a03 = matrix1[3],
				a10 = matrix1[4], a11 = matrix1[5], a12 = matrix1[6], a13 = matrix1[7],
				a20 = matrix1[8], a21 = matrix1[9], a22 = matrix1[10], a23 = matrix1[11],
				a30 = matrix1[12], a31 = matrix1[13], a32 = matrix1[14], a33 = matrix1[15];

			float b0 = matrix2[0], b1 = matrix2[1], b2 = matrix2[2], b3 = matrix2[3];
			output[0] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
			output[1] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
			output[2] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
			output[3] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

			b0 = matrix2[4]; b1 = matrix2[5]; b2 = matrix2[6]; b3 = matrix2[7];
			output[4] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
			output[5] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
			output[6] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
			output[7] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

			b0 = matrix2[8]; b1 = matrix2[9]; b2 = matrix2[10]; b3 = matrix2[11];
			output[8] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
			output[9] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
			output[10] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
			output[11] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

			b0 = matrix2[12]; b1 = matrix2[13]; b2 = matrix2[14]; b3 = matrix2[15];
			output[12] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
			output[13] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
			output[14] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
			output[15] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

			return output;
		}

		public static Matrix4 ExtractRotation(Matrix4 mat) {
			var scaleX = 1.0 / Vertex.CalculateLength(mat[0], mat[1], mat[2]);
			var scaleY = 1.0 / Vertex.CalculateLength(mat[4], mat[5], mat[6]);
			var scaleZ = 1.0 / Vertex.CalculateLength(mat[8], mat[9], mat[10]);

			Matrix4 outputMatrix = new Matrix4();
			outputMatrix[0] = (float)(mat[0] * scaleX);
			outputMatrix[1] = (float)(mat[1] * scaleX);
			outputMatrix[2] = (float)(mat[2] * scaleX);
			outputMatrix[4] = (float)(mat[4] * scaleY);
			outputMatrix[5] = (float)(mat[5] * scaleY);
			outputMatrix[6] = (float)(mat[6] * scaleY);
			outputMatrix[8] = (float)(mat[8] * scaleZ);
			outputMatrix[9] = (float)(mat[9] * scaleZ);
			outputMatrix[10] = (float)(mat[10] * scaleZ);

			return outputMatrix;
		}

		// ReSharper disable CompareOfFloatsByEqualityOperator
		public static bool operator ==(Matrix4 m1, Matrix4 m2) {
			if (ReferenceEquals(m1, m2)) return true;
			if (ReferenceEquals(m1, null)) return false;
			if (ReferenceEquals(m2, null)) return false;

			for (int i = 0; i < 16; i++) {
				if (m1._values[0] != m2._values[1])
					return false;
			}

			return true;
		}

		public static bool operator !=(Matrix4 m1, Matrix4 m2) {
			return !(m1 == m2);
		}

		public static Matrix4 RotateQuat(Matrix4 mat, RotKeyFrame w) {
			float a, b, c, d;
			a = w[0];
			b = w[1];
			c = w[2];
			d = w[3];

			var norm = (float)Math.Sqrt(a * a + b * b + c * c + d * d);
			a /= norm;
			b /= norm;
			c /= norm;
			d /= norm;

			return Multiply(mat, new Matrix4(
									 (float)(1.0 - 2.0 * (b * b + c * c)), (float)(2.0 * (a * b + c * d)), (float)(2.0 * (a * c - b * d)), (float)(0.0),
									 (float)(2.0 * (a * b - c * d)), (float)(1.0 - 2.0 * (a * a + c * c)), (float)(2.0 * (c * b + a * d)), (float)(0.0),
									 (float)(2.0 * (a * c + b * d)), (float)(2.0 * (b * c - a * d)), (float)(1.0 - 2.0 * (a * a + b * b)), (float)(0.0),
									 (float)(0.0), (float)(0.0), (float)(0.0), (float)(1.0)
									 ));
		}

		/// <summary>
		/// Appends a rotation transform to the current <see cref="T:System.Windows.Media.Media3D.Matrix3D"/>.
		/// </summary>
		/// <param name="quaternion"><see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the rotation.</param>
		public static Matrix4 Rotate(Matrix4 mat, TkQuaternion quaternion) {
			Vertex center = new Vertex();
			return Multiply2(mat, CreateRotationMatrix(ref quaternion, ref center));
		}

		public static Matrix4 Multiply2(Matrix4 matrix1, Matrix4 matrix2) {
			return new Matrix4(matrix1[0] * matrix2[0] + matrix1[1] * matrix2[4] + matrix1[2] * matrix2[8] + matrix1[3] * matrix2[12], matrix1[0] * matrix2[1] + matrix1[1] * matrix2[5] + matrix1[2] * matrix2[9] + matrix1[3] * matrix2[13], matrix1[0] * matrix2[2] + matrix1[1] * matrix2[6] + matrix1[2] * matrix2[10] + matrix1[3] * matrix2[14], matrix1[0] * matrix2[3] + matrix1[1] * matrix2[7] + matrix1[2] * matrix2[11] + matrix1[3] * matrix2[15], matrix1[4] * matrix2[0] + matrix1[5] * matrix2[4] + matrix1[6] * matrix2[8] + matrix1[7] * matrix2[12], matrix1[4] * matrix2[1] + matrix1[5] * matrix2[5] + matrix1[6] * matrix2[9] + matrix1[7] * matrix2[13], matrix1[4] * matrix2[2] + matrix1[5] * matrix2[6] + matrix1[6] * matrix2[10] + matrix1[7] * matrix2[14], matrix1[4] * matrix2[3] + matrix1[5] * matrix2[7] + matrix1[6] * matrix2[11] + matrix1[7] * matrix2[15], matrix1[8] * matrix2[0] + matrix1[9] * matrix2[4] + matrix1[10] * matrix2[8] + matrix1[11] * matrix2[12], matrix1[8] * matrix2[1] + matrix1[9] * matrix2[5] + matrix1[10] * matrix2[9] + matrix1[11] * matrix2[13], matrix1[8] * matrix2[2] + matrix1[9] * matrix2[6] + matrix1[10] * matrix2[10] + matrix1[11] * matrix2[14], matrix1[8] * matrix2[3] + matrix1[9] * matrix2[7] + matrix1[10] * matrix2[11] + matrix1[11] * matrix2[15], matrix1[12] * matrix2[0] + matrix1[13] * matrix2[4] + matrix1[14] * matrix2[8] + matrix1[15] * matrix2[12], matrix1[12] * matrix2[1] + matrix1[13] * matrix2[5] + matrix1[14] * matrix2[9] + matrix1[15] * matrix2[13], matrix1[12] * matrix2[2] + matrix1[13] * matrix2[6] + matrix1[14] * matrix2[10] + matrix1[15] * matrix2[14], matrix1[12] * matrix2[3] + matrix1[13] * matrix2[7] + matrix1[14] * matrix2[11] + matrix1[15] * matrix2[15]);
		}

		public static Vertex Multiply2(Matrix4 matrix, Vertex vertex) {
			float x = vertex.X;
			float y = vertex.Y;
			float z = vertex.Z;

			return new Vertex(
				matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12],
				matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13],
				matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14]
				);
		}

		internal static Matrix4 CreateRotationMatrix(ref TkQuaternion quaternion, ref Vertex center) {
			Matrix4 matrix3D = Identity;
			
			float num1 = quaternion.X + quaternion.X;
			float num2 = quaternion.Y + quaternion.Y;
			float num3 = quaternion.Z + quaternion.Z;
			float num4 = quaternion.X * num1;
			float num5 = quaternion.X * num2;
			float num6 = quaternion.X * num3;
			float num7 = quaternion.Y * num2;
			float num8 = quaternion.Y * num3;
			float num9 = quaternion.Z * num3;
			float num10 = quaternion.W * num1;
			float num11 = quaternion.W * num2;
			float num12 = quaternion.W * num3;
			matrix3D[0] = 1.0f - (num7 + num9);
			matrix3D[1] = num5 + num12;
			matrix3D[2] = num6 - num11;
			matrix3D[4] = num5 - num12;
			matrix3D[5] = 1.0f - (num4 + num9);
			matrix3D[6] = num8 + num10;
			matrix3D[8] = num6 + num11;
			matrix3D[9] = num8 - num10;
			matrix3D[10] = 1.0f - (num4 + num7);
			if (center.X != 0.0 || center.Y != 0.0 || center.Z != 0.0) {
				matrix3D[12] = -center.X * matrix3D[0] - center.Y * matrix3D[4] - center.Z * matrix3D[8] + center.X;
				matrix3D[13] = -center.X * matrix3D[1] - center.Y * matrix3D[5] - center.Z * matrix3D[9] + center.Y;
				matrix3D[14] = -center.X * matrix3D[2] - center.Y * matrix3D[6] - center.Z * matrix3D[10] + center.Z;
			}
			return matrix3D;
		}

		public bool IsDistinguishedIdentity {
			get {
				return Identity == this;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether this <see cref="T:System.Windows.Media.Media3D.Matrix3D"/> structure is affine.
		/// </summary>
		/// 
		/// <returns>
		/// true if the Matrix3D structure is affine; otherwise, false.
		/// </returns>
		public bool IsAffine {
			get {
				if (this.IsDistinguishedIdentity)
					return true;
				if (this[3] == 0.0 && this[7] == 0.0 && this[11] == 0.0)
					return this[15] == 1.0;
				else
					return false;
			}
		}

		public static bool IsZero(float value) {
			return Math.Abs(value) < 2.22044604925031E-15;
		}

		internal bool NormalizedAffineInvert() {
			float num1 = this[1] * this[6] - this[5] * this[2];
			float num2 = this[9] * this[2] - this[1] * this[10];
			float num3 = this[5] * this[10] - this[9] * this[6];
			float num4 = this[8] * num1 + this[4] * num2 + this[0] * num3;
			if (IsZero(num4))
				return false;
			float num5 = this[4] * this[2] - this[0] * this[6];
			float num6 = this[0] * this[10] - this[8] * this[2];
			float num7 = this[8] * this[6] - this[4] * this[10];
			float num8 = this[0] * this[5] - this[4] * this[1];
			float num9 = this[0] * this[9] - this[8] * this[1];
			float num10 = this[0] * this[13] - this[12] * this[1];
			float num11 = this[4] * this[9] - this[8] * this[5];
			float num12 = this[4] * this[13] - this[12] * this[5];
			float num13 = this[8] * this[13] - this[12] * this[9];
			float num14 = this[6] * num10 - this[14] * num8 - this[2] * num12;
			float num15 = this[2] * num13 - this[10] * num10 + this[14] * num9;
			float num16 = this[10] * num12 - this[14] * num11 - this[6] * num13;
			float num17 = num8;
			float num18 = -num9;
			float num19 = num11;
			float num20 = 1.0f / num4;
			this[0] = num3 * num20;
			this[1] = num2 * num20;
			this[2] = num1 * num20;
			this[4] = num7 * num20;
			this[5] = num6 * num20;
			this[6] = num5 * num20;
			this[8] = num19 * num20;
			this[9] = num18 * num20;
			this[10] = num17 * num20;
			this[12] = num16 * num20;
			this[13] = num15 * num20;
			this[14] = num14 * num20;
			return true;
		}

		internal bool InvertCore() {
			if (this.IsDistinguishedIdentity)
				return true;
			if (this.IsAffine)
				return this.NormalizedAffineInvert();
			float num1 = this[2] * this[7] - this[6] * this[3];
			float num2 = this[2] * this[11] - this[10] * this[3];
			float num3 = this[2] * this[15] - this[14] * this[3];
			float num4 = this[6] * this[11] - this[10] * this[7];
			float num5 = this[6] * this[15] - this[14] * this[7];
			float num6 = this[10] * this[15] - this[14] * this[11];
			float num7 = this[5] * num2 - this[9] * num1 - this[1] * num4;
			float num8 = this[1] * num5 - this[5] * num3 + this[13] * num1;
			float num9 = this[9] * num3 - this[13] * num2 - this[1] * num6;
			float num10 = this[5] * num6 - this[9] * num5 + this[13] * num4;
			float num11 = this[12] * num7 + this[8] * num8 + this[4] * num9 + this[0] * num10;
			if (IsZero(num11))
				return false;
			float num12 = this[0] * num4 - this[4] * num2 + this[8] * num1;
			float num13 = this[4] * num3 - this[12] * num1 - this[0] * num5;
			float num14 = this[0] * num6 - this[8] * num3 + this[12] * num2;
			float num15 = this[8] * num5 - this[12] * num4 - this[4] * num6;
			float num16 = this[0] * this[5] - this[4] * this[1];
			float num17 = this[0] * this[9] - this[8] * this[1];
			float num18 = this[0] * this[13] - this[12] * this[1];
			float num19 = this[4] * this[9] - this[8] * this[5];
			float num20 = this[4] * this[13] - this[12] * this[5];
			float num21 = this[8] * this[13] - this[12] * this[9];
			float num22 = this[2] * num19 - this[6] * num17 + this[10] * num16;
			float num23 = this[6] * num18 - this[14] * num16 - this[2] * num20;
			float num24 = this[2] * num21 - this[10] * num18 + this[14] * num17;
			float num25 = this[10] * num20 - this[14] * num19 - this[6] * num21;
			float num26 = this[7] * num17 - this[11] * num16 - this[3] * num19;
			float num27 = this[3] * num20 - this[7] * num18 + this[15] * num16;
			float num28 = this[11] * num18 - this[15] * num17 - this[3] * num21;
			float num29 = this[7] * num21 - this[11] * num20 + this[15] * num19;
			float num30 = 1.0f / num11;
			this[0] = num10 * num30;
			this[1] = num9 * num30;
			this[2] = num8 * num30;
			this[3] = num7 * num30;
			this[4] = num15 * num30;
			this[5] = num14 * num30;
			this[6] = num13 * num30;
			this[7] = num12 * num30;
			this[8] = num29 * num30;
			this[9] = num28 * num30;
			this[10] = num27 * num30;
			this[11] = num26 * num30;
			this[12] = num25 * num30;
			this[13] = num24 * num30;
			this[14] = num23 * num30;
			this[15] = num22 * num30;
			return true;
		}

		/// <summary>
		/// Inverts this <see cref="T:System.Windows.Media.Media3D.Matrix3D"/> structure.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Throws InvalidOperationException if the matrix is not invertible.</exception>
		public Matrix4 Invert() {
			this.InvertCore();
			return this;
		}
	}
}
