using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Be.Windows.Forms;
using ErrorManager;
using GRF.Core;
using TokeiLibrary;
using Utilities.Extension;
using Utilities.Services;
using Color = System.Drawing.Color;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewResoure.xaml
	/// </summary>
	public partial class PreviewResource : FilePreviewTab {
		public PreviewResource() {
			InitializeComponent();

			_isInvisibleResult = () => _hexEditorHost.Dispatch(p => p.Visibility = Visibility.Hidden);

			try {
				var color = ((SolidColorBrush)Application.Current.Resources["TabItemBackground"]).Color;
				_hexEditor.BackColor = Color.FromArgb(color.A, color.R, color.G, color.B);
			}
			catch {
			}
		}

		protected override void _load(FileEntry entry) {
			_labelHeader.Dispatch(p => p.Content = "Resource : " + Path.GetFileName(entry.RelativePath));

			byte[] data;
			bool? raw = entry.Flags.HasFlags(EntryType.GrfEditorCrypted);

			if (raw == true) {
				data = entry.GetCompressedData();
			}
			else {
				try {
					data = entry.GetDecompressedData();
				}
				catch {
					data = EncodingService.Ansi.GetBytes("GRF Editor - Broken file!");
				}
			}

			MemoryStream ms = new MemoryStream(data);
			DynamicFileByteProvider prov = new DynamicFileByteProvider(ms);
			_hexEditorHost.Dispatch(p => _hexEditor.ByteProvider = prov);
			_hexEditorHost.Dispatch(p => _hexEditor.Visible = true);
			_hexEditorHost.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _hexEditor_Copied(object sender, EventArgs e) {
			_hexEditor.CopyHex();
		}
	}
}