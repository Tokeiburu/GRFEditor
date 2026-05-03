using System;
using System.IO;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge;
using GrfToWpfBridge.PreviewTabs;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using TokeiLibrary;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewGRFProperties.xaml
	/// </summary>
	public partial class PreviewGRFProperties : FilePreviewTab {
		private FoldingManager _foldingManager;
		private AbstractFoldingStrategy _foldingStrategy = new XmlFoldingStrategy();

		public PreviewGRFProperties() {
			InitializeComponent();
			AvalonHelper.Load(_textEditor);

			_textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
			_foldingManager = FoldingManager.Install(_textEditor.TextArea);
			_foldingStrategy.UpdateFoldings(_foldingManager, _textEditor.Document);

			Binder.Bind(_cbWordWrap, () => GrfEditorConfiguration.EnableWordWrap, delegate {
				_textEditor.WordWrap = GrfEditorConfiguration.EnableWordWrap;
			}, true);
			WpfUtilities.AddMouseInOutUnderline(_cbWordWrap);
		}

		protected override void _load(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "GRF entry: " + Path.GetFileName(entry.RelativePath);
				_textEditor.Text = PreviewRawStructureLoader.GetGrfEditorHeader() + FileFormatParser.DisplayObjectPropertiesFromEntry(entry);
			});
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.SaveFileEditor(
					"defaultExt", ".txt",
					"filter", FileFormat.Txt.ToFilter(),
					"fileName", Path.GetFileNameWithoutExtension(_entry.RelativePath) + ".txt"
					);

				if (file != null) {
					File.WriteAllText(file, _textEditor.Text, EncodingService.DisplayEncoding);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}