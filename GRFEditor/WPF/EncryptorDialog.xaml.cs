using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Encryption;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.FileFormats;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.Application;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for EncryptorDialog.xaml
	/// </summary>
	public partial class EncryptorDialog : TkWindow {
		private readonly AsyncOperation _asyncOperationEncrypt;
		private readonly GrfHolder _grf;
		private readonly WpfRecentFiles _recentFiles;
		private string _outputClientPath = "";
		private string _outputDllPath = "";
		private string _password = "";

		public EncryptorDialog(GrfHolder grf) : base("GRF Encryption", "lock.ico") {
			InitializeComponent();

			_grf = grf;
			_recentFiles = new WpfRecentFiles(GrfEditorConfiguration.ConfigAsker, 6, _miLoadRecent, "Encryptor");
			_recentFiles.FileClicked += new RecentFilesManager.RFMFileClickedEventHandler(_recentFiles_FileClicked);
			_recentFiles.Reload();

			_tbClientPath.Text = GrfEditorConfiguration.EncryptorClientPath;
			_tbWrapperName.Text = GrfEditorConfiguration.EncryptorWrapper;

			_asyncOperationEncrypt = new AsyncOperation(_progressEncrypt);
			//_cbEncryptTable.Checked += delegate {
			//    _tkInfo.Visibility = Visibility.Visible;
			//};
			//_cbEncryptTable.Unchecked += delegate {
			//    _tkInfo.Visibility = Visibility.Collapsed;
			//};
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

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		//private void _buttonClientPathBrowse_Click(object sender, RoutedEventArgs e) {
		//    string file = TkPathRequest.OpenFile(
		//        new Setting(null, typeof (GrfEditorConfiguration).GetProperty("EncryptorClientPath")),
		//        "filter", FileFormat.MergeFilters(Format.Exe));

		//    if (file != null) {
		//        _tbClientPath.Text = file;
		//    }
		//}

		private void _tbWrapperName_TextChanged(object sender, TextChangedEventArgs e) {
			if (_tbWrapperName.Text.Length <= 7) {
				GrfEditorConfiguration.EncryptorWrapper = _tbWrapperName.Text;
				_outputDllPath = Path.Combine(GrfEditorConfiguration.EncryptorPath, GrfEditorConfiguration.EncryptorWrapper);
			}
		}

		private void _tbClientPath_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				GrfEditorConfiguration.EncryptorClientPath = _tbClientPath.Text;
				_outputClientPath = Path.Combine(GrfEditorConfiguration.EncryptorPath, Path.GetFileName(_tbClientPath.Text));
			}
			catch {
				_outputClientPath = Path.Combine(GrfEditorConfiguration.EncryptorPath, "MyRO.exe");
			}
		}

		private void _buttonGenerateClientConf_Click(object sender, RoutedEventArgs e) {
			try {
				if (!File.Exists(_tbClientPath.Text))
					throw new Exception("Client executable not found.");

				if (String.IsNullOrEmpty(_tbEncryptionPassword.Text)) {
					InputDialog dialog = WindowProvider.ShowWindow<InputDialog>(new InputDialog("Enter the password for the encryption.", "GRF Encryption", "", false), this);

					if (dialog.DialogResult == true) {
						_tbEncryptionPassword.Text = dialog.Input;
					}
					else {
						return;
					}
				}

				if (_tbEncryptionPassword.Text.Length < 4)
					throw new Exception("The encryption password must contain at least 4 characters.");

				//if (_tbWrapperName.Text == "cps.dll")
				//	throw new Exception("The wrapper name cannot be cps.dll.");

				byte[] client = File.ReadAllBytes(_tbClientPath.Text);
				byte[] cps;

				//if (File.Exists("cps.dll")) {
				//    cps = File.ReadAllBytes("cps.dll");
				//}
				//else {
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GRFEditor.Files.cps.dll")) {
					if (stream == null)
						throw new Exception("Couldn't load the resource cps.dll");

					cps = new byte[stream.Length];
					stream.Read(cps, 0, cps.Length);
				}
				//}

				byte[] newCps;
				bool success = Ee322.b3c0cf5bc709dff2229e99124d1968b7(_tbWrapperName.Text,
				                                                      _outputClientPath, _getKeyFromFile(), cps, client, out newCps);

				if (success) {
					if (!Directory.Exists(Path.GetDirectoryName(_outputDllPath))) {
						Directory.CreateDirectory(Path.GetDirectoryName(_outputDllPath));
					}

					File.WriteAllBytes(_outputDllPath, newCps);

					if (_tbWrapperName.Text != "cps.dll") {
						File.Delete(_outputClientPath);
						File.WriteAllBytes(_outputClientPath, client);

						OpeningService.FilesOrFolders(_outputDllPath, _outputClientPath);
					}
					else {
						OpeningService.FilesOrFolders(_outputDllPath);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private byte[] _getKeyFromFile() {
			if (_tbEncryptionPassword.IsEnabled) {
				return Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(Ee322.fec67f91f4ef59f498874efbdd21c1c1(_password));
			}

			return Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(File.ReadAllBytes(_tbEncryptionPassword.Text));
		}

		private void _buttonEncryptGrf_Click(object sender, RoutedEventArgs e) {
			try {
				if (_asyncOperationEncrypt.IsRunning)
					throw new Exception("An opration is currently running, wait for it to finish or cancel it.");

				if (_grf.IsClosed)
					throw new Exception("You must open the GRF with the editor first.");

				if (_grf.IsModified)
					throw new Exception("The GRF has been modified, you must save it first.");

				if (_grf.Header.MajorVersion < 2)
					throw new Exception("Only the GRF version 0x200 is supported for now.");

				if (String.IsNullOrEmpty(_tbEncryptionPassword.Text)) {
					InputDialog dialog = WindowProvider.ShowWindow<InputDialog>(new InputDialog("Enter the password for the encryption.", "GRF Encryption", "", false), this);

					if (dialog.DialogResult == true) {
						_tbEncryptionPassword.Text = dialog.Input;
					}
					else {
						return;
					}
				}

				if (_tbEncryptionPassword.Text.Length < 4)
					throw new Exception("The encryption password must contain at least 4 characters.");

				_grf.Header.SetEncryption(_getKeyFromFile(), _grf);
				//_grf.Header.SetFileTableEncryption(_cbEncryptTable.IsChecked == true);
				_asyncOperationEncrypt.SetAndRunOperation(new GrfThread(() => _grf.Save(Path.Combine(GrfEditorConfiguration.EncryptorPath, Path.GetFileName(_grf.FileName))), _grf, 200, null), _grfSavedFinished);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _grfSavedFinished(object state) {
			try {
				_grf.FileTable.DeleteFile(GrfStrings.EncryptionFilename);
				//_grf.Header.SetFileTableEncryption(false);
				_grf.Header.IsDecrypting = false;
				_grf.Header.IsEncrypting = false;
				OpeningService.FileOrFolder(Path.Combine(GrfEditorConfiguration.EncryptorPath, Path.GetFileName(_grf.FileName)));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonDecryptGrf_Click(object sender, RoutedEventArgs e) {
			try {
				if (_asyncOperationEncrypt.IsRunning)
					throw new Exception("An opration is currently running, wait for it to finish or cancel it.");

				if (_grf.IsClosed)
					throw new Exception("You must open the GRF with the editor first.");

				if (_grf.IsModified)
					throw new Exception("The GRF has been modified, you must save it first.");

				if (_grf.Header.MajorVersion < 2)
					throw new Exception("Only the GRF version 0x200 is supported for now.");

				if (String.IsNullOrEmpty(_tbEncryptionPassword.Text)) {
					InputDialog dialog = WindowProvider.ShowWindow<InputDialog>(new InputDialog("Enter the password for the decryption.", "GRF Encryption", "", false), this);

					if (dialog.DialogResult == true) {
						_tbEncryptionPassword.Text = dialog.Input;
					}
					else {
						return;
					}
				}

				if (_tbEncryptionPassword.Text.Length < 4)
					throw new Exception("The encryption password must contain at least 4 characters.");

				_grf.Header.SetDecryption(_getKeyFromFile(), _grf);
				_asyncOperationEncrypt.SetAndRunOperation(new GrfThread(() => _grf.Save(Path.Combine(GrfEditorConfiguration.EncryptorPath, Path.GetFileName(_grf.FileName))), _grf, 200, null), _grfSavedFinished);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miClear_Click(object sender, RoutedEventArgs e) {
			_tbEncryptionPassword.IsEnabled = true;
			_tbEncryptionPassword.Text = "";
		}

		private void _tbEncryptionPassword_TextChanged(object sender, TextChangedEventArgs e) {
			if (_tbEncryptionPassword.IsEnabled) {
				_password = _tbEncryptionPassword.Text;
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
	}
}