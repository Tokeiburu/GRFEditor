using System;
using System.IO;
using System.Windows;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;

namespace GrfToWpfBridge.TreeViewManager {
	public class ProjectTreeViewItem : TkTreeViewItem {
		private TkPath _tkPath;

		public ProjectTreeViewItem(TkView parent)
			: base(parent) {
			CanBeDragged = false;
		}

		public TkPath TKPath {
			get { return _tkPath; }
			set {
				_tkPath = value;

				if (!IsLoaded)
					Loaded += new RoutedEventHandler(_projectTreeViewItem_Loaded);
			}
		}

		private void _projectTreeViewItem_Loaded(object sender, RoutedEventArgs e) {
			string ext;
			TVIHeaderBrush.ToolTip = _tkPath.FilePath;
			ext = _tkPath.FilePath.GetExtension();

			if (!String.IsNullOrEmpty(_tkPath.RelativePath)) {
				string folder = Path.GetFileName(_tkPath.RelativePath);

				if (folder.GetExtension() != null) {
					ext = _tkPath.RelativePath.GetExtension();
					TVIHeaderBrush.ToolTip = folder;
				}
			}

			switch (ext) {
				case ".grf":
					PathIconClosed = "grf-16.png";
					PathIconOpened = "grf-16.png";
					break;
				case ".rgz":
					PathIconClosed = "rgz-16.png";
					PathIconOpened = "rgz-16.png";
					break;
				case ".gpf":
					PathIconClosed = "gpf-16.png";
					PathIconOpened = "gpf-16.png";
					break;
				case ".syn":
					PathIconClosed = "syn-16.png";
					PathIconOpened = "syn-16.png";
					break;
				case ".root":
					PathIconClosed = "home.png";
					PathIconOpened = "home.png";
					break;
				case ".thor":
					PathIconClosed = "thor-16.png";
					PathIconOpened = "thor-16.png";
					break;
			}
		}
	}
}