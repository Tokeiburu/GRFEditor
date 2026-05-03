using GRF.ContainerFormat;
using GRF.Graphics;
using GRF.Image.Decoders;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Utilities;

namespace GRF.Image {
	public partial class GrfImage {
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
				case GrfScalingMode.LinearScaling:
					if (GrfImageType == GrfImageType.Indexed8) {
						Convert(GrfImageType.Bgra32);
					}

					_scaleLinear(x, y);
					break;
				case GrfScalingMode.NearestNeighbor:
				default:
					_scaleNearest(x, y);
					break;
			}
		}

		private void _scaleLinear(float scaleX, float scaleY) {
			int bpp = GetBpp();
			int newWidth = (int)Math.Round(Width * scaleX, MidpointRounding.AwayFromZero);
			int newHeight = (int)Math.Round(Height * scaleY, MidpointRounding.AwayFromZero);
			byte[] pixels = new byte[newWidth * newHeight * bpp];

			scaleX = 1 / scaleX;
			scaleY = 1 / scaleY;

			Parallel.For(0, newWidth, x => {
				int floorX = (int)Math.Floor(x * scaleX);
				int ceilX = floorX + 1;
				if (ceilX >= Width) ceilX = floorX;
				double fractionX = x * scaleX - floorX;
				double oneMinusX = 1.0 - fractionX;

				for (int y = 0; y < newHeight; y++) {
					int floorY = (int)Math.Floor(y * scaleY);
					int ceilY = floorY + 1;
					if (ceilY >= Height) ceilY = floorY;
					double fractionY = y * scaleY - floorY;
					double oneMinusY = 1.0 - fractionY;

					for (int k = 0; k < bpp; k++) {
						pixels[bpp * (y * newWidth + x) + k] =
							(byte)(oneMinusY * (byte)(oneMinusX *
								Pixels[(floorY * Width + floorX) * bpp + k] + fractionX *
								Pixels[(floorY * Width + ceilX) * bpp + k]) + fractionY * (byte)(oneMinusX *
								Pixels[(ceilY * Width + floorX) * bpp + k] + fractionX *
								Pixels[(ceilY * Width + ceilX) * bpp + k]));
					}
				}
			});

			Pixels = pixels;
			Width = newWidth;
			Height = newHeight;
			InvalidateHash();
		}

		private unsafe void _scaleNearest(float sx, float sy) {
			int bpp = GetBpp();
			int newWidth = (int)Math.Round(Width * sx, MidpointRounding.AwayFromZero);
			int newHeight = (int)Math.Round(Height * sy, MidpointRounding.AwayFromZero);
			byte[] pixels = new byte[newWidth * newHeight * bpp];

			long ratioX = (Width << 16) / newWidth;
			long ratioY = (Height << 16) / newHeight;

			fixed (byte* pSrc = Pixels)
			fixed (byte* pDst = pixels) {
				for (int y = 0; y < newHeight; y++) {
					int srcY = (int)((y * ratioY) >> 16);
					int srcRowOffset = srcY * Width * bpp;
					int destRowOffset = y * newWidth * bpp;

					for (int x = 0; x < newWidth; x++) {
						int srcX = (int)((x * ratioX) >> 16);
						int srcIdx = srcRowOffset + (srcX * bpp);
						int dstIdx = destRowOffset + (x * bpp);

						switch (bpp) {
							case 1:
								pDst[dstIdx] = pSrc[srcIdx];
								break;
							case 3:
								pDst[dstIdx + 0] = pSrc[srcIdx + 0];
								pDst[dstIdx + 1] = pSrc[srcIdx + 1];
								pDst[dstIdx + 2] = pSrc[srcIdx + 2];
								break;
							case 4:
								*(int*)(pDst + dstIdx) = *(int*)(pSrc + srcIdx);
								break;
						}
					}
				}
			}
				
			Pixels = pixels;
			Width = newWidth;
			Height = newHeight;
			InvalidateHash();
		}

		/// <summary>
		/// Rotates the image left or right by 90 degrees.
		/// </summary>
		/// <param name="dir">The direction.</param>
		public unsafe void Rotate(RotateDirection dir) {
			int bpp = GetBpp();
			byte[] pixels = new byte[Pixels.Length];

			fixed (byte* pSrc = Pixels)
			fixed (byte* pDst = pixels) {
				for (int y = 0; y < Height; y++) {
					for (int x = 0; x < Width; x++) {
						int srcIdx = dir == RotateDirection.Left ? bpp * (Width * y + x) : bpp * (Width * y + x);
						int dstIdx = dir == RotateDirection.Left ? bpp * (Width * (-x + Width - 1) + y) : bpp * (Width * x - y + Height - 1);

						switch (bpp) {
							case 1:
								pDst[dstIdx] = pSrc[srcIdx];
								break;
							case 3:
								pDst[dstIdx + 0] = pSrc[srcIdx + 0];
								pDst[dstIdx + 1] = pSrc[srcIdx + 1];
								pDst[dstIdx + 2] = pSrc[srcIdx + 2];
								break;
							case 4:
								*(int*)(pDst + dstIdx) = *(int*)(pSrc + srcIdx);
								break;
						}
					}
				}
			}

			Pixels = pixels;
			var temp = Height;
			Height = Width;
			Width = temp;
			InvalidateHash();
		}

		/// <summary>
		/// Flips the image in the specified direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		public unsafe void Flip(FlipDirection direction) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] pixels = new byte[Pixels.Length];

			fixed (byte* pSrc = Pixels)
			fixed (byte* pDst = pixels) {
				if (direction == FlipDirection.Horizontal) {
					for (int y = 0; y < Height; y++) {
						for (int x = 0; x < Width; x++) {
							int srcIdx = (Width * y + (Width - x - 1)) * bpp;
							int dstIdx = (Width * y + x) * bpp;

							switch (bpp) {
								case 1:
									pDst[dstIdx] = pSrc[srcIdx];
									break;
								case 3:
									pDst[dstIdx + 0] = pSrc[srcIdx + 0];
									pDst[dstIdx + 1] = pSrc[srcIdx + 1];
									pDst[dstIdx + 2] = pSrc[srcIdx + 2];
									break;
								case 4:
									*(int*)(pDst + dstIdx) = *(int*)(pSrc + srcIdx);
									break;
							}
						}
					}
				}
				else {
					int stride = Width * bpp;
					for (int y = 0; y < Height; y++) {
						Buffer.BlockCopy(Pixels, y * stride, pixels, (Height - y - 1) * stride, stride);
					}
				}
			}

			Pixels = pixels;
			InvalidateHash();
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

			byte[] pixels = CopyPixelsUnrestricted(left, top, width, height);

			Pixels = pixels;
			Width = Math.Max(0, width);
			Height = Math.Max(0, height);
			InvalidateHash();
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
		/// Draws a line in the image from point 1 to point 2.
		/// </summary>
		/// <param name="x1">The x1.</param>
		/// <param name="y1">The y1.</param>
		/// <param name="x2">The x2.</param>
		/// <param name="y2">The y2.</param>
		/// <param name="color">The color.</param>
		public void DrawLine(int x1, int y1, int x2, int y2, in GrfColor color) {
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("x1", x1);
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("y1", y1);
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("y2", y2);
			GrfExceptions.IfLtZeroThrowInvalidImagePosition("x2", x2);

			int maxX = Math.Max(x1, x2);
			int maxY = Math.Max(y1, y2);
			int bpp = GetBpp();

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

			int length = (int)Math.Ceiling(diff.Length);
			diff = TkVector2.Normalize(diff);

			for (int i = 0; i < length; i++) {
				int x = x1 + (int)(diff.X * i);
				int y = x1 + (int)(diff.X * i);

				if (x >= Width) continue;
				if (y >= Height) continue;
				if (x < 0 || y < 0) continue;
				SetColor(x, y, color);
			}

			InvalidateHash();
		}

		/// <summary>
		/// Trims this image by removing transparent pixels.
		/// </summary>
		public void Trim() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			var trimLengths = GetTrimLengths();

			Crop(trimLengths.Left, trimLengths.Top, trimLengths.Right, trimLengths.Bottom);
		}

		/// <summary>
		/// Creates the a displacement map used for modifying an image. This tool is similar to "Liquify".
		/// </summary>
		/// <returns></returns>
		public WarpField CreateWarpField() {
			return new WarpField(this.Width, this.Height);
		}

		/// <summary>
		/// Applies the warp map to the image.
		/// </summary>
		/// <param name="field">The warp map.</param>
		public unsafe void ApplyWarpField(WarpField field) {
			GrfImage result = this;
			GrfImage source = this.Clone();
			Indexed8FormatConverter converter = new Indexed8FormatConverter();
			byte[] palette = source.Palette ?? new byte[1024];
			converter.ExistingPalette = palette;
			ConcurrentDictionary<int, int> matches = new ConcurrentDictionary<int, int>();

			fixed (byte* pPaletteBase = palette) {
				byte* pPalette = pPaletteBase;

				Parallel.For(0, source.Height * source.Width, index => {
					int x = index % source.Width;
					int y = index / source.Width;

					ref TkVector2 d = ref field.GetSafe(x, y);

					if (d.X == 0 && d.Y == 0)
						return;

					float sampleX = x - d.X;
					float sampleY = y - d.Y;

					if (field.UseClosestNearbyPixel) {
						int x0 = Methods.Clamp((int)Math.Round(sampleX), 0, source.Width - 1);
						int y0 = Methods.Clamp((int)Math.Round(sampleY), 0, source.Height - 1);

						if (result.GrfImageType == GrfImageType.Indexed8)
							result.SetColor(x, y, source.Pixels[y0 * source.Width + x0]);
						else
							result.SetColor(x, y, source.GetColor(x0, y0));
					}
					else {
						GrfColor c = SampleBilinear(source, sampleX, sampleY);

						if (result.GrfImageType == GrfImageType.Indexed8) {
							// ?? Test to see which feels best
							if (c.A < field.AlphaCutoff) {
								result.SetColor(x, y, 0);
							}
							else {
								var idx = converter.FindClosetMatch(matches, c.R, c.G, c.B, pPalette, 1);
								result.SetColor(x, y, idx);
							}
						}
						else {
							result.SetColor(x, y, c);
						}
					}
				});
			}
		}

		private static GrfColor SampleBilinear(GrfImage img, float x, float y) {
			int x0 = (int)Math.Floor(x);
			int y0 = (int)Math.Floor(y);
			int x1 = x0 + 1;
			int y1 = y0 + 1;

			float tx = x - x0;
			float ty = y - y0;

			x0 = Methods.Clamp(x0, 0, img.Width - 1);
			y0 = Methods.Clamp(y0, 0, img.Height - 1);
			x1 = Methods.Clamp(x1, 0, img.Width - 1);
			y1 = Methods.Clamp(y1, 0, img.Height - 1);

			GrfColor c00 = img.GetColor(x0, y0);
			GrfColor c10 = img.GetColor(x1, y0);
			GrfColor c01 = img.GetColor(x0, y1);
			GrfColor c11 = img.GetColor(x1, y1);

			return GrfColor.Lerp(
				GrfColor.Lerp(c00, c10, tx),
				GrfColor.Lerp(c01, c11, tx),
				ty
			);
		}
	}
}
