using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.FileFormats.TgaFormat;
using GRF.Image;
using Utilities;

namespace GrfToWpfBridge.Application {
	public class DefaultImageConverter : AbstractImageConverter {
		private readonly object[] _returnTypes = new object[] { typeof (BitmapSource), typeof (ImageSource) };

		public override object[] ReturnTypes {
			get { return _returnTypes; }
		}

		public override object Convert(GrfImage image) {
			WriteableBitmap bit;
			switch (image.GrfImageType) {
				case GrfImageType.Bgra32:
					bit = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null);
					bit.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), image.Pixels, image.Width * 4, 0);
					bit.Freeze();
					return bit;
				case GrfImageType.Bgr32:
					bit = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null);
					bit.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), image.Pixels, image.Width * 4, 0);
					bit.Freeze();
					return bit;
				case GrfImageType.Bgr24:
					bit = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr24, null);
					bit.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), image.Pixels, image.Width * 3, 0);
					bit.Freeze();
					return bit;
				case GrfImageType.Indexed8:
					List<Color> colors = _loadColors(image.Palette);

					if (Methods.CanUseIndexed8) {
						bit = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Indexed8, new BitmapPalette(colors));
						bit.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), image.Pixels, image.Width, 0);
					}
					else {
						bit = Imaging.ToBgra32FromIndexed8(image.Pixels, colors, image.Width, image.Height);
					}

					bit.Freeze();
					return bit;
				case GrfImageType.NotEvaluated:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedBmp:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedPng:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedJpg:
					return _readAsJpgFormat(image);
				case GrfImageType.NotEvaluatedTga:
					return _readAsTgaFormat(image);
				default:
					throw new Exception("Unsupported pixel format");
			}
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
				case GrfImageType.NotEvaluatedBmp:
				case GrfImageType.NotEvaluatedPng:
					bit = _readAsCommonFormat(image);
					break;
				case GrfImageType.NotEvaluatedJpg:
					bit = _readAsJpgFormat(image);
					break;
				case GrfImageType.NotEvaluatedTga:
					bit = _readAsTgaFormat(image);
					break;
				//case GrfImageType.NotEvaluatedBmp:
				//	return new BmpDecoder(image.Pixels).ToGrfImage();
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
						GrfImage img = new GrfImage(ref pixels, bit.PixelWidth, bit.PixelHeight, GrfImageType.Indexed8, ref palette);
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

			return new GrfImage(ref pixels, bit.PixelWidth, bit.PixelHeight, type, ref palette);
		}

		private BitmapSource _readAsCommonFormat(GrfImage image) {
			if (image.Pixels.Length > 3) {
				if (Methods.ByteArrayCompare(image.Pixels, 0, 4, GrfImage.PngHeader, 0)) {
					BitmapDecoder decoder = new PngBitmapDecoder(new MemoryStream(image.Pixels), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
					return decoder.Frames[0];
					//return Imaging.FixDpi(decoder.Frames[0]);
				}
				if (Methods.ByteArrayCompare(image.Pixels, 0, 2, GrfImage.BmpHeader, 0)) {
					BitmapDecoder decoder = new BmpBitmapDecoder(new MemoryStream(image.Pixels), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
					return decoder.Frames[0];
					//return Imaging.FixDpi(decoder.Frames[0]);
				}
			}

			BitmapImage bitImage = new BitmapImage { CreateOptions = BitmapCreateOptions.PreservePixelFormat, CacheOption = BitmapCacheOption.Default };
			bitImage.BeginInit();
			bitImage.StreamSource = new MemoryStream(image.Pixels);
			bitImage.EndInit();
			bitImage.Freeze();

			return bitImage;
			//return Imaging.FixDpi(bitImage);
		}

		private WriteableBitmap _readAsTgaFormat(GrfImage image) {
			Tga decoder = new Tga(image.Pixels);

			byte[] pixels = new byte[decoder.Pixels.Length];
			Buffer.BlockCopy(decoder.Pixels, 0, pixels, 0, pixels.Length);

			PixelFormat format = decoder.Type.ToPixelFormat();

			WriteableBitmap bit = new WriteableBitmap(decoder.Header.Width, decoder.Header.Height, 96, 96, format, null);
			bit.WritePixels(new Int32Rect(0, 0, decoder.Header.Width, decoder.Header.Height), pixels, decoder.Header.Width * format.BitsPerPixel / 8, 0);
			bit.Freeze();
			return bit;
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

			for (int i = 0, count = palette.Length; i < count; i += 4) {
				colors.Add(Color.FromArgb(palette[i + 3], palette[i], palette[i + 1], palette[i + 2]));
			}

			return colors;
		}
	}
}