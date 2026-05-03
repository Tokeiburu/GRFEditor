using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace GRF.Image {
	public static class GrfColorEventHandlers {
		#region Delegates

		public delegate void ColorChangedEventHandler(object sender, in GrfColor color);

		#endregion
	}

	public static class GrfColors {
		public static GrfColor White = new GrfColor(255, 255, 255, 255);
		public static GrfColor Black = new GrfColor(255, 0, 0, 0);
		public static GrfColor Transparent = new GrfColor(0, 0, 0, 0);
		public static GrfColor Red = new GrfColor(255, 255, 0, 0);
		public static GrfColor Green = new GrfColor(255, 0, 255, 0);
		public static GrfColor Blue = new GrfColor(255, 0, 0, 255);
		public static GrfColor Pink = new GrfColor(255, 255, 0, 255);
	}

	/// <summary>
	/// Color object used by the GRF library. It has more options than the regular .NET color.
	/// </summary>
	[Serializable]
	public struct GrfColor {
		public static GrfColor White = new GrfColor(255, 255, 255, 255);
		public static GrfColor Black = new GrfColor(255, 0, 0, 0);

		[Serializable]
		private struct ColorByte {
			public byte A;
			public byte R;
			public byte G;
			public byte B;
		}

		private ColorByte _rgba;

		public GrfColor(float a, float r, float g, float b) {
			_rgba.A = Methods.ClampToColorByte(a * 255);
			_rgba.R = Methods.ClampToColorByte(r * 255);
			_rgba.G = Methods.ClampToColorByte(g * 255);
			_rgba.B = Methods.ClampToColorByte(b * 255);
		}

		public GrfColor(byte a, byte r, byte g, byte b) {
			_rgba.A = a;
			_rgba.R = r;
			_rgba.G = g;
			_rgba.B = b;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfColor" /> class.
		/// </summary>
		/// <param name="color">The color.</param>
		public GrfColor(string color) {
			color = color.Replace("0x", "").Replace("#", "");

			if (color.Length == 6) {
				_rgba.A = 255;
				_rgba.R = Convert.ToByte(color.Substring(0, 2), 16);
				_rgba.G = Convert.ToByte(color.Substring(2, 2), 16);
				_rgba.B = Convert.ToByte(color.Substring(4, 2), 16);
			}
			else if (color.Length == 8) {
				_rgba.A = Convert.ToByte(color.Substring(0, 2), 16);
				_rgba.R = Convert.ToByte(color.Substring(2, 2), 16);
				_rgba.G = Convert.ToByte(color.Substring(4, 2), 16);
				_rgba.B = Convert.ToByte(color.Substring(6, 2), 16);
			}
			else {
				throw new ArgumentException("Invalid format for the color. Expected a hex number: '0xAAFF00BB' or '#AAFF00BB'.", "color");
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfColor" /> class.
		/// </summary>
		/// <param name="colorRGB">The color RGB.</param>
		public GrfColor(int colorRGB) {
			_rgba.A = 255;
			_rgba.R = (byte)((colorRGB & 0xFF0000) >> 16);
			_rgba.G = (byte)((colorRGB & 0x00FF00) >> 8);
			_rgba.B = (byte)(colorRGB & 0x0000FF);
		}

		public static GrfColor FromByteArray(IList<byte> data, int offset, GrfImageType type) {
			GrfColor color = new GrfColor();

			switch (type) {
				case GrfImageType.Indexed8:
					color.R = data[offset + 0];
					color.G = data[offset + 1];
					color.B = data[offset + 2];
					color.A = data[offset + 3];
					return color;
				case GrfImageType.Bgra32:
					color.B = data[offset + 0];
					color.G = data[offset + 1];
					color.R = data[offset + 2];
					color.A = data[offset + 3];
					return color;
				case GrfImageType.Bgr32:
					color.B = data[offset + 0];
					color.G = data[offset + 1];
					color.R = data[offset + 2];
					color.A = 255;
					return color;
				case GrfImageType.Bgr24:
					color.B = data[offset + 0];
					color.G = data[offset + 1];
					color.R = data[offset + 2];
					color.A = 255;
					return color;
			}

			throw new ArgumentException("Invalid type parameter. Expected Indexed8, Bgra32, Bgr32 or Bgr24.", "type");
		}

		public static unsafe GrfColor FromByteArray(byte* data, int offset, GrfImageType type) {
			GrfColor color = default(GrfColor);

			switch (type) {
				case GrfImageType.Indexed8:
					color.R = data[offset + 0];
					color.G = data[offset + 1];
					color.B = data[offset + 2];
					color.A = data[offset + 3];
					return color;
				case GrfImageType.Bgra32:
					color.B = data[offset + 0];
					color.G = data[offset + 1];
					color.R = data[offset + 2];
					color.A = data[offset + 3];
					return color;
				case GrfImageType.Bgr32:
					color.B = data[offset + 0];
					color.G = data[offset + 1];
					color.R = data[offset + 2];
					color.A = 255;
					return color;
				case GrfImageType.Bgr24:
					color.B = data[offset + 0];
					color.G = data[offset + 1];
					color.R = data[offset + 2];
					color.A = 255;
					return color;
			}

			throw new ArgumentException("Invalid type parameter. Expected Indexed8, Bgra32, Bgr32 or Bgr24.", "type");
		}

		/// <summary>
		/// To the rgba bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToRgbaBytes() {
			byte[] data = new byte[4];

			data[0] = _rgba.R;
			data[1] = _rgba.G;
			data[2] = _rgba.B;
			data[3] = _rgba.A;

			return data;
		}

		/// <summary>
		/// To the RGB bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToRgbBytes() {
			byte[] data = new byte[3];

			data[0] = _rgba.R;
			data[1] = _rgba.G;
			data[2] = _rgba.B;

			return data;
		}

		/// <summary>
		/// To the BGR bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToBgrBytes() {
			byte[] data = new byte[3];

			data[0] = _rgba.B;
			data[1] = _rgba.G;
			data[2] = _rgba.R;

			return data;
		}

		/// <summary>
		/// To the bgra bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToBgraBytes() {
			byte[] data = new byte[4];

			data[0] = _rgba.B;
			data[1] = _rgba.G;
			data[2] = _rgba.R;
			data[3] = _rgba.A;

			return data;
		}

		/// <summary>
		/// To the ARGB bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ToArgbBytes() {
			byte[] data = new byte[4];

			data[0] = _rgba.A;
			data[1] = _rgba.R;
			data[2] = _rgba.G;
			data[3] = _rgba.B;

			return data;
		}

		/// <summary>
		/// Gets the <see cref="byte" /> at the specified index (ARGB).
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The byte channel color at the specified index.</returns>
		public byte this[int index] {
			get {
				switch (index) {
					case 0: return _rgba.A;
					case 1: return _rgba.R;
					case 2: return _rgba.G;
					case 3: return _rgba.B;
				}

				throw new IndexOutOfRangeException("index is out of range, must be between 0 and 4.");
			}
		}

		/// <summary>
		/// Red color channel.
		/// </summary>
		public byte R { get => _rgba.R; set => _rgba.R = value; }

		/// <summary>
		/// Green color channel.
		/// </summary>
		public byte G { get => _rgba.G; set => _rgba.G = value; }

		/// <summary>
		/// Blue color channel.
		/// </summary>
		public byte B { get => _rgba.B; set => _rgba.B = value; }

		/// <summary>
		/// Alpha color channel.
		/// </summary>
		public byte A { get => _rgba.A; set => _rgba.A = value; }

		/// <summary>
		/// Gets or sets the HSV component.
		/// </summary>
		public HsvColor Hsv => HsvColor.FromColor(this);

		/// <summary>
		/// Gets or sets the HSL component
		/// </summary>
		public HslColor Hsl => HslColor.FromColor(this);
		
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

		private bool Equals(in GrfColor other) {
			return R == other.R && G == other.G && B == other.B && A == other.A;
		}

		public override int GetHashCode() {
			return (int)ToArgbInt32();
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

		/// <summary>
		/// Converts from HSV to RGB
		/// </summary>
		/// <param name="hue">The hue [0-1].</param>
		/// <param name="saturation">The saturation [0-1].</param>
		/// <param name="value">The value [0-1].</param>
		/// <param name="alpha">The alpha [0-255].</param>
		/// <returns>The resulting color.</returns>
		public static GrfColor FromHsv(double hue, double saturation, double value, byte alpha = 255) {
			return _fromHsv(DoubleToHue(hue), Methods.Clamp(saturation, 0, 1), Methods.Clamp(value, 0, 1), alpha);
		}

		private static double DoubleToHue(double val) {
			if (val < 0) {
				val = -1 * val;
				return 1 - (val - (int)val);
			}

			return val - (int)val;
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
		public static GrfColor RandomMix(in GrfColor color1, in GrfColor color2, in GrfColor color3, float greyControl) {
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

		public static GrfColor RandomMixPaint(in GrfColor color1, in GrfColor color2, in GrfColor color3, float greyControl) {
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

		public static bool operator ==(in GrfColor color1, in GrfColor color2) {
			if (ReferenceEquals(color1, null) && ReferenceEquals(color2, null)) return true;
			if (ReferenceEquals(color1, null) || ReferenceEquals(color2, null)) return false;

			return color1.Equals(color2);
		}

		public static bool operator !=(in GrfColor color1, in GrfColor color2) {
			return !(color1 == color2);
		}

		public static implicit operator string(in GrfColor value) {
			return value.ToHexString();
		}

		public static GrfColor operator *(in GrfColor color1, float rate) {
			return new GrfColor((byte)(color1.A * rate), (byte)(color1.R * rate), (byte)(color1.G * rate), (byte)(color1.B * rate));
		}

		public static GrfColor operator +(in GrfColor color1, in GrfColor color2) {
			return new GrfColor(Methods.ClampToColorByte(color1.A + color2.A), Methods.ClampToColorByte(color1.R + color2.R), Methods.ClampToColorByte(color1.G + color2.G), Methods.ClampToColorByte(color1.B + color2.B));
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
			return _fromHsl(DoubleToHue(hue), Methods.Clamp(saturation, 0, 1), Methods.Clamp(lightness, 0, 1), alpha);
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
			writer.Write(R);
			writer.Write(G);
			writer.Write(B);
			writer.Write(A);
		}

		public static GrfColor Random() {
			return new GrfColor(TkRandom.Next());
		}

		public static string ToHex(byte r, byte g, byte b) {
			return String.Format("#FF{0:X2}{1:X2}{2:X2}", r, g, b);
		}

		public static string ToHex(byte a, byte r, byte g, byte b) {
			return String.Format("#{3:X2}{0:X2}{1:X2}{2:X2}", r, g, b, a);
		}

		public static double CalculateGradientSlope(in GrfColor first, in GrfColor middle, in GrfColor last, double slopeCount) {
			return
				(((first.R - middle.R) / (255f - (middle.R >= 255 ? 1 : middle.R)) +
				  (first.G - middle.G) / (255f - (middle.G >= 255 ? 1 : middle.G)) +
				  (first.B - middle.B) / (255f - (middle.B >= 255 ? 1 : middle.B))) +
				 ((last.R - middle.R) / (0f - (middle.R <= 0 ? 1 : middle.R)) +
				  (last.G - middle.G) / (0f - (middle.G <= 0 ? 1 : middle.G)) +
				  (last.B - middle.B) / (0f - (middle.B <= 0 ? 1 : middle.B)))) / slopeCount;
		}

		public static byte[] GenerateGradientRgba(in GrfColor colorFrom, in GrfColor colorTo, int count) {
			byte[] colorsData = new byte[count * 4];

			Buffer.BlockCopy(colorFrom.ToRgbaBytes(), 0, colorsData, 0, 4);
			Buffer.BlockCopy(colorTo.ToRgbaBytes(), 0, colorsData, (count - 1) * 4, 4);

			// 
			float countF = count - 1;

			for (int i = 1; i < count - 1; i++) {
				GrfColor middle = FromArgb(255,
				                           Methods.ClampToColorByte((int) (i * (colorTo.R - colorFrom.R) / countF + colorFrom.R)),
				                           Methods.ClampToColorByte((int) (i * (colorTo.G - colorFrom.G) / countF + colorFrom.G)),
										   Methods.ClampToColorByte((int) (i * (colorTo.B - colorFrom.B) / countF + colorFrom.B)));

				Buffer.BlockCopy(middle.ToRgbaBytes(), 0, colorsData, i * 4, 4);
			}

			return colorsData;
		}

		public static byte[] GenerateGradientRgba(in GrfColor colorMiddle, int count, double factor) {
			byte[] colorsData = new byte[count * 4];

			GrfColor colorFirst = FromArgb(
				255,
				Methods.ClampToColorByte((int)((255 - colorMiddle.R) * factor + colorMiddle.R)),
				Methods.ClampToColorByte((int)((255 - colorMiddle.G) * factor + colorMiddle.G)),
				Methods.ClampToColorByte((int)((255 - colorMiddle.B) * factor + colorMiddle.B))
			);

			GrfColor colorLast = FromArgb(
				255,
				Methods.ClampToColorByte((int)((0 - colorMiddle.R) * factor + colorMiddle.R)),
				Methods.ClampToColorByte((int)((0 - colorMiddle.G) * factor + colorMiddle.G)),
				Methods.ClampToColorByte((int)((0 - colorMiddle.B) * factor + colorMiddle.B))
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

		private static void _setColor(in GrfColor colorFirst, in GrfColor colorMiddle, int arrayIndex, float factor, byte[] colorsData) {
			byte[] color = new byte[4];
			color[0] = Methods.ClampToColorByte((int)(colorFirst.R + (colorMiddle.R - colorFirst.R) * factor));
			color[1] = Methods.ClampToColorByte((int)(colorFirst.G + (colorMiddle.G - colorFirst.G) * factor));
			color[2] = Methods.ClampToColorByte((int)(colorFirst.B + (colorMiddle.B - colorFirst.B) * factor));
			color[3] = 255;

			Buffer.BlockCopy(color, 0, colorsData, arrayIndex, 4);
		}

		public static GrfColor Lerp(GrfColor a, GrfColor b, float t) {
			return GrfColor.FromArgb(
				(byte)(a.A + (b.A - a.A) * t),
				(byte)(a.R + (b.R - a.R) * t),
				(byte)(a.G + (b.G - a.G) * t),
				(byte)(a.B + (b.B - a.B) * t)
			);
		}
	}

	#region Nested type: HslData

	[Serializable]
	public struct HslColor {
		public double Hue;
		public double Saturation;
		public double Lightness;

		public static HslColor FromColor(in GrfColor color) {
			double r = color.R / 255.0;
			double g = color.G / 255.0;
			double b = color.B / 255.0;

			HslColor data = new HslColor();

			double v = Math.Max(r, g);
			v = Math.Max(v, b);
			double m = Math.Min(r, g);
			m = Math.Min(m, b);
			data.Lightness = (m + v) / 2.0;
			if (data.Lightness <= 0.0) {
				return data;
			}
			double vm = v - m;
			data.Saturation = vm;
			if (data.Saturation > 0.0) {
				data.Saturation /= (data.Lightness <= 0.5) ? (v + m) : (2.0 - v - m);
			}
			else {
				return data;
			}
			double r2 = (v - r) / vm;
			double g2 = (v - g) / vm;
			double b2 = (v - b) / vm;
			if (r == v) {
				data.Hue = (g == m ? 5.0 + b2 : 1.0 - g2);
			}
			else if (g == v) {
				data.Hue = (b == m ? 1.0 + r2 : 3.0 - b2);
			}
			else {
				data.Hue = (r == m ? 3.0 + g2 : 5.0 - r2);
			}
			data.Hue /= 6.0;

			return data;
		}

		public GrfColor ToColor() {
			return GrfColor.FromHsl(Hue, Saturation, Lightness);
		}
	}

	#endregion

	#region Nested type: HsvData

	[Serializable]
	public struct HsvColor {
		public double Hue;
		public double Saturation;
		public double Value;
		public double Brightness { get => Value; set => Value = value; }

		public static HsvColor FromColor(in GrfColor color) {
			HsvColor hsv = new HsvColor();

			int max = Math.Max(color.R, Math.Max(color.G, color.B));
			int min = Math.Min(color.R, Math.Min(color.G, color.B));
			int diff = max - min;

			hsv.Value = (double)max / 255;

			if (diff == 0)
				hsv.Saturation = 0;
			else
				hsv.Saturation = (double)diff / max;

			double q;
			if (diff == 0) q = 0;
			else q = (double)60 / diff;

			if (max == color.R) {
				if (color.G < color.B) hsv.Hue = (360 + q * (color.G - color.B)) / 360;
				else hsv.Hue = (q * (color.G - color.B)) / 360;
			}
			else if (max == color.G) hsv.Hue = (120 + q * (color.B - color.R)) / 360;
			else if (max == color.B) hsv.Hue = (240 + q * (color.R - color.G)) / 360;
			else hsv.Hue = 0.0;
			return hsv;
		}

		public GrfColor ToColor() {
			return GrfColor.FromHsv(Hue, Saturation, Value);
		}
	}

	#endregion
}
