using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Image;
using GRF.Image.Decoders;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace GRFEditor.Tools.SpriteEditor {
	/// <summary>
	/// Interaction logic for SpriteConverterFormatDialog.xaml
	/// </summary>
	public partial class SpriteConverterFormatDialog : TkWindow {
		#region Bgra32Mode enum

		public enum Bgra32Mode {
			Normal,
			PixelIndexZero,
			PixelIndexPink,
			FirstPixel,
			LastPixel
		}

		#endregion

		private readonly GrfImage _image;
		private readonly List<GrfImage> _images = new List<GrfImage>();
		private readonly List<RadioButton> _rbs = new List<RadioButton>();
		private readonly List<ScrollViewer> _svs = new List<ScrollViewer>();
		private readonly List<byte> _unusedIndexes = new List<byte>();
		private byte[] _originalPalette;
		private GrfImage _result;

		private bool _svEventsEnabled;

		public SpriteConverterFormatDialog(string filename, byte[] originalPalette, GrfImage image, List<byte> usedIndexes, int option = -1) : base("Format conflict", "convert.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			if (originalPalette == null) throw new ArgumentNullException("originalPalette");

			InitializeComponent();

			_images.Add(null);
			_images.Add(null);
			_images.Add(null);
			_images.Add(null);
			_originalPalette = originalPalette;
			RepeatOption = option;

			if (RepeatOption <= -1)
				Visibility = Visibility.Visible;
			else
				Visibility = Visibility.Hidden;

			_image = image;

			if (!usedIndexes.Contains(0))
				usedIndexes.Add(0);

			_description.Text = "The image " + Path.GetFileName(filename) + " is invalid for this operation. Select one of options below.";

			for (int i = 0; i < 256; i++) {
				if (!usedIndexes.Contains((byte) i))
					_unusedIndexes.Add((byte) i);
			}

			_load();
			_cbTransparency.SelectionChanged += _cbTransparency_SelectionChanged;
			_cbDithering.Checked += _cbDithering_Checked;
			_cbDithering.Unchecked += _cbDithering_Unchecked;

			_setScrollViewers();
		}

		private bool _repeatBehavior { get; set; }
		public int RepeatOption { get; private set; }

		public GrfImage Result {
			get { return _result; }
			set {
				if (value != null && value.GrfImageType == GrfImageType.Indexed8) {
					_imagePalette.Source = ImageProvider.GetImage(value.Palette, ".pal").Cast<BitmapSource>();
				}
				_result = value;
			}
		}

		private void _setScrollViewers() {
			_svs.Add(_sv0);
			_svs.Add(_sv1);
			_svs.Add(_sv2);
			_svs.Add(_sv3);
			_svs.Add(_sv4);

			_svs.ForEach(p => p.ScrollChanged += (e, a) => { if (_svEventsEnabled) _setAllScrollViewers(p); });
			_svEventsEnabled = true;
		}

		private void _setAllScrollViewers(ScrollViewer sv) {
			_svEventsEnabled = false;
			_svs.ForEach(p => {
				if (sv.ScrollableWidth > 0)
					p.ScrollToHorizontalOffset(sv.HorizontalOffset / sv.ScrollableWidth * p.ScrollableWidth);
				if (sv.ScrollableHeight > 0)
					p.ScrollToVerticalOffset(sv.VerticalOffset / sv.ScrollableHeight * p.ScrollableHeight);
			});
			_svEventsEnabled = true;
		}

		private void _load() {
			try {
				_rbs.Add(_rbOriginalPalette);
				_rbs.Add(_rbMatch);
				_rbs.Add(_rbMerge);
				_rbs.Add(_rbBgra32);

				_imageReal.Source = _image.Cast<BitmapSource>();
				_setImageDimensions(_imageReal);


				if (_image.GrfImageType == GrfImageType.Indexed8) {
					_images[0] = _loadFromOriginalPalette();
					_imageOriginal.Source = SpriteEditorHelper.MakeFirstPaletteColorTransparent(_images[0]);
				}
				else {
					_rbOriginalPalette.IsEnabled = false;
				}

				_setImageDimensions(_imageOriginal);
				_setImageDimensions(_imageClosestMatch);
				_setImageDimensions(_imageMergePalette);
				_setImageDimensions(_imageToBgra32);

				_tbTransparent.Text = "The transparent color is #AARRGGBB - #" + BitConverter.ToString(new byte[] { _originalPalette[3], _originalPalette[2], _originalPalette[1], _originalPalette[0] }).Replace("-", string.Empty) + " ";
				_imageTransparent.Fill = new SolidColorBrush(Color.FromArgb(_originalPalette[3], _originalPalette[0], _originalPalette[1], _originalPalette[2]));

				if (RepeatOption > -1) {
					_repeatBehavior = true;
				}

				switch (SpriteEditorConfiguration.FormatConflictOption) {
					case 0:
						_rbOriginalPalette.IsChecked = true;
						break;
					case 1:
						_rbMatch.IsChecked = true;
						break;
					case 2:
						_rbMerge.IsChecked = true;
						break;
					case 3:
						_rbBgra32.IsChecked = true;
						break;
				}

				_cbTransparency.SelectedIndex = SpriteEditorConfiguration.TransparencyMode;
				_cbDithering.IsChecked = SpriteEditorConfiguration.UseDithering;
				_imagePalette.Source = ImageProvider.GetImage(_originalPalette, ".pal").Cast<BitmapSource>();
				_update();
				_updateSelection();

				Loaded += new RoutedEventHandler(_spriteConverterFormatDialog_Loaded);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _spriteConverterFormatDialog_Loaded(object sender, RoutedEventArgs e) {
			if (RepeatOption > -1) {
				_repeatBehavior = true;
				_buttonOk_Click(null, null);
			}
			else {
				Visibility = Visibility.Visible;
			}
		}

		private GrfImage _loadFromOriginalPalette() {
			GrfImage image = _image.Copy();
			image.SetPalette(ref _originalPalette);
			return image;
		}

		private GrfImage _loadFromBestMatch(bool usePixelDithering) {
			GrfImage newImage = _image.Copy();
			Indexed8FormatConverter conv = new Indexed8FormatConverter();

			if (usePixelDithering) {
				conv.Options |= Indexed8FormatConverter.PaletteOptions.UseDithering | Indexed8FormatConverter.PaletteOptions.UseExistingPalette;
			}

			conv.ExistingPalette = _originalPalette;
			conv.BackgroundColor = GrfColor.White;

			newImage.Convert(conv);
			return newImage;
		}

		private GrfImage _showUsingBgra32Index0() {
			byte a;
			byte r;
			byte g;
			byte b;

			if (_image.GrfImageType == GrfImageType.Indexed8) {
				r = _image.Palette[0];
				g = _image.Palette[1];
				b = _image.Palette[2];
				a = _image.Palette[3];
			}
			else {
				r = _originalPalette[0];
				g = _originalPalette[1];
				b = _originalPalette[2];
				a = _originalPalette[3];
			}

			GrfImage newImage = _image.Copy();
			newImage.Convert(new Bgra32FormatConverter());

			for (int i = 0, size = newImage.Pixels.Length / 4; i < size; i++) {
				if (b == newImage.Pixels[4 * i + 0] &&
				    g == newImage.Pixels[4 * i + 1] &&
				    r == newImage.Pixels[4 * i + 2] &&
				    a == newImage.Pixels[4 * i + 3]) {
					newImage.Pixels[4 * i + 3] = 0;
				}
			}

			return newImage;
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
		}

		private GrfImage _getImageUsingPixelZero(GrfImage image) {
			if (image != null && image.GrfImageType == GrfImageType.Indexed8) {
				GrfImage im = image.Copy();

				byte[] palette = im.Palette;
				Buffer.BlockCopy(_originalPalette, 0, palette, 0, 4);

				if (_image.GrfImageType == GrfImageType.Indexed8) {
					if (_image.Pixels.Any(p => p == 0)) {
						for (int i = 0; i < im.Pixels.Length; i++) {
							if (_image.Pixels[i] == 0) {
								im.Pixels[i] = 0;
							}
						}
					}
				}

				return im;
			}

			return null;
		}

		private GrfImage _getImageUsingPixel(GrfImage image, Color color) {
			if (image != null && image.GrfImageType == GrfImageType.Indexed8) {
				GrfImage im = image.Copy();

				List<byte> toChange = new List<byte>();

				for (int i = 0; i < 256; i++) {
					if (image.Palette[4 * i + 0] == color.R &&
					    image.Palette[4 * i + 1] == color.G &&
					    image.Palette[4 * i + 2] == color.B) {
						toChange.Add((byte) i);
					}
				}

				byte[] palette = im.Palette;
				Buffer.BlockCopy(_originalPalette, 0, palette, 0, 4);

				for (int i = 0; i < im.Pixels.Length; i++) {
					if (toChange.Contains(im.Pixels[i])) {
						im.Pixels[i] = 0;
					}
				}

				return im;
			}

			return null;
		}

		private void _setImageDimensions(FrameworkElement image) {
			image.Width = _image.Width;
			image.Height = _image.Height;
		}

		private GrfImage _loadFromMerge(Color transparentColor, bool useDithering) {
			GrfImage im = _image.Copy();

			List<byte> unusedIndexes = new List<byte>(_unusedIndexes);
			byte[] newPalette = new byte[1024];
			Buffer.BlockCopy(_originalPalette, 0, newPalette, 0, 1024);

			int numberOfAvailableColors = unusedIndexes.Count;

			if (_image.GrfImageType == GrfImageType.Indexed8) {
				List<byte> newImageUsedIndexes = new List<byte>();
				for (int i = 0; i < 256; i++) {
					if (Array.IndexOf(im.Pixels, (byte) i) > -1) {
						newImageUsedIndexes.Add((byte) i);
					}
				}

				if (newImageUsedIndexes.Count < numberOfAvailableColors) {
					for (int usedIndex = 0; usedIndex < newImageUsedIndexes.Count; usedIndex++) {
						byte index = newImageUsedIndexes[usedIndex];

						for (int i = 0; i < 256; i++) {
							if (
								im.Palette[4 * index + 0] == _originalPalette[4 * i + 0] &&
								im.Palette[4 * index + 1] == _originalPalette[4 * i + 1] &&
								im.Palette[4 * index + 2] == _originalPalette[4 * i + 2]) {
								newImageUsedIndexes.Remove(index);
								usedIndex--;

								if (unusedIndexes.Contains(index))
									unusedIndexes.Remove(index);

								break;
							}
						}
					}
				}
				else {
					List<Tuple<int, byte>> colors = newImageUsedIndexes.Select(t => new Tuple<int, byte>((im.Palette[4 * t + 0]) << 16 | (im.Palette[4 * t + 1]) << 8 | (im.Palette[4 * t + 2]), t)).ToList();
					colors = colors.OrderBy(p => p.Item1).ToList();

					List<byte> newImageTempUsedIndexes = new List<byte>();
					newImageTempUsedIndexes.Add(colors[0].Item2);
					newImageTempUsedIndexes.Add(colors[colors.Count - 1].Item2);

					int numberToAdd = unusedIndexes.Count - 2;
					int numberOfItems = newImageUsedIndexes.Count - 2;

					for (int i = 0; i < numberToAdd; i++) {
						newImageTempUsedIndexes.Add(colors[(int) (((float) i / numberToAdd) * numberOfItems)].Item2);
					}

					newImageUsedIndexes = new List<byte>(newImageTempUsedIndexes);
				}

				for (int i = 0; i < newImageUsedIndexes.Count; i++) {
					if (unusedIndexes.Count <= 0) break;

					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = im.Palette[4 * newImageUsedIndexes[i] + 0];
					newPalette[4 * unused + 1] = im.Palette[4 * newImageUsedIndexes[i] + 1];
					newPalette[4 * unused + 2] = im.Palette[4 * newImageUsedIndexes[i] + 2];
					newPalette[4 * unused + 3] = im.Palette[4 * newImageUsedIndexes[i] + 3];
					unusedIndexes.RemoveAt(0);
				}
			}
			else {
				GrfImage imTemp = im.Copy();
				Bgr32FormatConverter tconv = new Bgr32FormatConverter();
				tconv.BackgroundColor = GrfColor.White;
				imTemp.Convert(tconv);

				List<int> colors = new List<int>(imTemp.Pixels.Length / 4);

				int index;
				for (int i = 0; i < imTemp.Pixels.Length / 4; i++) {
					index = 4 * i;
					if (imTemp.Pixels[index + 3] != 0)
						colors.Add(imTemp.Pixels[index + 2] << 16 | imTemp.Pixels[index + 1] << 8 | imTemp.Pixels[index + 0]);
				}

				colors = colors.Distinct().OrderBy(p => p).ToList();

				int color;
				for (int i = 0; i < 256; i++) {
					color = _originalPalette[4 * i + 0] << 16 | _originalPalette[4 * i + 1] << 8 | _originalPalette[4 * i + 2];
					if (colors.Contains(color))
						colors.Remove(color);
				}

				int numberOfColorsToAdd = numberOfAvailableColors - 1;
				numberOfColorsToAdd = colors.Count < numberOfColorsToAdd ? colors.Count : numberOfColorsToAdd;

				for (int i = 0; i < numberOfColorsToAdd - 1; i++) {
					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = (byte) ((colors[(int) (i / (float) numberOfColorsToAdd * colors.Count)] & 0xFF0000) >> 16);
					newPalette[4 * unused + 1] = (byte) ((colors[(int) (i / (float) numberOfColorsToAdd * colors.Count)] & 0x00FF00) >> 8);
					newPalette[4 * unused + 2] = (byte) ((colors[(int) (i / (float) numberOfColorsToAdd * colors.Count)] & 0x0000FF));
					newPalette[4 * unused + 3] = 255;
					unusedIndexes.RemoveAt(0);
				}

				if (numberOfColorsToAdd > 0) {
					byte unused = unusedIndexes[0];
					newPalette[4 * unused + 0] = (byte) ((colors[colors.Count - 1] & 0xFF0000) >> 16);
					newPalette[4 * unused + 1] = (byte) ((colors[colors.Count - 1] & 0x00FF00) >> 8);
					newPalette[4 * unused + 2] = (byte) ((colors[colors.Count - 1] & 0x0000FF));
					newPalette[4 * unused + 3] = 255;
					unusedIndexes.RemoveAt(0);
				}
			}

			Indexed8FormatConverter conv = new Indexed8FormatConverter();
			conv.BackgroundColor = new GrfColor(transparentColor.A, transparentColor.R, transparentColor.G, transparentColor.B);

			if (useDithering) {
				conv.Options |= Indexed8FormatConverter.PaletteOptions.UseDithering;
			}
			conv.ExistingPalette = newPalette;
			im.Convert(conv);
			return im;
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			if (SpriteEditorConfiguration.FormatConflictOption == -1) {
				WindowProvider.ShowDialog("Please select an option or cancel.");
				return;
			}

			RepeatOption = _repeatBehavior ? SpriteEditorConfiguration.FormatConflictOption : -1;
			DialogResult = true;
			Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			DialogResult = false;

			if (_cbRepeat.IsChecked == true)
				RepeatOption = -2;

			Close();
		}

		private void _cbTransparency_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			SpriteEditorConfiguration.TransparencyMode = _cbTransparency.SelectedIndex;
			_update();
			_updateSelection();
		}

		private void _cbDithering_Checked(object sender, RoutedEventArgs e) {
			SpriteEditorConfiguration.UseDithering = true;
			_update();
			_updateSelection();
		}

		private void _cbDithering_Unchecked(object sender, RoutedEventArgs e) {
			SpriteEditorConfiguration.UseDithering = false;
			_update();
			_updateSelection();
		}

		private void _update() {
			try {
				GrfImage match = null;
				GrfImage merge = null;
				GrfImage bgra32 = null;

				if (_cbTransparency.SelectedIndex == 0) {
					bgra32 = _loadFromBgra32(Bgra32Mode.Normal);

					if (_cbDithering.IsChecked == true) {
						match = _loadFromBestMatch(true);
						merge = _loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), true);
					}
					else {
						match = _loadFromBestMatch(false);
						merge = _loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), false);
					}
				}
				else if (_cbTransparency.SelectedIndex == 1) {
					bgra32 = _loadFromBgra32(Bgra32Mode.PixelIndexZero);

					if (_cbDithering.IsChecked == true) {
						match = _getImageUsingPixelZero(_loadFromBestMatch(true));
						merge = _getImageUsingPixelZero(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), true));
					}
					else {
						match = _getImageUsingPixelZero(_loadFromBestMatch(false));
						merge = _getImageUsingPixelZero(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), false));
					}
				}
				else if (_cbTransparency.SelectedIndex == 2) {
					bgra32 = _loadFromBgra32(Bgra32Mode.PixelIndexPink);

					Color color = Color.FromArgb(255, 255, 0, 255);
					if (_cbDithering.IsChecked == true) {
						match = _getImageUsingPixel(_loadFromBestMatch(true), color);
						merge = _getImageUsingPixel(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), true), color);
					}
					else {
						match = _getImageUsingPixel(_loadFromBestMatch(false), color);
						merge = _getImageUsingPixel(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), false), color);
					}
				}
				else if (_cbTransparency.SelectedIndex == 3) {
					bgra32 = _loadFromBgra32(Bgra32Mode.FirstPixel);

					Color color = _getColor(0);
					if (_cbDithering.IsChecked == true) {
						match = _getImageUsingPixel(_loadFromBestMatch(true), color);
						merge = _getImageUsingPixel(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), true), color);
					}
					else {
						match = _getImageUsingPixel(_loadFromBestMatch(false), color);
						merge = _getImageUsingPixel(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), false), color);
					}
				}
				else if (_cbTransparency.SelectedIndex == 4) {
					bgra32 = _loadFromBgra32(Bgra32Mode.LastPixel);

					Color color = _getColor(-1);
					if (_cbDithering.IsChecked == true) {
						match = _getImageUsingPixel(_loadFromBestMatch(true), color);
						merge = _getImageUsingPixel(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), true), color);
					}
					else {
						match = _getImageUsingPixel(_loadFromBestMatch(false), color);
						merge = _getImageUsingPixel(_loadFromMerge(Color.FromArgb(255, _originalPalette[0], _originalPalette[1], _originalPalette[2]), false), color);
					}
				}

				_imageClosestMatch.Source = SpriteEditorHelper.MakeFirstPaletteColorTransparent(match);
				_imageMergePalette.Source = SpriteEditorHelper.MakeFirstPaletteColorTransparent(merge);
				_imageToBgra32.Source = bgra32.Cast<BitmapSource>();
				_images[1] = match;
				_images[2] = merge;
				_images[3] = bgra32;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private Color _getColor(int pixel) {
			byte a;
			byte r;
			byte g;
			byte b;

			if (_image.GrfImageType == GrfImageType.Indexed8) {
				pixel = pixel < 0 ? _image.Pixels.Length - 1 : pixel;

				int index = _image.Pixels[pixel];
				r = _image.Palette[4 * index + 0];
				g = _image.Palette[4 * index + 1];
				b = _image.Palette[4 * index + 2];
				a = _image.Palette[4 * index + 3];
			}
			else {
				pixel = pixel < 0 ? (_image.Pixels.Length / 4) - 1 : pixel;

				r = _image.Pixels[4 * pixel + 2];
				g = _image.Pixels[4 * pixel + 1];
				b = _image.Pixels[4 * pixel + 0];
				a = _image.Pixels[4 * pixel + 3];
			}

			return Color.FromArgb(a, r, g, b);
		}

		private GrfImage _loadFromBgra32(Bgra32Mode mode) {
			switch (mode) {
				case Bgra32Mode.Normal:
					GrfImage im = _image.Copy();
					im.Convert(new Bgra32FormatConverter());
					return im;
				case Bgra32Mode.PixelIndexZero:
					return _showUsingBgra32Index0();
				case Bgra32Mode.PixelIndexPink:
					return _showUsingBgra32TransparentColor(255, 255, 0, 255);
				case Bgra32Mode.FirstPixel:
					return _showUsingBgra32Pixel(0);
				case Bgra32Mode.LastPixel:
					return _showUsingBgra32Pixel(-1);
			}
			return null;
		}

		private GrfImage _showUsingBgra32Pixel(int pixel) {
			byte a;
			byte r;
			byte g;
			byte b;

			if (_image.GrfImageType == GrfImageType.Indexed8) {
				pixel = pixel < 0 ? _image.Pixels.Length - 1 : pixel;

				int index = _image.Pixels[pixel];
				r = _image.Palette[4 * index + 0];
				g = _image.Palette[4 * index + 1];
				b = _image.Palette[4 * index + 2];
				a = _image.Palette[4 * index + 3];
			}
			else {
				pixel = pixel < 0 ? (_image.Pixels.Length / 4) - 1 : pixel;

				r = _image.Pixels[4 * pixel + 2];
				g = _image.Pixels[4 * pixel + 1];
				b = _image.Pixels[4 * pixel + 0];
				a = _image.Pixels[4 * pixel + 3];
			}

			return _showUsingBgra32TransparentColor(a, r, g, b);
		}

		private GrfImage _showUsingBgra32TransparentColor(byte a, byte r, byte g, byte b) {
			GrfImage newImage = _image.Copy();
			newImage.Convert(new Bgra32FormatConverter());

			for (int i = 0, size = newImage.Pixels.Length / 4; i < size; i++) {
				if (b == newImage.Pixels[4 * i + 0] &&
				    g == newImage.Pixels[4 * i + 1] &&
				    r == newImage.Pixels[4 * i + 2] &&
				    a == newImage.Pixels[4 * i + 3]) {
					newImage.Pixels[4 * i + 3] = 0;
				}
			}

			return newImage;
		}

		#region Checkboxes

		private void _uncheckAll(object sender = null) {
			_rbs.ForEach(p => p.IsChecked = false);

			if (sender != null) {
				_rbs.ForEach(p => p.Checked -= _rb_Checked);
				((RadioButton) sender).IsChecked = true;
				_updateSelection();
				_rbs.ForEach(p => p.Checked += _rb_Checked);
			}
		}

		private void _updateSelection() {
			bool somethingIsChecked = true;

			if (_rbOriginalPalette.IsChecked == true) {
				SpriteEditorConfiguration.FormatConflictOption = 0;
			}
			else if (_rbMatch.IsChecked == true) {
				SpriteEditorConfiguration.FormatConflictOption = 1;
			}
			else if (_rbMerge.IsChecked == true) {
				SpriteEditorConfiguration.FormatConflictOption = 2;
			}
			else if (_rbBgra32.IsChecked == true) {
				SpriteEditorConfiguration.FormatConflictOption = 3;
			}
			else {
				SpriteEditorConfiguration.FormatConflictOption = -1;
				somethingIsChecked = false;
			}

			if (somethingIsChecked) {
				Result = _images[SpriteEditorConfiguration.FormatConflictOption];
			}

			RepeatOption = _repeatBehavior ? SpriteEditorConfiguration.FormatConflictOption : -1;
		}

		private void _rb_Checked(object sender, RoutedEventArgs e) {
			_uncheckAll(sender);
		}

		private void _cbRepeat_Checked(object sender, RoutedEventArgs e) {
			_repeatBehavior = true;
		}

		private void _cbRepeat_Unchecked(object sender, RoutedEventArgs e) {
			_repeatBehavior = false;
		}

		#endregion
	}
}