using System;

namespace GRF.Graphics {
	public static class Helper {
		public static double ToDegree(double angle) {
			return angle * (180f / Math.PI);
		}

		public static float ToDegree(float angle) {
			return (float)(angle * (180f / Math.PI));
		}

		public static double ToRad(double angle) {
			return angle * (Math.PI / 180f);
		}

		public static float ToRad(float angle) {
			return (float)(angle * (Math.PI / 180f));
		}

		public static TkVector3 ToRad(TkVector3 v) {
			return v * (float)(Math.PI / 180f);
		}

		public static TkVector3 ToDegree(TkVector3 v) {
			return v * (float)(180f / Math.PI);
		}
	}
}
