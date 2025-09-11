using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RswFormat;
using GRF.Image.Decoders;
using GRF.IO;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.Tools.Map {
	public class MapEditorConfig {
		public bool FlattenGround;
		public bool RemoveLight;
		public bool RemoveColor;
		public bool RemoveShadow;
		public bool UseCustomTextures;
		public bool TextureOriginal;
		public bool TextureWalls;
		public bool StickGatCellsToGround;
		public bool ResetGlobalLighting;
		public bool RemoveAllObjects;
		public bool RemoveWater;
		public bool ShowGutterLines;
		public bool MatchShadowsWithGatCells;
		public bool UseShadowsForQuadrants;
	}

	public class MapEditor {
		private MapEditorState _state = new MapEditorState();
		public string OutputTexturePath { get; set; }
		public string InputTexturePath { get; set; }
		public string InputMapPath { get; set; }
		public string OutputMapPath { get; set; }
		private MapEditorConfig _mapConfig = new MapEditorConfig();
		public string[] TextureFiles { get; private set; }

		public MapEditorState State {
			get { return _state; }
		}

		public MapEditor(MapEditorWindow editorWindow) {
			_editorWindow = editorWindow;
		}

		public void Begin() {
			_state = new MapEditorState();
			_copyConfig();
		}

		public void ValidatePaths() {
			try {
				InputMapPath = GrfEditorConfiguration.FlatMapsMakerInputMapsPath;
				OutputMapPath = GrfEditorConfiguration.FlatMapsMakerOutputMapsPath;
				OutputTexturePath = GrfEditorConfiguration.FlatMapsMakerOutputTexturesPath;
				InputTexturePath = GrfEditorConfiguration.FlatMapsMakerInputTexturesPath;

				if (OutputMapPath != null) {
					try {
						if (OutputMapPath.IsExtension(".grf")) {
							GrfPath.CreateDirectoryFromFile(OutputMapPath);
						}
						else {
							Directory.CreateDirectory(OutputMapPath);
						}
					}
					catch {
					}
				}

				Directory.CreateDirectory(InputMapPath);
				Directory.CreateDirectory(OutputTexturePath);
				Directory.CreateDirectory(InputTexturePath);

				TextureFiles = new string[] { "cw.bmp", "c-3.bmp", "c-2.bmp", "c-1.bmp", "cx.bmp", "c0.bmp", "c1.bmp", "c2.bmp", "c3.bmp", "c4.bmp", "c5.bmp", "c6.bmp" };

				foreach (string file in TextureFiles) {
					if (!File.Exists(Path.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, file))) {
						using (Stream resFilestream = typeof(EditorMainWindow).Assembly.GetManifestResourceStream("GRFEditor.Resources." + file)) {
							if (resFilestream != null) {
								byte[] data = new byte[resFilestream.Length];
								resFilestream.Read(data, 0, data.Length);
								File.WriteAllBytes(Path.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, file), data);
							}
						}
					}
				}
			}
			catch (Exception err) {
				_editorWindow.AddException(new MapEditorWindow.MapEditorException("Failed to validate paths.", ErrorLevel.Critical, err));
				throw;
			}
		}

		public void Generate(string mapName, FileStream writer, object threadPoolLock, List<FileEntry> entries) {
			string gatFileName = Path.Combine(InputMapPath, mapName + ".gat");
			string rswFileName = Path.Combine(InputMapPath, mapName + ".rsw");
			string gndFileName = Path.Combine(InputMapPath, mapName + ".gnd");

			byte[] gatData = null;
			byte[] rswData = null;
			byte[] gndData = null;


			try {
				gatData = File.ReadAllBytes(gatFileName);
			}
			catch (Exception err) {
				_editorWindow.AddException(new MapEditorWindow.MapEditorException("File not found: data\\" + mapName + ".gat", ErrorLevel.Warning, err));
			}

			try {
				rswData = File.ReadAllBytes(rswFileName);
			}
			catch (Exception err) {
				_editorWindow.AddException(new MapEditorWindow.MapEditorException("File not found: data\\" + mapName + ".rsw", ErrorLevel.Warning, err));
			}

			try {
				gndData = File.ReadAllBytes(gndFileName);
			}
			catch (Exception err) {
				_editorWindow.AddException(new MapEditorWindow.MapEditorException("File not found: data\\" + mapName + ".gnd", ErrorLevel.Warning, err));
			}

			if (gatData == null || rswData == null || gndData == null)
				return;

			_generate(mapName, gatData, rswData, gndData, writer, threadPoolLock, entries);
		}

		public void Generate(string mapName, byte[] gatData, byte[] rswData, byte[] gndData, FileStream writer, object threadPoolLock, List<FileEntry> entries) {
			_generate(mapName, gatData, rswData, gndData, writer, threadPoolLock, entries);
		}

		private void _generate(string mapName, byte[] gatData, byte[] rswData, byte[] gndData, FileStream writer, object threadPoolLock, List<FileEntry> entries) {
			try {
				float waterLevel = Rsw.WaterLevel(rswData);

				RswHeader rswHeader = new RswHeader(new ByteReader(rswData));

				Gat gat = _configGatFile(gatData, rswHeader, waterLevel);
				Gnd gnd = _configGndFile(gndData, gat);
				Rsw rsw = _configRswFile(rswData, mapName, gnd);

				gat.LoadedPath = "data\\" + mapName + ".gat";
				gnd.LoadedPath = "data\\" + mapName + ".gnd";
				rsw.LoadedPath = "data\\" + mapName + ".rsw";

				_saveFile(gat, writer, threadPoolLock, entries);
				_saveFile(rsw, writer, threadPoolLock, entries);
				_saveFile(gnd, writer, threadPoolLock, entries);
			}
			catch (Exception err) {
				_editorWindow.AddException(new MapEditorWindow.MapEditorException("Failed to generate map: " + mapName, ErrorLevel.Warning, err));
			}
		}

		public void Generate(string mapName, byte[] gatData, byte[] rswData, byte[] gndData, out Rsw rsw, out Gnd gnd) {
			_copyConfig();
			float waterLevel = Rsw.WaterLevel(rswData);

			RswHeader rswHeader = new RswHeader(new ByteReader(rswData));

			Gat gat = _configGatFile(gatData, rswHeader, waterLevel);
			gnd = _configGndFile(gndData, gat);
			rsw = _configRswFile(rswData, mapName, gnd);
		}

		private void _copyConfig() {
			_mapConfig.FlattenGround = GrfEditorConfiguration.FlattenGround;
			_mapConfig.RemoveLight = GrfEditorConfiguration.RemoveLight;
			_mapConfig.RemoveColor = GrfEditorConfiguration.RemoveColor;
			_mapConfig.RemoveShadow = GrfEditorConfiguration.RemoveShadow;
			_mapConfig.UseCustomTextures = GrfEditorConfiguration.UseCustomTextures;
			_mapConfig.TextureOriginal = GrfEditorConfiguration.TextureOriginal;
			_mapConfig.TextureWalls = GrfEditorConfiguration.TextureWalls;
			_mapConfig.StickGatCellsToGround = GrfEditorConfiguration.StickGatCellsToGround;
			_mapConfig.ResetGlobalLighting = GrfEditorConfiguration.ResetGlobalLighting;
			_mapConfig.RemoveAllObjects = GrfEditorConfiguration.RemoveAllObjects;
			_mapConfig.RemoveWater = GrfEditorConfiguration.RemoveWater;
			_mapConfig.ShowGutterLines = GrfEditorConfiguration.ShowGutterLines;
			_mapConfig.MatchShadowsWithGatCells = GrfEditorConfiguration.MatchShadowsWithGatCells;
			_mapConfig.UseShadowsForQuadrants = GrfEditorConfiguration.UseShadowsForQuadrants;
		}

		private void _saveFile(IWriteableFile obj, FileStream writer, object threadPoolLock, List<FileEntry> entries) {
			using (var mem = new MemoryStream()) {
				//Z.Start(27);
				obj.Save(mem);
				//Z.Stop(27);
				var data = mem.ReadAllBytes();
				//Z.Start(28);
				var compressed = Compression.Compress(data);
				//Z.Stop(28);

				FileEntry entry;

				// All threads save to different files, so this is... useless?
				//lock (_state.Lock) {
					var offset = writer.Position;
					writer.Write(compressed, 0, compressed.Length);

					if (compressed.Length % 8 != 0) {
						writer.Write(new byte[8 - compressed.Length % 8], 0, 8 - compressed.Length % 8);
					}

					entry = FileEntry.CreateBufferedEntry(writer.Name, "data\\" + Path.GetFileName(obj.LoadedPath), offset, compressed.Length, (int)(writer.Position - offset), data.Length);
				//}

				lock (threadPoolLock) {
					entries.Add(entry);
				}
			}
		}

		private Gnd _configGndFile(byte[] gndData, Gat gat) {
			//Z.Start(1);
			Gnd gnd = new Gnd(gndData);
			//Z.Stop(1);

			if (gnd.Header.Version >= 1.7) {
				// Doesn't like new maps very much
				gnd.Header.SetVersion(1, 7);
			}

			if (_mapConfig.FlattenGround) {
				gnd.SetCubesHeight(0);

				foreach (var cube in gnd.Cubes) {
					cube.TileFront = -1;
					cube.TileSide = -1;
				}
			}

			if (_mapConfig.MatchShadowsWithGatCells || (_mapConfig.RemoveLight && _mapConfig.RemoveShadow)) {
				gnd.RemoveLightmaps();
			}
			else if (_mapConfig.RemoveLight) {
				gnd.RemoveLight();
			}
			else if (_mapConfig.RemoveShadow) {
				gnd.RemoveShadow();
			}

			if (_mapConfig.UseCustomTextures) {
				if (!_mapConfig.TextureOriginal) {
					gnd.ResetTextures();
				}

				if (_mapConfig.FlattenGround && _mapConfig.RemoveLight && _mapConfig.RemoveShadow && _mapConfig.RemoveColor) {
					gnd.RemoveAllTiles();
				}

				//Z.Start(2);
				_setCubesAndTiles(gnd, gat);
				//Z.Stop(2);
			}
			else if (_mapConfig.MatchShadowsWithGatCells) {
				_gndMatchShadowWithGatCells(gnd, gat);
			}

			if (_mapConfig.RemoveColor && !_mapConfig.MatchShadowsWithGatCells) {
				gnd.RemoveTileColor();
			}

			if (_mapConfig.UseShadowsForQuadrants) {
				_gndUseShadowsWithQuadrants(gnd, gat);
			}

			// Compress tiles
			// Don't bother cleaning up the tiles if they still have lightmaps
			//if ((!_mapConfig.RemoveLight || !_mapConfig.RemoveShadow) && _mapConfig.UseCustomTextures) {
			//	gnd.CleanDuplicateTiles();
			//}

			return gnd;
		}

		private string _uid = "";
		private static Dictionary<int, string> _computedGatTypes2Textures = new Dictionary<int, string>();
		private MapEditorWindow _editorWindow;

		private void _shadowProc(Gnd gnd, int xx, int yy, byte shadow) {
			int subx = xx % gnd.LightmapWidth;
			int suby = yy % gnd.LightmapHeight;
			int cx = xx / gnd.LightmapWidth;
			int cy = yy / gnd.LightmapHeight;

			if (cx < 0 || cx >= gnd.Header.Width ||
				cy < 0 || cy >= gnd.Header.Height ||
				xx < 0 || xx >= gnd.Header.Width * gnd.LightmapWidth ||
				yy < 0 || yy >= gnd.Header.Height * gnd.LightmapHeight)
				return;

			var cube = gnd[cx, cy];

			if (cube.TileUp < 0)
				return;

			var tile = gnd.Tiles[cube.TileUp];
			var data = gnd.LightmapContainer.GetRawLightmap(tile.LightmapIndex);
			data[suby * gnd.LightmapHeight + subx] = shadow;
		}

		private void _setCubesAndTiles(Gnd gnd, Gat gat) {
			string id = GrfEditorConfiguration.FlatMapsMakerId;

			if (id != _uid) {
				_computedGatTypes2Textures.Clear();
				_uid = id;
			}

			if (_mapConfig.TextureWalls) {
				gnd.AddTexture(id + "cw.bmp");

				try {
					if (!_state.WallTextureCopied) {
						_state.WallTextureCopied = true;
						File.Delete(Path.Combine(OutputTexturePath, id + "cw.bmp"));
						File.Copy(Path.Combine(InputTexturePath, "cw.bmp"), Path.Combine(OutputTexturePath, id + "cw.bmp"));
						_state.OutputTexturePaths.Add(id + "cw.bmp");
					}
				}
				catch {
				}
			}

			int offset;
			int[] cellIndexes = new int[4];
			sbyte[] cellTypes = new sbyte[4];
			//string[] cellTypesString = new string[4];
			Tile tile;
			Cube cube;
			Cell gatCell;
			sbyte gatType;

			//Z.Start(3);
			// For each cube...
			for (int y = 0; y < gnd.Header.Height; y++) {
				for (int x = 0; x < gnd.Header.Width; x++) {
					// Find the texture for the tile
					int cellIndex = 2 * (x + 2 * y * gnd.Header.Width);
					cellIndexes[0] = cellIndex;
					cellIndexes[1] = cellIndex + 1;
					cellIndexes[2] = cellIndex + gnd.Header.Width * 2;
					cellIndexes[3] = cellIndexes[2] + 1;

					//Z.Start(4);
					for (int i = 0; i < 4; i++) {
						gatCell = gat.Cells[cellIndexes[i]];
						gatType = (sbyte)gatCell.Type;

						if (gatCell.IsWater == true) {
							cellTypes[i] = -1;
						}
						else if (gatCell.IsInnerGutterLine == true) {
							cellTypes[i] = -3;
						}
						else if (gatCell.IsOutterGutterLine == true) {
							cellTypes[i] = -2;
						}
						else {
							// No point keeping useless types
							switch(gatType) {
								case 2:
								case 3:
								case 4:
								case 6:
									gatType = 0;
									break;
							}

							cellTypes[i] = gatType;
						}
					}

					int gatUid = (cellTypes[0] & 0xff) << 24 | (cellTypes[1] & 0xff) << 16 | (cellTypes[2] & 0xff) << 8 | (cellTypes[3] & 0xff);
					
					string textureName;

					if (!_computedGatTypes2Textures.TryGetValue(gatUid, out textureName)) {
						StringBuilder b = new StringBuilder();
						b.Append(id);

						for (int i = 0; i < 4; i++) {
							b.Append("c");
							b.Append(cellTypes[i]);
						}

						b.Append(".bmp");
						textureName = b.ToString();
						_computedGatTypes2Textures[gatUid] = textureName;
					}

					//Z.Stop(4);

					//Z.Start(5);
					if (_state.OutputTexturePaths == null) {
						lock (_state.Lock) {
							if (_state.OutputTexturePaths == null) {
								_state.OutputTexturePaths = new HashSet<string>();
							}
						}
					}
					//Z.Stop(5);

					//Z.Start(6);
					if (gnd.GetTextureIndex(textureName) < 0) {
						var texturePath = Path.Combine(OutputTexturePath, textureName);

						lock (_state.Lock) {
							if (!_state.OutputTexturePaths.Contains(texturePath)) {
								if (!File.Exists(texturePath)) {
									GenerateTexture(textureName, cellTypes);
								}

								_state.OutputTexturePaths.Add(texturePath);
							}
						}

						gnd.AddTexture(textureName);
					}
					//Z.Stop(6);

					//Z.Start(7);
					// If the ground is flattened, a new tile must be created
					if (_mapConfig.FlattenGround && _mapConfig.RemoveLight && _mapConfig.RemoveShadow && _mapConfig.RemoveColor) {
						tile = new Tile(gnd.GetTextureIndex(textureName));

						gnd.Tiles.Add(tile);
						offset = y * gnd.Header.Width + x;

						cube = gnd.Cubes[offset];
						cube.TileUp = gnd.Tiles.Count - 1;
						cube.TileSide = -1;
						cube.TileFront = -1;
					}
					// The old tile is kept
					else {
						cube = gnd.Cubes[y * gnd.Header.Width + x];

						if (cube.TileUp > -1) {
							tile = gnd.Tiles[cube.TileUp];
							tile.TextureIndex = gnd.GetTextureIndex(textureName);
							tile.ResetTextureUv();
						}

						if (_mapConfig.TextureWalls) {
							if (cube.TileSide > -1) {
								tile = gnd.Tiles[cube.TileSide];
								tile.TextureIndex = gnd.GetTextureIndex(id + "cw.bmp");
								tile.ResetTextureUv();
							}

							if (cube.TileFront > -1) {
								tile = gnd.Tiles[cube.TileFront];
								tile.TextureIndex = gnd.GetTextureIndex(id + "cw.bmp");
								tile.ResetTextureUv();
							}
						}
					}
					//Z.Stop(7);
				}
			}
			//Z.Stop(3);

			//Z.Start(8);
			if (_mapConfig.StickGatCellsToGround) {
				gat.Adjust(gnd);
			}
			//Z.Stop(8);
			_gndMatchShadowWithGatCells(gnd, gat);
		}

		private void _gndUseShadowsWithQuadrants(Gnd gnd, Gat gat) {
			if (!_mapConfig.UseShadowsForQuadrants)
				return;

			// Setup tiles and lightmaps
			int tileIndex = 0;
			int lightmapIndex = 0;
			int tileCount = gnd.Tiles.Count;
			int lightmapsCount = gnd.LightmapContainer.Lightmaps.Count;
			const byte QuadShadow = 191;

			// Add lightmaps
			for (int y = 0; y < gnd.Header.Height; y++) {
				for (int x = 0; x < gnd.Header.Width; x++) {
					var cube = gnd[x, y];

					if (cube.TileUp > -1) {
						var copy = gnd.Tiles[cube.TileUp].Copy();
						cube.TileUp = tileIndex;
						tileIndex++;

						var lightData = Methods.Copy(gnd.LightmapContainer.GetRawLightmap(copy.LightmapIndex));

						if ((x % 8 < 4 && y % 8 < 4) ||
							(x % 8 >= 4 && y % 8 >= 4)) {
							;
						}
						else {
							for (int i = 0; i < gnd.PerCell; i++) {
								lightData[i] = Math.Min(QuadShadow, lightData[i]);
							}
						}

						copy.LightmapIndex = (ushort)lightmapIndex;
						lightmapIndex++;
						gnd.LightmapContainer.Add(lightData);
						gnd.Tiles.Add(copy);
					}

					if (cube.TileSide > -1) {
						var copy = gnd.Tiles[cube.TileSide].Copy();
						cube.TileSide = tileIndex;
						tileIndex++;

						var lightData = Methods.Copy(gnd.LightmapContainer.GetRawLightmap(copy.LightmapIndex));

						if ((x % 8 < 4 && y % 8 < 4) ||
							(x % 8 >= 4 && y % 8 >= 4)) {
							;
						}
						else {
							for (int i = 0; i < gnd.PerCell; i++) {
								lightData[i] = Math.Min(QuadShadow, lightData[i]);
							}
						}

						copy.LightmapIndex = (ushort)lightmapIndex;
						lightmapIndex++;
						gnd.LightmapContainer.Add(lightData);
						gnd.Tiles.Add(copy);
					}

					if (cube.TileFront > -1) {
						var copy = gnd.Tiles[cube.TileFront].Copy();
						cube.TileFront = tileIndex;
						tileIndex++;

						var lightData = Methods.Copy(gnd.LightmapContainer.GetRawLightmap(copy.LightmapIndex));

						if ((x % 8 < 4 && y % 8 < 4) ||
						    (x % 8 >= 4 && y % 8 >= 4)) {
							;
						}
						else {
							for (int i = 0; i < gnd.PerCell; i++) {
								lightData[i] = Math.Min(QuadShadow, lightData[i]);
							}
						}

						copy.LightmapIndex = (ushort)lightmapIndex;
						lightmapIndex++;
						gnd.LightmapContainer.Add(lightData);
						gnd.Tiles.Add(copy);
					}
				}
			}

			// Delete all front tiles
			gnd.Tiles.RemoveRange(0, tileCount);
			gnd.LightmapContainer.Lightmaps.RemoveRange(0, lightmapsCount);
			gnd.CleanupLightmaps();
		}

		private void _gndMatchShadowWithGatCells(Gnd gnd, Gat gat) {
			if (!_mapConfig.MatchShadowsWithGatCells)
				return;

			// Setup tiles and lightmaps
			int tileIndex = 0;
			int lightmapIndex = 0;
			int tileCount = gnd.Tiles.Count;
			int lightmapsCount = gnd.LightmapContainer.Lightmaps.Count;

			// Add lightmaps
			for (int y = 0; y < gnd.Header.Height; y++) {
				for (int x = 0; x < gnd.Header.Width; x++) {
					var cube = gnd[x, y];

					if (_mapConfig.FlattenGround) {	
						cube.TileFront = -1;
						cube.TileSide = -1;
					}

					if (cube.TileUp > -1) {
						var copy = gnd.Tiles[cube.TileUp].Copy();
						cube.TileUp = tileIndex;
						tileIndex++;
						
						byte[] lightData = new byte[gnd.PerCell * 4];

						for (int i = 0; i < gnd.PerCell; i++) {
							lightData[i] = 255;
						}

						copy.TileColor = new GrfColor(255, 255, 255, 255);
						copy.LightmapIndex = (ushort)++lightmapIndex;
						gnd.LightmapContainer.Add(lightData);
						gnd.Tiles.Add(copy);
					}

					if (!_mapConfig.FlattenGround && cube.TileSide > -1) {
						var copy = gnd.Tiles[cube.TileSide].Copy();
						cube.TileSide = tileIndex;
						tileIndex++;

						byte[] lightData = new byte[gnd.PerCell * 4];

						for (int i = 0; i < gnd.PerCell; i++) {
							lightData[i] = 255;
						}

						copy.TileColor = new GrfColor(255, 255, 255, 255);
						copy.LightmapIndex = (ushort)++lightmapIndex;
						gnd.LightmapContainer.Add(lightData);
						gnd.Tiles.Add(copy);
					}

					if (!_mapConfig.FlattenGround && cube.TileFront > -1) {
						var copy = gnd.Tiles[cube.TileFront].Copy();
						cube.TileFront = tileIndex;
						tileIndex++;

						byte[] lightData = new byte[gnd.PerCell * 4];

						for (int i = 0; i < gnd.PerCell; i++) {
							lightData[i] = 255;
						}

						copy.TileColor = new GrfColor(255, 255, 255, 255);
						copy.LightmapIndex = (ushort)++lightmapIndex;
						gnd.LightmapContainer.Add(lightData);
						gnd.Tiles.Add(copy);
					}
				}
			}

			// Delete all front tiles
			gnd.Tiles.RemoveRange(0, tileCount);

			try {
				const byte shadowSnipable = 50;
				const byte shadowNonWalkable = 100;

				for (int y = 0; y < gat.Header.Height; y++) {
					for (int x = 0; x < gat.Header.Width; x++) {
						var gatType2 = gat[x, y].Type;

						// Set shadow
						if (gatType2 == GatType.NoWalkable || gatType2 == GatType.NoWalkableNoSnipable || gatType2 == GatType.Unknown) {
							for (int sx = 0; sx < gnd.LightmapWidth / 2; sx++) {
								for (int sy = 0; sy < gnd.LightmapHeight / 2; sy++) {
									int xx = x * gnd.LightmapWidth / 2 + sx;
									int yy = y * gnd.LightmapHeight / 2 + sy;

									// Disable shadow smooth edges
									{
										int subx = sx + ((x % 2 == 0) ? 0 : gnd.LightmapWidth / 2);
										int suby = sy + ((y % 2 == 0) ? 0 : gnd.LightmapHeight / 2);

										if (subx <= 1 || subx >= gnd.LightmapWidth - 2) {
											int extraX = subx <= 1 ? -2 : 2;

											if (subx == 0)
												xx++;
											else if (subx == gnd.LightmapWidth - 1)
												xx--;

											if (suby == 0)
												yy++;
											else if (suby == gnd.LightmapWidth - 1)
												yy--;

											_shadowProc(gnd, xx + extraX, yy, shadowNonWalkable);

											if (suby <= 1) {
												_shadowProc(gnd, xx, yy - 2, shadowNonWalkable);
												_shadowProc(gnd, xx + extraX, yy - 2, shadowNonWalkable);
											}
											else if (suby >= gnd.LightmapHeight - 2) {
												_shadowProc(gnd, xx, yy + 2, shadowNonWalkable);
												_shadowProc(gnd, xx + extraX, yy + 2, shadowNonWalkable);
											}
										}
										else if (suby <= 1 || suby >= gnd.LightmapHeight - 2) {
											if (suby == 0)
												yy++;
											else if (suby == gnd.LightmapHeight - 1)
												yy--;

											_shadowProc(gnd, xx, yy + (suby <= 1 ? -2 : 2), shadowNonWalkable);
										}

										_shadowProc(gnd, xx, yy, shadowNonWalkable);
									}
								}
							}
						}
						else if (gatType2 == GatType.NoWalkableSnipable) {
							int xx = x * gnd.LightmapWidth / 2 + ((x % 2) == 0 ? 2 : 1);
							int yy = y * gnd.LightmapHeight / 2 + ((y % 2) == 0 ? 2 : 1);

							_shadowProc(gnd, xx, yy, shadowSnipable);
						}
					}
				}
			}
			catch (Exception err) {
				Z.F(err);
			}

			gnd.CleanupLightmaps();
			Z.F();
		}

		public void GenerateTexture(string textureName, IList<sbyte> cellTypes) {
			var data = new byte[12288];

			// GrfImage for setting pixels, with Dotnet encoder, decent...
			//Z.Start(30);
			//for (int i = 0; i < 100; i++) {
				GrfImage img = new GrfImage(data, 64, 64, GrfImageType.Bgr24);
				img.SetPixels(32, 0, 32, 32, _getPixels(cellTypes[0]));
				img.SetPixels(0, 0, 32, 32, _getPixels(cellTypes[1]));
				img.SetPixels(32, 32, 32, 32, _getPixels(cellTypes[2]));
				img.SetPixels(0, 32, 32, 32, _getPixels(cellTypes[3]));
				img.Save(Path.Combine(OutputTexturePath, textureName));
			//}
			//Z.Stop(30);

			// WriteableBitmap for setting pixels, with Dotnet encoder, very slow...
			//Z.Start(31);
			//for (int i = 0; i < 100; i++) {
			//	WriteableBitmap bit = new WriteableBitmap(64, 64, 96, 96, PixelFormats.Bgr24, null);
			//	bit.WritePixels(new Int32Rect(32, 0, 32, 32), _getPixels(cellTypes[0]), 32 * PixelFormats.Bgr24.BitsPerPixel / 8, 0);
			//	bit.WritePixels(new Int32Rect(0, 0, 32, 32), _getPixels(cellTypes[1]), 32 * PixelFormats.Bgr24.BitsPerPixel / 8, 0);
			//	bit.WritePixels(new Int32Rect(32, 32, 32, 32), _getPixels(cellTypes[2]), 32 * PixelFormats.Bgr24.BitsPerPixel / 8, 0);
			//	bit.WritePixels(new Int32Rect(0, 32, 32, 32), _getPixels(cellTypes[3]), 32 * PixelFormats.Bgr24.BitsPerPixel / 8, 0);
			//
			//	try {
			//		using (FileStream stream = new FileStream(Path.Combine(OutputTexturePath, textureName), FileMode.Create)) {
			//			BmpBitmapEncoder encoder = new BmpBitmapEncoder();
			//			encoder.Frames.Add(BitmapFrame.Create(bit));
			//			encoder.Save(stream);
			//			stream.Close();
			//		}
			//	}
			//	catch {
			//	}
			//}
			//Z.Stop(31);

			// GrfImage for setting pixels, with custom encoder, kinda slow...
			//Z.Start(31);
			//for (int i = 0; i < 100; i++) {
			//	GrfImage img = new GrfImage(ref data, 64, 64, GrfImageType.Bgr24);
			//	img.SetPixels(32, 0, 32, 32, _getPixels(cellTypes[0]));
			//	img.SetPixels(0, 0, 32, 32, _getPixels(cellTypes[1]));
			//	img.SetPixels(32, 32, 32, 32, _getPixels(cellTypes[2]));
			//	img.SetPixels(0, 32, 32, 32, _getPixels(cellTypes[3]));
			//	BmpDecoder.Save(img, Path.Combine(OutputTexturePath, textureName));
			//}
			//Z.Stop(31);
		}

		private byte[] _getPixels(sbyte cellType) {
			if (_state.BufferedImages.ContainsKey(cellType))
				return _state.BufferedImages[cellType];

			string filePath = !File.Exists(Path.Combine(InputTexturePath, "c" + cellType + ".bmp")) ?
																										Path.Combine(InputTexturePath, "cx.bmp") : Path.Combine(InputTexturePath, "c" + cellType + ".bmp");

			BmpBitmapDecoder decoder = new BmpBitmapDecoder(new Uri(filePath), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			var frame = decoder.Frames[0];

			byte[] pixels = new byte[frame.PixelHeight * frame.PixelWidth * frame.Format.BitsPerPixel / 8];
			frame.CopyPixels(pixels, frame.PixelWidth * frame.Format.BitsPerPixel / 8, 0);

			_state.BufferedImages[cellType] = pixels;
			return pixels;
		}

		private Rsw _configRswFile(byte[] rswData, string mapName, Gnd gnd) {
			Rsw rsw;

			if (_mapConfig.FlattenGround) {
				rsw = Rsw.CreateEmpty(mapName);
				rsw.Header.SetVersion(1, 9);

				// Already reseted, no need for GrfEditorConfiguration.ResetGlobalLighting
				if (!_mapConfig.ResetGlobalLighting) {
					var rsw2 = new Rsw(rswData);
					rsw.Light.Longitude = rsw2.Light.Longitude;
					rsw.Light.Latitude = rsw2.Light.Latitude;
					rsw.Light.DiffuseRed = rsw2.Light.DiffuseRed;
					rsw.Light.DiffuseGreen = rsw2.Light.DiffuseGreen;
					rsw.Light.DiffuseBlue = rsw2.Light.DiffuseBlue;
					rsw.Light.AmbientRed = rsw2.Light.AmbientRed;
					rsw.Light.AmbientGreen = rsw2.Light.AmbientGreen;
					rsw.Light.AmbientBlue = rsw2.Light.AmbientBlue;
				}
			}
			else {
				rsw = new Rsw(rswData);

				// Water data is from the gnd file, copy that since we're downgrading
				if (rsw.Header.Version >= 2.6) {
					if (gnd.Water != null && gnd.Water.Zones.Count > 0) {
						rsw.Water = gnd.Water.Zones[0];
					}
				}

				// Downgrade to 1.9
				rsw.Header.SetVersion(1, 9);

				if (_mapConfig.ResetGlobalLighting) {
					rsw.ResetLight();
				}

				if (_mapConfig.RemoveAllObjects) {
					rsw.RemoveObjects();
				}

				if (_mapConfig.RemoveWater) {
					rsw.Water.Reset();
					rsw.Water.Level = -(gnd.Cubes.Min(p => p.BottomLeft) - 100f);
				}
			}

			return rsw;
		}

		private Gat _configGatFile(byte[] gatData, RswHeader rswHeader, float waterLevel) {
			Gat gat = new Gat(gatData);

			if (rswHeader.Version < 2.6) {
				gat.IdentifyWaterCells(waterLevel);
			}

			if (_mapConfig.FlattenGround)
				gat.SetCellsHeight(0);

			if (_mapConfig.ShowGutterLines)
				gat.IdentifyGutterLines();

			return gat;
		}

		public void ClearTextures() {
			try {
				foreach (string file in Directory.GetFiles(GrfEditorConfiguration.FlatMapsMakerOutputTexturesPath, "*")) {
					string id = GrfEditorConfiguration.FlatMapsMakerId;

					if (id == "")
						GrfPath.Delete(file);
					else {
						if (Path.GetFileName(file).StartsWith(id)) {
							GrfPath.Delete(file);
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#region Nested type: MapEditorState

		public class MapEditorState {
			public readonly TkDictionary<int, byte[]> BufferedImages = new TkDictionary<int, byte[]>();
			//public readonly TkDictionary<int, Tile> BufferedTiles = new TkDictionary<int, Tile>();
			public HashSet<string> OutputTexturePaths;
			public bool WallTextureCopied;
			public readonly object Lock = new object();
			public ReadOnlyCollection<string> OldTextures { get; set; }
		}

		#endregion
	}
}