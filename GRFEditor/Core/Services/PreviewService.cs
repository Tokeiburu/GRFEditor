using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.Core.Exceptions;
using GRF.Image;
using GRF.Image.Decoders;
using GRFEditor.WPF;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities.Extension;
using Utilities.Services;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;

namespace GRFEditor.Core.Services {
	public partial class PreviewService {
		public static ListView ListView;
		public static TreeView TreeView;
		private readonly AsyncOperation _asyncOperation;
		private readonly EditorMainWindow _editor;
		private readonly ListView _items;

		private readonly Queue<PreviewItem> _previewItems = new Queue<PreviewItem>();
		private readonly object _previewLock = new object();
		private readonly object _previewLockQuick = new object();
		private readonly string[] _rawStructureTextEditorExtensions = new string[] { ".fna", ".imf", ".rsm", ".rsm2", ".lub", ".str", ".bson" };
		private readonly TabControl _tabControlPreview;
		private readonly string[] _textEditorExtensions = new string[] { ".txt", ".log", ".xml", ".lua", ".ezv", ".ini", ".inf", ".conf", ".js", ".c", ".cpp", ".integrity", ".json" };
		private readonly TreeView _treeView;
		private PreviewDisplayConfiguration _currentConf = new PreviewDisplayConfiguration();
		private string _currentPath;
		private GrfHolder _grfData;
		private bool _hasBeenLoaded;
		private PreviewItem _previewItem;
		private string _selectedItem = "";

		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewService" /> class.
		/// The preview service shouldn't have critical errors, it doesn't modify the GRF.
		/// </summary>
		/// <param name="asyncOperation"> </param>
		/// <param name="tabControlPreview"> </param>
		/// <param name="treeView"> </param>
		/// <param name="items"> </param>
		/// <param name="editor"> </param>
		public PreviewService(AsyncOperation asyncOperation, TabControl tabControlPreview,
		                      TreeView treeView, ListView items, EditorMainWindow editor) {
			_asyncOperation = asyncOperation;
			_tabControlPreview = tabControlPreview;
			_treeView = treeView;
			_items = items;
			_editor = editor;

			SettingsDialog.GetBackgroundScrollViewers = delegate {
				if (!_hasBeenLoaded)
					_loadTabItems();

				var scrolls = new List<Action<Brush>>();

				if (_tabItemDbPreview.Content != null) scrolls.Add(((PreviewThumbDb)_tabItemDbPreview.Content).BackgroundBrushFunction);
				if (_tabItemMapGatPreview.Content != null) scrolls.Add(((PreviewMapGat)_tabItemMapGatPreview.Content).BackgroundBrushFunction);
				if (_tabItemImagePreview.Content != null) scrolls.Add(((PreviewImage)_tabItemImagePreview.Content).BackgroundBrushFunction);
				if (_tabItemActPreview.Content != null) scrolls.Add(((PreviewAct)_tabItemActPreview.Content).BackgroundBrushFunction);
				if (_tabItemMapExtractorPreview.Content != null) scrolls.Add(((PreviewMapExtractor) _tabItemMapExtractorPreview.Content).BackgroundBrushFunction);
				
				return scrolls.ToArray();
			};

			UpdatePreviewPanel(new PreviewDisplayConfiguration { ShowGrfProperties = false });
		}

