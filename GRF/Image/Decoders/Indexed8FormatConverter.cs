using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extension;

namespace GRF.Image.Decoders {
	public class Indexed8FormatConverter : AbstractImageFormatConverter, IImageFormatConverter {
		#region PaletteOptions enum

		[Flags]
		public enum PaletteOptions {
			UseExistingPalette,
			UseDithering,
			AutomaticallyGeneratePalette,
			MergePalettes,
		}

		#endregion

		private readonly Dictionary<int, byte> _matchClosestSearches = new Dictionary<int, byte>();
		private readonly Dictionary<int, byte> _matchPaletteSearches = new Dictionary<int, byte>();

		public PaletteOptions Options;
		public byte[] ExistingPalette { get; set; }

		//public void SetMergePaletteData(HashSet<byte> unusedIndexes, ) {
		//	this.Options |= PaletteOptions.MergePalettes;
		//}

		#region IImageFormatConverter Members

		public void ToBgra32(GrfImage image) {
			int size = image.Width * image.Height;
			byte[] newPixels = new byte[size * 4];
			byte[] reversedPalette = _toBgraPalette(image.Palette);

			for (int i = 0; i < size; i++) {
				Buffer.BlockCopy(reversedPalette, 4 * image.Pixels[i], newPixels, 4 * i, 4);
			}

			image.SetPixels(ref newPixels);
			image.SetGrfImageType(GrfImageType.Bgra32);
		}

		public void Convert(GrfImage image) {
			if (image.GrfImageType != GrfImageType.Bgra32) throw new Exception("Expected pixel format is Bgra32, found " + image.GrfImageType);

			if (UseBackgroundColor) {
				_applyBackgroundColor(image, BackgroundColor);
			}

			byte[] newPixels = new byte[image.Width * image.Height];

			if ((Options & PaletteOptions.AutomaticallyGeneratePalette) == PaletteOptions.AutomaticallyGeneratePalette) {
				ExistingPalette = _generatePalette(image);
			}

			if ((Options & PaletteOptions.UseDithering) == PaletteOptions.UseDithering) {
				GrfColor[] pixels = new GrfColor[image.Width * image.Height];
				int temp;

				for (int y = 0; y < image.Height; y++) {
					for (int x = 0; x < image.Width; x++) {
						temp = 4 * (y * image.Width + x);
						pixels[y * image.Width + x] = new GrfColor(255, image.Pixels[temp + 2], image.Pixels[temp + 1], image.Pixels[temp + 0]);
					}
				}

				GrfColor[] pal = new GrfColor[256];
				for (int i = 0; i < 256; i++) {
					pal[i] = new GrfColor(255, ExistingPalette[4 * i + 0], ExistingPalette[4 * i + 1], ExistingPalette[4 * i + 2]);
				}

				GrfColor currentError;
				GrfColor oldColor;
				GrfColor newColor;
				int blockOptY;
				int blockOptX;
				for (int y = 0; y < image.Height; y++) {
					blockOptY = y * image.Width;
					for (int x = 0; x < image.Width; x++) {
						blockOptX = blockOptY + x;
						oldColor = pixels[blockOptX];

						int index = _findClosestColor(oldColor, pal);
						newColor = pal[index];

						if (image.Pixels[(blockOptX) * 4 + 3] == 0)
							newPixels[blockOptX] = 0;
						else
							newPixels[blockOptX] = (byte)index;

						pixels[blockOptX] = new GrfColor(newColor);
						currentError = _sub(oldColor, newColor);

						if (x + 1 < image.Width)
							pixels[blockOptX + 1] = _add(pixels[blockOptX + 1], _mult(new GrfColor(currentError), 7 / 16f));

						if (x - 1 >= 0 && y + 1 < image.Height)
							pixels[(y + 1) * image.Width + x - 1] = _add(pixels[(y + 1) * image.Width + x - 1], _mult(currentError, 3 / 16f));

						if (y + 1 < image.Height)
							pixels[(y + 1) * image.Width + x] = _add(pixels[(y + 1) * image.Width + x], _mult(currentError, 5 / 16f));

						if (x + 1 < image.Width && y + 1 < image.Height)
							pixels[(y + 1) * image.Width + x + 1] = _add(pixels[(y + 1) * image.Width + x + 1], _mult(currentError, 1 / 16f));
					}
				}
			}
			else {
				Tuple<GrfColor, byte>[] colorIndexes = new Tuple<GrfColor, byte>[256];

				for (int i = 0, size = ExistingPalette.Length / 4; i < size; i++) {
					colorIndexes[i] = new Tuple<GrfColor, byte>(
										 GrfColor.FromArgb(255, ExistingPalette[4 * i + 0],
														   ExistingPalette[4 * i + 1],
														   ExistingPalette[4 * i + 2]),
										 (byte)i);
				}

				for (int i = 0; i < newPixels.Length; i++) {
					if (image.Pixels[4 * i + 3] == 0)
						newPixels[i] = 0;
					else {
						newPixels[i] = _findClosestPaletteMatch(
							colorIndexes,
							image.Pixels[4 * i + 2], image.Pixels[4 * i + 1], image.Pixels[4 * i + 0]);
					}
				}
			}

			{
				byte[] pal = new byte[ExistingPalette.Length];
				Buffer.BlockCopy(ExistingPalette, 0, pal, 0, pal.Length);

				image.SetPalette(ref pal);
			}

			image.SetGrfImageType(GrfImageType.Indexed8);
			image.SetPixels(ref newPixels);
		}

