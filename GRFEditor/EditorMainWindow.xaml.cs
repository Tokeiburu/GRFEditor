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

		public static class LZ4Raw {
			public static byte[] Decompress(byte[] input, int uncompressedSizeGuess = 0) {
				int outCap = uncompressedSizeGuess > 0 ? uncompressedSizeGuess : input.Length * 10;
				byte[] output = new byte[outCap];
				int ip = 0;
				int op = 0;

				while (ip < input.Length) {
					if (op >= output.Length - 300) {
						Array.Resize(ref output, output.Length * 2);
					}

					byte token = input[ip++];
					int literalLength = token >> 4;

					if (literalLength == 15) {
						byte len;
						do {
							len = input[ip++];
							literalLength += len;
						}
						while (len == 255);
					}

					// Copy literals
					Buffer.BlockCopy(input, ip, output, op, literalLength);
					ip += literalLength;
					op += literalLength;

					if (ip >= input.Length)
						break;

					// Read match offset
					int offset = input[ip++] | (input[ip++] << 8);

					int matchLength = token & 0x0F;

					// If match length is extended
					if (matchLength == 15) {
						byte len;
						do {
							len = input[ip++];
							matchLength += len;
						}
						while (len == 255);
					}

					matchLength += 4;
					int matchSrc = op - offset;

					if (matchSrc < 0)
						throw new Exception("Invalid LZ4 offset.");

					for (int i = 0; i < matchLength; i++) {
						output[op++] = output[matchSrc + i];
					}
				}

				Array.Resize(ref output, op);
				return output;
			}
		}

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
		}

		private int _loadBasicSettings() {
			int encoding = Configuration.EncodingCodepage;
			OpeningService.Enabled = Configuration.AlwaysOpenAfterExtraction;
			Settings.MaximumNumberOfThreads = Configuration.MaximumNumberOfThreads;
			Settings.TempPath = Configuration.TempPath;
			Settings.CpuMonitoringEnabled = Configuration.CpuPerformanceManagement;
			Settings.LockFiles = Configuration.LockFiles;
			Settings.AddHashFileForThor = Configuration.AddHashFileForThor;
			Settings.FullFileTableEncryptionSupport = Configuration.FullFileTableEncryptionSupport;
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

				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-N", "GrfEditor.New"), () => _menuItemNewGrf_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-O", "GrfEditor.Open"), () => _menuItemOpenFrom_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-S", "GrfEditor.Save"), () => _menuItemSave_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Shift-S", "GrfEditor.Defragment"), () => _menuItemCompress_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Alt-S", "GrfEditor.Compact"), () => _menuItemCompact_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Z", "GrfEditor.Undo"), () => _buttonUndo_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Y", "GrfEditor.Redo"), () => _buttonRedo_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Alt-Z", "GrfEditor.NavigateBackward"), () => _buttonPositionUndo_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Alt-Y", "GrfEditor.NavigateFoward"), () => _buttonPositionRedo_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-M", "GrfEditor.Merge"), () => _menuItemMerge_Click(null, null), this);
				ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-F", "GrfEditor.Search"), () => _menuFocus(null, null), this);
			}
			catch {
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
			_treeView.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_treeView_PreviewMouseLeftButtonDown);
			_listBoxResults.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_listBoxResults_PreviewMouseLeftButtonDown);
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
			}, new DefaultListViewComparer<FileEntry>(), new string[] { "Added", "{DynamicResource CellBrushAdded}", "CustomCompressed", "{DynamicResource CellBrushCustomCompression}", "Encrypted", "{DynamicResource CellBrushEncrypted}", "Removed", "{DynamicResource CellBrushRemoved}", "GravityEncrypted", "{DynamicResource CellGravityEncrypted}" });

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_items, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "FileType", FixedWidth = 20, MaxHeight = 16 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "DisplayRelativePath", SearchGetAccessor = "RelativePath", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding = "RelativePath", MinWidth = 100 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", FixedWidth = 40, ToolTipBinding = "FileType", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", SearchGetAccessor = "NewSizeDecompressed", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "NewSizeDecompressed" }
			}, new DefaultListViewComparer<FileEntry>(), new string[] { "Added", "{DynamicResource CellBrushAdded}", "CustomCompressed", "{DynamicResource CellBrushCustomCompression}", "Encrypted", "{DynamicResource CellBrushEncrypted}", "Removed", "{DynamicResource CellBrushRemoved}", "GravityEncrypted", "{DynamicResource CellGravityEncrypted}" });

			WpfUtilities.AddDragDropEffects(_items);
			WpfUtilities.AddDragDropEffects(_treeView, f => f.Select(p => p.GetExtension()).All(p => p == ".grf" || p == ".rgz" || p == ".thor" || p == ".gpf"));

			_grfEntrySorter.SetOrder("DisplayRelativePath", ListSortDirection.Ascending);
			_grfSearchEntrySorter.SetOrder("RelativePath", ListSortDirection.Ascending);
			_treeView.DoDragDropCustomMethod = delegate {
				VirtualFileDataObjectProgress vfop = new VirtualFileDataObjectProgress();
				VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject(
					_ => _asyncOperation.SetAndRunOperation(new GrfThread(vfop.Update, vfop, 500, null)),
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
					VirtualFileDataObject.DoDragDrop(_treeView, virtualFileDataObject, DragDropEffects.Move);
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

		private void _menuItemCompress_Click(object sender, RoutedEventArgs e) => _save(GrfEditorSaveMode.Compress);
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
								_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.SetEncryptionFlag(true), _grfHolder, 300, null, true));
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