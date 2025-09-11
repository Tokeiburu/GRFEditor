using System;
using System.Linq;
using GRF.Core;
using GRF.FileFormats.GndFormat;
using GRF.Image;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.GatFormat {
	internal static class GatPreviewImageMaker {
		private const int _middle = 7;
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
		private static int? _brushCount;

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
			GrfImage image = new GrfImage(ref pixels, header.Width, header.Height, GrfImageType.Indexed8, ref palette);

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

			if (options.HasFlags(GatPreviewOptions.Rescale)) {
				Rescale(gatSource, MapSize);
			}

			switch (previewFormat) {
				case GatPreviewFormat.Heightmap:
					_generateHeightMapImage(gatSource);
					break;
				case GatPreviewFormat.GrayBlock:
					_generateGrayBlockImage(gatSource);
					if (options.HasFlags(GatPreviewOptions.Transparent)) {
						Transparent(gatSource);
					}
					break;
				case GatPreviewFormat.LightAndShadow:
					_generateLightAndShadowMapImage(gatSource, fileName, grfData);
					break;
				case GatPreviewFormat.Light:
					_generateLightMapImage(gatSource, fileName, grfData);
					break;
				case GatPreviewFormat.Shadow:
					_generateShadowMapImage(gatSource, fileName, grfData);
					break;
			}

			if (options.HasFlags(GatPreviewOptions.Rescale)) {
				//double multipler = (double) MapSize / Math.Max(gatSource.Width, gatSource.Height);
				//gatSource.Image.Scale((float) multipler);

				if (previewFormat < GatPreviewFormat.LightAndShadow)
					gatSource.Image.Redim(MapSize, MapSize);
				else {
					double multipler = (double) MapSize / Math.Max(gatSource.Image.Width, gatSource.Image.Height);
					gatSource.Image.Scale((float) multipler, GrfScalingMode.LinearScaling);
					gatSource.Image.Redim(MapSize, MapSize, 0);
				}
			}
		}

		private static void _generateLightAndShadowMapImage(IImageable gatSource, string fileName, GrfHolder grf) {
			Gnd gnd = new Gnd(grf.FileTable.TryGet(fileName.ReplaceExtension(".gnd")).GetDecompressedData());
			gatSource.Image = new GrfImage(new TextureMapsGenerator().CreatePreviewMapData(gnd), gnd.Header.Width * (gnd.LightmapWidth - 2), gnd.Header.Height * (gnd.LightmapHeight - 2), GrfImageType.Bgra32);
			gatSource.Image.Flip(FlipDirection.Vertical);
		}

		private static void _generateLightMapImage(IImageable gatSource, string fileName, GrfHolder grf) {
			Gnd gnd = new Gnd(grf.FileTable.TryGet(fileName.ReplaceExtension(".gnd")).GetDecompressedData());
			gatSource.Image = new GrfImage(new TextureMapsGenerator().CreateLightmapData(gnd), gnd.Header.Width * (gnd.LightmapWidth - 2), gnd.Header.Height * (gnd.LightmapHeight - 2), GrfImageType.Bgra32);
			gatSource.Image.Flip(FlipDirection.Vertical);
		}

		private static void _generateShadowMapImage(IImageable gatSource, string fileName, GrfHolder grf) {
			Gnd gnd = new Gnd(grf.FileTable.TryGet(fileName.ReplaceExtension(".gnd")).GetDecompressedData());
			gatSource.Image = new GrfImage(new TextureMapsGenerator().CreateShadowmapData(gnd), gnd.Header.Width * (gnd.LightmapWidth - 2), gnd.Header.Height * (gnd.LightmapHeight - 2), GrfImageType.Bgra32);
			gatSource.Image.Flip(FlipDirection.Vertical);
		}

		private static void _generateGrayBlockImage(Gat gatSource) {
			byte[] pixels = new byte[gatSource.Width * gatSource.Height];
			byte[] palette = new byte[1024];
			Buffer.BlockCopy(PaletteTransparentMinimap, 0, palette, 0, 1024);

			int offset = 0;
			// We need to reverse the image vertically
			for (int i = gatSource.Height - 1; i > -1; i--) {
				for (int j = 0; j < gatSource.Width; j++) {
					pixels[offset++] = (byte) (gatSource.Cells[i * gatSource.Width + j].Type + 1);
				}
			}

			gatSource.Image = new GrfImage(ref pixels, gatSource.Width, gatSource.Height, GrfImageType.Indexed8, ref palette);
		}

		private static void _generateHeightMapImage(Gat gatSource) {
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

			float overall = max - min;

			foreach (Cell t in gatSource.Cells.Distinct()) {
				t.Average = (t.Average - min) / overall;
			}

			int index = 0;

			// We can't precalculate the colors for the heightmap =/...
			// We need to reverse the image vertically
			for (int i = gatSource.Height - 1; i > -1; i--) {
				for (int j = 0; j < gatSource.Width; j++) {
					pixels[index++] = (byte) (255 - gatSource.Cells[i * gatSource.Width + j].Average * 255);
				}
			}

			gatSource.Image = new GrfImage(ref pixels, gatSource.Width, gatSource.Height, GrfImageType.Indexed8, ref palette);
		}

		public static void Rescale(Gat gatSource, int size) {
			double multipler = Math.Max(gatSource.Width, gatSource.Height) / (double) size;
			Gat gat = new Gat((int) (gatSource.Width / multipler), (int) (gatSource.Height / multipler), null);

			for (int y = 0; y < gatSource.Height; y++) {
				for (int x = 0; x < gatSource.Width; x++) {
					gat.Cells[(int) (x / multipler) + (int) (y / multipler) * gat.Header.Width] = gatSource.Cells[y * gatSource.Width + x];
				}
			}

			Cell curr = null;

			for (int x = 0; x < gat.Header.Width; x++) {
				curr = gat.Cells[x];

				if (curr == null) continue;

				for (int y = 1; y < gat.Header.Height; y++) {
					if (gat.Cells[x + y * gat.Header.Width] == null) {
						gat.Cells[x + y * gat.Header.Width] = curr;
					}
					else {
						curr = gat.Cells[x + y * gat.Header.Width];
					}
				}
			}

			for (int i = 0, count = gat.Cells.Length; i < count; i++) {
				if (gat.Cells[i] != null) {
					curr = gat.Cells[i];
				}
				else {
					gat.Cells[i] = curr;
				}
			}

			gatSource.Cells = gat.Cells;
			gatSource.Width = gat.Header.Width;
			gatSource.Height = gat.Header.Height;
		}

		public static void HideBorders(Gat gatSource) {
			for (int i = 0; i < gatSource.Height; i++) {
				for (int j = 0; j < gatSource.Width; j++) {
					if (i < 2 || i > gatSource.Height - 3 ||
					    j < 2 || j > gatSource.Width - 3) {
						gatSource[j, i].Type = GatType.NoWalkable;
					}
				}
			}
		}

		public static void Transparent(Gat gatSource) {
			byte[] palette = new byte[1024];
			Buffer.BlockCopy(PaletteTransparentMinimap, 0, palette, 0, 1024);
			byte[] pixels = gatSource.Image.Pixels;

			for (int i = 0; i < gatSource.Image.Height; i += 2) {
				for (int j = 0; j < gatSource.Image.Width; j += 2) {
					_checkPixel(gatSource.Image.Width, gatSource.Image.Height, j, i, pixels);
				}
			}
		}

		private static void _checkPixel(int width, int height, int x, int y, byte[] pixels) {
			if (_brushCount == null) {
				_brushCount = 0;
				for (int i = 0; i < _brush.Length; i++) {
					for (int j = 0; j < _brush[i].Length; j++) {
						if (_brush[i][j] != 0)
							_brushCount++;
					}
				}
			}

			int found = 0;
			int indexX;
			int indexY;
			int index;

			for (int i = 0; i < _brush.Length; i++) {
				for (int j = 0; j < _brush.Length; j++) {
					if (_brush[i][j] == 0)
						continue;

					indexX = x - _middle + j;
					indexY = y - _middle + i;

					if (indexX < 0 || indexX >= width ||
					    indexY < 0 || indexY >= height) {
						found++;
						continue;
					}

					index = indexY * width + indexX;

					if ((pixels[index] == 2 || pixels[index] == 0)) {
						found++;
					}
				}
			}

			if (found != _brushCount)
				return;

			for (int i = 0; i < _brush.Length; i++) {
				for (int j = 0; j < _brush.Length; j++) {
					if (_brush[i][j] != 1) continue;

					indexX = x - _middle + j;
					indexY = y - _middle + i;

					if (indexX < 0 || indexX >= width ||
					    indexY < 0 || indexY >= height) {
						continue;
					}

					index = indexY * width + indexX;

					pixels[index] = 0;
				}
			}
		}
	}
}