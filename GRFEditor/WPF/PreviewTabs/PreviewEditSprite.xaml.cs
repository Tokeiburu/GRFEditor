using System;
using System.IO;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRFEditor.Core.Services;
using TokeiLibrary;
using GrfToWpfBridge.PreviewTabs;
using GRF.FileFormats.SprFormat;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewEditSprite.xaml
	/// </summary>
	public partial class PreviewEditSprite : FilePreviewTab {
		private readonly PreviewService _previewService;
		private string _currentlyLoadedSpr;

		public PreviewEditSprite(PreviewService previewService) : base(true) {
			_previewService = previewService;
			InitializeComponent();
			ErrorPanel = _errorPanel;
		}

		protected override void _load(FileEntry entry) {
			if (entry.RelativePath == _currentlyLoadedSpr)
				return;

			bool closed = _closeCurrentTab();

			if (!closed)
				return;

			_setupUI(entry);

			var spriteData = entry.GetDecompressedData();

			_loadSprite(entry, new Spr(spriteData));
		}

		private void _loadSprite(FileEntry entry, Spr spr) {
			this.Dispatch(delegate {
				try {
					if (_isCancelRequired()) return;

					_spriteEditorControl.Load(spr, entry.RelativePath);
					_currentlyLoadedSpr = entry.RelativePath;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "Edit sprite: " + entry.DisplayRelativePath;
			});
		}

		private bool _closeCurrentTab() {
			return this.Dispatch(() => _spriteEditorControl.Close());
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			try {
				byte[] data;

				using (MemoryStream stream = new MemoryStream()) {
					_spriteEditorControl.Spr.Save(stream);
					_spriteEditorControl.SaveCommandIndex();	// Marks the command stack as being not modified
					data = stream.ToArray();
				}

				_grfData.Commands.AddFile(_entry.RelativePath, data);

				// Ensure the other tabs use the updated SPR entry
				_previewService.InvalidateAllVisiblePreviewTabs(_grfData);
				ErrorHandler.HandleException("File successfully saved.", ErrorLevel.Low);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonSaveTo_Click(object sender, RoutedEventArgs e) => _spriteEditorControl.SaveAs();
		private void _buttonExportAll_Click(object sender, RoutedEventArgs e) => _spriteEditorControl.ExportAll();
	}
}