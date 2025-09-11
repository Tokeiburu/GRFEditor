using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Encryption;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.FileFormats;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for EncryptorDialog.xaml
	/// </summary>
	public partial class EncryptorDialog : TkWindow {
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
			GrfToWpfBridge.Binder.Bind(_cbRenameCps, () => GrfEditorConfiguration.RenameCps, v => GrfEditorConfiguration.RenameCps = v);
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

				byte[] client = File.ReadAllBytes(_tbClientPath.Text);
				byte[] cps;

				//string debugPath = @"C:\tktoolsuite\GRF Editor - #Client# encryption cps.dll - 2022\Release\Fury.dll";
				//if (File.Exists(debugPath)) {
				//	cps = File.ReadAllBytes(debugPath);
				//}
				//else {
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GRFEditor.Files.cps.dll")) {
					if (stream == null)
						throw new Exception("Couldn't load the resource cps.dll");
				
					cps = new byte[stream.Length];
					stream.Read(cps, 0, cps.Length);
				}
				//}

				var key = _getKeyFromFile();

				// Shuffle data a bit
				var key2 = Methods.Copy(key);

				for (int i = 0; i < 250000; i++) {
					var key3 = Methods.Copy(key2);
					GRF.Core.Encryption.Encrypt(key3, key2, key2.Length);
				}

				string newCpsName = GrfEditorConfiguration.RenameCps ? _generateNameFromKey("cps", key2) + ".dll" : "cps.dll";
				var newUncompressName = _generateNameFromKey("uncompress", key2);
				newUncompressName = "g" + newUncompressName.Substring(1);

				//var newCompressName = _generateNameFromKey("compress", key2);

				_tbUncompressName.Text = newUncompressName;

				GrfEditorConfiguration.EncryptorWrapper = newCpsName;
				_outputDllPath = Path.Combine(GrfEditorConfiguration.EncryptorPath, GrfEditorConfiguration.EncryptorWrapper);

				// 
				var cpsBytes = EncodingService.Ansi.GetBytes("cps.dll\0");
				bool removeCpsBandaidModified = false;

				if (Methods.IndexOf(client, cpsBytes) == -1) {
					byte[] newClient = new byte[client.Length + 8];
					Buffer.BlockCopy(client, 0, newClient, 0, client.Length);
					Buffer.BlockCopy(cpsBytes, 0, newClient, newClient.Length - 8, 8);
					client = newClient;
					removeCpsBandaidModified = true;
				}

				byte[] newCps;
				bool success = Ee322.b3c0cf5bc709dff2229e99124d1968b7("cps.dll", _outputClientPath, key, cps, client, out newCps);

				_binaryReplace(client, "cps.dll", newCpsName);
				_binaryReplace(client, "uncompress", newUncompressName);
				_binaryReplace(newCps, "uncompress", newUncompressName);
				//_binaryReplace(client, "compress", newCompressName);
				//_binaryReplace(newCps, "compress", newCompressName);

				if (Methods.IndexOf(client, EncodingService.Ansi.GetBytes(newUncompressName)) < 0) {
					throw new Exception("Some resources weren't found in your client. This can happen if you're using a 2024 client without the cps.dll restore patch or if you are using a client that has already been modified with this encryption tool, and the key has been changed.");
				}

				if (removeCpsBandaidModified) {
					byte[] newClient = new byte[client.Length - 8];
					Buffer.BlockCopy(client, 0, newClient, 0, client.Length - 8);
					client = newClient;
				}

				if (Methods.IndexOf(client, EncodingService.Ansi.GetBytes(newCpsName)) < 0) {
					throw new Exception("Resources weren't found in your client. This can happen if you're using a 2024 client without the cps.dll restore patch or if you are using a client that has already been modified with this encryption tool, and the key has been changed.");
				}

				if (success) {
					if (!Directory.Exists(Path.GetDirectoryName(_outputDllPath))) {
						Directory.CreateDirectory(Path.GetDirectoryName(_outputDllPath));
					}
					foreach (var file in Directory.GetFiles(Path.GetDirectoryName(_outputDllPath))) {
						GrfPath.Delete(file);
					}

					File.WriteAllBytes(_outputDllPath, newCps);
					File.Delete(_outputClientPath);
					File.WriteAllBytes(_outputClientPath, client);
					OpeningService.FilesOrFolders(_outputDllPath, _outputClientPath);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private string _letters = "abcdefghijklmnopqrstuvwyxz";
		private GrfHolder _grf;

		private string _generateNameFromKey(string name, byte[] key) {
			string newName = "";

			for (int i = 0; i < name.Length; i++) {
				newName += _letters[(key[i] * name[i]) % _letters.Length];
			}

			return newName;
		}

		private int _binaryReplace(byte[] data, string needle, string insert) {
			return _binaryReplace(data, EncodingService.Ansi.GetBytes(needle), EncodingService.Ansi.GetBytes(insert));
		}

		private int _binaryReplace(byte[] data, byte[] needle, byte[] insert) {
			int index = 0;
			int count = 0;

			while ((index = Methods.IndexOf(data, needle, index + 1)) > -1) {
				if (data[index - 1] != '#') {
					Buffer.BlockCopy(insert, 0, data, index, insert.Length);
					count++;
				}
			}

			return count;
		}

		private byte[] _getKeyFromFile() {
			if (_tbEncryptionPassword.IsEnabled) {
				return Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(Ee322.fec67f91f4ef59f498874efbdd21c1c1(_password));
			}

			return Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(File.ReadAllBytes(_tbEncryptionPassword.Text));
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

		private void _buttonEncryptGrf_Click(object sender, RoutedEventArgs e) {
			try {
				var ao = EditorMainWindow.Instance._asyncOperation;
				
				if (ao.IsRunning)
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
				ao.SetAndRunOperation(new GrfThread(() => _grf.Save(Path.Combine(GrfEditorConfiguration.EncryptorPath, Path.GetFileName(_grf.FileName))), _grf, 200, null), _grfSavedFinished);
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
				var ao = EditorMainWindow.Instance._asyncOperation;

				if (ao.IsRunning)
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
				ao.SetAndRunOperation(new GrfThread(() => _grf.Save(Path.Combine(GrfEditorConfiguration.EncryptorPath, Path.GetFileName(_grf.FileName))), _grf, 200, null), _grfSavedFinished);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _tbCopyUncompress_Click(object sender, RoutedEventArgs e) {
			Clipboard.SetDataObject(_tbUncompressName.Text);

			_tbCopyUncompress.IsEnabled = false;

			GrfThread.Start(delegate {
				Thread.Sleep(500);

				_tbCopyUncompress.Dispatch(p => _tbCopyUncompress.IsEnabled = true);
			});
		}
	}
}