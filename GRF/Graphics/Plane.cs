using System;

namespace GRF.Graphics {
	public class Plane {
		public Vertex[] Points = new Vertex[4];

		public Plane() {
			for (int i = 0; i < 4; i++) {
				Points[i] = new Vertex();
			}
		}

		public Plane(int width, int height) : this() {
			Points[0].Y = height;
			Points[2].X = width;
			Points[3].X = width;
			Points[3].Y = height;

			Translate(-(width + 1) / 2, -(height + 1) / 2);
		}

		public void Translate(float x, float y) {
			for (int i = 0; i < Points.Length; i++) {
				Points[i].Translate(x, y, 0);
			}
		}

		public void ScaleX(float x) {
			for (int i = 0; i < Points.Length; i++) {
				Points[i].X *= x;
			}
		}

		public void ScaleY(float y) {
			for (int i = 0; i < Points.Length; i++) {
				Points[i].Y *= y;
			}
		}

		public void RotateZ(float angle) {
			double sin = Math.Sin(angle * Math.PI / 180f);
			double cos = Math.Cos(angle * Math.PI / 180f);
			float x;

			for (int i = 0; i < Points.Length; i++) {
				x = Points[i].X;
				Points[i].X = (float)(Points[i].X * cos + Points[i].Y * sin);
				Points[i].Y =(float)(- x * sin + Points[i].Y * cos);
			}
		}
	}
}
