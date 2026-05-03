using GRF.ContainerFormat;
using System;
using System.Linq;
using Utilities;

namespace GRF.Image {
	public partial class GrfImage {
		public byte GetPixelAlphaChannel(int x, int y) {
			if (x < 0 || y < 0 || x >= Width || y >= Height)
				return 0;

			switch(GrfImageType) {
				case GrfImageType.Indexed8:
					int pixel = Pixels[Width * y + x];

					if (pixel == 0)
						return 0;

					return Palette[4 * pixel + 3];
				case GrfImageType.Bgra32:
					return Pixels[(Width * y + x) * 4 + 3];
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("GetPixelAlphaChannel");
			}
		}

		public void SetColor(int x, int y, in GrfColor color) {
			int bpp = GetBpp();

			switch (bpp) {
				case 3:
				case 4:
					if (x < 0 || x >= Width || y < 0 || y >= Height)
						return;

					Buffer.BlockCopy(color.ToBgraBytes(), 0, Pixels, (y * Width + x) * bpp, bpp);
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("SetColor");
			}

			InvalidateHash();
		}

		public void SetColor(int x, int y, byte paletteIndex) {
			int bpp = GetBpp();

			switch (bpp) {
				case 1:
					if (x < 0 || x >= Width || y < 0 || y >= Height)
						return;

					Pixels[x + Width * y] = paletteIndex;
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("SetColor");
			}

			InvalidateHash();
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
					return GrfColor.FromByteArray(Palette, 4 * Pixels[Pixels.Length - 1], GrfImageType);

				return GrfColor.FromByteArray(Palette, 4 * Pixels[pixelIndex], GrfImageType);
			}

			if (pixelIndex == -1)
				return GrfColor.FromByteArray(Pixels, Pixels.Length - bpp, GrfImageType);

			return GrfColor.FromByteArray(Pixels, bpp * pixelIndex, GrfImageType);
		}

		/// <summary>
		/// Gets the color at the requested pixel index.
		/// </summary>
		/// <param name="x">Index pixel for width.</param>
		/// <param name="y">Index pixel for height.</param>
		/// <returns>The requested color</returns>
		public GrfColor GetColor(int x, int y) {
			return GetColor(y * Width + x);
		}

		/// <summary>
		/// Gets the color address at the requested pixel index.
		/// </summary>
		/// <param name="x">Index pixel for width.</param>
		/// <param name="y">Index pixel for height.</param>
		/// <returns>The requested color</returns>
		public unsafe byte* GetRawColor(int x, int y) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			int bpp = GetBpp();

			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (GrfImageType == GrfImageType.Indexed8) {
				fixed (byte* pPixels = Pixels)
				fixed (byte* pPalette = Palette) {
					return pPalette + 4 * pPixels[y * Width + x];
				}
			}

