using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.GrfSystem;
using GRF.Threading;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.WPF;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;
using OpeningService = GRFEditor.Core.Services.OpeningService;
using GRFEditor.ApplicationConfiguration;
using System.Threading.Tasks;
using TokeiLibrary.WPF.Styles;

namespace GRFEditor {
	/// <summary>
	/// Interaction logic for empty.xaml
	/// </summary>
	public partial class EditorMainWindow : Window {
		internal readonly GrfHolder _grfHolder = new GrfHolder();
		internal AsyncOperation _asyncOperation;
		private ExtractingService _extractingService;
		internal GrfLoadSettings _lastLoadSettings = null;
		private double _oldGridWidth;
		private OpeningService _openingService;
		private PreviewResourceIndexer _previewResourceIndexer;
		private PreviewService _previewService;
		private RenamingService _renamingService;
		internal TreeViewPathManager _treeViewPathManager;
		private EditorPosition _editorPosition = new EditorPosition();
		public static EditorMainWindow Instance;

		public EditorMainWindow() {
			Instance = this;
			InitializeComponent();
			Title = Configuration.ProgramName;
			_parseCommandLineArguments();
			_loadEditorUI();
			_loadServices();
			_loadEditorSettings();
			_loadCpuPerformance();
			_loadEvents();
			_initSearchThreads();

			if (_lastLoadSettings != null) {
				Load(_lastLoadSettings);
			}
			else {
				_lastLoadSettings = new GrfLoadSettings();
				_newWithDataFolder(true);
			}
		}

		private int _loadBasicSettings() {
			int encoding = Configuration.EncodingCodepage;
			Settings.MaximumNumberOfThreads = Configuration.MaximumNumberOfThreads;
			Settings.TempPath = Configuration.TempPath;
			Settings.CpuMonitoringEnabled = Configuration.CpuPerformanceManagement;
			Settings.LockFiles = Configuration.LockFiles;
			Settings.AddHashFileForThor = Configuration.AddHashFileForThor;
			Settings.FullFileTableEncryptionSupport = Configuration.FullFileTableEncryptionSupport;
			Settings.DecompileLubOnExtract = Configuration.DecompileLubOnExtract;
			Settings.DecompileLubFileExtension = Configuration.DecompileLubFileExtension;
			TemporaryFilesManager.ClearTemporaryFiles();
			return encoding;
		}

