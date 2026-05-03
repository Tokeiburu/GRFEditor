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
		public string SelectedFilePath { get; set; }
		public TkPath SelectedGrfPath { get; set; }

		private string _filePath = "";

		public AddFileDialog(TreeView treeView, TkPath grfSelectPath = null) : base("Add files", "add.ico") {
			InitializeComponent();

			_copyTreeView(treeView);
			_selectNode(grfSelectPath);
			_treeView.ScrollToCenterOfView(_treeView.SelectedItem);

			WpfUtilities.SetMinAndMaxSize(this);
		}

		private void _copyTreeView(TreeView treeView) {
			foreach (TkTreeViewItem item in treeView.Items) {
				_treeView.Items.Add(_copyNode(item));
			}

			_treeView.Items.Refresh();
		}

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

		protected void _selectNode(TkPath grfPath) {
			try {
				if (grfPath == null) return;

				if (_treeView.Items.Count == 0)
					return;

				if (grfPath == "") {
					((TkTreeViewItem) _treeView.Items[0]).IsSelected = true;
					return;
				}

				string[] folders = grfPath.RelativePath.Split('\\');

				TkTreeViewItem currentNode = null;
				foreach (ProjectTreeViewItem pNode in _treeView.Items) {
					if (pNode.TkPath.FilePath == grfPath.FilePath) {
						currentNode = pNode;
						break;
					}
				}

				if (currentNode == null)
					return;

				foreach (string f in folders) {
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
				SelectedGrfPath = path;	
			}
		}

		protected void _buttonOK_Click(object sender, RoutedEventArgs e) {
			SelectedFilePath = _filePath;

			if (SelectedGrfPath.RelativePath == "") {
				if (SelectedGrfPath.FilePath.IsExtension(".rgz", ".thor")) {
					SelectedGrfPath.RelativePath = GrfStrings.RgzRoot;
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