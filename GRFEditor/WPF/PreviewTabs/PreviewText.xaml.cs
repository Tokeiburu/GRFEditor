using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.BsonFormat;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewProperties.xaml
	/// </summary>
	public partial class PreviewText : FilePreviewTab {
		private readonly PreviewTextLoader _loader = new PreviewTextLoader();

		public PreviewText() {
			InitializeComponent();
			AvalonHelper.Load(_textEditor);
			AvalonHelper.SetupSyntaxSelection(_textEditor, _highlightingComboBox);

			Binder.Bind(_cbWordWrap, () => GrfEditorConfiguration.EnableWordWrap, delegate {
				_textEditor.WordWrap = GrfEditorConfiguration.EnableWordWrap;
			}, true);
			WpfUtilities.AddMouseInOutUnderline(_cbWordWrap);
			ErrorPanel = _errorPanel;
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			var result = _loader.Load(entry);

			if (_isCancelRequired()) return;

			_displayFile(result);
		}

		private void _displayFile(PreviewTextLoader.LoadResult result) {
			this.Dispatch(delegate {
				if (result.Syntax != null) {
					AvalonHelper.Select(result.Syntax, _highlightingComboBox);
					AvalonHelper.SetSyntax(_textEditor, result.Syntax);
				}

				_textEditor.Encoding = result.EditorEncoding;
				_textEditor.IsReadOnly = result.IsReadOnly;
				_textEditor.Text = result.TextOutput;
				_buttonSave.IsButtonEnabled = false;
				_buttonSave.Visibility = result.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
			});
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "Text file: " + entry.DisplayRelativePath;
			});
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			try {
				// CSV file content is always in UTF8. The encoded file is always ANSI.
				if (_entry.RelativePath.IsExtension(".csv")) {
					_grfData.Commands.AddFile(_entry.RelativePath, EncodingService.Ansi.GetBytes(B64.Encode(_textEditor.Text)));
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
			if (!_textEditor.IsReadOnly)
				_buttonSave.IsButtonEnabled = true;
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string ext = _entry.RelativePath.GetExtension();

				List<FileFormat> formats = new List<FileFormat> {
					FileFormat.Txt,
					FileFormat.Lua,
					FileFormat.Log,
					FileFormat.Xml,
					FileFormat.Ezv,
					FileFormat.Json,
					FileFormat.Bson,
					FileFormat.Csv,
					FileFormat.All,
				};

				string file = PathRequest.SaveFileEditor(
					"defaultExt", ".txt",
					"filter", FileFormat.MergeFilters(formats),
					"fileName", _entry.DisplayRelativePath,
					"filterIndex", _getDefaultFilterIndex(formats, ext).ToString()
				);

				if (file != null) {
					switch (file.GetExtension()) {
						case ".csv":
							// CSV file content is always in UTF8. The encoded file is always ANSI.
							File.WriteAllText(file, B64.Encode(_textEditor.Text), EncodingService.Ansi);
							break;
						case ".bson":
							Json.Text2Binary(_textEditor.Text, file);
							break;
						case ".json":
							File.WriteAllText(file, _textEditor.Text, EncodingService.Utf8);
							break;
						default:
							// CSV file content is always in UTF8. The encoded file is always ANSI.
							// If exporting a CSV content file, UTF8 must be chosen for any other text file format output.
							if (ext.IsExtension(".csv"))
								File.WriteAllText(file, _textEditor.Text, EncodingService.Utf8);
							else
								File.WriteAllText(file, _textEditor.Text, EncodingService.DisplayEncoding);
							break;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private int _getDefaultFilterIndex(List<FileFormat> formats, string ext) {
			var format = formats.FirstOrDefault(p => p.Extensions.Contains(ext));

			// The only 1-based index in Windows' API, for who knows why reason.
			if (format == null)
				return 1;
			else
				return formats.IndexOf(format) + 1;
		}
	}
}