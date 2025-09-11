using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extension;

namespace GRF.Image.Decoders {
	public class Indexed8FormatConverter : AbstractImageFormatConverter, IImageFormatConverter {
		#region PaletteOptions enum

		[Flags]
		public enum PaletteOptions {
			UseExistingPalette = 1,
			UseExistingLabPalette = 2,
			UseDithering = 4,
			AutomaticallyGeneratePalette = 8,
			MergePalettes = 16,
			UseLabDistance = 32,
		}

		#endregion

		private readonly Dictionary<int, byte> _matchClosestSearches = new Dictionary<int, byte>();
		private readonly ConcurrentDictionary<int, byte> _matchPaletteSearches = new ConcurrentDictionary<int, byte>();

		public PaletteOptions Options;
		public byte[] ExistingPalette { get; set; }
		public List<_GrfColorLab> ExistingPaletteLab { get; set; }

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

		private static int[] _preSquared = new int[256];

		static Indexed8FormatConverter() {
			for (int i = 0; i < _preSquared.Length; i++) {
				_preSquared[i] = i * i;
			}
		}

		public void Convert(GrfImage image) {
			if (image.GrfImageType != GrfImageType.Bgra32) throw new Exception("Expected pixel format is Bgra32, found " + image.GrfImageType);

			if (UseBackgroundColor) {
				_applyBackgroundColor(image, BackgroundColor);
			}

			byte[] newPixels = new byte[image.Width * image.Height];

			if ((Options & PaletteOptions.AutomaticallyGeneratePalette) == PaletteOptions.AutomaticallyGeneratePalette) {
				OctreeQuantizer quantizer = new OctreeQuantizer();
				quantizer.AddImage(image);
				var colors = quantizer.GeneratePaletteRgbInt(255);

				ExistingPalette = new byte[1024];

				for (int i = 0; i < 256; i++) {
					if (i < colors.Count) {
						ExistingPalette[4 * i + 0] = (byte)((colors[i] & 0xFF0000) >> 16);
						ExistingPalette[4 * i + 1] = (byte)((colors[i] & 0x00FF00) >> 8);
						ExistingPalette[4 * i + 2] = (byte)(colors[i] & 0x0000FF);
						ExistingPalette[4 * i + 3] = 255;
					}
					else {
						ExistingPalette[4 * i + 3] = 255;
					}
				}
			}

			Dictionary<int, int> matches = new Dictionary<int, int>();

			if ((Options & PaletteOptions.UseDithering) == PaletteOptions.UseDithering) {
				bool isLab = (Options & PaletteOptions.UseLabDistance) == PaletteOptions.UseLabDistance;
				double[] paletteLab = new double[0];
				byte[] oldPixels = Methods.Copy(image.Pixels);
				
				if (isLab) {
					paletteLab = new double[ExistingPalette.Length / 4 * 3];

					for (int i = 0, size = ExistingPalette.Length / 4; i < size; i++) {
						var lab = _GrfColorLab.From(ExistingPalette[4 * i + 0], ExistingPalette[4 * i + 1], ExistingPalette[4 * i + 2]);
						paletteLab[3 * i + 0] = lab.L;
						paletteLab[3 * i + 1] = lab.A;
						paletteLab[3 * i + 2] = lab.B;
					}
				}

				unsafe {
					int[] dx = { 1, -1, 0, 1 };
					int[] dy = { 0, 1, 1, 1 };
					int[] coef = { 7, 3, 5, 1 };

					fixed (byte* pPaletteBase = ExistingPalette)
					fixed (byte* pPixelsBase = oldPixels)
					fixed (byte* pNewPixelsBase = newPixels)
					fixed (double* pPaletteLabBase = paletteLab) {
						// Pixels are in BGRA
						// Palette is in RGBA
						byte* pPixels = pPixelsBase;
						byte* pNewPixels = pNewPixelsBase;
						byte* pPixelsEnd = pPixelsBase + image.Pixels.Length;

						for (int y = 0; y < image.Height; y++) {
							for (int x = 0; x < image.Width; x++) {
								int idx = y * image.Width + x;

								// Find nearest palette color
								*pNewPixels = isLab ? _findClosetMatchLab(matches, pPixels, pPaletteLabBase) : _findClosetMatch(matches, pPixels, pPaletteBase);

								byte* pPalTarget = pPaletteBase + 4 * (*pNewPixels);

								// Compute error
								int errR = Math.Max(0, pPixels[2] - pPalTarget[0]);
								int errG = Math.Max(0, pPixels[1] - pPalTarget[1]);
								int errB = Math.Max(0, pPixels[0] - pPalTarget[2]);

								// Diffuse error
								for (int k = 0; k < 4; k++) {
									int nx = x + dx[k];
									int ny = y + dy[k];
									if (nx >= 0 && nx < image.Width && ny >= 0 && ny < image.Height) {
										byte* pPixelTarget = pPixelsBase + 4 * (ny * image.Width + nx);

										if (pPixelTarget[3] == 0)
											continue;

										pPixelTarget[0] = (byte)Math.Min(255, Math.Max(0, pPixelTarget[0] + errB * coef[k] / 16));
										pPixelTarget[1] = (byte)Math.Min(255, Math.Max(0, pPixelTarget[1] + errG * coef[k] / 16));
										pPixelTarget[2] = (byte)Math.Min(255, Math.Max(0, pPixelTarget[2] + errR * coef[k] / 16));
									}
								}

								pPixels += 4;
								pNewPixels++;
							}
						}
					}
				}
			}
			else {
				if ((Options & PaletteOptions.UseLabDistance) == PaletteOptions.UseLabDistance) {
					ExistingPaletteLab = new List<_GrfColorLab>();
					double[] paletteLab = new double[ExistingPalette.Length / 4 * 3];

					for (int i = 0, size = ExistingPalette.Length / 4; i < size; i++) {
						var lab = _GrfColorLab.From(ExistingPalette[4 * i + 0], ExistingPalette[4 * i + 1], ExistingPalette[4 * i + 2]);
						paletteLab[3 * i + 0] = lab.L;
						paletteLab[3 * i + 1] = lab.A;
						paletteLab[3 * i + 2] = lab.B;
					}

					unsafe {
						fixed (byte* pNewPixelsBase = newPixels)
						fixed (double* pPaletteBase = paletteLab)
						fixed (byte* pPixelsBase = image.Pixels) {
							byte* pPixels = pPixelsBase;
							byte* pNewPixels = pNewPixelsBase;
							byte* pPixelsEnd = pPixelsBase + image.Pixels.Length;

							while (pPixels < pPixelsEnd) {
								*pNewPixels = _findClosetMatchLab(matches, pPixels, pPaletteBase);
								pPixels += 4;
								pNewPixels++;
							}
						}
					}
				}
				else {
					unsafe {
						fixed (byte* pNewPixelsBase = newPixels)
						fixed (byte* pPaletteBase = ExistingPalette)
						fixed (byte* pPixelsBase = image.Pixels) {
							byte* pPixels = pPixelsBase;
							byte* pNewPixels = pNewPixelsBase;
							byte* pPixelsEnd = pPixelsBase + image.Pixels.Length;

							while (pPixels < pPixelsEnd) {
								*pNewPixels = _findClosetMatch(matches, pPixels, pPaletteBase);
								pPixels += 4;
								pNewPixels++;
							}
						}
					}
				}
			}

			byte[] imagepal = new byte[ExistingPalette.Length];
			Buffer.BlockCopy(ExistingPalette, 0, imagepal, 0, imagepal.Length);

			image.SetPalette(ref imagepal);
			image.SetGrfImageType(GrfImageType.Indexed8);
			image.SetPixels(ref newPixels);
		}

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe byte _findClosetMatchLab(Dictionary<int, int> matches, byte* pPixels, double* pPaletteBase) {
			if (pPixels[3] == 0)
				return 0;

			int bestIndex = 0;
			int l = 1;
			var lab = _GrfColorLab.From(pPixels[2], pPixels[1], pPixels[0]);

			if (!matches.TryGetValue(pPixels[2] << 16 | pPixels[1] << 8 | pPixels[0], out bestIndex)) {
				double bestDist = double.MaxValue;
				double* pPal = pPaletteBase + 3;
				double* pPalEnd = pPal + ExistingPalette.Length;

				while (pPal < pPalEnd) {
					double dL = lab.L - pPal[0];
					double da = lab.A - pPal[1];
					double db = lab.B - pPal[2];
					double dist = dL * dL + da * da + db * db;

					if (dist < bestDist) {
						bestDist = dist;
						bestIndex = l;
					}

					pPal += 3;
					l++;
				}

				matches[pPixels[2] << 16 | pPixels[1] << 8 | pPixels[0]] = bestIndex;
			}

			return (byte)bestIndex;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe byte _findClosetMatch(Dictionary<int, int> matches, byte* pPixels, byte* pPaletteBase) {
			if (pPixels[3] == 0)
				return 0;

			int bestIndex = 0;
			int l = 1;

			if (!matches.TryGetValue(pPixels[2] << 16 | pPixels[1] << 8 | pPixels[0], out bestIndex)) {
				int bestDist = int.MaxValue;
				byte* pPal = pPaletteBase + 4;
				byte* pPalEnd = pPal + ExistingPalette.Length;

				while (pPal < pPalEnd) {
					int dr = Math.Abs(pPixels[2] - pPal[0]);
					int dg = Math.Abs(pPixels[1] - pPal[1]);
					int db = Math.Abs(pPixels[0] - pPal[2]);
					int dist = _preSquared[dr] + _preSquared[dg] + _preSquared[db];

					if (dist < bestDist) {
						bestDist = dist;
						bestIndex = l;
					}

					pPal += 4;
					l++;
				}

				matches[pPixels[2] << 16 | pPixels[1] << 8 | pPixels[0]] = bestIndex;
			}

			return (byte)bestIndex;
		}
	}
}
