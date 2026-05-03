using GRF.FileFormats.GatFormat;
using GRF.Image;
using System.Threading.Tasks;
using Utilities;

namespace GRF.FileFormats.GndFormat {
	internal static class GndTextureHelper {
		public unsafe static GrfImage CreatePreviewMapData(Gnd gnd, GatPreviewFormat textureType) {
			int width = gnd.Header.Width;
			int height = gnd.Header.Height;

			int lmWidth = gnd.LightmapWidth;
			int lmHeight = gnd.LightmapHeight;

			int w = lmWidth - 2;
			int h = lmHeight - 2;

			int stride = width * w;
			int perCell = gnd.PerCell;

			byte[] data = new byte[(width * w) * (height * h) * 4];

			int noTileDefault;

			switch(textureType) {
				default:
				case GatPreviewFormat.Shadow:
					noTileDefault = 0;
					break;
				case GatPreviewFormat.Light:
				case GatPreviewFormat.LightAndShadow:
					noTileDefault = 255 << 24;
					break;
			}

			fixed (byte* pDataBase = data) {
				int* pData = (int*)pDataBase;

				Parallel.For(0, height, y => {
					for (int x = 0; x < width; x++) {
						int baseX = x * w;
						int baseY = y * h;

						Cube cell = gnd.Cubes[x + y * width];

						if (cell.TileUp > -1) {
							var lightData = gnd.Lightmaps[gnd.Tiles[cell.TileUp].LightmapIndex];

							fixed (byte* pLightData = lightData) {
								for (int j = 1; j < lmHeight - 1; j++) {
									int srcRow = j * lmWidth;
									int dstRow = (baseY + j - 1) * stride;

									for (int i = 1; i < lmWidth - 1; i++) {
										int srcIdx = srcRow + i;
										int dstIdx = baseX + (i - 1) + dstRow;
										int colorBase = perCell + 3 * srcIdx;

										byte a;
										byte r;
										byte g;
										byte b;

										switch (textureType) {
											case GatPreviewFormat.LightAndShadow:
												a = (byte)(255 - pLightData[j * gnd.LightmapWidth + i]);
												r = (byte)(((255 - a) * pLightData[colorBase + 0]) >> 8);
												g = (byte)(((255 - a) * pLightData[colorBase + 1]) >> 8);
												b = (byte)(((255 - a) * pLightData[colorBase + 2]) >> 8);
												break;
											case GatPreviewFormat.Light:
												r = pLightData[colorBase + 0];
												g = pLightData[colorBase + 1];
												b = pLightData[colorBase + 2];
												break;
											default:
											case GatPreviewFormat.Shadow:
												pData[dstIdx] = (byte)(255 - pLightData[j * gnd.LightmapWidth + i]) << 24;
												continue;
										}

										pData[dstIdx] = (255 << 24) | (r << 16) | (g << 8) | b;
									}
								}
							}
						}
						else {
							for (int j = 0; j < h; j++) {
								int dstRow = (baseY + j) * stride;

								for (int i = 0; i < w; i++) {
									pData[baseX + i + dstRow] = noTileDefault;
								}
							}
						}
					}
				});
			}

			return new GrfImage(data, gnd.Header.Width * (gnd.LightmapWidth - 2), gnd.Header.Height * (gnd.LightmapHeight - 2), GrfImageType.Bgra32);
		}
	}
}