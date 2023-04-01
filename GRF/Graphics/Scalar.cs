using System;

namespace GRF.Graphics {
	public static class Scalar {
		public static double Rescale(double value, double fromInput, double toInput, double fromDest, double toDest) {
			var position = value / (toInput - fromInput) + fromInput;
			return (toDest - fromDest) * position + fromDest;
		}

		public static double SubsetRescale(double value, int elements) {
			int elementIndex = (int) (value * elements);
			var groupLength = 1d / elements;
			var groupFromValue = elementIndex * groupLength;
			return (value - groupFromValue) / groupLength;
		}

		private static readonly Random _rnd = new Random();

		public static double RandomDouble() {
			return _rnd.NextDouble();
		}

		public static byte RandomByte() {
			return (byte) (_rnd.Next() % 256);
		}

		public static int RandomInt() {
			return _rnd.Next();
		}
	}
}
