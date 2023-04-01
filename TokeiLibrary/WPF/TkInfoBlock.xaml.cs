using System.Windows;
using System.Windows.Controls;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for TkInfoBlock.xaml
	/// </summary>
	public partial class TkInfoBlock : UserControl {
		public string ImagePath {
			get { return (string)GetValue(ImagePathProperty); }
			set { SetValue(ImagePathProperty, value); }
		}

		public static DependencyProperty ImagePathProperty = DependencyProperty.Register("ImagePath", typeof(string), typeof(TkInfoBlock), new PropertyMetadata(new PropertyChangedCallback(OnImagePathChanged)));

		private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkInfoBlock block = d as TkInfoBlock;

			if (block != null) {
				block._image.Source = ApplicationManager.GetResourceImage(e.NewValue.ToString());
			}
		}

		public string Text {
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TkInfoBlock), new PropertyMetadata(new PropertyChangedCallback(OnTextChanged)));

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkInfoBlock block = d as TkInfoBlock;

			if (block != null) {
				block._tbText.Text = e.NewValue.ToString();
				block.ToolTip = e.NewValue.ToString();
			}
		}

		public TkInfoBlock() {
			InitializeComponent();
		}
	}
}
