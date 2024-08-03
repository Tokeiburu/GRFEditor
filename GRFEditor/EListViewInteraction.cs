using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.Tools.MapExtractor;
using GRFEditor.WPF;
using GRFEditor.WPF.PreviewTabs;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;

namespace GRFEditor {
	partial class EditorMainWindow : Window {
		private readonly KnownPositionComponent _positions = new KnownPositionComponent();
		private FileEntry _latestSelectedItem;

		#region Events

		private void _menuItemsRename_Click(object sender, RoutedEventArgs e) {
			_renamingService.RenameFile(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem as FileEntry, _grfHolder, this, _renameFileCallback);
		}

		private void _menuItemsSaveMapAs_Click(object sender, RoutedEventArgs e) {
			if (_renamingService.SaveMap(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem as FileEntry, _grfHolder, this))
				_loadListItems();
		}

		private void _miConvertRsw_Anim_Click(object sender, RoutedEventArgs e) {
			if (_renamingService.DowngradeMap(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem as FileEntry, _grfHolder, this))
				_loadListItems();
		}

		private void _miConvertRsm_Anim_Click(object sender, RoutedEventArgs e) {
			if (_renamingService.DowngradeModel(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems.Cast<FileEntry>().ToList(), _grfHolder, this, false))
				_loadListItems();
		}

		private void _miConvertRsm_Flat_Click(object sender, RoutedEventArgs e) {
			if (_renamingService.DowngradeModel(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems.Cast<FileEntry>().ToList(), _grfHolder, this, true))
				_loadListItems();
		}

		private void _menuItemsDelete_Click(object sender, RoutedEventArgs e) {
			try {
				_grfHolder.Commands.RemoveFiles(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems.Cast<FileEntry>().Select(p => p.RelativePath), _deleteFilesCallback);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsOpen_Click(object sender, RoutedEventArgs e) {
			_openingService.RunSelected(_grfHolder, _extractingService, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem as FileEntry);
		}

		private void _menuItemsOpenExplorer_Click(object sender, RoutedEventArgs e) {
			_openingService.OpenSelectedInExplorer(_grfHolder, _extractingService, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem as FileEntry);
		}

		private void _menuItemsExtract_Click(object sender, RoutedEventArgs e) {
			_extractingService.FileExtract(_grfHolder, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems);
		}

		private void _menuItemsExtractAt_Click(object sender, RoutedEventArgs e) {
			_extractingService.FileExtractTo(_grfHolder, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems);
		}

		private void _miExportAsThor_Click(object sender, RoutedEventArgs e) {
			_extractingService.ExportAsThor(_grfHolder, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems);
		}

		private void _menuItemsExportMapFiles_Click(object sender, RoutedEventArgs e) {
			ListView items = WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement);

			if (items.SelectedItem != null && items.SelectedItem as FileEntry != null) {
				FileEntry entry = (FileEntry) items.SelectedItem;

				if (new string[] { ".rsm", ".rsm2", ".gat", ".rsw", ".gnd" }.Any(p => entry.RelativePath.GetExtension() == p)) {
					WindowProvider.ShowWindow(new MapExtractorDialog(_grfHolder, entry.RelativePath), this);
				}
			}
		}

		private void _menuItemsEncrypt_Click(object sender, RoutedEventArgs e) {
			ListView items = WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement);

			if (EncryptionService.Encrypt(_grfHolder, items)) {
				if (items == _items)
					_loadListItems();
				else
					_search(true);
			}
		}

		private void _menuItemsDecrypt_Click(object sender, RoutedEventArgs e) {
			ListView items = WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement);

			if (EncryptionService.Decrypt(_grfHolder, items)) {
				if (items == _items)
					_loadListItems();
				else
					_search(true);
			}
		}