		/// <summary>
		/// Shows the preview.
		/// </summary>
		/// <param name="grfData">The GRF data.</param>
		/// <param name="currentPath">The current path. In this case, it's better not to recalculate the current path manually.</param>
		/// <param name="selectedItem">The selected item.</param>
		public void ShowPreview(GrfHolder grfData, string currentPath, string selectedItem) {
			if (!_hasBeenLoaded)
				_loadTabItems();

			//GC.Collect();

			try {
				if (currentPath == null && selectedItem == null)
					return;

				if (currentPath != null && selectedItem == null) {
					lock (_previewLockQuick) {
						_currentPath = currentPath;
						_grfData = grfData;
						_previewItems.Enqueue(new PreviewItem {
							FileName = _currentPath,
							Extension = null
						});
					}
				}
				else {
					if (currentPath == null)
						return;

					lock (_previewLockQuick) {
						if (Path.GetExtension(_selectedItem) == null) return;

						_selectedItem = selectedItem;
						_currentPath = currentPath;
						_grfData = grfData;
						_previewItems.Enqueue(new PreviewItem {
							FileName = Path.Combine(_currentPath, _selectedItem),
							Extension = _selectedItem.GetExtension()
						});
					}
				}

				new Thread(_showPreview) { Name = "GrfEditor - Preview thread " + _currentPath + "\\" + _selectedItem }.Start();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		private void _showPreview() {
			try {
				// Prevents multiple read access (and ultimately slowing down the app)
				lock (_previewLock) {
					lock (_previewLockQuick) {
						if (_previewItems.Count == 0) return;
						_previewItem = _previewItems.Dequeue();
						if (_previewItems.Count != 0) return;
					}

					string extension = _previewItem.Extension;

					if (extension == null) {
						_readAsFolder(_previewItem.FileName);
						_readAsFolderStructure(_previewItem.FileName);
						_readAsContainer(_previewItem.FileName);
						_readAsPreviewSprites(_previewItem.FileName);

						UpdatePreviewPanel(new PreviewDisplayConfiguration {
							ShowContainerPreview = true,
							ShowFolderPreview = true,
							ShowSpritesPreview = true,
							ShowFolderStructurePreview = true,
							ShowGrfProperties = false
						});
					}
					else {
						FileEntry node = _grfData.FileTable[_previewItem.FileName];

						if (!node.EncryptionSafe()) {
							throw new EncryptionException(GrfExceptions.__NoKeyFileSet.Message, EncryptionExceptionReason.NoKeySet);
						}

						if (_previewItems.Count != 0) return;

						_readAsProperties(node);

						PreviewDisplayConfiguration previewDisplayConfiguration = new PreviewDisplayConfiguration();

						if (_textEditorExtensions.Contains(extension)) {
							previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowTextEditor = true };
						}

						if (_rawStructureTextEditorExtensions.Contains(extension)) {
							previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowRawStructureTextEditor = true };
						}

						if (node.Removed) {
							previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowRawStructureTextEditor = true };
							_readAsRawStructure(node);
						}
						else {
							switch (extension) {
								case ".ini":
								case ".inf":
								case ".conf":
								case ".log":
								case ".txt":
								case ".json":
								case ".xml":
								case ".lua":
								case ".c":
								case ".cpp":
								case ".integrity":
								case ".js":
								case ".ezv":
									_readAsTxt(node);
									break;
								default:
									if (node.IsEmpty()) {
										previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowRawStructureTextEditor = true };
										_readAsRawStructure(node);
									}
									else
										switch (extension) {
											case ".fna":
											case ".imf":
											case ".bson":
												_readAsRawStructure(node);
												break;
											case ".str":
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowRawStructureTextEditor = true, ShowMapExtractor = true, ShowStr = true };
												_readAsMapFile(node);
												_readAsRawStructure(node);
												_readAsStr(node);
												break;
											case ".lub":
												_readAsRawStructure(node);
												_readAsDecompilationSettings(node);
												previewDisplayConfiguration.ShowLubDecompiler = true;
												break;
											case ".rsm":
											case ".rsm2":
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowRawStructureTextEditor = true, ShowMapExtractor = true, ShowRsm = true };
												_readAsMapFile(node);
												_readAsRawStructure(node);
												_readAsRsm(node);
												break;
											case ".gnd":
											case ".rsw":
											case ".gat":
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowRawStructureTextEditor = true, ShowMapExtractor = true, ShowRsm = true };
												_readAsRawStructure(node);
												_readAsMapFile(node);
												_readAsRsm(node);

												try {
													string possibleGatName = Path.Combine(Path.GetDirectoryName(_previewItem.FileName), Path.GetFileNameWithoutExtension(_previewItem.FileName)) + ".gat";

													if (_grfData.FileTable.ContainsFile(possibleGatName)) {
														FileEntry tempNode = _grfData.FileTable[possibleGatName];
														_readAsMapGat(tempNode);
														previewDisplayConfiguration.ShowGat = true;
													}
												}
												catch {
												}
												break;
											case ".tga":
											case ".jpg":
											case ".png":
											case ".ebm":
											case ".pal":
											case ".spr":
											case ".bmp":
												if (extension == ".spr") {
													previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowEditSprite = true, ShowImagePreview = true, ShowRawStructureTextEditor = true };
													_readAsEditSprite(node);
													_readAsRawStructure(node);

													try {
														string possibleSpriteName = Path.Combine(Path.GetDirectoryName(_previewItem.FileName), Path.GetFileNameWithoutExtension(_previewItem.FileName)) + ".act";

														if (_grfData.FileTable.ContainsFile(possibleSpriteName)) {
															FileEntry tempNode = _grfData.FileTable[possibleSpriteName];
															_readAsAnimation(tempNode);
															previewDisplayConfiguration.ShowAnimation = true;
														}
													}
													catch {
													}
												}
												else {
													previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowImagePreview = true };
												}

