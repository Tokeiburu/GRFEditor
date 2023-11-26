using System;

namespace Utilities {
	public static class TkRandom {
		private static Random _rnd = new Random();

		public static byte NextByte() {
			return (byte) (_rnd.Next() % 256);
		}

		public static int Next() {
			return _rnd.Next();
		}

		public static int NextInt() {
			return _rnd.Next();
		}

		public static double NextDouble() {
			return _rnd.NextDouble();
		}

		public static float NextFloat() {
			return (float) _rnd.NextDouble();
		}

		public static void SetSeed(int seed) {
			_rnd = new Random(seed);
		}

		public static float Rand(float x1, float x2) {
			return (float)(_rnd.NextDouble() * (x2 - x1)) + x1;
		}

		public static int Rand(int x1, int x2) {
			return (int)(_rnd.NextDouble() * (x2 - x1)) + x1;
		}
	}
}
