using System;
using System.Globalization;
using System.IO;
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
				_grfData.Commands.AddFile(_entry.RelativePath, EncodingService.DisplayEncoding.GetBytes(_textEditor.Text));
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
					"filter", FileFormat.MergeFilters(Format.Txt, Format.Lua, Format.Log, Format.Xml, Format.Ezv, Format.Json, Format.Bson, Format.All),
					"fileName", Path.GetFileNameWithoutExtension(_entry.RelativePath) + ext,
					"filterIndex", (ext == ".txt" ? 1 : ext == ".lua" ? 2 : ext == ".log" ? 3 : ext == ".xml" ? 4 : ext == ".ezv" ? 5 : ext == ".json" ? 6 : ext == ".bson" ? 7 : 1).ToString(CultureInfo.InvariantCulture)
					);

				if (file != null) {
					if (file.GetExtension() == ".bson") {
						Json.Text2Binary(_textEditor.Text, file);
						return;
					}

					if (file.GetExtension() == ".json") {
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
	}
}