using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using TokeiLibrary.WPF.Styles;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for InputDialog.xaml
	/// </summary>
	public partial class InputDialog : TkWindow {
		private readonly bool _checkInvalidCharacters;
		public string Input;

		public InputDialog(string message, string title, string defaultValue) : this(message, title, defaultValue, false, false) {
		}

		public InputDialog(string message, string title, string def, bool checkInvalidCharacters, bool fileBrowser = false) : base(title, "refresh.ico", SizeToContent.Height) {
			InitializeComponent();

			_textBlockMessage.Text = message;
			_textBoxInput.Text = def;
			_buttonBrowse.Visibility = fileBrowser ? Visibility.Visible : Visibility.Collapsed;
			Input = def;
			_checkInvalidCharacters = checkInvalidCharacters;
			_textBoxInput.Loaded += new RoutedEventHandler(_textBoxInput_Loaded);
		}

		public TextBox TextBoxInput {
			get { return _textBoxInput; }
		}

		private void _textBoxInput_Loaded(object sender, RoutedEventArgs e) {
			_textBoxInput.SelectAll();
			_textBoxInput.Focus();
		}

		protected override void OnInitialized(EventArgs e) {
			_textBoxInput.Focus();
			_textBoxInput.SelectAll();
			_textBoxInput.Focus();
			base.OnInitialized(e);
		}

		protected void _buttonOk_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		protected override void OnClosing(CancelEventArgs e) {
			if (DialogResult == null || DialogResult == false) {
				base.OnClosing(e);
				return;
			}

			Input = _textBoxInput.Text;
			if (_checkInvalidCharacters) {
				foreach (char c in Path.GetInvalidFileNameChars()) {
					if (Input.Contains(c)) {
						_textBoxInput.Text = Input.Replace(c, '_');
						WindowProvider.ShowDialog("Invalid characters");
						DialogResult = null;
						e.Cancel = true;
						break;
					}
				}
			}

			base.OnClosing(e);
		}

		protected void _textBoxInput_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter)
				_buttonOk_Click(null, null);
		}

		private void _buttonBrowse_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.AddExtension = true;
			ofd.FileName = Path.GetFileName(_textBoxInput.Text);
			ofd.InitialDirectory = Path.GetDirectoryName(new FileInfo(_textBoxInput.Text).FullName);
			ofd.ValidateNames = true;
			ofd.CheckFileExists = true;

			if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				_textBoxInput.Text = ofd.FileName;
			}
		}
	}
}
