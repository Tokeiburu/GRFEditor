using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.Core;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewImage.xaml
	/// </summary>
	public partial class PreviewPalette : FilePreviewTab {
		private readonly GrfImageWrapper _primaryImage = new GrfImageWrapper();

		public PreviewPalette() {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			VirtualFileDataObject.SetDraggable(_imagePreview, _primaryImage);
			WpfUtilities.AddFocus(_tbEase);
			ImageHelper.SetupZoomUI(_imagePreview, 6f, _gpEase, _tbEase, () => GrfEditorConfiguration.PreviewPaletteZoom, v => GrfEditorConfiguration.PreviewPaletteZoom = v);
			ErrorPanel = _errorPanel;
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			var image = _tryLoadImage(entry);
			if (image == null) return;

			if (_isCancelRequired()) return;

			_displayImage(entry, image);
		}

		private void _displayImage(FileEntry entry, GrfImage image) {
			this.Dispatch(delegate {
				_primaryImage.Image = image;
				_primaryImage.ExportFileName = Path.GetFileNameWithoutExtension(entry.RelativePath);
				_imagePreview.Source = image.Cast<BitmapSource>();
				ImageHelper.UpdateZoom(_imagePreview, GrfEditorConfiguration.PreviewPaletteZoom);
			});
		}

		private GrfImage _tryLoadImage(FileEntry entry) {
			try {
				if (entry.RelativePath.IsExtension(".spr")) {
					return new Spr(entry).Palette.Image;
				}
				else {
					GrfImage image = new GrfImage(entry);

					if (image.GrfImageType == GrfImageType.Indexed8) {
						return new Pal(image.Palette, Pal.FormatMode.NoTransparency).Image;
					}
					else {
						_labelHeader.Dispatch(p => p.Text = "Image has no palette.");
						_imagePreview.Dispatch(p => p.Source = null);
					}
				}
			}
			catch {
				_imagePreview.Dispatch(p => p.Source = null);
				throw;
			}

			return null;
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "Palette preview: " + entry.DisplayRelativePath;
			});
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
	}
}