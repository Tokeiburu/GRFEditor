using GRF.ContainerFormat;
using GRF.FileFormats.RswFormat;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Utilities;
using Utilities.Services;

namespace GRF.FileFormats.GndFormat {
	[StructLayout(LayoutKind.Sequential)]
	public struct UCube {
		public float Height0;
		public float Height1;
		public float Height2;
		public float Height3;

		public int TileUp;
		public int TileFront;
		public int TileSide;

		public float this[int index] {
			get {
				switch(index) {
					case 0:
						return Height0;
					case 1:
						return Height1;
					case 2:
						return Height2;
					case 3:
						return Height3;
					default:
						throw new ArgumentOutOfRangeException("index");
				}
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct UTile {
		public float TexCoordsX0;
		public float TexCoordsX1;
		public float TexCoordsX2;
		public float TexCoordsX3;
		public float TexCoordsY0;
		public float TexCoordsY1;
		public float TexCoordsY2;
		public float TexCoordsY3;
		public Int16 TextureIndex;
		public UInt16 LightmapIndex;
		public uint Color;
	}

	public unsafe class UnsafeGnd {
		public UCube[] Cubes;
		public UTile[] Tiles;
		public WaterData Water = new WaterData();

		private int TextureCount;
		public int Width;
		public int Height;
		public int MajorVersion;
		public int MinorVersion;
		private int TexturePathSize;
		private float ProportionRatio;

		public int LightmapWidth;
		public int LightmapHeight;
		public int LightmapSizeCell;
		public int PerCell;

		public string[] Textures;
		private double? _version;

		public List<byte[]> Lightmaps;

		public double Version {
			get {
				if (_version == null) {
					_version = FormatConverters.DoubleConverter(MajorVersion + "." + MinorVersion);
				}

				return _version.Value;
			}
		}

		public UCube? this[int x, int y] {
			get {
				if (x < 0) return null;
				if (y < 0) return null;
				if (x >= Width) return null;
				if (y >= Height) return null;
				return Cubes[x + Width * y];
			}
		}

		public UnsafeGnd(byte[] data) {
			int count;

			fixed (byte* pDataBase = data) {
				byte* pData = pDataBase;
				byte* pEnd = pDataBase + data.Length;

				// Header
				if (pData + 26 > pEnd) {
					throw new IndexOutOfRangeException();
				}

				if (pData[0] != 'G' && pData[1] != 'R' && pData[2] != 'G' && pData[3] != 'N') {
					TextureCount = *(int*)(pData + 0);
					Width = *(int*)(pData + 4);
					Height = *(int*)(pData + 8);
					MajorVersion = 1;
					MinorVersion = 0;
					TexturePathSize = 80;

					if (Width > 5000 || Height > 5000)
						throw GrfExceptions.__FileFormatException.Create("GND");

					pData += 12;
				}
				else {
					MajorVersion = pData[4];
					MinorVersion = pData[5];
					Width = *(int*)(pData + 6);
					Height = *(int*)(pData + 10);
					ProportionRatio = *(float*)(pData + 14);
					TextureCount = *(int*)(pData + 18);
					TexturePathSize = *(int*)(pData + 22);

					pData += 26;
				}

				// _loadTexturesPath
				if (pData + TexturePathSize * TextureCount > pEnd)
					throw new IndexOutOfRangeException();

				Textures = new string[TextureCount];

				for (int i = 0; i < TextureCount; i++) {
					var path = ReadCString(pData, TexturePathSize);
					Textures[i] = path;
					pData += TexturePathSize;
				}

				// _loadLightmaps
				if (Version > 1) {
					if (pData + 16 > pEnd)
						throw new IndexOutOfRangeException();

					count = *(int*)(pData + 0);
					LightmapWidth = *(int*)(pData + 4);
					LightmapHeight = *(int*)(pData + 8);
					LightmapSizeCell = *(int*)(pData + 12);
					PerCell = LightmapWidth * LightmapHeight * LightmapSizeCell;
					int size = PerCell * 4;

					pData += 16;

					if (pData + size * count > pEnd)
						throw new IndexOutOfRangeException();

					Lightmaps = new List<byte[]>(count);

					for (int i = 0; i < count; i++) {
						byte[] lightData = new byte[size];

						fixed (byte* ptrLight = lightData) {
							Buffer.MemoryCopy(pData, ptrLight, size, size);
						}

						Lightmaps.Add(lightData);
					}

					pData += size * count;
				}

				if (Version > 1) {
					if (pData + 4 > pEnd)
						throw new IndexOutOfRangeException();

					count = *(int*)(pData + 0);
					pData += 4;

					if (pData + Marshal.SizeOf<UTile>() * count > pEnd)
						throw new IndexOutOfRangeException();

					Tiles = new UTile[Marshal.SizeOf<UTile>() * count];

					fixed (UTile* ptrTiles = Tiles) {
						Buffer.MemoryCopy(pData, ptrTiles, Marshal.SizeOf<UTile>() * count, Marshal.SizeOf<UTile>() * count);
					}

					pData += Marshal.SizeOf<UTile>() * count;
				}

				// _loadCubes
				Cubes = new UCube[count = Width * Height];

				if (Version > 1) {
					if (Version > 1.5) {
						if (pData + 28 * count > pEnd)
							throw new IndexOutOfRangeException();

						for (int i = 0; i < count; i++) {
							Cubes[i].Height0 = *(float*)(pData + 0);
							Cubes[i].Height1 = *(float*)(pData + 4);
							Cubes[i].Height2 = *(float*)(pData + 8);
							Cubes[i].Height3 = *(float*)(pData + 12);
							Cubes[i].TileUp = *(int*)(pData + 16);
							Cubes[i].TileFront = *(int*)(pData + 20);
							Cubes[i].TileSide = *(int*)(pData + 24);
							pData += 28;
						}
					}
					else {
						if (pData + 24 * count > pEnd)
							throw new IndexOutOfRangeException();

						for (int i = 0; i < count; i++) {
							Cubes[i].Height0 = *(float*)(pData + 0);
							Cubes[i].Height1 = *(float*)(pData + 4);
							Cubes[i].Height2 = *(float*)(pData + 8);
							Cubes[i].Height3 = *(float*)(pData + 12);
							Cubes[i].TileUp = *(short*)(pData + 16);
							Cubes[i].TileFront = *(short*)(pData + 18);
							Cubes[i].TileSide = *(short*)(pData + 20);
							pData += 24;
						}
					}
				}
				else {
					LightmapWidth = 8;
					LightmapHeight = 8;
					LightmapSizeCell = 1;

					byte[] light = new byte[256];

					for (int i = 0; i < 64; i++) {
						light[i] = 255;
					}

					Lightmaps.Add(light);
					
					count = Width * Height;
					List<UTile> tilesList = new List<UTile>();

					if (pData + count * (12 + 16 + 8 + 32 * 3) > pEnd)
						throw new IndexOutOfRangeException();

					for (int i = 0; i < count; i++) {
						UTile[] tiles = new UTile[3] { new UTile(), new UTile(), new UTile() };  // up, front, side
						UCube cube = new UCube();

						for (int l = 0; l < 3; l++) {
							tiles[l].Color = 0xFFFFFFFF;
							tiles[l].TextureIndex = *(short*)(pData + 4 * l);
						}

						pData += 12;

						cube.Height0 = *(float*)(pData + 0);
						cube.Height1 = *(float*)(pData + 4);
						cube.Height2 = *(float*)(pData + 8);
						cube.Height3 = *(float*)(pData + 12);
						pData += 16;
						pData += 8;

						for (int l = 0; l < 3; l++) {
							tiles[l].TexCoordsX0 = *(float*)(pData + 0);
							tiles[l].TexCoordsY0 = *(float*)(pData + 4);

							tiles[l].TexCoordsX1 = *(float*)(pData + 8);
							tiles[l].TexCoordsY1 = *(float*)(pData + 12);

							tiles[l].TexCoordsX2 = *(float*)(pData + 16);
							tiles[l].TexCoordsY2 = *(float*)(pData + 20);

							tiles[l].TexCoordsX3 = *(float*)(pData + 24);
							tiles[l].TexCoordsY3 = *(float*)(pData + 28);

							pData += 32;
						}

						cube.TileUp = cube.TileSide = cube.TileFront = -1;

						
						if (tiles[0].TextureIndex > -1) {
							cube.TileUp = tilesList.Count;
							tilesList.Add(tiles[0]);
						}
						if (tiles[1].TextureIndex > -1) {
							cube.TileSide = tilesList.Count;
							tilesList.Add(tiles[1]);
						}
						if (tiles[2].TextureIndex > -1) {
							cube.TileFront = tilesList.Count;
							tilesList.Add(tiles[2]);
						}

						Cubes[i] = cube;
					}

					Tiles = tilesList.ToArray();
				}

				// _loadWater
				if (Version >= 1.8) {
					RswWater defWWater = new RswWater();

					if (pData + 32 > pEnd)
						throw new IndexOutOfRangeException();

					defWWater.Level = *(float*)(pData + 0);
					defWWater.Type = *(int*)(pData + 4);
					defWWater.WaveHeight = *(float*)(pData + 8);
					defWWater.WaveSpeed = *(float*)(pData + 12);
					defWWater.WavePitch = *(float*)(pData + 16);
					defWWater.TextureCycling = *(int*)(pData + 20);

					Water.WaterSplitWidth = *(int*)(pData + 24);
					Water.WaterSplitHeight = *(int*)(pData + 28);

					pData += 32;

					if (Version >= 1.9) {
						Water.Zones.Clear();

						count = Water.WaterSplitWidth * Water.WaterSplitHeight;

						if (pData + 24 * count > pEnd)
							throw new IndexOutOfRangeException();

						for (int i = 0; i < count; i++) {
							RswWater waterSub = new RswWater();

							waterSub.Level = *(float*)(pData + 0);
							waterSub.Type = *(int*)(pData + 4);
							waterSub.WaveHeight = *(float*)(pData + 8);
							waterSub.WaveSpeed = *(float*)(pData + 12);
							waterSub.WavePitch = *(float*)(pData + 16);
							waterSub.TextureCycling = *(int*)(pData + 20);
							pData += 24;

							Water.Zones.Add(waterSub);
						}
					}
					else {
						Water.Zones.Clear();

						count = Water.WaterSplitWidth * Water.WaterSplitHeight;


						if (pData + 4 * count > pEnd)
							throw new IndexOutOfRangeException();

						for (int i = 0; i < count; i++) {
							RswWater waterSub = new RswWater(defWWater);
							waterSub.Level = *(float*)(pData + 0);
							pData += 4;

							Water.Zones.Add(waterSub);
						}
					}
				}
			}
		}

		string ReadCString(byte* pData, int maxLen) {
			int len = 0;
			while (len < maxLen && pData[len] != 0)
				len++;

			return EncodingService.DisplayEncoding.GetString(pData, len);
		}
	}
}
