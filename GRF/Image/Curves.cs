using GRF.Graphics;
using System;

namespace GRF.Image {
	public static class Curves {
		public static PowerCurve Pow2 = new PowerCurve(2);
		public static PowerCurve Pow3 = new PowerCurve(3);
		public static PowerCurve Pow4 = new PowerCurve(4);
		public static PowerCurve Pow5 = new PowerCurve(5);
		public static PowerCurve Sqrt = new PowerCurve(0.5d);
		public static LinearCurve Linear = new LinearCurve();
		public static BezierCurve Bell = new BezierCurve(new TkVector2(0.5, 0), new TkVector2(0.5, 1));
	}

	public abstract class Curve {
		public abstract double GetPoint(double t);
	}

	public class LinearCurve : Curve {
		public override double GetPoint(double t) {
			return t;
		}
	}

	public class PowerCurve : Curve {
		private double _exp;

		public PowerCurve(double exp) {
			_exp = exp;
		}

		public override double GetPoint(double t) {
			return Math.Pow(t, _exp);
		}
	}

	public class BezierCurve : Curve {
		public TkVector2 P1;
		public TkVector2 P2;

		public BezierCurve(in TkVector2 point1, in TkVector2 point2) {
			P1 = point1;
			P2 = point2;
		}

		public override double GetPoint(double t) {
			double invT = 1 - t;
			return 3 * Math.Pow(invT, 2) * t * P1.Y + 3 * invT * Math.Pow(t, 2) * P2.Y + Math.Pow(t, 3);
		}
	}
}
