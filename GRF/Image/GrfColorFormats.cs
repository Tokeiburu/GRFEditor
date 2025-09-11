using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GRF.Image {
	public struct _GrfColorLab {
		public double L;
		public double A;
		public double B;

		public static _GrfColorLab From(byte cr, byte cg, byte cb) {
			_GrfColorLab ret = new _GrfColorLab();

			// RGB (0-255) -> normalized [0,1]
			double r = cr / 255.0;
			double g = cg / 255.0;
			double b = cb / 255.0;

			// sRGB gamma correction
			r = r <= 0.04045 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
			g = g <= 0.04045 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
			b = b <= 0.04045 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

			// RGB -> XYZ
			double x = r * 0.4124 + g * 0.3576 + b * 0.1805;
			double y = r * 0.2126 + g * 0.7152 + b * 0.0722;
			double z = r * 0.0193 + g * 0.1192 + b * 0.9505;

			// XYZ -> Lab (reference white D65)
			double xr = x / 0.95047;
			double yr = y / 1.00000;
			double zr = z / 1.08883;

			Func<double, double> f = t => t > 0.008856 ? Math.Pow(t, 1.0 / 3) : 7.787 * t + 16 / 116.0;

			ret.L = 116 * f(yr) - 16;
			ret.A = 500 * (f(xr) - f(yr));
			ret.B = 200 * (f(yr) - f(zr));
			return ret;
		}

		public static _GrfColorLab From(_GrfColorXyz c) {
			_GrfColorLab ret = new _GrfColorLab();

			// XYZ -> Lab (reference white D65)
			double xr = c.X / 0.95047;
			double yr = c.Y / 1.00000;
			double zr = c.Z / 1.08883;

			Func<double, double> f = t => t > 0.008856 ? Math.Pow(t, 1.0 / 3) : 7.787 * t + 16 / 116.0;

			ret.L = 116 * f(yr) - 16;
			ret.A = 500 * (f(xr) - f(yr));
			ret.B = 200 * (f(yr) - f(zr));
			return ret;
		}

		public void Add(_GrfColorLab right) {
			L += right.L;
			A += right.A;
			B += right.B;
		}
	}

	public struct _GrfColorXyz {
		public double X;
		public double Y;
		public double Z;

		public _GrfColorXyz(double x, double y, double z) {
			X = x;
			Y = y;
			Z = z;
		}

		public void Add(_GrfColorXyz right) {
			X += right.X;
			Y += right.Y;
			Z += right.Z;
		}

		public static _GrfColorXyz From(long cr, long cg, long cb) {
			return From((byte)cr, (byte)cg, (byte)cb);
		}

		public static _GrfColorXyz From(int cr, int cg, int cb) {
			return From((byte)cr, (byte)cg, (byte)cb);
		}

		public static _GrfColorXyz From(byte cr, byte cg, byte cb) {
			_GrfColorXyz ret = new _GrfColorXyz();

			// RGB (0-255) -> normalized [0,1]
			double r = cr / 255.0;
			double g = cg / 255.0;
			double b = cb / 255.0;

			// sRGB gamma correction
			r = r <= 0.04045 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
			g = g <= 0.04045 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
			b = b <= 0.04045 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

			// RGB -> XYZ
			ret.X = r * 0.4124 + g * 0.3576 + b * 0.1805;
			ret.Y = r * 0.2126 + g * 0.7152 + b * 0.0722;
			ret.Z = r * 0.0193 + g * 0.1192 + b * 0.9505;
			return ret;
		}
	}

	public struct _GrfColorYCbCr {
		public int Y;
		public int Cb;
		public int Cr;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct _GrfColorBgra {
		public byte B;
		public byte G;
		public byte R;
		public byte A;

		public _GrfColorBgra(byte b, byte g, byte r, byte a) {
			B = b;
			G = g;
			R = r;
			A = a;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct _GrfColorRgba {
		public byte R;
		public byte G;
		public byte B;
		public byte A;
	}

	public struct _GrfColorRgb {
		public byte R;
		public byte G;
		public byte B;

		public static _GrfColorRgb From(_GrfColorLab lab) {
			_GrfColorRgb ret = new _GrfColorRgb();

			// Reference white D65
			double refX = 0.95047;
			double refY = 1.00000;
			double refZ = 1.08883;

			// Convert Lab to XYZ
			double y = (lab.L + 16.0) / 116.0;
			double x = lab.A / 500.0 + y;
			double z = y - lab.B / 200.0;

			Func<double, double> fInv = t => Math.Pow(t, 3) > 0.008856 ? Math.Pow(t, 3) : (t - 16.0 / 116.0) / 7.787;

			x = refX * fInv(x);
			y = refY * fInv(y);
			z = refZ * fInv(z);

			// Convert XYZ to linear RGB
			double r = 3.2406 * x - 1.5372 * y - 0.4986 * z;
			double g = -0.9689 * x + 1.8758 * y + 0.0415 * z;
			double bl = 0.0557 * x - 0.2040 * y + 1.0570 * z;

			// Gamma correction (sRGB)
			r = r <= 0.0031308 ? 12.92 * r : 1.055 * Math.Pow(r, 1.0 / 2.4) - 0.055;
			g = g <= 0.0031308 ? 12.92 * g : 1.055 * Math.Pow(g, 1.0 / 2.4) - 0.055;
			bl = bl <= 0.0031308 ? 12.92 * bl : 1.055 * Math.Pow(bl, 1.0 / 2.4) - 0.055;

			// Clamp and convert to byte
			ret.R = (byte)(Math.Min(Math.Max(r, 0.0), 1.0) * 255);
			ret.G = (byte)(Math.Min(Math.Max(g, 0.0), 1.0) * 255);
			ret.B = (byte)(Math.Min(Math.Max(bl, 0.0), 1.0) * 255);
			return ret;
		}

		public static _GrfColorRgb From(_GrfColorXyz xyz) {
			_GrfColorRgb ret = new _GrfColorRgb();

			double x = xyz.X;
			double y = xyz.Y;
			double z = xyz.Z;

			// Convert XYZ to linear RGB
			double r = 3.2406 * x - 1.5372 * y - 0.4986 * z;
			double g = -0.9689 * x + 1.8758 * y + 0.0415 * z;
			double b = 0.0557 * x - 0.2040 * y + 1.0570 * z;

			// Apply gamma correction (linear → sRGB)
			r = r <= 0.0031308 ? 12.92 * r : 1.055 * Math.Pow(r, 1.0 / 2.4) - 0.055;
			g = g <= 0.0031308 ? 12.92 * g : 1.055 * Math.Pow(g, 1.0 / 2.4) - 0.055;
			b = b <= 0.0031308 ? 12.92 * b : 1.055 * Math.Pow(b, 1.0 / 2.4) - 0.055;

			// Clamp 0..1 and scale to byte
			ret.R = (byte)(Math.Min(Math.Max(r, 0.0), 1.0) * 255.0 + 0.5);
			ret.G = (byte)(Math.Min(Math.Max(g, 0.0), 1.0) * 255.0 + 0.5);
			ret.B = (byte)(Math.Min(Math.Max(b, 0.0), 1.0) * 255.0 + 0.5);
			return ret;
		}
	}

	public struct _GrfColorRgbLong {
		public long R;
		public long G;
		public long B;

		public void Add(_GrfColorRgbLong right) {
			R += right.R;
			G += right.G;
			B += right.B;
		}
	}

	public enum GrfColorMode {
		Rgb,
		YCbCr,
		Xyz,
		Lab,
		RgbToXyz,
		RgbToLab,
	}
}