		private void _loadEditorSettings() {
			try {
				int encoding = _loadBasicSettings();

				CompressionMethodPicker.Load();
				EncryptionMethodPicker.Load();

				if (!SetEncoding(encoding)) {
					SetEncoding(1252);
				}

				GrfEditorConfiguration.OverrideShortcuts();
				ApplicationShortcut.Link(GrfEditorCommands.New, _menuItemNewGrf, this);
				ApplicationShortcut.Link(GrfEditorCommands.NewGpf, _menuItemNewGpf, this);
				ApplicationShortcut.Link(GrfEditorCommands.NewRgz, _menuItemNewRgz, this);
				ApplicationShortcut.Link(GrfEditorCommands.NewThor, _menuItemNewThor, this);
				ApplicationShortcut.Link(GrfEditorCommands.Open, _menuItemOpenFrom, this);
				ApplicationShortcut.Link(GrfEditorCommands.CloseGrf, _menuItemCloseGrf, this);
				ApplicationShortcut.Link(GrfEditorCommands.Save, _menuItemSave, this);
				ApplicationShortcut.Link(GrfEditorCommands.SaveAs, _menuItemSaveAs, this);
				ApplicationShortcut.Link(GrfEditorCommands.SaveDefragment, _menuItemDefragment, this);
				ApplicationShortcut.Link(GrfEditorCommands.SaveCompact, _menuItemCompact, this);
				ApplicationShortcut.Link(GrfEditorCommands.SaveRepack, _menuItemRepack, this);
				ApplicationShortcut.Link(GrfEditorCommands.EncryptFileTable, _menuItemEncryptTable, this);
				ApplicationShortcut.Link(GrfEditorCommands.Undo, _buttonUndo, this);
				ApplicationShortcut.Link(GrfEditorCommands.Redo, _buttonRedo, this);
				ApplicationShortcut.Link(GrfEditorCommands.NavigationBackward, _buttonPositionUndo, this);
				ApplicationShortcut.Link(GrfEditorCommands.NavigationForward, _buttonPositionRedo, this);
				ApplicationShortcut.Link(GrfEditorCommands.Merge, _menuItemMerge, this);
				ApplicationShortcut.Link(GrfEditorCommands.Subtract, _menuItemSubtract, this);
				ApplicationShortcut.Link(GrfEditorCommands.Search, _menuFocus, this);
				ApplicationShortcut.Link(GrfEditorCommands.SearchList, _menuListFocus, this);
				ApplicationShortcut.Link(GrfEditorCommands.ExtractRgz, _menuItemExtractRgz, this);
				ApplicationShortcut.Link(GrfEditorCommands.OpenContainerOptions, _menuItemEditContainerSettings, this);
				ApplicationShortcut.Link(GrfEditorCommands.ValidateGrf, _menuItemValidateGrf, this);
				ApplicationShortcut.Link(GrfEditorCommands.OpenProgramData, _menuItemOpenProgramData, this);
				ApplicationShortcut.Link(GrfEditorCommands.ImportSprite, _menuItemImportSprite, this);
				ApplicationShortcut.Link(GrfEditorCommands.SpriteConverter, _menuItemSpriteConverter, this);
				ApplicationShortcut.Link(GrfEditorCommands.FlatMapsMaker, _menuItemFlatMapsMaker, this);
				ApplicationShortcut.Link(GrfEditorCommands.PatchMaker, _menuItemPatchMaker, this);
				ApplicationShortcut.Link(GrfEditorCommands.Hash, _menuItemHash, this);
				ApplicationShortcut.Link(GrfEditorCommands.Encoding, _menuItemEncoding, this);
				ApplicationShortcut.Link(GrfEditorCommands.GrfSearch, _menuItemSearch, this);
				ApplicationShortcut.Link(GrfEditorCommands.ImageConverter, _menuItemImageConverter, this);
				ApplicationShortcut.Link(GrfEditorCommands.GrfCL, _menuItemGrfCL, this);
				ApplicationShortcut.Link(GrfEditorCommands.OpenGL, _menuItemOpenGL, this);
				ApplicationShortcut.Link(GrfEditorCommands.Encryptor, _menuItemEncryptor, this);
				ApplicationShortcut.Link(GrfEditorCommands.Settings, _menuItemSettings, this);

				_assignItemShortcut(GrfEditorCommands.OpenProperty, _miProperties, _menuItemsProperties_Click);
				_assignItemShortcut(GrfEditorCommands.Delete, _miDelete, _menuItemsDelete_Click);
				_assignItemShortcut(GrfEditorCommands.Rename, _miRename, _menuItemsRename_Click);
				_assignItemShortcut(GrfEditorCommands.Extract, _miExtract, _menuItemsExtract_Click);
				_assignItemShortcut(GrfEditorCommands.ExtractTo, _miExtractAt, _menuItemsExtractAt_Click);
				_assignItemShortcut(GrfEditorCommands.SelectInExplorer, _miOpenExplorer, _menuItemsOpenExplorer_Click);
				_assignItemShortcut(GrfEditorCommands.OpenFile, _miOpen, _menuItemsOpen_Click);
				_assignItemShortcut(GrfEditorCommands.ExportSelection, _miExportAsThor, _miExportAsThor_Click);
				_assignItemShortcut(GrfEditorCommands.FindUsage, _miUsage, _menuItemsUsage_Click);
				_assignItemShortcut(GrfEditorCommands.GrfeEncrypt, _miEncrypt, _menuItemsEncrypt_Click);
				_assignItemShortcut(GrfEditorCommands.GrfeDecrypt, _miDecrypt, _menuItemsDecrypt_Click);
				_assignItemShortcut(GrfEditorCommands.GravityDecrypt, _miDecodeProtectedEntries, _menuItemsGravityDecode_Click);
				_assignItemShortcut(GrfEditorCommands.SetKey, _miSetEncryptionKey, _menuItemSetEncryptionKey_Click);

				ApplicationShortcut.Link(GrfEditorCommands.Rename, _miTreeRename, this);
				ApplicationShortcut.Link(GrfEditorCommands.Delete, _miTreeDelete, this);
				ApplicationShortcut.Link(GrfEditorCommands.TreeAdd, _miTreeAdd, this);
				ApplicationShortcut.Link(GrfEditorCommands.TreeNewFolder, _miTreeNewFolder, this);
				ApplicationShortcut.Link(GrfEditorCommands.Extract, _miTreeExtractFolder, this);
				ApplicationShortcut.Link(GrfEditorCommands.ExtractTo, _miTreeExtractFolderAt, this);
				ApplicationShortcut.Link(GrfEditorCommands.ExtractRootFiles, _miTreeExtractRootFiles, this);
				ApplicationShortcut.Link(GrfEditorCommands.ExtractRootFilesTo, _miTreeExtractRootFilesAt, this);
				ApplicationShortcut.Link(GrfEditorCommands.GrfeEncrypt, _miTreeEncrypt, this);
				ApplicationShortcut.Link(GrfEditorCommands.GrfeDecrypt, _miTreeDecrypt, this);
				ApplicationShortcut.Link(GrfEditorCommands.GravityDecrypt, _miTreeDecodeProtectedEntries, this);
				ApplicationShortcut.Link(GrfEditorCommands.SetKey, _miTreeSetEncryptionKey, this);
				ApplicationShortcut.Link(GrfEditorCommands.SelectInExplorer, _miTreeSelectInExplorer, this);
				ApplicationShortcut.Link(GrfEditorCommands.SelectInExplorer, _miTreeOpenExplorer, this);
				ApplicationShortcut.Link(GrfEditorCommands.OpenProperty, _miTreeProperties, this);
			}
			catch {
			}
		}