		private void _menuItemsProperties_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new PropertiesDialog(_grfHolder, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem), this);
		}

		private void _menuItemProperties_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new PropertiesDialog(_grfHolder), this);
		}

		private void _menuItemsSelect_Click(object sender, RoutedEventArgs e) {
			PreviewService.Select(_treeView, _items, ((FileEntry) _listBoxResults.SelectedItem).RelativePath);
		}

		private void _menuItemsFlagRemove_Click(object sender, RoutedEventArgs e) {
			ListView items = WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement);
			List<string> files = items.SelectedItems.OfType<FileEntry>().Select(p => p.RelativePath).ToList();

			if (files.Count > 0) {
				_grfHolder.Commands.ThorAddFilesToRemove(files, _addFilesCallback);
				_loadListItems();

				if (items == _items)
					_loadListItems();
				else
					_search(true);
			}
		}

		private void _menuItemsUsage_Click(object sender, RoutedEventArgs e) {
			if (_previewResourceIndexer == null || !_previewResourceIndexer.IsVisible) {
				try {
					if (_previewResourceIndexer != null) _previewResourceIndexer.Close();
				}
				catch {
				}

				_previewResourceIndexer = new PreviewResourceIndexer(_grfHolder, WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem);
				_previewResourceIndexer.Closed += delegate { _previewResourceIndexer = null; };
				_previewResourceIndexer.Owner = this;
				_previewResourceIndexer.Show();
			}
			else {
				_previewResourceIndexer.Update(WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItem);
			}
		}

		private void _miToNpc_Click(object sender, RoutedEventArgs e) {
			try {
				var entries = WpfUtilities.GetPlacementFromContextMenu<ListView>(sender as FrameworkElement).SelectedItems.Cast<FileEntry>().ToList();

				StringBuilder builder = new StringBuilder();

				for (int i = 0; i < entries.Count; i++) {
					if (i == entries.Count - 1) {
						builder.Append(entries[i].DisplayRelativePath.ReplaceExtension("").ToUpper());
					}
					else {
						builder.AppendLine(entries[i].DisplayRelativePath.ReplaceExtension("").ToUpper());
					}
				}

				Clipboard.SetDataObject(builder.ToString());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _items_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _items_DragLeave(object sender, DragEventArgs e) {
		}

		private void _items_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (_grfHolder.IsOpened && new string[] { ".thor", ".rgz" }.Any(p => p == _grfHolder.FileName.GetExtension())) {
					}
					else {
						if (files != null && files.All(p => p.EndsWith(".grf") || p.EndsWith(".gpf") || p.EndsWith(".rgz") || p.EndsWith(".thor"))) {
							return;
						}

						if (files != null && files.Any(p => p.EndsWith(".grf") || p.EndsWith(".gpf"))) {
							ErrorHandler.HandleException("Deselect the GRF or GPF files before dropping them here.", ErrorLevel.Low);
							return;
						}
					}

					if (files != null) {
						_grfHolder.Commands.AddFilesInDirectory(_treeViewPathManager.GetCurrentRelativePath(), files, _addFilesCallback);
						_loadListItems();
					}

					e.Handled = true;
					return;
				}

				e.Handled = true;

				if (e.Data.GetDataPresent("FileGroupDescriptorW", true) && e.Data.GetDataPresent(DataFormats.StringFormat, true)) {
					// Identify the source
					MemoryStream sourcePathStream = e.Data.GetData(DataFormats.StringFormat, true) as MemoryStream;

					if (sourcePathStream == null)
						return;

					byte[] data = new byte[sourcePathStream.Length];
					sourcePathStream.Read(data, 0, (int) sourcePathStream.Length);

					string source = Encoding.ASCII.GetString(data, 0, data.Length, '\0');

					if (source == "ListViewSearch") {
						List<string> grfFiles = new List<string>();
						foreach (FileEntry entry in _listBoxResults.SelectedItems)
							grfFiles.Add(entry.RelativePath);

						try {
							_grfHolder.Commands.BeginNoDelay();

							string currentPath = _treeViewPathManager.GetCurrentRelativePath();

							foreach (string file in grfFiles) {
								_grfHolder.Commands.Move(file, GrfPath.Combine(currentPath, Path.GetFileName(file)));
							}
						}
						finally {
							_grfHolder.Commands.End();
						}

						_loadListItems();
						_search(true);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _items_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_items.SelectedItem == null) {
					return;
				}

				_latestSelectedItem = (FileEntry) _items.SelectedItem;
				FileEntry entry = (FileEntry) _items.SelectedItem;
				_positions.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = entry.RelativePath });
				_previewService.ShowPreview(_grfHolder, Path.GetDirectoryName(entry.RelativePath), Path.GetFileName(entry.RelativePath));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _items_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && _items.SelectedItem != null) {
					var virtualFileDataObject = new VirtualFileDataObject(
						_ => Dispatcher.BeginInvoke((Action) (() => _progressBarComponent.Progress = -1)),
						_ => Dispatcher.BeginInvoke((Action) (() => _progressBarComponent.Progress = 100.0f))
						);

					List<VirtualFileDataObject.FileDescriptor> descriptors = new List<VirtualFileDataObject.FileDescriptor>();

					string currentRelativePath = _treeViewPathManager.GetCurrentRelativePath();

					foreach (FileEntry file in _items.SelectedItems) {
						descriptors.Add(new VirtualFileDataObject.FileDescriptor {
							Name = Path.GetFileName(file.RelativePath),
							GrfData = _grfHolder,
							FilePath = Path.Combine(currentRelativePath, Path.GetFileName(file.RelativePath)),
							StreamContents = (grfData, filePath, stream, _) => {
								var data = grfData.FileTable[filePath].GetDecompressedData();
								stream.Write(data, 0, data.Length);
							}
						});
					}

					virtualFileDataObject.Source = DragAndDropSource.ListView;
					virtualFileDataObject.SetData(descriptors);

					VirtualFileDataObject.DoDragDrop(_items, virtualFileDataObject, DragDropEffects.Move);
					e.Handled = true;
					return;
				}
				else {
					ListViewItem item = _items.GetObjectAtPoint<ListViewItem>(e.GetPosition(_items));

					if (item != null && item.IsSelected) {
						_items_SelectionChanged(sender, null);
					}
				}

				e.Handled = false;
			}
			catch {
			}
		}

		private void _items_KeyDown(object sender, KeyEventArgs e) {
			if (ApplicationShortcut.Is(ApplicationShortcut.Delete))
				_menuItemsDelete_Click(_items, null);
			else if (ApplicationShortcut.Is(ApplicationShortcut.Rename))
				_menuItemsRename_Click(_items, null);
			else if (ApplicationShortcut.Is(ApplicationShortcut.Copy)) {
				if (_items.SelectedItems.Count > 0) {
					Clipboard.SetDataObject(String.Join("\r\n", _items.SelectedItems.Cast<FileEntry>().Select(p => p.DisplayRelativePath).ToArray()));
				}
			}
		}

		private void _items_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				ListViewItem lvi = _items.GetObjectAtPoint<ListViewItem>(e.GetPosition(_items));

				if (lvi != null) {
					FileEntry entry = (FileEntry) _items.SelectedItem;

					_miSaveMapAs.Visibility = new string[] { ".rsw", ".gnd", ".gat" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;
					_miConvertRsw_Anim.Visibility = new string[] { ".rsw", ".gnd", ".gat" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;
					_miConvertRsm_Anim.Visibility = new string[] { ".rsm2" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;
					_miConvertRsm_Flat.Visibility = new string[] { ".rsm2" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;
					_miExportMapFiles.Visibility = new string[] { ".rsw", ".rsm", ".rsm2", ".gnd", ".gat" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;

					_miSelect.Visibility = Visibility.Collapsed;

					_miFlagRemove.Visibility = _grfHolder.FileName.GetExtension() == ".thor" ? Visibility.Visible : Visibility.Collapsed;
					e.Handled = false;
				}
				else {
					e.Handled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#endregion

		private void _renameFileCallback(string oldFileName, string newFileName, bool isExecuted) {
			_items.Dispatcher.Invoke(new Action(delegate {
				CollectionViewSource.GetDefaultView(_itemEntries).Refresh();

				if (_itemSearchEntries.Contains(_grfHolder.FileTable[isExecuted ? newFileName : oldFileName])) {
					CollectionViewSource.GetDefaultView(_itemSearchEntries).Refresh();
				}
			}));
		}

		private int _getFirstSelected(IList listViewItems, IList items) {
			if (items.Count <= 0)
				return -1;
			else if (items.Count == 1)
				return listViewItems.IndexOf(items[0]);
			else {
				int minIndex = Int32.MaxValue;
				int tempIndex;

				if (items.Count > 50)
					return -1;

				for (int i = 0; i < items.Count; i++) {
					tempIndex = listViewItems.IndexOf(items[i]);
					if (tempIndex < minIndex)
						minIndex = tempIndex;
				}

				return minIndex;
			}
		}

		private void _deleteFilesCallback(List<string> files, bool isExecuted) {
			if (isExecuted) {
				_items.Dispatcher.Invoke(new Action(delegate {
					int oldSearchItemsIndex = _getFirstSelected(_listBoxResults.Items, _listBoxResults.SelectedItems);
					int oldItemsIndex = _getFirstSelected(_items.Items, _items.SelectedItems);
					FileEntry oldItemsEntry = oldItemsIndex < 0 ? null : (FileEntry) _items.Items[oldItemsIndex];
					FileEntry oldSearchItemsEntry = oldSearchItemsIndex < 0 ? null : (FileEntry) _listBoxResults.Items[oldSearchItemsIndex];

					_items.SelectionChanged -= _items_SelectionChanged;
					_listBoxResults.SelectionChanged -= _listBoxResults_SelectionChanged;

					List<FileEntry> entries = files.Select(p => _grfHolder.FileTable[p]).ToList();

					_itemEntries.RemoveRange(entries);
					_itemSearchEntries.RemoveRange(entries);

					_items.SelectionChanged += _items_SelectionChanged;
					_listBoxResults.SelectionChanged += _listBoxResults_SelectionChanged;

					oldSearchItemsIndex = oldSearchItemsIndex >= _listBoxResults.Items.Count ? _listBoxResults.Items.Count - 1 : oldSearchItemsIndex;
					oldItemsIndex = oldItemsIndex >= _items.Items.Count ? _items.Items.Count - 1 : oldItemsIndex;

					if (oldSearchItemsEntry == null) {
					}
					else if (_listBoxResults.Items.IndexOf(oldSearchItemsEntry) > 0) {
						_listBoxResults.SelectedItem = oldSearchItemsEntry;
					}
					else {
						_listBoxResults.SelectedIndex = oldSearchItemsIndex;
					}

					if (oldItemsEntry == null) {
					}
					else if (_items.Items.IndexOf(oldItemsEntry) > 0) {
						_items.SelectedItem = oldItemsEntry;
					}
					else {
						_items.SelectedIndex = oldItemsIndex;
					}

					if (oldItemsEntry == null && oldSearchItemsEntry == null) {
						_previewService.ShowPreview(_grfHolder, _treeViewPathManager.GetCurrentRelativePath(), null);
					}
				}));
			}
		}

		private void _replaceFilesCallback(string grfPath, string fileName, string filePath, bool isExecuted) {
			if (isExecuted) {
				_treeViewPathManager.AddFolders(_grfHolder.FileName, new List<string> { grfPath });
			}
			else {
				_treeViewPathManager.AddFoldersUndo(_grfHolder.FileName);
			}
		}

		private void _addFilesCallback(List<string> filesFullPath, List<string> grfFilesFullPath, List<string> grfPaths, bool isExecuted) {
			if (isExecuted) {
				_treeViewPathManager.AddFolders(_grfHolder.FileName, grfPaths);
			}
			else {
				_treeViewPathManager.AddFoldersUndo(_grfHolder.FileName);
			}
		}

		private void _loadListItems() {
			_folderListing();
		}
	}
}