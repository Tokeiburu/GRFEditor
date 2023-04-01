using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.Graphics;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.GndFormat {
	/// <summary>
	/// Ground textures for a map.
	/// </summary>
	public class Gnd : IPrintable, IWriteableFile {
		private readonly List<Cube> _cubes = new List<Cube>();
		private readonly List<string> _texturesPath = new List<string>();
		private readonly Dictionary<string, short> _texturesPathDico = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);
		private readonly List<Tile> _tiles = new List<Tile>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Gnd" /> class.
		/// </summary>
		/// <param name="sizeX">The size X.</param>
		/// <param name="sizeY">The size Y.</param>
		public Gnd(int sizeX, int sizeY) {
			LightmapContainer = new LightmapContainer(this);

			Header = new GndHeader(sizeX, sizeY);
			GridSizeCell = 1;
			GridSizeX = 8;
			GridSizeY = 8;
			AddTexture("backside.bmp");

			for (int i = 0; i < sizeX * sizeY; i++) {
				Cube cube = new Cube();
				_cubes.Add(cube);
			}

			RemoveLightmaps();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gnd" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Gnd(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		private Gnd(IBinaryReader data) {
			LightmapContainer = new LightmapContainer(this);

			Header = new GndHeader(data);
			_loadTexturesPath(data);
			_loadLightmaps(data);
			_loadTiles(data);
			_loadCubes(data);
		}

		/// <summary>
		/// Gets the <see cref="Cube" /> with the specified coordinates.
		/// </summary>
		/// <param name="x">The x offset.</param>
		/// <param name="y">The y offset.</param>
		/// <returns>The cube at the position (x, y).</returns>
		public Cube this[int x, int y] {
			get {
				if (x < 0) return null;
				if (y < 0) return null;
				if (x >= Header.Width) return null;
				if (y >= Header.Height) return null;
				return _cubes[x + Header.Width * y];
			}
		}

		/// <summary>
		/// Gets the GND header.
		/// </summary>
		public GndHeader Header { get; private set; }

		/// <summary>
		/// Gets the lightmap detail width per tile.
		/// </summary>
		public int GridSizeX { get; internal set; }

		/// <summary>
		/// Gets the lightmap detail height per tile.
		/// </summary>
		public int GridSizeY { get; internal set; }

		/// <summary>
		/// Gets the number of lightmap cells per tile.
		/// </summary>
		public int GridSizeCell { get; private set; }

		/// <summary>
		/// Gets or sets the number of lightmaps.
		/// </summary>
		internal int NumberOfLightmaps { get; set; }

		/// <summary>
		/// Gets or sets the number of tiles.
		/// </summary>
		internal int NumberOfTiles { get; set; }

		/// <summary>
		/// Gets the number of colors per tile.
		/// </summary>
		public int PerCell { get; private set; }

		/// <summary>
		/// Gets the textures path.
		/// </summary>
		public ReadOnlyCollection<string> TexturesPath {
			get { return _texturesPath.AsReadOnly(); }
		}

		public List<string> Textures {
			get { return _texturesPath; }
		}

		/// <summary>
		/// Gets the tiles.
		/// </summary>
		public List<Tile> Tiles {
			get { return _tiles; }
		}

		/// <summary>
		/// Gets the cubes.
		/// </summary>
		public List<Cube> Cubes {
			get { return _cubes; }
		}

		/// <summary>
		/// Gets the lightmap container.
		/// </summary>
		public LightmapContainer LightmapContainer { get; private set; }

		#region IPrintable Members

		/// <summary>
		/// Prints an object to a text format.
		/// </summary>
		/// <returns>
		/// A string containing the object parsed to a text format.
		/// </returns>
		public string GetInformation() {
			return FileFormatParser.DisplayObjectProperties(this);
		}

		#endregion

		#region IWriteableFile Members

		/// <summary>
		/// Gets or sets the loaded file path of this object.
		/// </summary>
		public string LoadedPath { get; set; }

		/// <summary>
		/// Saves this object from the LoadedPath.
		/// </summary>
		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "LoadedPath");
			Save(LoadedPath);
		}

		/// <summary>
		/// Saves the specified path.
		/// </summary>
		/// <param name="path">The path.</param>
		public void Save(string path) {
			using (BinaryWriter stream = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
				_save(stream);
			}
		}

		/// <summary>
		/// Saves this object to the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public void Save(Stream stream) {
			_save(new BinaryWriter(stream));
		}

		#endregion

		/// <summary>
		/// Resets the textures.
		/// </summary>
		public void ResetTextures() {
			_texturesPath.Clear();
			_texturesPathDico.Clear();

			AddTexture("backside.bmp");

			foreach (Tile tile in Tiles) {
				tile.TextureIndex = (short) (tile.TextureIndex < 0 ? tile.TextureIndex : 0);
			}
		}

		private void _loadCubes(IBinaryReader data) {
			_cubes.Capacity = Header.Width * Header.Height;

			for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
				_cubes.Add(new Cube(data));
			}
		}

		private void _loadTiles(IBinaryReader data) {
			NumberOfTiles = data.Int32();
			Tiles.Capacity = NumberOfTiles;

			for (int i = 0; i < NumberOfTiles; i++) {
				Tile tile = new Tile(data);

				_tiles.Add(tile);
			}
		}

		private void _loadLightmaps(IBinaryReader data) {
			NumberOfLightmaps = data.Int32();
			GridSizeX = data.Int32();
			GridSizeY = data.Int32();
			GridSizeCell = data.Int32();
			PerCell = GridSizeX * GridSizeY * GridSizeCell;
			int size = PerCell * 4;

			for (int i = 0; i < NumberOfLightmaps; i++) {
				LightmapContainer.Add(data.Bytes(size));
			}
		}

		private void _loadTexturesPath(IBinaryReader data) {
			_texturesPath.Capacity = Header.TextureCount;

			for (int i = 0; i < Header.TextureCount; i++) {
				var path = data.String(Header.TexturePathSize, '\0');
				_texturesPath.Add(path);
				_texturesPathDico[path] = (short) (_texturesPath.Count - 1);
			}
		}

		public string AddTexture(string name) {
			if (!_texturesPathDico.ContainsKey(name)) {
				_texturesPath.Add(name);
				_texturesPathDico.Add(name, (short) (_texturesPath.Count - 1));
				Header.TextureCount = _texturesPath.Count;
			}

			return name;
		}

		public void ReplaceAllTextures(string name) {
			for (int i = 0; i < _texturesPath.Count; i++) {
				_texturesPath[i] = name;
			}
		}

		/// <summary>
		/// Gets the index of the texture.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The index of the texture</returns>
		public short GetTextureIndex(string name) {
			short v;
			if (_texturesPathDico.TryGetValue(name, out v)) {
				return v;
			}
			return -1;
		}

		private void _save(BinaryWriter stream) {
			Header.Write(stream);

			for (int i = 0; i < Header.TextureCount; i++) {
				stream.WriteANSI(_texturesPath[i], Header.TexturePathSize);
			}

			stream.Write(LightmapContainer.Lightmaps.Count);
			stream.Write(GridSizeX);
			stream.Write(GridSizeY);
			stream.Write(GridSizeCell);

			foreach (Lightmap light in LightmapContainer.Lightmaps) {
				stream.Write(light.Data);
			}

			stream.Write(_tiles.Count);

			foreach (Tile tile in _tiles) {
				tile.Write(stream);
			}

			for (int i = 0; i < Header.Width * Header.Height; i++) {
				_cubes[i].Write(stream);
			}
		}

		/// <summary>
		/// Sets the floor height level.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetFloorHeightLevel(float height) {
			foreach (Cube cube in _cubes) {
				cube.SetHeight(height);
			}
		}

		/// <summary>
		/// Removes a tile from its index.
		/// </summary>
		/// <param name="index">The index.</param>
		public void RemoveTile(int index) {
			_tiles.RemoveAt(index);
		}

		/// <summary>
		/// Removes all tiles.
		/// </summary>
		public void RemoveAllTiles() {
			_tiles.Clear();
		}

		/// <summary>
		/// Adds a tile.
		/// </summary>
		/// <param name="tile">The tile.</param>
		public void AddTile(Tile tile) {
			_tiles.Add(tile);
		}

		/// <summary>
		/// Removes the lightmaps and sets a default one (at least one lightmap must be present).
		/// </summary>
		public void RemoveLightmaps() {
			LightmapContainer.Lightmaps.Clear();

			byte[] light = new byte[256];

			for (int i = 0; i < 64; i++) {
				light[i] = 255;
			}

			LightmapContainer.Add(light);

			foreach (Tile tile in _tiles) {
				tile.LightmapIndex = 0;
			}
		}

		public byte[] CreateLightmapImage() {
			int x = 0;
			int y = 0;

			Lightmap light;

			int count = LightmapContainer.Lightmaps.Count;
			byte[] data = new byte[2048 * 2048 * 4];

			for (int i = 0; i < count; i++) {
				try {
					light = LightmapContainer[i];
					//x = (i % width) * 8;
					//y = (i / width | 0) * 8;

					for (int xx = 0; xx < 8; xx++) {
						for (int yy = 0; yy < 8; yy++) {
							try {
								int xxx = 8 * x + xx;
								int yyy = 8 * y + yy;

								//idx = 4 * (xxx + Header.Width * 8 * yyy);
								data[4 * (xxx + 2048 * yyy) + 0] = (byte) ((light.Data[64 + 3 * (xx + 8 * yy) + 0] >> 4) << 4);
								data[4 * (xxx + 2048 * yyy) + 1] = (byte) ((light.Data[64 + 3 * (xx + 8 * yy) + 1] >> 4) << 4);
								data[4 * (xxx + 2048 * yyy) + 2] = (byte) ((light.Data[64 + 3 * (xx + 8 * yy) + 2] >> 4) << 4);
								data[4 * (xxx + 2048 * yyy) + 3] = light.Data[xx + 8 * yy];
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
							}
						}
					}
					x++;

					if (x * 8 >= 2048) {
						x = 0;
						y++;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			return data;
		}

		public GndMesh Compile(float waterLevel, float waterHeight, bool ignoreNormals) {
			Dictionary<int, List<Vertex>> normals = _getSmoothNormals(ignoreNormals);
			List<Vertex> n;
			Tile tile;
			Cube cell_a, cell_b;
			int x, y;
			float[] h_a, h_b;

			int height = Header.Height;
			int width = Header.Width;

			// Water
			//List<float[]> mesh = new List<float[]>();
			//List<float[]> water = new List<float[]>();

			List<MeshRawData> meshRawData = new List<MeshRawData>();
			List<int> meshOffsets = new List<int>();

			//Dictionary<string, MeshRawData> waterRawData = new Dictionary<string, MeshRawData>();

			//List<MeshTriangle> waterMeshTriangles = new List<MeshTriangle>();
			//string waterTexture = @"¿öÅÍ\water000.jpg";
			//waterRawData[waterTexture] = new MeshRawData {
			//    Alpha = 150,
			//    Texture = waterTexture
			//};

			List<int> numberOfTriangles = new List<int>(TexturesPath.Count);

			for (int i = 0; i < TexturesPath.Count; i++) {
				numberOfTriangles.Add(0);
				meshOffsets.Add(0);
				meshRawData.Add(null);
			}

			for (y = 0; y < height; y++) {
				for (x = 0; x < width; x++) {
					cell_a = _cubes[x + y * width];

					if (cell_a.TileUp > -1) {
						tile = _tiles[cell_a.TileUp];

						if (tile.TextureIndex > -1) {
							numberOfTriangles[tile.TextureIndex]++;
						}
					}

					if (cell_a.TileFront > -1) {
						tile = _tiles[cell_a.TileFront];

						if (tile.TextureIndex > -1 && _cubes.Count > (x + (y + 1) * width)) {
							numberOfTriangles[tile.TextureIndex]++;
						}
					}

					if (cell_a.TileRight > -1) {
						tile = _tiles[cell_a.TileRight];

						if (tile.TextureIndex > -1 && _cubes.Count > ((x + 1) + y * width)) {
							numberOfTriangles[tile.TextureIndex]++;
						}
					}
				}
			}

			for (int i = 0; i < TexturesPath.Count; i++) {
				var meshRaw = new MeshRawData();
				meshRaw.Texture = TexturesPath[i];
				meshRaw.Alpha = 255;
				meshRaw.MeshTriangles = new MeshTriangle[numberOfTriangles[i] * 2];
				meshRawData[i] = meshRaw;
				meshOffsets[i] = 0;
			}

			// Compiling mesh
			for (y = 0; y < height; y++) {
				for (x = 0; x < width; x++) {
					cell_a = _cubes[x + y * width];
					h_a = new float[] { cell_a.BottomLeft, cell_a.BottomRight, cell_a.TopLeft, cell_a.TopRight };

					// Check tile up
					if (cell_a.TileUp > -1) {
						tile = _tiles[cell_a.TileUp];

						// Check if has texture
						if (tile.TextureIndex > -1) {
							n = normals[x + y * width];

							MeshTriangle face1 = new MeshTriangle();
							face1.Normals[0] = n[0];
							face1.Normals[1] = n[1];
							face1.Normals[2] = n[2];
							face1.Positions[0] = new Vertex((x + 0) * 2, h_a[0] / 5f, (y + 0) * 2);
							face1.Positions[1] = new Vertex((x + 1) * 2, h_a[1] / 5f, (y + 0) * 2);
							face1.Positions[2] = new Vertex((x + 1) * 2, h_a[3] / 5f, (y + 1) * 2);
							face1.TextureCoords[0] = new Point(tile.U1, tile.V1);
							face1.TextureCoords[1] = new Point(tile.U2, tile.V2);
							face1.TextureCoords[2] = new Point(tile.U4, tile.V4);

							MeshTriangle face2 = new MeshTriangle();
							face2.Normals[0] = n[2];
							face2.Normals[1] = n[3];
							face2.Normals[2] = n[0];
							face2.Positions[0] = new Vertex((x + 1) * 2, h_a[3] / 5f, (y + 1) * 2);
							face2.Positions[1] = new Vertex((x + 0) * 2, h_a[2] / 5f, (y + 1) * 2);
							face2.Positions[2] = new Vertex((x + 0) * 2, h_a[0] / 5f, (y + 0) * 2);
							face2.TextureCoords[0] = new Point(tile.U4, tile.V4);
							face2.TextureCoords[1] = new Point(tile.U3, tile.V3);
							face2.TextureCoords[2] = new Point(tile.U1, tile.V1);

							var textIndex = tile.TextureIndex;
							meshRawData[textIndex].MeshTriangles[meshOffsets[textIndex]++] = face1;
							meshRawData[textIndex].MeshTriangles[meshOffsets[textIndex]++] = face2;
						}
					}

					// Check tile front
					if (cell_a.TileFront > -1) {
						tile = _tiles[cell_a.TileFront];

						// Check if has texture
						if (tile.TextureIndex > -1 && _cubes.Count > (x + (y + 1) * width)) {
							cell_b = _cubes[x + (y + 1) * width];
							h_b = new float[] { cell_b.BottomLeft, cell_b.BottomRight, cell_b.TopLeft, cell_b.TopRight };
							//generateLightmapAltlas(tile.LightmapIndex);

							float mult = h_a[0] < h_b[0] ? 1f : -1f;

							MeshTriangle face1 = new MeshTriangle();
							face1.Normals[0] = new Vertex(0.0f, 0.0f, 1.0f * mult);
							face1.Normals[1] = new Vertex(0.0f, 0.0f, 1.0f * mult);
							face1.Normals[2] = new Vertex(0.0f, 0.0f, 1.0f * mult);
							face1.Positions[0] = new Vertex((x + 0) * 2, h_b[0] / 5f, (y + 1) * 2);
							face1.Positions[1] = new Vertex((x + 1) * 2, h_a[3] / 5f, (y + 1) * 2);
							face1.Positions[2] = new Vertex((x + 1) * 2, h_b[1] / 5f, (y + 1) * 2);
							face1.TextureCoords[0] = new Point(tile.U3, tile.V3);
							face1.TextureCoords[1] = new Point(tile.U2, tile.V2);
							face1.TextureCoords[2] = new Point(tile.U4, tile.V4);

							MeshTriangle face2 = new MeshTriangle();
							face2.Normals[0] = new Vertex(0.0f, 0.0f, -1.0f * mult);
							face2.Normals[1] = new Vertex(0.0f, 0.0f, -1.0f * mult);
							face2.Normals[2] = new Vertex(0.0f, 0.0f, -1.0f * mult);
							face2.Positions[0] = new Vertex((x + 0) * 2, h_b[0] / 5f, (y + 1) * 2);
							face2.Positions[1] = new Vertex((x + 1) * 2, h_a[3] / 5f, (y + 1) * 2);
							face2.Positions[2] = new Vertex((x + 0) * 2, h_a[2] / 5f, (y + 1) * 2);
							face2.TextureCoords[0] = new Point(tile.U3, tile.V3);
							face2.TextureCoords[1] = new Point(tile.U2, tile.V2);
							face2.TextureCoords[2] = new Point(tile.U1, tile.V1);

							var textIndex = tile.TextureIndex;
							meshRawData[textIndex].MeshTriangles[meshOffsets[textIndex]++] = face1;
							meshRawData[textIndex].MeshTriangles[meshOffsets[textIndex]++] = face2;
						}
					}

					// Check tile right
					if (cell_a.TileRight > -1) {
						tile = _tiles[cell_a.TileRight];

						// Check if has texture
						if (tile.TextureIndex > -1 && _cubes.Count > ((x + 1) + y * width)) {
							cell_b = _cubes[(x + 1) + y * width];
							h_b = new float[] { cell_b.BottomLeft, cell_b.BottomRight, cell_b.TopLeft, cell_b.TopRight };

							float mult = h_a[0] > h_b[0] ? 1f : -1f;

							MeshTriangle face1 = new MeshTriangle();
							face1.Normals[0] = new Vertex(1.0f * mult, 0.0f, 0.0f);
							face1.Normals[1] = new Vertex(1.0f * mult, 0.0f, 0.0f);
							face1.Normals[2] = new Vertex(1.0f * mult, 0.0f, 0.0f);
							face1.Positions[0] = new Vertex((x + 1) * 2, h_a[1] / 5f, (y + 0) * 2);
							face1.Positions[1] = new Vertex((x + 1) * 2, h_a[3] / 5f, (y + 1) * 2);
							face1.Positions[2] = new Vertex((x + 1) * 2, h_b[0] / 5f, (y + 0) * 2);
							face1.TextureCoords[0] = new Point(tile.U2, tile.V2);
							face1.TextureCoords[1] = new Point(tile.U1, tile.V1);
							face1.TextureCoords[2] = new Point(tile.U4, tile.V4);

							MeshTriangle face2 = new MeshTriangle();
							face2.Normals[0] = new Vertex(-1.0f * mult, 0.0f, 0.0f);
							face2.Normals[1] = new Vertex(-1.0f * mult, 0.0f, 0.0f);
							face2.Normals[2] = new Vertex(-1.0f * mult, 0.0f, 0.0f);
							face2.Positions[0] = new Vertex((x + 1) * 2, h_b[0] / 5f, (y + 0) * 2);
							face2.Positions[1] = new Vertex((x + 1) * 2, h_b[2] / 5f, (y + 1) * 2);
							face2.Positions[2] = new Vertex((x + 1) * 2, h_a[3] / 5f, (y + 1) * 2);
							face2.TextureCoords[0] = new Point(tile.U4, tile.V4);
							face2.TextureCoords[1] = new Point(tile.U3, tile.V3);
							face2.TextureCoords[2] = new Point(tile.U1, tile.V1);

							var textIndex = tile.TextureIndex;
							meshRawData[textIndex].MeshTriangles[meshOffsets[textIndex]++] = face1;
							meshRawData[textIndex].MeshTriangles[meshOffsets[textIndex]++] = face2;
						}
					}
				}
			}

			//waterRawData[waterTexture].MeshTriangles = waterMeshTriangles.ToArray();
			Dictionary<string, MeshRawData> meshRawDataDico = new Dictionary<string, MeshRawData>();

			for (int i = 0; i < TexturesPath.Count; i++) {
				meshRawDataDico[TexturesPath[i]] = meshRawData[i];
			}

			// Return mesh informations
			return new GndMesh {
				Width = Header.Width,
				Height = Header.Height,

				//Lightmap = lightmap,
				//LightmapSize = lightmap.Length,
				//TileColor = CreateTilesColorImage(),
				//ShadowMap = CreateShadowMapData(),

				//Mesh = mesh,
				//MeshVertCount = mesh.Count / 12,
				MeshRawData = meshRawDataDico,
				//WaterMesh = water,
				//WaterVertCount = water.Count / 5,

				//WaterRawData = waterRawData,
			};
		}

		private Dictionary<int, List<Vertex>> _getSmoothNormals(bool ignoreNormals) {
			int x, y;
			Vertex a = new Vertex();
			Vertex b = new Vertex();
			Vertex c = new Vertex();
			Vertex d = new Vertex();
			List<Vertex> n;

			Dictionary<int, Vertex> tmp = new Dictionary<int, Vertex>();
			Dictionary<int, List<Vertex>> normals = new Dictionary<int, List<Vertex>>();

			Vertex emptyVec = new Vertex();
			Cube cell;

			// Calculate normal for each cells
			for (y = 0; y < Header.Height; y++) {
				for (x = 0; x < Header.Width; x++) {
					tmp[x + y * Header.Width] = new Vertex();

					if (!ignoreNormals)
						continue;

					cell = _cubes[x + y * Header.Width];

					// Tile Up
					if (cell.TileUp > -1 && _tiles[cell.TileUp].TextureIndex > -1) {
						a[0] = (x + 0) * 2;
						a[1] = cell.BottomLeft;
						a[2] = (y + 0) * 2;
						b[0] = (x + 1) * 2;
						b[1] = cell.BottomRight;
						b[2] = (y + 0) * 2;
						c[0] = (x + 1) * 2;
						c[1] = cell.TopLeft;
						c[2] = (y + 1) * 2;
						d[0] = (x + 0) * 2;
						d[1] = cell.TopRight;
						d[2] = (y + 1) * 2;

						tmp[x + y * Header.Width] = Vertex.CalculateNormal(a, b, c, d);
					}
				}
			}

			// Smooth normals
			for (y = 0; y < Header.Height; y++) {
				for (x = 0; x < Header.Width; x++) {
					n = normals[x + y * Header.Width] = new List<Vertex> { new Vertex(), new Vertex(), new Vertex(), new Vertex() };

					if (ignoreNormals)
						continue;

					// Up Left
					n[0] = n[0] + tmp[(x + 0) + (y + 0) * Header.Width];
					n[0] = n[0] + (tmp.ContainsKey((x - 1) + (y + 0) * Header.Width) ? tmp[(x - 1) + (y + 0) * Header.Width] : emptyVec);
					n[0] = n[0] + (tmp.ContainsKey((x - 1) + (y - 1) * Header.Width) ? tmp[(x - 1) + (y - 1) * Header.Width] : emptyVec);
					n[0] = n[0] + (tmp.ContainsKey((x + 0) + (y - 1) * Header.Width) ? tmp[(x + 0) + (y - 1) * Header.Width] : emptyVec);
					n[0] = Vertex.Normalize(n[0]);

					// Up Right
					n[1] = n[1] + tmp[(x + 0) + (y + 0) * Header.Width];
					n[1] = n[1] + (tmp.ContainsKey((x + 1) + (y + 0) * Header.Width) ? tmp[(x + 1) + (y + 0) * Header.Width] : emptyVec);
					n[1] = n[1] + (tmp.ContainsKey((x + 1) + (y - 1) * Header.Width) ? tmp[(x + 1) + (y - 1) * Header.Width] : emptyVec);
					n[1] = n[1] + (tmp.ContainsKey((x + 0) + (y - 1) * Header.Width) ? tmp[(x + 0) + (y - 1) * Header.Width] : emptyVec);
					n[1] = Vertex.Normalize(n[1]);

					// Bottom Right
					n[2] = n[2] + tmp[(x + 0) + (y + 0) * Header.Width];
					n[2] = n[2] + (tmp.ContainsKey((x + 1) + (y + 0) * Header.Width) ? tmp[(x + 1) + (y + 0) * Header.Width] : emptyVec);
					n[2] = n[2] + (tmp.ContainsKey((x + 1) + (y + 1) * Header.Width) ? tmp[(x + 1) + (y + 1) * Header.Width] : emptyVec);
					n[2] = n[2] + (tmp.ContainsKey((x + 0) + (y + 1) * Header.Width) ? tmp[(x + 0) + (y + 1) * Header.Width] : emptyVec);
					n[2] = Vertex.Normalize(n[2]);

					// Bottom Left
					n[3] = n[3] + tmp[(x + 0) + (y + 0) * Header.Width];
					n[3] = n[3] + (tmp.ContainsKey((x - 1) + (y + 0) * Header.Width) ? tmp[(x - 1) + (y + 0) * Header.Width] : emptyVec);
					n[3] = n[3] + (tmp.ContainsKey((x - 1) + (y + 1) * Header.Width) ? tmp[(x - 1) + (y + 1) * Header.Width] : emptyVec);
					n[3] = n[3] + (tmp.ContainsKey((x + 0) + (y + 1) * Header.Width) ? tmp[(x + 0) + (y + 1) * Header.Width] : emptyVec);
					n[3] = Vertex.Normalize(n[3]);
				}
			}

			return normals;
		}

		/// <summary>
		/// Sets the height of the cubes.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetCubesHeight(float height) {
			for (int i = 0; i < _cubes.Count; i++) {
				_cubes[i].SetHeight(height);
			}
		}
	}
}