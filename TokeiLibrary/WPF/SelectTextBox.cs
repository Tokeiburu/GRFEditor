using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TokeiLibrary.WPF {
	public class SelectTextBox : TextBox {
		public SelectTextBox() {
			AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(_selectivelyIgnoreMouseButton), true);
			AddHandler(GotKeyboardFocusEvent, new RoutedEventHandler(_selectAllText), true);
			AddHandler(MouseDoubleClickEvent, new RoutedEventHandler(_selectAllText), true);
		}

		protected override void OnDragEnter(DragEventArgs e) {
			if (e.Data.GetData("ImageIndex") != null) {
				e.Effects = DragDropEffects.All;
				return;
			}

			base.OnDragEnter(e);
		}

		private static void _selectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e) {
			// Find the TextBox
			DependencyObject parent = e.OriginalSource as UIElement;
			while (parent != null && !(parent is TextBox))
				parent = VisualTreeHelper.GetParent(parent);

			if (parent != null) {
				var textBox = (TextBox) parent;
				if (!textBox.IsKeyboardFocusWithin) {
					// If the text box is not yet focussed, give it the focus and
					// stop further processing of this click event.
					textBox.Focus();
					e.Handled = true;
				}
			}
		}

		private static void _selectAllText(object sender, RoutedEventArgs e) {
			var textBox = e.OriginalSource as TextBox;
			if (textBox != null)
				textBox.SelectAll();
		}
	}
}