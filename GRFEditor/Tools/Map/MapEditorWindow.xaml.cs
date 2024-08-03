using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRF.Image;
using GRF.System;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;

//using Parallel = GRF.Threading.Parallel;

namespace GRFEditor.Tools.Map {
	/// <summary>
	/// Interaction logic for MapEditorCustom.xaml
	/// </summary>
	public partial class MapEditorWindow : TkWindow, IProgress {
		private readonly AsyncOperation _asyncOperation;
		private readonly GrfHolder _grfHolder;
		private readonly MapEditor _mapEditor = new MapEditor();
		private bool _grfOnly;
		private bool _updatingTextures;

		public MapEditorWindow() {
			InitializeComponent();
		}

		public MapEditorWindow(GrfHolder grfHolder)
			: base("Flat maps maker", "mapEditor.ico", SizeToContent.Manual, ResizeMode.NoResize) {
			InitializeComponent();

			_grfHolder = grfHolder;

			_mapEditor.ValidatePaths();

			_setButtonImages();
			_setColors();

			_cBorder.ColorChanged += (s, c) => _updateTextures();
			_cgutter1.ColorChanged += (s, c) => _updateTextures();
			_cgutter2.ColorChanged += (s, c) => _updateTextures();
			_cwater0.ColorChanged += (s, c) => _updateTextures();
			_cwater1.ColorChanged += (s, c) => _updateTextures();
			_cwall.ColorChanged += (s, c) => _updateTextures();
			//_cwall.ColorChanged += (s, c) => _updateTextures();
			_c0.ColorChanged += (s, c) => _updateTextures();
			_c1.ColorChanged += (s, c) => _updateTextures();
			_c2.ColorChanged += (s, c) => _updateTextures();
			_c3.ColorChanged += (s, c) => _updateTextures();
			_c4.ColorChanged += (s, c) => _updateTextures();
			_c5.ColorChanged += (s, c) => _updateTextures();
			_c6.ColorChanged += (s, c) => _updateTextures();
			_cx.ColorChanged += (s, c) => _updateTextures();

			Binder.Bind(_tbBorderWidth, () => GrfEditorConfiguration.FlatMapsCellWidth);

			_textBoxIM.Text = GrfEditorConfiguration.FlatMapsMakerInputMapsPath;
			_textBoxIM.TextChanged += (e, a) => GrfEditorConfiguration.FlatMapsMakerInputMapsPath = _textBoxIM.Text;

			_mapId.Text = GrfEditorConfiguration.FlatMapsMakerId;
			Binder.Bind(_cbRemoveLighting, () => GrfEditorConfiguration.RemoveAllLighting);
			Binder.Bind(_cbRemoveObjects, () => GrfEditorConfiguration.RemoveAllObjects);
			Binder.Bind(_cbGutterLines, () => GrfEditorConfiguration.ShowGutterLines);
			Binder.Bind(_cbResetGlobalLighting, () => GrfEditorConfiguration.ResetGlobalLighting);
			Binder.Bind(_cbFlattenGround, () => GrfEditorConfiguration.FlattenGround);
			Binder.Bind(_cbStickGatCells, () => GrfEditorConfiguration.StickGatCellsToGround);
			Binder.Bind(_cbRemoveWater, () => GrfEditorConfiguration.RemoveWater);
			_cbTextureWalls.IsChecked = GrfEditorConfiguration.TextureWalls;
			_cbTextureBlack.IsChecked = GrfEditorConfiguration.TextureBlack;
			_cbTextureOriginal.IsChecked = GrfEditorConfiguration.TextureOriginal;
			Binder.Bind(_checkBoxGrfOnly, delegate {
				_grfOnly = _checkBoxGrfOnly.IsChecked == true;
				_textBoxIM.IsEnabled = _checkBoxGrfOnly.IsChecked == false;
			}, true);

			Action action = new Action(delegate {
				_cbGutterLines.IsEnabled = GrfEditorConfiguration.UseCustomTextures;
				_cbStickGatCells.IsEnabled = GrfEditorConfiguration.UseCustomTextures;
				_cbTextureBlack.IsEnabled = GrfEditorConfiguration.UseCustomTextures;
				_cbTextureWalls.IsEnabled = GrfEditorConfiguration.UseCustomTextures;
				_cbTextureOriginal.IsEnabled = GrfEditorConfiguration.UseCustomTextures;
			});

			Binder.Bind(_cbUseCustomTextures, () => GrfEditorConfiguration.UseCustomTextures, action, true);

			_asyncOperation = new AsyncOperation(_progressBar);

			WpfUtils.AddMouseInOutEffectsBox(
				_cbRemoveLighting, _cbRemoveObjects, _cbGutterLines, _cbResetGlobalLighting, _cbFlattenGround,
				_cbStickGatCells, _cbRemoveWater, _cbUseCustomTextures, _checkBoxGrfOnly);

			WpfUtils.AddMouseInOutEffectsBox(_cbTextureBlack, _cbTextureWalls, _cbTextureOriginal);

			_cbTextureBlack.Checked += _wall_Checked;
			_cbTextureWalls.Checked += _wall_Checked;
			_cbTextureOriginal.Checked += _wall_Checked;
		}

