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
using GRF.System;
using GRF.Threading;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.Tools.SpriteEditor;
using GRFEditor.WPF;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.TreeViewManager;
using TheCodeKing.Net.Messaging;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;
using Utilities.Services;
using Action = System.Action;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;
using OpeningService = GRFEditor.Core.Services.OpeningService;

namespace GRFEditor {
	/// <summary>
	/// Interaction logic for empty.xaml
	/// </summary>
	public partial class EditorMainWindow : Window {
		internal readonly GrfHolder _grfHolder = new GrfHolder();
		private AsyncOperation _asyncOperation;
		private ExtractingService _extractingService;
		internal GrfLoadingSettings _grfLoadingSettings = new GrfLoadingSettings();
		private double _oldGridWidth;
		private OpeningService _openingService;
		private PreviewResourceIndexer _previewResourceIndexer;
		private PreviewService _previewService;
		private RenamingService _renamingService;
		internal TreeViewPathManager _treeViewPathManager;
		public static EditorMainWindow Instance;

		public EditorMainWindow() {
			Instance = this;
			InitializeComponent();
			_parseCommandLineArguments();
			_loadEditorUI();
			_loadServices();
			_loadEditorSettings();
			_loadCpuPerformance();
			_loadEvents();
			_initSearchThreads();
		}

		private int _loadBasicSettings() {
			int encoding = Configuration.EncodingCodepage;
			OpeningService.Enabled = Configuration.AlwaysOpenAfterExtraction;
			Settings.MaximumNumberOfThreads = Configuration.MaximumNumberOfThreads;
			Settings.TempPath = Configuration.TempPath;
			Settings.CpuMonitoringEnabled = Configuration.CpuPerformanceManagement;
			Settings.LockFiles = Configuration.LockFiles;
			Settings.AddHashFileForThor = Configuration.AddHashFileForThor;
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
					WindowProvider.ShowDialog("Minimalist debug log : \r\n" + Methods.Aggregate(_grfHolder.Header.Errors, "\r\n"), "Errors were detected", MessageBoxButton.OK);
				}
			};
			_loadMenus();

