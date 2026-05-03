using GRF.FileFormats.SprFormat;
using GRF.Image.Decoders;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace GRF.Image {
	public partial class GrfImage {
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
			byte[] originalPalette;

			if (spr.Palette == null || spr.Palette.BytePalette == null) {
				originalPalette = new byte[1024];
				originalPalette[0] = 255;
				originalPalette[1] = 0;
				originalPalette[2] = 255;
				originalPalette[3] = 255;
			}
			else {
				originalPalette = spr.Palette.BytePalette;
			}

			if (mode == SprConvertMode.Original) {
				if (imageSource.GrfImageType != GrfImageType.Indexed8)
					return null;

				var imageCopy = imageSource.Copy();
				imageCopy.SetPalette(Methods.Copy(originalPalette));
				imageCopy.MakeFirstPixelTransparent();
				return imageCopy;
			}

			var _unusedIndexes = spr.GetUnusedPaletteIndexes();
			_unusedIndexes.Remove(0);

			GrfColor? transparencyColor = null;

			// Apply transparency mode
			switch (transparency) {
				case SprTransparencyMode.Normal:
					break;
				case SprTransparencyMode.PixelIndexZero:
					transparencyColor = GrfColor.FromByteArray(imageSource.GrfImageType == GrfImageType.Indexed8 ? imageSource.Palette : originalPalette, 0, GrfImageType.Indexed8);
					break;
				case SprTransparencyMode.PixelIndexPink:
					transparencyColor = GrfColors.Pink;
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
						bgra32.MakeColorTransparent(transparencyColor.Value);

					return bgra32;
				case SprConvertMode.BestMatch:
					Z.Start(2003);
					GrfImage match = imageSource.Copy();
					Indexed8FormatConverter conv = new Indexed8FormatConverter();
					Z.Stop(2003);

					Z.Start(2004);
					// PixelIndexZero is done automatically
					if (transparencyColor != null && transparency != SprTransparencyMode.PixelIndexZero) {
						match.MakeColorTransparent(transparencyColor.Value);
					}
					Z.Stop(2004);

					if (useDithering) {
						conv.Options |= Indexed8FormatConverter.PaletteOptions.UseDithering | Indexed8FormatConverter.PaletteOptions.UseExistingPalette;
					}

					conv.ExistingPalette = originalPalette;
					conv.BackgroundColor = GrfColors.White;

					Z.Start(2005);
					match.Convert(conv);
					Z.Stop(2005);

					Z.Start(2006);
					switch (transparency) {
						case SprTransparencyMode.PixelIndexZero:
							match = _sprGetImageUsingPixelZero(originalPalette, imageSource, match);
							break;
						case SprTransparencyMode.PixelIndexPink:
						case SprTransparencyMode.FirstPixel:
						case SprTransparencyMode.LastPixel:
							match = _sprGetImageUsingPixel(originalPalette, match, transparencyColor.Value);
							break;
					}
					Z.Stop(2006);

					match.MakeFirstPixelTransparent();
					return match;
				case SprConvertMode.MergeOld:
				case SprConvertMode.MergeRgb:
				case SprConvertMode.MergeLab:
					GrfImage merge = imageSource.Copy();

					if (transparencyColor != null && transparency != SprTransparencyMode.PixelIndexZero) {
						merge.MakeColorTransparent(transparencyColor.Value);
					}

					merge = SpriteImageToIndexed8(spr, merge, useDithering, mode);

					switch (transparency) {
						case SprTransparencyMode.PixelIndexZero:
							merge = _sprGetImageUsingPixelZero(originalPalette, imageSource, merge);
							break;
						case SprTransparencyMode.PixelIndexPink:
						case SprTransparencyMode.FirstPixel:
						case SprTransparencyMode.LastPixel:
							merge = _sprGetImageUsingPixel(originalPalette, merge, transparencyColor.Value);
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

		private static GrfImage _sprGetImageUsingPixel(byte[] originalPalette, GrfImage image, in GrfColor color) {
			if (image != null && image.GrfImageType == GrfImageType.Indexed8) {
				GrfImage im = image.Copy();

				bool[] toChange = new bool[256];

				for (int i = 0; i < 256; i++) {
					if (image.Palette[4 * i + 0] == color.R &&
						image.Palette[4 * i + 1] == color.G &&
						image.Palette[4 * i + 2] == color.B) {
						toChange[i] = true;
					}
				}

				Buffer.BlockCopy(originalPalette, 0, im.Palette, 0, 4);

				unsafe {
					fixed (byte* pDstBase = im.Pixels)
					fixed (bool* pToChange = toChange) {
						byte* pDst = pDstBase;
						byte* pDstEnd = pDstBase + im.Pixels.Length;

						while (pDst < pDstEnd) {
							if (pToChange[*pDst])
								*pDst = 0;

							pDst++;
						}
					}
				}

				return im;
			}

			return null;
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
			byte[] newPalette = Methods.Copy(originalPalette);

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

		public unsafe void Fill(byte value) {
			fixed (byte* pDst = Pixels) {
				NativeMethods.memset((IntPtr)pDst, value, (UIntPtr)Pixels.Length);
			}
		}

		public unsafe void Fill(int offset, int length, byte value) {
			length = Math.Min(Pixels.Length - offset - 1, length);

			if (length < 0) return;
			if (offset + length >= Pixels.Length)
				throw new ArgumentOutOfRangeException("Total length of the fill buffer is larger than the image size.", "length");

			fixed (byte* pDst = Pixels) {
				NativeMethods.memset((IntPtr)(pDst + offset), value, (UIntPtr)length);
			}
		}
	}
}
