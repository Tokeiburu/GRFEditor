using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.BsonFormat;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.ImfFormat;
using GRF.FileFormats.LubFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.StrFormat;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using GrfToWpfBridge;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Indentation.CSharp;
using Lua;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewRawStructure.xaml
	/// </summary>
	public partial class PreviewRawStructure : FilePreviewTab {
		private readonly string[] _advancedView = new string[] { "Tree view", "Show the tree view" };
		private readonly string[] _rawView = new string[] { "Raw view", "Show the raw text view" };
		private FoldingManager _foldingManager;
		private AbstractFoldingStrategy _foldingStrategy;

		private bool _isLua;
		private bool _isBson;

		public PreviewRawStructure() : base(true) {
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
			_textEditor.TextChanged += delegate {
				try {
					if (_textEditor.IsReadOnly)
						return;

					string ext = _entry.RelativePath.GetExtension();

					if (ext == ".bson") {
						_buttonSave.IsButtonEnabled = true;
					}
				}
				catch {
				}
			};
		}

		protected override void _load(FileEntry entry) {
			_isLua = false;
			_isBson = false;
			_labelHeader.Dispatch(p => p.Content = "File info : " + Path.GetFileName(entry.RelativePath));
			_textEditor.Encoding = null;
			_buttonSave.Dispatch(delegate {
				_textEditor.IsReadOnly = true;
				_buttonSave.Visibility = Visibility.Collapsed;
				_buttonSave.IsButtonEnabled = false;
			});

			string text;

			if (entry.Removed) {
				text = "\"" + entry.RelativePath + "\" is a special GRF entry used for removing files with Thor patches.";

				_showTextEditor(text);
				_buttonRawView.Dispatch(p => p.Visibility = Visibility.Collapsed);
			}
			else if (entry.IsEmpty()) {
				text = "\"" + entry.RelativePath + "\" is empty (0 byte).";

				_showTextEditor(text);
				_buttonRawView.Dispatch(p => p.Visibility = Visibility.Collapsed);
			}
			else {
				try {
					switch(entry.RelativePath.GetExtension()) {
						case ".gat":
							Gat gat = new Gat(entry.GetDecompressedData());

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(gat.GetInformation());
							}
							else {
								_showTypeExplorer(gat);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".gnd":
							Gnd gnd = new Gnd(entry.GetDecompressedData());

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(gnd.GetInformation());
							}
							else {
								_showTypeExplorer(gnd);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".rsm":
						case ".rsm2":
							Rsm rsm = new Rsm(entry.GetDecompressedData());

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(rsm.GetInformation());
							}
							else {
								_showTypeExplorer(rsm);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".rsw":
							Rsw rsw = new Rsw(entry.GetDecompressedData());

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(rsw.GetInformation());
							}
							else {
								_showTypeExplorer(rsw);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".str":
							Str str = new Str(entry.GetDecompressedData());

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(str.GetInformation());
							}
							else {
								_showTypeExplorer(str);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".fna":
							Fna fna = new Fna(entry.GetDecompressedData());

							//if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(fna.GetInformation());
							//}
							//else {
							//	_showTypeExplorer(fna);
							//}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Collapsed);
							break;
						case ".imf":
							Imf imf = new Imf(entry.GetDecompressedData());

							//if (GrfEditorConfiguration.PreviewRawFileStructure) {
								AvalonHelper.Select("Imf", _highlightingComboBox);
								_textEditor.Dispatch(p => AvalonHelper.SetSyntax(_textEditor, "Imf"));
								_showTextEditor(imf.GetInformation());
							//}
							//else {
							//	_showTypeExplorer(imf);
							//}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Collapsed);
							break;
						case ".act":
							string sprName = GrfPath.Combine(Path.GetDirectoryName(entry.RelativePath), Path.GetFileNameWithoutExtension(entry.RelativePath) + ".spr");
							Act act;

							if (_grfData.FileTable.ContainsFile(sprName)) {
								Spr sprT = new Spr(_grfData.FileTable[sprName].GetDecompressedData());
								act = new Act(entry.GetDecompressedData(), sprT);
							}
							else {
#pragma warning disable 612,618
								act = new Act(entry.GetDecompressedData());
#pragma warning restore 612,618
							}

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(FileFormatParser.DisplayObjectProperties(act));
							}
							else {
								_showTypeExplorer(act);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".spr":
							Spr spr = new Spr(entry.GetDecompressedData());

							if (GrfEditorConfiguration.PreviewRawFileStructure) {
								_showTextEditor(FileFormatParser.DisplayObjectProperties(spr));
							}
							else {
								_showTypeExplorer(spr);
							}
							_buttonRawView.Dispatch(p => p.Visibility = Visibility.Visible);
							break;
						case ".lub":
							try {
								_isLua = true;
								AvalonHelper.Select("Lua", _highlightingComboBox);
								_textEditor.Dispatch(p => AvalonHelper.SetSyntax(_textEditor, "Lua"));

								byte[] data = entry.GetDecompressedData();

								if (Methods.ByteArrayCompare(data, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0)) {
									Lub lub = new Lub(entry.GetDecompressedData());

									//if (GrfEditorConfiguration.PreviewRawFileStructure) {
										text = lub.Decompile();
										text = "-- Using GRF Editor Decompiler (beta 1.1.0)\r\n\r\n" + text;
										_showTextEditor(text);
									//}
									//else {
									//	_showTypeExplorer(new Lub(data));
									//}
								}
								else {
									text = EncodingService.DisplayEncoding.GetString(data);
									_showTextEditor(text);
								}
								_buttonRawView.Dispatch(p => p.Visibility = Visibility.Collapsed);
							}
							catch (Exception err) {
								text = "-- An unhandled exception has been caught : " + err.Message;
								_showTextEditor(text);
							}
							break;
						case ".bson":
							try {
								_isBson = true;
								_buttonSave.Dispatch(delegate {
									_buttonSave.IsButtonEnabled = false;
									_buttonSave.Visibility = Visibility.Visible;
								});
								BsonList bson = Bson.Parse(entry.GetDecompressedData());
								AvalonHelper.Select("Json", _highlightingComboBox);
								_textEditor.Dispatch(p => AvalonHelper.SetSyntax(_textEditor, "Json"));

								//if (GrfEditorConfiguration.PreviewRawFileStructure) {
									_textEditor.Encoding = EncodingService.Utf8;
									text = Bson.Bson2Json(entry.GetDecompressedData());
									_showTextEditor(text);

									_buttonSave.Dispatch(delegate {
										_textEditor.IsReadOnly = false;
									});
								//}
								//else {
								//	_showTypeExplorer(bson);
								//}
							}
							catch (Exception err) {
								text = "-- An unhandled exception has been caught : " + err.Message;
								_showTextEditor(text);
							}
							finally {
								_buttonRawView.Dispatch(p => p.Visibility = Visibility.Collapsed);
							}
							break;
					}
				}
				catch (GrfException grfErr) {
					if (grfErr == GrfExceptions.__CorruptedOrEncryptedEntry) {
						text = "-- An unhandled exception has been caught : " + grfErr.Message;
						_showTextEditor(text);
					}
					else if (grfErr == GrfExceptions.__ContainerBusy) {
					}
					else {
						throw;
					}
				}
			}
		}

		private string _formatText(string text) {
			List<string> lines = text.Replace("\r\n", "\r").Split('\r').ToList();

			for (int i = 0; i < lines.Count; i++) {
				string original = lines[i];

				while (lines[i].TrimStart('\t').StartsWith("  ") && original != (lines[i] = lines[i].ReplaceOnce("  ", "\t"))) {
					original = lines[i];
				}
			}

			text = Methods.Aggregate(lines, "\r\n");

			using (MemoryStream output = new MemoryStream())
			using (LuaWriter writer = new LuaWriter(output))
			using (LuaReader reader = new LuaReader(new MemoryStream(EncodingService.Ansi.GetBytes(text)))) {
				writer.Write(reader.ReadAll());
				byte[] dataStream = new byte[output.Length];
				output.Seek(0, SeekOrigin.Begin);
				output.Read(dataStream, 0, dataStream.Length);
				return EncodingService.DisplayEncoding.GetString(dataStream);
			}
		}

		private void _showTextEditor(string text) {
			if (_isCancelRequired()) return;

			_textEditor.Dispatch(p => p.IsReadOnly = true);
			_textEditor.Dispatch(p => p.Text = (_isBson ? "" : (_isLua ? GetGrfEditorHeaderForLua() : GetGrfEditorHeader())) + text);
			_textEditor.Dispatch(p => p.Visibility = Visibility.Visible);
			_typeExplorer.Dispatch(p => p.Visibility = Visibility.Hidden);
		}

		private void _showTypeExplorer(object item) {
			_typeExplorer.Dispatch(p => p.LoadObject(item, new TypeExplorer.CancelLoadingDelegate(() => _isCancelRequired()), 1));
			_textEditor.Dispatch(p => p.Visibility = Visibility.Hidden);
			_typeExplorer.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		public static string GetGrfEditorHeaderForLua() {
			if (!GrfEditorConfiguration.ShowGrfEditorHeader)
				return "";

			return "--[[\n" +
			       "    GRF Editor [Version - " + GrfEditorConfiguration.PublicVersion + "]  [Build - " + GrfEditorConfiguration.RealVersion + "]\n" +
			       "  \n" +
			       "    This file was generated by GRF Editor\n" +
			       "--______________________________________________________]]\n\n";
		}

		public static string GetGrfEditorHeader() {
			if (!GrfEditorConfiguration.ShowGrfEditorHeader)
				return "";

			return "//\n" +
			       "//  GRF Editor [Version - " + GrfEditorConfiguration.PublicVersion + "]  [Build - " + GrfEditorConfiguration.RealVersion + "]\n" +
			       "//\n" +
			       "//  This file was generated by GRF Editor\n" +
			       "//______________________________________________________\n\n";
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
			GrfEditorConfiguration.PreviewRawFileStructure = !GrfEditorConfiguration.PreviewRawFileStructure;
			_changeRawViewButton();

			_oldEntry = null;
			Update();
		}

		private void _changeRawViewButton() {
			if (GrfEditorConfiguration.PreviewRawFileStructure) {
				_buttonRawView.Dispatch(p => p.TextHeader = _advancedView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _advancedView[1]);
			}
			else {
				_buttonRawView.Dispatch(p => p.TextHeader = _rawView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _rawView[1]);
			}
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			try {
				string ext = _entry.RelativePath.GetExtension();

				if (ext == ".bson") {
					try {
						_grfData.Commands.AddFile(_entry.RelativePath, Json.Text2Binary(_textEditor.Text));
						EditorMainWindow.Instance.InvalidateVisualOnly();
						_buttonSave.IsButtonEnabled = false;
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string ext = _entry.RelativePath.GetExtension();

				string file = PathRequest.SaveFileEditor(
					"defaultExt", ".txt",
					"filter", FileFormat.MergeFilters(Format.Txt, Format.Lua, Format.Lub, Format.Log, Format.Xml, Format.Ezv, Format.Json, Format.Bson, Format.All),
					"fileName", Path.GetFileNameWithoutExtension(_entry.RelativePath) + (ext == ".lub" ? ".lub" : ext == ".lua" ? ".lua" : ext == ".bson" ? ".bson" : ".txt"),
					"filterIndex", (ext == ".txt" ? 1 : ext == ".lua" ? 2 : ext == ".lub" ? 3 : ext == ".log" ? 4 : ext == ".xml" ? 5 : ext == ".ezv" ? 6 : ext == ".json" ? 7 : ext == ".bson" ? 8 : 1).ToString(CultureInfo.InvariantCulture)
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