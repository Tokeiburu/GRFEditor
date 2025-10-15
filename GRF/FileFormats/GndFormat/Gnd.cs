﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats.RswFormat;
using GRF.Image;
using GRF.IO;
using Utilities;
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
			LightmapSizeCell = 1;
			LightmapWidth = 8;
			LightmapHeight = 8;
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
		public int LightmapWidth { get; set; }

		/// <summary>
		/// Gets the lightmap detail height per tile.
		/// </summary>
		public int LightmapHeight { get; set; }

		/// <summary>
		/// Gets the number of lightmap cells per tile.
		/// </summary>
		public int LightmapSizeCell { get; set; }

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
				LightmapWidth = 8;
				LightmapHeight = 8;
				LightmapSizeCell = 1;

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
			LightmapWidth = data.Int32();
			LightmapHeight = data.Int32();
			LightmapSizeCell = data.Int32();
			PerCell = LightmapWidth * LightmapHeight * LightmapSizeCell;
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
			stream.Write(LightmapWidth);
			stream.Write(LightmapHeight);
			stream.Write(LightmapSizeCell);

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

		public void RemoveLight() {
			foreach (var container in LightmapContainer.Lightmaps) {
				for (int i = 64; i < 256; i++) {
					container.Data[i] = 0;
				}
			}
		}

		public void RemoveShadow() {
			foreach (var container in LightmapContainer.Lightmaps) {
				for (int i = 0; i < 64; i++) {
					container.Data[i] = 255;
				}
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

		public void RemoveTileColor() {
			var defColor = new GrfColor(255, 255, 255, 255);

			foreach (var tile in _tiles) {
				tile.TileColor = defColor;
			}
		}

		public class LightComparer : IEqualityComparer<byte[]> {
			
			public bool Equals(byte[] x, byte[] y) {
				return Methods.ByteArrayCompare(x, y);
			}

			public int GetHashCode(byte[] obj) {
				if (obj.Length != 256)
					return obj[0] | obj[obj.Length / 4] << 8 | obj[obj.Length / 2] << 16 | obj[3 * obj.Length / 4] << 24;
				
				int h = 0;

				for (int i = 0; i < 32; i++)
					h |= (obj[i * 8] & 0x1) << i;

				return h;
			}
		}

		public void CleanDuplicateTiles() {
			var comparer = new LightComparer();
			Dictionary<byte[], ushort> light_uid2index = new Dictionary<byte[], ushort>(comparer);

			foreach (var light in LightmapContainer.Lightmaps) {
				ushort v;

				if (!light_uid2index.TryGetValue(light.Data, out v)) {
					light_uid2index[light.Data] = (ushort)light_uid2index.Count;
				}
			}

			List<byte[]> outputLights = light_uid2index.Keys.ToList();

			for (int i = 0; i < Cubes.Count; i++) {
				var cube = Cubes[i];

				if (cube.TileUp > -1) {
					var tile = Tiles[cube.TileUp];
					tile.LightmapIndex = light_uid2index[LightmapContainer[tile.LightmapIndex].Data];
				}

				if (cube.TileSide > -1) {
					var tile = Tiles[cube.TileSide];
					tile.LightmapIndex = light_uid2index[LightmapContainer[tile.LightmapIndex].Data];
				}

				if (cube.TileFront > -1) {
					var tile = Tiles[cube.TileFront];
					tile.LightmapIndex = light_uid2index[LightmapContainer[tile.LightmapIndex].Data];
				}
			}

			LightmapContainer.Lightmaps.Clear();
			LightmapContainer.Lightmaps.AddRange(outputLights.Select(p => new Lightmap(p)));

			Z.F();
		}

		public void CleanupLightmaps() {
			Dictionary<int, List<int>> light2lightmapIndex = new Dictionary<int, List<int>>();
			Dictionary<int, ushort> oldIndex2newIndex = new Dictionary<int, ushort>();
			List<Lightmap> newLightmaps = new List<Lightmap>();

			var lightmaps = LightmapContainer.Lightmaps;

			for (int i = 0; i < lightmaps.Count; i++) {
				int hash = lightmaps[i].Hash(this);
				bool found = false;

				if (light2lightmapIndex.ContainsKey(hash)) {
					foreach (var ii in light2lightmapIndex[hash]) {
						if (lightmaps[i] == lightmaps[ii]) {
							oldIndex2newIndex[i] = oldIndex2newIndex[ii];
							found = true;
							break;
						}
					}
				}

				if (!found) {
					if (!light2lightmapIndex.ContainsKey(hash))
						light2lightmapIndex[hash] = new List<int>();

					light2lightmapIndex[hash].Add(i);
					oldIndex2newIndex[i] = (ushort)newLightmaps.Count;
					newLightmaps.Add(new Lightmap(lightmaps[i].Data));
				}
			}

			LightmapContainer.Lightmaps.Clear();
			LightmapContainer.Lightmaps.AddRange(newLightmaps);

			foreach (var tile in Tiles) {
				tile.LightmapIndex = oldIndex2newIndex[tile.LightmapIndex];
			}
		}

		public static GrfImage GenerateShadowMap(int shadowmapSize, int off, int lightmapWidth, int lightmapHeight, int lightmapSizeCell, List<byte[]> lightmaps) {
			byte[] data = new byte[shadowmapSize * shadowmapSize * 4];

			int xs = 0;
			int ys = 0;
			int alphaSize = lightmapWidth * lightmapHeight * lightmapSizeCell;
			int minLightmapSize = alphaSize * 4;

			unsafe {
				fixed (byte* pDataBase = data) {
					byte* pData = pDataBase;

					for (int i = 0; i < lightmaps.Count; i++) {
						var lightMap = lightmaps[i];

						int xxs = xs;
						int yys = ys;

						if (lightMap.Length >= minLightmapSize) {
							fixed (byte* pLightmapBase = lightMap) {
								byte* pLightmapAlpha = pLightmapBase;
								byte* pLightmap = pLightmapBase + alphaSize;

								for (int yy = 0; yy < lightmapHeight; yy++) {
									for (int xx = 0; xx < lightmapWidth; xx++) {
										int xxx = xxs + xx;
										int yyy = yys + yy;

										int off1 = (xxx + shadowmapSize * yyy);

										pData[4 * off1 + 0] = pLightmap[2];
										pData[4 * off1 + 1] = pLightmap[1];
										pData[4 * off1 + 2] = pLightmap[0];
										pData[4 * off1 + 3] = *pLightmapAlpha;

										pLightmap += 3;
										pLightmapAlpha++;
									}
								}
							}
						}

						xs += lightmapWidth;

						if (xs >= shadowmapSize) {
							xs = 0;
							ys += lightmapHeight;

							if (ys >= shadowmapSize) {
								ys = 0;
							}
						}
					}
				}
			}

			return new GrfImage(ref data, shadowmapSize, shadowmapSize, GrfImageType.Bgra32);
		}
	}
}