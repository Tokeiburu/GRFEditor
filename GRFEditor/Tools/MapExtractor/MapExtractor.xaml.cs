using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.StrFormat;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GrfToWpfBridge.Application;
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
		private string _fileName;
		private GrfHolder _grf;
		private string _grfPath;

		public MapExtractor(GrfHolder grf, string fileName) {
			_grfPath = Path.GetDirectoryName(fileName);
			_grf = grf;
			_fileName = fileName;

			InitializeComponent();

			_treeViewMapExtractor.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(_treeViewMapExtractor_SelectedItemChanged);
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
						MultiGrfReader metaGrfArg = (MultiGrfReader) argument;

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

			_asyncOperation = new AsyncOperation(_progressBarComponent);
			_quickPreview.Set(_asyncOperation);

			_itemsResources2.SaveResourceMethod = v => Configuration.Resources.SaveResources(v);
			_itemsResources2.LoadResourceMethod = () => Configuration.Resources.LoadResources();
			Configuration.Resources.Modified += delegate {
				_itemsResources2.LoadResourcesInfo();
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _updateMapFiles(_fileName, null), this, 200, null, false, true));
			};
			_itemsResources2.LoadResourcesInfo();
			_itemsResources2.CanDeleteMainGrf = false;
			_progressBarComponent.SetSpecialState(TkProgressBar.ProgressStatus.Finished);
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

		public void Export() {
			_destinationPath = Configuration.OverrideExtractionPath ? Configuration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(_grf.FileName).FullName);
			_asyncOperation.SetAndRunOperation(new GrfThread(_export, this, 200), _openFolderCallback);
		}

		public void ExportAt() {
			string path = PathRequest.FolderExtract();

			if (path != null) {
				_destinationPath = path;
				_asyncOperation.SetAndRunOperation(new GrfThread(_export, this, 200), _openFolderCallback);
			}
		}

		public void Reload(GrfHolder grf, string fileName, Func<bool> cancelMethod) {
			if (_asyncOperation.IsRunning)
				return;

			_grfPath = Path.GetDirectoryName(fileName);
			_grf = grf;
			_fileName = fileName;

			new Thread(() => _updateMapFiles(fileName, cancelMethod)) { Name = "GrfEditor - MapExtractor map update thread" }.Start();
		}

		private void _disableNode(MapExtractorTreeViewItem gndTextureNode) {
			gndTextureNode.Dispatcher.Invoke(new Action(delegate {
				gndTextureNode.CheckBoxHeaderIsEnabled = false;
				gndTextureNode.ResourcePath = null;
			}));
		}

		private void _checkParents(MapExtractorTreeViewItem item, bool value) {
			try {
				if (item.Parent != null) {
					MapExtractorTreeViewItem parent = item.Parent as MapExtractorTreeViewItem;

					if (parent != null) {
						bool allChildrenEqualValue = true;

						foreach (MapExtractorTreeViewItem child in parent.Items) {
							if (child.CheckBoxHeaderIsEnabled && child.IsChecked != value) {
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

		private void _export() {
			try {
				Progress = -1;

				List<Utilities.Extension.Tuple<TkPath, string>> selectedNodes = _getSelectedNodes(null).GroupBy(p => p.Item1.GetFullPath()).Select(p => p.First()).ToList();
				List<string> pathsToCreate = selectedNodes.Select(p => Path.Combine(_destinationPath, Path.GetDirectoryName(p.Item2))).Distinct().ToList();

				foreach (string pathToCreate in pathsToCreate) {
					if (!Directory.Exists(pathToCreate))
						Directory.CreateDirectory(pathToCreate);
				}

				for (int index = 0; index < selectedNodes.Count; index++) {
					string relativePath = selectedNodes[index].Item2;

					string outputPath = Path.Combine(_destinationPath, relativePath);

					File.WriteAllBytes(outputPath, GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath));

					Progress = (float) (index + 1) / selectedNodes.Count * 100f;
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

		private void _openFolderCallback(object state) {
			try {
				OpeningService.FileOrFolder(_destinationPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _updateMapFiles(string fileName, Func<bool> cancelMethod) {
			try {
				lock (_lock) {
					if (cancelMethod != null && cancelMethod()) return;

					string mapFile = Path.GetFileNameWithoutExtension(fileName);
					Progress = -1;

					_treeViewMapExtractor.Dispatch(p => p.Items.Clear());
					_quickPreview.ClearPreview();

					if (fileName.IsExtension(".rsm")) {
						if (cancelMethod != null && cancelMethod()) return;
						_addNode(cancelMethod, mapFile + ".rsm", _grfPath, null);
					}
					else if (fileName.IsExtension(".rsm2")) {
						if (cancelMethod != null && cancelMethod()) return;
						_addNode(cancelMethod, mapFile + ".rsm2", _grfPath, null);
					}
					else if (fileName.IsExtension(".str")) {
						if (cancelMethod != null && cancelMethod()) return;
						_addNode(cancelMethod, mapFile + ".str", _grfPath, null);
					}
					else {
						if (cancelMethod != null && cancelMethod()) return;
						_addNode(cancelMethod, mapFile + ".gnd", @"data\", null, fileName.IsExtension(".gnd"));

						if (cancelMethod != null && cancelMethod()) return;
						_treeViewMapExtractor.Dispatcher.Invoke(new Action(delegate {
							foreach (MapExtractorTreeViewItem node in _treeViewMapExtractor.Items) {
								if (node.IsChecked == true) {
									node.IsExpanded = true;
								}
							}
						}));

						_addNode(cancelMethod, mapFile + ".rsw", @"data\", null, fileName.IsExtension(".rsw"));
					}

					if (cancelMethod != null && cancelMethod()) return;
					_treeViewMapExtractor.Dispatcher.Invoke(new Action(delegate {
						foreach (MapExtractorTreeViewItem node in _treeViewMapExtractor.Items) {
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
				Progress = 100;
			}
		}

		private void _addNode(Func<bool> cancelMethod, string subRelativeFile, string relativeResourceLocation, MapExtractorTreeViewItem parent, bool isChecked = true) {
			try {
				if (cancelMethod != null && cancelMethod()) return;

				MapExtractorTreeViewItem mainNode = (MapExtractorTreeViewItem)_treeViewMapExtractor.Dispatcher.Invoke(new Func<MapExtractorTreeViewItem>(() => new MapExtractorTreeViewItem(_treeViewMapExtractor)));
				string relativePath = Path.Combine(relativeResourceLocation, subRelativeFile);
				List<string> resources = new List<string>();

				mainNode.Dispatch(delegate {
					mainNode.Checked += new RoutedEventHandler(_checkBox_Checked);
					mainNode.Unchecked += new RoutedEventHandler(_checkBox_Unchecked);
					mainNode.HeaderText = subRelativeFile;

					var path = GrfEditorConfiguration.Resources.MultiGrf.FindTkPath(relativePath);

					mainNode.ResourcePath = path;

					if (parent != null)
						parent.Items.Add(mainNode);
					else
						_treeViewMapExtractor.Items.Add(mainNode);
				});

				if (mainNode.ResourcePath != null) {
					mainNode.Dispatch(p => p.IsChecked = isChecked);
					string extension = subRelativeFile.GetExtension();

					if (cancelMethod != null && cancelMethod()) return;

					switch (extension) {
						case ".rsm":
						case ".rsm2":
							var byteData = GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath);

							var binaryReader = ((MultiType)byteData).GetBinaryReader();
							RsmHeader rsmHeader = new RsmHeader(binaryReader);

							if (rsmHeader.Version < 2.0) {
								binaryReader.Forward(8);

								if (rsmHeader.Version >= 1.4) {
									binaryReader.Forward(1);
								}

								binaryReader.Forward(16);
								var count = binaryReader.Int32();

								for (int i = 0; i < count; i++) {
									resources.Add(binaryReader.String(40, '\0'));
								}
							}
							else {
								binaryReader.Position = 0;
								Rsm rsm2 = new Rsm(binaryReader);

								resources.AddRange(rsm2.Textures);

								foreach (var mesh in rsm2.Meshes) {
									resources.AddRange(mesh.Textures);
								}

								resources = resources.Distinct().ToList();
							}

							foreach (string texture in resources) {
								_addNode(cancelMethod, texture, @"data\texture\", mainNode, isChecked);
							}
							break;
						case ".gnd":
							var dataEntry = ((MultiType)GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath)).GetBinaryReader();
							GndHeader gndHeader = new GndHeader(dataEntry);
							
							for (int i = 0; i < gndHeader.TextureCount; i++) {
								resources.Add(dataEntry.String(gndHeader.TexturePathSize, '\0'));
							}

							foreach (string texture in resources.Distinct()) {
								_addNode(cancelMethod, texture, @"data\texture\", mainNode, isChecked);
							}
							break;
						case ".rsw":
							Rsw rsw = new Rsw(GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath));

							foreach (string model in rsw.ModelResources.Distinct()) {
								_addNode(cancelMethod, model, @"data\model\", mainNode, isChecked);
							}
							break;
						case ".str":
							Str str = new Str(GrfEditorConfiguration.Resources.MultiGrf.GetData(relativePath));

							resources = str.Textures;

							foreach (string resource in resources) {
								_addNode(cancelMethod, resource, relativeResourceLocation, mainNode, isChecked);
							}
							break;
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

		private IEnumerable<Utilities.Extension.Tuple<TkPath, string>> _getSelectedNodes(MapExtractorTreeViewItem node) {
			return (List<Utilities.Extension.Tuple<TkPath, string>>)_treeViewMapExtractor.Dispatcher.Invoke(new Func<List<Utilities.Extension.Tuple<TkPath, string>>>(delegate {
				List<Utilities.Extension.Tuple<TkPath, string>> paths = new List<Utilities.Extension.Tuple<TkPath, string>>();

				if (node == null) {
					foreach (MapExtractorTreeViewItem mapNode in _treeViewMapExtractor.Items) {
						paths.AddRange(_getSelectedNodes(mapNode));
					}
				}
				else {
					if (node.IsChecked == true) {
						paths.Add(new Utilities.Extension.Tuple<TkPath, string>(node.ResourcePath, node.ResourcePath.RelativePath));
					}

					foreach (MapExtractorTreeViewItem mapNode in node.Items) {
						paths.AddRange(_getSelectedNodes(mapNode));
					}
				}

				return paths;
			}));
		}

		private void _menuItemsSelectInGrf_Click(object sender, RoutedEventArgs e) {
			try {
				TkPath path = ((MapExtractorTreeViewItem) _treeViewMapExtractor.SelectedItem).ResourcePath;

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

		#region Events

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

		private void _menuItemsSelectRootFiles_Click(object sender, RoutedEventArgs e) {
			if (_treeViewMapExtractor.SelectedItem != null) {
				MapExtractorTreeViewItem tvi = (MapExtractorTreeViewItem) _treeViewMapExtractor.SelectedItem;

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

		private void _menuItemsSelectInExplorer_Click(object sender, RoutedEventArgs e) {
			try {
				TkPath path = ((MapExtractorTreeViewItem)_treeViewMapExtractor.SelectedItem).ResourcePath;

				if (path == null) {
					ErrorHandler.HandleException("This file isn't present in the currently opened GRF.", ErrorLevel.Low);
					return;
				}

				var destinationPath = GrfPath.Combine(Configuration.OverrideExtractionPath ? Configuration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(_grf.FileName).FullName), path.RelativePath);

				OpeningService.FileOrFolder(destinationPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}