		private void _assignItemShortcut(TkCommand command, TkMenuItem menuItem, Action<object, RoutedEventArgs> routedCommand) {
			menuItem.ShortcutCmd = command.CommandName;

			ListView[] lists = new[] { _listBoxResults, _items };

			foreach (var list in lists) {
				ApplicationShortcut.Link(command, () => routedCommand(list, null), list);
			}
		}

		private void _loadEditorUI() {
			_asyncOperation = new AsyncOperation(_progressBarComponent);
			_recentFilesManager = new WpfRecentFiles(Configuration.ConfigAsker, 6, _menuItemRecentFiles, "GRFEditor");

			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

			_treeViewPathManager = new TreeViewPathManager(_treeView);
			_items.ItemsSource = _itemEntries;
			_listBoxResults.ItemsSource = _itemSearchEntries;
			_recentFilesManager.FileClicked += _recentFilesManager_FileClicked;
			_treeView.PreviewMouseLeftButtonDown += _treeView_PreviewMouseLeftButtonDown;
			_listBoxResults.PreviewMouseLeftButtonDown += _listBoxResults_PreviewMouseLeftButtonDown;
			_progressBarComponent.ShowErrors += delegate {
				if (_grfHolder.IsOpened) {
					WindowProvider.ShowDialog("Minimalist debug log: \r\n" + Methods.Aggregate(_grfHolder.Header.Errors, "\r\n"), "Errors were detected", MessageBoxButton.OK);
				}
			};
			_loadMenus();

			if (_lastLoadSettings == null && Configuration.AlwaysReopenLatestGrf) {
				if (_recentFilesManager.Files.Count > 0 && File.Exists(_recentFilesManager.Files[0])) {
					_lastLoadSettings = new GrfLoadSettings();
					_lastLoadSettings.FileName = _recentFilesManager.Files[0];
				}
			}

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listBoxResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "FileType", FixedWidth = 20, MaxHeight = 16 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "RelativePath", SearchGetAccessor = "RelativePath", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding = "RelativePath", MinWidth = 100 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", FixedWidth = 40, ToolTipBinding = "FileType", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", SearchGetAccessor = "NewSizeDecompressed", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "NewSizeDecompressed" }
			}, new DefaultListViewComparer<FileEntry>(), new string[] { "Added", "{DynamicResource CellBrushAdded}", "LzmaCompressed", "{DynamicResource CellBrushCustomCompression}", "Encrypted", "{DynamicResource CellBrushEncrypted}", "Removed", "{DynamicResource CellBrushRemoved}", "Editor_GravityEncrypted", "{DynamicResource CellGravityEncrypted}" });

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_items, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "FileType", FixedWidth = 20, MaxHeight = 16 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "DisplayRelativePath", SearchGetAccessor = "RelativePath", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding = "RelativePath", MinWidth = 100 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", FixedWidth = 40, ToolTipBinding = "FileType", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", SearchGetAccessor = "NewSizeDecompressed", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "NewSizeDecompressed" }
			}, new DefaultListViewComparer<FileEntry>(), new string[] { "Added", "{DynamicResource CellBrushAdded}", "LzmaCompressed", "{DynamicResource CellBrushCustomCompression}", "Encrypted", "{DynamicResource CellBrushEncrypted}", "Removed", "{DynamicResource CellBrushRemoved}", "Editor_GravityEncrypted", "{DynamicResource CellGravityEncrypted}" });

			WpfUtilities.AddDragDropEffects(_items);
			WpfUtilities.AddDragDropEffects(_treeView, f => f.Select(p => p.GetExtension()).All(p => p == ".grf" || p == ".rgz" || p == ".thor" || p == ".gpf" || p == ".gsf"));

			_grfEntrySorter.SetOrder("DisplayRelativePath", ListSortDirection.Ascending);
			_grfSearchEntrySorter.SetOrder("RelativePath", ListSortDirection.Ascending);
			_treeView.DoDragDropCustomMethod = delegate {
				VirtualFileDataObjectProgress vfop = new VirtualFileDataObjectProgress();
				VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject(
					_ => _asyncOperation.SetAndRunOperation(new GrfThread(vfop.Update, vfop, null)),
					_ => vfop.Finished = true
					);

				string currentRelativePath = _treeViewPathManager.GetCurrentRelativePath();
				string headPath = Path.GetDirectoryName(currentRelativePath) + "\\";

				List<VirtualFileDataObject.FileDescriptor> descriptors = _grfHolder.FileTable.GetFiles(currentRelativePath, SearchOption.AllDirectories).Select(file => new VirtualFileDataObject.FileDescriptor {
					Name = file.StartsWith(headPath) ? file.ReplaceFirst(headPath, "") : file,
					GrfData = _grfHolder,
					FilePath = file,
					Length = _grfHolder.FileTable[file].NewSizeDecompressed,
					StreamContents = (grfData, filePath, stream, _) => {
						var data = grfData.FileTable[filePath].GetDecompressedData();
						stream.Write(data, 0, data.Length);
						vfop.ItemsProcessed++;
					}
				}).ToList();

				vfop.Vfo = virtualFileDataObject;
				vfop.ItemsToProcess = descriptors.Count;
				virtualFileDataObject.Source = DragAndDropSource.TreeView;
				virtualFileDataObject.SelectedPath = currentRelativePath;
				virtualFileDataObject.SetData(descriptors);

				try {
					VirtualFileDataObject.DoDragDrop(_treeView, virtualFileDataObject, DragDropEffects.Copy);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			_grfHolder.ContainerOpened += delegate {
				_grfHolder.Commands.CommandIndexChanged += _grfHolder_ModifiedStateChanged;

				this.Dispatch(delegate {
					_tmbUndo.LinkUndo(_grfHolder.Commands, Undo);
					_tmbRedo.LinkRedo(_grfHolder.Commands, Redo);
				});
			};

			_listBoxResults.Loaded += delegate {
				_listBoxResults.Visibility = Visibility.Collapsed;
				_listBoxResults.SetValue(Grid.RowProperty, 0);
			};

			_editorPosition.Load(this);
		}

		private void _loadServices() {
			_previewService = new PreviewService(_asyncOperation, _tabControlPreview, _treeView, _items, this);
			_openingService = new OpeningService();
			_extractingService = new ExtractingService(_asyncOperation);
			_renamingService = new RenamingService();
			Configuration.Resources = new Configuration.GrfResources(_grfHolder);

			PreviewService.ListView = _items;
			PreviewService.TreeView = _treeView;
		}

		private void _parseCommandLineArguments() {
			List<GenericCLOption> options = CommandLineParser.GetOptions(Environment.CommandLine, false);

			foreach (GenericCLOption option in options) {
				if (option.CommandName == "-REM" || option.CommandName == "REM") {
					break;
				}
				else if (option.CommandName == "-shellCommand") {
					try {
						new MultiProgressWindow(option).ShowDialog();
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
					ApplicationManager.Shutdown();
				}
				else {
					if (option.Args.Count <= 0)
						continue;

					_lastLoadSettings = new GrfLoadSettings();
					_lastLoadSettings.FileName = option.Args[0];
				}
			}
		}

		private void _buttonExpand_Click(object sender, RoutedEventArgs e) {
			if (_buttonExpand.IsPressed) {
				_buttonExpand.TextHeader = "Hide";
				_gridSplitterPanels.Visibility = Visibility.Visible;
				_primaryGrid.ColumnDefinitions[0].Width = new GridLength(_oldGridWidth);
			}
			else {
				_buttonExpand.TextHeader = "Show";
				_gridSplitterPanels.Visibility = Visibility.Collapsed;
				_oldGridWidth = _primaryGrid.ColumnDefinitions[0].ActualWidth;
				_primaryGrid.ColumnDefinitions[0].Width = new GridLength(0);
			}

			_buttonExpand.IsPressed = !_buttonExpand.IsPressed;
		}

		private void _menuItemOpenProgramData_Click(object sender, RoutedEventArgs e) {
			try {
				Utilities.Services.OpeningService.FileOrFolder(Configuration.ProgramDataPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSelect_Click(object sender, RoutedEventArgs e) {
			try {
				if (File.Exists(_grfHolder.FileName)) {
					try {
						Utilities.Services.OpeningService.FileOrFolder(_grfHolder.FileName);
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
				else if (_grfHolder.IsNewGrf || !File.Exists(_grfHolder.FileName)) {
					ErrorHandler.HandleException("The GRF file doesn't exist yet.", ErrorLevel.NotSpecified);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonUndo_Click(object sender, RoutedEventArgs e) {
			Undo();
		}

		private void _menuItemNewThor_Click(object sender, RoutedEventArgs e) {
			try {
				if (!_validateNewContainer()) return;
				_newWithRoot("new.thor");

				foreach (var tab in _tabControlPreview.Items.OfType<TabItem>()) {
					if (tab.Content is PreviewContainer) {
						_tabControlPreview.SelectedItem = tab;
						break;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemDefragment_Click(object sender, RoutedEventArgs e) => _save(GrfEditorSaveMode.Defragment);
		private void _menuItemCompact_Click(object sender, RoutedEventArgs e) => _save(GrfEditorSaveMode.Compact);

		#region Window events

		protected override void OnClosing(CancelEventArgs e) {
			try {
				if (_grfHolder.IsOpened && _grfHolder.IsModified) {
					MessageBoxResult res = WindowProvider.ShowDialog("The GRF has been modified, do you want to save it first?", "Modified GRF",
					                                                 MessageBoxButton.YesNoCancel);
					if (res == MessageBoxResult.Yes) {
						_menuItemSaveAs_Click(null, null);
						e.Cancel = true;
						return;
					}

					if (res == MessageBoxResult.Cancel) {
						e.Cancel = true;
						return;
					}
				}

				_editorPosition.Save(this);
				_saveTreeExpansion();
				_asyncOperation.Cancel();
				ApplicationManager.Shutdown();
				base.OnClosing(e);
			}
			catch (Exception err) {
				try {
					ErrorHandler.HandleException("The application hasn't ended properly. Please report this issue.", err);
				}
				catch {
				}
				ApplicationManager.Shutdown();
			}
		}

		public void Backward(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs) {
			_positions.Undo();
		}

		public void Forward(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs) {
			_positions.Redo();
		}

		public void Undo(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs) {
			Undo();
		}

		public void Redo(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs) {
			Redo();
		}

		public void Undo() {
			try {
				if (_grfHolder.Commands.Undo()) {
					_update(false);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Redo() {
			try {
				if (_grfHolder.Commands.Redo()) {
					_update(false);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _update(bool async = false) {
			_search(async);
			_loadListItems();
			_previewService.ShowPreview(_grfHolder, _treeViewPathManager.GetCurrentRelativePath(), null);
		}

		public void InvalidateVisualOnly() {
			_search(false);
			_loadListItems();
		}

		#endregion

		#region Logic methods

		private void _loadCpuPerformance() {
			Task.Run(() => CpuPerformance.GetCurrentCpuUsage());
		}

		private void _checkIfEncrypted(bool fromLoading = true) {
			new Thread(new ThreadStart(delegate {
				if (_grfHolder.Header.EncryptionKey != null)
					return;

				if (fromLoading) {
					if (!_grfHolder.Header.IsEncrypted)
						return;
				}

				this.Dispatch(delegate {
					try {
						EncryptorInputKeyDialog dialog = new EncryptorInputKeyDialog((fromLoading ? "The file has been encrypted by using GRF Editor. " : "") + "Enter the encryption key to automatically decrypt the content or click cancel to ignore.");
						dialog.Owner = this;
						dialog.ShowDialog();

						if (dialog.Result == MessageBoxResult.OK) {
							_grfHolder.Header.SetKey(dialog.Key, _grfHolder);

							if (!fromLoading) {
								_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.SetEncryptionFlag(true), _grfHolder, null, true));
							}
						}
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				});
			})) { Name = "GrfEditor - Encryption validation thread" }.Start();
		}

		#endregion
	}
}