using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRFEditor.ApplicationConfiguration;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities.Tools;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for HashDialog.xaml
	/// </summary>
	public partial class HashDialog : TkWindow {
		const int WarningFileSize = 75 * 1024 * 1024;

		public HashDialog() : base("Hash viewer", "hash.ico") {
			InitializeComponent();
		}

		private void _hashFile(string fileName, TextBox tbName, TextBox tbSize, TextBox tbCrc, TextBox tbMd5) {
			try {
				tbName.Text = fileName;
				long length = new FileInfo(fileName).Length;
				tbSize.Text = length + " B - " + length / 1024 + " KB - " + length / (1024 * 1024) + " MB";
				tbCrc.Text = "";
				tbMd5.Text = "";

				bool answer = true;

				if (length > WarningFileSize) {
					answer = ErrorHandler.YesNoRequest("Files bigger than 75 MB can take a while to process, are you sure you want to continue?", "Large file");
				}

				if (answer) {
					uint hash = Crc32.Compute(File.ReadAllBytes(fileName));
					tbCrc.Text = String.Format("{0:X8}", hash);

					using (MD5 md5 = new MD5CryptoServiceProvider()) {
						byte[] ba = md5.ComputeHash(File.ReadAllBytes(fileName));
						tbMd5.Text = ba.Select(p => p.ToString("x2")).Aggregate((a, b) => a + b);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException("Failed to hash file '" + fileName + "'.", err);
			}
		}

		private void _updateFile(int source, string fileName) {
			switch (source) {
				case 0:
					_hashFile(fileName, _tbName1, _tbSize1, _tbCrc1, _tbMd51);
					break;
				case 1:
					_hashFile(fileName, _tbName2, _tbSize2, _tbCrc2, _tbMd52);
					break;
			}
		}

		private void _buttonFile_Click(object sender, RoutedEventArgs e) => _selectFile(sender == _grid1 ? 0 : 1);
		
		private void _selectFile(int source) {
			string file = PathRequest.OpenFileEditor("filter", "Any File|*.*");

			if (file != null && File.Exists(file)) {
				_updateFile(source, file);
			}
		}

		private void _grid_DragEnter(object sender, DragEventArgs e) => e.Effects = DragDropEffects.Copy;

		private void _grid_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Is(DataFormats.FileDrop)) {
					_updateFile(sender == _grid1 ? 0 : 1, e.Get<string>(DataFormats.FileDrop));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}