												_readAsImage(node);
												break;
											case ".db":
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowDb = true };
												_readAsDb(node);
												break;
											case ".wav":
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowSoundPreview = true };
												_readAsSound(node);
												break;
											case ".act":
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowAnimation = true, ShowRawStructureTextEditor = true };
												_readAsAnimation(node);
												_readAsRawStructure(node);

												try {
													string possibleSpriteName = Path.Combine(Path.GetDirectoryName(_previewItem.FileName), Path.GetFileNameWithoutExtension(_previewItem.FileName)) + ".spr";

													if (_grfData.FileTable.ContainsFile(possibleSpriteName)) {
														FileEntry tempNode = _grfData.FileTable[possibleSpriteName];
														_readAsEditSprite(tempNode);
														_readAsImage(tempNode);
														previewDisplayConfiguration.ShowEditSprite = true;
														previewDisplayConfiguration.ShowImagePreview = true;
													}
												}
												catch {
												}
												break;
											default:
												previewDisplayConfiguration = new PreviewDisplayConfiguration { ShowHexEditor = true };
												_readAsResources(node);
												break;
										}
									break;
							}
						}

						UpdatePreviewPanel(previewDisplayConfiguration);
					}
				}
			}
			catch (EncryptionException err) {
				if (err.Reason == EncryptionExceptionReason.NoKeySet) {
					try {
						Window parent = WpfUtilities.FindDirectParentControl<Window>(TreeView);

						parent.Dispatch(delegate {
							try {
								EncryptorInputKeyDialog dialog = new EncryptorInputKeyDialog("Enter the encryption key to automatically decrypt the content or click cancel to ignore.");
								dialog.Owner = parent;
								dialog.ShowDialog();

								if (dialog.Result == MessageBoxResult.OK) {
									Configuration.EncryptorPassword = dialog.Key;
									_grfData.Header.SetKey(dialog.Key, _grfData);
								}
							}
							catch (Exception err2) {
								ErrorHandler.HandleException(err2);
							}
						});
					}
					catch (Exception err2) {
						ErrorHandler.HandleException(err2);
					}
				}
				else {
					throw;
				}
			}
			catch (Exception err) {
				try {
					FileEntry entry = _grfData.FileTable[_previewItem.FileName];

					if (entry.Flags.HasFlags(EntryType.GrfEditorCrypted)) {
						if (EncryptionService.RequestDecryptionKey(_grfData)) {
							lock (_previewLockQuick) {
								_previewItems.Enqueue(new PreviewItem { Extension = _previewItem.Extension, FileName = _previewItem.FileName });
							}
							_showPreview();
						}
						else {
							UpdatePreviewPanel(new PreviewDisplayConfiguration { ShowHexEditor = true });
							_readAsResources(entry);
						}
					}
					else {
						UpdatePreviewPanel(new PreviewDisplayConfiguration { ShowHexEditor = true });
						ErrorHandler.HandleException("Failed to read the data. " + (entry.Flags.HasFlags(EntryType.GrfEditorCrypted) ? "\n\nNow showing the raw data (from the GRF stream)." : "\n\nNow showing the decompressed data."), err);
						_readAsResources(entry);
					}
				}
				catch (Exception err2) {
					ErrorHandler.HandleException(err2, ErrorLevel.Critical);
				}
			}
		}

		public void UpdatePreviewPanel(PreviewDisplayConfiguration conf) {
			PreviewItem previewItem = _previewItem;

			if (!_hasBeenLoaded) {
				_tabControlPreview.Dispatch(p => p.Visibility = Visibility.Hidden);
				return;
			}

			if (previewItem.FileName != _previewItem.FileName || _previewItems.Count != 0) return;

			PreviewDisplayUpdaterConfiguration res = conf - _currentConf;

			if (res.HasValueChanged(new { conf.ShowAnimation })) _tabItemActPreview.Dispatch(p => p.Visibility = conf.ShowAnimation ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowGat })) _tabItemMapGatPreview.Dispatch(p => p.Visibility = conf.ShowGat ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowHexEditor })) _tabItemResourcePreview.Dispatch(p => p.Visibility = conf.ShowHexEditor ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowTextEditor })) _tabItemTextPreview.Dispatch(p => p.Visibility = conf.ShowTextEditor ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowFolderPreview })) _tabFolderPreview.Dispatch(p => p.Visibility = conf.ShowFolderPreview ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowContainerPreview })) _tabContainerPreview.Dispatch(p => p.Visibility = conf.ShowContainerPreview ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowSpritesPreview })) _tabSpritesPreview.Dispatch(p => p.Visibility = conf.ShowSpritesPreview ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowFolderStructurePreview })) _tabFolderStructurePreview.Dispatch(p => p.Visibility = conf.ShowFolderStructurePreview ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowGrfProperties })) _tabItemGrfPropertiesPreview.Dispatch(p => p.Visibility = conf.ShowGrfProperties ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowRawStructureTextEditor })) _tabItemRawStructurePreview.Dispatch(p => p.Visibility = conf.ShowRawStructureTextEditor ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowImagePreview })) _tabItemImagePreview.Dispatch(p => p.Visibility = conf.ShowImagePreview ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowMapExtractor })) _tabItemMapExtractorPreview.Dispatch(p => p.Visibility = conf.ShowMapExtractor ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowDb })) _tabItemDbPreview.Dispatch(p => p.Visibility = conf.ShowDb ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowEditSprite })) _tabItemEditSpritePreview.Dispatch(p => p.Visibility = conf.ShowEditSprite ? Visibility.Visible : Visibility.Collapsed);
			//if (res.HasValueChanged(new { conf.ShowGatEditor })) _tabItemGatEditorPreview.Dispatch(p => p.Visibility = conf.ShowGatEditor ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowRsm })) _tabItemRsmPreview.Dispatch(p => p.Visibility = conf.ShowRsm ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowStr })) _tabItemStrPreview.Dispatch(p => p.Visibility = conf.ShowStr ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowSoundPreview })) _tabItemWavPreview.Dispatch(p => p.Visibility = conf.ShowSoundPreview ? Visibility.Visible : Visibility.Collapsed);
			if (res.HasValueChanged(new { conf.ShowLubDecompiler })) _tabItemLubPreview.Dispatch(p => p.Visibility = conf.ShowLubDecompiler ? Visibility.Visible : Visibility.Collapsed);

			_tabControlPreview.Dispatcher.Invoke(new Action(delegate {
				if (previewItem.FileName != _previewItem.FileName || _previewItems.Count != 0) return;

				if (_tabControlPreview.Items.Count <= 0)
					return;

				List<int> availableTabs = _tabControlPreview.Items.Cast<TabItem>().Where(p => p.Visibility == Visibility.Visible).Select(p => _tabControlPreview.Items.IndexOf(p)).ToList();

				if (availableTabs.Count == 0) {
					_tabControlPreview.Visibility = Visibility.Hidden;
					return;
				}
				else {
					_tabControlPreview.Visibility = Visibility.Visible;
				}

				string extension = _previewItem.Extension ?? "";

				if (_lastTabTools.Any(availableTabs.Contains)) {
					int preferredTool = -1;
					List<int> lastTabToolsReversed = new List<int>(_lastTabTools);
					lastTabToolsReversed.Reverse();

					foreach (int option in lastTabToolsReversed) {
						if (availableTabs.Contains(option)) {
							preferredTool = option;
							break;
						}
					}

					if (_preferredTools.ContainsKey(extension)) {
						_preferredTools[extension] = preferredTool;
					}
					else {
						_preferredTools.Add(extension, preferredTool);
					}

					_tabControlPreview.SelectedIndex = _preferredTools[extension];
				}
				else if (_preferredTools.ContainsKey(extension) && availableTabs.Contains(_preferredTools[extension])) {
					_tabControlPreview.SelectedIndex = _preferredTools[extension];
				}
				else {
					if (_preferredOptions.ContainsKey(extension) && availableTabs.Contains(_preferredOptions[extension])) {
						_tabControlPreview.SelectedIndex = _preferredOptions[extension];
					}
					else {
						TabItem item = _tabControlPreview.Items.Cast<TabItem>().FirstOrDefault(p => p.Visibility == Visibility.Visible);

						if (item != null) {
							_tabControlPreview.SelectedItem = item;
						}
					}
				}
			}));

			_currentConf = conf;
		}

		public static void Select(TreeView treeView, ListBox items, string oldSelectedPath) {
			new Thread(new ThreadStart(delegate {
				try {
					treeView = treeView ?? TreeView;
					items = items ?? ListView;

					if (treeView == null || items == null || String.IsNullOrEmpty(oldSelectedPath))
						return;

					oldSelectedPath = EncodingService.CorrectPathExplode(oldSelectedPath, true);
					items.Dispatch(p => p.SelectedItem = null);

					string[] folders = oldSelectedPath.Split('\\');

					TreeViewItem node = null;

					treeView.Dispatcher.Invoke((Action) delegate {
						if (folders.Length > 0) {
							node = (TkTreeViewItem) treeView.Items[0];
						}

						if (node == null)
							return;

						for (int i = 0; i < folders.Length; i++) {
							foreach (TkTreeViewItem item in node.Items) {
								if (String.Compare(item.HeaderText, folders[i], StringComparison.OrdinalIgnoreCase) == 0) {
									node.IsExpanded = true;
									node = item;
									break;
								}
							}
						}

						node.IsSelected = true;
					});

					if (folders.Last().Contains(".")) {
						int attempts = 40;
						TreeViewItem currentItem = treeView.Dispatcher.Invoke(new Func<TreeViewItem>(() => treeView.SelectedItem as TreeViewItem)) as TreeViewItem;
						FileEntry selectedEntry = items.Dispatcher.Invoke(new Func<FileEntry>(() => items.SelectedItem as FileEntry)) as FileEntry;

						if (currentItem == null)
							return;

						items.Dispatch(p => p.SelectedItem = null);

						while (attempts > 0) {
							attempts--;

							if (!Equals(currentItem, treeView.Dispatcher.Invoke(new Func<TreeViewItem>(() => treeView.SelectedItem as TreeViewItem)) as TreeViewItem)) {
								return;
							}

							if (selectedEntry != items.Dispatcher.Invoke(new Func<FileEntry>(() => items.SelectedItem as FileEntry)) as FileEntry) {
								return;
							}

							if ((int) items.Dispatcher.Invoke(new Func<int>(() => items.Items.Count)) == 0) {
								Thread.Sleep(200);
								continue;
							}

							bool found = (bool) items.Dispatcher.Invoke(new Func<bool>(delegate {
								bool hasBeenFound = false;

								foreach (FileEntry entry in items.Items) {
									if (String.Compare(entry.RelativePath, oldSelectedPath, StringComparison.OrdinalIgnoreCase) == 0) {
										items.SelectedItem = entry;
										items.ScrollToCenterOfView(entry);
										hasBeenFound = true;
										break;
									}
								}

								return hasBeenFound;
							}));

							if (found)
								break;

							Thread.Sleep(100);
						}
					}

					treeView.Dispatcher.Invoke((Action)delegate {
						if (treeView.SelectedItem is TkTreeViewItem) {
							((TkTreeViewItem)treeView.SelectedItem).ScrollToCenterOfView(treeView);
							((TkTreeViewItem)treeView.SelectedItem).Focus();
							//((TkTreeViewItem)treeView.SelectedItem).ScrollToCenterOfView(treeView);
						}
					});
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			})) { Name = "GrfEditor - Select item thread" }.Start();
		}

		public static bool IsImageCutable(string filename, GrfHolder grfData) {
			try {
				PreviewItem previewItem = new PreviewItem { FileName = filename, Extension = filename.GetExtension() };
				string path = Path.GetDirectoryName(previewItem.FileName);

				if (path == null)
					return false;

				string extension = previewItem.Extension;

				string fileNameWithoutNumber = Path.GetFileNameWithoutExtension(previewItem.FileName);

				if (fileNameWithoutNumber == null)
					return false;

				if (!fileNameWithoutNumber.Contains('-'))
					return false;

				fileNameWithoutNumber = fileNameWithoutNumber.Remove(fileNameWithoutNumber.Length - 3, 3);

				int numX = 1;
				int numY = 1;

				// We try to find the number of images on X
				while (grfData.FileTable.InternalContains(Path.Combine(path, fileNameWithoutNumber) + "1-" + (numX + 1) + extension)) {
					numX++;
				}

				// We try to find the number of images on Y
				while (grfData.FileTable.InternalContains(Path.Combine(path, fileNameWithoutNumber) + (numY + 1) + "-1" + extension)) {
					numY++;
				}

				// We check if all the images exist
				for (int i = 0; i < numY; i++) {
					for (int j = 0; j < numX; j++) {
						string fullFileName = Path.Combine(path, fileNameWithoutNumber) + (i + 1) + "-" + (j + 1) + extension;
						if (!grfData.FileTable.InternalContains(fullFileName))
							return false;
					}
				}

				if (numX == 0 || numY == 0)
					return false;

				return true;
			}
			catch {
				return false;
			}
		}

		public static void RebuildSelectedImage(string filename, GrfHolder grfData, Image imagePreview) {
			try {
				PreviewItem previewItem = new PreviewItem { FileName = filename, Extension = filename.GetExtension() };
				string path = Path.GetDirectoryName(previewItem.FileName);
				string extension = previewItem.Extension;
				string fileNameWithoutNumber = Path.GetFileNameWithoutExtension(previewItem.FileName);

				if (fileNameWithoutNumber == null)
					return;

				if (path == null)
					return;

				if (!fileNameWithoutNumber.Contains('-'))
					throw new Exception("The image doesn't appear to be a separated image.");

				fileNameWithoutNumber = fileNameWithoutNumber.Remove(fileNameWithoutNumber.Length - 3, 3);

				List<GrfImage> images = new List<GrfImage>();

				int width = -1;
				int height = -1;

				int numX = 1;
				int numY = 1;

				// We try to find the number of images on X
				while (grfData.FileTable.InternalContains(Path.Combine(path, fileNameWithoutNumber) + "1-" + (numX + 1) + extension)) {
					numX++;
				}

				// We try to find the number of images on Y
				while (grfData.FileTable.InternalContains(Path.Combine(path, fileNameWithoutNumber) + (numY + 1) + "-1" + extension)) {
					numY++;
				}

				for (int i = 0; i < numY; i++) {
					int twidth = 0;
					for (int j = 0; j < numX; j++) {
						string fullFileName = Path.Combine(path, fileNameWithoutNumber) + (i + 1) + "-" + (j + 1) + extension;
						GrfImage timage = ImageProvider.GetImage(grfData.FileTable[fullFileName].GetDecompressedData(), extension);
						twidth += timage.Width;
						images.Add(timage);
					}

					if (width == -1)
						width = twidth;
					else {
						if (twidth != width)
							throw new Exception("The width of the images don't match.");
					}
				}

				for (int j = 0; j < numX; j++) {
					int theight = 0;

					for (int i = 0; i < numY; i++) {
						theight += images[i * numX + j].Height;
					}

					if (height == -1)
						height = theight;
					else {
						if (height != theight)
							throw new Exception("The height of the images don't match.");
					}
				}

				WriteableBitmap bit = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

				int posX;
				int posY = 0;

				for (int i = 0; i < numY; i++) {
					posX = 0;
					for (int j = 0; j < numX; j++) {
						if (images[numX * i + j].GrfImageType != GrfImageType.Bgra32) {
							images[numX * i + j].Convert(new Bgra32FormatConverter());
						}

						BitmapSource im = images[numX * i + j].Cast<BitmapSource>();

						byte[] pixels = new byte[im.PixelHeight * im.PixelWidth * 4];
						im.CopyPixels(pixels, im.PixelWidth * 4, 0);
						bit.WritePixels(new Int32Rect(posX, posY, im.PixelWidth, im.PixelHeight), pixels, im.PixelWidth * 4, 0);
						posX += im.PixelWidth;
					}

					posY += images[0].Height;
				}

				bit.Freeze();
				imagePreview.Source = bit;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}