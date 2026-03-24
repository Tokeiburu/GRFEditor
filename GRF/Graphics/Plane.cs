using GRF.FileFormats.ActFormat;
using System;

namespace GRF.Graphics {
	public class Plane {
		public TkVector2[] Points = new TkVector2[4];

		public TkVector2 Center {
			get {
				TkVector2 center = new TkVector2();

				foreach (var point in Points) {
					center += point;
				}

				return center / 4;
			}
		}

		public Plane() {
			for (int i = 0; i < 4; i++) {
				Points[i] = new TkVector2();
			}
		}

		public Plane(int width, int height) : this(width, height, true) {
		}

		public Plane(int width, int height, bool translateToCenter) : this() {
			Points[0].Y = height;
			Points[2].X = width;
			Points[3].X = width;
			Points[3].Y = height;

			if (translateToCenter)
				Translate(-(width + 1) / 2, -(height + 1) / 2);
		}

		public void Translate(float x, float y) {
			for (int i = 0; i < Points.Length; i++) {
				Points[i].X += x;
				Points[i].Y += y;
			}
		}

		public void Translate(double x, double y) {
			Translate((float)x, (float)y);
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

		public void RotateZ(float angle, float centerX, float centerY) {
			Translate(-centerX, -centerY);
			RotateZ(angle);
			Translate(centerX, centerY);
		}

		public void Margin(float left, float top, float right, float bottom) {
			Points[0].X -= left;
			Points[1].X -= left;

			Points[3].X += right;
			Points[2].X += right;

			Points[0].Y -= top;
			Points[3].Y -= top;

			Points[1].Y += bottom;
			Points[2].Y += bottom;
		}

		public void Crop(float left, float top, float right, float bottom) {
			Margin(-left, -top, -right, -bottom);
		}

		public void Scale(int sx, int sy, float centerX, float centerY) {
			Translate(-centerX, -centerY);
			ScaleX(sx);
			ScaleY(sy);
			Translate(centerX, centerY);
		}

		public static Plane FromLayer(Act act, Layer layer) {
			if (layer.SpriteIndex < 0) return null;
			var image = layer.GetImage(act.Sprite);

			Plane plane = new Plane(image.Width, image.Height);

			//if (layer.Mirror)
			//	plane.Scale(-1, 1, plane.Center.X, plane.Center.Y);

			plane.ScaleX(layer.ScaleX);
			plane.ScaleY(layer.ScaleY);
			plane.RotateZ(-layer.Rotation, image.Width % 2 == 1 ? -0.5f * layer.ScaleX : 0.0f, image.Height % 2 == 1 ? -0.5f * layer.ScaleY: 0.0f);
			plane.Translate(layer.OffsetX, layer.OffsetY);
			return plane;
		}
	}
}
