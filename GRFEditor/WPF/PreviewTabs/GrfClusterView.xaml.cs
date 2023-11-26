using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using TokeiLibrary;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for GrfClusterView.xaml
	/// </summary>
	public partial class GrfClusterView : UserControl {
		private const int _numberOfLines = 20;
		private readonly List<Color> _colorsList = new List<Color>();
		private readonly List<int[]> _lengthsList = new List<int[]>();
		private readonly List<uint[]> _offsetsList = new List<uint[]>();
		private int _bpp;
		private int[] _lengths;
		private uint[] _offsets;

		private int _physicalX;
		private int _physicalY;
		private byte[] _pixels;
		private uint _totalSize;

		public GrfClusterView() {
			InitializeComponent();
			_canvas.SizeChanged += new SizeChangedEventHandler(_canvas_SizeChanged);
		}

		private void _canvas_SizeChanged(object sender, SizeChangedEventArgs e) {
			try {
				if (_offsets == null || _lengths == null)
					return;

				List<uint[]> offsets = new List<uint[]>(_offsetsList);
				List<int[]> lengths = new List<int[]>(_lengthsList);
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

				_physicalX = (int) _canvas.ActualWidth;
				_physicalY = _numberOfLines * 7;

				if (_physicalX <= 0)
					return;

				byte[] background = new byte[] { 229, 165, 155 };

				_bpp = PixelFormats.Bgr24.BitsPerPixel / 8;

				_pixels = new byte[_physicalX * _physicalY * _bpp];

				for (int i = 0; i < _pixels.Length / _bpp; i++) {
					Buffer.BlockCopy(background, 0, _pixels, i * _bpp, _bpp);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Draw(uint totalSize, uint[] offsets, int[] lengths, Color colorToDrawOffsets) {
			try {
				_offsetsList.Add(offsets);
				_lengthsList.Add(lengths);
				_colorsList.Add(colorToDrawOffsets);

				_totalSize = totalSize;
				_offsets = offsets;
				_lengths = lengths;

				if (_physicalX <= 0 || totalSize == 0)
					return;

				byte[] color = new byte[] { colorToDrawOffsets.B, colorToDrawOffsets.G, colorToDrawOffsets.R };

				int imageOffsetX;

				int totalImageLength = _physicalX * _numberOfLines;

				int positionX;
				int lineOffset;

				for (int i = 0; i < offsets.Length; i++) {
					positionX = (int) ((float) offsets[i] / totalSize * totalImageLength);
					int lengthToDraw = (int) Math.Ceiling(((float) lengths[i] / totalSize * totalImageLength));
					lengthToDraw = lengthToDraw == 0 ? 1 : lengthToDraw;

					for (int x = positionX; x < positionX + lengthToDraw; x++) {
						imageOffsetX = x % _physicalX;
						lineOffset = x / _physicalX;

						for (int y = 0; y < _physicalY / _numberOfLines; y++) {
							Buffer.BlockCopy(color, 0, _pixels, (imageOffsetX + (y * _physicalX) + (lineOffset * (_physicalY / _numberOfLines) * _physicalX)) * _bpp, _bpp);
						}
					}
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

				WriteableBitmap bitmap = new WriteableBitmap(_physicalX, _physicalY, 96, 96, PixelFormats.Bgr24, null);
				bitmap.WritePixels(new Int32Rect(0, 0, _physicalX, _physicalY), _pixels, _physicalX * _bpp, 0);
				bitmap.Freeze();
				Image image = new Image();
				image.Source = bitmap;
				_canvas.Children.Clear();
				_canvas.Children.Add(image);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Clear() {
			_canvas.Dispatch(p => p.Children.Clear());
		}
	}
}