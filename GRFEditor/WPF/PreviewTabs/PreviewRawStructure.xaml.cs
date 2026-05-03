using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.BsonFormat;
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
	/// Interaction logic for PreviewRawStructure.xaml
	/// </summary>
	public partial class PreviewRawStructure : FilePreviewTab {
		private readonly PreviewRawStructureLoader _loader = new PreviewRawStructureLoader();

		public PreviewRawStructure() {
			InitializeComponent();
			AvalonHelper.Load(_textEditor);
			AvalonHelper.SetupSyntaxSelection(_textEditor, _highlightingComboBox);

			_changeRawViewButtonDisplay();
			Binder.Bind(_cbWordWrap, () => GrfEditorConfiguration.EnableWordWrap, delegate {
				_textEditor.WordWrap = GrfEditorConfiguration.EnableWordWrap;
			}, true);
			WpfUtilities.AddMouseInOutUnderline(_cbWordWrap);
			_textEditor.TextChanged += delegate {
				try {
					if (_textEditor.IsReadOnly || !_entry.RelativePath.IsExtension(".bson"))
						return;

					_buttonSave.IsButtonEnabled = true;
				}
				catch {
				}
			};
			ErrorPanel = _errorPanel;
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);
			
			var result = _loader.Load(entry);
			
			if (_isCancelRequired()) return;

			_displayFile(result);
		}

		private void _displayFile(PreviewRawStructureLoader.LoadResult result) {
			this.Dispatch(delegate {
				_textEditor.IsReadOnly = result.IsReadOnly;
				_textEditor.Visibility = result.IsTextOutput ? Visibility.Visible : Visibility.Hidden;
				_typeExplorer.Visibility = result.IsTextOutput ? Visibility.Hidden : Visibility.Visible;

				_textEditor.Encoding = result.EditorEncoding;

				if (result.IsTextOutput)
					_textEditor.Text = result.TextOutput;
				else
					this.BeginDispatch(() => _typeExplorer.LoadObject(result.ObjectOutput, new TypeExplorer.CancelTokenDelegate(() => _isCancelRequired()), 1));
					//_typeExplorer.LoadObject(result.ObjectOutput, new TypeExplorer.CancelTokenDelegate(() => _isCancelRequired()), 1);

				_buttonRawView.Visibility = result.RawViewAvailable ? Visibility.Visible : Visibility.Collapsed;

				if (result.Syntax != null) {
					AvalonHelper.Select(result.Syntax, _highlightingComboBox);
					AvalonHelper.SetSyntax(_textEditor, result.Syntax);
				}

				_buttonSave.Visibility = result.SavingEnabled ? Visibility.Visible : Visibility.Collapsed;
				_buttonSave.IsButtonEnabled = false;
			});
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "File info: " + entry.DisplayRelativePath;
			});
		}

		private void _buttonRawView_Click(object sender, RoutedEventArgs e) {
			GrfEditorConfiguration.PreviewRawFileStructure = !GrfEditorConfiguration.PreviewRawFileStructure;
			_changeRawViewButtonDisplay();

			_oldEntry = null;
			Update();
		}

		private void _changeRawViewButtonDisplay() {
			bool isRaw = GrfEditorConfiguration.PreviewRawFileStructure;

			_buttonRawView.TextHeader = isRaw ? "Tree view" : "Raw view";
			_buttonRawView.TextDescription = isRaw ? "Show the tree view" : "Show the raw text view";
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			try {
				string ext = _entry.RelativePath.GetExtension();

				if (ext == ".bson") {
					_grfData.Commands.AddFile(_entry.RelativePath, Json.Text2Binary(_textEditor.Text));
					EditorMainWindow.Instance.InvalidateVisualOnly();
					_buttonSave.IsButtonEnabled = false;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string ext = _entry.RelativePath.GetExtension();

				List<FileFormat> formats = new List<FileFormat> {
					FileFormat.Txt,
					FileFormat.Lua,
					FileFormat.Lub,
					FileFormat.Log,
					FileFormat.Xml,
					FileFormat.Ezv,
					FileFormat.Json,
					FileFormat.Bson,
					FileFormat.All,
				};

				string file = PathRequest.SaveFileEditor(
					"defaultExt", ".txt",	// Why is this here...?
					"filter", FileFormat.MergeFilters(formats),
					"fileName", _getDefaultFilename(_entry.RelativePath),
					"filterIndex", _getDefaultFilterIndex(formats, ext).ToString()
				);

				if (file != null) {
					switch (file.GetExtension()) {
						case ".bson":
							Json.Text2Binary(_textEditor.Text, file);
							break;
						case ".json":
							File.WriteAllText(file, _textEditor.Text, EncodingService.Utf8);
							break;
						default:
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

		private string _getDefaultFilename(string relativePath) {
			string ext;

			switch(relativePath.GetExtension()) {
				case ".lub":
				case ".lua":
				case ".bson":
					ext = relativePath.GetExtension();
					break;
				default:
					ext = ".txt";
					break;
			}

			return Path.GetFileNameWithoutExtension(relativePath) + ext;
		}
	}
}