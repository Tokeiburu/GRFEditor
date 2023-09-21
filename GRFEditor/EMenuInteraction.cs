using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Tools.GrfValidation;
using GRFEditor.Tools.Map;
using GRFEditor.Tools.SpriteEditor;
using GRFEditor.WPF;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;

namespace GRFEditor {
	partial class EditorMainWindow : Window {
		private readonly ContextMenu _contextMenuEntries = new ContextMenu();
		private readonly ContextMenu _contextMenuNodes = new ContextMenu();
		private WpfRecentFiles _recentFilesManager;

		#region Menu action/window events

		private void _menuItemOpenFrom_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.OpenFileEditor("filter", FileFormat.MergeFilters(Format.AllContainers | Format.Grf | Format.Gpf | Format.Rgz | Format.Thor));
				if (file != null) {
					if (File.Exists(file)) {
						_recentFilesManager.AddRecentFile(file);
						Load(new GrfLoadingSettings(_grfLoadingSettings) { FileName = file });
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemQuit_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _menuItemSave_Click(object sender, RoutedEventArgs e) {
			try {
				if (_grfHolder.IsNewGrf) {
					_menuItemSaveAs_Click(null, null);
				}
				else {
					if (_grfHolder.IsOpened && (_grfHolder.IsBusy || _asyncOperation.IsRunning)) {
						ErrorHandler.HandleException("An opration is currently running, wait for it to finish or cancel it.");
						return;
					}

					_grfLoadingSettings.FileName = _grfHolder.FileName;
					_asyncOperation.ProgressBar.Progress = 0;
					_asyncOperation.ProgressBar.Progress = -1;
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.QuickSave(), _grfHolder, 250, AsyncOperationReturnState.DoesNotRequireVisualReload), _grfSavingFinished);
				}

				if (e != null)
					e.Handled = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemRepack_Click(object sender, RoutedEventArgs e) {
			try {
				_grfLoadingSettings.FileName = _grfHolder.FileName;
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.Repack(), _grfHolder, 250, AsyncOperationReturnState.None), _grfSavingFinished);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemTableEncrypt_Click(object sender, RoutedEventArgs e) {
			try {
				_grfLoadingSettings.FileName = _grfHolder.FileName;
				_grfHolder.Header.SetFileTableEncryption(true);
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.QuickSave(), _grfHolder, 250, AsyncOperationReturnState.DoesNotRequireVisualReload), _grfSavingFinished);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string extension = _grfHolder.FileName.GetExtension();
				string file = PathRequest.SaveFileEditor("filter", FileFormat.MergeFilters(Format.Grf | Format.Gpf | Format.Rgz | Format.Thor),
				                                         "fileName", Path.GetFileName(_grfHolder.FileName),
				                                         "filterIndex", (extension == ".grf" ? 1 : extension == ".gpf" ? 2 : extension == ".rgz" ? 3 : extension == ".thor" ? 4 : 1).ToString(CultureInfo.InvariantCulture));

				if (file != null) {
					_grfLoadingSettings.VisualReloadRequired = false;
					extension = file.GetExtension();

					if (file == _grfHolder.FileName) {
						_grfHolder.IsNewGrf = false;
						_menuItemSave_Click(null, null);
					}
					else {
						if (extension == ".rgz" || _grfHolder.FileName.GetExtension() == ".rgz" ||
						    extension == ".thor" || _grfHolder.FileName.GetExtension() == ".thor")
							_grfLoadingSettings.VisualReloadRequired = true;

						_grfLoadingSettings.FileName = file;

						_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.Save(file), _grfHolder, 250, AsyncOperationReturnState.None), _grfSavingFinished);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonRedo_Click(object sender, RoutedEventArgs e) {
			Redo();
		}

		private void _menuItemNewGrf_Click(object sender, RoutedEventArgs e) {
			try {
				if (!_validateNewContainer()) return;
				_newWithDataFolder(true);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemNewGpf_Click(object sender, RoutedEventArgs e) {
			try {
				if (!_validateNewContainer()) return;
				_newWithDataFolder(true, "new.gpf");
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemMerge_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.ShowWindow<MergeDialogCustom>(new MergeDialogCustom(this, _grfHolder), this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSoustract_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.ShowWindow<SubtractDialogCustom>(new SubtractDialogCustom(this, _grfHolder), this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemValidateGrf_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new ValidationDialog(_grfHolder), _menuItemValidateGrf, null);
		}

		private void _menuItemAbout_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new AboutDialog(Configuration.PublicVersion, Configuration.RealVersion, Configuration.Author, Configuration.ProgramName), this);
		}

		private void _menuItemSettings_OnClick(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new SettingsDialog(_grfHolder, this), _menuItemSettings, this);
		}

		public class ImportSprite {
			public string Illustration { get; set; }
			public string Item { get; set; }
			public string DragAct { get; set; }
			public string DragSpr { get; set; }
			public string AccMaleAct { get; set; }
			public string AccMaleSpr { get; set; }
			public string AccFemaleAct { get; set; }
			public string AccFemaleSpr { get; set; }
		}

		private void _menuItemImportSprite_Click(object sender, RoutedEventArgs e) {
			try {
				string path = PathRequest.FolderExtract();
				_grfHolder.Commands.BeginNoDelay();

				List<ImportSprite> sprites = new List<ImportSprite>();

				if (path != null && Directory.Exists(path)) {
					string[] common = new string[4];
					string[] male = new string[2];
					string[] female = new string[2];
					string name = "";
					string root = (_grfHolder.FileName.IsExtension(".thor") ? GrfStrings.RgzRoot : "");

					foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)) {
						if (file.IsExtension(".bmp")) {
							GrfImage image = new GrfImage(file);

							if (image.Width < 30 && image.Height < 30) {
								common[1] = file;
							}
							else if (image.Width > 60 && image.Height > 80 && image.Width < 80 && image.Height < 120) {
								common[0] = file;
								name = Path.GetFileNameWithoutExtension(file);
							}
						}
						else if (file.IsExtension(".act")) {
							Act act = new Act(file);

							if (act.Actions.Count < 8) {
								common[2] = file;
								common[3] = file.ReplaceExtension(".spr");
							}
							else {
								if (file.Contains("¿+") || file.Contains("¿©") || file.Contains("여")) {
									female[0] = file;
									female[1] = file.ReplaceExtension(".spr");
								}
								else if (file.Contains("n²") || file.Contains("³²") || file.Contains("남")) {
									male[0] = file;
									male[1] = file.ReplaceExtension(".spr");
								}
								else {
									male[0] = file;
									male[1] = file.ReplaceExtension(".spr");
									female[0] = file;
									female[1] = file.ReplaceExtension(".spr");
								}
							}
						}
					}

					if (female[0] != null)
						_grfHolder.Commands.AddFile(root + @"data\sprite\¾Ç¼¼»ç¸®\¿©\¿©_" + name + ".act", female[0], _replaceFilesCallback);

					if (female[1] != null)
						_grfHolder.Commands.AddFile(root + @"data\sprite\¾Ç¼¼»ç¸®\¿©\¿©_" + name + ".spr", female[1], _replaceFilesCallback);

					if (male[0] != null)
						_grfHolder.Commands.AddFile(root + @"data\sprite\¾Ç¼¼»ç¸®\³²\³²_" + name + ".act", male[0], _replaceFilesCallback);

					if (male[1] != null)
						_grfHolder.Commands.AddFile(root + @"data\sprite\¾Ç¼¼»ç¸®\³²\³²_" + name + ".spr", male[1], _replaceFilesCallback);

					_grfHolder.Commands.AddFile(root + @"data\sprite\¾ÆÀÌÅÛ\" + name + ".act", common[2], _replaceFilesCallback);
					_grfHolder.Commands.AddFile(root + @"data\sprite\¾ÆÀÌÅÛ\" + name + ".spr", common[3], _replaceFilesCallback);

					_grfHolder.Commands.AddFile(root + @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection\" + name + ".bmp", common[0], _replaceFilesCallback);
					_grfHolder.Commands.AddFile(root + @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + name + ".bmp", common[1], _replaceFilesCallback);
				}
			}
			catch (Exception err) {
				_grfHolder.Commands.CancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				_grfHolder.Commands.End();
			}
		}

		private void _menuItemSpriteConverter_Click(object sender, RoutedEventArgs e) {
			if (_latestSelectedItem != null && _grfHolder.FileTable.ContainsFile(_latestSelectedItem.RelativePath)) {
				try {
					FileEntry entry = _latestSelectedItem;
					SpriteConverter dialog;

					if (entry.RelativePath.ToLower().EndsWith(".spr")) {
						byte[] data = entry.GetDecompressedData();
						File.WriteAllBytes(Path.Combine(Configuration.TempPath, Path.GetFileName(entry.RelativePath)), data);
						dialog = new SpriteConverter(new string[] { Path.Combine(Configuration.TempPath, Path.GetFileName(entry.RelativePath)) }.ToList());
					}
					else {
						dialog = new SpriteConverter();
					}

					_menuItemSpriteConverter.IsEnabled = false;
					dialog.Closed += (send, a) => _menuItemSpriteConverter.IsEnabled = true;
					dialog.Show();
					return;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			var sprite = new SpriteConverter();
			_menuItemSpriteConverter.IsEnabled = false;
			sprite.Closed += (send, a) => _menuItemSpriteConverter.IsEnabled = true;
			sprite.Show();
		}

		private void _menuItemFlatMapsMaker_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new MapEditorWindow(_grfHolder), this);
		}

		private void _menuItemPatchMaker_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new PatcherDialog(), _menuItemPatchMaker, this);
		}

		private void _menuItemHash_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new HashDialog(), _menuItemHash, this);
		}

