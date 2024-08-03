using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.FileFormats;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.WPF;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;

namespace GRFEditor {
	partial class EditorMainWindow : Window {
		private bool _reloading;

		#region TreeView main events

		private void _menuItemRename_Click(object sender, RoutedEventArgs e) {
			_renamingService.RenameFolder(_treeView.SelectedItem, _treeView, _grfHolder, this, _renameFolderCallback, _deleteFolderCallback, _addFilesCallback);
		}

		private void _menuItemDelete_Click(object sender, RoutedEventArgs e) {
			try {
				_grfHolder.Commands.RemoveFolder(_treeViewPathManager.GetCurrentRelativePath(), _deleteFolderCallback);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemAdd_Click(object sender, RoutedEventArgs e) {
			try {
				var dialog = new AddFileDialog(_treeView, _treeViewPathManager.GetCurrentPath());
				dialog.Owner = this;

				if (dialog.ShowDialog() == true && !String.IsNullOrEmpty(dialog.FilePath) && dialog.GrfPath != null) {
					_grfHolder.Commands.AddFileInDirectory(dialog.GrfPath.RelativePath, dialog.FilePath, _addFilesCallback);
					_loadListItems();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuOpen_Click(object sender, RoutedEventArgs e) {
			_openingService.OpenSelectedFolder(_grfHolder, _treeView, _extractingService);
		}

		private void _menuItemExtractFiles_Click(object sender, RoutedEventArgs e) {
			_extractingService.RootFilesExtract(_grfHolder, _treeView.SelectedItem);
		}

		private void _menuItemExtractFilesTo_Click(object sender, RoutedEventArgs e) {
			_extractingService.RootFilesExtractTo(_grfHolder, _treeView.SelectedItem);
		}

		private void _menuItemExtract_Click(object sender, RoutedEventArgs e) {
			_extractingService.FolderExtract(_grfHolder, _treeView.SelectedItem);
		}

		private void _menuItemExtractTo_Click(object sender, RoutedEventArgs e) {
			_extractingService.FolderExtractTo(_grfHolder, _treeView.SelectedItem);
		}

		private void _menuItemNewFolder_Click(object sender, RoutedEventArgs e) {
			try {
				InputDialog input = WindowProvider.ShowWindow<InputDialog>(new InputDialog("Enter the new folder name : ", "New folder", "", true), this);

				if (input.DialogResult == true) {
					if (String.IsNullOrEmpty(input.Input))
						return;

					var treeViewItem = _treeView.SelectedItem as TkTreeViewItem;
					if (treeViewItem != null) {
						if (treeViewItem.Items.Cast<TkTreeViewItem>().Any(tItem => tItem.HeaderText == input.Input)) {
							ErrorHandler.HandleException("The folder name already exists.", ErrorLevel.Low);
							return;
						}

						string currentPath = _treeViewPathManager.GetCurrentRelativePath();
						_grfHolder.Commands.AddFolder(Path.Combine(currentPath, input.Input), _addedFolderCallback);
					}
					else {
						ErrorHandler.HandleException("No folder selected.", ErrorLevel.Low);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemEncrypt_Click(object sender, RoutedEventArgs e) {
			var relativePath = _treeViewPathManager.GetCurrentRelativePath();
			if (EncryptionService.Encrypt(_grfHolder, _grfHolder.FileTable.Entries.Where(p => p.RelativePath.StartsWith(relativePath)).ToList()))
				_loadListItems();
		}

		private void _menuItemDecrypt_Click(object sender, RoutedEventArgs e) {
			var relativePath = _treeViewPathManager.GetCurrentRelativePath();
			if (EncryptionService.Decrypt(_grfHolder, _grfHolder.FileTable.Entries.Where(p => p.RelativePath.StartsWith(relativePath)).ToList()))
				_loadListItems();
		}

		private void _menuItemSetEncryptionKey_Click(object sender, RoutedEventArgs e) {
			_checkIfEncrypted(false);
		}

		private void _menuItemFlagRemove_Click(object sender, RoutedEventArgs e) {
			try {
				InputDialog dialog = new InputDialog("Type in the files you want to remove (full relative path).\r\nExample : \r\ndata\\map.rsw\r\ndata\\map.gnd.", "Add files to remove", "", false, false);
				dialog.Owner = this;
				dialog.TextBoxInput.AcceptsReturn = true;
				dialog.TextBoxInput.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				dialog.TextBoxInput.TextWrapping = TextWrapping.Wrap;
				dialog.TextBoxInput.Height = 200;
				dialog.TextBoxInput.MinHeight = 200;
				dialog.TextBoxInput.MaxHeight = 200;

				if (dialog.ShowDialog() == true) {
					List<string> files = dialog.Input.Replace("\r", "").Replace("\n\n", "\n").Replace("\n", ",").Trim(new char[] { '\r', '\n' }).Split(',').ToList();

					if (files.Count > 0) {
						_grfHolder.Commands.ThorAddFilesToRemove(files, _addFilesCallback);
						_loadListItems();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemCloseGrf_Click(object sender, RoutedEventArgs e) {
			_closeGrf();
		}

		private bool _closeGrf() {
			if (!_validateNewContainer()) return false;
			_saveTreeExpansion();
			_newWithDataFolder(false);
			return true;
		}

		private void _menuItemExtractRgz_Click(object sender, RoutedEventArgs e) {
			_asyncOperation.SetAndRunOperation(new GrfThread(_extractRgz, _grfHolder, 200));
		}

		private void _extractRgz() {
			string path = PathRequest.FolderExtract();

			if (path != null) {
				Rgz.ExtractRgz(_grfHolder, _grfHolder.FileName, path, true);
			}
		}

		private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			TkTreeViewItem item = _treeView.SelectedItem as TkTreeViewItem;

			if (item != null) {
				_loadListItems();
				_previewService.ShowPreview(_grfHolder, _treeViewPathManager.GetCurrentRelativePath(), null);
			}
		}

		private void _treeView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			var item = _treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));

			if (item != null) {
				_contextMenuNodes.Items.Cast<UIElement>().ToList().ForEach(p => p.Visibility = Visibility.Visible);

				if (item is ProjectTreeViewItem) {
					string ext = ((ProjectTreeViewItem) item).TkPath.FilePath.GetExtension();
					if (ext == ".rgz" || ext == ".thor") {
						_miTreeOpenExplorer.Visibility = Visibility.Collapsed;
						_miTreeRename.Visibility = Visibility.Collapsed;
						_miTreeDelete.Visibility = Visibility.Collapsed;
						_miTreeAdd.Visibility = Visibility.Collapsed;
						_miTreeNewFolder.Visibility = Visibility.Collapsed;
					}
					else {
						_miTreeOpenExplorer.Visibility = Visibility.Collapsed;
						_miTreeRename.Visibility = Visibility.Collapsed;
						_miTreeDelete.Visibility = Visibility.Collapsed;
					}
				}
				else {
					_miTreeProperties.Visibility = Visibility.Collapsed;
					_miTreeSelectInExplorer.Visibility = Visibility.Collapsed;
				}

				if (_grfHolder.FileName.GetExtension() != ".thor") {
					_miTreeFlagRemove.Visibility = Visibility.Collapsed;
				}

				_treeView.ContextMenu.IsOpen = true;
				e.Handled = false;
			}
			else {
				_treeView.ContextMenu.IsOpen = false;
				e.Handled = true;
			}
		}

		private void _treeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			var item = _treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));
			
			if (item != null) {
				item.IsSelected = true;
			}
		}

		private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			var item = _treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));

			if (item != null && item == _treeView.SelectedItem) {
				_treeView_SelectedItemChanged(sender, null);
			}
		}

		private void _treeView_Drop(object sender, DragEventArgs e) {
			try {
				string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

				#region Files dropped

				// Comes from outside the application, files being dropped
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					if (_grfHolder.IsOpened && new string[] { ".thor", ".rgz" }.Any(p => p == _grfHolder.FileName.GetExtension())) {
					}
					else {
						if (files != null && files.All(p => p.EndsWith(".grf") || p.EndsWith(".gpf") || p.EndsWith(".rgz") || p.EndsWith(".thor"))) {
							return; // Gives the event to the MenuInteracionDrop
						}

						if (files != null && files.Any(p => p.EndsWith(".grf") || p.EndsWith(".gpf"))) {
							ErrorHandler.HandleException("Deselect the GRF or GPF files before dropping them here.", ErrorLevel.Low);
							return;
						}
					}

					var tvi = _treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));

					if (tvi == null) {
						if (files != null && GrfEditorConfiguration.AutomaticallyPlaceFiles && files.Length < 30) {
							foreach (var file in files) {
								_placeFile(file);
							}
						}

						return;
					}

					var previewItem = (TkTreeViewItem)_treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));
					string currentPath = TreeViewPathManager.GetTkPath(previewItem).RelativePath;
					bool hasFoundAtLeastOne = false;
					bool hasFoundAtLeastOneRoot = false;

					if (files != null && files.All(Directory.Exists)) {
						if (previewItem is ProjectTreeViewItem) {
							// Files were dropped on the "root" node
							foreach (string f in files) {
								string folderName = Path.GetFileName(f);
								if (_treeView.Items.Cast<TkTreeViewItem>().Any(p => p.HeaderText.ToString(CultureInfo.InvariantCulture) == folderName)) {
									hasFoundAtLeastOneRoot = true;
									break;
								}
							}
						}
						else if (files.Any(p => Path.GetFileName(p) == previewItem.HeaderText.ToString(CultureInfo.InvariantCulture))) {
							hasFoundAtLeastOne = true;
						}
					}

					GrfThread.Start(delegate {
						try {
							if (hasFoundAtLeastOneRoot) {
								MessageBoxResult res = WindowProvider.ShowDialog("At least one folder with the same name already exists. Do you want to merge them?", "Merge", MessageBoxButton.YesNo);

								if (res == MessageBoxResult.No)
									return;

								if (res == MessageBoxResult.Yes)
									currentPath = Path.GetDirectoryName(currentPath);
							}

							if (hasFoundAtLeastOne) {
								MessageBoxResult res = WindowProvider.ShowDialog("At least one folder with the same name already exists. Do you want to merge them or include the folders as subdirectories?", "Merge", MessageBoxButton.YesNoCancel, "Merge", "Subdirectories");

								if (res == MessageBoxResult.Cancel)
									return;

								if (res == MessageBoxResult.Yes)
									currentPath = Path.GetDirectoryName(currentPath);
							}

							_progressBarComponent.Progress = -1;
							_grfHolder.Commands.AddFilesInDirectory(currentPath, files, _addFilesCallback);
							_progressBarComponent.Progress = 100.0f;
							_loadListItems();
							_reload();
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}, "GrfEditor - File dropped thread");
					e.Handled = true;
					return;
				}

				#endregion

				e.Handled = true;

				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					// Not tested 
					// Note : because the execution literally can't come here?...
					if (files != null && files.All(p => p.EndsWith(".grf") || p.EndsWith(".gpf"))) {
						return;
					}

					if (files != null && files.Any(p => p.EndsWith(".grf") || p.EndsWith(".gpf"))) {
						ErrorHandler.HandleException("Deselect the GRF or GPF files before dropping them here.", ErrorLevel.Low);
						return;
					}

					TkTreeViewItem previewItem = (TkTreeViewItem)_treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));
					string currentPath = TreeViewPathManager.GetTkPath(previewItem).RelativePath;

