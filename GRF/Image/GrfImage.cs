using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GRF.ContainerFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.TgaFormat;
using GRF.Graphics;
using GRF.Image.Decoders;
using Utilities;
using Utilities.Extension;

namespace GRF.Image {
	/// <summary>
	/// Image used by the GRF library
	/// </summary>
	public class GrfImage {
		public static byte[] PngHeader = new byte[] {0x89, 0x50, 0x4e, 0x47};
		public static byte[] BmpHeader = new byte[] {0x42, 0x4d};
		public static byte[] JpgHeader = new byte[] {0xff, 0xd8};
		private static readonly Dictionary<GrfImage, List<int>> _bufferedSimilarities = new Dictionary<GrfImage, List<int>>();
		private bool _isClosed;

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public GrfImage(MultiType data) {
			byte[] fileData = data.UniqueData;
			Width = -1;
			Height = -1;

			Pixels = fileData;
			GrfImageType = _getType();

			SelfAny();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="type">The type.</param>
		/// <param name="palette">The palette.</param>
		public GrfImage(ref byte[] pixels, int width, int height, GrfImageType type, ref byte[] paletteRgba) {
			Width = width;
			Height = height;
			GrfImageType = type;

			Pixels = pixels;
			Palette = paletteRgba;

			if (type >= GrfImageType.NotEvaluated) {
				SelfAny();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="type">The type.</param>
		public GrfImage(ref byte[] pixels, int width, int height, GrfImageType type) {
			Width = width;
			Height = height;
			GrfImageType = type;

			Pixels = pixels;

			if (type >= GrfImageType.NotEvaluated) {
				SelfAny();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="type">The type.</param>
		public GrfImage(byte[] pixels, int width, int height, GrfImageType type) {
			Width = width;
			Height = height;
			GrfImageType = type;

			Pixels = Methods.Copy(pixels);

			if (type >= GrfImageType.NotEvaluated) {
				SelfAny();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="type">The type.</param>
		/// <param name="palette">The palette.</param>
		public GrfImage(byte[] pixels, int width, int height, GrfImageType type, byte[] paletteRgba) {
			Width = width;
			Height = height;
			GrfImageType = type;

			Pixels = Methods.Copy(pixels);
			Palette = Methods.Copy(paletteRgba);

			if (type >= GrfImageType.NotEvaluated) {
				SelfAny();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public GrfImage(ref byte[] data) {
			Width = -1;
			Height = -1;

			Pixels = data;
			GrfImageType = _getType();

			SelfAny();
		}

		/// <summary>
		/// Gets the width of the image.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Gets the height of the image.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets the pixels of the image, either indexed or Bgr(a).
		/// </summary>
		public byte[] Pixels { get; private set; }

		internal bool[] TransparentPixels { get; private set; }

		/// <summary>
		/// Gets the type of the image.
		/// </summary>
		public GrfImageType GrfImageType { get; private set; }

		/// <summary>
		/// Gets the palette, in the RGBA format.
		/// </summary>
		public byte[] Palette { get; private set; }

		public int NumberOfPixels {
			get {
				if (GrfImageType == GrfImageType.Indexed8)
					return Pixels.Length;
				if (GrfImageType == GrfImageType.Bgr24)
					return Pixels.Length / 3;
				if (GrfImageType == GrfImageType.Bgra32 ||
				    GrfImageType == GrfImageType.Bgr32)
					return Pixels.Length / 4;
				return -1;
			}
		}

		public IEnumerable<GrfColor> Colors {
			get {
				if (GrfImageType == GrfImageType.Indexed8) {
					Dictionary<byte, GrfColor> colors = new Dictionary<byte, GrfColor>();

					for (int i = 0; i < 256; i++) {
						colors[(byte) i] = new GrfColor(Palette, i * 4);
					}

					foreach (byte pixel in Pixels) {
						yield return colors[pixel];
					}
				}
				else if (GrfImageType == GrfImageType.Bgra32 || GrfImageType == GrfImageType.Bgr32) {
					for (int i = 0; i < Pixels.Length; i += 4) {
						yield return new GrfColor(Pixels, i, GrfImageType);
					}
				}
				else if (GrfImageType == GrfImageType.Bgr24) {
					for (int i = 0; i < Pixels.Length; i += 3) {
						yield return new GrfColor(Pixels, i, GrfImageType);
					}
				}
			}
		}

		public static implicit operator GrfImage(string value) {
			return new GrfImage(value);
		}

		#region Pixel methods
		/// <summary>
		/// Copies the pixels at the specified zone.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <returns>The pixels in the specified zone</returns>
		public byte[] CopyPixels(int left, int top, int width, int height) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			if (left + width > Width ||
			    top + height > Height)
				throw new Exception("Values go outside the image size.");

			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			int stride = width * bpp;

			byte[] pixels = new byte[bpp * width * height];

			for (int y = 0; y < height; y++) {
				Buffer.BlockCopy(Pixels, (left + (y + top) * Width) * bpp, pixels, y * width * bpp, stride);
			}

			return pixels;
		}

		/// <summary>
		/// Copies the pixels at the specified zone. If the zone goes 
		/// outside the image, it will be filled with a predefined color.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <returns></returns>
		public byte[] CopyPixelsUnrestricted(int left, int top, int width, int height) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] pixels = new byte[bpp * width * height];
			int stride = width * bpp;
			int copyWidth = width;
			byte initData = _getDefaultByteColor();

			if (initData != 0) {
				byte[] strideLine = new byte[stride];

				for (int i = 0; i < strideLine.Length; i++) {
					strideLine[i] = initData;
				}

				for (int i = 0; i < height; i++) {
					Buffer.BlockCopy(strideLine, 0, pixels, stride * i, strideLine.Length);
				}
			}

			int marginLeft = 0;
			int marginTop = 0;

			if (left < 0) {
				width = width + left;
				marginLeft = -left;
				left = 0;
			}

			if (left + width > Width) {
				width = Width - left;
			}

			if (top < 0) {
				height = height + top;
				marginTop = -top;
				top = 0;
			}

			if (top + height > Height) {
				height = Height - top;
			}

			stride = width * bpp;

			if (stride > 0 && height > 0) {
				for (int y = 0; y < height; y++) {
					Buffer.BlockCopy(Pixels, (left + (y + top) * Width) * bpp, pixels, ((y + marginTop) * copyWidth + marginLeft) * bpp, stride);
				}
			}

			return pixels;
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="width">The width of the pixels.</param>
		/// <param name="height">The height of the pixels.</param>
		/// <param name="pixels">The pixels.</param>
		public void SetPixels(int left, int top, int width, int height, byte[] pixels) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			if (left + width > Width ||
			    top + height > Height)
				throw new Exception("Values go outside the image size.");

			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			int stride = width * bpp;

			unsafe {
				fixed (byte* bImagePixels = Pixels)
				fixed (byte* bCopyPixels = pixels) {
					for (int y = 0; y < height; y++) {
						Buffer.MemoryCopy(bCopyPixels + y * width * bpp, bImagePixels + (left + (y + top) * Width) * bpp, stride, stride);
						//Buffer.BlockCopy(pixels, y * width * bpp, Pixels, (left + (y + top) * Width) * bpp, stride);
					}
				}
			}
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="image">The image.</param>
		public void SetPixels(int left, int top, GrfImage image) {
			SetPixels(left, top, image.Width, image.Height, image);
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="width">The width of the image to copy.</param>
		/// <param name="height">The height of the image to copy.</param>
		/// <param name="image">The image.</param>
		public void SetPixels(int left, int top, int width, int height, GrfImage image) {
			_setPixels(left, top, width, height, image, false);
		}

		private void _setPixels(int left, int top, int width, int height, GrfImage image, bool blendLayers) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			if (left + width > Width ||
				top + height > Height)
				throw new Exception("Values go outside the image size.");

			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (image.GrfImageType != GrfImageType) {
				image = image.Copy();
				image.Convert(GrfImageType);
			}

			int stride = width * bpp;
			byte[] pixels = image.CopyPixels(0, 0, width, height);

			if (blendLayers && GrfImageType == GrfImageType.Bgra32) {
				int offset;
				int offsetBackground;
				double pF;
				double a1;
				double a2;
				
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						offset = (y * width + x) * bpp;
						offsetBackground = (left + x + (y + top) * Width) * bpp;
						a2 = pixels[offset + 3] / 255d;
						a1 = Pixels[offsetBackground + 3] / 255d;

						if (a1 <= 0) {
							Pixels[offsetBackground + 0] = pixels[offset + 0];
							Pixels[offsetBackground + 1] = pixels[offset + 1];
							Pixels[offsetBackground + 2] = pixels[offset + 2];
							Pixels[offsetBackground + 3] = pixels[offset + 3];
						}
						else {
							pF = a1 * (1d - a2);

							Pixels[offsetBackground + 0] = (byte) (pixels[offset + 0] * a2 + Pixels[offsetBackground + 0] * pF);
							Pixels[offsetBackground + 2] = (byte) (pixels[offset + 2] * a2 + Pixels[offsetBackground + 2] * pF);
							Pixels[offsetBackground + 1] = (byte) (pixels[offset + 1] * a2 + Pixels[offsetBackground + 1] * pF);
							Pixels[offsetBackground + 3] = (byte) ((a2 + pF) * 255d);
						}
						//Pixels[offsetBackground + 3] = (byte)((1d - (1d - a2) * (1d - a1)) * 255d);
					}
				}
			}
			//else if (blendLayers && GrfImageType == GrfImageType.Indexed8) {
			//	int offset;
			//	int offsetBackground;
			//
			//	for (int y = 0; y < height; y++) {
			//		for (int x = 0; x < width; x++) {
			//			offset = (y * width + x) * bpp;
			//			offsetBackground = (left + x + (y + top) * Width) * bpp;
			//
			//			if (Pixels[offset] != 0) {
			//				Pixels[offsetBackground] = pixels[offset];
			//			}
			//		}
			//	}
			//}
			else {
				for (int y = 0; y < height; y++) {
					Buffer.BlockCopy(pixels, y * width * bpp, Pixels, (left + (y + top) * Width) * bpp, stride);
				}
			}
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		public void SetPixels(ref byte[] pixels) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Pixels = pixels;
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		public void SetPixels(byte[] pixels) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Pixels = Methods.Copy(pixels);
		}

		/// <summary>
		/// Sets pixels in the image, if the image goes outside the boundaries the source will be increased.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="image">The image.</param>
		public void SetPixelsUnrestricted(int left, int top, GrfImage image) {
			SetPixelsUnrestricted(left, top, image, false);
		}

		/// <summary>
		/// Sets pixels in the image, if the image goes outside the boundaries the source will be increased.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="image">The image.</param>
		/// <param name="blendLayers">Indicate whether the layers should blend together.</param>
		public void SetPixelsUnrestricted(int left, int top, GrfImage image, bool blendLayers) {
			//SetPixelsUnrestricted(left, top, image.Width, image.Height, image);
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (image.GrfImageType != GrfImageType) {
				image = image.Copy();
				image.Convert(GrfImageType);
			}

			int mL = 0;
			int mR = 0;
			int mT = 0;
			int mB = 0;

			if (left < 0) mL = -left;
			if (top < 0) mT = -top;
			if (left + image.Width > Width) mR = left + image.Width - Width;
			if (top + image.Height > Height) mB = top + image.Height - Height;

			Margin(mL, mT, mR, mB);

			if (left < 0) left = 0;
			if (top < 0) top = 0;

			_setPixels(left, top, image.Width, image.Height, image, blendLayers);
		}

		/// <summary>
		/// Sets the palette.
		/// </summary>
		/// <param name="newPalette">The new palette.</param>
		public void SetPalette(ref byte[] newPalette) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Palette = newPalette;
		}

		/// <summary>
		/// Sets the palette.
		/// </summary>
		/// <param name="newPalette">The new palette.</param>
		public void SetPalette(byte[] newPalette) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Palette = Methods.Copy(newPalette);
		}
		#endregion

		#region Image similarity methods
		/// <summary>
		/// Clears the cache used for identifying the image similarities.
		/// </summary>
		public static void ClearBufferedData() {
			_bufferedSimilarities.Clear();
		}

		public bool IsIdentical(GrfImage image) {
			return Methods.ByteArrayCompare(image.Pixels, Pixels) && Methods.ByteArrayCompare(image.Palette, Palette);
		}

		/// <summary>
		/// Compares this image with the one specified.
		/// </summary>
		/// <param name="image">The image to compare with.</param>
		/// <returns>The similarity ratio, between 0 and 1</returns>
		public double SimilarityWith(GrfImage image) {
			var data1 = CalculateSimilarityData(this);
			var data2 = CalculateSimilarityData(image);

			double similarity = 0;
			double factor = 1 / 6d;

			double maxPixels = Math.Max(NumberOfPixels, image.NumberOfPixels);
			double minPixels = Math.Min(NumberOfPixels, image.NumberOfPixels);

			double globalFactor = 1;

			if (maxPixels - minPixels > 0) {
				globalFactor = (1d - (maxPixels - minPixels) / maxPixels);
			}

			for (int i = 0; i < 6; i++) {
				double max = Math.Max(data1[i], data2[i]);

				if (max == 0)
					similarity += factor;
				else
					similarity += (1d - Math.Abs((data1[i] - data2[i]) / max)) * factor;
			}

			return similarity * globalFactor;
		}

		/// <summary>
		/// Computes the image similarity data.
		/// </summary>
		/// <param name="grfImage">The image.</param>
		/// <returns>A list of hue amount.</returns>
		public static List<int> CalculateSimilarityData(GrfImage grfImage) {
			if (!_bufferedSimilarities.ContainsKey(grfImage)) {
				List<int> huesCount = new List<int>(6) {0, 0, 0, 0, 0, 0};
				GrfColor toIgnore = grfImage.GrfImageType == GrfImageType.Indexed8 ? new GrfColor(grfImage.Palette, 0) : null;

				foreach (GrfColor color in grfImage.Colors) {
					if (toIgnore != null && toIgnore.Equals(color)) {
						continue;
					}

					if (color.A == 0) continue;

					var hue = color.Hue * 360d + 30;

					int container = (int) (hue / 60) % 6;

					huesCount[container]++;
				}

				_bufferedSimilarities[grfImage] = huesCount;
			}

			return _bufferedSimilarities[grfImage];
		}

		#endregion

		#region Transformation methods
		/// <summary>
		/// Scales the image by a proportional scale.
		/// </summary>
		/// <param name="scale">The scale.</param>
		/// <param name="scalingMode">The scaling mode.</param>
		public void Scale(float scale, GrfScalingMode scalingMode) {
			Scale(scale, scale, scalingMode);
		}

		/// <summary>
		/// Scales the image in X and Y.
		/// </summary>
		/// <param name="x">The scale x.</param>
		/// <param name="y">The scale y.</param>
		/// <param name="scalingMode">The scaling mode.</param>
		public void Scale(float x, float y, GrfScalingMode scalingMode) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			switch (scalingMode) {
				case GrfScalingMode.NearestNeighbor:
					_scaleNearest(x, y);
					break;
				case GrfScalingMode.LinearScaling:
					if (GrfImageType == GrfImageType.Indexed8) {
						Convert(GrfImageType.Bgra32);
					}
					
					_scaleLinear(x, y);
					break;
				default:
					_scaleNearest(x, y);
					break;
			}
		}

		/// <summary>
		/// Adds a margin around the image by specifying the desired width and height.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public void Redim(int width, int height) {
			Redim(width, height, -1);
		}

		/// <summary>
		/// Adds a margin around the image by specifying the desired width and height.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="initValue">The default pixel value.</param>
		public void Redim(int width, int height, int initValue) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			int bpp = GetBpp();

			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			int leftMargin = (width - Width) / 2;
			int topMargin = (height - Height) / 2;

			if (leftMargin < 0) return;
			if (topMargin < 0) return;

			byte[] data = new byte[width * height * bpp];
			byte initData = initValue < 0 ? _getDefaultByteColor() : (byte)initValue;

			for (int i = 0; i < data.Length; i++) {
				data[i] = initData;
			}

			for (int j = 0; j < Height; j++) {
				Buffer.BlockCopy(Pixels, j * Width * bpp, data, width * (topMargin + j) * bpp + leftMargin * bpp, Width * bpp);
			}

			Pixels = data;
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Rotates the image left or right by 90 degrees.
		/// </summary>
		/// <param name="dir">The direction.</param>
		public void Rotate(RotateDirection dir) {
			int bpp = _getBpp();

			byte[] pixels = new byte[Pixels.Length];

			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					for (int k = 0; k < bpp; k++) {
						if (dir == RotateDirection.Left)
							pixels[bpp * (Width * (-x + Width - 1) + y) + k] = Pixels[bpp * (Width * y + x) + k];
						else
							pixels[bpp * (Width * x - y + Height - 1) + k] = Pixels[bpp * (Width * y + x) + k];
					}
				}
			}

