using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;

namespace GrfToWpfBridge.TreeViewManager {
	public class ProjectTreeViewItem : TkTreeViewItem {
		public static DependencyProperty ImagePathProperty = DependencyProperty.Register("ImagePath", typeof(ImageSource), typeof(ProjectTreeViewItem), new PropertyMetadata(default(ImageSource)));

		public ImageSource ImagePath {
			get { return (ImageSource)GetValue(ImagePathProperty); }
			set { SetValue(ImagePathProperty, value); }
		}

		public static DependencyProperty TooltipTextProperty = DependencyProperty.Register("TooltipText", typeof(string), typeof(ProjectTreeViewItem), new PropertyMetadata(default(string)));

		public string TooltipText {
			get { return (string)GetValue(TooltipTextProperty); }
			set { SetValue(TooltipTextProperty, value); }
		}

		private TkPath _tkPath;

		public TkPath TkPath {
			get { return _tkPath; }
		}

		public ProjectTreeViewItem(TkPath path, TkView parent)
			: base(parent, false) {
			_tkPath = path;

			TooltipText = _tkPath.FilePath;
			string ext = _tkPath.FilePath.GetExtension();

			if (!String.IsNullOrEmpty(_tkPath.RelativePath)) {
				string folder = Path.GetFileName(_tkPath.RelativePath);

				if (folder.GetExtension() != null) {
					ext = _tkPath.RelativePath.GetExtension();
					TooltipText = folder;
				}
			}

			switch (ext) {
				case ".grf":
					ImagePath = ApplicationManager.PreloadResourceImage("grf-16.png");
					break;
				case ".rgz":
					ImagePath = ApplicationManager.PreloadResourceImage("rgz-16.png");
					break;
				case ".gpf":
					ImagePath = ApplicationManager.PreloadResourceImage("gpf-16.png");
					break;
				case ".syn":
					ImagePath = ApplicationManager.PreloadResourceImage("syn-16.png");
					break;
				case ".root":
					ImagePath = ApplicationManager.PreloadResourceImage("home.png");
					break;
				case ".thor":
					ImagePath = ApplicationManager.PreloadResourceImage("thor-16.png");
					break;
			}

			Style = (Style)this.TryFindResource("ProjectTreeViewItemStyle");
		}
	}
}