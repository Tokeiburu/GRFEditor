using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRF.Image.Decoders;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.Tools.SpriteEditor;
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using TokeiLibrary.Paths;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewImage.xaml
	/// </summary>
	public partial class PreviewImage : FilePreviewTab {
		private readonly GrfImageWrapper _primaryImage = new GrfImageWrapper();
		private readonly GrfImageWrapper _spriteImage = new GrfImageWrapper();
		private string _sprFilePath;
		private string _sprExportPath;
		private Spr _spr;

		public PreviewImage() {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			VirtualFileDataObject.SetDraggable(_imagePreview, _primaryImage);
			VirtualFileDataObject.SetDraggable(_imagePreviewSprite, _spriteImage);
			WpfUtilities.AddFocus(_tbEase);
			ImageHelper.SetupZoomUI(_imagePreview, 6f, _gpEase, _tbEase, () => GrfEditorConfiguration.PreviewImageZoom, v => GrfEditorConfiguration.PreviewImageZoom = v);
			ErrorPanel = _errorPanel;
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			var loadData = _tryLoadImage(entry);
			if (loadData.Image == null) return;

			_displayImage(entry, loadData);
			_updateSpritePreview(entry);
		}

		private void _updateSpritePreview(FileEntry entry) {
			this.Dispatch(delegate {
				if (entry.RelativePath.GetExtension() == ".pal") {
					_buttonSelectSprite.Visibility = Visibility.Visible;
					_imagePreviewSprite.Visibility = Visibility.Visible;
					_loadSpr(entry);
				}
				else {
					_buttonSelectSprite.Visibility = Visibility.Collapsed;
					_imagePreviewSprite.Visibility = Visibility.Collapsed;
				}
			});
		}

		private void _displayImage(FileEntry entry, (GrfImage Image, string DisplayType) loadData) {
			this.Dispatch(delegate {
				_primaryImage.Image = loadData.Image;
				_primaryImage.ExportFileName = Path.GetFileNameWithoutExtension(entry.RelativePath);
				_imagePreview.Source = loadData.Image.Cast<BitmapSource>();
				ImageHelper.UpdateZoom(_imagePreview, GrfEditorConfiguration.PreviewImageZoom);
				
				_tbFileInfo.Text =
					$"Type: {entry.FileType}\r\n" +
					$"Format: {loadData.DisplayType ?? loadData.Image.GrfImageType.ToString()}\r\n" +
					$"Size: {_primaryImage.Image.Width}x{_primaryImage.Image.Height}";
			});
		}

		private (GrfImage Image, string DisplayType) _tryLoadImage(FileEntry entry) {
			try {
				if (entry.RelativePath.IsExtension(".gif"))
					return (GifFormat.LoadAsGrfImage(entry), null);

				var data = entry.GetDecompressedData();
				var image = ImageProvider.GetImage(data, entry.RelativePath.GetExtension());
				string displayType = null;

				// This is necessary because the displayed image type will be converted to a different format
				if (entry.RelativePath.IsExtension(".bmp")) {
					try {
						BmpDecoder decoder = new BmpDecoder(data);
						displayType = decoder.GetDisplayType();
					}
					catch {
						
					}
				}

				return (image, displayType);
			}
			catch (GrfException err) {
				if (err == GrfExceptions.__CorruptedOrEncryptedEntry || err == GrfExceptions.__GravityEncryptedFile) {
					_imagePreview.Dispatch(p => p.Source = null);
					throw;
				}

				if (err == GrfExceptions.__ContainerBusy)
					return (null, null);

				throw;
			}
		}

		private void _setupUI(FileEntry entry) {
			string fileName = entry.RelativePath;

			this.Dispatch(delegate {
				_labelHeader.Text = "Image preview: " + entry.DisplayRelativePath;
				_buttonGroupImage.Visibility = PreviewService.IsImageCutable(entry.RelativePath, _grfData) ? Visibility.Visible : Visibility.Collapsed;
			});
		}

		private void _loadSpr(FileEntry entry) {
			if (_spr != null) {
				_imagePreviewSprite.Visibility = Visibility.Visible;

				try {
					_spr.SetPalette(new Pal(entry.GetDecompressedData(), Pal.FormatMode.NoTransparencyExceptFirstPixel).BytePalette);

					// Clear the sprite's cached image since we're reusing the sprite file.
					_spr.Image = null;
					_spriteImage.Image = _spr.Image;
					_imagePreviewSprite.Source = _spriteImage.Image.Cast<BitmapSource>();
					_sprExportPath = _sprFilePath.ReplaceExtension(".bmp");
					_spriteImage.ExportFileName = Path.GetFileNameWithoutExtension(_sprExportPath);
				}
				catch {
				}
			}
		}

		private void _buttonGroupImage_Click(object sender, RoutedEventArgs e) {
			try {
				PreviewService.RebuildSelectedImage(_entry.RelativePath, _grfData, _imagePreview);
				BitmapSource bitmap = (BitmapSource)_imagePreview.Source;

				byte[] pixels = WpfImaging.GetData(bitmap);
				_primaryImage.Image = new GrfImage(pixels, bitmap.PixelWidth, bitmap.PixelHeight, GrfImageType.Bgra32);
				_imagePreview.Width = _primaryImage.Image.Width;
				_imagePreview.Height = _primaryImage.Image.Height;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonSelectSprite_Click(object sender, RoutedEventArgs e) {
			try {
				string path = TkPathRequest.OpenFile(SpriteEditorConfiguration.AppLastPath_Config, "filter", FileFormat.Spr.ToFilter());

				if (path != null) {
					_sprFilePath = path;

					_spr = new Spr(File.ReadAllBytes(_sprFilePath));
					if (_spr.Palette == null)
						_spr.Palette = new Pal();

					_loadSpr(_entry);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
		private void _menuItemImageExport2_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_spriteImage, _sprExportPath);
	}
}