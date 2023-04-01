using System.Windows;
using System.Windows.Controls;

namespace TokeiLibrary.WPF.Styles {
	/// <summary>
	/// Interaction logic for Header.xaml
	/// </summary>
	public partial class Header : UserControl {
		
		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(Header), new PropertyMetadata(new PropertyChangedCallback(OnTextChanged)));

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			Header h = d as Header;

			if (h != null) {
				h._label.Content = e.NewValue;
			}
		}

		public Header() {
			InitializeComponent();
		}
	}
}
