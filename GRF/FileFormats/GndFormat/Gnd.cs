using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats.RswFormat;
using GRF.Image;
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
		public WaterData Water = new WaterData();

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
			_loadWater(data);
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
		public int GridSizeX { get; set; }

		/// <summary>
		/// Gets the lightmap detail height per tile.
		/// </summary>
		public int GridSizeY { get; set; }

		/// <summary>
		/// Gets the number of lightmap cells per tile.
		/// </summary>
		public int GridSizeCell { get; set; }

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

			if (Header.Version <= 1) {
				GridSizeX = 8;
				GridSizeY = 8;
				GridSizeCell = 1;

				byte[] light = new byte[256];

				for (int i = 0; i < 64; i++) {
					light[i] = 255;
				}

				LightmapContainer.Add(light);
				
				for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
					Tile[] tiles = new Tile[3] { new Tile(), new Tile(), new Tile() };	// up, front, side
					Cube cube = new Cube();

					for (int l = 0; l < 3; l++) {
						tiles[l].TileColor = new GrfColor(255, 255, 255, 255);
						tiles[l].TextureIndex = (short)data.Int32();
					}

					cube.BottomLeft = data.Float(); cube.BottomRight = data.Float(); cube.TopLeft = data.Float(); cube.TopRight = data.Float();
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
				}
			}
			else {
				for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
					_cubes.Add(new Cube(Header, data));
				}
			}
		}

		private void _loadTiles(IBinaryReader data) {
			if (Header.Version <= 1)
				return;

			NumberOfTiles = data.Int32();
			Tiles.Capacity = NumberOfTiles;

			for (int i = 0; i < NumberOfTiles; i++) {
				Tile tile = new Tile(data);

				_tiles.Add(tile);
			}
		}

		private void _loadLightmaps(IBinaryReader data) {
			if (Header.Version <= 1)
				return;

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

			if (Header.Version >= 1.8) {
				if (Water.Zones.Count == 0)
					throw new Exception("For GND version 1.8 and above, a water must be defined in Gnd.Water.Zones.");

				var defWater = Water.Zones[0];

				stream.Write(defWater.Level);
				stream.Write(defWater.Type);
				stream.Write(defWater.WaveHeight);
				stream.Write(defWater.WaveSpeed);
				stream.Write(defWater.WavePitch);
				stream.Write(defWater.TextureCycling);
				stream.Write(Water.WaterSplitWidth);
				stream.Write(Water.WaterSplitHeight);

				if (Header.Version >= 1.9) {
					foreach (var water in Water.Zones) {
						stream.Write(water.Level);
						stream.Write(water.Type);
						stream.Write(water.WaveHeight);
						stream.Write(water.WaveSpeed);
						stream.Write(water.WavePitch);
						stream.Write(water.TextureCycling);
					}
				}
				else {
					foreach (var water in Water.Zones) {
						stream.Write(water.Level);
					}
				}
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