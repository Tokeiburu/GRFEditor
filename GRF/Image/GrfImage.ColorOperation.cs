using GRF.ContainerFormat;
using System;
using Utilities;

namespace GRF.Image {
	public partial class GrfImage {
		/// <summary>
		/// Sets the pixel transparent at the specified coordinate.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public void SetPixelTransparent(int x, int y) {
			if (x < 0 || y < 0 || x >= Width || y >= Height)
				return;

			switch (GrfImageType) {
				case GrfImageType.Indexed8:
					Pixels[Width * y + x] = 0;
					break;
				case GrfImageType.Bgra32:
					Pixels[(Width * y + x) * 4 + 3] = 0;
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("SetPixelTransparent");
			}

			InvalidateHash();
		}

		/// <summary>
		/// Multiplies the image with the color specified for each channel, R = R * C.
		/// </summary>
		/// <param name="color">The color.</param>
		public unsafe void Multiply(in GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (color.Equals(GrfColors.White)) {
				return;
			}

			int fR = color.R;
			int fG = color.G;
			int fB = color.B;
			int fA = color.A;

			switch(bpp) {
				case 1:
					fixed (byte* pDst = Palette) {
						for (int i = 0; i < Palette.Length; i += 4) {
							pDst[i + 0] = (byte)((pDst[i + 0] * fR) >> 8);
							pDst[i + 1] = (byte)((pDst[i + 1] * fG) >> 8);
							pDst[i + 2] = (byte)((pDst[i + 2] * fB) >> 8);
							pDst[i + 3] = (byte)((pDst[i + 3] * fA) >> 8);
						}
					}
					break;
				case 3:
				case 4:
					byte[] lutB = new byte[256];
					byte[] lutG = new byte[256];
					byte[] lutR = new byte[256];
					byte[] lutA = new byte[256];

					for (int i = 0; i < 256; i++) {
						lutB[i] = (byte)((i * fB) >> 8);
						lutG[i] = (byte)((i * fG) >> 8);
						lutR[i] = (byte)((i * fR) >> 8);
						lutA[i] = (byte)((i * fA) >> 8);
					}

					if (bpp == 3) {
						fixed (byte* lb = lutB, lg = lutG, lr = lutR)
						fixed (byte* pDst = Pixels) {
							for (int i = 0; i < Pixels.Length; i += 3) {
								pDst[i + 0] = lb[pDst[i + 0]];
								pDst[i + 1] = lg[pDst[i + 1]];
								pDst[i + 2] = lr[pDst[i + 2]];
							}
						}
					}
					else {
						fixed (byte* lb = lutB, lg = lutG, lr = lutR, la = lutA)
						fixed (byte* pDst = Pixels) {
							uint* p = (uint*)pDst;
							uint* pEnd = (uint*)(pDst + Pixels.Length);

							while (p < pEnd) {
								uint px = *p;

								*p++ =
									((uint)la[(px >> 24) & 0xFF] << 24) |
									((uint)lr[(px >> 16) & 0xFF] << 16) |
									((uint)lg[(px >> 8) & 0xFF] << 8) |
									 (uint)lb[px & 0xFF];

							}
						}
					}
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("Multiply");
			}
			
			InvalidateHash();
		}

		/// <summary>
		/// Add the image with the color specified for each channel, R = R + C.
		/// </summary>
		/// <param name="color">The color.</param>
		public unsafe void Add(in GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (color.Equals(GrfColors.Transparent)) {
				return;
			}

			switch (bpp) {
				case 1:
					fixed (byte* pDst = Palette) {
						for (int i = 0; i < Palette.Length; i += 4) {
							pDst[i + 0] = Methods.ClampToColorByte(pDst[i + 0] + color.R);
							pDst[i + 1] = Methods.ClampToColorByte(pDst[i + 1] + color.G);
							pDst[i + 2] = Methods.ClampToColorByte(pDst[i + 2] + color.B);
						}
					}
					break;
				case 3:
				case 4:
					byte[] lutB = new byte[256];
					byte[] lutG = new byte[256];
					byte[] lutR = new byte[256];

					for (int i = 0; i < 256; i++) {
						lutB[i] = Methods.ClampToColorByte(i + color.B);
						lutG[i] = Methods.ClampToColorByte(i + color.G);
						lutR[i] = Methods.ClampToColorByte(i + color.R);
					}

					fixed (byte* lb = lutB, lg = lutG, lr = lutR)
					fixed (byte* pDst = Pixels) {
						for (int i = 0; i < Pixels.Length; i += bpp) {
							pDst[i + 0] = lb[pDst[i + 0]];
							pDst[i + 1] = lg[pDst[i + 1]];
							pDst[i + 2] = lr[pDst[i + 2]];
						}
					}
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("Add");
			}

			InvalidateHash();
		}

