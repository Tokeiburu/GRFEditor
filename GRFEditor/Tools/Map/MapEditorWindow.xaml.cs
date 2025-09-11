using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.Graphics;
using GRF.IO;
using GRF.Image;
using GRF.GrfSystem;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
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
		private readonly MapEditor _mapEditor;
		private bool _grfOnly;
		private bool _updatingTextures;

		public MapEditorWindow() {
			InitializeComponent();
		}

		public MapEditorWindow(GrfHolder grfHolder)
			: base("Flat maps maker", "mapEditor.ico", SizeToContent.Manual, ResizeMode.NoResize) {
			InitializeComponent();

			_mapEditor = new MapEditor(this);
			_grfHolder = grfHolder;

			_mapEditor.ValidatePaths();

			try {
				Methods.FileModified(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "*.bmp", _fileModified);
			}
			catch {
			}

			_viewport.EnableRenderThread = false;
			_viewport.RenderOptions.FpsCap = 30;

			_cecWall._imagePreview.Width = _cecWall._imagePreview.Height = 32;
			_setButtonImages();
			_setColors();

			_cBorder.ColorChanged += (s, c) => _updateTextures();
			_cecGutter1._qcs.ColorChanged += (s, c) => _updateTextures();
			_cecGutter2._qcs.ColorChanged += (s, c) => _updateTextures();
			_cecWater0._qcs.ColorChanged += (s, c) => _updateTextures();
			_cecWater1._qcs.ColorChanged += (s, c) => _updateTextures();
			_cecWall._qcs.ColorChanged += (s, c) => _updateTextures();
			_cec0._qcs.ColorChanged += (s, c) => _updateTextures();
			_cec1._qcs.ColorChanged += (s, c) => _updateTextures();
			//_cec2._qcs.ColorChanged += (s, c) => _updateTextures();
			//_cec3._qcs.ColorChanged += (s, c) => _updateTextures();
			//_cec4._qcs.ColorChanged += (s, c) => _updateTextures();
			_cec5._qcs.ColorChanged += (s, c) => _updateTextures();
			//_cec6._qcs.ColorChanged += (s, c) => _updateTextures();
			_cecX._qcs.ColorChanged += (s, c) => _updateTextures();

			_tbPreviewName.TextChanged += delegate {
				_tbPreviewNamePreview.Visibility = _tbPreviewName.Text == "" && !_tbPreviewName.IsFocused ? Visibility.Visible : Visibility.Collapsed;
			};

			Binder.Bind(_tbBorderWidth, () => GrfEditorConfiguration.FlatMapsCellWidth, _updateTextures, false);

			_pbBoxIM.Text = GrfEditorConfiguration.FlatMapsMakerInputMapsPath;
			_pbBoxIM.TextChanged += (e, a) => {
				GrfEditorConfiguration.FlatMapsMakerInputMapsPath = _pbBoxIM.Text;
				_updatePreviewMap();
			};

			_buttonOpenIM.Content = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png"), Stretch = Stretch.None };

			_mapId.Text = GrfEditorConfiguration.FlatMapsMakerId;
			Binder.Bind(_cbRemoveLight, () => GrfEditorConfiguration.RemoveLight, _updatePreviewMap, false);
			Binder.Bind(_cbRemoveShadow, () => GrfEditorConfiguration.RemoveShadow, _updatePreviewMap, false);
			Binder.Bind(_cbRemoveColor, () => GrfEditorConfiguration.RemoveColor, _updatePreviewMap, false);
			Binder.Bind(_cbRemoveObjects, () => GrfEditorConfiguration.RemoveAllObjects, _updatePreviewMap, false);
			Binder.Bind(_cbGutterLines, () => GrfEditorConfiguration.ShowGutterLines, _updatePreviewMap, false);
			Binder.Bind(_cbResetGlobalLighting, () => GrfEditorConfiguration.ResetGlobalLighting, _updatePreviewMap, false);
			Binder.Bind(_cbMatchShadow, () => GrfEditorConfiguration.MatchShadowsWithGatCells, _updatePreviewMap, false);
			Binder.Bind(_cbQuadmaps, () => GrfEditorConfiguration.UseShadowsForQuadrants, _updatePreviewMap, false);
			Binder.Bind(_cbFlattenGround, () => GrfEditorConfiguration.FlattenGround, _enabledCheck, false);
			Binder.Bind(_cbStickGatCells, () => GrfEditorConfiguration.StickGatCellsToGround, _updatePreviewMap, false);
			Binder.Bind(_cbRemoveWater, () => GrfEditorConfiguration.RemoveWater, _updatePreviewMap, false);
			_cbTextureWalls.IsChecked = GrfEditorConfiguration.TextureWalls;
			_cbTextureBlack.IsChecked = GrfEditorConfiguration.TextureBlack;
			_cbTextureOriginal.IsChecked = GrfEditorConfiguration.TextureOriginal;
			Binder.Bind(_checkBoxGrfOnly, delegate {
				_grfOnly = _checkBoxGrfOnly.IsChecked == true;
				_pbBoxIM.IsEnabled = _checkBoxGrfOnly.IsChecked == false;
				_updatePreviewMap();
			}, true);
			Binder.Bind(_tbPreviewName, () => GrfEditorConfiguration.FlatMapsPreviewMapName, _updatePreviewMap, false);

			_tbPreviewName.GotFocus += delegate {
				_tbPreviewNamePreview.Visibility = Visibility.Collapsed;
			};
			_tbPreviewName.LostFocus += delegate {
				if (_tbPreviewName.Text == "")
					_tbPreviewNamePreview.Visibility = Visibility.Visible;
			};

			Binder.Bind(_cbUseCustomTextures, () => GrfEditorConfiguration.UseCustomTextures, _enabledCheck, false);

			_asyncOperation = new AsyncOperation(_progressBar);

			WpfUtils.AddMouseInOutEffectsBox(
				_cbRemoveLight, _cbRemoveShadow, _cbRemoveColor, _cbRemoveObjects, _cbGutterLines, _cbResetGlobalLighting, _cbFlattenGround,
				_cbStickGatCells, _cbRemoveWater, _cbUseCustomTextures, _checkBoxGrfOnly, _cbMatchShadow, _cbQuadmaps);

			WpfUtils.AddMouseInOutEffectsBox(_cbTextureBlack, _cbTextureWalls, _cbTextureOriginal);

			_cbTextureBlack.Checked += _wall_Checked;
			_cbTextureWalls.Checked += _wall_Checked;
			_cbTextureOriginal.Checked += _wall_Checked;

			this.Loaded += delegate {
				_canLoadPreview = true;
				_updatePreviewMap();
			};

			_enabledCheck();
			_remakeTextureThread.Start("GRF - Remake texture thread", (t, c) => _remakeTextures(c));

			this.Dispatcher.ShutdownStarted += delegate {
				_remakeTextureThread.Terminate();
			};

			this.Closed += delegate {
				_remakeTextureThread.Terminate();
			};

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo {Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "IsWarning", FixedWidth = 20, MaxHeight = 24},
				new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Description", DisplayExpression = "Description", ToolTipBinding = "ToolTipDescription", TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap, IsFill = true},
			}, new DefaultListViewComparer<MapEditorExceptionView>(), new string[] { "Default", "{DynamicResource TextForeground}" });

			_listView.Loaded += delegate {
				_gridError.Visibility = Visibility.Collapsed;
			};
		}

		private void _fileModified(object arg1, FileSystemEventArgs arg2) {
			if (_updatingTextures)
				return;

			_applyChanges();
		}

		private void _enabledCheck() {
			_cbRemoveWater.IsEnabled = !GrfEditorConfiguration.FlattenGround;
			_cbRemoveObjects.IsEnabled = !GrfEditorConfiguration.FlattenGround;

			_cbGutterLines.IsEnabled = GrfEditorConfiguration.UseCustomTextures;
			_cbStickGatCells.IsEnabled = GrfEditorConfiguration.UseCustomTextures && !GrfEditorConfiguration.FlattenGround;
			_cbTextureBlack.IsEnabled = GrfEditorConfiguration.UseCustomTextures && !GrfEditorConfiguration.FlattenGround;
			_cbTextureWalls.IsEnabled = GrfEditorConfiguration.UseCustomTextures && !GrfEditorConfiguration.FlattenGround;
			_cbTextureOriginal.IsEnabled = GrfEditorConfiguration.UseCustomTextures && !GrfEditorConfiguration.FlattenGround;
			_updatePreviewMap();
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

			_updatePreviewMap();
		}

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		private void _setColors() {
			var config = GrfEditorConfiguration.ConfigAsker;

			var set = new Action<CellEditControl, Func<object>>(delegate(CellEditControl cec, Func<object> get) {
				cec._qcs.SetColor(new GrfColor(get().ToString()));
				cec._qcs.SetResetColor(config.RetrieveSetting(get));
			});

			_cBorder.SetColor(GrfEditorConfiguration.FlatMapsMakerCBorder);
			_cBorder.SetResetColor(config.RetrieveSetting(() => GrfEditorConfiguration.FlatMapsMakerCBorder));

			set(_cecGutter1, () => GrfEditorConfiguration.FlatMapsMakerGutter1);
			set(_cecGutter2, () => GrfEditorConfiguration.FlatMapsMakerGutter2);
			set(_cecWater0, () => GrfEditorConfiguration.FlatMapsMakerCwBackground);
			set(_cecWater1, () => GrfEditorConfiguration.FlatMapsMakerCwForeground);
			set(_cecWall, () => GrfEditorConfiguration.FlatMapsMakerCWall);
			set(_cec0, () => GrfEditorConfiguration.FlatMapsMakerC0);
			set(_cec1, () => GrfEditorConfiguration.FlatMapsMakerC1);
			//set(_cec2, () => GrfEditorConfiguration.FlatMapsMakerC2);
			//set(_cec3, () => GrfEditorConfiguration.FlatMapsMakerC3);
			//set(_cec4, () => GrfEditorConfiguration.FlatMapsMakerC4);
			set(_cec5, () => GrfEditorConfiguration.FlatMapsMakerC5);
			//set(_cec6, () => GrfEditorConfiguration.FlatMapsMakerC6);
			set(_cecX, () => GrfEditorConfiguration.FlatMapsMakerCx);

			_tbBorderWidth.Text = GrfEditorConfiguration.FlatMapsCellWidth;
		}

		private void _setButtonImages() {
			_setCellPreviewImage(_cec0._imagePreview, "c0.bmp");
			_setCellPreviewImage(_cec1._imagePreview, "c1.bmp");
			//_setCellPreviewImage(_cec2._imagePreview, "c2.bmp");
			//_setCellPreviewImage(_cec3._imagePreview, "c3.bmp");
			//_setCellPreviewImage(_cec4._imagePreview, "c4.bmp");
			_setCellPreviewImage(_cec5._imagePreview, "c5.bmp");
			//_setCellPreviewImage(_cec6._imagePreview, "c6.bmp");
			_setCellPreviewImage(_cecWater0._imagePreview, "c-1.bmp");
			_setCellPreviewImage(_cecWater1._imagePreview, "c-1.bmp");
			_setCellPreviewImage(_cecGutter1._imagePreview, "c-2.bmp");
			_setCellPreviewImage(_cecGutter2._imagePreview, "c-3.bmp");
			_setCellPreviewImage(_cecX._imagePreview, "cx.bmp");
			_setCellPreviewImage(_cecWall._imagePreview, "cw.bmp");
		}

		private void _setCellPreviewImage(Image img, string fileName) {
			byte[] imageData = File.ReadAllBytes(Path.Combine(_mapEditor.InputTexturePath, fileName));
			GrfImage im = new GrfImage(ref imageData);
			img.Source = im.Cast<BitmapSource>();
		}

		protected void _buttonOk_Click(object sender, RoutedEventArgs e) {
			_asyncOperation.SetAndRunOperation(new GrfThread(_generate, this, 200, null));
			_buttonGenerate.IsEnabled = false;
			_tabItemOptions.IsEnabled = false;
			_tabItemTexture.IsEnabled = false;
		}

		private void _updateTextures() {
			try {
				_updatingTextures = true;
				GrfEditorConfiguration.FlatMapsMakerCBorder = _cBorder.Color;
				GrfEditorConfiguration.FlatMapsMakerGutter1 = _cecGutter1._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerGutter2 = _cecGutter2._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerCwBackground = _cecWater0._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerCwForeground = _cecWater1._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerCWall = _cecWall._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerC0 = _cec0._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerC1 = _cec1._qcs.Color;
				//GrfEditorConfiguration.FlatMapsMakerC2 = _cec2._qcs.Color;
				//GrfEditorConfiguration.FlatMapsMakerC3 = _cec3._qcs.Color;
				//GrfEditorConfiguration.FlatMapsMakerC4 = _cec4._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerC5 = _cec5._qcs.Color;
				//GrfEditorConfiguration.FlatMapsMakerC6 = _cec6._qcs.Color;
				GrfEditorConfiguration.FlatMapsMakerCx = _cecX._qcs.Color;

				_generateTexture("c0.bmp", GrfEditorConfiguration.FlatMapsMakerC0);
				_generateTexture("c1.bmp", GrfEditorConfiguration.FlatMapsMakerC1);
				//_generateTexture("c2.bmp", GrfEditorConfiguration.FlatMapsMakerC2);
				//_generateTexture("c3.bmp", GrfEditorConfiguration.FlatMapsMakerC3);
				//_generateTexture("c4.bmp", GrfEditorConfiguration.FlatMapsMakerC4);
				_generateTexture("c5.bmp", GrfEditorConfiguration.FlatMapsMakerC5);
				//_generateTexture("c6.bmp", GrfEditorConfiguration.FlatMapsMakerC6);

				string fullPath = GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "c0.bmp");
				GrfPath.Copy(fullPath, GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "c2.bmp"));
				GrfPath.Copy(fullPath, GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "c3.bmp"));
				GrfPath.Copy(fullPath, GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "c4.bmp"));
				GrfPath.Copy(fullPath, GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, "c6.bmp"));

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

				_generateTexture("cw.bmp", GrfEditorConfiguration.FlatMapsMakerCWall, 64);

				_setButtonImages();
				_applyChanges();
			}
			catch (Exception err) {
				AddException(new MapEditorException("Failed to update textures.", ErrorLevel.NotSpecified, err));
			}
			finally {
				_updatingTextures = false;
			}
		}

		private void _generateTexture(string name, Color color, int size = 32) {
			try {
				byte[] data = new byte[size * size * 3];
				byte[] border = GrfEditorConfiguration.FlatMapsMakerCBorder.ToGrfColor().ToBgrBytes();
				byte[] background = color.ToGrfColor().ToBgrBytes();

				int cellWidth = GrfEditorConfiguration.FlatMapsCellWidth2;

				for (int y = 0, offset = 0; y < size; y++) {
					for (int x = 0; x < size; x++, offset += 3) {
						if (y < cellWidth || y >= (size - cellWidth) || x < cellWidth || x >= (size - cellWidth)) {
							Buffer.BlockCopy(border, 0, data, offset, 3);
						}
						else {
							Buffer.BlockCopy(background, 0, data, offset, 3);
						}
					}
				}

				string fullPath = GrfPath.Combine(GrfEditorConfiguration.FlatMapsMakerInputTexturesPath, name);
				GrfPath.CreateDirectoryFromFile(fullPath);

				WriteableBitmap bit = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr24, null);
				bit.WritePixels(new Int32Rect(0, 0, size, size), data, size * 3, 0);
				bit.Freeze();

				try {
					using (FileStream stream = new FileStream(fullPath, FileMode.Create)) {
						BmpBitmapEncoder encoder = new BmpBitmapEncoder();
						encoder.Frames.Add(BitmapFrame.Create(bit));
						encoder.Save(stream);
						stream.Close();
					}
				}
				catch (Exception err) {
					AddException(new MapEditorException("Failed to save texture.\n" + fullPath, ErrorLevel.NotSpecified, err));
				}
			}
			catch (Exception err) {
				AddException(new MapEditorException("Failed to generate texture.\n" + name, ErrorLevel.NotSpecified, err));
			}
		}

		private object _errorLock = new object();

		public void AddException(MapEditorException exception) {
			lock (_errorLock) {
				_listView.Dispatch(p => p.Items.Add(new MapEditorExceptionView(exception)));
			}

			_gridError.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _generate() {
			try {
				Progress = -1;

				while (_updatingTextures)
					Thread.Sleep(200);

				_listView.Dispatch(p => p.Items.Clear());
				
				_mapEditor.Begin();
				IsCancelled = false;
				IsCancelling = false;

				_mapEditor.ValidatePaths();
				_mapEditor.ClearTextures();

				if (File.Exists(_mapEditor.OutputMapPath) && Methods.IsFileLocked(_mapEditor.OutputMapPath)) {
					throw new Exception("The output file is locked: " + _mapEditor.OutputMapPath);
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

				object threadPoolLock = new object();
				int totalGrfFiles = 0;

				GenericThreadPool<string> threadPool = new GenericThreadPool<string>();
				Dictionary<int, FileStream> streams = new Dictionary<int, FileStream>();

				List<FileEntry> entries = new List<FileEntry>();
				var textStream = new FileStream(Path.Combine(Settings.TempPath, "~fmtmp_textures"), FileMode.Create);
				streams[-1] = textStream;
				GrfPath.Delete(_mapEditor.OutputMapPath);
				
				threadPool.Initialize(this, files, (file, thread) => {
					try {
						lock (threadPoolLock) {
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

							if (gat == null)
								AddException(new MapEditorException("File not found in GRF: data\\" + file + ".gat", ErrorLevel.Warning));
							if (rsw == null)
								AddException(new MapEditorException("File not found in GRF: data\\" + file + ".rsw", ErrorLevel.Warning));
							if (gnd == null)
								AddException(new MapEditorException("File not found in GRF: data\\" + file + ".gnd", ErrorLevel.Warning));
							
							if (gat == null || rsw == null || gnd == null) {
								return;
							}

							byte[] gatData = null;
							byte[] rswData = null;
							byte[] gndData = null;

							try {
								gatData = gat.GetDecompressedData();
							}
							catch (Exception err) {
								AddException(new MapEditorException("Failed to read data from GRF (possibly encrypted?): data\\" + file + ".gat", ErrorLevel.Warning, err));
							}

							try {
								rswData = rsw.GetDecompressedData();
							}
							catch (Exception err) {
								AddException(new MapEditorException("Failed to read data from GRF (possibly encrypted?): data\\" + file + ".rsw", ErrorLevel.Warning, err));
							}

							try {
								gndData = gnd.GetDecompressedData();
							}
							catch (Exception err) {
								AddException(new MapEditorException("Failed to read data from GRF (possibly encrypted?): data\\" + file + ".gnd", ErrorLevel.Warning, err));
							}

							if (gatData == null || rswData == null || gndData == null)
								return;

							try {
								_mapEditor.Generate(file, gatData, rswData, gndData, f, threadPoolLock, entries);
							}
							catch (Exception err) {
								AddException(new MapEditorException("Failed to generate map: " + file, ErrorLevel.Warning, err));
							}
						}
						else {
							try {
								_mapEditor.Generate(file, f, threadPoolLock, entries);
							}
							catch (Exception err) {
								AddException(new MapEditorException("Failed to generate map: " + file, ErrorLevel.Warning, err));
							}
						}

						lock (threadPoolLock) {
							totalGrfFiles++;

							if (totalGrfFiles > 100) {
								GC.Collect();
								totalGrfFiles = 0;
							}
						}
					}
					catch (Exception err) {
						AddException(new MapEditorException("A thread has failed to finish correctly.", ErrorLevel.Critical, err));
						IsCancelling = true;
					}
				}, 5);

				try {
					threadPool.Start(p => Progress = AProgress.LimitProgress(p), () => IsCancelling);

					// Directory.GetFiles(_mapEditor.OutputTexturePath, "*.bmp", SearchOption.TopDirectoryOnly)
					foreach (var texture in _mapEditor.State.OutputTexturePaths) {
						var data = File.ReadAllBytes(texture);
						var compressed = Compression.Compress(data);
						var offset = textStream.Position;
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

				_buttonGenerate.Dispatch(p => p.IsEnabled = true);
				this.Dispatch(delegate {
					_tabItemOptions.IsEnabled = true;
					_tabItemTexture.IsEnabled = true;
				});
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

		public class RemakeTexture {
			
		}

		private readonly GrfPushSingleThread<RemakeTexture> _remakeTextureThread = new GrfPushSingleThread<RemakeTexture>();

		private void _applyChanges() {
			_remakeTextureThread.Push(new RemakeTexture());
		}

		private void _remakeTextures(Func<bool> isCancelling) {
			while (_asyncOperation.IsRunning && _remakeTextureThread.IsRunning) {
				Thread.Sleep(200);
			}

			if (!_remakeTextureThread.IsRunning)
				return;

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
					sbyte[] types = texture.Split('c').Select(sbyte.Parse).ToArray();
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

				if (!_remakeTextureThread.IsRunning)
					return;

				if (isCancelling()) {
					return;
				}
			}

			this.Dispatch(delegate {
				_updatePreviewMap();
			});
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape) {
				_asyncOperation.Cancel();
				Close();
			}
		}

		private void _buttonOpenIm_Click(object sender, RoutedEventArgs e) {
			try {
				OpeningService.OpenFolder(_pbBoxIM.Text);
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

		private void _mapId_TextChanged(object sender, TextChangedEventArgs e) {
			GrfEditorConfiguration.FlatMapsMakerId = _mapId.Text;
		}

		private void _buttonResetOptions_Click(object sender, RoutedEventArgs e) {
			_cbRemoveLight.IsChecked = true;
			_cbRemoveShadow.IsChecked = true;
			_cbRemoveColor.IsChecked = true;
			_cbRemoveObjects.IsChecked = false;
			_cbGutterLines.IsChecked = false;
			_cbUseCustomTextures.IsChecked = true;
			_cbFlattenGround.IsChecked = true;
			_cbStickGatCells.IsChecked = false;
			_cbRemoveWater.IsChecked = false;
			_cbQuadmaps.IsChecked = false;
			_cbResetGlobalLighting.IsChecked = true;
			_cbTextureBlack.IsChecked = true;
			_cbTextureWalls.IsChecked = false;
			_cbTextureOriginal.IsChecked = false;
			_cbMatchShadow.IsChecked = false;

			_mapId.Text = "";
		}

		public class PreviewMapData {
			public byte[] RswData;
			public byte[] GatData;
			public byte[] GndData;
			public string MapNameExplorer;
			public string MapNameGrf;
			public string MapNamePreview;
			public bool IsGrfOnly;
		}

		private PreviewMapData _previewMapData = new PreviewMapData();
		private bool _canLoadPreview;

		private void _updatePreviewMap() {
			if (!_canLoadPreview)
				return;

			GrfThread.Start(_updatePreviewMap_Sub, "GRF - Update preview sub");
		}

		private void _updatePreviewMap_Sub() {
			try {
				string mapName;

				string s = _tbPreviewName.Dispatch(p => _tbPreviewName.Text);
				bool forceReload = false;

				if (s != "") {
					mapName = Path.GetFileNameWithoutExtension(s);
					_previewMapData.MapNameGrf = null;
					_previewMapData.MapNameExplorer = null;
					forceReload = _previewMapData.MapNamePreview != mapName;
					_previewMapData.MapNamePreview = mapName;
				}
				else {
					if (_grfOnly) {
						mapName = Path.GetFileNameWithoutExtension(_grfHolder.FileTable.Entries.OrderBy(p => p.FileExactOffset).Select(p => p.RelativePath).First(p => p.IsExtension(".rsw")));
						_previewMapData.MapNamePreview = null;
						_previewMapData.MapNameExplorer = null;
						forceReload = _previewMapData.MapNameGrf != mapName;
						_previewMapData.MapNameGrf = mapName;
					}
					else {
						mapName = Path.GetFileNameWithoutExtension(Directory.GetFiles(_mapEditor.InputMapPath, "*.rsw").First());
						_previewMapData.MapNameGrf = null;
						_previewMapData.MapNamePreview = null;
						forceReload = _previewMapData.MapNameExplorer != mapName;
						_previewMapData.MapNameExplorer = mapName;
					}
				}

				if (_previewMapData.IsGrfOnly != _grfOnly)
					forceReload = true;

				if (_grfOnly) {
					if (forceReload || _previewMapData.RswData == null)
						_previewMapData.RswData = _grfHolder.FileTable[@"data\" + mapName + ".rsw"].GetDecompressedData();
					if (forceReload || _previewMapData.GatData == null)
						_previewMapData.GatData = _grfHolder.FileTable[@"data\" + mapName + ".gat"].GetDecompressedData();
					if (forceReload || _previewMapData.GndData == null)
						_previewMapData.GndData = _grfHolder.FileTable[@"data\" + mapName + ".gnd"].GetDecompressedData();
				}
				else {
					if (forceReload || _previewMapData.RswData == null)
						_previewMapData.RswData = File.ReadAllBytes(Path.Combine(_mapEditor.InputMapPath, mapName + ".rsw"));
					if (forceReload || _previewMapData.GatData == null)
						_previewMapData.GatData = File.ReadAllBytes(Path.Combine(_mapEditor.InputMapPath, mapName + ".gat"));
					if (forceReload || _previewMapData.GndData == null)
						_previewMapData.GndData = File.ReadAllBytes(Path.Combine(_mapEditor.InputMapPath, mapName + ".gnd"));
				}

				_previewMapData.IsGrfOnly = _grfOnly;
				GRF.FileFormats.RswFormat.Rsw outputRsw;
				GRF.FileFormats.GndFormat.Gnd outputGndV1;
				Gnd outputGnd;

				_mapEditor.Generate(mapName, _previewMapData.GatData, _previewMapData.RswData, _previewMapData.GndData, out outputRsw, out outputGndV1);
				outputGnd = new Gnd(outputGndV1);

				for (int i = 0; i < outputGnd.Textures.Count; i++) {
					var sText = Path.GetFileName(outputGnd.Textures[i]);

					if (File.Exists(Path.Combine(_mapEditor.OutputTexturePath, sText))) {
						outputGnd.Textures[i] = Path.Combine(_mapEditor.OutputTexturePath, sText);
					}
				}

				_viewport.Dispatch(delegate {
					if (_viewport._request != null) {	
						_viewport.ResetCameraPosition = false;
						_viewport.ResetCameraDistance = false;
					}
				});

				_viewport.Loader.AddRequest(new RendererLoadRequest {
					IsMap = true,
					Preloaded = true,
					Rsw = outputRsw,
					Gnd = outputGnd,
					Resource = @"data\" + mapName,
					CancelRequired = () => false,
					Context = _viewport
				});
			}
			catch {
				_viewport.Loader.AddRequest(new RendererLoadRequest {
					ClearOnly = true,
					CancelRequired = () => false,
					Context = _viewport
				});
			}
		}

		private void _buttonApplyTextures_Click(object sender, RoutedEventArgs e) {
			_applyChanges();
		}

		#region Nested type: CompilerErrorView

		public class MapEditorException : Exception {
			public string Error { get; set; }
			public ErrorLevel ErrorLevel { get; set; }

			public MapEditorException(string error, ErrorLevel level, Exception err = null)
				: base(error, err) {
				Error = error;
				ErrorLevel = level;
			}
		}

		public class MapEditorExceptionView {
			public MapEditorExceptionView(MapEditorException exception) : this(exception.Error, exception.ErrorLevel) {
				ToolTipDescription = exception.ToString();
			}

			public MapEditorExceptionView(string error, ErrorLevel errorLevel) {
				Description = error;
				ToolTipDescription = error;

				switch(errorLevel) {
					case ErrorLevel.NotSpecified:
					case ErrorLevel.Low:
						DataImage = ApplicationManager.PreloadResourceImage("help.png");
						break;
					case ErrorLevel.Warning:
						DataImage = ApplicationManager.PreloadResourceImage("warning16.png");
						break;
					case ErrorLevel.Critical:
						DataImage = ApplicationManager.PreloadResourceImage("error16.png");
						break;
				}
			}

			public string Description { get; set; }
			public string ToolTipDescription { get; set; }
			public object DataImage { get; set; }

			public bool Default {
				get { return true; }
			}
		}

		#endregion
	}
}