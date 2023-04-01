using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities;

namespace TokeiLibrary {
	public static class WpfImaging {
		public static byte[] GetData(BitmapSource image) {
			byte[] dataToReturn = new byte[image.PixelWidth * image.PixelHeight * image.Format.BitsPerPixel / 8];
			image.CopyPixels(dataToReturn, image.PixelWidth * image.Format.BitsPerPixel / 8, 0);
			return dataToReturn;
		}

		public static WriteableBitmap ToBgra32FromIndexed8(byte[] frameData, IList<Color> colors, int width, int height) {
			byte[] newData = new byte[width * height * 4];

			WriteableBitmap bit = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
			int index;

			for (int j = 0; j < frameData.Length; j++) {
				index = j * 4;
				newData[index] = colors[frameData[j]].B;
				newData[index + 1] = colors[frameData[j]].G;
				newData[index + 2] = colors[frameData[j]].R;
				newData[index + 3] = colors[frameData[j]].A;
			}

			bit.WritePixels(new Int32Rect(0, 0, width, height), newData, width * 4, 0);

			return bit;
		}

		public static byte[] GetBytePaletteRGBA(BitmapPalette pal) {
			byte[] palette = new byte[pal.Colors.Count * 4];
			int index;

			for (int i = 0; i < pal.Colors.Count; i++) {
				index = 4 * i;
				palette[index] = pal.Colors[i].R;
				palette[index + 1] = pal.Colors[i].G;
				palette[index + 2] = pal.Colors[i].B;
				palette[index + 3] = pal.Colors[i].A;
			}

			return palette;
		}
		public static byte[] GetBytePaletteRGB(BitmapPalette pal) {
			byte[] palette = new byte[pal.Colors.Count * 3];
			int index;

			for (int i = 0; i < pal.Colors.Count; i++) {
				index = 3 * i;
				palette[index] = pal.Colors[i].R;
				palette[index + 1] = pal.Colors[i].G;
				palette[index + 2] = pal.Colors[i].B;
			}

			return palette;
		}

		public static WriteableBitmap FixDPI(BitmapSource bitImage) {
			if (bitImage == null) return null;

			const double DPI = 96;

			if (Methods.CanUseIndexed8 || bitImage.Format != PixelFormats.Indexed8) {
				int width = bitImage.PixelWidth;
				int height = bitImage.PixelHeight;

				int stride = (int)Math.Ceiling(width * bitImage.Format.BitsPerPixel / 8f);
				byte[] pixelData = new byte[stride * height];
				bitImage.CopyPixels(pixelData, stride, 0);

				WriteableBitmap bitmap = new WriteableBitmap(BitmapSource.Create(width, height, DPI, DPI, bitImage.Format, bitImage.Palette, pixelData, stride));
				bitmap.Freeze();
				return bitmap;
			}

			List<Color> colors = new List<Color>(bitImage.Palette.Colors);
			byte[] pixels = new byte[bitImage.PixelWidth * bitImage.PixelHeight * bitImage.Format.BitsPerPixel / 8];
			bitImage.CopyPixels(pixels, bitImage.PixelWidth * bitImage.Format.BitsPerPixel / 8, 0);
			WriteableBitmap bit = ToBgra32FromIndexed8(pixels, colors, bitImage.PixelWidth, bitImage.PixelHeight);
			bit.Freeze();
			return bit;
		}

		public static byte[] GetPixels(BitmapSource bit) {
			byte[] pixels = new byte[bit.PixelWidth * bit.PixelHeight * bit.Format.BitsPerPixel / 8];
			bit.CopyPixels(pixels, bit.PixelWidth * bit.Format.BitsPerPixel / 8, 0);
			return pixels;
		}
	}
}
