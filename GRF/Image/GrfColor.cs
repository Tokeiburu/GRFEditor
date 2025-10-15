using System;
using System.Collections.Generic;
using System.IO;
using GRF.ContainerFormat;
using Utilities;

namespace GRF.Image {
	public static class GrfColorEventHandlers {
		#region Delegates

		public delegate void ColorChangedEventHandler(object sender, GrfColor color);

		#endregion
	}

	/// <summary>
	/// Color object used by the GRF library. It has more options than the regular .NET color.
	/// </summary>
	[Serializable]
	public class GrfColor {
		public static GrfColor White = new GrfColor(255, 255, 255, 255);
		public static GrfColor Black = new GrfColor(255, 0, 0, 0);

		/// <summary>
		/// The raw bytes of the color, ARGB
		/// </summary>
		private readonly byte[] _raw = new byte[4];
		private bool _hasBeenModified;
		private HslColor _hsl;
		private HsvColor _hsv;

		public GrfColor(float a, float r, float g, float b) {
			_raw[0] = (byte)(a * 255f);
			_raw[1] = (byte)(r * 255f);
			_raw[2] = (byte)(g * 255f);
			_raw[3] = (byte)(b * 255f);
		}

		public GrfColor(byte a, byte r, byte g, byte b) {
			_raw[0] = a;
			_raw[1] = r;
			_raw[2] = g;
			_raw[3] = b;
		}

		public GrfColor(IList<byte> data, int offset) {
			_raw[0] = data[offset + 3];
			_raw[1] = data[offset];
			_raw[2] = data[offset + 1];
			_raw[3] = data[offset + 2];
		}