			if (_grfLoadingSettings.FileName == null && Configuration.AlwaysReopenLatestGrf) {
				if (_recentFilesManager.Files.Count > 0 && File.Exists(_recentFilesManager.Files[0])) {
					_grfLoadingSettings.FileName = _recentFilesManager.Files[0];
				}
			}

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listBoxResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "FileType", FixedWidth = 20, MaxHeight = 24 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "RelativePath", SearchGetAccessor = "RelativePath", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding = "RelativePath", MinWidth = 100 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", FixedWidth = 40, ToolTipBinding = "FileType", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", SearchGetAccessor = "NewSizeDecompressed", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "NewSizeDecompressed" }
			}, new DefaultListViewComparer<FileEntry>(), new string[] { "Added", "{DynamicResource CellBrushAdded}", "Lzma", "{DynamicResource CellBrushLzma}", "Encrypted", "{DynamicResource CellBrushEncrypted}", "Removed", "{DynamicResource CellBrushRemoved}" });

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_items, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "FileType", FixedWidth = 20, MaxHeight = 24 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "DisplayRelativePath", SearchGetAccessor = "RelativePath", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding = "RelativePath", MinWidth = 100 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", FixedWidth = 40, ToolTipBinding = "FileType", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", SearchGetAccessor = "NewSizeDecompressed", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "NewSizeDecompressed" }
			}, new DefaultListViewComparer<FileEntry>(), new string[] { "Added", "{DynamicResource CellBrushAdded}", "Lzma", "{DynamicResource CellBrushLzma}", "Encrypted", "{DynamicResource CellBrushEncrypted}", "Removed", "{DynamicResource CellBrushRemoved}" });

			WpfUtils.AddDragDropEffects(_items);
			WpfUtils.AddDragDropEffects(_treeView, f => f.Select(p => p.GetExtension()).All(p => p == ".grf" || p == ".rgz" || p == ".thor" || p == ".gpf"));

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

					if (option.Args.All(p => p.GetExtension() == ".spr")) {
						int encoding = _loadBasicSettings();
						EncodingService.SetDisplayEncoding(encoding);

						string execFileName = Configuration.ProgramName;
						int curId = Process.GetCurrentProcess().Id;

						foreach (Process proc in Process.GetProcessesByName(execFileName)) {
							try {
								if (proc.Id != curId) {
									XDListener listener = new XDListener();
									bool response = false;

									listener.RegisterChannel("openSpriteResponse");
									listener.MessageReceived += (s, e) => {
										if (e.DataGram.Channel == "openSpriteResponse") {
											response = true;
										}
									};

									XDBroadcast.SendToChannel("openSprite", options[0].Args[0]);
									Thread.Sleep(500);
									listener.UnRegisterChannel("openSpriteResponse");

									if (!response) {
										break;
									}

									ApplicationManager.Shutdown();
									return;
								}
							}
							catch {
								//ErrorHandler.HandleException(err);
							}
						}

						Window window = new SpriteConverter(options[0].Args);
						Application.Current.MainWindow = window;
						window.ShowDialog();
						ApplicationManager.Shutdown();
					}
					else {
						_grfLoadingSettings.FileName = option.Args[0];
					}
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

		private void _menuItemCompress_Click(object sender, RoutedEventArgs e) {
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
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.Save(), _grfHolder, 250, AsyncOperationReturnState.DoesNotRequireVisualReload), _grfSavingFinished);
				}

				if (e != null)
					e.Handled = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemCompact_Click(object sender, RoutedEventArgs e) {
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
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.Compact(), _grfHolder, 250, AsyncOperationReturnState.DoesNotRequireVisualReload), _grfSavingFinished);
				}

				if (e != null)
					e.Handled = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

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

				//GrfEditorConfiguration.
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
					_update();
				}

				_setupTitle(_grfHolder.IsModified || _grfHolder.IsNewGrf);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Redo() {
			try {
				if (_grfHolder.Commands.Redo()) {
					_update();
				}

				_setupTitle(_grfHolder.IsModified || _grfHolder.IsNewGrf);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _update() {
			_search(false);
			_loadListItems();
			_previewService.ShowPreview(_grfHolder, _treeViewPathManager.GetCurrentRelativePath(), null);
		}

		public void InvalidateVisualOnly() {
			_search(false);
			_loadListItems();
		}

		#endregion

		#region Logic methods

		private void _grfSavingFinished(object state) {
			AsyncOperationReturnState op = state == null ? AsyncOperationReturnState.None : (AsyncOperationReturnState) state;

			if (_grfHolder.CancelReload) {
				return;
			}

			try {
				_grfLoadingSettings.ReloadKey = _grfHolder.Header.EncryptionKey != null;

				if (!_grfHolder.CancelReload)
					_grfHolder.Close();

				_recentFilesManager.AddRecentFile(_grfLoadingSettings.FileName);

				if (op.HasFlags(AsyncOperationReturnState.DoesNotRequireVisualReload)) {
					_grfLoadingSettings.VisualReloadRequired = false;
					_grfLoadingSettings.ReloadKey = true;
				}

				Load(null);
			}
			catch (Exception ex) {
				ErrorHandler.HandleException(ex);
			}
		}

		private void _loadCpuPerformance() {
			new Thread(() => CpuPerformance.GetCurrentCpuUsage()) { Name = "GrfEditor - CpuPerformance loading thread" }.Start();
		}

		private void _checkIfEncrypted(bool fromLoading = true) {
			new Thread(new ThreadStart(delegate {
				if ((fromLoading && _grfHolder.Header.IsEncrypted) ||
				    (!fromLoading && _grfHolder.Header.EncryptionKey == null)) {
					Dispatcher.Invoke(new Action(delegate {
						try {
							EncryptorInputKeyDialog dialog = new EncryptorInputKeyDialog((fromLoading ? "The file has been encrypted by using GRF Editor. " : "") + "Enter the encryption key to automatically decrypt the content or click cancel to ignore.");
							dialog.Owner = this;
							dialog.ShowDialog();

							if (dialog.Result == MessageBoxResult.OK) {
								Configuration.EncryptorPassword = dialog.Key;
								_grfHolder.Header.SetKey(Configuration.EncryptorPassword, _grfHolder);

								if (!fromLoading) {
									_grfHolder.Header.IsEncrypted = true;
									_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfHolder.SetEncryptionFlag(true), _grfHolder, 300, null, true));
									//_grfHolder.SetEncryptionFlag(true);
								}
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}));
				}
			})) { Name = "GrfEditor - Encryption validation thread" }.Start();
		}

		#endregion
	}
}