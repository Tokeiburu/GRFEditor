using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GRF.Core;
using GRF.FileFormats.GndFormat;
using GRF.Image;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.GatFormat {
	public static class GatPreviewImageMaker {
		public static byte[] Type0 = new byte[] { 25, 25, 25, 255 };
		public static byte[] Type1 = new byte[] { 153, 153, 153, 255 };
		public static byte[] Type5 = new byte[] { 70, 70, 70, 255 };
		public static byte[] Type6 = new byte[] { 109, 109, 109, 255 };
		public static byte[] Type8 = new byte[] { 66, 97, 255, 255 };
		public static byte[] Type99 = new byte[] { 255, 255, 255, 255 };
		public static byte[] TransparentColor = new byte[] { 255, 0, 255, 255 };

		// Brushes and palettes
		private static byte[] __paletteNormal;
		private static byte[] __paletteTransparentMinimap;
		private static byte[] __paletteHeightmap;

		private static readonly byte[][] _brush = new[] {
			new byte[] { 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 0, 0, 0 },
			new byte[] { 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0 },
			new byte[] { 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0 },
			new byte[] { 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0 },
			new byte[] { 0, 2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 2, 2, 0 },
			new byte[] { 2, 2, 2, 2, 2, 1, 1, 1, 1, 2, 2, 2, 2, 2 },
			new byte[] { 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2 },
			new byte[] { 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2 },
			new byte[] { 2, 2, 2, 2, 2, 1, 1, 1, 1, 2, 2, 2, 2, 2 },
			new byte[] { 0, 2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 2, 2, 0 },
			new byte[] { 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0 },
			new byte[] { 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0 },
			new byte[] { 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0 },
			new byte[] { 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 0, 0, 0 }
		};

		private static readonly byte[][] _brushX = new[] {
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 0, 0, 0, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 0, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 0, 0 },
			new byte[] { 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 0, 0, 0, 0 }
		};

		private struct BrushOffset {
			public int Dx, Dy;
			public byte Value;
		}

		private static BrushOffset[] _brushOffsets;
		private static BrushOffset[] _brushOffsetsX;
		private static BrushOffset[] _brushOffsetsMask;

		static GatPreviewImageMaker() {
			int size = _brush.Length;
			int middle = size / 2;

			var list = new List<BrushOffset>();
			var listMask = new List<BrushOffset>();
			var listX = new List<BrushOffset>();

			for (int y = 0; y < size; y++) {
				for (int x = 0; x < _brush[y].Length; x++) {
					byte v = _brush[y][x];
					if (v == 0) continue;

					var brushOffset = new BrushOffset {
						Dx = x - middle,
						Dy = y - middle + 1,
						Value = v
					};

					list.Add(brushOffset);

					if (v == 1) {
						listMask.Add(brushOffset);
					}

					byte vX = _brushX[y][x];
					if (vX != 0) {
						listX.Add(brushOffset);
					}
				}
			}

			_brushOffsets = list.ToArray();
			_brushOffsetsX = listX.ToArray();
			_brushOffsetsMask = listMask.ToArray();
		}

		internal static byte[] PaletteNormal {
			get {
				if (__paletteNormal == null) {
					__paletteNormal = new byte[1024];

					for (int i = 0; i < 1024; i++) {
						__paletteNormal[i] = 255;
					}

					Buffer.BlockCopy(TransparentColor, 0, __paletteNormal, 4 * 0, 4);
					Buffer.BlockCopy(Type0, 0, __paletteNormal, 4 * 1, 4);
					Buffer.BlockCopy(Type1, 0, __paletteNormal, 4 * 2, 4);
					Buffer.BlockCopy(Type5, 0, __paletteNormal, 4 * 6, 4);
					Buffer.BlockCopy(Type6, 0, __paletteNormal, 4 * 7, 4);
					Buffer.BlockCopy(Type8, 0, __paletteNormal, 4 * 9, 4);
					Buffer.BlockCopy(Type99, 0, __paletteNormal, 4 * 255, 4);
				}

				return __paletteNormal;
			}
		}

		internal static byte[] PaletteTransparentMinimap {
			get {
				if (__paletteTransparentMinimap == null) {
					__paletteTransparentMinimap = new byte[1024];

					for (int i = 0; i < 1024; i++) {
						__paletteTransparentMinimap[i] = 255;
					}

					Buffer.BlockCopy(TransparentColor, 0, __paletteTransparentMinimap, 4 * 0, 4);
					Buffer.BlockCopy(Type0, 0, __paletteTransparentMinimap, 4 * 1, 4);
					Buffer.BlockCopy(Type1, 0, __paletteTransparentMinimap, 4 * 2, 4);
					Buffer.BlockCopy(Type1, 0, __paletteTransparentMinimap, 4 * 3, 4);
					Buffer.BlockCopy(Type0, 0, __paletteTransparentMinimap, 4 * 4, 4);
					Buffer.BlockCopy(Type0, 0, __paletteTransparentMinimap, 4 * 5, 4);
					Buffer.BlockCopy(Type5, 0, __paletteTransparentMinimap, 4 * 6, 4);
					Buffer.BlockCopy(Type0, 0, __paletteTransparentMinimap, 4 * 7, 4);
					Buffer.BlockCopy(Type99, 0, __paletteTransparentMinimap, 4 * 255, 4);
				}

				return __paletteTransparentMinimap;
			}
		}

		internal static byte[] PaletteHeightmap {
			get {
				if (__paletteHeightmap == null) {
					__paletteHeightmap = new byte[1024];

					for (int i = 0; i < 256; i++) {
						__paletteHeightmap[4 * i + 0] = (byte) i;
						__paletteHeightmap[4 * i + 1] = (byte) i;
						__paletteHeightmap[4 * i + 2] = (byte) i;
						__paletteHeightmap[4 * i + 3] = 255;
					}
				}

				return __paletteHeightmap;
			}
		}

		public static GrfImage LoadQuickPreviewImage(byte[] gatData) {
			var reader = new ByteReader(gatData);
			GatHeader header = new GatHeader(reader);

			var pixels = new byte[header.Width * header.Height];
			var palette = new byte[1024];
			Buffer.BlockCopy(PaletteTransparentMinimap, 0, palette, 0, 1024);
			GrfImage image = new GrfImage(pixels, header.Width, header.Height, GrfImageType.Indexed8, palette);

			int offset = 0;
			// We need to reverse the image vertically
			for (int i = header.Height - 1; i > -1; i--) {
				for (int j = 0; j < header.Width; j++) {
					reader.Forward(4 * 4);
					pixels[offset++] = (byte)(reader.Int32() + 1);
				}
			}

			return image;
		}

		public static void LoadImage(Gat gatSource, GatPreviewFormat previewFormat, GatPreviewOptions options, string fileName, GrfHolder grfData) {
			const int MapSize = 512;

			if (options.HasFlags(GatPreviewOptions.HideBorders)) {
				HideBorders(gatSource);
			}

			switch (previewFormat) {
				case GatPreviewFormat.Heightmap:
					GenerateHeightMapImage(gatSource);

					if (options.HasFlag(GatPreviewOptions.Rescale))
						Rescale(gatSource, gatSource.Image, MapSize);

					gatSource.Image.Flip(FlipDirection.Vertical);
					break;
				case GatPreviewFormat.GrayBlock:
					GenerateGrayBlockImage(gatSource);

					if (options.HasFlag(GatPreviewOptions.Rescale))
						Rescale(gatSource, gatSource.Image, MapSize);

					if (options.HasFlags(GatPreviewOptions.Transparent))
						Transparent(gatSource);

					gatSource.Image.Flip(FlipDirection.Vertical);
					break;
				case GatPreviewFormat.LightAndShadow:
				case GatPreviewFormat.Light:
				case GatPreviewFormat.Shadow:
					Gnd gnd = new Gnd(grfData.FileTable.TryGet(fileName.ReplaceExtension(".gnd")));
					gatSource.Image = GndTextureHelper.CreatePreviewMapData(gnd, previewFormat);
					gatSource.Image.Flip(FlipDirection.Vertical);
					break;
			}

			if (options.HasFlags(GatPreviewOptions.Rescale)) {
				if (previewFormat < GatPreviewFormat.LightAndShadow) {
					int pHorizontal = MapSize - gatSource.Image.Width;
					int pVertical = MapSize - gatSource.Image.Height;
					gatSource.Image.Margin(pHorizontal / 2, pVertical / 2, pHorizontal - (pHorizontal / 2), pVertical - (pVertical / 2));
				}
				else {
					double multiplier = (double) MapSize / Math.Max(gatSource.Image.Width, gatSource.Image.Height);
					gatSource.Image.Scale((float) multiplier, GrfScalingMode.LinearScaling);
					int pHorizontal = MapSize - gatSource.Image.Width;
					int pVertical = MapSize - gatSource.Image.Height;
					gatSource.Image.Margin(pHorizontal / 2, pVertical / 2, pHorizontal - (pHorizontal / 2), pVertical - (pVertical / 2));
				}
			}
		}

		public static void GenerateGrayBlockImage(Gat gatSource) {
			byte[] pixels = new byte[gatSource.Width * gatSource.Height];
			byte[] palette = new byte[1024];
			Buffer.BlockCopy(PaletteTransparentMinimap, 0, palette, 0, 1024);

			if (gatSource.Cells.Length < gatSource.Width * gatSource.Height)
				throw new ArgumentOutOfRangeException("gatSource.Cells");

			unsafe {
				fixed (byte* pDstBase = pixels)
				fixed (Cell* pSrcBase = gatSource.Cells) {
					byte* pDst = pDstBase;
					byte* pDstEnd = pDstBase + pixels.Length;
					Cell* pSrc = pSrcBase;
					Cell* pSrcEnd = pSrcBase + gatSource.Cells.Length;

					while (pDst < pDstEnd) {
						*pDst = (byte)((*pSrc).Type + 1);

						pSrc++;
						pDst++;
					}
				}
			}

			gatSource.Image = new GrfImage(pixels, gatSource.Width, gatSource.Height, GrfImageType.Indexed8, palette);
		}

		public static void GenerateHeightMapImage(Gat gatSource) {
			byte[] pixels = new byte[gatSource.Width * gatSource.Height];
			byte[] palette = new byte[1024];
			Buffer.BlockCopy(PaletteHeightmap, 0, palette, 0, 1024);
			float max = Single.MinValue;
			float min = Single.MaxValue;

			for (int i = 0, count = gatSource.Cells.Length; i < count; i++) {
				if (gatSource.Cells[i].Average > max)
					max = gatSource.Cells[i].Average;
				else if (gatSource.Cells[i].Average < min)
					min = gatSource.Cells[i].Average;
			}

			float range = max - min;
			int index = 0;

			// We can't precalculate the colors for the heightmap =/...
			// We need to reverse the image vertically
			for (int i = 0; i < gatSource.Height; i++) {
				for (int j = 0; j < gatSource.Width; j++) {
					pixels[index++] = (byte) (255 - (gatSource.Cells[i * gatSource.Width + j].Average - min) / range * 255);
				}
			}

			gatSource.Image = new GrfImage(pixels, gatSource.Width, gatSource.Height, GrfImageType.Indexed8, palette);
		}

		public unsafe static void Rescale(Gat gatSource, GrfImage image, int size) {
			double multiplier = Math.Max(image.Width, image.Height) / (double)size;

			int bpp = image.GetBpp();

			int newWidth = (int)(image.Width / multiplier);
			int newHeight = (int)(image.Height / multiplier);
			int oldWidth = image.Width;
			int oldHeight = image.Height;

			byte[] pixels = new byte[newWidth * newHeight * bpp];
			bool[] written = new bool[newWidth * newHeight];

			fixed (byte* pSrc = image.Pixels)
			fixed (byte* pDst = pixels) {
				for (int y = 0; y < oldHeight; y++) {
					int dstYStride = (int)(y / multiplier) * newWidth;

					for (int x = 0; x < oldWidth; x++) {
						int dstX = (int)(x / multiplier);
						int dstIndex = dstX + dstYStride;
						int dstByte = dstIndex * bpp;

						int srcIndex = (y * oldWidth + x) * bpp;

						switch (bpp) {
							case 1:
								pDst[dstByte] = pSrc[srcIndex];
								break;
							case 3:
								pDst[dstByte + 0] = pSrc[srcIndex + 0];
								pDst[dstByte + 1] = pSrc[srcIndex + 1];
								pDst[dstByte + 2] = pSrc[srcIndex + 2];
								break;
							case 4:
								*(int*)(pDst + dstByte) = *(int*)(pSrc + srcIndex);
								break;
						}

						written[dstIndex] = true;
					}
				}

				for (int x = 0; x < newWidth; x++) {
					int lastIdx = -1;

					for (int y = 0; y < newHeight; y++) {
						int idx = x + y * newWidth;

						if (written[idx]) {
							lastIdx = idx;
						}
						else if (lastIdx != -1) {
							if (bpp == 1)
								pDst[idx] = pDst[lastIdx];
							else
								Buffer.MemoryCopy(pDst + lastIdx * bpp, pDst + idx * bpp, bpp, bpp);

							written[idx] = true;
						}
					}
				}

				int lastValid = -1;

				for (int i = 0; i < written.Length; i++) {
					if (written[i]) {
						lastValid = i;
					}
					else if (lastValid != -1) {
						if (bpp == 1)
							pDst[i] = pDst[lastValid];
						else
							Buffer.MemoryCopy(pDst + lastValid * bpp, pDst + i * bpp, bpp, bpp);
					}
				}

				image.SetPixels(ref pixels, newWidth, newHeight);
			}
		}

		public static void HideBorders(Gat gatSource) {
			const int thickness = 2;
			int width = gatSource.Width;
			int height = gatSource.Height;
			
			if (height > 2) {
				for (int y = 0; y < thickness; y++) {
					for (int x = 0; x < width; x++) {
						gatSource[x, y].Type = GatType.NoWalkable;
						gatSource[x, height - y - 1].Type = GatType.NoWalkable;
					}
				}
			}
			
			if (width > 2) {
				for (int x = 0; x < thickness; x++) {
					for (int y = 2; y < height - 2; y++) {
						gatSource[x, y].Type = GatType.NoWalkable;
						gatSource[width - x - 1, y].Type = GatType.NoWalkable;
					}
				}
			}
		}

		public unsafe static void Transparent(Gat gat) {
			byte[] pixels = gat.Image.Pixels;
			int width = gat.Image.Width;
			int height = gat.Image.Height;

			fixed (byte* pDstBase = pixels) {
				byte* pDst = pDstBase;
				byte*[] offsets = new byte*[height];
				
				for (int y = 0; y < height; y++) {
					offsets[y] = pDst + y * width;
				}

				Parallel.For(0, height / 2 + 1, yy => {
					int y = height - yy * 2 - 1;
					bool previous = false;

					for (int x = 0; x < width + 2; x += 2) {
						previous = _checkPixel(width, height, x, y, pDst, offsets, previous);
					}
				});
			}
		}

		private unsafe static bool _checkPixel(int width, int height, int x, int y, byte* pixels, byte*[] pRows, bool previous) {
			var brush = _brushOffsets;

			if (previous) {
				brush = _brushOffsetsX;
			}

			// Check against mask
			for (int i = 0; i < brush.Length; i++) {
				ref var b = ref brush[i];

				int px = x + b.Dx;
				int py = y + b.Dy;

				if ((uint)px < (uint)width && (uint)py < (uint)height) {
					byte p = *(pRows[py] + px);

					if (p != 2 && p != 0) {
						return false;
					}
				}
			}

			// Apply mask
			for (int i = 0; i < _brushOffsetsMask.Length; i++) {
				ref var b = ref _brushOffsetsMask[i];

				int px = x + b.Dx;
				int py = y + b.Dy;

				if ((uint)px >= (uint)width || (uint)py >= (uint)height)
					continue;

				pixels[py * width + px] = 0;
			}

			return true;
		}
	}
}