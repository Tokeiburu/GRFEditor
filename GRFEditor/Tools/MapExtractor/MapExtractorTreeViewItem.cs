using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.WPF;
using Utilities;

namespace GRFEditor.Tools.MapExtractor {
	public class MapExtractorTreeViewItem : TkTreeViewItem {
		private TkPath _resourcePath;

		public MapExtractorTreeViewItem(TkView parent) : base(parent) {
			CanBeDragged = true;
			UseCheckBox = true;

			HorizontalContentAlignment = HorizontalAlignment.Left;
			VerticalContentAlignment = VerticalAlignment.Center;
		}

		public TkPath ResourcePath {
			get { return _resourcePath; }
			set {
				_resourcePath = value;

				if (_resourcePath != null)
					ToolTip = _resourcePath.GetFullPath();
				else {
					ToolTip = "Resource not found.";
				}
			}
		}
	}
}