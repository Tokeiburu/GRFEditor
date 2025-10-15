using System;
using System.IO;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRF.GrfSystem;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GRFEditor.Tools.SpriteEditor;
using TokeiLibrary;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewEditSprite.xaml
	/// </summary>
	public partial class PreviewEditSprite : FilePreviewTab {
		private readonly PreviewService _previewService;
		private SpriteEditorTab _spriteEditorTab;

		public PreviewEditSprite(PreviewService previewService) : base(true) {
			_previewService = previewService;
			InitializeComponent();

			_isInvisibleResult = () => _primary.Dispatch(p => p.Items.Clear());
		}

		protected override void _load(FileEntry entry) {
			Dispatcher.Invoke(new Action(delegate {
				try {
					if (_spriteEditorTab != null) {
						if (!_spriteEditorTab.Closing()) {
							return;
						}
					}

					_labelHeader.Text = "Edit sprite : " + Path.GetFileName(entry.RelativePath);

					if (_spriteEditorTab != null && _primary.Items.Contains(_spriteEditorTab))
						_primary.Items.Remove(_spriteEditorTab);


					_spriteEditorTab = null;

					if (_spriteEditorTab == null) {
						byte[] data = entry.GetDecompressedData();
						File.WriteAllBytes(GrfPath.Combine(GrfEditorConfiguration.TempPath, Path.GetFileName(entry.RelativePath)), data);

						if (_isCancelRequired()) return;

						_spriteEditorTab = new SpriteEditorTab(Path.GetFileName(entry.RelativePath), GrfPath.Combine(GrfEditorConfiguration.TempPath, Path.GetFileName(entry.RelativePath)), true);

						if (_isCancelRequired()) return;

						_primary.Items.Add(_spriteEditorTab);
						_primary.SelectedIndex = 0;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}));
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			try {
				_spriteEditorTab.Save();

				string baseFileName = _spriteEditorTab.OpenedSprite;
				string outputFile = TemporaryFilesManager.GetTemporaryFilePath(baseFileName + "_{0:000000}.spr");

				File.Copy(_spriteEditorTab.OpenedSprite, outputFile);
				_grfData.Commands.AddFile(_entry.RelativePath, outputFile);
				_entry = _grfData.FileTable[_entry.RelativePath];
				_previewService.InvalidateAllVisiblePreviewTabs(_grfData);
				ErrorHandler.HandleException("File successfully saved.", ErrorLevel.Low);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonSaveTo_Click(object sender, RoutedEventArgs e) {
			_spriteEditorTab.SaveAs();
		}

		private void _buttonExportAll_Click(object sender, RoutedEventArgs e) {
			_spriteEditorTab.ExportAll();
		}
	}
}