		#endregion

		private byte[] _generatePalette(GrfImage image) {
			List<int> colors = new List<int>();

			Dictionary<int, int> colorDictionary = new Dictionary<int, int>();
			colorDictionary.Add(0xff << 24 | 0xff << 16 | 0 << 8 | 0xff, 0);

			int color;
			//int colorTransparent = 0xff << 24 | 0xff << 16 | 0 << 8 | 0xff;
			byte colorA;

			byte[] pixels = image.Pixels;
			GrfColor background = BackgroundColor;

			for (int i = 0, numPixels = pixels.Length / 4; i < numPixels; i++) {
				if (pixels[4 * i + 3] != 0) {
					colorA = pixels[4 * i + 3];

					color = 0xff << 24 |
							(byte)(((255 - colorA) * background.R + colorA * pixels[4 * i + 2]) / 255f) << 16 |
							(byte)(((255 - colorA) * background.G + colorA * pixels[4 * i + 1]) / 255f) << 8 |
							(byte)(((255 - colorA) * background.B + colorA * pixels[4 * i + 0]) / 255f);
				}
				else {
					continue;
					//color = colorTransparent;
				}

				if (!colorDictionary.ContainsKey(color)) {
					colorDictionary.Add(color, 0);
				}
			}

			List<GrfColor> toGrfColors = new List<GrfColor>(256);
			colors = colorDictionary.Keys.ToList();

			for (int i = 0; i < colors.Count; i++) {
				color = colors[i];

				toGrfColors.Add(new GrfColor(
									255,
									(byte)((color & 0x00ff0000) >> 16),
									(byte)((color & 0x0000ff00) >> 8),
									(byte)((color & 0x000000ff))
									));
			}

			if (toGrfColors.Count > 256)
				toGrfColors = _reduceImageQuality(toGrfColors);

			while (toGrfColors.Count < 256) {
				toGrfColors.Add(new GrfColor(255, 0, 0, 0));
			}

			byte[] palette = new byte[1024];

			for (int i = 0; i < 256; i++) {
				palette[4 * i + 0] = toGrfColors[i].R;
				palette[4 * i + 1] = toGrfColors[i].G;
				palette[4 * i + 2] = toGrfColors[i].B;
				palette[4 * i + 3] = toGrfColors[i].A;
			}

			return palette;
		}