			Buffer.BlockCopy(pixels, 0, Pixels, 0, Pixels.Length);
			var temp = Height;
			Height = Width;
			Width = temp;
		}

		/// <summary>
		/// Flips the image in the specified direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		public void Flip(FlipDirection direction) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] pixels = new byte[Pixels.Length];

			if (direction == FlipDirection.Horizontal) {
				for (int i = 0; i < Height; i++) {
					for (int j = 0; j < Width; j++) {
						for (int k = 0; k < bpp; k++) {
							pixels[(Width * i + j) * bpp + k] = Pixels[(Width * i + (Width - j - 1)) * bpp + k];
						}
					}
				}
			}
			else {
				int stride = Width * bpp;
				for (int i = 0; i < Height; i++) {
					Buffer.BlockCopy(Pixels, i * stride, pixels, (Height - i - 1) * stride, stride);
				}
			}

			Buffer.BlockCopy(pixels, 0, Pixels, 0, Pixels.Length);
		}

		public void DrawLine(int x1, int y1, int x2, int y2, GrfColor color) {
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("x1", x1);
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("y1", y1);
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("y2", y2);
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("x2", x2);

			int maxX = Math.Max(x1, x2);
			int maxY = Math.Max(y1, y2);
			int bpp = _getBpp();

			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			int rM = 0;
			int tM = 0;

			if (maxX >= Width) {
				rM = Width - maxX + 1;
			}

			if (maxY >= Height) {
				tM = Height - maxY + 1;
			}

			Margin(0, tM, rM, 0);

			TkVector2 p1 = new TkVector2(x1, y1);
			TkVector2 p2 = new TkVector2(x2, y2);
			TkVector2 diff = p2 - p1;

			int length = (int) Math.Ceiling(diff.Length);
			diff = TkVector2.Normalize(diff);

			for (int i = 0; i < length; i++) {
				int x = x1 + (int)(diff.X * i);
				int y = x1 + (int)(diff.X * i);

				if (x >= Width) continue;
				if (y >= Height) continue;
				if (x < 0 || y < 0) continue;
				SetColor(x, y, color);
			}
		}

		internal void SetColor(int x, int y, GrfColor color) {
			int bpp = _getBpp();

			if (bpp <= 1)
				throw GrfExceptions.__UnsupportedImageFormat.Create(bpp);

			if (GrfImageType == GrfImageType.Bgr24) {
				Buffer.BlockCopy(color.ToBgrBytes(), 0, Pixels, (y * Width + x) * bpp, bpp);
			}
			else if (GrfImageType == GrfImageType.Bgra32) {
				Buffer.BlockCopy(color.ToBgraBytes(), 0, Pixels, (y * Width + x) * bpp, bpp);
			}
			else if (GrfImageType == GrfImageType.Bgr32) {
				Buffer.BlockCopy(color.ToBgraBytes(), 0, Pixels, (y * Width + x) * bpp, bpp);
			}
		}

		#endregion

		#region Public methods
		/// <summary>
		/// Sets the type of the GRF image.
		/// </summary>
		/// <param name="type">The type.</param>
		public void SetGrfImageType(GrfImageType type) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfImageType = type;
		}

		public void Stroke(int thickness, int pixelIndex) {
			
		}

		public void Stroke(int thickness, GrfColor color) {
			//GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			//GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			//int bpp = GetBpp();
			//GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);
			//
			//var stroke = new bool[this.Pixels.Length / bpp];
			//
			//if (GrfImageType == Image.GrfImageType.Bgra32) {
			//	for (int y = 0; y < Height; y++) {
			//		for (int x = 0; x < Width; x++) {
			//			int idx = y * Height + x;
			//			int alpha = idx * bpp + 3;
			//			
			//			if (Pixels[alpha] == 0)
			//		}
			//	}
			//}
			//else {
			//	throw GrfExceptions.__UnsupportedImageFormat.Create();
			//}
		}

		/// <summary>
		/// Crops the image by the specified amount.
		/// </summary>
		/// <param name="amount">The amount.</param>
		public void Crop(int amount) {
			Crop(amount, amount, amount, amount);
		}

		/// <summary>
		/// Crops the image by the specified amount.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="right">The amount on right.</param>
		/// <param name="bottom">The amount on bottom.</param>
		public void Crop(int left, int top, int right, int bottom) {
			int width = Width - left - right;
			int height = Height - top - bottom;
			int pixelsLeft = width * height;

			if (left == 0 && top == 0 && right == 0 && bottom == 0)
				return;

			if (pixelsLeft <= 0 || width <= 0 || height <= 0) {
				Width = 0;
				Height = 0;
				Pixels = new byte[0];
			}
			else {
				byte[] pixels = CopyPixelsUnrestricted(left, top, width, height);

				Pixels = pixels;
				Width = width;
				Height = height;
			}
		}

		/// <summary>
		/// Adds a margins for the specified amount.
		/// </summary>
		/// <param name="amount">The amount.</param>
		public void Margin(int amount) {
			Crop(-amount);
		}

		/// <summary>
		/// Adds a margins for the specified amount.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="right">The amount on right.</param>
		/// <param name="bottom">The amount on bottom.</param>
		public void Margin(int left, int top, int right, int bottom) {
			if (left == 0 && top == 0 && right == 0 && bottom == 0)
				return;

			Crop(-left, -top, -right, -bottom);
		}

		/// <summary>
		/// Trims this image by removing transparent pixels.
		/// </summary>
		public void Trim() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte initData = _getTransparentByteColor();

			int leftToRemove = GetLengthTrim(TrimDirection.Left, bpp, initData);
			int rightToRemove = GetLengthTrim(TrimDirection.Right, bpp, initData);
			int topToRemove = GetLengthTrim(TrimDirection.Top, bpp, initData);
			int bottomToRemove = GetLengthTrim(TrimDirection.Bottom, bpp, initData);

			Crop(leftToRemove, topToRemove, rightToRemove, bottomToRemove);
		}

		/// <summary>
		/// Gets the color at the requested pixel index.
		/// </summary>
		/// <param name="pixelIndex">Index of the pixel.</param>
		/// <returns>The requested color</returns>
		public GrfColor GetColor(int pixelIndex) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			int bpp = GetBpp();

			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (GrfImageType == GrfImageType.Indexed8) {
				if (pixelIndex == -1)
					return new GrfColor(Palette, 4 * Pixels[Pixels.Length - 1], GrfImageType);

				return new GrfColor(Palette, 4 * Pixels[pixelIndex], GrfImageType);
			}

			if (pixelIndex == -1)
				return new GrfColor(Pixels, Pixels.Length - bpp, GrfImageType);

			return new GrfColor(Pixels, bpp * pixelIndex, GrfImageType);
		}

		/// <summary>
		/// Sets the color at the requested pixel index.
		/// </summary>
		/// <param name="pixelIndex">Index of the pixel.</param>
		/// <param name="color">The requested color.</param>
		public void SetColor(int pixelIndex, GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			int bpp = GetBpp();

			if (GrfImageType == GrfImageType.Indexed8) {
				throw GrfExceptions.__UnsupportedImageFormat.Create();
			}

			if (bpp == 4) {
				Buffer.BlockCopy(color.ToBgraBytes(), 0, Pixels, bpp * pixelIndex, bpp);
			}
			else {
				Buffer.BlockCopy(color.ToBgrBytes(), 0, Pixels, bpp * pixelIndex, bpp);
			}
		}

		/// <summary>
		/// Dispose the object immediatly.
		/// </summary>
		public void Close() {
			_isClosed = true;
			Pixels = null;
			Palette = null;
		}

		/// <summary>
		/// Applies the color the each color component.
		/// </summary>
		/// <param name="color">The color.</param>
		public void ApplyChannelColor(GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (color.Equals(GrfColor.White)) {
				return;
			}

			float facB = color.B / 255f;
			float facG = color.G / 255f;
			float facR = color.R / 255f;
			float facA = color.A / 255f;

			if (GrfImageType == GrfImageType.Indexed8) {
				if (Palette != null) {
					for (int i = 0; i < 1024; i += 4) {
						Palette[i + 0] = (byte)(Palette[i + 0] * facR);
						Palette[i + 1] = (byte)(Palette[i + 1] * facG);
						Palette[i + 2] = (byte)(Palette[i + 2] * facB);
						Palette[i + 3] = (byte)(Palette[i + 3] * facA);
					}
				}
			}
			else {
				float[] fac = new float[] {
					color.B / 255f,
					color.G / 255f,
					color.R / 255f,
					color.A / 255f
				};

				int numIt = Width * Height * bpp;

				for (int i = 0; i < numIt; i += bpp) {
					for (int k = 0; k < bpp; k++) {
						Pixels[i + k] = (byte)(Pixels[i + k] * fac[k]);
					}
				}
			}
		}

		/// <summary>
		/// Applies the color factor for each color component.
		/// </summary>
		/// <param name="fact">The color factor to apply.</param>
		public void ApplyColorChannel(float fact) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (fact > 1) {
				if (fact > 2)
					fact = 2;

				byte add = (byte)(255 * (fact - 1));

				if (GrfImageType == GrfImageType.Indexed8) {
					if (Palette != null) {
						for (int i = 0; i < Palette.Length; i += 4) {
							Palette[i + 0] = (byte)Math.Min(255, Palette[i + 0] + add);
							Palette[i + 1] = (byte)Math.Min(255, Palette[i + 1] + add);
							Palette[i + 2] = (byte)Math.Min(255, Palette[i + 2] + add);
						}
					}
				}
				else {
					for (int i = 0; i < Pixels.Length; i += bpp) {
						Pixels[i + 0] = (byte)Math.Min(255, Pixels[i + 0] + add);
						Pixels[i + 1] = (byte)Math.Min(255, Pixels[i + 1] + add);
						Pixels[i + 2] = (byte)Math.Min(255, Pixels[i + 2] + add);
					}
				}
			}
			else {
				if (GrfImageType == GrfImageType.Indexed8) {
					if (Palette != null) {
						for (int i = 0; i < Palette.Length; i += 4) {
							Palette[i + 0] = (byte)(Palette[i + 0] * fact);
							Palette[i + 1] = (byte)(Palette[i + 1] * fact);
							Palette[i + 2] = (byte)(Palette[i + 2] * fact);
						}
					}
				}
				else {
					for (int i = 0; i < Pixels.Length; i += bpp) {
						Pixels[i + 0] = (byte)(Pixels[i + 0] * fact);
						Pixels[i + 1] = (byte)(Pixels[i + 1] * fact);
						Pixels[i + 2] = (byte)(Pixels[i + 2] * fact);
					}
				}
			}
		}

		/// <summary>
		/// Copy an image at the specified location and return the content.
		/// </summary>
		/// <param name="x">x.</param>
		/// <param name="y">y.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <returns>The copied image at the specified location</returns>
		public GrfImage Extract(int x, int y, int width, int height) {
			if (x < 0) throw new ArgumentOutOfRangeException("x");
			if (y < 0) throw new ArgumentOutOfRangeException("y");

			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] pixels = new byte[width * height * bpp];

			byte defaultByte = (byte)(GrfImageType == GrfImageType.Indexed8 ? 0 : 255);

			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = defaultByte;

			int actualWidth = (x + width) > Width ? (Width - x) : width;
			int actualHeight = (y + height) > Height ? (Height - y) : height;

			if (actualWidth < 0 || actualHeight < 0) {
				return new GrfImage(ref pixels, width, height, GrfImageType);
			}

			for (int y2 = 0; y2 < actualHeight; y2++) {
				Buffer.BlockCopy(Pixels, ((y2 + y) * Width + x) * bpp, pixels, y2 * bpp * width, actualWidth * bpp);
			}

			if (GrfImageType == GrfImageType.Indexed8) {
				byte[] palette = Methods.Copy(Palette);
				return new GrfImage(ref pixels, width, height, GrfImageType, ref palette);
			}

			return new GrfImage(ref pixels, width, height, GrfImageType);
		}

		/// <summary>
		/// Gets the bit per pixel rate.
		/// </summary>
		/// <returns></returns>
		public int GetBpp() {
			return _getBpp();
		}

		/// <summary>
		/// Sets the color of the palette directly.
		/// </summary>
		/// <param name="index256">The index.</param>
		/// <param name="color">The color.</param>
		public void SetPaletteColor(int index256, GrfColor color) {
			Buffer.BlockCopy(color.ToRgbaBytes(), 0, Palette, 4 * index256, 4);
		}

		/// <summary>
		/// Makes the transparent color as pink.
		/// </summary>
		public void MakeTransparentPink() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			byte[] pink = new byte[] { 255, 0, 255, 255 };

			if (GrfImageType == GrfImageType.Indexed8) {
				Palette[0] = 255;
				Palette[1] = 0;
				Palette[2] = 255;
			}
			else if (GrfImageType == GrfImageType.Bgra32) {
				int bpp = _getBpp();

				for (int i = 0, count = Pixels.Length; i < count; i += bpp) {
					if (Pixels[i + 3] == 0) {
						Pixels[i + 0] = 255;
						Pixels[i + 1] = 0;
						Pixels[i + 2] = 255;
						Pixels[i + 3] = 255;
					}
				}
			}
			else {
				int bpp = _getBpp();

				for (int i = 0, count = Pixels.Length; i < count; i += bpp) {
					if (Pixels[i] == pink[0] &&
						Pixels[i + 1] == pink[1] &&
						Pixels[i + 2] == pink[2]) {
						for (int p = 0; p < bpp; p++) {
							Pixels[p + i] = 0;
						}
					}
				}
			}
		}

		/// <summary>
		/// Makes a specified color transparent.
		/// </summary>
		/// <param name="color">The color that will be made transparent.</param>
		public void MakeColorTransparent(GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			// Palette uses Rgba format
			if (GrfImageType == GrfImageType.Indexed8) {
				unsafe {
					fixed (byte* pBase = Palette) {
						byte* p = pBase;
						byte* pEnd = pBase + Palette.Length;

						while (p < pEnd) {
							if (p[0] == color.R && p[1] == color.G && p[2] == color.B)
								p[3] = 0;
							p += 4;
						}
					}
				}
			}
			// Pixels uses Bgra format
			else if (GrfImageType == GrfImageType.Bgr24) {
				int bpp = _getBpp();
				TransparentPixels = new bool[Width * Height];

				unsafe {
					fixed (byte* pBase = Pixels) {
						byte* p = pBase;
						byte* pEnd = pBase + Pixels.Length;
						int i = 0;

						while (p < pEnd) {
							if (p[0] == color.B && p[1] == color.G && p[2] == color.R) {
								p[3] = 0;
								TransparentPixels[i / bpp] = true;
								p[0] = p[1] = p[2] = 0;
							}

							p += bpp;
							i += bpp;
						}
					}
				}
			}
			else {
				int bpp = _getBpp();

				unsafe {
					fixed (byte* pBase = Pixels) {
						byte* p = pBase;
						byte* pEnd = pBase + Pixels.Length;

						while (p < pEnd) {
							if (p[0] == color.B && p[1] == color.G && p[2] == color.R) {
								p[0] = p[1] = p[2] = 0;

								if (bpp > 3)
									p[3] = 0;
							}

							p += bpp;
						}
					}
				}
			}
		}

		/// <summary>
		/// Makes the pink color transparent.
		/// </summary>
		public void MakePinkTransparent() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			byte[] pink = new byte[] { 255, 0, 255, 255 };

			if (GrfImageType == GrfImageType.Indexed8) {
				for (int i = 0; i < 1024; i += 4) {
					if (Palette[i] > 250 &&
						Palette[i + 1] < 5 && 
						Palette[i + 2] > 250) {
						Palette[i + 0] = 255;
						Palette[i + 1] = 0;
						Palette[i + 2] = 255;
						Palette[i + 3] = 0;
					}
				}
			}
			else if (GrfImageType == GrfImageType.Bgr24) {
				int bpp = _getBpp();
				TransparentPixels = new bool[Width * Height];

				for (int i = 0, count = Pixels.Length; i < count; i += bpp) {
					if (Pixels[i] > 250 &&
						Pixels[i + 1] < 5 &&
						Pixels[i + 2] > 250) {
						TransparentPixels[i / bpp] = true;

						for (int p = 0; p < bpp; p++) {
							Pixels[p + i] = 0;
						}
					}
				}
			}
			else {
				int bpp = _getBpp();

				for (int i = 0, count = Pixels.Length; i < count; i += bpp) {
					if (Pixels[i] > 250 &&
						Pixels[i + 1] < 5 &&
						Pixels[i + 2] > 250) {
						for (int p = 0; p < bpp; p++) {
							Pixels[p + i] = 0;
						}
					}
				}
			}
		}

		/// <summary>
		/// Makes the first pixel transparent.
		/// </summary>
		public void MakeFirstPixelTransparent() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			if (GrfImageType == GrfImageType.Indexed8) {
				Palette[3] = 0;
			}
		}

		/// <summary>
		/// Creates a copy of the image.
		/// </summary>
		/// <returns></returns>
		public GrfImage Copy() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			byte[] pixels = new byte[Pixels.Length];
			Buffer.BlockCopy(Pixels, 0, pixels, 0, pixels.Length);

			bool[] transPixels = null;

			if (TransparentPixels != null) {
				transPixels = new bool[TransparentPixels.Length];
				Buffer.BlockCopy(TransparentPixels, 0, transPixels, 0, transPixels.Length);
			}

			if (Palette != null) {
				byte[] pal = new byte[Palette.Length];
				Buffer.BlockCopy(Palette, 0, pal, 0, Palette.Length);
				return new GrfImage(ref pixels, Width, Height, GrfImageType, ref pal) { TransparentPixels = transPixels };
			}

			return new GrfImage(ref pixels, Width, Height, GrfImageType) { TransparentPixels = transPixels };
		}

		/// <summary>
		/// Creates the transparency image using two composites.
		/// The black composite is the original image using a black background, while the white one uses a white background.
		/// </summary>
		/// <param name="blackComposite">The black composite.</param>
		/// <param name="whiteComposite">The white composite.</param>
		/// <returns></returns>
		public static GrfImage CreateTransparencyImage(GrfImage blackComposite, GrfImage whiteComposite) {
			if (blackComposite.GrfImageType != GrfImageType.Bgr24 && blackComposite.GrfImageType != GrfImageType.Bgr32)
				throw GrfExceptions.__UnsupportedImageFormat.Create();

			if (whiteComposite.GrfImageType != GrfImageType.Bgr24 && whiteComposite.GrfImageType != GrfImageType.Bgr32)
				throw GrfExceptions.__UnsupportedImageFormat.Create();

			if (whiteComposite.GrfImageType != blackComposite.GrfImageType)
				throw GrfExceptions.__UnsupportedImageFormat.Create();

			GrfImage result = new GrfImage(new byte[blackComposite.Width * blackComposite.Height * 4], blackComposite.Width, blackComposite.Height, GrfImageType.Bgra32);
			
			int bpp = blackComposite.GetBpp();
			int count = blackComposite.Pixels.Length / bpp;
			int idx = 0;
			int idxR = 0;

			for (int j = 0; j < count; j++) {
				byte transparency = 0;

				for (int i = 0; i < 3; i++) {
					transparency = Math.Max(transparency, (byte)(255 - whiteComposite.Pixels[idx + i] + blackComposite.Pixels[idx + i]));
				}

				if (transparency > 0) {
					for (int i = 0; i < 3; i++) {
						result.Pixels[idxR + i] = (byte)(Math.Round(255f * blackComposite.Pixels[idx + i] / transparency, MidpointRounding.AwayFromZero));
					}
				}

				result.Pixels[idxR + 3] = transparency;
				idx += bpp;
				idxR += 4;
			}

			return result;
		}
		#endregion

		#region Conversion methods

		/// <summary>
		/// Reads the image and converts it to a readable format.
		/// </summary>
		/// <typeparam name="T">The image parser</typeparam>
		public void Self<T>() where T : class {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			GrfImage image = ImageConverterManager.Self<T>(this);
			SetGrfImageType(image.GrfImageType);

			byte[] pixels = image.Pixels;
			byte[] palette = image.Palette;

			SetPalette(ref palette);
			SetPixels(ref pixels);
		}

		/// <summary>
		/// Reads the image and converts it to a readable format by using the first image parser.
		/// </summary>
		public void SelfAny() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			GrfImage image = ImageConverterManager.SelfAny(this);

			GrfImageType = image.GrfImageType;

			Pixels = image.Pixels;
			Palette = image.Palette;
			Width = image.Width;
			Height = image.Height;
		}

		/// <summary>
		/// Converts an image to a .net format (or a custom one) defined by the 
		/// ImageConverterManager class.
		/// </summary>
		/// <typeparam name="T">Image format</typeparam>
		/// <returns>The image converted</returns>
		public T Cast<T>() where T : class {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			return ImageConverterManager.Convert<T>(this);
		}

		/// <summary>
		/// Converts the image to the specified destination format.
		/// </summary>
		/// <param name="destinationFormat">The destination format.</param>
		/// <param name="sourceFormat">The source format.</param>
		public void Convert(IImageFormatConverter destinationFormat, IImageFormatConverter sourceFormat = null) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			if (sourceFormat == null)
				sourceFormat = ImageFormatProvider.GetFormatConverter(GrfImageType);

			sourceFormat.ToBgra32(this);
			destinationFormat.Convert(this);
		}

		/// <summary>
		/// Converts the image to the specified new format.
		/// </summary>
		/// <param name="newFormat">The new image format.</param>
		public void Convert(GrfImageType newFormat) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			switch (newFormat) {
				case GrfImageType.Bgr24:
					Convert(new Bgr24FormatConverter());
					break;
				case GrfImageType.Bgr32:
					Convert(new Bgr32FormatConverter());
					break;
				case GrfImageType.Bgra32:
					if (GrfImageType == GrfImageType.Indexed8)
						Palette[3] = 0;

					Convert(new Bgra32FormatConverter());
					break;
				case GrfImageType.Indexed8:
					Convert(new Indexed8FormatConverter { Options = Indexed8FormatConverter.PaletteOptions.AutomaticallyGeneratePalette });
					break;
				default:
					throw new Exception("Image format not supported. Use the method requiring an IImageFormatConverter provider instead.");
			}
		}

		/// <summary>
		/// Converts the image to the specified new format.
		/// </summary>
		/// <param name="newFormat">The new image format.</param>
		/// <param name="palette">The palette used for Indexed8 conversion.</param>
		public void Convert(GrfImageType newFormat, byte[] palette) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			if (newFormat == GrfImageType.Indexed8) {
				Convert(new Indexed8FormatConverter { ExistingPalette = palette, Options = Indexed8FormatConverter.PaletteOptions.UseExistingPalette });
			}
			else {
				Convert(newFormat);
			}
		}

		/// <summary>
		/// Determines whether this image can be converted to Indexed8.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance can be converted to Indexed8; otherwise, <c>false</c>.
		/// </returns>
		public bool CanConvertToIndexed8() {
			if (GrfImageType == GrfImageType.Indexed8)
				return true;

			HashSet<GrfColor> colors = new HashSet<GrfColor>();

			foreach (GrfColor color in Colors) {
				colors.Add(color);

				if (colors.Count > 255)
					return false;
			}

			return true;
		}
		#endregion

		public static GrfImageType GetGrfImageType(byte[] data) {
			if (data.Length > 3) {
				if (Methods.ByteArrayCompare(data, 0, 4, PngHeader, 0)) return GrfImageType.NotEvaluatedPng;
				if (Methods.ByteArrayCompare(data, 0, 2, BmpHeader, 0)) return GrfImageType.NotEvaluatedBmp;
				if (Methods.ByteArrayCompare(data, 0, 2, JpgHeader, 0)) return GrfImageType.NotEvaluatedJpg;

				// Might be a TGA, but... since TGAs don't have
				// have a header (sigh, that, is a bad design) we have
				// to try and partially read it.

				if (data.Length > TgaHeader.StructSize) {
					TgaHeader tgaHeader = new TgaHeader(data);

					if (tgaHeader.ValidateHeader()) {
						return GrfImageType.NotEvaluatedTga;
					}
				}

				return GrfImageType.NotEvaluatedJpg;
			}

			return GrfImageType.NotEvaluated;
		}

		private byte _getDefaultByteColor() {
			if (GrfImageType == GrfImageType.Indexed8) {
				return 0;
			}

			if (GrfImageType == GrfImageType.Bgra32 && Height * Width > 0) {
				return GetColor(0).A;
			}

			return 255;
		}
		private byte _getTransparentByteColor() {
			if (GrfImageType == GrfImageType.Indexed8) {
				return 0;
			}

			if (GrfImageType == GrfImageType.Bgra32 && Height * Width > 0) {
				return 0;
			}

			return 255;
		}
		private int _getBpp() {
			switch (GrfImageType) {
				case GrfImageType.Bgr24:
					return 3;
				case GrfImageType.Bgr32:
					return 4;
				case GrfImageType.Bgra32:
					return 4;
				case GrfImageType.Indexed8:
					return 1;
			}

			return -1;
		}
		private GrfImageType _getType() {
			return GetGrfImageType(Pixels);
		}
		private void _scaleLinear(float scaleX, float scaleY) {
			int bpp = _getBpp();
			int newWidth = (int)Math.Round(Width * scaleX, MidpointRounding.AwayFromZero);
			int newHeight = (int)Math.Round(Height * scaleY, MidpointRounding.AwayFromZero);
			byte[] data = new byte[newWidth * newHeight * bpp];

			scaleX = 1 / scaleX;
			scaleY = 1 / scaleY;

			double fractionX, fractionY, oneMinusX, oneMinusY;
			int ceilX, ceilY, floorX, floorY;

			for (int x = 0; x < newWidth; x++) {
				floorX = (int)Math.Floor(x * scaleX);
				ceilX = floorX + 1;
				if (ceilX >= Width) ceilX = floorX;
				fractionX = x * scaleX - floorX;
				oneMinusX = 1.0 - fractionX;

				for (int y = 0; y < newHeight; y++) {
					floorY = (int)Math.Floor(y * scaleY);
					ceilY = floorY + 1;
					if (ceilY >= Height) ceilY = floorY;
					fractionY = y * scaleY - floorY;
					oneMinusY = 1.0 - fractionY;

					for (int k = 0; k < bpp; k++) {
						data[bpp * (y * newWidth + x) + k] =
							(byte)(oneMinusY * (byte)(oneMinusX *
								Pixels[(floorY * Width + floorX) * bpp + k] + fractionX *
								Pixels[(floorY * Width + ceilX) * bpp + k]) + fractionY * (byte)(oneMinusX *
								Pixels[(ceilY * Width + floorX) * bpp + k] + fractionX *
								Pixels[(ceilY * Width + ceilX) * bpp + k]));
					}
				}
			}

			Pixels = data;
			Width = newWidth;
			Height = newHeight;
		}
		private void _scaleNearest(float x, float y) {
			if (GrfImageType == GrfImageType.Indexed8) {
				int newWidth = (int)Math.Round(Width * x, MidpointRounding.AwayFromZero);
				int newHeight = (int)Math.Round(Height * y, MidpointRounding.AwayFromZero);
				byte[] data = new byte[newWidth * newHeight];

				for (int j = 0; j < newHeight; j++) {
					for (int i = 0; i < newWidth; i++) {
						data[j * newWidth + i] = Pixels[(int)((float)j / newHeight * Height) * Width + (int)((float)i / newWidth * Width)];
					}
				}

				Pixels = data;
				Width = newWidth;
				Height = newHeight;
			}
			else {
				int bpp = _getBpp();
				int newWidth = (int)Math.Round(Width * x, MidpointRounding.AwayFromZero);
				int newHeight = (int)Math.Round(Height * y, MidpointRounding.AwayFromZero);
				byte[] data = new byte[newWidth * newHeight * bpp];

				for (int j = 0; j < newHeight; j++) {
					for (int i = 0; i < newWidth; i++) {
						int offset = bpp * ((int)((float)j / newHeight * Height) * Width + (int)((float)i / newWidth * Width));

						for (int k = 0; k < bpp; k++) {
							data[bpp * (j * newWidth + i) + k] = Pixels[offset + k];
						}
					}
				}

				Pixels = data;
				Width = newWidth;
				Height = newHeight;
			}
		}

		public override bool Equals(object obj) {
			if (obj == null) return false;
			if (ReferenceEquals(obj, this)) return true;

			var grfImage = obj as GrfImage;

			if (grfImage != null) {
				if (grfImage.GrfImageType == GrfImageType) {
					if (grfImage.Width != Width || grfImage.Height != Height) return false;

					if (grfImage.GrfImageType == GrfImageType.Indexed8) {
						return Methods.ByteArrayCompare(grfImage.Pixels, Pixels) && Methods.ByteArrayCompare(grfImage.Palette, Palette);
					}

					return Methods.ByteArrayCompare(grfImage.Pixels, Pixels);
				}
			}

			return false;
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = Width;
				hashCode = (hashCode * 397) ^ Height;
				hashCode = (hashCode * 397) ^ (Pixels != null ? Pixels.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ GrfImageType.GetHashCode();
				hashCode = (hashCode * 397) ^ (Palette != null ? Palette.GetHashCode() : 0);
				return hashCode;
			}
		}

		public override string ToString() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			return String.Format("Width = {0}; Height = {1}; ImageType = {2}", Width, Height, GrfImageType);
		}

		internal int GetLengthTrim(TrimDirection direction, int bpp, byte initData) {
			bool stop = false;
			int toRemove = 0;
			FlipDirection stride = direction == TrimDirection.Top || direction == TrimDirection.Bottom ? FlipDirection.Horizontal : FlipDirection.Vertical;

			if (stride == FlipDirection.Vertical) {
				for (int x = direction == TrimDirection.Left ? 0 : Width - 1; x < Width && x >= 0; ) {
					for (int y = 0; y < Height; y++) {
						if (bpp == 1) {
							if (Pixels[y * Width + x] != initData) {
								stop = true;
								break;
							}
						}
						else if (bpp == 3) {
							if (Pixels[bpp * (y * Width + x) + 0] != initData ||
								Pixels[bpp * (y * Width + x) + 1] != initData ||
								Pixels[bpp * (y * Width + x) + 2] != initData) {
								stop = true;
								break;
							}
						}
						else if (bpp == 4) {
							if (Pixels[bpp * (y * Width + x) + 3] != initData) {
								stop = true;
								break;
							}
						}
					}

					if (stop) {
						if (direction == TrimDirection.Right)
							toRemove = Width - x - 1;
						else
							toRemove = x;
						break;
					}

					if (direction == TrimDirection.Left)
						x++;
					else
						x--;
				}
			}
			else {
				for (int y = direction == TrimDirection.Top ? 0 : Height - 1; y < Height && y >= 0; ) {
					for (int x = 0; x < Width; x++) {
						if (bpp == 1) {
							if (Pixels[y * Width + x] != initData) {
								stop = true;
								break;
							}
						}
						else if (bpp == 3) {
							if (Pixels[bpp * (y * Width + x) + 0] != initData ||
								Pixels[bpp * (y * Width + x) + 1] != initData ||
								Pixels[bpp * (y * Width + x) + 2] != initData) {
								stop = true;
								break;
							}
						}
						else if (bpp == 4) {
							if (Pixels[bpp * (y * Width + x) + 3] != initData) {
								stop = true;
								break;
							}
						}
					}

					if (stop) {
						if (direction == TrimDirection.Bottom)
							toRemove = Height - y - 1;
						else
							toRemove = y;
						break;
					}

					if (direction == TrimDirection.Top)
						y++;
					else
						y--;
				}
			}

			return toRemove;
		}

		public static GrfImage Empty(GrfImageType type) {
			int bpp = -1;

			switch (type) {
				case GrfImageType.Bgr24:
					bpp = 3;
					break;
				case GrfImageType.Bgr32:
				case GrfImageType.Bgra32:
					bpp = 4;
					break;
				case GrfImageType.Indexed8:
					bpp = 1;
					break;
			}

			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] data = new byte[bpp];

			if (type == GrfImageType.Indexed8) {
				byte[] palette = new byte[1024];
				return new GrfImage(ref data, 1, 1, type, ref palette);
			}
			
			return new GrfImage(ref data, 1, 1, type);
		}

		public enum SprTransparencyMode {
			Normal,
			PixelIndexZero,
			PixelIndexPink,
			FirstPixel,
			LastPixel,
		}

		public enum SprConvertMode {
			Original,
			BestMatch,
			MergeRgb,
			MergeLab,
			MergeOld,
			Bgra32,
		}

		public static GrfImage SprConvert(Spr spr, GrfImage imageSource, bool useDithering, SprTransparencyMode transparency, SprConvertMode mode) {
			// ?? What if palette is not set...?
			var _originalPalette = spr.Palette.BytePalette;

			if (mode == SprConvertMode.Original) {
				if (imageSource.GrfImageType != GrfImageType.Indexed8)
					return null;

				var imageCopy = imageSource.Copy();
				imageCopy.SetPalette(_originalPalette);
				imageCopy.MakeFirstPixelTransparent();
				return imageCopy;
			}

			var _unusedIndexes = spr.GetUnusedPaletteIndexes();
			_unusedIndexes.Remove(0);

			GrfColor transparencyColor = null;

			// Apply transparency mode
			switch (transparency) {
				case SprTransparencyMode.Normal:
					break;
				case SprTransparencyMode.PixelIndexZero:
					byte r = imageSource.GrfImageType == GrfImageType.Indexed8 ? imageSource.Palette[0] : _originalPalette[0];
					byte g = imageSource.GrfImageType == GrfImageType.Indexed8 ? imageSource.Palette[1] : _originalPalette[1];
					byte b = imageSource.GrfImageType == GrfImageType.Indexed8 ? imageSource.Palette[2] : _originalPalette[2];
					byte a = imageSource.GrfImageType == GrfImageType.Indexed8 ? imageSource.Palette[3] : _originalPalette[3];

					transparencyColor = new GrfColor(a, r, g, b);
					break;
				case SprTransparencyMode.PixelIndexPink:
					transparencyColor = new GrfColor(255, 255, 0, 255);
					break;
				case SprTransparencyMode.FirstPixel:
					transparencyColor = imageSource.GetColor(0);
					break;
				case SprTransparencyMode.LastPixel:
					transparencyColor = imageSource.GetColor(-1);
					break;
			}

			switch (mode) {
				case SprConvertMode.Bgra32:
					GrfImage bgra32 = imageSource.Copy();
					bgra32.Convert(new Bgra32FormatConverter());

					if (transparencyColor != null)
						_sprMakeTransparent(bgra32, transparencyColor.B, transparencyColor.G, transparencyColor.R, transparencyColor.A);

					return bgra32;
				case SprConvertMode.BestMatch:
					GrfImage match = imageSource.Copy();
					Indexed8FormatConverter conv = new Indexed8FormatConverter();

					// PixelIndexZero is done automatically
					if (transparencyColor != null && transparency != SprTransparencyMode.PixelIndexZero) {
						match.MakeColorTransparent(transparencyColor);
					}

					if (useDithering) {
						conv.Options |= Indexed8FormatConverter.PaletteOptions.UseDithering | Indexed8FormatConverter.PaletteOptions.UseExistingPalette;
					}

					conv.ExistingPalette = _originalPalette;
					conv.BackgroundColor = GrfColor.White;

					match.Convert(conv);

					switch (transparency) {
						case SprTransparencyMode.PixelIndexZero:
							match = _sprGetImageUsingPixelZero(_originalPalette, imageSource, match);
							break;
						case SprTransparencyMode.PixelIndexPink:
						case SprTransparencyMode.FirstPixel:
						case SprTransparencyMode.LastPixel:
							match = _sprGetImageUsingPixel(_originalPalette, match, transparencyColor);
							break;
					}
					
					match.MakeFirstPixelTransparent();
					return match;
				case SprConvertMode.MergeOld:
				case SprConvertMode.MergeRgb:
				case SprConvertMode.MergeLab:
					GrfImage merge = imageSource.Copy();

					if (transparencyColor != null && transparency != SprTransparencyMode.PixelIndexZero) {
						merge.MakeColorTransparent(transparencyColor);
					}

					merge = SpriteImageToIndexed8(spr, merge, useDithering, mode);

					switch (transparency) {
						case SprTransparencyMode.PixelIndexZero:
							merge = _sprGetImageUsingPixelZero(_originalPalette, imageSource, merge);
							break;
						case SprTransparencyMode.PixelIndexPink:
						case SprTransparencyMode.FirstPixel:
						case SprTransparencyMode.LastPixel:
							merge = _sprGetImageUsingPixel(_originalPalette, merge, transparencyColor);
							break;
					}

					merge.MakeFirstPixelTransparent();
					return merge;
			}

			return null;
		}

		private static GrfImage _sprGetImageUsingPixelZero(byte[] originalPalette, GrfImage imageSource, GrfImage image) {
			if (image != null && image.GrfImageType == GrfImageType.Indexed8) {
				GrfImage im = image.Copy();

				byte[] palette = im.Palette;
				Buffer.BlockCopy(originalPalette, 0, palette, 0, 4);

				if (imageSource.GrfImageType == GrfImageType.Indexed8) {
					if (imageSource.Pixels.Any(p => p == 0)) {
						for (int i = 0; i < im.Pixels.Length; i++) {
							if (imageSource.Pixels[i] == 0) {
								im.Pixels[i] = 0;
							}
						}
					}
				}

				return im;
			}

			return null;
		}

		private static GrfImage _sprGetImageUsingPixel(byte[] originalPalette, GrfImage image, GrfColor color) {
			if (image != null && image.GrfImageType == GrfImageType.Indexed8) {
				GrfImage im = image.Copy();

				List<byte> toChange = new List<byte>();

				for (int i = 0; i < 256; i++) {
					if (image.Palette[4 * i + 0] == color.R &&
						image.Palette[4 * i + 1] == color.G &&
						image.Palette[4 * i + 2] == color.B) {
						toChange.Add((byte)i);
					}
				}

				byte[] palette = im.Palette;
				Buffer.BlockCopy(originalPalette, 0, palette, 0, 4);

				for (int i = 0; i < im.Pixels.Length; i++) {
					if (toChange.Contains(im.Pixels[i])) {
						im.Pixels[i] = 0;
					}
				}

				return im;
			}

			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void _sprMakeTransparent(GrfImage image, byte b, byte g, byte r, byte a) {
			unsafe {
				fixed (byte* pBase = image.Pixels) {
					byte* p = pBase;
					byte* pEnd = pBase + image.Pixels.Length;

					while (p < pEnd) {
						if (b == p[0] && g == p[1] && r == p[2] && a == p[3]) {
							p[3] = 0;
						}

						p += 4;
					}
				}
			}
		}

		public static GrfImage SpriteImageToIndexed8(Spr spr, GrfImage imageSource, bool useDithering) {
			return SpriteImageToIndexed8(spr, imageSource, useDithering, SprConvertMode.MergeOld);
		}

		public static GrfImage SpriteImageToIndexed8(Spr spr, GrfImage imageSource, bool useDithering, SprConvertMode mode) {
			GrfImage im = imageSource.Copy();
			byte[] originalPalette = spr.Palette.BytePalette;
			HashSet<byte> unusedIndexesHS = spr.GetUnusedPaletteIndexes();
			unusedIndexesHS.Remove(0);
			List<byte> unusedIndexes = new List<byte>(unusedIndexesHS);
			byte[] newPalette = new byte[1024];

			Buffer.BlockCopy(originalPalette, 0, newPalette, 0, 1024);

			int numberOfAvailableColors = unusedIndexes.Count;

			if (imageSource.GrfImageType == GrfImageType.Indexed8) {
				List<byte> newImageUsedIndexes = new List<byte>();
				for (int i = 0; i < 256; i++) {
					if (Array.IndexOf(im.Pixels, (byte)i) > -1) {
						newImageUsedIndexes.Add((byte)i);
					}
				}

				if (newImageUsedIndexes.Count < numberOfAvailableColors) {
					for (int usedIndex = 0; usedIndex < newImageUsedIndexes.Count; usedIndex++) {
						byte index = newImageUsedIndexes[usedIndex];

						for (int i = 0; i < 256; i++) {
							if (
								im.Palette[4 * index + 0] == originalPalette[4 * i + 0] &&
								im.Palette[4 * index + 1] == originalPalette[4 * i + 1] &&
								im.Palette[4 * index + 2] == originalPalette[4 * i + 2]) {
								newImageUsedIndexes.Remove(index);
								usedIndex--;

								if (unusedIndexes.Contains(index))
									unusedIndexes.Remove(index);

								break;
							}
						}
					}
				}
				else {
					var colors = newImageUsedIndexes.Select(t => new Utilities.Extension.Tuple<int, byte>((im.Palette[4 * t + 0]) << 16 | (im.Palette[4 * t + 1]) << 8 | (im.Palette[4 * t + 2]), t)).ToList();
					colors = colors.OrderBy(p => p.Item1).ToList();

					List<byte> newImageTempUsedIndexes = new List<byte>();
					newImageTempUsedIndexes.Add(colors[0].Item2);
					newImageTempUsedIndexes.Add(colors[colors.Count - 1].Item2);

					int numberToAdd = unusedIndexes.Count - 2;
					int numberOfItems = newImageUsedIndexes.Count - 2;

					for (int i = 0; i < numberToAdd; i++) {
						newImageTempUsedIndexes.Add(colors[(int)(((float)i / numberToAdd) * numberOfItems)].Item2);
					}

					newImageUsedIndexes = new List<byte>(newImageTempUsedIndexes);
				}

				for (int i = 0; i < newImageUsedIndexes.Count; i++) {
					if (unusedIndexes.Count <= 0) break;

					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = im.Palette[4 * newImageUsedIndexes[i] + 0];
					newPalette[4 * unused + 1] = im.Palette[4 * newImageUsedIndexes[i] + 1];
					newPalette[4 * unused + 2] = im.Palette[4 * newImageUsedIndexes[i] + 2];
					newPalette[4 * unused + 3] = im.Palette[4 * newImageUsedIndexes[i] + 3];
					unusedIndexes.RemoveAt(0);
				}
			}
			else if (mode == SprConvertMode.MergeOld) {
				HashSet<int> colors = new HashSet<int>();

				// Set white background at the same time
				unsafe {
					fixed (byte* pBase = im.Pixels) {
						byte* p = pBase;
						byte* pEnd = pBase + im.Pixels.Length;

						while (p < pEnd) {
							byte a = p[3];
							int key = 0;

							if (a == 0) {
								p += 4;
								continue;
							}

							if (a != 255) {
								int m = (255 - a) * 255;
								p[0] = (byte)((m + a * p[0]) / 255);
								p[1] = (byte)((m + a * p[1]) / 255);
								p[2] = (byte)((m + a * p[2]) / 255);
								p[3] = 255;
							}

							key = (p[2] << 16) | (p[1] << 8) | p[0];
							colors.Add(key);
							p += 4;
						}
					}
				}

				int color;
				for (int i = 0; i < 256; i++) {
					color = originalPalette[4 * i + 0] << 16 | originalPalette[4 * i + 1] << 8 | originalPalette[4 * i + 2];
					if (!unusedIndexesHS.Contains((byte)i) && colors.Contains(color)) {
						colors.Remove(color);
					}
				}

				int numberOfColorsToAdd = numberOfAvailableColors;
				numberOfColorsToAdd = colors.Count < numberOfColorsToAdd ? colors.Count : numberOfColorsToAdd;

				List<int> colorsList = colors.ToList();
				colorsList.Sort();

				for (int i = 0; i < numberOfColorsToAdd - 1; i++) {
					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = (byte)((colorsList[(int)(i / (float)numberOfColorsToAdd * colorsList.Count)] & 0xFF0000) >> 16);
					newPalette[4 * unused + 1] = (byte)((colorsList[(int)(i / (float)numberOfColorsToAdd * colorsList.Count)] & 0x00FF00) >> 8);
					newPalette[4 * unused + 2] = (byte)((colorsList[(int)(i / (float)numberOfColorsToAdd * colorsList.Count)] & 0x0000FF));
					newPalette[4 * unused + 3] = 255;
					unusedIndexes.RemoveAt(0);
				}

				if (numberOfColorsToAdd > 0) {
					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = (byte)((colorsList[colorsList.Count - 1] & 0xFF0000) >> 16);
					newPalette[4 * unused + 1] = (byte)((colorsList[colorsList.Count - 1] & 0x00FF00) >> 8);
					newPalette[4 * unused + 2] = (byte)((colorsList[colorsList.Count - 1] & 0x0000FF));
					newPalette[4 * unused + 3] = 255;
					unusedIndexes.RemoveAt(0);
				}
			}
			else {
				int numberOfColorsToAdd = numberOfAvailableColors;

				var usedPaletteIndexes = spr.GetUsedPaletteIndexes();
				usedPaletteIndexes.Remove(0);

				// Do not include the transparent palette index (0)
				OctreeQuantizer quantizer = new OctreeQuantizer();
				quantizer.ColorMode = mode == SprConvertMode.MergeLab ? GrfColorMode.Lab : GrfColorMode.Rgb;
				quantizer.AddImage(im);

				// The quantizer doesn't know we already have colors, so add them to its blacklist
				HashSet<int> usedColors = new HashSet<int>();
				foreach (var idx in usedPaletteIndexes) {
					usedColors.Add(originalPalette[4 * idx + 0] << 16 | originalPalette[4 * idx + 1] << 8 | originalPalette[4 * idx + 2]);
				}
				quantizer.SetReservedColors(usedColors);

				var colors = quantizer.GeneratePaletteRgbInt(255);
				//colors.RemoveAt(0);	// Remove transparent (pink) color from generated palette
				//
				//// Use 255 instead of 256 because the transparent color needs space
				//colors = quantizer.RefinePalette(255, colors, usedColors);
				
				var colorsHash = new HashSet<int>(colors);
				
				// Remove duplicates
				foreach (var idx in usedPaletteIndexes) {
					colorsHash.Remove(originalPalette[4 * idx + 0] << 16 | originalPalette[4 * idx + 1] << 8 | originalPalette[4 * idx + 2]);
				}
				
				colors = colorsHash.ToList();
				numberOfColorsToAdd = colors.Count < numberOfColorsToAdd ? colors.Count : numberOfColorsToAdd;

				for (int i = 0; i < numberOfColorsToAdd; i++) {
					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = (byte)((colors[i] & 0xFF0000) >> 16);
					newPalette[4 * unused + 1] = (byte)((colors[i] & 0x00FF00) >> 8);
					newPalette[4 * unused + 2] = (byte)(colors[i] & 0x0000FF);
					newPalette[4 * unused + 3] = 255;
					unusedIndexes.RemoveAt(0);
				}
			}

			Indexed8FormatConverter conv = new Indexed8FormatConverter();
			conv.BackgroundColor = new GrfColor(255, 255, 0, 255);

			if (useDithering)
				conv.Options |= Indexed8FormatConverter.PaletteOptions.UseDithering;

			if (mode == SprConvertMode.MergeLab)
				conv.Options |= Indexed8FormatConverter.PaletteOptions.UseLabDistance;

			conv.ExistingPalette = newPalette;
			im.Convert(conv, null);
			return im;
		}

		public void ChangePinkToBlack(byte rT, byte gT, byte bT) {
			int bpp = GetBpp();

			if (bpp != 4)
				return;

			unsafe {
				fixed (byte* ptr = Pixels) {
					byte* pPixels = ptr;
					byte* pPixelsEnd = ptr + Pixels.Length;

					while (pPixels < pPixelsEnd) {
						if (pPixels[0] > rT && pPixels[1] < gT && pPixels[2] > bT) {
							pPixels[0] = 0;
							pPixels[1] = 0;
							pPixels[2] = 0;
							pPixels[3] = 0;
						}

						pPixels += bpp;
					}
				}
			}
		}

		public void DitherAndChangePinkToBlack(byte rT, byte gT, byte bT, int ditherDividerShift, float ditherMultiplier) {
			int bpp = GetBpp();

			if (bpp != 4)
				return;

			unsafe {
				fixed (byte* ptr = Pixels) {
					for (int i = 0; i < Pixels.Length; i += bpp) {
						if (ptr[i + 0] > rT &&
							ptr[i + 1] < gT &&
							ptr[i + 2] > bT) {
							ptr[i + 0] = 0;
							ptr[i + 1] = 0;
							ptr[i + 2] = 0;
							ptr[i + 3] = 0;
						}
						else {
							ptr[i + 0] = (byte)Math.Min(255, (ptr[i + 0] >> ditherDividerShift) * ditherMultiplier);
							ptr[i + 1] = (byte)Math.Min(255, (ptr[i + 1] >> ditherDividerShift) * ditherMultiplier);
							ptr[i + 2] = (byte)Math.Min(255, (ptr[i + 2] >> ditherDividerShift) * ditherMultiplier);
							ptr[i + 3] = (byte)Math.Min(255, (ptr[i + 3] >> ditherDividerShift) * ditherMultiplier);
						}
					}
				}
			}
		}
	}
}