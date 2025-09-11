using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewImageItem.xaml
	/// </summary>
	public partial class PreviewImageItem : UserControl {
		public int PreviewIndex { get; set; }

		public PreviewImageItem() {
			InitializeComponent();

			this.MouseEnter += new MouseEventHandler(_previewItem_MouseEnter);
			this.MouseLeave += new MouseEventHandler(_previewItem_MouseLeave);
		}

		private void _previewItem_MouseLeave(object sender, MouseEventArgs e) {
			_borderOverlay.Visibility = Visibility.Hidden;
		}

		private void _previewItem_MouseEnter(object sender, MouseEventArgs e) {
			if (_image.Source == null)
				return;

			_borderOverlay.Visibility = Visibility.Visible;
		}
	}
}