		private List<GrfColor> _reduceImageQuality(List<GrfColor> colors) {
			int exceedingColors = colors.Count - 256;
			Dictionary<int, int> closestMatchingColors = new Dictionary<int, int>();

			int searchRadius = (int)(exceedingColors / 150f + 10);

			while (closestMatchingColors.Count < exceedingColors) {
				closestMatchingColors.Clear();

				for (int i = 1; i < colors.Count; i++) {
					if (closestMatchingColors.ContainsKey(i))
						continue;

					for (int j = 1; j < colors.Count; j++) {
						if (j == i || closestMatchingColors.ContainsKey(j))
							continue;

						if (Math.Abs(colors[i].R - colors[j].R) + Math.Abs(colors[i].G - colors[j].G) + Math.Abs(colors[i].B - colors[j].B) < searchRadius) {
							closestMatchingColors.Add(j, i);
						}
					}
				}

				searchRadius *= 2;
			}

			List<GrfColor> newColors = new List<GrfColor>(colors);
			foreach (KeyValuePair<int, int> tuple in closestMatchingColors) {
				newColors[tuple.Key] = colors[tuple.Value];
			}

			newColors = newColors.Distinct().ToList();

			// We fill in the colors

			List<GrfColor> otherColors = new List<GrfColor>(colors);

			newColors.ForEach(p => otherColors.Remove(p));

			for (int i = 0, toFill = 256 - newColors.Count; i < toFill; i++) {
				newColors.Add(otherColors[(int) (i / (float) toFill * otherColors.Count)]);
			}

			return newColors;
		}

		private byte _findClosestPaletteMatch(IList<Tuple<GrfColor, byte>> colorIndexes, byte r, byte g, byte b) {
			if (_matchPaletteSearches.ContainsKey(r << 16 | g << 8 | b))
				return _matchPaletteSearches[r << 16 | g << 8 | b];

			int min = Int32.MaxValue;
			int temp;
			int lastIndex = 0;

			for (int i = 0; i < colorIndexes.Count; i++) {
				temp = Math.Abs(r - colorIndexes[i].Item1.R) +
					   Math.Abs(g - colorIndexes[i].Item1.G) +
					   Math.Abs(b - colorIndexes[i].Item1.B);

				if (temp < min) {
					min = temp;
					lastIndex = i;
				}
			}

			_matchPaletteSearches.Add(r << 16 | g << 8 | b, colorIndexes[lastIndex].Item2);
			return colorIndexes[lastIndex].Item2;
		}

		private byte _clamp(float color) {
			return (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
		}

		private GrfColor _sub(GrfColor oldColor, GrfColor newColor) {
			return new GrfColor(255,
							 _clamp(oldColor.R - newColor.R),
							 _clamp(oldColor.G - newColor.G),
							 _clamp(oldColor.B - newColor.B)
					);
		}

		private GrfColor _add(GrfColor oldColor, GrfColor newColor) {
			return new GrfColor(255,
							 _clamp(oldColor.R + newColor.R),
							 _clamp(oldColor.G + newColor.G),
							 _clamp(oldColor.B + newColor.B)
					);
		}

		private GrfColor _mult(GrfColor oldColor, float mult) {
			return new GrfColor(255,
							 _clamp(oldColor.R * mult),
							 _clamp(oldColor.G * mult),
							 _clamp(oldColor.B * mult)
					);
		}

		private int _findClosestColor(GrfColor oldColor, GrfColor[] pal) {
			if (_matchClosestSearches.ContainsKey(oldColor.ToRgbInt24()))
				return _matchClosestSearches[oldColor.ToRgbInt24()];

			int temp;
			int min = Int32.MaxValue;
			int lastIndex = 0;
			int diffR;
			int diffG;
			int diffB;

			for (int i = 0; i < pal.Length; i++) {
				diffR = Math.Abs(oldColor.R - pal[i].R);
				diffG = Math.Abs(oldColor.G - pal[i].G);
				diffB = Math.Abs(oldColor.B - pal[i].B);

				temp = diffR + diffG + diffB;
				if (temp < min) {
					min = temp;
					lastIndex = i;
				}
			}

			_matchClosestSearches.Add(oldColor.ToRgbInt24(), (byte)lastIndex);
			return lastIndex;
		}
	}
}
