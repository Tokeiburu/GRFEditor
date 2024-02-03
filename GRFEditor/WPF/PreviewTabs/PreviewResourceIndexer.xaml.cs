using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.Tools.MapExtractor;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;
using OpeningService = Utilities.Services.OpeningService;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewResourceIndexer.xaml
	/// </summary>
	public partial class PreviewResourceIndexer : TkWindow, IProgress, IDisposable {
		private readonly AsyncOperation _asyncOperation;
		private readonly GrfHolder _grf;
		private readonly GrfIndexor _indexer = new GrfIndexor();
		private readonly object _lock = new object();
		private string _destinationPath;
		private string _fileName;
		private bool _reloadIndex = true;
		private float _subIndex;

		public PreviewResourceIndexer() {
		}

		public PreviewResourceIndexer(GrfHolder grf, object selectedItem) : base("Resource indexer", "add.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			_grf = grf;

			if (selectedItem is FileEntry) {
				_fileName = ((FileEntry) selectedItem).RelativePath;
			}

			InitializeComponent();

			_tree.CopyMethod = delegate {
				if (_tree.SelectedItem != null)
					Clipboard.SetDataObject(((MapExtractorTreeViewItem)_tree.SelectedItem).ResourcePath.RelativePath);
			};

			_labelHeader.Dispatch(p => p.Content = "Searching usages of " + Path.GetFileName(_fileName));
			ShowInTaskbar = true;

			_asyncOperation = new AsyncOperation(_progressBarComponent);

			_asyncOperation.SetAndRunOperation(new GrfThread(() => _updateMapFiles(_fileName, null), this, 200, null, false, true));
			_quickPreview.Set(_asyncOperation);

			_tree.DoDragDropCustomMethod = delegate {
				VirtualFileDataObjectProgress vfop = new VirtualFileDataObjectProgress();
				VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject(
					_ => _asyncOperation.SetAndRunOperation(new GrfThread(vfop.Update, vfop, 500, null)),
					_ => vfop.Finished = true
					);

				IEnumerable<MapExtractorTreeViewItem> allNodes = _tree.SelectedItems.Items.Cast<MapExtractorTreeViewItem>().Where(p => p.ResourcePath != null);

				List<VirtualFileDataObject.FileDescriptor> descriptors = allNodes.Select(node => new VirtualFileDataObject.FileDescriptor {
					Name = Path.GetFileName(node.ResourcePath.RelativePath),
					FilePath = node.ResourcePath.RelativePath,
					Argument = GrfEditorConfiguration.Resources.MultiGrf,
					StreamContents = (grfData, filePath, stream, argument) => {
						MultiGrfReader metaGrf = (MultiGrfReader) argument;

						var data = metaGrf.GetData(filePath);
						stream.Write(data, 0, data.Length);
						vfop.ItemsProcessed++;
					}
				}).ToList();

				vfop.Vfo = virtualFileDataObject;
				vfop.ItemsToProcess = descriptors.Count;
				virtualFileDataObject.Source = DragAndDropSource.ResourceExtractor;
				virtualFileDataObject.SetData(descriptors);

				try {
					VirtualFileDataObject.DoDragDrop(_tree, virtualFileDataObject, DragDropEffects.Copy);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			_itemsResources2.SaveResourceMethod = v => GrfEditorConfiguration.Resources.SaveResources(v);
			_itemsResources2.LoadResourceMethod = () => GrfEditorConfiguration.Resources.LoadResources();
			GrfEditorConfiguration.Resources.Modified += delegate {
				_itemsResources2.LoadResourcesInfo();
				_reloadIndex = true;
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _updateMapFiles(_fileName, null), this, 200, null, false, true));
			};
			_itemsResources2.LoadResourcesInfo();
			_itemsResources2.CanDeleteMainGrf = false;
			_progressBarComponent.SetSpecialState(TkProgressBar.ProgressStatus.Finished);
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		private void _update(float val) {
			Progress = val / GrfEditorConfiguration.Resources.LoadResources().Count + (_subIndex / GrfEditorConfiguration.Resources.LoadResources().Count * 100f);
		}

		private void _updateMapFiles(string fileName, Func<bool> cancelMethod) {
			try {
				ThreadGenericGrf.IgnoreUnreadableFiles = true;

				lock (_lock) {
					if (cancelMethod != null && cancelMethod()) return;

					Progress = -1;

					_tree.Dispatch(p => p.Items.Clear());
					_quickPreview.ClearPreview();

					if (_reloadIndex) {
						_indexer.Clear();

						var paths = GrfEditorConfiguration.Resources.LoadResources();

						for (int index = 0; index < paths.Count; index++) {
							var path = paths[index];

							try {
								_subIndex = index;
								_indexer.Add(path, GrfEditorConfiguration.Resources.MultiGrf, _update);
							}
							catch {
								// May come from closing the window...!
								return;
							}
						}

						_reloadIndex = false;
					}

					if (cancelMethod != null && cancelMethod()) return;

					_addNode(cancelMethod, fileName, null, 0, true);

					if (cancelMethod != null && cancelMethod()) return;

					_tree.Dispatcher.Invoke(new Action(delegate {
						foreach (MapExtractorTreeViewItem node in _tree.Items) {
							if (node.IsChecked == true) {
								node.IsExpanded = true;
							}
						}
					}));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				ThreadGenericGrf.IgnoreUnreadableFiles = false;
				Progress = 100;
			}
		}

		private void _addNode(Func<bool> cancelMethod, string relativePath, MapExtractorTreeViewItem parent, int level, bool isChecked = true) {
			try {
				if (level == 4)
					return;

				if (cancelMethod != null && cancelMethod()) return;

				MapExtractorTreeViewItem mainNode = (MapExtractorTreeViewItem) _tree.Dispatcher.Invoke(new Func<MapExtractorTreeViewItem>(() => new MapExtractorTreeViewItem(_tree)));
				string header = Path.GetFileName(relativePath);

				mainNode.Dispatch(p => p.Checked += new RoutedEventHandler(_checkBox_Checked));
				mainNode.Dispatch(p => p.Unchecked += new RoutedEventHandler(_checkBox_Unchecked));
				mainNode.Dispatch(p => p.HeaderText = header);

				if (GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath) == null) {
					mainNode.Dispatch(p => p.ResourcePath = null);
				}
				else {
					mainNode.Dispatch(p => p.ResourcePath = GrfEditorConfiguration.Resources.MultiGrf.FindTkPath(relativePath));
				}

				if (parent != null)
					parent.Dispatch(p => p.Items.Add(mainNode));
				else
					_tree.Dispatch(p => p.Items.Add(mainNode));

				if (mainNode.ResourcePath != null) {
					mainNode.Dispatch(p => p.IsChecked = isChecked);

					if (cancelMethod != null && cancelMethod()) return;

					List<string> parents = _indexer.FindUsage(relativePath);

					foreach (string parentResource in parents.Distinct()) {
						_addNode(cancelMethod, parentResource, mainNode, level + 1, isChecked);
					}
				}
				else {
					_disableNode(mainNode);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _disableNode(MapExtractorTreeViewItem gndTextureNode) {
			gndTextureNode.Dispatcher.Invoke(new Action(delegate {
				gndTextureNode.IsEnabled = false;
				gndTextureNode.CheckBoxHeaderIsEnabled = false;
				gndTextureNode.ResourcePath = null;
			}));
		}

		private void _checkBox_Checked(object sender, RoutedEventArgs e) {
			try {
				MapExtractorTreeViewItem item = sender as MapExtractorTreeViewItem;

				if (item != null) {
					foreach (MapExtractorTreeViewItem tvi in item.Items) {
						if (tvi.IsEnabled)
							tvi.IsChecked = true;
					}

					_checkParents(item, true);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _checkBox_Unchecked(object sender, RoutedEventArgs e) {
			try {
				MapExtractorTreeViewItem item = sender as MapExtractorTreeViewItem;

				if (item != null) {
					foreach (MapExtractorTreeViewItem tvi in item.Items) {
						if (tvi.IsEnabled)
							tvi.IsChecked = false;
					}

					_checkParents(item, false);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _checkParents(MapExtractorTreeViewItem item, bool value) {
			try {
				if (item.Parent != null) {
					MapExtractorTreeViewItem parent = item.Parent as MapExtractorTreeViewItem;

					if (parent != null) {
						bool allChildrenEqualValue = true;

						foreach (MapExtractorTreeViewItem child in parent.Items) {
							if (child.IsEnabled && child.IsChecked != value) {
								allChildrenEqualValue = false;
								break;
							}
						}

						if (allChildrenEqualValue) {
							parent.IsChecked = value;
							_checkParents(parent, value);
						}
						else if (parent.IsChecked != value) {
							parent.IsChecked = null;

							_checkParents(parent, value);
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonRebuild_Click(object sender, RoutedEventArgs e) {
			_indexer.DeleteIndexes();
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _updateMapFiles(_fileName, null), this, 200, null, false, true));
		}

		private void _tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			try {
				MapExtractorTreeViewItem item = e.NewValue as MapExtractorTreeViewItem;

				if (item != null && item.ResourcePath != null) {
					_quickPreview.Update(item.ResourcePath.RelativePath);
				}
				else {
					_quickPreview.ClearPreview();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsSelectInGrf_Click(object sender, RoutedEventArgs e) {
			try {
				TkPath path = ((MapExtractorTreeViewItem) _tree.SelectedItem).ResourcePath;

				if (path == null) {
					ErrorHandler.HandleException("This file isn't present in the currently opened GRF.", ErrorLevel.Low);
					return;
				}

				if (_grf.FileName == path.FilePath)
					PreviewService.Select(null, null, path.RelativePath);
				else
					ErrorHandler.HandleException("This file isn't present in the currently opened GRF.", ErrorLevel.Low);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsSelectRootFiles_Click(object sender, RoutedEventArgs e) {
			if (_tree.SelectedItem != null) {
				MapExtractorTreeViewItem tvi = (MapExtractorTreeViewItem) _tree.SelectedItem;

				if (tvi.ResourcePath != null) {
					tvi.Checked -= _checkBox_Checked;
					tvi.IsChecked = true;
					tvi.Checked += _checkBox_Checked;
				}

				foreach (MapExtractorTreeViewItem child in tvi.Items) {
					if (child.ResourcePath == null)
						continue;

					child.Checked -= _checkBox_Checked;

					bool allChecked = true;

					foreach (MapExtractorTreeViewItem subChild in child.Items) {
						if (subChild.ResourcePath == null)
							continue;

						if (subChild.IsChecked != true) {
							allChecked = false;
						}
					}

					if (!allChecked) {
						child.IsChecked = null;

						tvi.Checked -= _checkBox_Checked;
						tvi.IsChecked = null;
						tvi.Checked += _checkBox_Checked;
					}
					else {
						child.IsChecked = true;
					}

					child.Checked += _checkBox_Checked;
				}

				if (tvi.IsChecked == true) {
					_checkParents(tvi, true);
				}
			}
		}

		public void Update(object selectedItem) {
			if (selectedItem is FileEntry) {
				_fileName = ((FileEntry) selectedItem).RelativePath;
				_labelHeader.Dispatch(p => p.Content = "Searching usages of " + Path.GetFileName(_fileName));
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _updateMapFiles(_fileName, null), this, 200, null, false, true));
			}
		}

		private void _tree_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			TreeViewItem item = WpfUtilities.GetTreeViewItemClicked((FrameworkElement)e.OriginalSource, _tree);

			if (item != null) {
				item.IsSelected = true;
				_tree.ContextMenu.IsOpen = true;
				e.Handled = false;
			}
			else {
				_tree.ContextMenu.IsOpen = false;
				e.Handled = true;
			}
		}

		private void _buttonExport_Click(object sender, RoutedEventArgs e) {
			Export();
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) {
			ExportAt();
		}

		private void _getSelectedNodes(MapExtractorTreeViewItem currentNode, List<TkPath> nodes) {
			if (currentNode == null) {
				foreach (MapExtractorTreeViewItem mapNode in _tree.Items) {
					_getSelectedNodes(mapNode, nodes);
				}
			}
			else {
				if (currentNode.IsChecked != false && currentNode.ResourcePath != null) {
					if (currentNode.ResourcePath.RelativePath != null)
						nodes.Add(currentNode.ResourcePath);
				}

				foreach (MapExtractorTreeViewItem mapNode in currentNode.Items) {
					_getSelectedNodes(mapNode, nodes);
				}
			}
		}

		private void _export() {
			try {
				Progress = -1;

				List<TkPath> selectedNodes = new List<TkPath>();
				_tree.Dispatch(() => _getSelectedNodes(null, selectedNodes));
				List<string> pathsToCreate = selectedNodes.Select(p => GrfPath.Combine(_destinationPath, Path.GetDirectoryName(p.RelativePath))).Distinct().ToList();

				foreach (string pathToCreate in pathsToCreate) {
					if (!Directory.Exists(pathToCreate))
						Directory.CreateDirectory(pathToCreate);
				}

				for (int index = 0; index < selectedNodes.Count; index++) {
					string relativePath = selectedNodes[index].RelativePath;
					string outputPath = Path.Combine(_destinationPath, relativePath);

					File.WriteAllBytes(outputPath, GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath));

					Progress = (float) (index + 1) / selectedNodes.Count * 100f;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100f;
			}
		}

		public void Export() {
			_destinationPath = GrfEditorConfiguration.OverrideExtractionPath ? GrfEditorConfiguration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(_grf.FileName).FullName);
			_asyncOperation.SetAndRunOperation(new GrfThread(_export, this, 200), _openFolderCallback);
		}

		public void ExportAt() {
			string path = PathRequest.FolderExtract();

			if (path != null) {
				_destinationPath = path;
				_asyncOperation.SetAndRunOperation(new GrfThread(_export, this, 200), _openFolderCallback);
			}
		}

		private void _openFolderCallback(object state) {
			try {
				OpeningService.FileOrFolder(Path.Combine(_destinationPath, "data"));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_indexer != null)
					_indexer.Clear();

				if (_tree != null)
					_tree.Dispose();

				if (_quickPreview != null)
					_quickPreview.Dispose();
			}
		}
	}
}