		private void _wall_Checked(object sender, RoutedEventArgs e) {
			RadioButton[] buttons = { _cbTextureBlack, _cbTextureWalls, _cbTextureOriginal };

			for (int i = 0; i < buttons.Length; i++) {
				if (buttons[i] != sender) {
					buttons[i].IsChecked = false;
				}
			}

			GrfEditorConfiguration.TextureBlack = false;
			GrfEditorConfiguration.TextureWalls = false;
			GrfEditorConfiguration.TextureOriginal = false;

			if (sender == _cbTextureBlack) {
				GrfEditorConfiguration.TextureBlack = true;
			}
			else if (sender == _cbTextureWalls) {
				GrfEditorConfiguration.TextureWalls = true;
			}
			else if (sender == _cbTextureOriginal) {
				GrfEditorConfiguration.TextureOriginal = true;
			}
		}

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		private void _setColors() {
			_cBorder.SetColor(GrfEditorConfiguration.FlatMapsMakerCBorder);
			_cgutter1.SetColor(GrfEditorConfiguration.FlatMapsMakerGutter1);
			_cgutter2.SetColor(GrfEditorConfiguration.FlatMapsMakerGutter2);
			_cwater0.SetColor(GrfEditorConfiguration.FlatMapsMakerCwBackground);
			_cwater1.SetColor(GrfEditorConfiguration.FlatMapsMakerCwForeground);
			_cwall.SetColor(GrfEditorConfiguration.FlatMapsMakerCWall);
			_cwall.SetColor(GrfEditorConfiguration.FlatMapsMakerCWall);
			_c0.SetColor(GrfEditorConfiguration.FlatMapsMakerC0);
			_c1.SetColor(GrfEditorConfiguration.FlatMapsMakerC1);
			_c2.SetColor(GrfEditorConfiguration.FlatMapsMakerC2);
			_c3.SetColor(GrfEditorConfiguration.FlatMapsMakerC3);
			_c4.SetColor(GrfEditorConfiguration.FlatMapsMakerC4);
			_c5.SetColor(GrfEditorConfiguration.FlatMapsMakerC5);
			_c6.SetColor(GrfEditorConfiguration.FlatMapsMakerC6);
			_cx.SetColor(GrfEditorConfiguration.FlatMapsMakerCx);
			_tbBorderWidth.Text = GrfEditorConfiguration.FlatMapsCellWidth;
		}

		private void _setButtonImages() {
			_setButtonImage(_buttonCell_0, "c0.bmp");
			_setButtonImage(_buttonCell_1, "c1.bmp");
			_setButtonImage(_buttonCell_2, "c2.bmp");
			_setButtonImage(_buttonCell_3, "c3.bmp");
			_setButtonImage(_buttonCell_4, "c4.bmp");
			_setButtonImage(_buttonCell_5, "c5.bmp");
			_setButtonImage(_buttonCell_6, "c6.bmp");
			_setButtonImage(_buttonCell_n1, "c-1.bmp");
			_setButtonImage(_buttonCell_n2, "c-2.bmp");
			_setButtonImage(_buttonCell_n3, "c-3.bmp");
			_setButtonImage(_buttonCell_x, "cx.bmp");
		}

		private void _setButtonImage(FancyButton button, string fileName) {
			byte[] imageData = File.ReadAllBytes(Path.Combine(_mapEditor.InputTexturePath, fileName));
			GrfImage im = new GrfImage(ref imageData);
			button.ImageIcon.Source = im.Cast<BitmapSource>();
		}

		private void _selectFolder(TextBox textBoxIm) {
			string path = PathRequest.FolderEditor("selectedPath", textBoxIm.Text);

			if (path != null) {
				textBoxIm.Text = path;
			}
		}

		private void _buttonBrowseIm_Click(object sender, RoutedEventArgs e) {
			_selectFolder(_textBoxIM);
		}

