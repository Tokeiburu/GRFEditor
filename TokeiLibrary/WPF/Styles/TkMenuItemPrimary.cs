using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TokeiLibrary.WPF.Styles {
	public class TkMenuItemPrimary : MenuItem {
		public string HeaderText {
			get { return (string)GetValue(HeaderTextProperty); }
			set { SetValue(HeaderTextProperty, value); }
		}
		public static DependencyProperty HeaderTextProperty = DependencyProperty.Register("HeaderText", typeof(string), typeof(TkMenuItemPrimary), new PropertyMetadata(new PropertyChangedCallback(OnHeaderTextChanged)));

		private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			MenuItem menuItem = d as MenuItem;

			if (menuItem != null) {
				menuItem.Header = new TextBlock {Padding = new Thickness(0, 2, 0, 0), VerticalAlignment = VerticalAlignment.Center, Text = e.NewValue.ToString()};

				try {
					Binding b = new Binding();
					b.Path = new PropertyPath(HeightProperty);
					b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(Menu) };
					menuItem.SetBinding(HeightProperty, b);
				}
				catch { }
			}
		}
	}
}
