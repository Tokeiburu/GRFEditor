using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Encryption;
using ErrorManager;
using GRF.FileFormats;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.Application;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for EncryptorInputKeyDialog.xaml
	/// </summary>
	public partial class EncryptorInputKeyDialog : TkWindow {
		private readonly WpfRecentFiles _recentFiles;

		public MessageBoxResult Result = MessageBoxResult.No;
		private string _password = "";

		public EncryptorInputKeyDialog(string message = "Information") : base("GRF Encryption", "lock.ico") {
			InitializeComponent();

			_recentFiles = new WpfRecentFiles(GrfEditorConfiguration.ConfigAsker, 6, _miLoadRecent, "Encryptor");
			_recentFiles.FileClicked += new RecentFilesManager.RFMFileClickedEventHandler(_recentFiles_FileClicked);
			_recentFiles.Reload();

			_tbInfo.Text = message;
		}

		public byte[] Key {
			get {
				if (_tbEncryptionPassword.IsEnabled) {
					return Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(Ee322.fec67f91f4ef59f498874efbdd21c1c1(_password));
				}

				return Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(File.ReadAllBytes(_tbEncryptionPassword.Text));
			}
		}

		private void _recentFiles_FileClicked(string file) {
			try {
				if (File.Exists(file)) {
					_tbEncryptionPassword.IsEnabled = false;
					_tbEncryptionPassword.Text = file;
				}
				else {
					_recentFiles.RemoveRecentFile(file);
					ErrorHandler.HandleException("File not found : " + file, ErrorLevel.Low);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _tbEncryptionPassword_TextChanged(object sender, TextChangedEventArgs e) {
			if (_tbEncryptionPassword.IsEnabled) {
				_password = _tbEncryptionPassword.Text;
			}
		}

		private void _miClear_Click(object sender, RoutedEventArgs e) {
			_tbEncryptionPassword.IsEnabled = true;
			_tbEncryptionPassword.Text = "";
		}

		private void _miSave_Click(object sender, RoutedEventArgs e) {
			if (!_tbEncryptionPassword.IsEnabled) {
				ErrorHandler.HandleException("You can only save new passwords.", ErrorLevel.NotSpecified);
				return;
			}

			string file = PathRequest.SaveFileEditor("filter", FileFormat.MergeFilters(Format.GrfKey));

			if (file != null) {
				_recentFiles.AddRecentFile(file);
				File.WriteAllBytes(file, Ee322.fec67f91f4ef59f498874efbdd21c1c1(_tbEncryptionPassword.Text));
			}
		}

		private void _miLoad_Click(object sender, RoutedEventArgs e) {
			string file = PathRequest.OpenFileEditor("filter", FileFormat.MergeFilters(Format.GrfKey));

			if (file != null) {
				_recentFiles.AddRecentFile(file);
				_tbEncryptionPassword.IsEnabled = false;
				_tbEncryptionPassword.Text = file;
			}
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Result = MessageBoxResult.Cancel;
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			Result = MessageBoxResult.OK;
			Close();
		}
	}
}