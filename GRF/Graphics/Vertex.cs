using System;
using System.IO;
using GRF.IO;

namespace GRF.Graphics {
	public struct Vertex {
		public float X;
		public float Y;
		public float Z;

		public Vertex(Point point) {
			X = point[0];
			Y = point[1];
			Z = 0;
		}

		public Vertex(IBinaryReader reader) {
			X = reader.Float();
			Y = reader.Float();
			Z = reader.Float();
		}

		public Vertex(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		public Vertex(double x, double y, double z)
			: this((float)x, (float)y, (float)z) {
		}

		public Vertex(byte[] data, int offset) {
			X = BitConverter.ToSingle(data, offset);
			Y = BitConverter.ToSingle(data, offset + 4);
			Z = BitConverter.ToSingle(data, offset + 8);
		}

		public Vertex(float x, float y) {
			X = x;
			Y = y;
			Z = 0;
		}

		public Vertex(double x, double y) : this((float) x, (float) y) {
		}

		public float Length {
			get { return (float)Math.Sqrt(X * X + Y * Y + Z * Z); }
		}

		public float this[int index] {
			get {
				if (index == 0)
					return X;
				if (index == 1)
					return Y;
				if (index == 2)
					return Z;
				throw new IndexOutOfRangeException("index");
			}
			set {
				if (index == 0)
					X = value;
				if (index == 1)
					Y = value;
				if (index == 2)
					Z = value;
			}
		}

		public void Write(BinaryWriter writer) {
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}

		public static Vertex Normalize(Vertex a) {
			Vertex toReturn = new Vertex();
			float len = CalculateLength(a[0], a[1], a[2]);

			if (len > 0) {
				len = (float) (1 / Math.Sqrt(len));
				toReturn[0] = a[0] * len;
				toReturn[1] = a[1] * len;
				toReturn[2] = a[2] * len;
			}

			return toReturn;
		}

		/// <summary>
		/// Normalizes the specified <see cref="T:System.Windows.Media.Media3D.Vector3D"/> structure.
		/// </summary>
		public void Normalize() {
			float num1 = Math.Abs(this.X);
			float num2 = Math.Abs(this.Y);
			float num3 = Math.Abs(this.Z);
			if (num2 > num1)
				num1 = num2;
			if (num3 > num1)
				num1 = num3;
			this.X = this.X / num1;
			this.Y = this.Y / num1;
			this.Z = this.Z / num1;
			this = this / Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
		}

		public static float CalculateLength(float x, float y, float z) {
			return (float)Math.Sqrt(x * x + y * y + z * z);
		}

		public static Vertex CalculateNormal(Vertex a, Vertex b, Vertex c) {
			float x1, y1, z1, x2, y2, z2, x3, y3, z3, len;
	
			x1 = c[0] - b[0];
			y1 = c[1] - b[1];
			z1 = c[2] - b[2];
	
			x2 = a[0] - b[0];
			y2 = a[1] - b[1];
			z2 = a[2] - b[2];
	
			x3 = y1 * z2 - z1 * y2;
			y3 = z1 * x2 - x1 * z2;
			z3 = x1 * y2 - y1 * x2;

			len = 1 / CalculateLength(x3, y3, z3);
	
			return new Vertex(x3 * len, y3 * len, z3 * len);
		}

		public static Vertex CalculateNormal(Vertex a, Vertex b, Vertex c, Vertex d) {
			float x, y, z, x1, y1, z1, x2, y2, z2, x3, y3, z3, len;

			x1 = c[0] - b[0];
			y1 = c[1] - b[1];
			z1 = c[2] - b[2];
			x2 = a[0] - b[0];
			y2 = a[1] - b[1];
			z2 = a[2] - b[2];
			x3 = y1 * z2 - z1 * y2;
			y3 = z1 * x2 - x1 * z2;
			z3 = x1 * y2 - y1 * x2;
			len = 1 / CalculateLength(x3, y3, z3);
			x = x3 * len;
			y = y3 * len;
			z = z3 * len;

			x1 = a[0] - d[0];
			y1 = a[1] - d[1];
			z1 = a[2] - d[2];
			x2 = c[0] - d[0];
			y2 = c[1] - d[1];
			z2 = c[2] - d[2];
			x3 = y1 * z2 - z1 * y2;
			y3 = z1 * x2 - x1 * z2;
			z3 = x1 * y2 - y1 * x2;
			len = 1 / CalculateLength(x3, y3, z3);

			x += x3 * len;
			y += y3 * len;
			z += z3 * len;

			len = 1 / CalculateLength(x, y, z);
			return new Vertex(x * len, y * len, z * len);
		}

		public static Vertex operator +(Vertex a, Vertex b) {
			return new Vertex(a[0] + b[0], a[1] + b[1], a[2] + b[2]);
		}

		public static Vertex operator +(Vertex a, Point b) {
			return new Vertex(a[0] + b[0], a[1] + b[1], a[2]);
		}

		public static Vertex operator +(Point a, Vertex b) {
			return new Vertex(a[0] + b[0], a[1] + b[1], b[2]);
		}

		public static Vertex operator -(Vertex a, Vertex b) {
			return new Vertex(a[0] - b[0], a[1] - b[1], a[2] - b[2]);
		}

		public static Vertex operator -(Vertex a, Point b) {
			return new Vertex(a[0] - b[0], a[1] - b[1], a[2]);
		}

		public static Vertex operator -(Point a, Vertex b) {
			return new Vertex(a[0] - b[0], a[1] - b[1], b[2]);
		}

		public static Vertex operator -(Vertex a) {
			return new Vertex(-a[0], -a[1], -a[2]);
		}

		public static Vertex operator /(Vertex a, int vi) {
			float v = vi;
			return new Vertex(a[0] / v, a[1] / v, a[2] / v);
		}

		public static Vertex operator /(Vertex a, float v) {
			return new Vertex(a[0] / v, a[1] / v, a[2] / v);
		}

		public static Vertex operator /(Vertex a, double v) {
			return new Vertex(a[0] / v, a[1] / v, a[2] / v);
		}

		public static Vertex operator *(Vertex a, double v) {
			return new Vertex(a[0] * v, a[1] * v, a[2] * v);
		}

		public static Vertex operator *(double v, Vertex a) {
			return new Vertex(a[0] * v, a[1] * v, a[2] * v);
		}

		public static Vertex operator *(Vertex a, int v) {
			return new Vertex(a[0] * v, a[1] * v, a[2] * v);
		}

		public static Vertex operator *(int v, Vertex a) {
			return new Vertex(a[0] * v, a[1] * v, a[2] * v);
		}

		public void Translate(float x, float y, float z) {
			this[0] += x;
			this[1] += y;
			this[2] += z;
		}

		public void RotateZ(float angle) {
			double sin = Math.Sin(angle * Math.PI / 180f);
			double cos = Math.Cos(angle * Math.PI / 180f);
			float x = X;

			X = (float)(X * cos + Y * sin);
			Y = (float)(-x * sin + Y * cos);
		}

		public void RotateZ(float angle, Point center) {
			Vertex current = this - center;
			current.RotateZ(angle);
			current = current + center;

			X = current.X;
			Y = current.Y;
		}

		public void Scale(float x, float y, float z) {
			X = x * X;
			Y = y * Y;
			Z = z * Z;
		}

		public override string ToString() {
			return String.Format("X = {0}; Y = {1}; Z = {2}", X, Y, Z);
		}
	}
}
