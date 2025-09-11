using System;
using System.Collections.Generic;
using System.Windows;
using GRF;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RswFormat;
using GRF.Graphics;
using GRF.Image;
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

		public Gnd(GRF.FileFormats.GndFormat.Gnd gnd) {
			this.Header = gnd.Header;
			Textures = gnd.Textures;


			LightmapWidth = gnd.LightmapWidth;
			LightmapHeight = gnd.LightmapHeight;
			LightmapSizeCell = gnd.LightmapSizeCell;

			foreach (var light in gnd.LightmapContainer.Lightmaps) {
				Lightmaps.Add(light.Data);
			}
			//grfcolor: argb
			//grfcolor: z = a, y = r, x = g, w = b
			foreach (var tile in gnd.Tiles) {
				var nTile = new Tile {
					Color = new TkVector4(tile.TileColor.G, tile.TileColor.R, tile.TileColor.A, tile.TileColor.B),
					LightmapIndex = tile.LightmapIndex,
					TextureIndex = tile.TextureIndex
				};
				for (int i = 0; i < 4; i++) {	
					nTile.TexCoords[i].X = tile.TexCoords[i].X;
					nTile.TexCoords[i].Y = tile.TexCoords[i].Y;
				}
				Tiles.Add(nTile);
			}

			foreach (var cube in gnd.Cubes) {
				var nCube = new Cube {
					TileFront = cube.TileFront,
					TileSide = cube.TileSide,
					TileUp = cube.TileUp,
				};
				for (int i = 0; i < 4; i++)
					nCube[i] = cube[i];

				Cubes.Add(nCube);
			}

			Water = gnd.Water;
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

			if (Header.Version <= 1) {
				LightmapHeight = 8;
				LightmapWidth = 8;
				LightmapSizeCell = 1;

				byte[] light = new byte[256];

				for (int i = 0; i < 64; i++) {
					light[i] = 255;
				}

				Lightmaps.Add(light);
				
				for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
					Tile[] tiles = new Tile[3] { new Tile(), new Tile(), new Tile() };	// up, front, side
					Cube cube = new Cube();

					for (int l = 0; l < 3; l++) {
						tiles[l].Color = new TkVector4(255, 255, 255, 255);
						tiles[l].TextureIndex = (short)data.Int32();
					}

					cube[0] = data.Float(); cube[1] = data.Float(); cube[2] = data.Float(); cube[3] = data.Float();
					data.Forward(8);

					for (int l = 0; l < 3; l++)
						for (int j = 0; j < 4; j++)
							for (int k = 0; k < 2; k++)
								tiles[l].TexCoords[j][k] = data.Float();

					cube.TileUp = cube.TileSide = cube.TileFront = -1;

					if (tiles[0].TextureIndex > -1) {
						cube.TileUp = Tiles.Count;
						Tiles.Add(tiles[0]);
					}
					if (tiles[1].TextureIndex > -1) {
						cube.TileSide = Tiles.Count;
						Tiles.Add(tiles[1]);
					}
					if (tiles[2].TextureIndex > -1) {
						cube.TileFront = Tiles.Count;
						Tiles.Add(tiles[2]);
					}

					Cubes.Add(cube);
					cube.CalculateNormal();
				}
			}
			else {
				for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
					Cubes.Add(new Cube(Header, data));
				}
			}
			
			for (int x = 0; x < Header.Width; x++)
				for (int y = 0; y < Header.Height; y++)
					this[x, y].CalcNormals(this, x, y);
		}

		private void _loadTiles(IBinaryReader data) {
			if (Header.Version <= 1)
				return;

			int count = data.Int32();
			Tiles.Capacity = count;

			for (int i = 0; i < count; i++) {
				Tiles.Add(new Tile(data));
			}
		}

		private void _loadLightmaps(IBinaryReader data) {
			if (Header.Version <= 1)
				return;

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
				RswWater defWWater = new RswWater();

				defWWater.Level = data.Float();
				defWWater.Type = data.Int32();
				defWWater.WaveHeight = data.Float();
				defWWater.WaveSpeed = data.Float();
				defWWater.WavePitch = data.Float();
				defWWater.TextureCycling = data.Int32();

				Water.WaterSplitWidth = data.Int32();
				Water.WaterSplitHeight = data.Int32();

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
					Water.Zones.Clear();

					int count = Water.WaterSplitWidth * Water.WaterSplitHeight;
					for (int i = 0; i < count; i++) {
						RswWater waterSub = new RswWater(defWWater);
						waterSub.Level = data.Float();
						Water.Zones.Add(waterSub);
					}
				}
			}
		}

		public int LightmapOffset() {
			return LightmapWidth * LightmapHeight;
		}

		public Vector3 RayCast(Ray ray, bool showBlackTiles, bool pickCubeMode) {
			if (Cubes.Count == 0)
				return new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

			float rayOffset = 0;

			int chunkSize = 10;
			List<Vector3> collisions = new List<Vector3>();
			float f = 0;

			for (int xx = 0; xx < Width; xx += chunkSize) {
				for (int yy = 0; yy < Height; yy += chunkSize) {
					//math::AABB box(glm::vec3(10*(xx-1), -999999, 10*height - 10*((yy+chunkSize+1))), glm::vec3(10*(xx + chunkSize+1), 999999, 10*height - (10 * (yy-1))));
					AABB box = new AABB(new Vector3(10 * (xx - 1), -999999, 10 * Height - 10 * ((yy + chunkSize + 1))), new Vector3(10 * (xx + chunkSize + 1), 999999, 10 * Height - (10 * (yy - 1))));

					if (!box.HasRayCollision(ray, -999999, 9999999))
						continue;

					for (int x = xx; x < Math.Min(Width, xx + chunkSize); x++) {
						for (int y = yy; y < Math.Min(Height, yy + chunkSize); y++) {
							var cube = this[x, y];

							if (cube.TileUp != -1 || showBlackTiles || pickCubeMode) {
								var v1 = new Vector3(10 * x, -cube[2], 10 * Height - 10 * y);
								var v2 = new Vector3(10 * x + 10, -cube[3], 10 * Height - 10 * y);
								var v3 = new Vector3(10 * x, -cube[0], 10 * Height - 10 * y + 10);
								var v4 = new Vector3(10 * x + 10, -cube[1], 10 * Height - 10 * y + 10);

								List<Vector3> v = new List<Vector3> { v4, v2, v1, v4, v1, v3 };
								if (ray.LineIntersectPolygon(v, 0, ref f))
									if (f >= rayOffset)
										collisions.Add(ray.Origin + f * ray.Dir);
								if (ray.LineIntersectPolygon(v, 3, ref f))
									if (f >= rayOffset)
										collisions.Add(ray.Origin + f * ray.Dir);
							}

							if ((cube.TileSide != -1 || pickCubeMode) && x < Width - 1) {
								var v1 = new Vector3(10 * x + 10, -cube[3], 10 * Height - 10 * y);
								var v2 = new Vector3(10 * x + 10, -this[x + 1, y][2], 10 * Height - 10 * y);
								var v3 = new Vector3(10 * x + 10, -cube[1], 10 * Height - 10 * y + 10);
								var v4 = new Vector3(10 * x + 10, -this[x + 1, y][0], 10 * Height - 10 * y + 10);

								List<Vector3> v = new List<Vector3> { v4, v2, v1, v4, v1, v3 };
								if (ray.LineIntersectPolygon(v, 0, ref f))
									if (f >= rayOffset)
										collisions.Add(ray.Origin + f * ray.Dir);
								if (ray.LineIntersectPolygon(v, 3, ref f))
									if (f >= rayOffset)
										collisions.Add(ray.Origin + f * ray.Dir);
							}

							if ((cube.TileFront != -1 || pickCubeMode) && y < Height - 1) {
								var v1 = new Vector3(10 * x, -cube[2], 10 * Height - 10 * y);
								var v2 = new Vector3(10 * x + 10, -cube[3], 10 * Height - 10 * y);
								var v3 = new Vector3(10 * x, -this[x, y + 1][0], 10 * Height - 10 * y);
								var v4 = new Vector3(10 * x + 10, -this[x, y + 1][1], 10 * Height - 10 * y);

								List<Vector3> v = new List<Vector3> { v4, v2, v1, v4, v1, v3 };
								if (ray.LineIntersectPolygon(v, 0, ref f))
									if (f >= rayOffset)
										collisions.Add(ray.Origin + f * ray.Dir);
								if (ray.LineIntersectPolygon(v, 3, ref f))
									if (f >= rayOffset)
										collisions.Add(ray.Origin + f * ray.Dir);
							}
						}
					}
				}
			}

			if (collisions.Count == 0)
				return new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

			collisions.Sort(delegate(Vector3 a, Vector3 b) {
				return Vector3.Distance(a, ray.Origin) < Vector3.Distance(b, ray.Origin) ? -1 : 1;
			});

			return collisions[0];
		}
	}

	public class Tile {
		public TkVector4 Color;
		public Vector2[] TexCoords = new Vector2[4];
		public Int16 TextureIndex;
		public UInt16 LightmapIndex;

		public Tile() {
		}

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
			Color = new TkVector4 { Z = data.Byte(), Y = data.Byte(), X = data.Byte(), W = data.Byte() };
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
		public Vector3[] NormalsLocal = new Vector3[4];

		public Cube() {
		}

		public Cube(GndHeader header, IBinaryReader data) {
			_heights[0] = data.Float(); _heights[1] = data.Float(); _heights[2] = data.Float(); _heights[3] = data.Float();

			if (header.Version > 1.5) {
				TileUp = data.Int32();
				TileFront = data.Int32();
				TileSide = data.Int32();
			}
			else {
				TileUp = data.Int16();
				TileFront = data.Int16();
				TileSide = data.Int16();
				data.Forward(2);
			}

			CalculateNormal();
		}

		public float this[int index] {
			get { return _heights[index]; }
			set { _heights[index] = value; }
		}

		public void CalculateNormal() {
			Vector3 v1 = new Vector3(0, _heights[0], 0);
			Vector3 v2 = new Vector3(10, _heights[1], 0);
			Vector3 v3 = new Vector3(0, _heights[2], 10);
			Vector3 v4 = new Vector3(10, _heights[3], 10);

			// Do not normalize these vectors, otherwise the ratio when adding the normals won't be valid
			Vector3 normal1 = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
			Vector3 normal2 = Vector3.Normalize(Vector3.Cross(v3 - v4, v2 - v4));
			Normal = normal1 + normal2;

			Normals[0] = normal1;
			Normals[1] = Normal;
			Normals[2] = Normal;
			Normals[3] = normal1;

			NormalsLocal[0] = normal1;
			NormalsLocal[1] = Normal;
			NormalsLocal[2] = Normal;
			NormalsLocal[3] = normal2;

			Normal = Vector3.Normalize(Normal);
		}

		public void CalcNormals(Gnd gnd, int x, int y) {
			for (int i = 0; i < 4; i++) {
				float h = _heights[i];

				for (int ii = 1; ii < 4; ii++) {
					int xx = (ii % 2) * ((i % 2 == 0) ? -1 : 1);
					int yy = (ii / 2) * (i < 2 ? -1 : 1);

					if (gnd[x + xx, y + yy] != null) {
						int ci = (i + ii * (1 - 2 * (i & 1))) & 3;

						if (gnd[x + xx, y + yy]._heights[ci] != h)
							continue;

						Normals[i] += gnd[x + xx, y + yy].NormalsLocal[ci];
					}
				}
				
				Normals[i] = Vector3.NormalizeFast(Normals[i]);
			}
		}
	}
}