		/// <summary>
		/// Substract the image with the color specified for each channel, R = R - C.
		/// </summary>
		/// <param name="color">The color.</param>
		public unsafe void Substract(in GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (color.Equals(GrfColors.Transparent)) {
				return;
			}

			switch (bpp) {
				case 1:
					fixed (byte* pDst = Palette) {
						for (int i = 0; i < Palette.Length; i += 4) {
							pDst[i + 0] = Methods.ClampToColorByte(pDst[i + 0] - color.R);
							pDst[i + 1] = Methods.ClampToColorByte(pDst[i + 1] - color.G);
							pDst[i + 2] = Methods.ClampToColorByte(pDst[i + 2] - color.B);
						}
					}
					break;
				case 3:
				case 4:
					byte[] lutB = new byte[256];
					byte[] lutG = new byte[256];
					byte[] lutR = new byte[256];

					for (int i = 0; i < 256; i++) {
						lutB[i] = Methods.ClampToColorByte(i - color.B);
						lutG[i] = Methods.ClampToColorByte(i - color.G);
						lutR[i] = Methods.ClampToColorByte(i - color.R);
					}

					fixed (byte* lb = lutB, lg = lutG, lr = lutR)
					fixed (byte* pDst = Pixels) {
						for (int i = 0; i < Pixels.Length; i += bpp) {
							pDst[i + 0] = lb[pDst[i + 0]];
							pDst[i + 1] = lg[pDst[i + 1]];
							pDst[i + 2] = lr[pDst[i + 2]];
						}
					}
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("Substract");
			}

			InvalidateHash();
		}

		/// <summary>
		/// Applies the color factor for each color component.
		/// </summary>
		/// <param name="fact">The color factor to apply.</param>
		public void Multiply(float fact) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			if (fact > 1) {
				if (fact > 2)
					fact = 2;

				byte add = (byte)(255 * (fact - 1));

				Add(new GrfColor(255, add, add, add));
			}
			else {
				byte add = (byte)(255 * fact);

				Multiply(new GrfColor(255, add, add, add));
			}

