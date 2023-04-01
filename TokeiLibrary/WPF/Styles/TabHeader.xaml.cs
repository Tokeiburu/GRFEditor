using System.Windows;
using System.Windows.Controls;

namespace TokeiLibrary.WPF.Styles {
	/// <summary>
	/// Interaction logic for TabHeader.xaml
	/// </summary>
	public partial class TabHeader : UserControl {
		
		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TabHeader), new PropertyMetadata(new PropertyChangedCallback(OnTextChanged)));

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var th = d as TabHeader;

			if (th != null) {
				th.Dispatch(p => p._header.Text = e.NewValue.ToString());
			}
		}

		public TabHeader() {
			InitializeComponent();
		}
	}
}
