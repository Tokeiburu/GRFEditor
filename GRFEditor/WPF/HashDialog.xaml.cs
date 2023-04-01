using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
		public HashDialog() : base("Hash viewer", "hash.ico") {
			InitializeComponent();
		}

		private void _buttonFile1_Click(object sender, RoutedEventArgs e) {
			string file = PathRequest.OpenFileEditor("filter", "Any File|*.*");

			if (file != null && File.Exists(file)) {
				_updateFile1(file);
			}
		}

		private void _updateFile1(string fileName) {
			_updateFile(fileName, _tbName1, _tbSize1, _tbCrc1, _tbMd51);
		}

		private void _updateFile2(string fileName) {
			_updateFile(fileName, _tbName2, _tbSize2, _tbCrc2, _tbMd52);
		}

		private void _updateFile(string fileName, TextBox tbName, TextBox tbSize, TextBox tbCrc, TextBox tbMd5) {
			try {
				tbName.Text = fileName;
				long length = new FileInfo(fileName).Length;
				tbSize.Text = length + " B - " + length / 1024 + " KB - " + length / (1024 * 1024) + " MB";
				tbCrc.Text = "";
				tbMd5.Text = "";

				bool answer = true;
				if (length > 75 * 1024 * 1024) {
					answer = ErrorHandler.YesNoRequest("Files bigger than 75 MB can take a while to process, are you sure you want to continue?", "Large file");
				}

				if (answer) {
					uint hash = Crc32.Compute(File.ReadAllBytes(fileName));
					tbCrc.Text = String.Format("{0:X8}", hash);

					using (MD5 md5 = new MD5CryptoServiceProvider()) {
						byte[] ba = md5.ComputeHash(File.ReadAllBytes(fileName));
						StringBuilder sb = new StringBuilder(ba.Length * 2);

						foreach (byte b in ba) {
							sb.AppendFormat("{0:x2}", b);
						}

						tbMd5.Text = sb.ToString();
					}
				}
			}
			catch {
			}
		}

		private void _buttonFile2_Click(object sender, RoutedEventArgs e) {
			string file = PathRequest.OpenFileEditor("filter", "Any File|*.*");

			if (file != null && File.Exists(file)) {
				_updateFile2(file);
			}
		}

		private void _grid1_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _grid1_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Is(DataFormats.FileDrop)) {
					_updateFile1(e.Get<string>(DataFormats.FileDrop));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _grid2_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _grid2_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Is(DataFormats.FileDrop)) {
					_updateFile2(e.Get<string>(DataFormats.FileDrop));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}