		protected void _buttonOk_Click(object sender, RoutedEventArgs e) {
			_asyncOperation.SetAndRunOperation(new GrfThread(_generate, this, 200, null));
		}

		private void _updateTextures() {
			try {
				_updatingTextures = true;
				GrfEditorConfiguration.FlatMapsMakerCBorder = _cBorder.Color;
				GrfEditorConfiguration.FlatMapsMakerGutter1 = _cgutter1.Color;
				GrfEditorConfiguration.FlatMapsMakerGutter2 = _cgutter2.Color;
				GrfEditorConfiguration.FlatMapsMakerCwBackground = _cwater0.Color;
				GrfEditorConfiguration.FlatMapsMakerCwForeground = _cwater1.Color;
				GrfEditorConfiguration.FlatMapsMakerCWall = _cwall.Color;
				GrfEditorConfiguration.FlatMapsMakerCWall = _cwall.Color;
				GrfEditorConfiguration.FlatMapsMakerC0 = _c0.Color;
				GrfEditorConfiguration.FlatMapsMakerC1 = _c1.Color;
				GrfEditorConfiguration.FlatMapsMakerC2 = _c2.Color;
				GrfEditorConfiguration.FlatMapsMakerC3 = _c3.Color;
				GrfEditorConfiguration.FlatMapsMakerC4 = _c4.Color;
				GrfEditorConfiguration.FlatMapsMakerC5 = _c5.Color;
				GrfEditorConfiguration.FlatMapsMakerC6 = _c6.Color;
				GrfEditorConfiguration.FlatMapsMakerCx = _cx.Color;

				_generateTexture("c0.bmp", GrfEditorConfiguration.FlatMapsMakerC0);
				_generateTexture("c1.bmp", GrfEditorConfiguration.FlatMapsMakerC1);
				_generateTexture("c2.bmp", GrfEditorConfiguration.FlatMapsMakerC2);
				_generateTexture("c3.bmp", GrfEditorConfiguration.FlatMapsMakerC3);
				_generateTexture("c4.bmp", GrfEditorConfiguration.FlatMapsMakerC4);
				_generateTexture("c5.bmp", GrfEditorConfiguration.FlatMapsMakerC5);
				_generateTexture("c6.bmp", GrfEditorConfiguration.FlatMapsMakerC6);
				_generateTexture("c-2.bmp", GrfEditorConfiguration.FlatMapsMakerGutter1);
				_generateTexture("c-3.bmp", GrfEditorConfiguration.FlatMapsMakerGutter2);
				_generateTexture("cx.bmp", GrfEditorConfiguration.FlatMapsMakerCx);

				byte[] cWater = ApplicationManager.GetResource("c8w.bmp");
				GrfImage image = new GrfImage(ref cWater);
				int cellWidth = GrfEditorConfiguration.FlatMapsCellWidth2;

				for (int x = 0; x < 32; x++) {
					for (int y = 0; y < 32; y++) {
						if (y < cellWidth || y >= (32 - cellWidth) || x < cellWidth || x >= (32 - cellWidth)) {
							image.Pixels[y * 32 + x] = 1;
						}
					}
				}

				image.SetPaletteColor(1, GrfEditorConfiguration.FlatMapsMakerCBorder.ToGrfColor());
				image.SetPaletteColor(2, GrfEditorConfiguration.FlatMapsMakerCwBackground.ToGrfColor());
				image.SetPaletteColor(3, GrfEditorConfiguration.FlatMapsMakerCwForeground.ToGrfColor());
				image.Save(GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "c-1.bmp"), PixelFormats.Bgr24);

				byte[] cWall = ApplicationManager.GetResource("c8wall.bmp");
				GrfImage image2 = new GrfImage(ref cWall);
				image2.SetPaletteColor(1, GrfEditorConfiguration.FlatMapsMakerCBorder.ToGrfColor());
				image2.SetPaletteColor(2, GrfEditorConfiguration.FlatMapsMakerCWall.ToGrfColor());
				image2.Save(GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "cw.bmp"), PixelFormats.Bgr24);