		private void _menuItemEncoding_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new ViewEncodingDialog(), _menuItemEncoding, this);
		}

		private void _menuItemSearch_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new SearchDialog(_grfHolder), _menuItemSearch, this);
		}

		private void _menuItemImageConverter_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new ImageConverter(), _menuItemImageConverter, this);
		}

		private void _menuItemGrfCL_Click(object sender, RoutedEventArgs e) {
			try {
				string grfClPath = GrfPath.Combine(TokeiLibrary.Configuration.ApplicationPath, "GrfCL.exe");

				if (!File.Exists(grfClPath))
					throw new Exception("Executable not found: " + grfClPath);

				Process.Start(grfClPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemImageRecon_Click(object sender, RoutedEventArgs e) {
			//WindowProvider.Show(new ImageFinder(this), _menuItemImageRecon, this);
		}

		private void _menuItemEncryptor_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new EncryptorDialog(_grfHolder), this);
		}

		private void _menuItemNewRgz_Click(object sender, RoutedEventArgs e) {
			try {
				if (!_validateNewContainer()) return;
				_newWithRoot("new.rgz");
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#endregion

		#region Menu other events (Encoding, drag/drop)

		public bool SetEncoding(int encoding) {
			try {
				if (_asyncOperation != null && _asyncOperation.IsRunning) {
					ErrorHandler.HandleException("An operation on the GRF is currently running, wait for it to finish or cancel it.");
					return false;
				}

				if (_grfHolder.IsOpened && (_grfHolder.IsModified || _grfHolder.IsBusy || _grfHolder.CancelReload)) {
					ErrorHandler.HandleException("Cannot change the display encoding while a GRF is modified or is being saved. Save your GRF first.");
					return false;
				}

				EncodingService.SetDisplayEncoding(encoding);
				Configuration.EncodingCodepage = encoding;

				if (_grfHolder.IsOpened)
					_grfLoadingSettings.VisualReloadRequired = false;

				bool result = Load();

				if (!result) {
					ErrorHandler.HandleException("Behavior not expected, please report this error with the steps you did.", ErrorLevel.Critical);
					return false;
				}

				if (_grfHolder.IsOpened) {
					var ao = _asyncOperation.Begin();

					GrfThread.Start(delegate {
						try {
							_treeView.Dispatch(() => _treeView.UpdateEncoding());

							while (_reloading) {
								Thread.Sleep(200);
							}

							_loadListItems();
						}
						finally {
							ao.Close();
						}
					});
				}

				Configuration.EncodingCodepage = EncodingService.DisplayEncoding.CodePage;
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Critical);
				return false;
			}
		}

		private void _menuFocus(object sender, RoutedEventArgs e) {
			try {
				_textBoxMainSearch.SelectAll();
				_textBoxMainSearch.Focus();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menu_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _menu_Drop(object sender, DragEventArgs e) {
			try {
				if (!e.Data.GetDataPresent(DataFormats.FileDrop, true)) return;
				string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

				if (files != null && files.Length == 1) {
					if (files[0].IsExtension(FileFormat.AllGrfs.Extensions)) {
						_grfLoadingSettings.FileName = files[0];
						Load();
						e.Handled = true;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#endregion

		#region Logic methods (recent items, title, new RGZ/GRF)

		private void _recentFilesManager_FileClicked(string fileName) {
			try {
				if (File.Exists(fileName)) {
					Load(new GrfLoadingSettings(_grfLoadingSettings) { FileName = fileName });
				}
				else {
					ErrorHandler.HandleException("File not found : " + fileName, ErrorLevel.Low);
					_recentFilesManager.RemoveRecentFile(fileName);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _setupTitle(bool hasBeenModified) {
			if (hasBeenModified) {
				if (!Title.EndsWith("*")) {
					Title += " *";
				}
			}
			else {
				if (Title.EndsWith("*")) {
					Title = Title.Substring(0, Title.Length - 2);
				}
			}
		}

		private bool _validateNewContainer() {
			try {
				if (_grfHolder.IsOpened && _grfHolder.IsModified) {
					MessageBoxResult res = WindowProvider.ShowDialog("The GRF has been modified, do you want to save it first?", "Modified GRF", MessageBoxButton.YesNoCancel);

					if (res == MessageBoxResult.Yes) {
						_menuItemSaveAs_Click(null, null);
						return false;
					}

					if (res == MessageBoxResult.Cancel) {
						return false;
					}
				}
			}
			catch {
				return true;
			}

			return true;
		}

		private void _newWithRoot(string name) {
			_grfHolder.Close();
			_grfHolder.New(name);
			_treeViewPathManager.ClearAll();
			_items.Dispatch(p => _itemEntries.Clear());
			_treeView.Dispatch(p => p.Items.Clear());
			_grfLoadingSettings.FileName = _grfHolder.FileName;

			_listBoxResults.Dispatch(p => _itemSearchEntries.Clear());
			_treeViewPathManager.AddNewRgz(_grfLoadingSettings.FileName);
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = "root" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\datainfo" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\effecttool" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\navigation" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\skillinfoz" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\stateicon" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\skilleffectinfo" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\dressroom" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\hateffectinfo" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\luafiles514\lua files\quest" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\BGM" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\AI" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\SaveData" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\System" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\model" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\effect" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\sprite\npc" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\map") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\illust") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\basic_interface") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\minimap") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\cardbmp") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\¸ó½ºÅÍ") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\ÀÌÆÑÆ®") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\¾ÆÀÌÅÛ") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\¾Ç¼¼»ç¸®\³²") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\¾Ç¼¼»ç¸®\¿©") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\·Îºê") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\ÀÎ°£Á·\¸öÅë\³²") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\ÀÎ°£Á·\¸öÅë\¿©") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\ÀÎ°£Á·\¸Ó¸®Åë\³²") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\sprite\ÀÎ°£Á·\¸Ó¸®Åë\¿©") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\palette\¸ö") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\palette\¸Ó¸®") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = EncodingService.FromAnsiToDisplayEncoding(@"root\data\palette\µµ¶÷Á·\") });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\texture\effect" });
			_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data\wav" });
			_treeViewPathManager.ExpandFirstNode();
			_treeViewPathManager.Expand(@"root\data");
			_treeViewPathManager.Select(new TkPath { FilePath = _grfHolder.FileName, RelativePath = @"root\data" });
			this.Dispatch(p => p.Title = "GRF Editor - new *");
		}

		private void _newWithDataFolder(bool isSTA, string name = "new.grf") {
			_grfHolder.Close();
			_grfHolder.New();

			_treeViewPathManager.ClearAll();
			_items.Dispatch(p => _itemEntries.Clear());
			_grfLoadingSettings.FileName = name;
			_listBoxResults.Dispatch(p => _itemSearchEntries.Clear());

			if (isSTA) {
				_treeViewPathManager.ClearAll();
				_treeViewPathManager.AddNewGrf(name);
			}

			_treeViewPathManager.ExpandFirstNode();
			this.Dispatch(p => p.Title = "GRF Editor - new *");
		}

		#endregion

		private void _buttonPositionUndo_Click(object sender, RoutedEventArgs e) {
			Backward(null, null);
		}

		private void _buttonPositionRedo_Click(object sender, RoutedEventArgs e) {
			Forward(null, null);
		}

		private void _loadMenus() {
			_miOpen.Click += new RoutedEventHandler(_menuItemsOpen_Click);
			_miOpenExplorer.Click += new RoutedEventHandler(_menuItemsOpenExplorer_Click);
			_miExtract.Click += new RoutedEventHandler(_menuItemsExtract_Click);
			_miExtractAt.Click += new RoutedEventHandler(_menuItemsExtractAt_Click);
			_miExportMapFiles.Click += new RoutedEventHandler(_menuItemsExportMapFiles_Click);
			_miDelete.Click += new RoutedEventHandler(_menuItemsDelete_Click);
			_miRename.Click += new RoutedEventHandler(_menuItemsRename_Click);
			_miSaveMapAs.Click += new RoutedEventHandler(_menuItemsSaveMapAs_Click);
			_miConvertRsm2.Click += new RoutedEventHandler(_miConvertRsm2_Click);
			_miEncrypt.Click += new RoutedEventHandler(_menuItemsEncrypt_Click);
			_miDecrypt.Click += new RoutedEventHandler(_menuItemsDecrypt_Click);
			_miProperties.Click += new RoutedEventHandler(_menuItemsProperties_Click);
			_miSelect.Click += new RoutedEventHandler(_menuItemsSelect_Click);
			_miUsage.Click += new RoutedEventHandler(_menuItemsUsage_Click);
			_miToNpc.Click += new RoutedEventHandler(_miToNpc_Click);
			_miExportAsThor.Click += new RoutedEventHandler(_miExportAsThor_Click);
			_miSetEncryptionKey.Click += new RoutedEventHandler(_menuItemSetEncryptionKey_Click);
			_miFlagRemove.Click += new RoutedEventHandler(_menuItemsFlagRemove_Click);

			_contextMenuEntries.Items.Add(_miOpen);
			_contextMenuEntries.Items.Add(_miOpenExplorer);
			_contextMenuEntries.Items.Add(_miExtract);
			_contextMenuEntries.Items.Add(_miExtractAt);
			_contextMenuEntries.Items.Add(_miExportAsThor);
			_contextMenuEntries.Items.Add(_miExportMapFiles);
			_contextMenuEntries.Items.Add(_miSaveMapAs);
			_contextMenuEntries.Items.Add(_miConvertRsm2);
			_contextMenuEntries.Items.Add(_miFlagRemove);
			_contextMenuEntries.Items.Add(_miDelete);
			_contextMenuEntries.Items.Add(_miRename);
			_contextMenuEntries.Items.Add(_miUsage);
			_contextMenuEntries.Items.Add(_miToNpc);
			_contextMenuEntries.Items.Add(_miEncryption);
			_miEncryption.Items.Add(_miEncrypt);
			_miEncryption.Items.Add(_miDecrypt);
			_miEncryption.Items.Add(_miSetEncryptionKey);
			_contextMenuEntries.Items.Add(_miProperties);
			_contextMenuEntries.Items.Add(_miSelect);

			_miTreeOpenExplorer.Click += new RoutedEventHandler(_menuOpen_Click);
			_miTreeExtractFolder.Click += new RoutedEventHandler(_menuItemExtract_Click);
			_miTreeExtractFolderAt.Click += new RoutedEventHandler(_menuItemExtractTo_Click);
			_miTreeExtractRootFiles.Click += new RoutedEventHandler(_menuItemExtractFiles_Click);
			_miTreeExtractRootFilesAt.Click += new RoutedEventHandler(_menuItemExtractFilesTo_Click);
			_miTreeRename.Click += new RoutedEventHandler(_menuItemRename_Click);
			_miTreeDelete.Click += new RoutedEventHandler(_menuItemDelete_Click);
			_miTreeAdd.Click += new RoutedEventHandler(_menuItemAdd_Click);
			_miTreeNewFolder.Click += new RoutedEventHandler(_menuItemNewFolder_Click);
			_miTreeEncrypt.Click += new RoutedEventHandler(_menuItemEncrypt_Click);
			_miTreeDecrypt.Click += new RoutedEventHandler(_menuItemDecrypt_Click);
			_miTreeSetEncryptionKey.Click += new RoutedEventHandler(_menuItemSetEncryptionKey_Click);
			_miTreeSelectInExplorer.Click += new RoutedEventHandler(_menuItemSelect_Click);
			_miTreeProperties.Click += new RoutedEventHandler(_menuItemProperties_Click);
			_miTreeFlagRemove.Click += new RoutedEventHandler(_menuItemFlagRemove_Click);

			_contextMenuNodes.Items.Add(_miTreeOpenExplorer);
			_contextMenuNodes.Items.Add(_miTreeExtract);
			_miTreeExtract.Items.Add(_miTreeExtractFolder);
			_miTreeExtract.Items.Add(_miTreeExtractFolderAt);
			_miTreeExtract.Items.Add(_miTreeExtractRootFiles);
			_miTreeExtract.Items.Add(_miTreeExtractRootFilesAt);
			_contextMenuNodes.Items.Add(_miTreeSeparator);
			_contextMenuNodes.Items.Add(_miTreeRename);
			_contextMenuNodes.Items.Add(_miTreeDelete);
			_contextMenuNodes.Items.Add(_miTreeAdd);
			_contextMenuNodes.Items.Add(_miTreeFlagRemove);
			_contextMenuNodes.Items.Add(_miTreeNewFolder);
			_contextMenuNodes.Items.Add(_miTreeEncryption);
			_miTreeEncryption.Items.Add(_miTreeEncrypt);
			_miTreeEncryption.Items.Add(_miTreeDecrypt);
			_miTreeEncryption.Items.Add(_miTreeSetEncryptionKey);
			_contextMenuNodes.Items.Add(_miTreeProperties);
			_contextMenuNodes.Items.Add(_miTreeSelectInExplorer);

			_listBoxResults.ContextMenu = _contextMenuEntries;
			_items.ContextMenu = _contextMenuEntries;
			_treeView.ContextMenu = _contextMenuNodes;
		}

		#region MenuItems

		private readonly MenuItem _miDecrypt = new MenuItem { Header = "Decrypt", Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miDelete = new MenuItem { Header = "Delete", Icon = new Image { Source = ApplicationManager.GetResourceImage("delete.png") } };
		private readonly MenuItem _miEncrypt = new MenuItem { Header = "Encrypt", IsEnabled = true, Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miEncryption = new MenuItem { Header = "Encryption", Icon = new Image { Source = ApplicationManager.GetResourceImage("lock.png") } };
		private readonly MenuItem _miExportMapFiles = new MenuItem { Header = "Export map files...", Icon = new Image { Source = ApplicationManager.GetResourceImage("mapEditor.png") } };
		private readonly MenuItem _miExtract = new MenuItem { Header = "Extract", Icon = new Image { Source = ApplicationManager.GetResourceImage("archive.png") } };
		private readonly MenuItem _miExtractAt = new MenuItem { Header = "Extract...", Icon = new Image { Source = ApplicationManager.GetResourceImage("archive.png") } };
		private readonly MenuItem _miExportAsThor = new MenuItem { Header = "Export selection...", Icon = new Image { Source = new GrfImage(ApplicationManager.GetResource("grf-16.png")).Cast<BitmapSource>() } };
		private readonly MenuItem _miFlagRemove = new MenuItem { Header = "Set as removed", Icon = new Image { Source = ApplicationManager.GetResourceImage("error16.png") } };
		private readonly MenuItem _miOpen = new MenuItem { Header = "Open file", Icon = new Image { Source = ApplicationManager.GetResourceImage("newFile.png") } };
		private readonly MenuItem _miOpenExplorer = new MenuItem { Header = "Select in explorer", Icon = new Image { Source = ApplicationManager.GetResourceImage("open.png") } };
		private readonly MenuItem _miProperties = new MenuItem { Header = "Properties", Icon = new Image { Source = ApplicationManager.GetResourceImage("properties.png") } };
		private readonly MenuItem _miRename = new MenuItem { Header = "Rename...", Icon = new Image { Source = ApplicationManager.GetResourceImage("refresh.png") } };
		private readonly MenuItem _miSaveMapAs = new MenuItem { Header = "Save map as...", Icon = new Image { Source = ApplicationManager.GetResourceImage("imconvert.png") } };
		private readonly MenuItem _miConvertRsm2 = new MenuItem { Header = "Convert RSM2 to RSM1", Icon = new Image { Source = ApplicationManager.GetResourceImage("imconvert.png") } };
		private readonly MenuItem _miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = ApplicationManager.GetResourceImage("arrowdown.png") } };
		private readonly MenuItem _miSetEncryptionKey = new MenuItem { Header = "Set key", Icon = new Image { Source = ApplicationManager.GetResourceImage("lock.png") } };
		private readonly MenuItem _miToNpc = new MenuItem { Header = "Copy NPC name", Icon = new Image { Source = ApplicationManager.GetResourceImage("copy.png") } };
		private readonly MenuItem _miTreeAdd = new MenuItem { Header = "Add...", Icon = new Image { Source = ApplicationManager.GetResourceImage("add.png") } };
		private readonly MenuItem _miTreeDecrypt = new MenuItem { Header = "Decrypt", Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miTreeDelete = new MenuItem { Header = "Delete", Icon = new Image { Source = ApplicationManager.GetResourceImage("delete.png") } };
		private readonly MenuItem _miTreeEncrypt = new MenuItem { Header = "Encrypt", IsEnabled = true, Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miTreeEncryption = new MenuItem { Header = "Encryption", Icon = new Image { Source = ApplicationManager.GetResourceImage("lock.png") } };
		private readonly MenuItem _miTreeExtract = new MenuItem { Header = "Extract", Icon = new Image { Source = ApplicationManager.GetResourceImage("archive.png") } };
		private readonly MenuItem _miTreeExtractFolder = new MenuItem { Header = "Extract folder", Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miTreeExtractFolderAt = new MenuItem { Header = "Extract folder...", Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miTreeExtractRootFiles = new MenuItem { Header = "Extract root files only", Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miTreeExtractRootFilesAt = new MenuItem { Header = "Extract root files only...", Icon = new Image { Source = ApplicationManager.GetResourceImage("empty.png") } };
		private readonly MenuItem _miTreeFlagRemove = new MenuItem { Header = "Add files to remove...", Icon = new Image { Source = ApplicationManager.GetResourceImage("error16.png") } };
		private readonly MenuItem _miTreeNewFolder = new MenuItem { Header = "New folder...", Icon = new Image { Source = ApplicationManager.GetResourceImage("newFolder.png") } };
		private readonly MenuItem _miTreeOpenExplorer = new MenuItem { Header = "Select in explorer", Icon = new Image { Source = ApplicationManager.GetResourceImage("open.png") } };
		private readonly MenuItem _miTreeProperties = new MenuItem { Header = "Properties", Icon = new Image { Source = ApplicationManager.GetResourceImage("properties.png") } };
		private readonly MenuItem _miTreeRename = new MenuItem { Header = "Rename...", Icon = new Image { Source = ApplicationManager.GetResourceImage("refresh.png") } };
		private readonly MenuItem _miTreeSelectInExplorer = new MenuItem { Header = "Select in explorer", Icon = new Image { Source = ApplicationManager.GetResourceImage("arrowdown.png") } };
		private readonly Separator _miTreeSeparator = new Separator();
		private readonly MenuItem _miTreeSetEncryptionKey = new MenuItem { Header = "Set key", Icon = new Image { Source = ApplicationManager.GetResourceImage("lock.png") } };
		private readonly MenuItem _miUsage = new MenuItem { Header = "Find usages...", Icon = new Image { Source = ApplicationManager.GetResourceImage("help.png") } };

		#endregion
	}
}