using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using TokeiLibrary;

namespace GRFEditor.Core.Avalon {
	/// <summary>
	/// Provides additional methods for AvalonEdit
	/// </summary>
	public static class AvalonHelper {
		public static HighlightingManager Instance = new HighlightingManager();

		static AvalonHelper() {
			IHighlightingDefinition customHighlighting;

			Assembly assembly = Assembly.Load("ICSharpCode.AvalonEdit");
			
			_loadFromICSharpCode(assembly, "C#", new[] { ".cs" }, "CSharp-Mode.xshd");
			_loadFromICSharpCode(assembly, "HTML", new[] { ".htm", ".html" }, "HTML-Mode.xshd");
			_loadFromICSharpCode(assembly, "C++", new[] { ".c", ".h", ".cc", ".cpp", ".hpp" }, "CPP-Mode.xshd");
			_loadFromICSharpCode(assembly, "Java", new[] { ".java" }, "Java-Mode.xshd");
			_loadFromICSharpCode(assembly, "XML", (".xml;.xsl;.xslt;.xsd;.manifest;.config;.addin;" +
											 ".xshd;.wxs;.wxi;.wxl;.proj;.csproj;.vbproj;.ilproj;" +
											 ".booproj;.build;.xfrm;.targets;.xaml;.xpt;" +
											 ".xft;.map;.wsdl;.disco;.ps1xml;.nuspec").Split(';'),
									 "XML-Mode.xshd");
			
			foreach (var resource in ApplicationManager.GetResources(".xshd")) {
				using (XmlReader reader = new XmlTextReader(new MemoryStream(resource))) {
					customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			
				Instance.RegisterHighlighting("Custom Highlighting", new[] { ".cool" }, customHighlighting);
			}
		}

		private static void _loadFromICSharpCode(Assembly assembly, string name, string[] extensions, string resource) {
			try {
				IHighlightingDefinition customHighlighting;

				using (XmlReader reader = new XmlTextReader(new MemoryStream(ApplicationManager.GetResource(resource, assembly)))) {
					customHighlighting = HighlightingLoader.Load(reader, Instance);
				}

				Instance.RegisterHighlighting(name, extensions, customHighlighting);
			}
			catch {
			}
		}

		public static void Load(TextEditor editor) {
			new AvalonDefaultLoading().Attach(editor);
		}

		/// <summary>
		/// Selects the specified extension in the combo box.
		/// </summary>
		/// <param name="extension">The extension.</param>
		/// <param name="box">The combo box.</param>
		public static void Select(string extension, ComboBox box) {
			box.Dispatcher.Invoke(new Action(delegate {
				try {
					var result = box.Items.Cast<object>().FirstOrDefault(p => p.ToString() == extension);
					if (result != null) {
						box.SelectedItem = result;
					}
				}
				catch {
				}
			}));
		}

		public static bool IsWordBorder(ITextSource document, int offset) {
			return TextUtilities.GetNextCaretPosition(document, offset - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder) == offset;
		}

		public static bool IsWholeWord(ITextSource document, int offsetStart, int offsetEnd) {
			int start = TextUtilities.GetNextCaretPosition(document, offsetStart - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder);

			if (start != offsetStart)
				return false;

			int end = TextUtilities.GetNextCaretPosition(document, offsetStart, LogicalDirection.Forward, CaretPositioningMode.WordBorder);

			if (end != offsetEnd)
				return false;

			return true;
		}

		public static string GetWholeWord(TextDocument document, TextEditor textEditor) {
			TextArea textArea = textEditor.TextArea;
			TextView textView = textArea.TextView;

			if (textView == null) return null;

			Point pos = textArea.TextView.GetVisualPosition(textArea.Caret.Position, VisualYPosition.LineMiddle) - textArea.TextView.ScrollOffset;
			VisualLine line = textView.GetVisualLine(textEditor.TextArea.Caret.Position.Line);

			if (line != null) {
				int visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
				int wordStartVc;

				if (line.VisualLength == visualColumn) {
					wordStartVc = line.GetNextCaretPosition(visualColumn, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}
				else {
					wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}

				if (wordStartVc == -1)
					wordStartVc = 0;

				int wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);

				if (wordEndVc == -1)
					wordEndVc = line.VisualLength;

				if (visualColumn < wordStartVc || visualColumn > wordEndVc)
					return "";

				int relOffset = line.FirstDocumentLine.Offset;
				int wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
				int wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;


				return textEditor.TextArea.Document.GetText(wordStartOffset, wordEndOffset - wordStartOffset);
			}

			return null;
		}

		public static ISegment GetWholeWordSegment(TextDocument document, TextEditor textEditor) {
			TextArea textArea = textEditor.TextArea;
			TextView textView = textArea.TextView;

			if (textView == null) return null;

			Point pos = textArea.TextView.GetVisualPosition(textArea.Caret.Position, VisualYPosition.LineMiddle) - textArea.TextView.ScrollOffset;
			VisualLine line = textView.GetVisualLine(textEditor.TextArea.Caret.Position.Line);

			if (line != null) {
				int visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
				int wordStartVc;

				if (line.VisualLength == visualColumn) {
					wordStartVc = line.GetNextCaretPosition(visualColumn, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}
				else {
					wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}

				if (wordStartVc == -1)
					wordStartVc = 0;

				int wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);

				if (wordEndVc == -1)
					wordEndVc = line.VisualLength;

				if (visualColumn < wordStartVc || visualColumn > wordEndVc)
					return new SimpleSegment();

				int relOffset = line.FirstDocumentLine.Offset;
				int wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
				int wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;


				return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
			}

			return null;
		}

		public static Dictionary<string, bool> DirtySyntaxes = new Dictionary<string, bool>();

		public static void SetSyntax(TextEditor editor, string ext) {
			bool dirty = (DirtySyntaxes.ContainsKey(ext) && DirtySyntaxes[ext] == true);
			IHighlightingDefinition customHighlighting;

			var def = HighlightingManager.Instance.GetDefinition(ext);

			if (def == null || dirty) {
				var resource = ApplicationManager.GetResource(ext + ".xshd");
				using (var s = new MemoryStream(resource)) {
					using (XmlReader reader = new XmlTextReader(s)) {
						customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
					}
				}

				if (def == null) {
					HighlightingManager.Instance.RegisterHighlighting(ext, new[] { ext }, customHighlighting);
					def = customHighlighting;
				}
				else {
					var colors_ori = customHighlighting.NamedHighlightingColors.ToList();
					var colors_new = def.NamedHighlightingColors.ToList();

					for (int index = 0; index < colors_new.Count; index++) {
						var color_new = colors_new[index];
						var color_ori = colors_ori[index];

						color_new.Foreground = color_ori.Foreground;
						color_new.FontStyle = color_ori.FontStyle;
						color_new.FontWeight = color_ori.FontWeight;
					}
				}
			}

			foreach (var color in def.NamedHighlightingColors) {
				var tb = Application.Current.TryFindResource("Avalon" + ext + color.Name) as TextBlock;

				if (tb != null) {
					color.Foreground = new SimpleHighlightingBrush(tb.Foreground as SolidColorBrush);
					color.FontStyle = tb.FontStyle;
					color.FontWeight = tb.FontWeight;
				}
			}

			editor.SyntaxHighlighting = def;
		}
	}
}