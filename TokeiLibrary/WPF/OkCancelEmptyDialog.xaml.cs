using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for OkCancelEmptyDialog.xaml
	/// </summary>
	public partial class OkCancelEmptyDialog : TkWindow {
		public ContentControl ContentControl {
			get {
				return _content;
			}
		}

		public Grid GridActionPresenter {
			get { return _gridActionPresenter; }
		}

		public OkCancelEmptyDialog(string title, string ico) : base(title, ico) {
			InitializeComponent();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			this.DialogResult = true;
			this.Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}
