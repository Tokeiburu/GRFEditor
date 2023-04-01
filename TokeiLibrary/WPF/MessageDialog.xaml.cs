using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for MessageDialog.xaml
	/// </summary>
	public partial class MessageDialog : TkWindow {
		public MessageBoxResult Result = MessageBoxResult.Cancel;
		protected TextBlock _textBoxMessageAbstract;

		public MessageDialog(string message, MessageBoxButton options, string caption = "Information", string yes = "Yes", string no = "No", string cancel = "Cancel") : 
			base(caption == "" ? "Information" : caption, "help.ico", SizeToContent.Height) {
			InitializeComponent();

			_textBoxMessage.Text = message;
			_buttonYes.Content = yes;
			_buttonNo.Content = no;
			_buttonCancel.Content = cancel;
			_textBoxMessageAbstract = _textBoxMessage;

			switch (options) {
				case MessageBoxButton.OK:
					_buttonOk.Visibility = Visibility.Visible;
					break;
				case MessageBoxButton.OKCancel:
					_buttonOk.Visibility = Visibility.Visible;
					_buttonCancel.Visibility = Visibility.Visible;
					break;
				case MessageBoxButton.YesNo:
					_buttonYes.Visibility = Visibility.Visible;
					_buttonNo.Visibility = Visibility.Visible;
					break;
				case MessageBoxButton.YesNoCancel:
					_buttonYes.Visibility = Visibility.Visible;
					_buttonNo.Visibility = Visibility.Visible;
					_buttonCancel.Visibility = Visibility.Visible;
					break;
			}
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Result = MessageBoxResult.Cancel;
			Close();
		}

		protected void _buttonYes_Click(object sender, RoutedEventArgs e) {
			Result = MessageBoxResult.Yes;
			Close();
		}

		protected void _buttonNo_Click(object sender, RoutedEventArgs e) {
			Result = MessageBoxResult.No;
			Close();
		}

		protected void _buttonOk_Click(object sender, RoutedEventArgs e) {
			Result = MessageBoxResult.OK;
			Close();
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.C && (Keyboard.Modifiers & (ModifierKeys.Control)) == ModifierKeys.Control)
				Clipboard.SetDataObject(_textBoxMessageAbstract.Text);
		}
	}
}
