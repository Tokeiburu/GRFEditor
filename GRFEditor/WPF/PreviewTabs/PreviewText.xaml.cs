using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.BsonFormat;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using GrfToWpfBridge;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Indentation.CSharp;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewProperties.xaml
	/// </summary>
	public partial class PreviewText : FilePreviewTab {
		private FoldingManager _foldingManager;
		private AbstractFoldingStrategy _foldingStrategy;

		public PreviewText() {
			InitializeComponent();
			AvalonHelper.Load(_textEditor);
			Loaded += (e, a) => _highlightingComboBox_SelectionChanged(null, null);
			_isInvisibleResult = () => _textEditor.Dispatch(p => p.Visibility = Visibility.Hidden);
			Binder.Bind(_cbWordWrap, () => GrfEditorConfiguration.EnableWordWrap, delegate {
				_textEditor.WordWrap = GrfEditorConfiguration.EnableWordWrap;
			}, true);
			WpfUtils.AddMouseInOutEffectsBox(_cbWordWrap);
		}

		protected override void _load(FileEntry entry) {
			_labelHeader.Dispatch(p => p.Content = "Text file : " + Path.GetFileName(entry.RelativePath));
			_textEditor.Encoding = null;

			switch (entry.RelativePath.GetExtension()) {
				case ".csv":
				case ".txt":
				case ".xml":
					AvalonHelper.Select("XML", _highlightingComboBox);
					_textEditor.Dispatch(p => AvalonHelper.SetSyntax(_textEditor, "XML"));
					break;
				case ".json":
					AvalonHelper.Select("Json", _highlightingComboBox);
					_textEditor.Dispatch(p => AvalonHelper.SetSyntax(_textEditor, "Json"));
					_textEditor.Encoding = EncodingService.Utf8;
					break;
				case ".lua":
					AvalonHelper.Select("Lua", _highlightingComboBox);
					_textEditor.Dispatch(p => AvalonHelper.SetSyntax(_textEditor, "Lua"));
					break;
			}

			switch (entry.RelativePath.GetExtension()) {
				case ".integrity":
					_textEditor.Dispatch(p => p.IsReadOnly = true);
					break;
				default:
					_textEditor.Dispatch(p => p.IsReadOnly = false);
					break;
			}

			string text;

			if (entry.IsEmpty()) {
				text = "";
			}
			else if (entry.RelativePath.GetExtension() == ".json") {
				text = EncodingService.Utf8.GetString(entry.GetDecompressedData());
			}
			else if (entry.RelativePath.GetExtension() == ".csv") {
				text = _decodeB64(EncodingService.Ansi.GetString(entry.GetDecompressedData()));
			}
			else {
				text = EncodingService.DisplayEncoding.GetString(entry.GetDecompressedData());
			}

			if (_isCancelRequired()) return;

			_textEditor.Dispatch(p => p.Text = text);
			_textEditor.Dispatch(p => p.Visibility = Visibility.Visible);
			_buttonSave.Dispatch(p => p.IsButtonEnabled = false);
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			try {
				if (_entry.RelativePath.IsExtension(".csv")) {
					_grfData.Commands.AddFile(_entry.RelativePath, EncodingService.Ansi.GetBytes(_encodeB64(_textEditor.Text)));
				}
				else {
					_grfData.Commands.AddFile(_entry.RelativePath, EncodingService.DisplayEncoding.GetBytes(_textEditor.Text));
				}

				_buttonSave.IsButtonEnabled = false;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _textEditor_TextChanged(object sender, EventArgs e) {
			try {
				if (_buttonSave.IsButtonEnabled)
					return;

				string ext = _entry.RelativePath.GetExtension();

				switch(ext) {
					case ".txt":
					case ".log":
					case ".xml":
					case ".lua":
					case ".json":
					case ".ezv":
					case ".csv":
						_buttonSave.IsButtonEnabled = true;
						break;
				}
			}
			catch {
			}
		}

		private void _highlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_textEditor.SyntaxHighlighting == null) {
				_foldingStrategy = null;
			}
			else {
				switch (_textEditor.SyntaxHighlighting.Name) {
					case "XML":
						_foldingStrategy = new XmlFoldingStrategy();
						_textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
						break;
					case "C#":
					case "C++":
					case "PHP":
					case "Java":
					case "Json":
						_textEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(_textEditor.Options);
						break;
					default:
						_textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
						_foldingStrategy = null;
						break;
				}

				AvalonHelper.SetSyntax(_textEditor, _textEditor.SyntaxHighlighting.Name);
			}
			if (_foldingStrategy != null) {
				if (_foldingManager == null)
					_foldingManager = FoldingManager.Install(_textEditor.TextArea);
				_foldingStrategy.UpdateFoldings(_foldingManager, _textEditor.Document);
			}
			else {
				if (_foldingManager != null) {
					FoldingManager.Uninstall(_foldingManager);
					_foldingManager = null;
				}
			}
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string ext = _entry.RelativePath.GetExtension();

				string file = PathRequest.SaveFileEditor(
					"defaultExt", ".txt",
					"filter", FileFormat.MergeFilters(Format.Txt, Format.Lua, Format.Log, Format.Xml, Format.Ezv, Format.Json, Format.Bson, Format.Csv, Format.All),
					"fileName", Path.GetFileNameWithoutExtension(_entry.RelativePath) + ext,
					"filterIndex", (ext == ".txt" ? 1 : ext == ".lua" ? 2 : ext == ".log" ? 3 : ext == ".xml" ? 4 : ext == ".ezv" ? 5 : ext == ".json" ? 6 : ext == ".bson" ? 7 : ext == ".csv" ? 8 : 1).ToString(CultureInfo.InvariantCulture)
					);

				if (file != null) {
					if (file.GetExtension() == ".csv") {
						var text = _encodeB64(_textEditor.Text);
						File.WriteAllText(file, text, EncodingService.Ansi);
						return;
					}

					if (file.GetExtension() == ".bson") {
						Json.Text2Binary(_textEditor.Text, file);
						return;
					}

					if (file.GetExtension() == ".json") {
						File.WriteAllText(file, _textEditor.Text, EncodingService.Utf8);
						return;
					}

					if (ext.IsExtension(".csv")) {
						File.WriteAllText(file, _textEditor.Text, EncodingService.Utf8);
						return;
					}

					File.WriteAllText(file, _textEditor.Text, EncodingService.DisplayEncoding);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#region B64 Encoder/Decoder
		private string _encodeB64(string text) {
			_createB64Index();

			text = text.ReplaceAll("\r\n", "\n");
			var lines = text.Split('\n');

			StringBuilder b = new StringBuilder();

			for (int i = 0; i < lines.Length; i++) {
				var splitData = lines[i].Split('\t');

				for (int j = 0; j < splitData.Length; j++) {
					var data = splitData[j];
					var byteData = EncodingService.Utf8.GetBytes(data);
					int bitPosition = 0;
					StringBuilder b2 = new StringBuilder();
					byte c = 0;

					for (int k = 0; k < byteData.Length; k++) {
						if (bitPosition == 4) {
							b2.Append(_b64[(byte)(byteData[k] >> 6) | (c << 2)]);
							bitPosition = 6;
							c = (byte)(byteData[k] & 63);
						}
						else if (bitPosition == 2) {
							b2.Append(_b64[(byte)(byteData[k] >> 4) | (c << 4)]);
							bitPosition = 4;
							c = (byte)(byteData[k] & 15);
						}
						else {
							b2.Append(_b64[(byte)(byteData[k] >> 2)]);
							bitPosition = 2;
							c = (byte)(byteData[k] & 3);
						}

						if (bitPosition >= 6) {
							b2.Append(_b64[c]);
							bitPosition = 0;
						}
					}

					if (bitPosition > 0) {
						b2.Append(_b64[c << (bitPosition == 2 ? 4 : 2)]);
					}

					var res = b2.ToString();
					var padding = 4 - (res.Length % 4);

					b.Append(res);

					if (padding < 4) {
						for (int k = 0; k < padding; k++) {
							b.Append('=');
						}
					}

					if (j != splitData.Length - 1)
						b.Append(',');
				}

				if (i < lines.Length - 1)
					b.AppendLine();
			}

			return b.ToString();
		}

		private string _b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
		private byte[] _b642index;

		public class BitWriter : IDisposable {
			private MemoryStream _stream = new MemoryStream();
			private int _bitsLength = 0;
			private bool _disposed;
			private ushort _c = 0xffff;

			~BitWriter() {
				Dispose(true);
			}

			protected virtual void Dispose(bool disposing) {
				if (_disposed) {
					return;
				}
				if (disposing) {
					if (_stream != null)
						_stream.Dispose();
				}
				_stream = null;
				_disposed = true;
			}

			public void Dispose() {
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public void Write(byte data, byte offset, int count) {
				if (count >= 8) {
					throw new InvalidOperationException("Count cannot be great than 8 bits.");
				}

				data = (byte)((byte)(data << offset) >> offset);

				_c = (ushort)(_c << count);
				_c = (ushort)(_c | data);

				_bitsLength += count;

				if (_bitsLength >= 8) {
					_stream.Write(new byte[] { (byte)(_c >> (_bitsLength - 8)) }, 0, 1);
					_bitsLength -= 8;
				}
			}

			public void Reset() {
				_stream.Position = 0;
				_bitsLength = 0;
			}

			public string GetString() {
				var length = _stream.Position;
				byte[] data = new byte[length];
				_stream.Position = 0;
				_stream.Read(data, 0, (int)length);

				return EncodingService.Utf8.GetString(data);
			}
		}

		private string _decodeB64(string text) {
			_createB64Index();

			text = text.ReplaceAll("\r\n", "\n");
			var lines = text.Split('\n');

			StringBuilder b = new StringBuilder();

			for (int i = 0; i < lines.Length; i++) {
				BitWriter writer = new BitWriter();
				var line = lines[i];

				for (int k = 0; k < line.Length; k++) {
					switch (line[k]) {
						case ',':
							b.Append(writer.GetString());
							b.Append("\t");
							writer.Reset();
							break;
						case '=':
							break;
						default:
							writer.Write(_b642index[line[k]], 2, 6);
							break;
					}
				}

				b.Append(writer.GetString());

				if (i < lines.Length - 1)
					b.AppendLine();

				writer.Reset();
			}

			return b.ToString();
		}

		private void _createB64Index() {
			if (_b642index != null)
				return;

			_b642index = new byte[256];

			for (int i = 0; i < _b64.Length; i++) {
				_b642index[(byte)_b64[i]] = (byte)i;
			}
		}

		#endregion
	}
}