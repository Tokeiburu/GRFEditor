using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.Image;
using Utilities;

namespace GrfToWpfBridge.Application {
	public class DefaultImageConverter : AbstractImageConverter {
		private readonly object[] _returnTypes = new object[] { typeof (BitmapSource), typeof (ImageSource) };

		public override object[] ReturnTypes {
			get { return _returnTypes; }
		}

		public override object Convert(GrfImage image) {
			BitmapSource bitmap = null;

			switch (image.GrfImageType) {
				case GrfImageType.Bgra32:
					bitmap = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null, image.Pixels, image.Width * 4);
					break;
				case GrfImageType.Bgr32:
					bitmap = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null, image.Pixels, image.Width * 4);
					break;
				case GrfImageType.Bgr24:
					bitmap = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr24, null, image.Pixels, image.Width * 3);
					break;
				case GrfImageType.Indexed8:
					bitmap = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Indexed8, new BitmapPalette(_loadColors(image.Palette)), image.Pixels, image.Width);
					break;
				case GrfImageType.NotEvaluated:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedPng:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedJpg:
					return _readAsJpgFormat(image);
				default:
					throw new Exception("Unsupported pixel format");
			}

			if (bitmap != null)
				bitmap.Freeze();

			return bitmap;
		}

		public override GrfImage ConvertToSelf(GrfImage image) {
			BitmapSource bit;

			switch (image.GrfImageType) {
				case GrfImageType.Bgra32:
				case GrfImageType.Bgr32:
				case GrfImageType.Bgr24:
				case GrfImageType.Indexed8:
					return image;
				case GrfImageType.NotEvaluated:
				case GrfImageType.NotEvaluatedPng:
					bit = _readAsCommonFormat(image);
					break;
				case GrfImageType.NotEvaluatedJpg:
					bit = _readAsJpgFormat(image);
					break;
				default:
					throw new Exception("Unsupported pixel format");
			}

			GrfImageType type = image.GrfImageType;

			if (bit.Format == PixelFormats.Bgra32) type = GrfImageType.Bgra32;
			else if (bit.Format == PixelFormats.Bgr32) type = GrfImageType.Bgr32;
			else if (bit.Format == PixelFormats.Bgr24) type = GrfImageType.Bgr24;
			else if (bit.Format == PixelFormats.Indexed8) type = GrfImageType.Indexed8;

			byte[] pixels;
			byte[] palette;

			if (type < GrfImageType.NotEvaluated) {
				pixels = Imaging.GetPixels(bit);
				palette = Imaging.Get256BytePaletteRGBA(bit.Palette);

				// Fix : 2015-08-16
				// Some PNGs are treated as palette images, and they will lose their transparency.
				// The fix below converts the file to an Indexed8 image and the palette contains the transparency.
				if (image.GrfImageType == GrfImageType.NotEvaluatedPng && bit.Palette != null) {
					if (type != GrfImageType.Bgra32) {
						// Convert the palette to Bgra32
						GrfImage img = new GrfImage(pixels, bit.PixelWidth, bit.PixelHeight, GrfImageType.Indexed8, palette);
						img.Convert(GrfImageType.Bgra32);
						pixels = img.Pixels;
						palette = null;
						type = GrfImageType.Bgra32;
					}
				}
			}
			else {
				bit = Imaging.ConvertToBgra32(bit);
				type = GrfImageType.Bgra32;

				pixels = Imaging.GetPixels(bit);
				palette = Imaging.Get256BytePaletteRGBA(bit.Palette);
			}

			return new GrfImage(pixels, bit.PixelWidth, bit.PixelHeight, type, palette);
		}

		private BitmapSource _readAsCommonFormat(GrfImage image) {
			if (image.Pixels.Length > 3) {
				if (Methods.ByteArrayCompare(image.Pixels, 0, 4, GrfImageAnalysis.PngHeader, 0)) {
					BitmapDecoder decoder = new PngBitmapDecoder(new MemoryStream(image.Pixels), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
					return decoder.Frames[0];
				}
				if (Methods.ByteArrayCompare(image.Pixels, 0, 2, GrfImageAnalysis.BmpHeader, 0)) {
					BitmapDecoder decoder = new BmpBitmapDecoder(new MemoryStream(image.Pixels), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
					return decoder.Frames[0];
				}
			}

			BitmapImage bitImage = new BitmapImage { CreateOptions = BitmapCreateOptions.PreservePixelFormat, CacheOption = BitmapCacheOption.Default };
			bitImage.BeginInit();
			bitImage.StreamSource = new MemoryStream(image.Pixels);
			bitImage.EndInit();
			bitImage.Freeze();

			return bitImage;
		}

		private BitmapSource _readAsJpgFormat(GrfImage image) {
			try {
				return _readAsCommonFormat(image);
			}
			catch {
				using (MemoryStream memStream = new MemoryStream(image.Pixels)) {
					JpegBitmapDecoder decoder = new JpegBitmapDecoder(memStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
					BitmapFrame frame = decoder.Frames[0];

					byte[] pixels = new byte[frame.PixelHeight * frame.PixelWidth * frame.Format.BitsPerPixel / 8];
					frame.CopyPixels(pixels, frame.PixelWidth * frame.Format.BitsPerPixel / 8, 0);

					WriteableBitmap bit = new WriteableBitmap(frame.PixelWidth, frame.PixelHeight, 96, 96, frame.Format, frame.Palette);
					bit.WritePixels(new Int32Rect(0, 0, frame.PixelWidth, frame.PixelHeight), pixels, frame.PixelWidth * frame.Format.BitsPerPixel / 8, 0);
					bit.Freeze();
					return bit;
				}
			}
		}

		private List<Color> _loadColors(byte[] palette) {
			if (palette == null)
				throw new Exception("Palette not loaded.");

			List<Color> colors = new List<Color>(256);

			unsafe {
				fixed (byte* bPalette = palette) {
					for (int i = 0, count = palette.Length; i < count; i += 4) {
						colors.Add(Color.FromArgb(bPalette[i + 3], bPalette[i], bPalette[i + 1], bPalette[i + 2]));
					}
				}
			}

			return colors;
		}
	}
}