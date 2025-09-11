using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TokeiLibrary.WPF;
using Utilities;

namespace GrfToWpfBridge.TreeViewManager {
	public class MapExtractorTreeViewItem : TkTreeViewItem {
		private TkPath _resourcePath;
		public string RelativeGrfPath { get; set; }

		public MapExtractorTreeViewItem(TkView parent) : base(parent, false) {
			CanBeDragged = true;
			UseCheckBox = true;
			CheckBoxHeaderIsEnabled = true;

			Style = (Style)FindResource("MapExtractorTreeViewItemStyle");
		}

		public TkPath ResourcePath {
			get { return _resourcePath; }
			set {
				_resourcePath = value;

				if (_resourcePath != null) {
					if (_resourcePath.RelativePath == null)
						ToolTip = "File: " + _resourcePath.FilePath;
					else
						ToolTip = "GRF: " + _resourcePath.FilePath + (String.IsNullOrEmpty(_resourcePath.RelativePath) ? "" : "\r\n" + _resourcePath.RelativePath);
				}
				else {
					ToolTip = "Resource not found.";
				}
			}
		}
	}
}
