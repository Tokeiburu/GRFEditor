using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Be.Windows.Forms;
using ErrorManager;
using GRF;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Color = System.Drawing.Color;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for AddFile.xaml
	/// </summary>
	public partial class MagicEditDialog : TkWindow {
		public MagicEditDialog(string header) : base("Edit magic header", "refresh.ico") {
			OutputHeader = header;
			InitializeComponent();

			byte[] data = EncodingService.Ansi.GetBytes(header);
			byte[] data2 = new byte[16];

			Buffer.BlockCopy(data, 0, data2, 0, Math.Min(16, data.Length));
			MemoryStream ms = new MemoryStream(data2);
			DynamicFileByteProvider prov = new DynamicFileByteProvider(ms);
			_hexEditor.ByteProvider = prov;
			_hexEditor.CurrentLineChanged += new EventHandler(_hexEditor_CurrentLineChanged);
			_hexEditor.SelectionStartChanged += new EventHandler(_hexEditor_SelectionStartChanged);

			try {
				var color = ((SolidColorBrush)Application.Current.Resources["TabItemBackground"]).Color;
				_hexEditor.BackColor = Color.FromArgb(color.A, color.R, color.G, color.B);
				_hexEditor.Font = new Font("Consolas", 10f, System.Drawing.FontStyle.Regular, GraphicsUnit.Point, 0);
			}
			catch {
			}
		}

		public string OutputHeader { get; set; }

		protected void _buttonOK_Click(object sender, RoutedEventArgs e) {
			byte[] data = new byte[16];

			for (int i = 0; i < _hexEditor.ByteProvider.Length && i < 16; i++) {
				data[i] = _hexEditor.ByteProvider.ReadByte(i);
			}

			OutputHeader = EncodingService.Ansi.GetString(data);
			DialogResult = true;
			Close();
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _hexEditor_Copied(object sender, EventArgs e) {
			_hexEditor.CopyHex();
		}

		private void _hexEditor_CurrentLineChanged(object sender, EventArgs e) {
			
		}

		private void _hexEditor_SelectionStartChanged(object sender, EventArgs e) {
			if (_hexEditor.SelectionStart >= 16)
				_hexEditor.SelectionStart = 0;
		}
	}
}