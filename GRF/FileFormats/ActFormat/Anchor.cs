using System;
using Utilities;

namespace GRF.FileFormats.ActFormat {
	[Serializable]
	public class Anchor {
		public Anchor(byte[] unknown, int offsetX, int offsetY, int other) {
			//Console.WriteLine("Unknown : " + BitConverter.ToString(unknown));
			Unknown = unknown;
			OffsetX = offsetX;
			OffsetY = offsetY;
			Other = other;
		}

		public Anchor(Anchor pass) {
			Unknown = pass.Unknown;
			OffsetX = pass.OffsetX;
			OffsetY = pass.OffsetY;
			Other = pass.Other;
		}

		public Anchor() {
			Unknown = new byte[] { 0, 0, 0, 0 };
			OffsetX = 0;
			OffsetY = 0;
			Other = 0;
		}

		public byte[] Unknown { get; set; }
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
		public int Other { get; set; }

		public override int GetHashCode() {
			unchecked {
				int hashCode = (Unknown != null ? Unknown.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ OffsetX;
				hashCode = (hashCode * 397) ^ OffsetY;
				hashCode = (hashCode * 397) ^ Other;
				return hashCode;
			}
		}

		public void Magnify(float value) {
			OffsetX = (int) (value * OffsetX);
			OffsetY = (int) (value * OffsetY);
		}

		public void TranslateX(int x) {
			Translate(x, 0);
		}

		public void TranslateY(int y) {
			Translate(0, y);
		}

		public void Translate(int x, int y) {
			OffsetX += x;
			OffsetY += y;
		}

		public override string ToString() {
			return "Offsets (" + OffsetX + ", " + OffsetY + ")";
		}

		public override bool Equals(object obj) {
			var anchor = obj as Anchor;
			if (anchor != null) {
				if (Methods.ByteArrayCompare(Unknown, anchor.Unknown) && Other == anchor.Other &&
				    OffsetX == anchor.OffsetX && OffsetY == anchor.OffsetY) {
					return true;
				}
			}

			return false;
		}
	}
}