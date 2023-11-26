using System;
using System.Collections.Generic;
using GRF;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RswFormat;
using GRF.IO;
using OpenTK;

namespace GRFEditor.OpenGL.MapComponents {
	/// <summary>
	/// This is simplified version of the GRF.FileFormats.Gnd which uses OpenTK structures.
	/// </summary>
	public class Gnd {
		public readonly List<Cube> Cubes = new List<Cube>();
		public readonly List<string> Textures = new List<string>();
		public readonly List<Tile> Tiles = new List<Tile>();
		public readonly List<byte[]> Lightmaps = new List<byte[]>();
		public WaterData Water = new WaterData();

		public Gnd(MultiType data)
			: this(data.GetBinaryReader()) {
		}

		private Gnd(IBinaryReader data) {
			Header = new GndHeader(data);
			_loadTexturesPath(data);
			_loadLightmaps(data);
			_loadTiles(data);
			_loadCubes(data);
			_loadWater(data);
		}

		public Cube this[int x, int y] {
			get {
				if (x < 0) return null;
				if (y < 0) return null;
				if (x >= Header.Width) return null;
				if (y >= Header.Height) return null;
				return Cubes[x + Header.Width * y];
			}
		}

		public GndHeader Header { get; private set; }
		public int LightmapWidth;
		public int LightmapHeight;
		public int LightmapSizeCell;

		public int Height {
			get { return Header.Height; }
		}

		public int Width {
			get { return Header.Width; }
		}

		private void _loadCubes(IBinaryReader data) {
			Cubes.Capacity = Header.Width * Header.Height;

			for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
				Cubes.Add(new Cube(data));
			}
			
			for (int x = 0; x < Header.Width; x++)
				for (int y = 0; y < Header.Height; y++)
					this[x, y].CalcNormals(this, x, y);
		}

		private void _loadTiles(IBinaryReader data) {
			int count = data.Int32();
			Tiles.Capacity = count;

			for (int i = 0; i < count; i++) {
				Tiles.Add(new Tile(data));
			}
		}

		private void _loadLightmaps(IBinaryReader data) {
			int count = data.Int32();
			LightmapWidth = data.Int32();
			LightmapHeight = data.Int32();
			LightmapSizeCell = data.Int32();
			int size = LightmapWidth * LightmapHeight * LightmapSizeCell * 4;

			for (int i = 0; i < count; i++) {
				Lightmaps.Add(data.Bytes(size));
			}
		}

		private void _loadTexturesPath(IBinaryReader data) {
			Textures.Capacity = Header.TextureCount;

			for (int i = 0; i < Header.TextureCount; i++) {
				Textures.Add(data.String(Header.TexturePathSize, '\0'));
			}
		}

		private void _loadWater(IBinaryReader data) {
			if (Header.IsCompatibleWith(1, 8)) {
				RswWater water = new RswWater();

				water.Level = data.Float();
				water.Type = data.Int32();
				water.WaveHeight = data.Float();
				water.WaveSpeed = data.Float();
				water.WavePitch = data.Float();
				water.TextureCycling = data.Int32();

				Water.WaterSplitWidth = data.Int32();
				Water.WaterSplitHeight = data.Int32();
				Water.Zones.Add(water);

				if (Header.IsCompatibleWith(1, 9)) {
					Water.Zones.Clear();

					int count = Water.WaterSplitWidth * Water.WaterSplitHeight;
					for (int i = 0; i < count; i++) {
						RswWater waterSub = new RswWater();

						waterSub.Level = data.Float();
						waterSub.Type = data.Int32();
						waterSub.WaveHeight = data.Float();
						waterSub.WaveSpeed = data.Float();
						waterSub.WavePitch = data.Float();
						waterSub.TextureCycling = data.Int32();

						Water.Zones.Add(waterSub);
					}
				}
				else {
					water.Level = data.Float();
				}
			}
		}

		public int LightmapOffset() {
			return LightmapWidth * LightmapHeight;
		}
	}

	public class Tile {
		public Vector4 Color;
		public Vector2[] TexCoords = new Vector2[4];
		public Int16 TextureIndex;
		public UInt16 LightmapIndex;

		public Tile(Tile tile) {
			Color = tile.Color;
			TexCoords[0] = tile.TexCoords[0];
			TexCoords[1] = tile.TexCoords[1];
			TexCoords[2] = tile.TexCoords[2];
			TexCoords[3] = tile.TexCoords[3];
			TextureIndex = tile.TextureIndex;
			LightmapIndex = tile.LightmapIndex;
		}

		public Tile(IBinaryReader data) {
			TexCoords[0].X = data.Float(); TexCoords[1].X = data.Float(); TexCoords[2].X = data.Float(); TexCoords[3].X = data.Float();
			TexCoords[0].Y = data.Float(); TexCoords[1].Y = data.Float(); TexCoords[2].Y = data.Float(); TexCoords[3].Y = data.Float();
			TextureIndex = data.Int16();
			LightmapIndex = data.UInt16();
			Color = new Vector4 { Z = data.Byte(), Y = data.Byte(), X = data.Byte(), W = data.Byte() };
		}

		public Vector2 this[int index] {
			get { return TexCoords[index]; }
		}
	}

	public class Cube {
		private readonly float[] _heights = new float[4];
		public int TileUp;
		public int TileFront;
		public int TileSide;
		public Vector3 Normal;
		public Vector3[] Normals = new Vector3[4];

		public Cube(IBinaryReader data) {
			_heights[0] = data.Float(); _heights[1] = data.Float(); _heights[2] = data.Float(); _heights[3] = data.Float();
			TileUp = data.Int32();
			TileFront = data.Int32();
			TileSide = data.Int32();

			CalculateNormal();
		}

		public float this[int index] {
			get { return _heights[index]; }
		}

		public void CalculateNormal() {
			Vector3 v1 = new Vector3(10, -_heights[0], 0);
			Vector3 v2 = new Vector3(0, -_heights[1], 0);
			Vector3 v3 = new Vector3(10, -_heights[2], 10);
			Vector3 v4 = new Vector3(0, -_heights[3], 10);

			Vector3 normal1 = Vector3.Normalize(Vector3.Cross(v4 - v3, v1 - v3));
			Vector3 normal2 = Vector3.Normalize(Vector3.Cross(v1 - v2, v4 - v2));
			Normal = Vector3.Normalize(normal1 + normal2);

			for (int i = 0; i < 4; i++)
				Normals[i] = Normal;
		}

		public void CalcNormals(Gnd gnd, int x, int y) {
			for (int i = 0; i < 4; i++) {
				Normals[i] = new Vector3(0);

				for (int ii = 0; ii < 4; ii++) {
					int xx = (ii % 2) * ((i % 2 == 0) ? -1 : 1);
					int yy = (ii / 2) * (i < 2 ? -1 : 1);
					
					if (gnd[x + xx, y + yy] != null)
						Normals[i] += gnd[x + xx, y + yy].Normal;
				}
				
				Normals[i] = Vector3.Normalize(Normals[i]);
			}
		}
	}
}
