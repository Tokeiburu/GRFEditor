using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace TokeiLibrary.WPF.Styles {
	public class ButtonMenu : Button {
		public ButtonMenu() {
			this.Loaded += delegate {
				if (ContextMenu != null) {
					ContextMenu.Placement = PlacementMode.Bottom;
					ContextMenu.PlacementTarget = this;
					PreviewMouseRightButtonUp += new MouseButtonEventHandler(_buttonMenu_PreviewMouseRightButtonUp);
				}
			};
		}

		private void _buttonMenu_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
		}

		protected override void OnClick() {
			ContextMenu.IsOpen = true;
		}
	}
}
