using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using GrfToWpfBridge;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Indentation.CSharp;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewGRFProperties.xaml
	/// </summary>
	public partial class PreviewGRFProperties : FilePreviewTab {
		private readonly string[] _advancedView = new string[] { "Tree view", "Show the tree view" };
		private readonly string[] _rawView = new string[] { "Raw view", "Show the raw text view" };
		private FoldingManager _foldingManager;
		private AbstractFoldingStrategy _foldingStrategy;

		public PreviewGRFProperties() : base(true) {
			InitializeComponent();
			AvalonHelper.Load(_textEditor);

			_highlightingComboBox_SelectionChanged(null, null);

			_isInvisibleResult = delegate {
				_textEditor.Dispatch(p => p.Visibility = Visibility.Hidden);
				_typeExplorer.Dispatch(p => p.Visibility = Visibility.Hidden);
			};

			_changeRawViewButton();
			Binder.Bind(_cbWordWrap, () => GrfEditorConfiguration.EnableWordWrap, delegate {
				_textEditor.WordWrap = GrfEditorConfiguration.EnableWordWrap;
			}, true);
			WpfUtils.AddMouseInOutEffectsBox(_cbWordWrap);
		}

		protected override void _load(FileEntry entry) {
			_changeRawViewButton();

			FileEntry entryClosure = entry;
			_labelHeader.Dispatch(p => p.Text = "GRF entry : " + Path.GetFileName(entryClosure.RelativePath));
			_buttonRawView.Dispatch(p => p.Visibility = System.Windows.Visibility.Collapsed);

			//if (GrfEditorConfiguration.PreviewRawGrfProperties) {
				string text = PreviewRawStructure.GetGrfEditorHeader() + FileFormatParser.DisplayObjectPropertiesFromEntry(_grfData, entry);

				if (_isCancelRequired()) return;

				_textEditor.Dispatch(p => p.IsReadOnly = true);
				_textEditor.Dispatch(p => p.Text = text);
				_textEditor.Dispatch(p => p.Visibility = Visibility.Visible);
				_typeExplorer.Dispatch(p => p.Visibility = Visibility.Hidden);
			//}
			//else {
			//	_typeExplorer.Dispatch(p => p.LoadObject(
			//		entry,
			//		new TypeExplorer.CancelLoadingDelegate(() => _isCancelRequired()),
			//		3));
			//	_textEditor.Dispatch(p => p.Visibility = Visibility.Hidden);
			//	_typeExplorer.Dispatch(p => p.Visibility = Visibility.Visible);
			//}
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

		private void _buttonRawView_Click(object sender, RoutedEventArgs e) {
			GrfEditorConfiguration.PreviewRawGrfProperties = !GrfEditorConfiguration.PreviewRawGrfProperties;
			_changeRawViewButton();
			Update(true);
		}

		private void _changeRawViewButton() {
			if (GrfEditorConfiguration.PreviewRawGrfProperties) {
				_buttonRawView.Dispatch(p => p.TextHeader = _advancedView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _advancedView[1]);
			}
			else {
				_buttonRawView.Dispatch(p => p.TextHeader = _rawView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _rawView[1]);
			}
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.SaveFileEditor(
					"defaultExt", ".txt",
					"filter", FileFormat.MergeFilters(Format.Txt | Format.Lua | Format.Log | Format.Xml | Format.Ezv | Format.All),
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