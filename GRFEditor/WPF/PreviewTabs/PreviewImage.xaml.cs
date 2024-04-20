using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.Tools.SpriteEditor;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.Paths;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewImage.xaml
	/// </summary>
	public partial class PreviewImage : FilePreviewTab {
		private readonly GrfImageWrapper _wrapper = new GrfImageWrapper();
		private readonly GrfImageWrapper _wrapper2 = new GrfImageWrapper();
		private TransformGroup _regularTransformGroup = new TransformGroup();
		private string _sprFilePath;

		public PreviewImage() {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			_loadOtherTransformGroup();
			_imagePreview.RenderTransform = _regularTransformGroup;
			_isInvisibleResult = () => _imagePreview.Dispatch(p => p.Visibility = Visibility.Hidden);
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper);
			VirtualFileDataObject.SetDraggable(_imagePreviewSprite, _wrapper2);
			WpfUtilities.AddFocus(_tbEase);

			bool eventsEnabled = true;

			_gpEase.ValueChanged += delegate {
				if (!eventsEnabled) return;
				_tbEase.Text = String.Format("{0:0.00}", _gpEase.Position * 5d + 1);
			};

			_tbEase.TextChanged += delegate {
				eventsEnabled = false;

				try {
					var zoom = FormatConverters.DoubleConverter(_tbEase.Text);
					GrfEditorConfiguration.PreviewImageZoom = (float) zoom;
					_gpEase.SetPosition((zoom - 1) / 5d, false);
					_updateZoom(zoom);
				}
				catch {
					_updateZoom(1d);
				}
				finally {
					eventsEnabled = true;
				}
			};

			_tbEase.Text = String.Format("{0:0.00}", GrfEditorConfiguration.PreviewImageZoom);
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		private void _updateZoom() {
			this.Dispatch(delegate {
				try {
					_updateZoom(FormatConverters.DoubleConverter(_tbEase.Text));
				}
				catch {
					_updateZoom(1d);
				}
			});
		}

		private void _updateZoom(double zoom) {
			if (_imagePreview.Source == null) return;
			BitmapSource bitmap = (BitmapSource) _imagePreview.Source;

			_imagePreview.Width = bitmap.PixelWidth * zoom;
			_imagePreview.Height = bitmap.PixelHeight * zoom;
			_imagePreview.Stretch = Stretch.Fill;
		}

		private void _loadOtherTransformGroup() {
			_regularTransformGroup = new TransformGroup();
			ScaleTransform flipTrans = new ScaleTransform();
			TranslateTransform translate = new TranslateTransform();
			RotateTransform rotate = new RotateTransform();
			translate.X = 0;
			translate.Y = 0;
			rotate.Angle = 0;
			flipTrans.ScaleX = 1;
			flipTrans.ScaleY = 1;

			_regularTransformGroup.Children.Add(rotate);
			_regularTransformGroup.Children.Add(flipTrans);
			_regularTransformGroup.Children.Add(translate);
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null)
				_wrapper.Image.SaveTo(_entry.RelativePath, PathRequest.ExtractSetting);
		}

		protected override void _load(FileEntry entry) {
			string fileName = entry.RelativePath;

			_buttonSelectSprite.Dispatch(delegate {
				if (fileName.GetExtension() == ".pal") {
					_buttonSelectSprite.Visibility = Visibility.Visible;
					_imagePreviewSprite.Visibility = Visibility.Visible;
					_loadSpr();
				}
				else {
					_buttonSelectSprite.Visibility = Visibility.Collapsed;
					_imagePreviewSprite.Visibility = Visibility.Collapsed;
				}
			});

			_imagePreview.Dispatch(p => p.Tag = Path.GetFileNameWithoutExtension(fileName));
			_labelHeader.Dispatch(p => p.Text = "Image preview : " + Path.GetFileName(fileName));

			_buttonGroupImage.Dispatch(p => p.Visibility = PreviewService.IsImageCutable(entry.RelativePath, _grfData) ? Visibility.Visible : Visibility.Collapsed);

			try {
				_wrapper.Image = ImageProvider.GetImage(_grfData.FileTable[fileName].GetDecompressedData(), Path.GetExtension(fileName).ToLower());
			}
			catch (GrfException err) {
				if (err == GrfExceptions.__CorruptedOrEncryptedEntry) {
					_imagePreview.Dispatch(delegate {
						_imagePreview.Source = null;
						_updateZoom();
					});

					_labelHeader.Dispatch(p => p.Text = "Failed to decompressed data. Corrupted or encrypted entry.");
					return;
				}

				if (err == GrfExceptions.__ContainerBusy)
					return;

				throw;
			}

			_imagePreview.Dispatch(delegate {
				_imagePreview.Source = _wrapper.Image.Cast<BitmapSource>();
				_updateZoom();
			});
			_imagePreview.Dispatch(p => p.Visibility = Visibility.Visible);
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _loadSpr() {
			if (_sprFilePath != null) {
				_imagePreviewSprite.Visibility = Visibility.Visible;

				if (File.Exists(_sprFilePath)) {
					try {
						byte[] data = File.ReadAllBytes(_sprFilePath);
						Spr spr = new Spr(data);
						byte[] palette = new Pal(_entry.GetDecompressedData()).BytePalette;
						palette[3] = 0;

						if (spr.Palette == null)
							spr.Palette = new Pal(palette);

						spr.Palette.SetPalette(palette);

						for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
							spr.Images[i].SetPalette(ref palette);
						}

						_wrapper2.Image = spr.Image;
						_imagePreviewSprite.Source = _wrapper2.Image.Cast<BitmapSource>();
						_imagePreviewSprite.Tag = _sprFilePath.ReplaceExtension(".bmp");
					}
					catch {
					}
				}
			}
		}

		private void _buttonGroupImage_Click(object sender, RoutedEventArgs e) {
			try {
				PreviewService.RebuildSelectedImage(_entry.RelativePath, _grfData, _imagePreview);
				BitmapSource bitmap = (BitmapSource) _imagePreview.Source;

				byte[] pixels = WpfImaging.GetData(bitmap);
				_wrapper.Image = new GrfImage(ref pixels, bitmap.PixelWidth, bitmap.PixelHeight, GrfImageType.Bgra32);
				_imagePreview.Width = _wrapper.Image.Width;
				_imagePreview.Height = _wrapper.Image.Height;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null)
				_wrapper.Image.SaveTo(_entry.RelativePath, PathRequest.ExtractSetting);
		}

		private void _buttonSelectSprite_Click(object sender, RoutedEventArgs e) {
			_sprFilePath = TkPathRequest.OpenFile<SpriteEditorConfiguration>("AppLastPath", "filter", FileFormat.MergeFilters(Format.Spr));
			_loadSpr();
		}

		private void _menuItemImageExport2_Click(object sender, RoutedEventArgs e) {
			if (_wrapper2.Image != null)
				_wrapper2.Image.SaveTo(_imagePreviewSprite.Tag.ToString(), PathRequest.ExtractSetting);
		}
	}
}