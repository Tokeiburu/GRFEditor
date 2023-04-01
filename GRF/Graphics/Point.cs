using System;

namespace GRF.Graphics {
	public struct Point {
		public float X;
		public float Y;
		public double Lenght {
			get { return Math.Pow(X * X + Y * Y, 0.5); }
		}

		public float this[int index] {
			get {
				return index == 0 ? X : Y;
			}
		}

		public Point(float x, float y) {
			X = x;
			Y = y;
		}

		public Point(double x, double y) {
			X = (float) x;
			Y = (float) y;
		}

		public Point(Vertex vert) {
			X = vert.X;
			Y = vert.Y;
		}

		public Point(byte[] data, int offset) {
			X = BitConverter.ToSingle(data, offset);
			Y = BitConverter.ToSingle(data, offset + 4);
		}

		public Point(TextureVertex tvertex) {
			X = tvertex.U;
			Y = tvertex.V;
		}

		public override string ToString() {
			return String.Format("X = {0}; Y = {1}", X, Y);
		}

		public static double CalculateAngle(Point u, Point v) {
			return Math.Acos(((u.X * v.X) + (u.Y * v.Y)) / (Math.Pow(u.X * u.X + u.Y * u.Y, 0.5) * Math.Pow(v.X * v.X + v.Y * v.Y, 0.5)));
		}

		public static double CalculateSignedAngle(Point a, Point b) {
			return a[0] * b[1] - a[1] * b[0];
		}

		public static double CalculateDistance(Point u, Point v) {
			return (u - v).Lenght;
		}

		public static Point operator -(Point u, Point v) {
			return new Point(u.X - v.X, u.Y - v.Y);
		}

		public static Point operator +(Point u, Point v) {
			return new Point(u.X + v.X, u.Y + v.Y);
		}

		public static Point operator *(Point u, float m) {
			return new Point(u.X * m, u.Y * m);
		}

		public static Point operator *(float m, Point u) {
			return new Point(u.X * m, u.Y * m);
		}

		public static Point operator /(Point u, float m) {
			return new Point(u.X / m, u.Y / m);
		}
	}
}
