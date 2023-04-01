using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using TokeiLibrary.WPF.Styles;

namespace GRFEditor.Tools.SpriteEditor {
	public partial class SpriteConverter : TkWindow {
		private void _mainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			_updatePalette();
		}

		public List<SpriteEditorTab> Tabs() {
			return _mainTabControl.Items.Cast<TabItem>().Where(p => p is SpriteEditorTab).Cast<SpriteEditorTab>().ToList();
		}
	}
}