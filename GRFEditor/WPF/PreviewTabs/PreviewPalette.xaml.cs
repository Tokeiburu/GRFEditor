using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GrfToWpfBridge;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewImage.xaml
	/// </summary>
	public partial class PreviewPalette : FilePreviewTab {
		private readonly GrfImageWrapper _wrapper = new GrfImageWrapper();
		private TransformGroup _regularTransformGroup = new TransformGroup();

		public PreviewPalette() {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			_loadOtherTransformGroup();
			_imagePreview.RenderTransform = _regularTransformGroup;
			_isInvisibleResult = () => _imagePreview.Dispatch(p => p.Visibility = Visibility.Hidden);
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper);
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
			ErrorPanel = _errorPanel;
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

			_imagePreview.Dispatch(p => p.Tag = Path.GetFileNameWithoutExtension(fileName));
			_labelHeader.Dispatch(p => p.Text = "Palette preview: " + Path.GetFileName(fileName));

			try {
				GrfImage paletteImage;

				if (fileName.IsExtension(".spr")) {
					paletteImage = new Spr(_grfData.FileTable[fileName]).Palette.Image;
					_wrapper.Image = paletteImage;
				}
				else {
					GrfImage image = new GrfImage(_grfData.FileTable[fileName]);

					if (image.GrfImageType == GrfImageType.Indexed8) {
						image = new Pal(image.Palette).Image;
						_wrapper.Image = image;
					}
					else {
						_labelHeader.Dispatch(p => p.Text = "Image has no palette.");
					
						_imagePreview.Dispatch(delegate {
							_imagePreview.Source = null;
							_updateZoom();
						});
					}
				}
			}
			catch {
				_imagePreview.Dispatch(delegate {
					_imagePreview.Source = null;
					_updateZoom();
				});

				throw;
			}

			_imagePreview.Dispatch(delegate {
				_imagePreview.Source = _wrapper.Image.Cast<BitmapSource>();
				_updateZoom();
			});
			_imagePreview.Dispatch(p => p.Visibility = Visibility.Visible);
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null)
				_wrapper.Image.SaveTo(_entry.RelativePath, PathRequest.ExtractSetting);
		}
	}
}