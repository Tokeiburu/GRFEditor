using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.OpenGL.MapComponents;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;

namespace GRFEditor {
	partial class EditorMainWindow : Window {
		private readonly object _filterLock = new object();
		private readonly FileEntryComparer2 _grfEntrySorter = new FileEntryComparer2();
		private readonly FileEntryComparer2 _grfSearchEntrySorter = new FileEntryComparer2();
		private readonly object _searchLock = new object();
		private RangeObservableCollection<FileEntry> _itemEntries = new RangeObservableCollection<FileEntry>();
		private RangeObservableCollection<FileEntry> _itemSearchEntries = new RangeObservableCollection<FileEntry>();
		private string _searchString = "";
		private double _previousHeight = 180;
		private readonly GrfPushMultiThread<FolderListingSearch> _searchFolderListingThread = new GrfPushMultiThread<FolderListingSearch>();

		public class FolderListingSearch {
			public string RelativePath;
			public string Search;
		}

		#region Search ListView interactions

		private void _listBoxResults_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_listBoxResults.SelectedItem != null) {
					_latestSelectedItem = _listBoxResults.SelectedItem as FileEntry;
					_positions.AddPath(new TkPath { FilePath = _grfHolder.FileName, RelativePath = _latestSelectedItem.RelativePath });
					_previewService.ShowPreview(_grfHolder, Path.GetDirectoryName(_listBoxResults.SelectedItem.ToString()),
												Path.GetFileName(_listBoxResults.SelectedItem.ToString()));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _listBoxResults_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				object item = _listBoxResults.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listBoxResults));

				if (item == null) {
					e.Handled = true;
					return;
				}

				if (_listBoxResults.SelectedItem != null) {
					FileEntry entry = (FileEntry)_listBoxResults.SelectedItem;

					_miSaveMapAs.Visibility = new string[] { ".rsw", ".gnd", ".gat" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;
					_miConvertRsm2.Visibility = new string[] { ".rsm2" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;
					_miExportMapFiles.Visibility = new string[] { ".rsw", ".rsm", ".rsm2", ".gnd", ".gat" }.Any(p => entry.RelativePath.GetExtension() == p) ? Visibility.Visible : Visibility.Collapsed;

					_miFlagRemove.Visibility = _grfHolder.FileName.IsExtension(".thor") ? Visibility.Visible : Visibility.Collapsed;

					_miSelect.Visibility = Visibility.Visible;
				}

				e.Handled = false;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _listBoxResults_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && _listBoxResults.SelectedItem != null) {
					var virtualFileDataObject = new VirtualFileDataObject(
						_ => Dispatcher.BeginInvoke((Action)(() => _progressBarComponent.Progress = -1)),
						_ => Dispatcher.BeginInvoke((Action)(() => _progressBarComponent.Progress = 100.0f))
						);

					List<VirtualFileDataObject.FileDescriptor> descriptors = new List<VirtualFileDataObject.FileDescriptor>();

					foreach (FileEntry file in _listBoxResults.SelectedItems) {
						descriptors.Add(new VirtualFileDataObject.FileDescriptor {
							Name = Path.GetFileName(file.RelativePath),
							GrfData = _grfHolder,
							FilePath = file.RelativePath,
							StreamContents = (grfData, filePath, stream, _) => {
								var data = grfData.FileTable[filePath].GetDecompressedData();
								stream.Write(data, 0, data.Length);
							}
						});
					}

					virtualFileDataObject.Source = DragAndDropSource.ListViewSearch;
					virtualFileDataObject.SetData(descriptors);

					VirtualFileDataObject.DoDragDrop(_listBoxResults, virtualFileDataObject, DragDropEffects.Move);
					e.Handled = true;
					return;
				}
				else {
					ListViewItem item = _listBoxResults.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listBoxResults));

					if (item != null) {
						if (item.IsSelected)
							_listBoxResults_SelectionChanged(item, null);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#endregion

		#region Search interactions

		private void _listBoxResults_KeyDown(object sender, KeyEventArgs e) {
			if (ApplicationShortcut.Is(ApplicationShortcut.Delete))
				_menuItemsDelete_Click(_listBoxResults, null);
			else if (ApplicationShortcut.Is(ApplicationShortcut.Rename))
				_menuItemsRename_Click(_listBoxResults, null);
			else if (ApplicationShortcut.Is(ApplicationShortcut.Copy)) {
				if (_listBoxResults.SelectedItems.Count > 0) {
					Clipboard.SetDataObject(String.Join("\r\n", _listBoxResults.SelectedItems.Cast<FileEntry>().Select(p => p.RelativePath).ToArray()));
				}
			}
		}

		private void _textBoxSearch_TextChanged(object sender, TextChangedEventArgs e) {
			_folderListing();
		}

		private void _textBox_TextChanged(object sender, EventArgs keyEventArgs) {
			_searchString = _textBoxMainSearch.Text;
			_search(true);
		}

		private void _initSearchThreads() {
			_searchFolderListingThread.Start("GrfEditor - Search filter for folder listing", (folderListingSearch, cancel) => {
				try {
					if (cancel())
						return;

					if (_items == null) return;

					if (folderListingSearch.RelativePath == null) {
						_itemEntries.Clear();
						return;
					}

					this.Dispatch(p => p._grfEntrySorter.SetOrder(WpfUtils.GetLastGetSearchAccessor(_items), WpfUtils.GetLastSortDirection(_items)));

					if (cancel())
						return;

					List<Utilities.Extension.Tuple<string, string, FileEntry>> entries = _grfHolder.FileTable.FastTupleAccessEntries;
					List<string> search = folderListingSearch.Search.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
					List<FileEntry> result;

					if (search.Any(p => p.Contains("*") || p.Contains("?"))) {
						IEnumerable<Utilities.Extension.Tuple<string, string, FileEntry>> res = entries.Where(p => String.Compare(p.Item1, folderListingSearch.RelativePath, StringComparison.OrdinalIgnoreCase) == 0);

						//if (cancel())
						//	return;

						foreach (var query in search) {
							if (query.Contains("*") || query.Contains("?")) {
								Regex regex = new Regex(Methods.WildcardToRegex(query), RegexOptions.IgnoreCase);
								res = res.Where(p => regex.IsMatch(p.Item2));
							}
							else {
								res = res.Where(p => p.Item2.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) != -1);
							}

							//if (cancel())
							//	return;
						}

						//if (cancel())
						//	return;

						result = res.Select(p => p.Item3).ToList();
					}
					else {
						var res = entries.Where(p => String.Compare(p.Item1, folderListingSearch.RelativePath, StringComparison.OrdinalIgnoreCase) == 0 && search.All(q => p.Item2.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) != -1)).Select(p => p.Item3).ToList();

						//if (cancel())
						//	return;

						if (res.Count < 10000) {
							_grfEntrySorter.UseAlphaNum = true;
							result = res.OrderBy(p => p, _grfEntrySorter).ToList();
						}
						else {
							_grfEntrySorter.UseAlphaNum = false;
							result = res.OrderBy(p => p, _grfEntrySorter).ToList();
						}
					}

					//if (cancel())
					//	return;

					//result.Clear();
					//result = new List<FileEntry>();
					//result.AddRange(_grfHolder.FileTable.FastAccessEntries.Select(p => p.Value).ToList());
					_itemEntries = new RangeObservableCollection<FileEntry>(result);

					for (int i = 0; i < result.Count; i++) {
						if (result[i].DataImage == null) {
							result[i].DataImage = IconProvider.GetSmallIcon(result[i].RelativePath);
						}
					}

					//if (cancel())
					//	return;

					_items.Dispatch(delegate {
						_items.ItemsSource = _itemEntries;
					});
				}
				catch {
				}
			});

			Dispatcher.ShutdownStarted += delegate {
				_searchFolderListingThread.Terminate();
				TextureManager.ExitTextureThreads();
			};
		}

		private void _search(bool isAsync = true) {
			string currentSearch = _searchString;

			GrfThread.Start(delegate {
				lock (_searchLock) {
					try {
						if (currentSearch != _searchString)
							return;

						if (currentSearch == "") {
							_previousHeight = _rDefSearch.Dispatch(p => p.ActualHeight);

							if (_previousHeight <= 0)
								_previousHeight = 180;

							_listBoxResults.Dispatch(p => p.Visibility = Visibility.Collapsed);
							_rDefSearch.Dispatch(p => p.Height = new GridLength(-1, GridUnitType.Auto));
							_gridSplitterSearch.Dispatch(p => p.Visibility = Visibility.Collapsed);
							return;
						}

						if (currentSearch.Split(' ').All(p => p.Length == 0))
							return;

						_listBoxResults.Dispatch(delegate {
							if (_listBoxResults.Visibility != Visibility.Visible)
								_rDefSearch.Height = new GridLength(_previousHeight);

							_listBoxResults.Visibility = Visibility.Visible;
							_gridSplitterSearch.Visibility = Visibility.Visible;
						});

						this.Dispatch(p => p._grfSearchEntrySorter.SetOrder(WpfUtils.GetLastGetSearchAccessor(_listBoxResults), WpfUtils.GetLastSortDirection(_listBoxResults)));

						if (_grfHolder.IsClosed)
							return;

						List<KeyValuePair<string, FileEntry>> entries = _grfHolder.FileTable.FastAccessEntries;
						List<string> search = currentSearch.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
						List<FileEntry> result;

						if (search.Any(p => p.Contains("*") || p.Contains("?"))) {
							IEnumerable<Utilities.Extension.Tuple<string, string, FileEntry>> res = _grfHolder.FileTable.FastTupleAccessEntries;

							foreach (var query in search) {
								if (query.Contains("*") || query.Contains("?")) {
									Regex regex = new Regex(Methods.WildcardToRegex(query), RegexOptions.IgnoreCase);
									res = res.Where(p => regex.IsMatch(p.Item2) || regex.IsMatch(p.Item3.RelativePath));
								}
								else {
									res = res.Where(p => p.Item3.RelativePath.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) != -1);
								}
							}

							result = res.Select(p => p.Item3).ToList();
						}
						else {
							var res = entries.Where(p => search.All(q => p.Key.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) != -1)).Select(p => p.Value).ToList();

							if (res.Count < 10000) {
								_grfSearchEntrySorter.UseAlphaNum = true;
								result = res.OrderBy(p => p, _grfSearchEntrySorter).ToList();
							}
							else {
								_grfSearchEntrySorter.UseAlphaNum = false;
								result = res.OrderBy(p => p, _grfSearchEntrySorter).ToList();
							}
						}

						_itemSearchEntries = new RangeObservableCollection<FileEntry>(result);

						if (currentSearch != _searchString) return;

						for (int i = 0; i < result.Count; i++) {
							if (result[i].DataImage == null) {
								result[i].DataImage = IconProvider.GetSmallIcon(result[i].RelativePath);
							}
						}

						_listBoxResults.Dispatch(p => p.ItemsSource = _itemSearchEntries);
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			}, "GrfEditor - Search filter for all items thread", isAsync);
		}

		private void _folderListing() {
			_searchFolderListingThread.Push(new FolderListingSearch { Search = _textBoxSearch.Dispatch(p => p.Text), RelativePath = _treeViewPathManager.GetCurrentRelativePath() });
		}

		#region Nested type: FileEntryComparer

		public class FileEntryComparer<T> : IComparer<T> {
			private readonly DefaultListViewComparer<T> _internalSearch = new DefaultListViewComparer<T>();
			private string _searchGetAccessor;

			#region IComparer<T> Members

			public int Compare(T x, T y) {
				if (_searchGetAccessor != null)
					return _internalSearch.Compare(x, y);

				return 0;
			}

			#endregion

			public void SetOrder(string searchGetAccessor, ListSortDirection direction) {
				if (searchGetAccessor != null) {
					_searchGetAccessor = searchGetAccessor;
					_internalSearch.SetSort(searchGetAccessor, direction);
				}
			}
		}

		#endregion

		#region Nested type: FileEntryComparer2

		public class FileEntryComparer2 : IComparer<FileEntry> {
			private readonly DefaultListViewComparer<FileEntry> _internalSearch = new DefaultListViewComparer<FileEntry>();
			private ListSortDirection _direction;
			private string _searchGetAccessor;
			private readonly AlphanumComparator _alphanumComparer = new AlphanumComparator(StringComparison.OrdinalIgnoreCase);
			public bool UseAlphaNum { get; set; }

			#region IComparer<FileEntry> Members

			public int Compare(FileEntry x, FileEntry y) {
				if (_searchGetAccessor == "DisplayRelativePath" || _searchGetAccessor == "RelativePath") {
					if (UseAlphaNum) {
						if (_direction == ListSortDirection.Ascending)
							return _alphanumComparer.Compare(x.RelativePath, y.RelativePath);
						return _alphanumComparer.Compare(y.RelativePath, x.RelativePath);
					}
					else {
						if (_direction == ListSortDirection.Ascending)
							return String.CompareOrdinal(x.RelativePath, y.RelativePath);
						return String.CompareOrdinal(y.RelativePath, x.RelativePath);
					}
				}

				if (_searchGetAccessor == "NewSizeDecompressed") {
					return _direction == ListSortDirection.Ascending ? (x.NewSizeDecompressed - y.NewSizeDecompressed) : (y.NewSizeDecompressed - x.NewSizeDecompressed);
				}

				if (_searchGetAccessor == "FileType") {
					if (_direction == ListSortDirection.Ascending)
						return String.CompareOrdinal(x.FileType, y.FileType);
					return String.CompareOrdinal(y.FileType, x.FileType);
				}

				if (_searchGetAccessor != null)
					return _internalSearch.Compare(x, y);

				return 0;
			}

			#endregion

			public void SetOrder(string searchGetAccessor, ListSortDirection direction) {
				_direction = direction;
				if (searchGetAccessor != null) {
					_searchGetAccessor = searchGetAccessor;
					_internalSearch.SetSort(searchGetAccessor, direction);
				}
			}
		}

		#endregion

		#endregion
	}
}