			InvalidateHash();
		}

		/// <summary>
		/// Makes colors close to pink transparent.
		/// </summary>
		public unsafe void MakePinkShadeTransparent() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			switch (bpp) {
				case 1:
					fixed (byte* pDst = Palette) {
						for (int i = 0; i < Palette.Length; i += 4) {
							if (pDst[i + 0] > 250 && pDst[i + 1] < 5 && pDst[i + 2] > 250) {
								pDst[i + 0] = 255;
								pDst[i + 1] = 0;
								pDst[i + 2] = 255;
								pDst[i + 3] = 0;
							}
						}
					}
					break;
				case 3:
					TransparentPixels = new bool[Width * Height];

					fixed (bool* pTransPixel = TransparentPixels)
					fixed (byte* pDst = Pixels) {
						for (int i = 0, k = 0; i < Pixels.Length; i += 3, k++) {
							if (pDst[i + 0] > 250 && pDst[i + 1] < 5 && pDst[i + 2] > 250) {
								pDst[i + 0] = 0;
								pDst[i + 1] = 0;
								pDst[i + 2] = 0;
								TransparentPixels[k] = true;
							}
						}
					}
					break;
				case 4:
					fixed (byte* pDst = Pixels) {
						for (int i = 0; i < Pixels.Length; i += 4) {
							if (pDst[i + 0] > 250 && pDst[i + 1] < 5 && pDst[i + 2] > 250) {
								*(int*)(pDst + i) = 0;
							}
						}
					}
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("MakePinkShadeTransparent");
			}

			InvalidateHash();
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
			else {
				throw GrfExceptions.__UnsupportedImageFormatMethod.Create("MakeFirstPixelTransparent");
			}

			InvalidateHash();
		}

		/// <summary>
		/// Makes a specified color transparent.
		/// </summary>
		/// <param name="color">The color that will be made transparent.</param>
		public unsafe void MakeColorTransparent(in GrfColor color) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();

			switch (bpp) {
				case 1:
					fixed (byte* pDst = Palette) {
						for (int i = 0; i < Palette.Length; i += 4) {
							if (pDst[i + 0] == color.R && pDst[i + 1] == color.G && pDst[i + 2] == color.B)
								pDst[3] = 0;
						}
					}
					break;
				case 3:
					TransparentPixels = new bool[Width * Height];

					fixed (byte* pDst = Pixels) {
						for (int i = 0, k = 0; i < Palette.Length; i += 3, k++) {
							if (pDst[i + 0] == color.B && pDst[i + 1] == color.G && pDst[i + 2] == color.R) {
								pDst[i + 0] = 0;
								pDst[i + 1] = 0;
								pDst[i + 2] = 0;
								TransparentPixels[k] = true;
							}
						}
					}
					break;
				case 4:
					fixed (byte* pDstBase = Pixels) {
						uint* pDst = (uint*)pDstBase;
						uint* pDstEnd = pDst + Pixels.Length / 4;
						uint targetColor = color.ToArgbInt32() & 0x00FFFFFF;

						while (pDst < pDstEnd) {
							if ((*pDst & 0x00FFFFFF) == targetColor) {
								*pDst = 0;
							}

							pDst++;
						}
					}
					break;
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("MakeColorTransparent");
			}

			InvalidateHash();
		}

		/// <summary>
		/// Makes a specified color transparent.
		/// </summary>
		public unsafe void Grayscale(GrayscaleMode mode = GrayscaleMode.MaxValue) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();

			fixed (byte* pDstBase = (bpp == 1 ? Palette : Pixels)) {
				byte* pDst = pDstBase;
				int length = (bpp == 1 ? Palette.Length : Pixels.Length);
				int nBpp = bpp == 1 ? 4 : bpp;

				switch (mode) {
					case GrayscaleMode.MaxValue:
						for (int i = 0; i < length; i += nBpp, pDst += nBpp) {
							byte value = Math.Max(Math.Max(pDst[0], pDst[1]), pDst[2]);

							if (nBpp == 3) {
								pDst[0] = value;
								pDst[1] = value;
								pDst[2] = value;
							}
							else {
								*(int*)pDst = (pDst[3] << 24) | (value << 16) | (value << 8) | value;
							}
						}
						break;
					case GrayscaleMode.Average:
						for (int i = 0; i < length; i += nBpp, pDst += nBpp) {
							byte value = (byte)((pDst[0] + pDst[1] + pDst[2]) / 3);

							if (nBpp == 3) {
								pDst[0] = value;
								pDst[1] = value;
								pDst[2] = value;
							}
							else {
								*(int*)pDst = (pDst[3] << 24) | (value << 16) | (value << 8) | value;
							}
						}
						break;
					case GrayscaleMode.Lightness:
						for (int i = 0; i < length; i += nBpp, pDst += nBpp) {
							GrfColor color = GrfColor.FromByteArray(pDst, 0, GrfImageType);
							HslColor hslColor = color.Hsl;

							byte value = Methods.ClampToColorByte(hslColor.Lightness * 255);

							if (nBpp == 3) {
								pDst[0] = value;
								pDst[1] = value;
								pDst[2] = value;
							}
							else {
								*(int*)pDst = (pDst[3] << 24) | (value << 16) | (value << 8) | value;
							}
						}
						break;
					case GrayscaleMode.Luminosity:
						for (int i = 0; i < length; i += nBpp, pDst += nBpp) {
							GrfColor color = GrfColor.FromByteArray(pDst, 0, GrfImageType);
							byte value = Methods.ClampToColorByte(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);

							if (nBpp == 3) {
								pDst[0] = value;
								pDst[1] = value;
								pDst[2] = value;
							}
							else {
								*(int*)pDst = (pDst[3] << 24) | (value << 16) | (value << 8) | value;
							}
						}

						break;
				}
			}

			InvalidateHash();
		}

		public unsafe void ChangeIntoWhite(int paletteIndex = -1) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();

			fixed (byte* pDstBase = Pixels) {
				byte* pDst = pDstBase;
				int length = Pixels.Length;

				switch (bpp) {
					case 1:
						if (paletteIndex < 0 || paletteIndex >= Palette.Length)
							throw new ArgumentException("Invalid value for paletteIndex, must be between 0 and 255.", "paletteIndex");

						byte bytePaletteIndex = (byte)paletteIndex;

						for (int i = 0; i < length; i++) {
							if (pDst[i] != 0)
								pDst[i] = bytePaletteIndex;
						}

						break;
					case 4:
						int[] lut = new int[256];
						
						for (int i = 0; i < lut.Length; i++) {
							lut[i] = ((byte)i << 24) | (0x00FFFFFF);
						}

						int* pDstInt = (int*)pDstBase;

						length = length / 4;

						for (int i = 0; i < length; i++) {
							pDstInt[i] = lut[(pDstInt[i] >> 24) & 0xFF];
						}

						break;
					default:
						throw GrfExceptions.__UnsupportedImageFormatMethod.Create("ChangingIntoWhite");
				}
			}
		}

		/// <summary>
		/// Sets the color of the palette directly.
		/// </summary>
		/// <param name="index256">The index.</param>
		/// <param name="color">The color.</param>
		public void SetPaletteColor(int index256, in GrfColor color) {
			int offset = index256 * 4;

			Palette[offset + 0] = color.R;
			Palette[offset + 1] = color.G;
			Palette[offset + 2] = color.B;
			Palette[offset + 3] = color.A;
			InvalidateHash();
		}

		/// <summary>
		/// Sets the color of the palette directly.
		/// </summary>
		/// <param name="index256">The index.</param>
		/// <returns>The color of the palette.</returns>
		public GrfColor GetPaletteColor(int index256) {
			return GrfColor.FromByteArray(Palette, index256 * 4, GrfImageType.Indexed8);
		}
	}
}
