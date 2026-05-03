using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Image;
using TokeiLibrary;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for GrfClusterView.xaml
	/// </summary>
	public partial class GrfClusterView : UserControl {
		private const int _numberOfLines = 20;
		private readonly List<Color> _colorsList = new List<Color>();
		private readonly List<List<int>> _lengthsList = new List<List<int>>();
		private readonly List<List<long>> _offsetsList = new List<List<long>>();
		private List<int> _lengths;
		private List<long> _offsets;

		private int _physicalX;
		private int _physicalY;
		private List<Color> _palette = new List<Color>();
		private long _totalSize;
		private GrfImage _image;

		public GrfClusterView() {
			InitializeComponent();
			_canvas.SizeChanged += _canvas_SizeChanged;
		}

		private void _canvas_SizeChanged(object sender, SizeChangedEventArgs e) {
			try {
				if (_offsets == null || _lengths == null)
					return;

				List<List<long>> offsets = new List<List<long>>(_offsetsList);
				List<List<int>> lengths = new List<List<int>>(_lengthsList);
				List<Color> colors = new List<Color>(_colorsList);

				DrawBackground();

				for (int i = 0; i < offsets.Count; i++) {
					Draw(_totalSize, offsets[i], lengths[i], colors[i]);
				}

				DrawImage();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void DrawBackground() {
			try {
				_offsetsList.Clear();
				_lengthsList.Clear();
				_colorsList.Clear();

				_physicalX = (int) _canvas.Dispatch(p => p.ActualWidth);
				_physicalY = _numberOfLines;

				if (_physicalX <= 0)
					return;

				_image = new GrfImage(new byte[_physicalX * _physicalY], _physicalX, _physicalY, GrfImageType.Indexed8);
				_palette.Clear();
				_palette.Add(Color.FromArgb(255, 155, 165, 229));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Draw(long totalSize, List<long> offsets, List<int> lengths, in Color colorToDrawOffsets) {
			try {
				_offsetsList.Add(offsets);
				_lengthsList.Add(lengths);
				_colorsList.Add(colorToDrawOffsets);

				_totalSize = totalSize;
				_offsets = offsets;
				_lengths = lengths;

				if (_physicalX <= 0 || totalSize == 0)
					return;

				byte paletteIdx = (byte)_palette.Count;
				_palette.Add(colorToDrawOffsets);

				int totalImageLength = _physicalX * _numberOfLines;
				int positionX;

				for (int i = 0; i < offsets.Count; i++) {
					positionX = (int) ((float) offsets[i] / totalSize * totalImageLength);
					int lengthToDraw = (int) Math.Ceiling(((float) lengths[i] / totalSize * totalImageLength));
					lengthToDraw = lengthToDraw == 0 ? 1 : lengthToDraw;

					_image.Fill(positionX, lengthToDraw, paletteIdx);
				}
			}
			catch {
				//ErrorHandler.HandleException(err);
			}
		}

		public void DrawImage() {
			try {
				if (_physicalX <= 0)
					return;

				var bitmap = BitmapSource.Create(_physicalX, _physicalY, 96, 96, PixelFormats.Indexed8, new BitmapPalette(_palette), _image.Pixels, _physicalX);
				bitmap.Freeze();
				_drawImage.Source = bitmap;
				_drawImage.Height = _numberOfLines * 7;
				_drawImage.Width = _physicalX;
			}
			catch {
				//ErrorHandler.HandleException(err);
			}
		}
	}
}