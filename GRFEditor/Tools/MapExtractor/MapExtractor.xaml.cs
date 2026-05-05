using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;
using OpeningService = Utilities.Services.OpeningService;

namespace GRFEditor.Tools.MapExtractor {
	/// <summary>
	/// Interaction logic for MapExtractor.xaml
	/// </summary>
	public partial class MapExtractor : UserControl, IProgress {
		private readonly AsyncOperation _asyncOperation;
		private readonly object _lock = new object();
		private string _destinationPath;
		private bool _userDestination;
		private string _fileName;
		private GrfHolder _grf;
		private string _grfPath;
		private MapResourceResolver _mapResourceResolver = new MapResourceResolver();
		private SharedTreeViewEvent _tvSharedEvent = new SharedTreeViewEvent();

		public MapExtractor(GrfHolder grf, string fileName) {
			_grfPath = Path.GetDirectoryName(fileName);
			_grf = grf;
			_fileName = fileName;

			InitializeComponent();

			_initializeTreeView();
			_initializeResources();

			_asyncOperation = new AsyncOperation(_progressBarComponent);
			_progressBarComponent.SetSpecialState(TkProgressBar.ProgressStatus.Finished);
			_quickPreview.Set(_asyncOperation);
		}

		private void _initializeResources() {
			_itemsResources.SaveResourceMethod = v => Configuration.Resources.SaveResources(v);
			_itemsResources.LoadResourceMethod = () => Configuration.Resources.LoadResources();
			Configuration.Resources.Modified += delegate {
				_itemsResources.LoadResourcesInfo();
				
				if (!_asyncOperation.IsRunning)
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _updateMapFiles(_fileName, null), this, 200, null, false, true));
			};
			_itemsResources.LoadResourcesInfo();
			_itemsResources.CanDeleteMainGrf = false;
		}

		private void _initializeTreeView() {
			_treeViewMapExtractor.SelectedItemChanged += _treeViewMapExtractor_SelectedItemChanged;
			_treeViewMapExtractor.CopyMethod = delegate {
				if (_treeViewMapExtractor.SelectedItem != null)
					Clipboard.SetDataObject(((MapExtractorTreeViewItem)_treeViewMapExtractor.SelectedItem).ResourcePath.GetMostRelative());
			};

			_treeViewMapExtractor.DoDragDropCustomMethod = delegate {
				VirtualFileDataObjectProgress vfop = new VirtualFileDataObjectProgress();
				VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject(
					_ => _asyncOperation.SetAndRunOperation(new GrfThread(vfop.Update, vfop, 500, null)),
					_ => vfop.Finished = true
				);

				IEnumerable<MapExtractorTreeViewItem> allNodes = _treeViewMapExtractor.SelectedItems.Items.Cast<MapExtractorTreeViewItem>().Where(p => p.ResourcePath != null);

				List<VirtualFileDataObject.FileDescriptor> descriptors = allNodes.Select(node => new VirtualFileDataObject.FileDescriptor {
					Name = Path.GetFileName(node.ResourcePath.RelativePath),
					FilePath = node.ResourcePath.RelativePath,
					Argument = GrfEditorConfiguration.Resources.MultiGrf,
					StreamContents = (grfData, filePath, stream, argument) => {
						MultiGrfReader metaGrfArg = (MultiGrfReader)argument;

						var data = metaGrfArg.GetData(filePath);
						stream.Write(data, 0, data.Length);
						vfop.ItemsProcessed++;
					}
				}).ToList();

				vfop.Vfo = virtualFileDataObject;
				vfop.ItemsToProcess = descriptors.Count;
				virtualFileDataObject.Source = DragAndDropSource.ResourceExtractor;
				virtualFileDataObject.SetData(descriptors);

				try {
					VirtualFileDataObject.DoDragDrop(_treeViewMapExtractor, virtualFileDataObject, DragDropEffects.Copy);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};
		}

		public AsyncOperation AsyncOperation {
			get { return _asyncOperation; }
		}

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		private void _treeViewMapExtractor_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			try {
				MapExtractorTreeViewItem item = e.NewValue as MapExtractorTreeViewItem;

				if (item != null && item.ResourcePath != null) {
					_quickPreview.Update(item.ResourcePath.GetMostRelative());
				}
				else {
					_quickPreview.ClearPreview();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Reload(GrfHolder grf, string fileName, Func<bool> cancelMethod) {
			if (_asyncOperation.IsRunning)
				return;

			_grfPath = Path.GetDirectoryName(fileName);
			_grf = grf;
			_fileName = fileName;

			Task.Run(() => _updateMapFiles(fileName, cancelMethod));
		}

		private void _disableNode(MapExtractorTreeViewItem node, string tooltip = null) {
			node.Dispatch(delegate {
				node.IsChecked = false;
				node.CheckBoxHeaderIsEnabled = false;
				node.ResourcePath = null;
				node.RelativeGrfPath = null;

				if (tooltip != null)
					node.ToolTip = tooltip;
			});
		}

		private void _openFolderCallback(object state) {
			try {
				OpeningService.FileOrFolder(_destinationPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _updateMapFiles(string fileName, Func<bool> cancelToken) {
			try {
				if (cancelToken == null)
					cancelToken = () => false;

				lock (_lock) {
					if (cancelToken()) return;

					string mapFile = Path.GetFileNameWithoutExtension(fileName);
					string expandExt = fileName.GetExtension();
					Progress = -1;

					_treeViewMapExtractor.Dispatch(p => p.Items.Clear());
					_quickPreview.ClearPreview();
					bool isMapFile = fileName.IsExtension(".rsw", ".gat", ".gnd");

					if (fileName.IsExtension(".rsm")) {
						if (cancelToken()) return;
						_addNode(cancelToken, new MapResourcePath(_grfPath, mapFile + ".rsm"), _treeViewMapExtractor.Items);
					}
					else if (fileName.IsExtension(".rsm2")) {
						if (cancelToken()) return;
						_addNode(cancelToken, new MapResourcePath(_grfPath, mapFile + ".rsm2"), _treeViewMapExtractor.Items);
					}
					else if (fileName.IsExtension(".str")) {
						if (cancelToken()) return;
						_addNode(cancelToken, new MapResourcePath(_grfPath, mapFile + ".str"), _treeViewMapExtractor.Items);
					}
					else {
						if (cancelToken()) return;
						_addNode(cancelToken, new MapResourcePath(@"data\", mapFile + ".gnd"), _treeViewMapExtractor.Items, isMapFile);
						if (cancelToken()) return;
						_addNode(cancelToken, new MapResourcePath(@"data\", mapFile + ".rsw"), _treeViewMapExtractor.Items, isMapFile);
						if (cancelToken()) return;
						_addNode(cancelToken, new MapResourcePath(@"data\", mapFile + ".gat"), _treeViewMapExtractor.Items, isMapFile);

						if (GrfEditorConfiguration.Resources.MultiGrf.Exists(@"data\luafiles514\lua files\effecttool\" + mapFile + ".lub")) {
							_addNode(cancelToken, new MapResourcePath(@"data\luafiles514\lua files\effecttool\", mapFile + ".lub"), _treeViewMapExtractor.Items, isMapFile);
						}
					}

					if (cancelToken()) return;
					_treeViewMapExtractor.Dispatch(delegate {
						foreach (MapExtractorTreeViewItem node in _treeViewMapExtractor.Items) {
							if (node.HeaderText.IsExtension(expandExt)) {
								if (node.IsChecked == true) {
									node.IsExpanded = true;
								}
							}
						}
					});
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
			}
		}

		public MapExtractorTreeViewItem _createNode(MapResourcePath resource) {
			MapExtractorTreeViewItem node = new MapExtractorTreeViewItem(_treeViewMapExtractor, _tvSharedEvent);

			node.HeaderText = resource.NodeDisplayPath;
			node.ResourcePath = GrfEditorConfiguration.Resources.MultiGrf.FindTkPath(resource.RelativePath);
			node.RelativeGrfPath = resource.RelativePath;

			return node;
		}

		/// <summary>
		/// Adds the node.
		/// </summary>
		/// <param name="cancelToken">The cancel token.</param>
		/// <param name="nodeDisplayPath">The node display path (ex: "abyss\abyss_j_06.bmp"), what the TreeViewItem header will display.</param>
		/// <param name="parentDirectory">The parent GRF directory (ex: "data\model"), where the textures or assets are relative to by nodeDisplayPath.</param>
		/// <param name="parent">The list parent where to add this new node.</param>
		/// <param name="isChecked">if set to <c>true</c> [is checked].</param>
		private void _addNode(Func<bool> cancelToken, MapResourcePath resource, ItemCollection parent, bool isChecked = true) {
			try {
				if (cancelToken()) return;

				MapExtractorTreeViewItem node = null;

				this.Dispatch(delegate {
					node = _createNode(resource);
					parent.Add(node);
				});

				if (node.ResourcePath == null) {
					_disableNode(node);
					return;
				}

				if (cancelToken()) return;

				try {
					List<MapResourcePath> resources = _mapResourceResolver.GetMapResources(resource);

					if (isChecked) {
						node.Dispatch(delegate {
							if (node.CheckBoxHeaderIsEnabled) {
								node.IsChecked = isChecked;
							}
						});
					}

					foreach (var res in resources) {
						_addNode(cancelToken, res, node.Items, isChecked);
					}
				}
				catch {
					_disableNode(node, "Cannot read the file. It is either encrypted or corrupted.");
				}

				if (isChecked) {
					node.Dispatch(delegate {
						if (node.CheckBoxHeaderIsEnabled) {
							node.IsChecked = isChecked;
						}
					});
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private IEnumerable<Utilities.Extension.Tuple<TkPath, string>> _getSelectedNodes(MapExtractorTreeViewItem node) {
			return _treeViewMapExtractor.Dispatch(delegate {
				List<Utilities.Extension.Tuple<TkPath, string>> paths = new List<Utilities.Extension.Tuple<TkPath, string>>();

				if (node == null) {
					foreach (MapExtractorTreeViewItem mapNode in _treeViewMapExtractor.Items) {
						paths.AddRange(_getSelectedNodes(mapNode));
					}
				}
				else {
					if (node.IsChecked == true && node.CheckBoxHeaderIsEnabled) {
						paths.Add(new Utilities.Extension.Tuple<TkPath, string>(node.ResourcePath, node.RelativeGrfPath));
					}

					foreach (MapExtractorTreeViewItem mapNode in node.Items) {
						paths.AddRange(_getSelectedNodes(mapNode));
					}
				}

				return paths;
			});
		}

		#region Events
		private void _treeViewMapExtractor_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			TreeViewItem item = WpfUtilities.GetTreeViewItemClicked((FrameworkElement) e.OriginalSource, _treeViewMapExtractor);

			if (item != null) {
				item.IsSelected = true;
				_treeViewMapExtractor.ContextMenu.IsOpen = true;
				e.Handled = false;
			}
			else {
				_treeViewMapExtractor.ContextMenu.IsOpen = false;
				e.Handled = true;
			}
		}
		#endregion

		#region Select File
		private void _menuItemsSelectInExplorer_Click(object sender, RoutedEventArgs e) => _focusFile(isExplorer: true);
		private void _menuItemsSelectInGrf_Click(object sender, RoutedEventArgs e) => _focusFile(isExplorer: false);
		
		private void _focusFile(bool isExplorer) {
			try {
				var node = (MapExtractorTreeViewItem)_treeViewMapExtractor.SelectedItem;
				TkPath path = node.ResourcePath;

				if (path == null) {
					ErrorHandler.HandleException("This file isn't present in the currently opened GRF.", ErrorLevel.Low);
					return;
				}

				if (isExplorer) {
					var destinationPath = GrfPath.Combine(Configuration.OverrideExtractionPath ? Configuration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(_grf.FileName).FullName), node.RelativeGrfPath);
					OpeningService.FileOrFolder(path.RelativePath == null ? path.FilePath : destinationPath);
				}
				else {
					if (_grf.FileName == path.FilePath)
						PreviewService.Select(null, null, path.RelativePath);
					else
						ErrorHandler.HandleException("This file isn't present in the currently opened GRF.", ErrorLevel.Low);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		#endregion

		#region Export resources
		public void Export() {
			_userDestination = false;
			_destinationPath = Configuration.OverrideExtractionPath ? Configuration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(_grf.FileName).FullName);
			_asyncOperation.SetAndRunOperation(new GrfThread(_export, this, 200), _openFolderCallback);
		}

		public void ExportAt() {
			string path = PathRequest.FolderExtract();

			if (path != null) {
				_userDestination = true;
				_destinationPath = path;
				_asyncOperation.SetAndRunOperation(new GrfThread(_export, this, 200), _openFolderCallback);
			}
		}

		private void _export() {
			try {
				Progress = -1;

				List<Utilities.Extension.Tuple<TkPath, string>> selectedNodes = _getSelectedNodes(null).GroupBy(p => p.Item1.GetFullPath()).Select(p => p.First()).ToList();
				string commonRoot = "";

				if (_userDestination) {
					// Find common root
					var splitPaths = selectedNodes
						.Select(p => p.Item2.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
						.ToList();

					var commonPrefixParts = splitPaths
						.First()
						.TakeWhile((part, index) => splitPaths.All(p => p.Length > index && string.Equals(p[index], part, StringComparison.OrdinalIgnoreCase)))
						.ToArray();

					commonRoot = string.Join("" + Path.DirectorySeparatorChar, commonPrefixParts);
				}

				List<string> pathsToCreate = selectedNodes.Select(p => Path.Combine(_destinationPath, Path.GetDirectoryName(p.Item2.ReplaceFirst(commonRoot, "").TrimStart('\\')))).Distinct().ToList();

				foreach (string pathToCreate in pathsToCreate) {
					if (!Directory.Exists(pathToCreate))
						Directory.CreateDirectory(pathToCreate);
				}

				for (int index = 0; index < selectedNodes.Count; index++) {
					string relativePath = selectedNodes[index].Item2;

					string outputPath = Path.Combine(_destinationPath, relativePath.ReplaceFirst(commonRoot, "").TrimStart('\\'));

					File.WriteAllBytes(outputPath, GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath));

					Progress = (float)(index + 1) / selectedNodes.Count * 100f;
				}

				// Find topmost directory
				if (pathsToCreate.Count > 0)
					_destinationPath = pathsToCreate[0];
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100f;
			}
		}
		#endregion
	}
}