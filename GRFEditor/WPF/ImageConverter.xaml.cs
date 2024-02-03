using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.FileFormats;
using GRF.Image;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for ImageConverter.xaml
	/// </summary>
	public partial class ImageConverter : TkWindow {
		private List<string> _paths = new List<string>();
		private readonly List<PixelFormatInfo> _formats = new List<PixelFormatInfo>();

		public ImageConverter() : base("Image converter", "imconvert.ico") {
			InitializeComponent();

			_formats.AddRange(PixelFormatInfo.Formats);

			_cbFormats.ItemsSource = _formats.Select(p => p.DisplayName + " (*" + p.Extension + ")").ToList();
			ShowInTaskbar = true;
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				if (_cbFormats.SelectedIndex < 0)
					throw new Exception("No output format selected.");

				if (_paths.Count == 0) {
					return;
				}
				
				var path = PathRequest.FolderExtract();

				if (path != null) {
					foreach (var file in _paths) {
						var format = _formats[_cbFormats.SelectedIndex];
						var image = new GrfImage(file);

						if (file.IsExtension(".bmp")) {
							image.MakePinkTransparent();
						}
						else if (file.IsExtension(".png")) {
							image.MakeTransparentPink();
						}

						image.Save(GrfPath.Combine(path, Path.GetFileNameWithoutExtension(file) + format.Extension), format);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonBrowse_Click(object sender, RoutedEventArgs e) {
			try {
				var paths = PathRequest.OpenFilesSprite("filter", "Image Files|*.bmp;*.png;*.jpg;*.tga|Bitmap Files|*.bmp|PNG Files|*.png|Jpeg Files|*.jpg|Targa Files|*.tga") ?? new string[] { };

				_paths.Clear();
				_paths.AddRange(paths);

				_resetPreview();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _resetPreview() {
			int limit = 20;
			const int max = 120;

			try {
				_imagesPreview.Children.Clear();
				_labelDrop.Visibility = Visibility.Collapsed;

				foreach (var path in _paths) {
					Image image = new Image();
					var grfImage = new GrfImage(path);
					image.Source = grfImage.Cast<BitmapSource>();

					if (grfImage.Width > max && grfImage.Width > grfImage.Height) {
						image.Width = max;
						image.Height = grfImage.Height / (grfImage.Width / (double)max);
					}
					else if (grfImage.Height > max) {
						image.Width = grfImage.Width / (grfImage.Height / (double)max);
						image.Height = max;
					}
					else {
						image.Width = grfImage.Width;
						image.Height = grfImage.Height;
					}

					limit--;
					image.Margin = new Thickness(3);
					image.VerticalAlignment = VerticalAlignment.Center;
					_imagesPreview.Children.Add(image);

					if (limit < 0)
						break;
				}
			}
			finally {
				_buttonSaveAs.IsEnabled = _paths.Count != 0;
			}
		}

		private void _scrollViewer_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null) {
						_paths = files.Where(p => p.IsExtension(".jpg", ".bmp", ".png", ".tga")).ToList();
						_resetPreview();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}