			fixed (byte* pPixels = Pixels) {
				return pPixels + bpp * (y * Width + x);
			}
		}

		/// <summary>
		/// Sets the color at the requested pixel index.
		/// </summary>
		/// <param name="pixelIndex">Index of the pixel.</param>
		/// <param name="color">The requested color.</param>
		public void SetColor(int pixelIndex, in GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			int bpp = GetBpp();

			if (GrfImageType == GrfImageType.Indexed8) {
				throw GrfExceptions.__UnsupportedImageFormat.Create();
			}

			Buffer.BlockCopy(color.ToBgraBytes(), 0, Pixels, bpp * pixelIndex, bpp);
			InvalidateHash();
		}

		/// <summary>
		/// Sets the color at the requested pixel index.
		/// </summary>
		/// <param name="pixelIndex">Index of the pixel.</param>
		/// <param name="paletteIndex">The requested color.</param>
		public void SetColor(int pixelIndex, byte paletteIndex) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			int bpp = GetBpp();

			if (GrfImageType != GrfImageType.Indexed8) {
				throw GrfExceptions.__UnsupportedImageFormat.Create();
			}

			Pixels[pixelIndex] = paletteIndex;
			InvalidateHash();
		}

		public (int Left, int Top, int Right, int Bottom) GetTrimLengths(byte tolerance = 0) {
			int l = 0, t = 0, r = 0, b = 0;

			bool IsColEmpty(int x) {
				for (int y = 0; y < Height; y++)
					if (GetPixelAlphaChannel(x, y) > tolerance) return false;
				return true;
			}

			bool IsRowEmpty(int y) {
				for (int x = l; x < Width - r; x++)
					if (GetPixelAlphaChannel(x, y) > tolerance) return false;
				return true;
			}

			while (l < Width && IsColEmpty(l)) l++;
			if (l == Width) return (Width, Height, 0, 0);
			while (r < Width - l - 1 && IsColEmpty(Width - 1 - r)) r++;
			while (t < Height && IsRowEmpty(t)) t++;
			while (b < Height - t - 1 && IsRowEmpty(Height - 1 - b)) b++;

			return (l, t, r, b);
		}

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
		/// outside the image, it will be filled with the fillColor.
		/// </summary>
		/// <param name="left">The start position on x.</param>
		/// <param name="top">The start position on y.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="fillColor">Color used to fill in padding pixels.</param>
		/// <returns></returns>
		public byte[] CopyPixelsUnrestricted(int left, int top, int width, int height, byte[] fillColor = null) {
			if (left == 0 && top == 0 && width == Width && height == Height)
				return Pixels;

			if (width * height <= 0 || width <= 0 || height <= 0)
				return new byte[0];

			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] pixels = new byte[bpp * width * height];
			int stride = width * bpp;
			int copyWidth = width;

			if (fillColor == null)
				fillColor = new byte[0];

			if (!fillColor.All(p => p == 0)) {
				byte[] strideLine = new byte[stride];

				for (int i = 0; i < strideLine.Length; i++) {
					strideLine[i] = fillColor[i % fillColor.Length];
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
			_setPixels(left, top, width, height, pixels, false);
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
			if (image.GrfImageType != GrfImageType) {
				image = image.Copy();
				image.Convert(GrfImageType);
			}

			_setPixels(left, top, width, height, image.Pixels, false);
		}

		private unsafe void _setPixels(int left, int top, int width, int height, byte[] pixels, bool blendLayers) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);

			if (left + width > Width ||
				top + height > Height)
				throw new Exception("Values go outside the image size.");

			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (width * height * bpp != pixels.Length)
				throw new Exception("The amount of pixels does not match the dimension provided with the image format type.");

			if (blendLayers && GrfImageType == GrfImageType.Bgra32) {
				fixed (byte* pSrcStart = pixels)
				fixed (byte* pDstStart = Pixels) {
					byte* pSrcRow = pSrcStart;
					byte* pDstRow = pDstStart + ((top * Width + left) * bpp);

					int dstStride = Width * bpp;
					int srcStride = width * bpp;

					for (int y = 0; y < height; y++) {
						byte* pSrc = pSrcRow;
						byte* pDst = pDstRow;

						for (int x = 0; x < width; x++) {
							byte a2 = pSrc[3];
							byte a1 = pDst[3];

							if (a1 == 0 || a2 == 255) {
								*(uint*)pDst = *(uint*)pSrc; // copy BGRA as 32-bit
							}
							else if (a2 > 0) {
								int b = (((pSrc[0] - pDst[0]) * a2) >> 8) + pDst[0];
								int g = (((pSrc[1] - pDst[1]) * a2) >> 8) + pDst[1];
								int r = (((pSrc[2] - pDst[2]) * a2) >> 8) + pDst[2];
								int a = a2 + ((a1 * (255 - a2)) >> 8);
								*(uint*)pDst = (uint)((a << 24) | (r << 16) | (g << 8) | b);
							}

							pSrc += bpp;
							pDst += bpp;
						}

						pSrcRow += srcStride;
						pDstRow += dstStride;
					}
				}
			}
			else if (blendLayers && GrfImageType == GrfImageType.Indexed8) {
				fixed (byte* pDst = Pixels)
				fixed (byte* pSrc = pixels) {
					byte* pSrcRow = pSrc;
					byte* pDstRow = pDst + (top * Width + left);

					int dstStride = Width;
					int srcStride = width;

					for (int y = 0; y < height; y++) {
						for (int x = 0; x < width; x++) {
							if (pSrcRow[x] != 0) {
								pDstRow[x] = pSrcRow[x];
							}
						}

						pSrcRow += srcStride;
						pDstRow += dstStride;
					}
				}
			}
			else {
				fixed (byte* pDst = Pixels)
				fixed (byte* pSrc = pixels) {
					int pSrcStride = width * bpp;
					int pDstStride = Width * bpp;
					byte* pSrcRow = pSrc;
					byte* pDstRow = pDst + (top * Width + left) * bpp;
					int srcStride = width * bpp;

					for (int y = 0; y < height; y++) {
						Buffer.MemoryCopy(pSrcRow, pDstRow, srcStride, srcStride);

						pDstRow += pDstStride;
						pSrcRow += pSrcStride;
					}
				}
			}

			InvalidateHash();
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		public void SetPixels(ref byte[] pixels) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Pixels = pixels;
			InvalidateHash();
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">Width of the new image data.</param>
		/// <param name="height">Height of the new image data.</param>
		public void SetPixels(ref byte[] pixels, int width, int height) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Pixels = pixels;
			Width = width;
			Height = height;
			InvalidateHash();
		}

		/// <summary>
		/// Sets pixels in the image.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		public void SetPixels(byte[] pixels) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Pixels = Methods.Copy(pixels);
			InvalidateHash();
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
		/// <param name="overridePalette">If set, uses this palette for the image.</param>
		public void SetPixelsUnrestricted(int left, int top, GrfImage image, bool blendLayers, byte[] overridePalette = null) {
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

			_setPixels(left, top, image.Width, image.Height, image.Pixels, blendLayers);
		}

		/// <summary>
		/// Determines whether the pixel at the position x/y is transparent or not.
		/// </summary>
		/// <param name="x">The x offset.</param>
		/// <param name="y">The y offset.</param>
		/// <returns>
		///   <c>true</c> if the pixel is transparent, <c>false</c> otherwise.
		/// </returns>
		public bool IsPixelTransparent(int x, int y) {
			if (x < 0 || y < 0 || x >= Width || y >= Height)
				return false;

			if (GrfImageType == GrfImageType.Indexed8) {
				return Pixels[Width * y + x] == 0;
			}
			else if (GrfImageType == GrfImageType.Bgra32) {
				return Pixels[(Width * y + x) * 4 + 3] == 0;
			}

			return false;
		}
	}
}