				_setButtonImages();
			}
			finally {
				_updatingTextures = false;
			}
		}

		private void _generateTexture(string name, Color color) {
			byte[] data = new byte[32 * 32 * 3];
			byte[] border = GrfEditorConfiguration.FlatMapsMakerCBorder.ToGrfColor().ToBgrBytes();
			byte[] background = color.ToGrfColor().ToBgrBytes();

			int cellWidth = GrfEditorConfiguration.FlatMapsCellWidth2;

			for (int y = 0, offset = 0; y < 32; y++) {
				for (int x = 0; x < 32; x++, offset += 3) {
					if (y < cellWidth || y >= (32 - cellWidth) || x < cellWidth || x >= (32 - cellWidth)) {
						Buffer.BlockCopy(border, 0, data, offset, 3);
					}
					else {
						Buffer.BlockCopy(background, 0, data, offset, 3);
					}
				}
			}

			string fullPath = GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, name);
			GrfPath.CreateDirectoryFromFile(fullPath);

			WriteableBitmap bit = new WriteableBitmap(32, 32, 96, 96, PixelFormats.Bgr24, null);
			bit.WritePixels(new Int32Rect(0, 0, 32, 32), data, 32 * 3, 0);
			bit.Freeze();

			try {
				using (FileStream stream = new FileStream(fullPath, FileMode.Create)) {
					BmpBitmapEncoder encoder = new BmpBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(bit));
					encoder.Save(stream);
					stream.Close();
				}
			}
			catch {
			}
		}

		private void _generate() {
			try {
				Progress = -1;

				while (_updatingTextures)
					Thread.Sleep(200);

				_mapEditor.Begin();
				IsCancelled = false;
				IsCancelling = false;

				_mapEditor.ValidatePaths();
				_mapEditor.ClearTextures();

				if (File.Exists(_mapEditor.OutputMapPath) && Methods.IsFileLocked(_mapEditor.OutputMapPath)) {
					throw new Exception("The output file is locked : " + _mapEditor.OutputMapPath);
				}

				string[] files;

				if (_grfOnly) {
					files = _grfHolder.FileTable.Entries.OrderBy(p => p.FileExactOffset).Select(p => p.RelativePath).Where(p => p.IsExtension(".gat")).Select(Path.GetFileNameWithoutExtension).ToArray();
				}
				else {
					files = Directory.GetFiles(_mapEditor.InputMapPath, "*.gat").ToList().Select(Path.GetFileNameWithoutExtension).ToArray();
				}

				if (files.Length == 0) {
					ErrorHandler.HandleException("No maps have been found.");
					throw new OperationCanceledException();
				}

				object oLock = new object();
				int totalGrfFiles = 0;

				GenericThreadPool<string> threadPool = new GenericThreadPool<string>();
				Dictionary<int, FileStream> streams = new Dictionary<int, FileStream>();

				List<FileEntry> entries = new List<FileEntry>();
				var textStream = new FileStream(Path.Combine(Settings.TempPath, "~fmtmp_textures"), FileMode.Create);
				streams[-1] = textStream;
				GrfPath.Delete(_mapEditor.OutputMapPath);

				threadPool.Initialize(this, files, (file, thread) => {
					try {
						lock (oLock) {
							if (!streams.ContainsKey(thread.ThreadId)) {
								string name = Path.Combine(Settings.TempPath, "~fmtmp" + thread.ThreadId);
								streams[thread.ThreadId] = new FileStream(name, FileMode.Create);
							}
						}

						FileStream f = streams[thread.ThreadId];

						if (_grfOnly) {
							var gat = _grfHolder.FileTable.TryGet("data\\" + file + ".gat");
							var rsw = _grfHolder.FileTable.TryGet("data\\" + file + ".rsw");
							var gnd = _grfHolder.FileTable.TryGet("data\\" + file + ".gnd");

							if (gat == null || rsw == null || gnd == null) {
								ErrorHandler.HandleException("The file [" + file + "] couldn't be completed.", ErrorLevel.NotSpecified);
								return;
							}

							try {
								byte[] gatData = gat.GetDecompressedData();
								byte[] rswData = rsw.GetDecompressedData();
								byte[] gndData = gnd.GetDecompressedData();

								_mapEditor.Generate(file, gatData, rswData, gndData, f, oLock, entries);
							}
							catch {
								// Couldn't read the file?
							}
						}
						else {
							_mapEditor.Generate(file, f, oLock, entries);
						}

						lock (oLock) {
							totalGrfFiles++;

							if (totalGrfFiles > 100) {
								GC.Collect();
								totalGrfFiles = 0;
							}
						}
					}
					catch (Exception err) {
						IsCancelling = true;
						ErrorHandler.HandleException(err, ErrorLevel.NotSpecified);
					}
				}, 5);

				try {
					threadPool.Start(p => Progress = AProgress.LimitProgress(p), () => IsCancelling);

					foreach (var texture in Directory.GetFiles(_mapEditor.OutputTexturePath, "*.bmp", SearchOption.TopDirectoryOnly)) {
						var data = File.ReadAllBytes(texture);
						var compressed = Compression.Compress(data);
						var offset = (uint)textStream.Position;
						textStream.Write(compressed, 0, compressed.Length);

						if (compressed.Length % 8 != 0) {
							textStream.Write(new byte[8 - compressed.Length % 8], 0, 8 - compressed.Length % 8);
						}

						var entry = FileEntry.CreateBufferedEntry(textStream.Name, "data\\texture\\" + Path.GetFileName(texture), offset, compressed.Length, (int)(textStream.Position - offset), data.Length);
						entries.Add(entry);
					}

					foreach (var stream in streams.Values) {
						stream.Close();
					}

					GrfHolder.CreateFromBufferedFiles(_mapEditor.OutputMapPath, entries);
					OpeningService.FileOrFolder(_mapEditor.OutputMapPath);
				}
				finally {
					foreach (var stream in streams.Values) {
						stream.Close();
					}
				}

				if (IsCancelling) {
					IsCancelled = true;
				}
			}
			catch (OperationCanceledException) {
				if (IsCancelling) {
					IsCancelled = true;
				}
			}
			finally {
				Progress = 100.0f;
				GC.Collect();
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			_asyncOperation.Cancel();
			GC.Collect();
			base.OnClosing(e);
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		protected void _buttonRebuildTextures_Click(object sender, RoutedEventArgs e) {
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _remakeTextures(false), this, 200, null));
		}

		private void _remakeTextures(bool limit) {
			try {
				Progress = -1;
				IsCancelling = false;
				IsCancelled = false;
				_mapEditor.Begin();

				string[] files = Directory.GetFiles(_mapEditor.OutputTexturePath, "*.bmp");

				for (int i = 0; i < files.Length; i++) {
					string file = files[i];
					string texture = Path.GetFileNameWithoutExtension(file);
					string id = "";

					try {
						id = texture.Substring(0, texture.IndexOf('c'));

						if (id != GrfEditorConfiguration.FlatMapsMakerId) continue;

						texture = texture.Substring(texture.IndexOf('c') + 1);
						int[] types = texture.Split('c').Select(Int32.Parse).ToArray();
						_mapEditor.GenerateTexture(id + "c" + texture + ".bmp", types);
					}
					catch {
						try {
							if (texture.EndsWith("w")) {
								if (id != GrfEditorConfiguration.FlatMapsMakerId) continue;

								GrfPath.Copy(GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "cw.bmp"), GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerOutputTexturesPath, GrfEditorConfiguration.FlatMapsMakerId + "cw.bmp"));
							}
						}
						catch {
						}
					}

					var prog = (float)(i + 1) / files.Length * 100.0f;
					Progress = limit ? AProgress.LimitProgress(prog) : prog;

					if (IsCancelling) {
						IsCancelled = true;
						break;
					}
				}
			}
			finally {
				Progress = limit ? AProgress.LimitProgress(100.0f) : 100.0f;
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape) {
				_asyncOperation.Cancel();
				Close();
			}
		}

		private void _buttonOpenIm_Click(object sender, RoutedEventArgs e) {
			try {
				OpeningService.OpenFolder(_textBoxIM.Text);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonOpenTexturesFolder_Click(object sender, RoutedEventArgs e) {
			try {
				OpeningService.OpenFolder(_mapEditor.InputTexturePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonResetTextures_Click(object sender, RoutedEventArgs e) {
			try {
				GrfEditorConfiguration.ConfigAsker.DeleteKeys("[FlatMapsMaker - Cell color ");
				_setColors();
				_updateTextures();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _mapId_TextChanged(object sender, TextChangedEventArgs e) {
			GrfEditorConfiguration.FlatMapsMakerId = _mapId.Text;
		}

		private void _buttonResetOptions_Click(object sender, RoutedEventArgs e) {
			_cbRemoveLighting.IsChecked = true;
			_cbRemoveObjects.IsChecked = false;
			_cbGutterLines.IsChecked = false;
			_cbUseCustomTextures.IsChecked = true;
			_cbFlattenGround.IsChecked = true;
			_cbStickGatCells.IsChecked = false;
			_cbRemoveWater.IsChecked = false;
			_cbResetGlobalLighting.IsChecked = true;
			_cbTextureBlack.IsChecked = true;
			_cbTextureWalls.IsChecked = false;
			_cbTextureOriginal.IsChecked = false;

			_mapId.Text = "";
		}
	}
}