using System;
using Utilities;

namespace GRF.Graphics {
	public class BoundingBox {
		public BoundingBox(byte[] data, ref int offset) {
			Max = new Vertex(data, offset);
			Min = new Vertex(data, offset += 12);
			Offset = new Vertex(data, offset += 12);
			Range = new Vertex(data, offset += 12);
			offset += 12;
		}

		public BoundingBox() {
			Max = new Vertex(float.MinValue, float.MinValue, float.MinValue);
			Min = new Vertex(float.MaxValue, float.MaxValue, float.MaxValue);
			Offset = new Vertex();
			Range = new Vertex();
			Center = new Vertex();
		}

		public BoundingBox(BoundingBox box) {
			Max = box.Max;
			Min = box.Min;
			Offset = box.Offset;
			Range = box.Range;
			Center = box.Center;
		}

		public void BaseCenter() {
			Max = Max - Center;
			Min = Min - Center;
			Offset = Offset - Center;
			Center = new Vertex();

			Max.Y += -Min.Y;
			Offset.Y += -Min.Y;
			Min.Y += -Min.Y;
		}

		public Vertex Max;
		public Vertex Min;
		public Vertex Offset;
		public Vertex Range;
		public Vertex Center;

		public void Multiply(Matrix4 matrix) {
			Max = Matrix4.Multiply(matrix, Max);
			Min = Matrix4.Multiply(matrix, Min);
			Center = Matrix4.Multiply(matrix, Center);
			Offset = Matrix4.Multiply(matrix, Offset);
			Range = Matrix4.Multiply(matrix, Range);
		}

		public void ReverseY() {
			Max.Y *= -1;
			Min.Y *= -1;
			Offset.Y *= -1;
			Center.Y *= -1;
		}

		public static BoundingBox operator +(BoundingBox boxA, BoundingBox boxB) {
			BoundingBox box = new BoundingBox();

			for (int i = 0; i < 3; i++) {
				box.Max[i] = Math.Max(boxA.Max[i], boxB.Max[i]);
				box.Min[i] = Math.Min(boxA.Min[i], boxB.Min[i]);
				box.Center[i] = (box.Max[i] - box.Min[i]) / 2f + box.Min[i];
				box.Range[i] = box.Max[i] - box.Center[i];
			}

			return box;
		}
	}
}
