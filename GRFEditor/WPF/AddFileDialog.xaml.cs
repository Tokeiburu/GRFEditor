using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for AddFile.xaml
	/// </summary>
	public partial class AddFileDialog : TkWindow {
		private string _filePath = "";

		public AddFileDialog(TreeView treeView, string grfPath = null) : base("Add files", "add.ico") {
			InitializeComponent();

			foreach (TkTreeViewItem item in treeView.Items) {
				_treeView.Items.Add(_copyNode(item));
			}

			_treeView.Items.Refresh();

			_selectNode(grfPath);
			_filePath = _textBoxSourceFile.Text;

			if (treeView.Items.Count > 0)
				GrfPath = new TkPath(TreeViewPathManager.GetTkPath(treeView.Items[0]));

			WpfUtilities.SetMinAndMaxSize(this);
			_treeView_SelectedItemChanged(null, null);
		}

		public string FilePath { get; set; }
		public TkPath GrfPath { get; set; }

		protected TreeViewItem _copyNode(TkTreeViewItem viewItem) {
			TkTreeViewItem currentNode;

			if (viewItem is ProjectTreeViewItem) {
				currentNode = new ProjectTreeViewItem(new TkPath(((ProjectTreeViewItem)viewItem).TkPath), _treeView);
			}
			else {
				currentNode = new TkTreeViewItem(_treeView);
			}

			currentNode.HeaderText = viewItem.HeaderText;
			currentNode.CanBeDragged = false;

			foreach (TkTreeViewItem item in viewItem.Items) {
				currentNode.Items.Add(_copyNode(item));
			}

			currentNode.IsExpanded = viewItem.IsExpanded;
			return currentNode;
		}

		protected void _selectNode(string grfPath) {
			try {
				if (grfPath == null) return;

				if (_treeView.Items.Count == 0)
					return;

				if (grfPath == "") {
					((TkTreeViewItem) _treeView.Items[0]).IsSelected = true;
					return;
				}

				string containerPath = grfPath.Split('?')[0];
				string[] values = grfPath.Split('?')[1].Split('\\');

				TkTreeViewItem currentNode = null;
				foreach (ProjectTreeViewItem pNode in _treeView.Items) {
					if (pNode.TkPath.FilePath == containerPath) {
						currentNode = pNode;
						break;
					}
				}

				if (currentNode == null)
					return;

				foreach (string f in values) {
					foreach (TkTreeViewItem item in currentNode.Items) {
						if (item.HeaderText == f) {
							currentNode = item;
							break;
						}
					}
				}

				currentNode.IsSelected = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			TkTreeViewItem item = _treeView.SelectedItem as TkTreeViewItem;
			var path = TreeViewPathManager.GetTkPath(item);

			if (path != null) {
				_textBoxGrfPath.Text = path.RelativePath;
				GrfPath = path;	
			}
		}

		protected void _buttonOK_Click(object sender, RoutedEventArgs e) {
			FilePath = _filePath;

			if (GrfPath.RelativePath == "") {
				if (GrfPath.FilePath.IsExtension(".rgz", ".thor")) {
					GrfPath.RelativePath = GrfStrings.RgzRoot;
				}
			}

			DialogResult = true;
			Close();
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		protected void Window_Drop(object sender, DragEventArgs e) {
			string file = _getFileName(e);
			_textBoxSourceFile.Text = file;
		}

		protected string _getFileName(DragEventArgs e) {
			try {
				Array data = e.Data.GetData("FileName") as Array;

				if (data != null && data.Length == 1) {
					return new FileInfo(((string[]) data)[0]).FullName;
				}
			}
			catch {
				return null;
			}

			return null;
		}

		protected void Window_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		protected void _textBoxSourceFile_TextChanged(object sender, TextChangedEventArgs e) {
			_filePath = _textBoxSourceFile.Text;
		}

		private void _buttonFolderBrowse_Click(object sender, RoutedEventArgs e) {
			string path = PathRequest.FolderExtract();

			if (path != null) {
				_textBoxSourceFile.Text = path;
			}
		}

		private void _buttonFileBrowse_Click(object sender, RoutedEventArgs e) {
			string file = PathRequest.OpenFileEditor();

			if (file != null) {
				_textBoxSourceFile.Text = file;
			}
		}
	}
}