		public GrfColor(IList<byte> data, int offset, GrfImageType type) {
			if (type == GrfImageType.Bgra32) {
				_raw[0] = data[offset + 3];
				_raw[1] = data[offset + 2];
				_raw[2] = data[offset + 1];
				_raw[3] = data[offset];
			}
			else if (type == GrfImageType.Bgr32) {
				_raw[0] = 255;
				_raw[1] = data[offset + 2];
				_raw[2] = data[offset + 1];
				_raw[3] = data[offset];
			}
			else if (type == GrfImageType.Bgr24) {
				_raw[0] = 255;
				_raw[1] = data[offset + 2];
				_raw[2] = data[offset + 1];
				_raw[3] = data[offset];
			}
			else if (type == GrfImageType.Indexed8) {
				_raw[0] = data[offset + 3];
				_raw[1] = data[offset];
				_raw[2] = data[offset + 1];
				_raw[3] = data[offset + 2];
			}
			else {
				throw GrfExceptions.__UnsupportedImageFormat.Create();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfColor" /> class.
		/// </summary>
		/// <param name="color">The color.</param>
		public GrfColor(GrfColor color) {
			Buffer.BlockCopy(color._raw, 0, _raw, 0, 4);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfColor" /> class.
		/// </summary>
		public GrfColor() {
			_raw[0] = 255;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfColor" /> class.
		/// </summary>
		/// <param name="color">The color.</param>
		public GrfColor(string color) {
			color = color.Replace("0x", "").Replace("#", "");

			if (color.Length == 6) {
				_raw[0] = 255;
				_raw[1] = Convert.ToByte(color.Substring(0, 2), 16);
				_raw[2] = Convert.ToByte(color.Substring(2, 2), 16);
				_raw[3] = Convert.ToByte(color.Substring(4, 2), 16);
			}
			else if (color.Length == 8) {
				_raw[0] = Convert.ToByte(color.Substring(0, 2), 16);
				_raw[1] = Convert.ToByte(color.Substring(2, 2), 16);
				_raw[2] = Convert.ToByte(color.Substring(4, 2), 16);
				_raw[3] = Convert.ToByte(color.Substring(6, 2), 16);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfColor" /> class.
		/// </summary>
		/// <param name="colorRGB">The color RGB.</param>
		public GrfColor(int colorRGB) {
			_raw[1] = (byte)((colorRGB & 0xFF0000) >> 16);
			_raw[2] = (byte)((colorRGB & 0x00FF00) >> 8);
			_raw[3] = (byte)(colorRGB & 0x0000FF);
			_raw[0] = 255;
		}

		internal bool HasBeenModified {
			set {
				if (value && _hasBeenModified == false) {
					_hsl = null;
					_hsv = null;
				}

				_hasBeenModified = value;
			}
		}

		/// <summary>
		/// To the rgba bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToRgbaBytes() {
			byte[] data = new byte[4];

			data[0] = _raw[1];
			data[1] = _raw[2];
			data[2] = _raw[3];
			data[3] = _raw[0];

			return data;
		}

		/// <summary>
		/// To the RGB bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToRgbBytes() {
			byte[] data = new byte[3];

			data[0] = _raw[1];
			data[1] = _raw[2];
			data[2] = _raw[3];

			return data;
		}

		/// <summary>
		/// To the BGR bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToBgrBytes() {
			byte[] data = new byte[3];

			data[0] = _raw[3];
			data[1] = _raw[2];
			data[2] = _raw[1];

			return data;
		}

		/// <summary>
		/// To the bgra bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToBgraBytes() {
			byte[] data = new byte[4];

			data[0] = _raw[3];
			data[1] = _raw[2];
			data[2] = _raw[1];
			data[3] = _raw[0];

			return data;
		}

		/// <summary>
		/// To the ARGB bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToArgbBytes() {
			byte[] data = new byte[4];

			data[0] = _raw[0];
			data[1] = _raw[1];
			data[2] = _raw[2];
			data[3] = _raw[3];

			return data;
		}

		/// <summary>
		/// Gets the <see cref="byte" /> at the specified index (ARGB).
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The byte channel color at the specified index.</returns>
		public byte this[int index] {
			get { return _raw[index]; }
		}

		/// <summary>
		/// Red color channel.
		/// </summary>
		public byte R {
			get { return _raw[1]; }
			set { HasBeenModified = true; _raw[1] = value; }
		}

		/// <summary>
		/// Green color channel.
		/// </summary>
		public byte G {
			get { return _raw[2]; }
			set { HasBeenModified = true; _raw[2] = value; }
		}

		/// <summary>
		/// Blue color channel.
		/// </summary>
		public byte B {
			get { return _raw[3]; }
			set { HasBeenModified = true; _raw[3] = value; }
		}

		/// <summary>
		/// Alpha color channel.
		/// </summary>
		public byte A {
			get { return _raw[0]; }
			set { HasBeenModified = true; _raw[0] = value; }
		}

		/// <summary>
		/// Gets or sets the HSV component.
		/// </summary>
		public HsvColor Hsv {
			get {
				if (_hsv == null) {
					_hsv = _getHsv();
					_hasBeenModified = false;
				}

				return _hsv;
			}
			set {
				GrfColor color = FromHsv(value.H, value.S, value.V);
				InternalCopy(color);
				HasBeenModified = true;
			}
		}

		/// <summary>
		/// Gets or sets the HSL component
		/// </summary>
		public HslColor Hsl {
			get {
				if (_hsl == null) {
					_hsl = _getHsl();
					_hasBeenModified = false;
				}

				return _getHsl();
			}
			set {
				GrfColor color = FromHsl(value.H, value.S, value.L);
				InternalCopy(color);
				HasBeenModified = true;
			}
		}

		/// <summary>
		/// Gets or sets the hue.
		/// </summary>
		public double Hue {
			get { return Hsv.H; }
			set { 
				GrfColor color = FromHsv(value, Saturation, Brightness);
				InternalCopy(color);
				HasBeenModified = true;
			}
		}

		/// <summary>
		/// Gets or sets the saturation.
		/// </summary>
		public double Saturation {
			get { return Hsv.S; }
			set {
				GrfColor color = FromHsv(Hue, value, Brightness);
				InternalCopy(color);
				HasBeenModified = true;
			}
		}

		/// <summary>
		/// Gets or sets the brightness (value).
		/// </summary>
		public double Brightness {
			get { return Hsv.V; }
			set {
				GrfColor color = FromHsv(Hue, Saturation, value);
				InternalCopy(color);
				HasBeenModified = true;
			}
		}

		/// <summary>
		/// Gets or sets the lightness.
		/// </summary>
		public double Lightness {
			get { return Hsl.L; }
			set {
				GrfColor color = FromHsl(Hue, Saturation, value);
				InternalCopy(color);
				HasBeenModified = true;
			}
		}

		internal void InternalCopy(GrfColor color) {
			_raw[0] = color._raw[0];
			_raw[1] = color._raw[1];
			_raw[2] = color._raw[2];
			_raw[3] = color._raw[3];
		}

		public static double ClampDouble(double val) {
			return val < 0 ? 0 : val > 1 ? 1 : val;
		}

		public static double DoubleToHue(double val) {
			if (val < 0) {
				val = -1 * val;
				return 1 - (val - (int) val);
			}

			return val - (int)val;
		}

		/// <summary>
		/// Clamps the int value to a byte value [0-255].
		/// </summary>
		/// <param name="val">The value.</param>
		/// <returns>The clamped value.</returns>
		public static byte ClampInt(int val) {
			return (byte) (val < 0 ? 0 : val > 255 ? 255 : val);
		}

		/// <summary>
		/// Clamps the int value to a byte value [0-255].
		/// </summary>
		/// <param name="val">The value.</param>
		/// <returns>The clamped value.</returns>
		public static byte ClampToByte(int val) {
			return (byte)(val < 0 ? 0 : val > 255 ? 255 : val);
		}

		/// <summary>
		/// To an int RGB.
		/// </summary>
		/// <returns></returns>
		public int ToRgbInt24() {
			return R << 16 | G << 8 | B;
		}

		/// <summary>
		/// To an uint ARGB.
		/// </summary>
		/// <returns></returns>
		public uint ToArgbInt32() {
			return (uint)(A << 24 | R << 16 | G << 8 | B);
		}

		protected bool Equals(GrfColor other) {
			return Methods.ByteArrayCompare(_raw, other._raw);
		}

		public override int GetHashCode() {
			return _raw[0] << 24 | _raw[1] << 16 | _raw[2] << 8 | _raw[3];
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((GrfColor)obj);
		}

		public override string ToString() {
			return string.Format("#{0:X}; A={1}, R={2}, G={3}, B={4}", (A << 24) + (R << 16) + (G << 8) + (B), A, R, G, B);
		}

		public string ToHexString() {
			return string.Format("#{0:X8}", (A << 24) + (R << 16) + (G << 8) + (B));
		}

		public static GrfColor FromArgb(byte a, byte r, byte g, byte b) {
			return new GrfColor(a, r, g, b);
		}

		public void SetColor(GrfColor color) {
			InternalCopy(color);
			HasBeenModified = true;
		}

		private HsvColor _getHsv() {
			HsvColor hsv = new HsvColor();

			int max = Math.Max(R, Math.Max(G, B));
			int min = Math.Min(R, Math.Min(G, B));
			int diff = max - min;

			hsv.V = (double) max / 255;

			if (diff == 0)
				hsv.S = 0;
			else
				hsv.S = (double) diff / max;

			double q;
			if (diff == 0) q = 0;
			else q = (double) 60 / diff;

			if (max == R) {
				if (G < B) hsv.H = (360 + q * (G - B)) / 360;
				else hsv.H = (q * (G - B)) / 360;
			}
			else if (max == G) hsv.H = (120 + q * (B - R)) / 360;
			else if (max == B) hsv.H = (240 + q * (R - G)) / 360;
			else hsv.H = 0.0;
			return hsv;
		}

		private HslColor _getHsl() {
			double r = R / 255.0;
			double g = G / 255.0;
			double b = B / 255.0;

			HslColor data = new HslColor();

			double v = Math.Max(r, g);
			v = Math.Max(v, b);
			double m = Math.Min(r, g);
			m = Math.Min(m, b);
			data.L = (m + v) / 2.0;
			if (data.L <= 0.0) {
				return data;
			}
			double vm = v - m;
			data.S = vm;
			if (data.S > 0.0) {
				data.S /= (data.L <= 0.5) ? (v + m) : (2.0 - v - m);
			}
			else {
				return data;
			}
			double r2 = (v - r) / vm;
			double g2 = (v - g) / vm;
			double b2 = (v - b) / vm;
			if (r == v) {
				data.H = (g == m ? 5.0 + b2 : 1.0 - g2);
			}
			else if (g == v) {
				data.H = (b == m ? 1.0 + r2 : 3.0 - b2);
			}
			else {
				data.H = (r == m ? 3.0 + g2 : 5.0 - r2);
			}
			data.H /= 6.0;

			return data;
		}

		/// <summary>
		/// Converts from HSV to RGB
		/// </summary>
		/// <param name="hue">The hue [0-1].</param>
		/// <param name="saturation">The saturation [0-1].</param>
		/// <param name="value">The value [0-1].</param>
		/// <param name="alpha">The alpha [0-255].</param>
		/// <returns>The resulting color.</returns>
		public static GrfColor FromHsv(double hue, double saturation, double value, byte alpha = 255) {
			return _fromHsv(DoubleToHue(hue), ClampDouble(saturation), ClampDouble(value), alpha);
		}

		private static GrfColor _fromHsv(double hue, double saturation, double value, byte alpha) {
			hue = hue - Math.Floor(hue);

			hue = hue * 360f;
			int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
			double f = hue / 60 - Math.Floor(hue / 60);

			value = value * 255;
			byte v = Convert.ToByte(value);
			byte p = Convert.ToByte(value * (1 - saturation));
			byte q = Convert.ToByte(value * (1 - f * saturation));
			byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

			switch (hi) {
				case 0:
					return FromArgb(alpha, v, t, p);
				case 1:
					return FromArgb(alpha, q, v, p);
				case 2:
					return FromArgb(alpha, p, v, t);
				case 3:
					return FromArgb(alpha, p, q, v);
				case 4:
					return FromArgb(alpha, t, p, v);
				default:
					return FromArgb(alpha, v, p, q);
			}
		}

		/// <summary>
		/// Random mixing algorithm.
		/// </summary>
		/// <param name="color1">The color1.</param>
		/// <param name="color2">The color2.</param>
		/// <param name="color3">The color3.</param>
		/// <param name="greyControl">The grey control [0-1].</param>
		/// <returns></returns>
		public static GrfColor RandomMix(GrfColor color1, GrfColor color2, GrfColor color3, float greyControl) {
			int randomIndex = TkRandom.NextByte() % 3;

			float mixRatio1 = (randomIndex == 0) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();
			float mixRatio2 = (randomIndex == 1) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();
			float mixRatio3 = (randomIndex == 2) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();

			float sum = mixRatio1 + mixRatio2 + mixRatio3;

			mixRatio1 /= sum;
			mixRatio2 /= sum;
			mixRatio3 /= sum;

			return FromArgb(
			   255,
			   (byte)(mixRatio1 * color1.R + mixRatio2 * color2.R + mixRatio3 * color3.R),
			   (byte)(mixRatio1 * color1.G + mixRatio2 * color2.G + mixRatio3 * color3.G),
			   (byte)(mixRatio1 * color1.B + mixRatio2 * color2.B + mixRatio3 * color3.B));
		}

		public static GrfColor RandomMixHSL(GrfColor color1, GrfColor color2, GrfColor color3, float greyControl) {
			int randomIndex = TkRandom.NextByte() % 3;

			float mixRatio1 = (randomIndex == 0) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();
			float mixRatio2 = (randomIndex == 1) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();
			float mixRatio3 = (randomIndex == 2) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();

			float sum = mixRatio1 + mixRatio2 + mixRatio3;

			mixRatio1 /= sum;
			mixRatio2 /= sum;
			mixRatio3 /= sum;

			return FromHsl(
				(mixRatio1 * color1.Hue + mixRatio2 * color2.Hue + mixRatio3 * color3.Hue),
				(mixRatio1 * color1.Saturation + mixRatio2 * color2.Saturation + mixRatio3 * color3.Saturation),
				(mixRatio1 * color1.Lightness + mixRatio2 * color2.Lightness + mixRatio3 * color3.Lightness));
		}

		public static GrfColor RandomMixPaint(GrfColor color1, GrfColor color2, GrfColor color3, float greyControl) {
			int randomIndex = TkRandom.NextByte() % 3;

			float mixRatio1 = (randomIndex == 0) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();
			float mixRatio2 = (randomIndex == 1) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();
			float mixRatio3 = (randomIndex == 2) ? TkRandom.NextFloat() * greyControl : TkRandom.NextFloat();

			float sum = mixRatio1 + mixRatio2 + mixRatio3;

			mixRatio1 /= sum;
			mixRatio2 /= sum;
			mixRatio3 /= sum;

			return FromArgb(
				255,
				(byte) (255 - (byte)(mixRatio1 * (255 - color1.R) + mixRatio2 * (255 - color2.R) + mixRatio3 * (255 - color3.R))),
				(byte) (255 - (byte)(mixRatio1 * (255 - color1.G) + mixRatio2 * (255 - color2.G) + mixRatio3 * (255 - color3.G))),
				(byte) (255 - (byte)(mixRatio1 * (255 - color1.B) + mixRatio2 * (255 - color2.B) + mixRatio3 * (255 - color3.B))));
		}

		/// <summary>
		/// Generates the color harmonies.
		/// </summary>
		/// <param name="colorCount">The color count.</param>
		/// <param name="offsetAngle1">The offset angle1 [0~360].</param>
		/// <param name="offsetAngle2">The offset angle2 [0~360].</param>
		/// <param name="rangeAngle0">The range angle0 [0~360].</param>
		/// <param name="rangeAngle1">The range angle1 [0~360].</param>
		/// <param name="rangeAngle2">The range angle2 [0~360].</param>
		/// <param name="saturation">The saturation [0~1].</param>
		/// <param name="luminance">The luminance [0~1].</param>
		/// <returns></returns>
		public static List<GrfColor> GenerateColorHarmonies(int colorCount, float offsetAngle1, float offsetAngle2, float rangeAngle0, float rangeAngle1, float rangeAngle2, float saturation, float luminance) {
			var colors = new List<GrfColor>();
			float referenceAngle = TkRandom.NextFloat() * 360;

			for (int i = 0; i < colorCount; i++) {
				float randomAngle = TkRandom.NextFloat() * (rangeAngle0 + rangeAngle1 + rangeAngle2);

				if (randomAngle > rangeAngle0) {
					if (randomAngle < rangeAngle0 + rangeAngle1) {
						randomAngle += offsetAngle1;
					}
					else {
						randomAngle += offsetAngle2;
					}
				}

				colors.Add(FromHsl(((referenceAngle + randomAngle) / 360.0f) % 1.0f, saturation, luminance));
			}

			return colors;
		}

		public static bool operator ==(GrfColor color1, GrfColor color2) {
			if (ReferenceEquals(color1, null) && ReferenceEquals(color2, null)) return true;
			if (ReferenceEquals(color1, null) || ReferenceEquals(color2, null)) return false;

			return color1.Equals(color2);
		}

		public static bool operator !=(GrfColor color1, GrfColor color2) {
			return !(color1 == color2);
		}

		public static implicit operator GrfColor(string value) {
			return new GrfColor(value);
		}

		public static implicit operator string(GrfColor value) {
			return value.ToHexString();
		}

		/// <summary>
		/// Converts from HSL to RGB
		/// </summary>
		/// <param name="hue">The hue [0-1].</param>
		/// <param name="saturation">The saturation [0-1].</param>
		/// <param name="lightness">The value [0-1].</param>
		/// <param name="alpha">The alpha [0-255].</param>
		/// <returns>The resulting color.</returns>
		public static GrfColor FromHsl(double hue, double saturation, double lightness, byte alpha = 255) {
			return _fromHsl(DoubleToHue(hue), ClampDouble(saturation), ClampDouble(lightness), alpha);
		}

		private static GrfColor _fromHsl(double hue, double saturation, double lightness, byte alpha) {
			double r = 0, g = 0, b = 0;
			if (lightness != 0) {
				if (saturation == 0)
					r = g = b = lightness;
				else {
					double temp2 = _internalHslTemp(lightness, saturation);
					double temp1 = 2.0 * lightness - temp2;

					r = _getColorComponent(temp1, temp2, hue + 1.0 / 3.0);
					g = _getColorComponent(temp1, temp2, hue);
					b = _getColorComponent(temp1, temp2, hue - 1.0 / 3.0);
				}
			}

			return new GrfColor(alpha, (byte) (r * 255), (byte) (g * 255), (byte) (b * 255));
		}

		private static double _getColorComponent(double temp1, double temp2, double temp3) {
			temp3 = _moveIntoRange(temp3);
			if (temp3 < 1.0 / 6.0)
				return temp1 + (temp2 - temp1) * 6.0 * temp3;
			if (temp3 < 0.5)
				return temp2;
			if (temp3 < 2.0 / 3.0)
				return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
			return temp1;
		}
		private static double _moveIntoRange(double temp3) {
			if (temp3 < 0.0)
				temp3 += 1.0;
			else if (temp3 > 1.0)
				temp3 -= 1.0;
			return temp3;
		}
		private static double _internalHslTemp(double lightness, double saturation) {
			double temp2;
			if (lightness < 0.5)  //<=??
				temp2 = lightness * (1.0 + saturation);
			else
				temp2 = lightness + saturation - (lightness * saturation);
			return temp2;
		}

		public void Write(BinaryWriter writer) {
			writer.Write(_raw, 1, 3);
			writer.Write(_raw[0]);
		}

		#region Nested type: HslData

		[Serializable]
		public class HslColor {
			public double H { get; internal set; }
			public double S { get; internal set; }
			public double L { get; internal set; }
		}

		#endregion

		#region Nested type: HsvData

		[Serializable]
		public class HsvColor {
			public double H { get; internal set; }
			public double S { get; internal set; }
			public double V { get; internal set; }
		}

		#endregion

		public static GrfColor Random() {
			return new GrfColor(TkRandom.Next());
		}

		public static string ToHex(byte r, byte g, byte b) {
			return String.Format("#FF{0:X2}{1:X2}{2:X2}", r, g, b);
		}

		public static string ToHex(byte a, byte r, byte g, byte b) {
			return String.Format("#{3:X2}{0:X2}{1:X2}{2:X2}", r, g, b, a);
		}

		public static double CalculateGradientSlope(GrfColor first, GrfColor middle, GrfColor last, double slopeCount) {
			return
				(((first.R - middle.R) / (255f - (middle.R >= 255 ? 1 : middle.R)) +
				  (first.G - middle.G) / (255f - (middle.G >= 255 ? 1 : middle.G)) +
				  (first.B - middle.B) / (255f - (middle.B >= 255 ? 1 : middle.B))) +
				 ((last.R - middle.R) / (0f - (middle.R <= 0 ? 1 : middle.R)) +
				  (last.G - middle.G) / (0f - (middle.G <= 0 ? 1 : middle.G)) +
				  (last.B - middle.B) / (0f - (middle.B <= 0 ? 1 : middle.B)))) / slopeCount;
		}

		public static byte[] GenerateGradientRgba(GrfColor colorFrom, GrfColor colorTo, int count) {
			byte[] colorsData = new byte[count * 4];

			Buffer.BlockCopy(colorFrom.ToRgbaBytes(), 0, colorsData, 0, 4);
			Buffer.BlockCopy(colorTo.ToRgbaBytes(), 0, colorsData, (count - 1) * 4, 4);

			// 
			float countF = count - 1;

			for (int i = 1; i < count - 1; i++) {
				GrfColor middle = FromArgb(255,
				                           ClampInt((int) (i * (colorTo.R - colorFrom.R) / countF + colorFrom.R)),
				                           ClampInt((int) (i * (colorTo.G - colorFrom.G) / countF + colorFrom.G)),
				                           ClampInt((int) (i * (colorTo.B - colorFrom.B) / countF + colorFrom.B)));

				Buffer.BlockCopy(middle.ToRgbaBytes(), 0, colorsData, i * 4, 4);
			}

			return colorsData;
		}

		public static byte[] GenerateGradientRgba(GrfColor colorMiddle, int count, double factor) {
			byte[] colorsData = new byte[count * 4];

			GrfColor colorFirst = FromArgb(
				255,
				ClampInt((int)((255 - colorMiddle.R) * factor + colorMiddle.R)),
				ClampInt((int)((255 - colorMiddle.G) * factor + colorMiddle.G)),
				ClampInt((int)((255 - colorMiddle.B) * factor + colorMiddle.B))
			);

			GrfColor colorLast = FromArgb(
				255,
				ClampInt((int)((0 - colorMiddle.R) * factor + colorMiddle.R)),
				ClampInt((int)((0 - colorMiddle.G) * factor + colorMiddle.G)),
				ClampInt((int)((0 - colorMiddle.B) * factor + colorMiddle.B))
			);

			Buffer.BlockCopy(colorFirst.ToRgbaBytes(), 0, colorsData, 0, 4);
			Buffer.BlockCopy(colorLast.ToRgbaBytes(), 0, colorsData, (count - 1) * 4, 4);

			var middle = count / 2f;
			var shifted = count % 2 == 0;

			for (int i = 0; i < count; i++) {
				if (i < middle) {
					if (shifted) {
						_setColor(colorFirst, colorMiddle, i * 4, (2 * i) / (count - 1f), colorsData);
					}
					else {
						_setColor(colorFirst, colorMiddle, i * 4, i / ((count - 1) / 2f), colorsData);
					}
				}
				//else if (i == middle) {
				//    Buffer.BlockCopy(colorMiddle.ToBytesRgba(), 0, colorsData, i * 4, 4);
				//}
				else {
					if (shifted) {
						_setColor(colorMiddle, colorLast, i * 4, (2 * (i - middle) + 1) / (count - 1f), colorsData);
					}
					else {
						_setColor(colorMiddle, colorLast, i * 4, (i - ((count - 1) / 2f)) / ((count - 1) / 2f), colorsData);
					}
				}
			}

			//_setColor(colorFirst, colorMiddle, 0 * 4, 0, colorsData);
			//_setColor(colorFirst, colorMiddle, 1 * 4, 2 / 7f, colorsData);
			//_setColor(colorFirst, colorMiddle, 2 * 4, 4 / 7f, colorsData);
			//_setColor(colorFirst, colorMiddle, 3 * 4, 6 / 7f, colorsData);
			//_setColor(colorMiddle, colorLast, 4 * 4, 1 / 7f, colorsData);
			//_setColor(colorMiddle, colorLast, 5 * 4, 3 / 7f, colorsData);
			//_setColor(colorMiddle, colorLast, 6 * 4, 5 / 7f, colorsData);
			//_setColor(colorMiddle, colorLast, 7 * 4, 1, colorsData);

			return colorsData;
		}

		private static void _setColor(GrfColor colorFirst, GrfColor colorMiddle, int arrayIndex, float factor, byte[] colorsData) {
			byte[] color = new byte[4];
			color[0] = ClampInt((int)(colorFirst.R + (colorMiddle.R - colorFirst.R) * factor));
			color[1] = ClampInt((int)(colorFirst.G + (colorMiddle.G - colorFirst.G) * factor));
			color[2] = ClampInt((int)(colorFirst.B + (colorMiddle.B - colorFirst.B) * factor));
			color[3] = 255;

			Buffer.BlockCopy(color, 0, colorsData, arrayIndex, 4);
		}
	}
}
