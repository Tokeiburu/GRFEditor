using System;

namespace GRF.Graphics {
	public class BoundingBox {
		public TkVector3 PCenter {
			get { return (Min + Max) / 2.0f; }
		}

		public TkVector3 PRange {
			get { return (Max - Min) / 2.0f; }
		}

		public BoundingBox(byte[] data, ref int offset) {
			Max = new TkVector3(data, offset);
			Min = new TkVector3(data, offset += 12);
			Offset = new TkVector3(data, offset += 12);
			Range = new TkVector3(data, offset += 12);
			offset += 12;
		}

		public BoundingBox() {
			Max = new TkVector3(float.MinValue, float.MinValue, float.MinValue);
			Min = new TkVector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Offset = new TkVector3();
			Range = new TkVector3();
			Center = new TkVector3();
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
			Center = new TkVector3();

			Max.Y += -Min.Y;
			Offset.Y += -Min.Y;
			Min.Y += -Min.Y;
		}

		public void AddVertex(TkVector3 v) {
			for (int c = 0; c < 3; c++) {
				Min[c] = Math.Min(Min[c], v[c]);
				Max[c] = Math.Max(Max[c], v[c]);
			}
		}

		public TkVector3 Max;
		public TkVector3 Min;
		public TkVector3 Offset;
		public TkVector3 Range;
		public TkVector3 Center;

		public void Multiply(TkMatrix4 matrix) {
			Max = new TkVector3(matrix * new TkVector4(Max, 1));
			Min = new TkVector3(matrix * new TkVector4(Min, 1));
			Center = new TkVector3(matrix * new TkVector4(Center, 1));
			Offset = new TkVector3(matrix * new TkVector4(Offset, 1));
			Range = new TkVector3(matrix * new TkVector4(Range, 1));
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