					if (files != null && previewItem != null && previewItem.HeaderText == Path.GetFileName(Path.GetDirectoryName(files[0]))) {
						MessageBoxResult res = WindowProvider.ShowDialog("Do you want to merge the current directory or include it as a subfolder?", "Merge", MessageBoxButton.YesNoCancel);

						if (res == MessageBoxResult.Cancel)
							return;

						if (res == MessageBoxResult.Yes)
							currentPath = Path.GetDirectoryName(currentPath);
					}

					_grfHolder.Commands.AddFilesInDirectory(currentPath, files, _addFilesCallback);
					_loadListItems();
					_reload();
				}
				else {
					TkPath path = TreeViewPathManager.GetTkPath(_treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView)));

					if (path == null)
						return;

					string previewPath = path.RelativePath;

					if (e.Data.GetDataPresent("FileGroupDescriptorW", true) && e.Data.GetDataPresent(DataFormats.StringFormat, true)) {
						// Identify the source
						MemoryStream sourcePathStream = e.Data.GetData(DataFormats.StringFormat, true) as MemoryStream;

						if (sourcePathStream == null)
							return;

						byte[] data = new byte[sourcePathStream.Length];
						sourcePathStream.Read(data, 0, (int) sourcePathStream.Length);

						string source = Encoding.ASCII.GetString(data, 0, data.Length, '\0');

						if (source == "ListView") {
							List<string> grfFiles = new List<string>();
							foreach (FileEntry entry in _items.SelectedItems)
								grfFiles.Add(Path.GetFileName(entry.RelativePath));

							_grfHolder.Commands.MoveFiles(_treeViewPathManager.GetCurrentRelativePath(), previewPath, grfFiles);
							_loadListItems();
						}
						else if (source == "ListViewSearch") {
							List<string> grfFiles = new List<string>();
							foreach (FileEntry entry in _listBoxResults.SelectedItems)
								grfFiles.Add(entry.RelativePath);

							try {
								_grfHolder.Commands.Begin();

								foreach (string file in grfFiles) {
									_grfHolder.Commands.Move(file, GrfPath.Combine(previewPath, Path.GetFileName(file)));
								}
							}
							catch {
								_grfHolder.Commands.CancelEdit();
								throw;
							}
							finally {
								_grfHolder.Commands.End();
							}

							_loadListItems();
							_search();
						}
						else if (source == "TreeView") {
							string sourcePath = _treeViewPathManager.GetCurrentRelativePath();

							if (sourcePath == previewPath)
								return;

							string destinationPath = Path.Combine(previewPath, Path.GetFileName(sourcePath));

							if (_grfHolder.FileTable.GetFiles(sourcePath, SearchOption.AllDirectories).Count == 0) {
								try {
									//List<IContainerCommand<FileEntry>> commands = new List<IContainerCommand<FileEntry>>();
									_grfHolder.Commands.BeginNoDelay();
									_grfHolder.Commands.RemoveFolder(sourcePath, _deleteFolderCallback);
									_grfHolder.Commands.AddFolder(destinationPath, _addedFolderCallback);
								}
								catch (Exception err) {
									_grfHolder.Commands.CancelEdit();
									ErrorHandler.HandleException(err);
								}
								finally {
									_grfHolder.Commands.End();
								}
							}
							else {
								_grfHolder.Commands.Rename(sourcePath, destinationPath, _renameFolderCallback);
							}

							((TreeViewItem) _treeView.SelectedItem).IsSelected = false;
							TreeViewItem item = _treeView.GetObjectAtPoint<TreeViewItem>(e.GetPosition(_treeView));
							TreeViewItem selectedNode = _treeViewPathManager.GetNode(new TkPath { FilePath = _grfHolder.FileName, RelativePath = destinationPath });

							if (selectedNode != null)
								selectedNode.IsSelected = false;

							if (item != null) {
								item.IsSelected = true;
								item.IsExpanded = true;
							}

							_reload();
						}
					}
					else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) {
						// Data most likely comes from the list view
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _placeFile(string file) {
			string ext = file.GetExtension();

			try {
				_grfHolder.Commands.BeginNoDelay();
				string root = _grfHolder.FileName.IsExtension(".thor", ".rgz") ? GrfStrings.RgzRoot : "";
				string currentFileName = Path.GetFileName(file);

				switch(ext) {
					case ".rsw":
					case ".gnd":
					case ".gat":
						_grfHolder.Commands.AddFile(root + GrfPath.Combine(@"data\", currentFileName), file);
						break;
					case ".lua":
					case ".lub":
						break;
					case "":
					case null:
						return;
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

		private void _treeView_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Delete)
				_menuItemDelete_Click(null, null);

			if (e.Key == Key.F2) {
				_renamingService.RenameFolder(_treeView.SelectedItem, _treeView, _grfHolder, this, _renameFolderCallback, _deleteFolderCallback, _addFilesCallback);
			}
		}

		#endregion

		#region TreeView loading (logic)

		private void _renameFolderCallback(string oldName, string newName, bool isExecuted) {
			if (!isExecuted) {
				string temp = oldName;
				oldName = newName;
				newName = temp;
			}

			_treeViewPathManager.Rename(new TkPath { FilePath = _grfHolder.FileName, RelativePath = oldName }, new TkPath { FilePath = _grfHolder.FileName, RelativePath = newName });
		}

		private void _deleteFolderCallback(List<string> folders, bool isExecuted) {
			foreach (string folder in folders) {
				if (isExecuted) {
					_items.Dispatch(p => _itemEntries.Clear());
					_treeViewPathManager.DeletePath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = folder });
				}
				else {
					_treeViewPathManager.UndoDeletePath(_grfHolder.FileName);
				}
			}
		}

		private void _addedFolderCallback(string folderName, bool isExecuted) {
			if (isExecuted) {
				_treeViewPathManager.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = folderName });
				_treeViewPathManager.Expand(new TkPath { FilePath = _grfHolder.FileName, RelativePath = Path.GetDirectoryName(folderName) });
			}
			else {
				_treeViewPathManager.DeletePath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = folderName }, false);
			}
		}

		public class GrfTreeNode {
			public GrfTreeNode Parent;
			public Dictionary<string, GrfTreeNode> Children = new Dictionary<string, GrfTreeNode>();
			public string Name;

			public GrfTreeNode(GrfTreeNode parent, string name) {
				Parent = parent;
				Name = name;
			}

			public void AddPath(string[] paths, int index) {
				if (index >= paths.Length)
					return;

				GrfTreeNode child;

				if (!Children.TryGetValue(paths[index], out child)) {
					child = new GrfTreeNode(this, paths[index]);
					Children[paths[index]] = child;
				}

				child.AddPath(paths, index + 1);
			}

			public TkTreeViewItem GetTvi(TkView tree, Style style) {
				var tvi = new TkTreeViewItem(tree) { HeaderText = Name };
				tvi.Style = style;
				
				foreach (var child in Children) {
					tvi.Items.Add(child.Value.GetTvi(tree, style));
				}

				return tvi;
			}
		}

		internal bool Load(GrfLoadingSettings settings = null) {
			if (settings != null)
				_grfLoadingSettings = settings;

			if (settings == null)
				settings = _grfLoadingSettings;

			if (_grfHolder.IsOpened && _grfHolder.CancelReload) {
				_grfHolder.CancelReload = false;
				return false;
			}

			if (!_validateNewContainer()) return false;

			if (settings.FileName == null) {
				_newWithDataFolder(true);
			}

			_reloading = true;
			GrfThread.Start(delegate {
				try {
					if (settings.FileName == null) {
						return;
					}

					if (!File.Exists(settings.FileName)) {
						return;
					}

					_menuItemExtractRgz.Dispatch(p => p.IsEnabled = settings.FileName.ToLower().EndsWith(".rgz"));

					bool hasAKey = false;

					_saveTreeExpansion();

					if (settings.VisualReloadRequired) {
						_listBoxResults.Dispatch(p => _itemSearchEntries.Clear());
						_items.Dispatch(p => _itemEntries.Clear());
						_treeViewPathManager.ClearAll();
					}
					else {
						if (_grfHolder.IsOpened)
							hasAKey = _grfHolder.Header.EncryptionKey != null;
						else if (settings.ReloadKey)
							hasAKey = true;
					}

					if (_grfLoadingSettings.VisualReloadRequired)
						_progressBarComponent.Progress = -1;

					_treeViewPathManager.ClearCommands();
					_positions.Reset();
					_grfHolder.Close();

					_grfHolder.Open(settings.FileName);
					
					if (_grfHolder.Header == null) {
						this.Dispatch(p => p.Title = "GRF Editor");
						_treeViewPathManager.ClearAll();
						_grfHolder.Close();
						_progressBarComponent.Progress = 100;
					}
					else {
						this.Dispatch(p => p.Title = "GRF Editor - " + Methods.CutFileName(settings.FileName));

						if (_grfHolder.Header.IsEncrypted) {
							_progressBarComponent.Progress = -2;
						}

						if (settings.VisualReloadRequired)
							_checkIfEncrypted();
						else if (hasAKey && _grfHolder.Header.IsEncrypted)
							_grfHolder.Header.SetKey(Configuration.EncryptorPassword, _grfHolder);

						_asyncOperation.QueueAndRunOperation(new GrfThread(() => _grfHolder.SetEncryptionFlag(), _grfHolder, 300, null, true));

						//if (!_grfHolder.Header.IsEncrypted) {
						//	_grfHolder.SetLzmaFlag();	
						//}

						if (!settings.VisualReloadRequired) {
							_treeViewPathManager.RenamePrimaryProject(settings.FileName);
						}

						if (settings.VisualReloadRequired) {
							//var paths = _grfHolder.FileTable.Files.Select(GrfPath.GetDirectoryName).Distinct().Where(p => !String.IsNullOrEmpty(p)).ToList();
							//
							//var rootPath = Path.GetFileName(settings.FileName);
							//GrfTreeNode root = new GrfTreeNode(null, rootPath);
							//
							//foreach (var path in paths) {
							//	var splitPath = (rootPath + "\\" + path).Replace("\\\\", "\\").Split('\\');
							//
							//	root.AddPath(splitPath, 0);
							//}
							
							//this.Dispatch(delegate {
							//	//_treeView.ItemContainerStyle = (Style)this.FindResource("GrfTreeViewStyle");
							//	//_treeView.Items.Add(root.GetTvi((Style)this.FindResource("GrfTreeViewStyle")));
							//
							//	var style = (Style)this.FindResource("TkTreeViewItemStyle");
							//	//this.HeaderTemplate.Triggers.Add(new Trigger() { })
							//	
							//	_treeView.Items.Add(root.GetTvi(_treeView, style));
							//	//_treeView.ItemTemplate
							//});

							_treeViewPathManager.AddPath(new TkPath { FilePath = settings.FileName, RelativePath = "" });
							_treeViewPathManager.AddPaths(settings.FileName, _grfHolder.FileTable.Files.Select(GrfPath.GetDirectoryName).Distinct().Where(p => !String.IsNullOrEmpty(p)).ToList());
						}

						if (settings.VisualReloadRequired) {
							_treeViewPathManager.ExpandFirstNode();
							_treeViewPathManager.SelectFirstNode();
						}
						//
						if (settings.VisualReloadRequired && Configuration.TreeBehaviorExpandSpecificFolders) {
							List<string> paths = Methods.StringToList(Configuration.TreeBehaviorSpecificFolders);
						
							foreach (string path in paths) {
								_treeViewPathManager.Expand(path);
							}
						}
						
						if (settings.VisualReloadRequired && Configuration.TreeBehaviorSaveExpansion) {
							List<string> metaContainers = Methods.StringToList(Configuration.TreeBehaviorSaveExpansionFolders);
						
							if (metaContainers.Any(p => p.StartsWith(settings.FileName + ">"))) {
								string metaContainer = metaContainers.First(p => p.StartsWith(settings.FileName));
								List<string> paths = metaContainer.Split('>')[1].Split(':').ToList();
						
								foreach (string path in paths) {
									_treeViewPathManager.Expand(new TkPath { FilePath = settings.FileName, RelativePath = path });
								}
							}
						}
						
						if (settings.VisualReloadRequired && Configuration.TreeBehaviorSelectLatest) {
							List<string> tkPaths = Methods.StringToList(Configuration.TreeBehaviorSelectLatestFolders);
						
							if (tkPaths.Any(p => p.StartsWith(settings.FileName + "?"))) {
								string tkPath = tkPaths.First(p => p.StartsWith(settings.FileName + "?"));
								_treeViewPathManager.Select(new TkPath(tkPath));
							}
						}

						_recentFilesManager.AddRecentFile(settings.FileName);
						_progressBarComponent.SetSpecialState(_grfHolder.Header != null && _grfHolder.Header.FoundErrors ? TkProgressBar.ProgressStatus.ErrorsDetected : TkProgressBar.ProgressStatus.Finished);

						_search();

						if (!settings.VisualReloadRequired)
							_loadListItems();
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
				finally {
					if (_progressBarComponent.Progress == -1 || _progressBarComponent.Progress == 0)
						_progressBarComponent.Progress = 100;

					_grfLoadingSettings.VisualReloadRequired = true;
					_reloading = false;
				}
			}, "GrfEditor - GRF loading thread");

			return true;
		}

		private void _saveTreeExpansion() {
			try {
				if (Configuration.TreeBehaviorSaveExpansion) {
					List<string> metaContainers = Methods.StringToList(Configuration.TreeBehaviorSaveExpansionFolders);

					string containerPath = _grfHolder.IsOpened ? _grfHolder.FileName : _grfLoadingSettings.FileName;

					if (containerPath == null)
						return;

					if (metaContainers.Any(p => p.StartsWith(containerPath + ">"))) {
						metaContainers.Remove(metaContainers.First(p => p.StartsWith(containerPath + ">")));
					}

					List<string> expandedFolders = _treeViewPathManager.GetExpandedFolders();

					if (expandedFolders == null)
						return;

					if (expandedFolders.Count > 0)
						metaContainers.Add(containerPath + ">" + expandedFolders.Aggregate((a, b) => a + ':' + b));

					Configuration.TreeBehaviorSaveExpansionFolders = Methods.ListToString(metaContainers);
				}

				if (Configuration.TreeBehaviorSelectLatest) {
					string currentPath = _treeViewPathManager.GetCurrentPath();

					if (!String.IsNullOrEmpty(currentPath)) {
						List<string> tkPaths = Methods.StringToList(Configuration.TreeBehaviorSelectLatestFolders);

						if (tkPaths.Any(p => p.StartsWith(_treeViewPathManager.GetContainerPath() + "?"))) {
							tkPaths[tkPaths.IndexOf(tkPaths.First(p => p.StartsWith(_treeViewPathManager.GetContainerPath() + "?")))] = currentPath;
						}
						else {
							tkPaths.Add(currentPath);
						}

						Configuration.TreeBehaviorSelectLatestFolders = Methods.ListToString(tkPaths);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _reload() {
			new Thread(new ThreadStart(delegate {
				foreach (string pathname in _grfHolder.FileTable.Directories) {
					_treeViewPathManager.AddPath(new TkPath { FilePath = _grfLoadingSettings.FileName, RelativePath = pathname });
				}
			})) { Name = "GrfEditor - TreeView path loading thread" }.Start();
		}

		#endregion
	}
}