using System;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using ErrorManager;
using TokeiLibrary.Paths;
using Utilities;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace TokeiLibrary.WPF.Styles {
	/// <summary>
	/// Interaction logic for PathBrowser.xaml
	/// </summary>
	public partial class PathBrowser : UserControl {
		#region Delegates

		public delegate void PathBrowserEventHandler(object sender, EventArgs e);

		#endregion

		#region BrowseModeType enum

		public enum BrowseModeType {
			Folder,
			File,
			Files
		}

		#endregion

		private bool _defaultButtonClickEventIsEnabled = true;

		private string _filter = "";

		private PathBrowserRecentFiles _recentFiles;
		private bool _useSavePath;
		private string _savePathUniqueName;

		public PathBrowserRecentFiles RecentFiles {
			get { return _recentFiles; }
		}

		public PathBrowser() {
			InitializeComponent();
			BrowseMode = BrowseModeType.Folder;
			_button.Click += new RoutedEventHandler(OnButtonClick);
			_tb.TextChanged += (s, e) => _onTextChanged(e);
			_tb.VerticalAlignment = VerticalAlignment.Center;

			Loaded += (e, a) => {
				if (UseSavePath) {
					_button.ContextMenu.Placement = PlacementMode.Bottom;
					_button.ContextMenu.PlacementTarget = _button;
					_button.PreviewMouseRightButtonUp += _disableButton;
				}
				else {
					_button.ContextMenu = null;
				}
			};
		}

		private void _disableButton(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
		}

		public bool UseSavePath {
			get { return _useSavePath; }
			set {
				_useSavePath = value;
				_setupRecentFiles();
			}
		}

		public string SavePathUniqueName {
			get { return _savePathUniqueName; }
			set {
				_savePathUniqueName = value;
				_setupRecentFiles();
			}
		}

		private void _setupRecentFiles() {
			if (UseSavePath && !String.IsNullOrEmpty(SavePathUniqueName) && _recentFiles == null) {
				_recentFiles = new PathBrowserRecentFiles(Configuration.ConfigAsker, 6, _miLoadRecent, SavePathUniqueName);
				_recentFiles.Reload();
				_recentFiles.FileClicked += new RecentFilesManager.RFMFileClickedEventHandler(_recentFiles_FileClicked);
			}
		}

		public void SetToLatestRecentFile() {
			if (this.RecentFiles.Files.Count > 0) {
				this.Text = this.RecentFiles.Files[0];
			}
		}

		public BrowseModeType BrowseMode {
			get;
			set;
		}

		public string Filter {
			get { return _filter; }
			set { _filter = value; }
		}

		public string Text {
			get { return _tb.Text; }
			set { _tb.Text = value; }
		}

		public Button Button {
			get { return _button; }
		}

		public TextBox TextBox {
			get { return _tb; }
		}

		public string FolderDescription { get; set; }

		public bool DefaultButtonClickEventIsEnabled {
			get { return _defaultButtonClickEventIsEnabled; }
			set { _defaultButtonClickEventIsEnabled = value; }
		}

		public event PathBrowserEventHandler Clicked;
		public event PathBrowserEventHandler TextChanged;

		public void _onClicked(EventArgs e) {
			PathBrowserEventHandler handler = Clicked;
			if (handler != null) handler(this, e);
		}

		public void _onTextChanged(EventArgs e) {
			PathBrowserEventHandler handler = TextChanged;
			if (handler != null) handler(this, e);
		}

		public bool IgnoreFileNotFound { get; set; }

		private void _recentFiles_FileClicked(string file) {
			if (!File.Exists(file) && !Directory.Exists(file) &&
				!file.StartsWith("sftp://") && !file.StartsWith("ftp://")) {
				_recentFiles.RemoveRecentFile(file);
				ErrorHandler.HandleException("File couldn't be found " + file, ErrorLevel.Low);
			}
			else {
				_tb.Text = file;
			}
		}

		public void Click() {
			_selectFileOrFolder();
			_onClicked(new RoutedEventArgs());
		}

		private void _selectFileOrFolder() {
			if (DefaultButtonClickEventIsEnabled) {
				if (BrowseMode == BrowseModeType.Folder) {
					string path = TkPathRequest.Folder("selectedPath", _tb.Text);

					if (path != null) {
						_tb.Text = path;

						if (UseSavePath) {
							_recentFiles.AddRecentFile(path);
						}
					}
				}
				else if (BrowseMode == BrowseModeType.File || BrowseMode == BrowseModeType.Files) {
					OpenFileDialog ofd = new OpenFileDialog();
					ofd.AddExtension = true;
					ofd.InitialDirectory = _tb.Text;
					ofd.Filter = Filter;
					ofd.ValidateNames = true;
					ofd.Multiselect = BrowseMode == BrowseModeType.Files;

					if (ofd.ShowDialog() == DialogResult.OK) {
						if (BrowseMode == BrowseModeType.Files && ofd.FileNames.Length > 1) {
							_tb.Text = Methods.Aggregate(ofd.FileNames, "|");
						}
						else {
							_tb.Text = ofd.FileName;
						}

						if (UseSavePath) {
							_recentFiles.AddRecentFile(ofd.FileName);
						}
					}
				}
			}
		}

		public virtual void OnButtonClick(object sender, RoutedEventArgs e) {
			if (UseSavePath) {
				_button.ContextMenu.IsOpen = true;
			}
			else {
				_selectFileOrFolder();
				_onClicked(e);
			}
		}

		private void _mainGrid_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		public void OnMainGridDrop(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent(DataFormats.FileDrop, true)) return;
			string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

			if (files == null || files.Length < 1) return;

			if (!String.IsNullOrEmpty(Filter)) {
				if (Path.GetExtension(files[0]) != null && !Filter.Contains("*" + Path.GetExtension(files[0]).ToLower())) {
					ErrorHandler.HandleException("This file type is not allowed.", ErrorLevel.NotSpecified);
					e.Handled = true;
					return;
				}
			}

			_tb.Text = files[0];

			if (UseSavePath)
				_recentFiles.AddRecentFile(files[0]);
		}

		private void _tb_PreviewDragOver(object sender, DragEventArgs e) {
			e.Handled = true;
		}

		private void _miClear_Click(object sender, RoutedEventArgs e) {
			_tb.Text = "";
		}

		private void _miLoad_Click(object sender, RoutedEventArgs e) {
			_selectFileOrFolder();
			_onClicked(e);
		}
	}
}
