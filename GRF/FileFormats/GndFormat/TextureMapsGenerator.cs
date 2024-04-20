using System;
using System.Collections.Generic;
using ErrorManager;
using GRF.Core;
using GRF.Image;

namespace GRF.FileFormats.GndFormat {
	internal class TextureMapsGenerator {
		public byte[] CreatePreviewMapData(Gnd gnd) {
			int width = gnd.Header.Width;
			int height = gnd.Header.Height;

			int w = gnd.GridSizeX - 2;
			int h = gnd.GridSizeY - 2;

			byte[] data = new byte[(width * w) * (height * h) * 4];
			int x, y, i, j;
			Cube cell;

			for (y = 0; y < height; y++) {
				for (x = 0; x < width; x++) {
					cell = gnd.Cubes[x + y * width];

					if (cell.TileUp > -1) {
						var lightData = gnd.LightmapContainer.GetRawLightmap(gnd.Tiles[cell.TileUp].LightmapIndex);

						for (i = 1; i < gnd.GridSizeX - 1; i++) {
							for (j = 1; j < gnd.GridSizeY - 1; j++) {
								int idx = 4 * ((x * h + i - 1) + (y * w + j - 1) * (width * w));

								byte a = (byte)(255 - lightData[j * gnd.GridSizeX + i]);
								byte r = lightData[gnd.PerCell + 3 * (j * gnd.GridSizeX + i) + 0];
								byte g = lightData[gnd.PerCell + 3 * (j * gnd.GridSizeX + i) + 1];
								byte b = lightData[gnd.PerCell + 3 * (j * gnd.GridSizeX + i) + 2];

								data[idx + 0] = (byte) (((255 - a) * b + a * 0) / 255f);
								data[idx + 1] = (byte) (((255 - a) * g + a * 0) / 255f);
								data[idx + 2] = (byte) (((255 - a) * r + a * 0) / 255f);
								data[idx + 3] = 255;
							}
						}
					}
					else {
						for (i = 1; i < gnd.GridSizeX - 1; i++) {
							for (j = 1; j < gnd.GridSizeY - 1; j++) {
								int idx = 4 * ((x * h + i - 1) + (y * w + j - 1) * (width * w));
								data[idx + 3] = 255;
							}
						}
					}
				}
			}

			return data;
		}

		public byte[] CreateLightmapData(Gnd gnd) {
			int width = gnd.Header.Width;
			int height = gnd.Header.Height;

			int w = gnd.GridSizeX - 2;
			int h = gnd.GridSizeY - 2;

			byte[] data = new byte[(width * w) * (height * h) * 4];
			int x, y, i, j;
			Cube cell;

			for (y = 0; y < height; y++) {
				for (x = 0; x < width; x++) {
					cell = gnd.Cubes[x + y * width];

					if (cell.TileUp > -1) {
						var lightData = gnd.LightmapContainer.GetRawLightmap(gnd.Tiles[cell.TileUp].LightmapIndex);

						for (i = 1; i < gnd.GridSizeX - 1; i++) {
							for (j = 1; j < gnd.GridSizeY - 1; j++) {
								int idx = 4 * ((x * h + i - 1) + (y * w + j - 1) * (width * w));

								byte r = lightData[gnd.PerCell + 3 * (j * gnd.GridSizeX + i) + 0];
								byte g = lightData[gnd.PerCell + 3 * (j * gnd.GridSizeX + i) + 1];
								byte b = lightData[gnd.PerCell + 3 * (j * gnd.GridSizeX + i) + 2];

								data[idx + 0] = b;
								data[idx + 1] = g;
								data[idx + 2] = r;
								data[idx + 3] = 255;
							}
						}
					}
					else {
						for (i = 1; i < gnd.GridSizeX - 1; i++) {
							for (j = 1; j < gnd.GridSizeY - 1; j++) {
								int idx = 4 * ((x * h + i - 1) + (y * w + j - 1) * (width * w));
								data[idx + 3] = 255;
							}
						}
					}
				}
			}

			return data;
		}

		public byte[] CreateShadowmapData(Gnd gnd) {
			int width = gnd.Header.Width;
			int height = gnd.Header.Height;

			int w = gnd.GridSizeX - 2;
			int h = gnd.GridSizeY - 2;

			byte[] data = new byte[(width * w) * (height * h) * 4];
			int x, y, i, j;
			Cube cell;

			for (y = 0; y < height; y++) {
				for (x = 0; x < width; x++) {
					cell = gnd.Cubes[x + y * width];

					if (cell.TileUp > -1) {
						var lightData = gnd.LightmapContainer.GetRawLightmap(gnd.Tiles[cell.TileUp].LightmapIndex);

						for (i = 1; i < gnd.GridSizeX - 1; i++) {
							for (j = 1; j < gnd.GridSizeY - 1; j++) {
								int idx = 4 * ((x * h + i - 1) + (y * w + j - 1) * (width * w));
								data[idx + 3] = (byte)(255 - lightData[j * gnd.GridSizeX + i]);
							}
						}
					}
					else {
						for (i = 1; i < gnd.GridSizeX - 1; i++) {
							for (j = 1; j < gnd.GridSizeY - 1; j++) {
								int idx = 4 * ((x * h + i - 1) + (y * w + j - 1) * (width * w));
								data[idx + 3] = 0;
							}
						}
					}
				}
			}

			return data;
		}

		public byte[] CreateTilesColorImage(Gnd gnd) {
			int x, y, i;
			GrfColor c;
			Cube cell;
			int width = gnd.Header.Width;
			int height = gnd.Header.Height;
			byte[] data = new byte[width * height * 4];

			for (y = 0; y < height; ++y) {
				for (x = 0; x < width; ++x) {
					cell = gnd.Cubes[x + y * width];

					// Check tile up
					if (cell.TileUp > -1) {
						i = (x + y * width) * 4;
						c = gnd.Tiles[cell.TileUp].TileColor;

						data[i + 0] = c.B;
						data[i + 1] = c.G;
						data[i + 2] = c.R;
						data[i + 3] = c.A;
					}
				}
			}

			return data;
		}
	}
}