using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.FileFormats;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.Threading;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.Encoders;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Action = System.Action;
using Point = GRF.Graphics.Point;

namespace GrfToWpfBridge {
	public static partial class Imaging {
		#region Extension methods for GrfImage and related

		public static void Save(this GrfImage image, string pathDestination, PixelFormatInfo format) {
			pathDestination = _validatePath(pathDestination, image.GrfImageType);

			GrfImage convertedImage = _convert(image, format);
			BridgeEncoder encoder = BridgeEncoder.GetEncoder(format);

			encoder.AddFrame(convertedImage);
			encoder.Save(pathDestination);
		}

		public static void Save(this GrfImage image, string pathDestination, PixelFormat format) {
			pathDestination = _validatePath(pathDestination, image.GrfImageType);
			Save(image, pathDestination, PixelFormatInfo.GetFormat(pathDestination.GetExtension(), format));
		}

		public static void Save(this GrfImage image, string pathDestination) {
			pathDestination = _validatePath(pathDestination, image.GrfImageType);
			Save(image, pathDestination, PixelFormatInfo.GetFormat(pathDestination.GetExtension(), image.GrfImageType.ToPixelFormat()));
		}

		public static string SaveTo(this GrfImage image, string suggestedFileName, Setting setting) {
			try {
				List<PixelFormatInfo> formats = PixelFormatInfo.Formats;

				if (suggestedFileName.GetExtension() == null) {
					suggestedFileName = suggestedFileName + GuessExtension(image.GrfImageType);
				}

				string extension = suggestedFileName.GetExtension();

				SaveFileDialog sfd = new SaveFileDialog();
				sfd.FileName = Path.GetFileNameWithoutExtension(suggestedFileName);
				sfd.AddExtension = true;
				sfd.InitialDirectory = (string) setting.Get();
				sfd.OverwritePrompt = true;
				sfd.Filter = string.Join("|", formats.Select(p => p.Filter).ToArray());

				if (extension == ".gat" || extension == ".pal") {
					sfd.FilterIndex = formats.IndexOf(PixelFormatInfo.BmpIndexed8) + 1;
				}
				else if (extension == ".act") {
					sfd.FilterIndex = formats.IndexOf(PixelFormatInfo.GifIndexed8) + 1;
				}
				else {
					sfd.FilterIndex = formats.IndexOf(PixelFormatInfo.GetFormat(extension, image)) + 1;
				}

				DialogResult res = sfd.ShowDialog();

				if (res == DialogResult.OK) {
					PixelFormatInfo selectedFormat = formats[sfd.FilterIndex - 1];
					setting.Set(Path.GetDirectoryName(sfd.FileName));

					try {
						image.Save(sfd.FileName, selectedFormat);
						return sfd.FileName;
					}
					catch (Exception err) {
						ErrorHandler.HandleException("Couldn't save the image.", err, ErrorLevel.NotSpecified);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return null;
		}

		private static GrfImage _convert(GrfImage image, PixelFormatInfo format) {
			if (image.GrfImageType == format.Format.ToGrfImageType()) {
				if (image.GrfImageType != GrfImageType.Bgra32 || format != PixelFormatInfo.BmpBgra32) {
					return image;
				}
			}

			GrfImage newImage = image.Copy();
			IImageFormatConverter converter = null;

			switch (format.Format.ToGrfImageType()) {
				case GrfImageType.Bgr24:
					converter = new Bgr24FormatConverter();
					break;
				case GrfImageType.Bgr32:
					converter = new Bgr32FormatConverter();
					break;
				case GrfImageType.Bgra32:
					if (format == PixelFormatInfo.BmpBgra32) {
						converter = new Bgra32FormatConverter { UseBackgroundColor = true, KeepFullyTransparentBackground = false };
					}
					else {
						converter = new Bgra32FormatConverter();
					}
					break;
				case GrfImageType.Indexed8:
					converter = new Indexed8FormatConverter { KeepFullyTransparentBackground = true, UseBackgroundColor = true, Options = Indexed8FormatConverter.PaletteOptions.AutomaticallyGeneratePalette };
					break;
			}

			if (converter != null) {
				newImage.Convert(converter);
			}
			else {
				throw new Exception("No converter was found for this format : " + format.Format);
			}

			return newImage;
		}

		public static GrfImageType ToGrfImageType(this PixelFormat format) {
			if (format == PixelFormats.Bgr24) return GrfImageType.Bgr24;
			if (format == PixelFormats.Bgr32) return GrfImageType.Bgr32;
			if (format == PixelFormats.Bgra32) return GrfImageType.Bgra32;
			if (format == PixelFormats.Indexed8) return GrfImageType.Indexed8;

			throw new Exception("Invalid image format : " + format);
		}

		private static string _validatePath(string path, GrfImageType type) {
			if (!Directory.Exists(Path.GetDirectoryName(path))) {
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			}

			if (path.GetExtension() == null) {
				path = path + GuessExtension(type);
			}

			return path;
		}

		public static string GuessExtension(GrfImageType desiredFormat) {
			if (desiredFormat == GrfImageType.Indexed8)
				return ".bmp";
			if (desiredFormat == GrfImageType.Bgr24)
				return ".bmp";
			if (desiredFormat == GrfImageType.Bgra32)
				return ".png";
			if (desiredFormat == GrfImageType.Bgr32)
				return ".bmp";

			return ".png";
		}

		public static string GuessExtension(PixelFormat desiredFormat) {
			if (desiredFormat == PixelFormats.Indexed8)
				return ".bmp";
			if (desiredFormat == PixelFormats.Bgr24)
				return ".bmp";
			if (desiredFormat == PixelFormats.Bgra32)
				return ".png";
			if (desiredFormat == PixelFormats.Bgr32)
				return ".bmp";

			return ".png";
		}

		#endregion

		#region Extension methods for Act

		public static void SaveTo(this Act act, int actionIndex, string suggestedFileName, Setting setting, AsyncOperation asyncOperation = null) {
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.FileName = Path.GetFileNameWithoutExtension(suggestedFileName);
			sfd.AddExtension = true;
			sfd.InitialDirectory = (string) setting.Get();
			sfd.OverwritePrompt = true;
			sfd.Filter = FileFormat.MergeFilters(Format.Gif);

			DialogResult res = sfd.ShowDialog();

			if (res == DialogResult.OK) {
				setting.Set(sfd.FileName);
				string filename = sfd.FileName;

				ProgressDummy dum = new ProgressDummy();
				dum.Progress = -1;

				Action action = () => ActImaging.Imaging.SaveAsGif(filename, act, actionIndex, dum);

				if (asyncOperation != null) {
					GrfThread thread = new GrfThread(action, dum, 200, null, true, true);

					string fileNameClosure = filename;
					thread.Finished += delegate {
						if (File.Exists(fileNameClosure))
							OpeningService.FileOrFolder(sfd.FileName);
					};

					asyncOperation.SetAndRunOperation(thread);
				}
				else {
					action();
				}
			}
		}

		#endregion

		#region Pure WPF Imaging

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

		public static byte[] GetPixels(BitmapSource bit) {
			byte[] pixels = new byte[bit.PixelWidth * bit.PixelHeight * bit.Format.BitsPerPixel / 8];
			bit.CopyPixels(pixels, bit.PixelWidth * bit.Format.BitsPerPixel / 8, 0);
			return pixels;
		}

		public static PixelFormat ToPixelFormat(this GrfImageType format) {
			if (format == GrfImageType.Bgr24) return PixelFormats.Bgr24;
			if (format == GrfImageType.Bgr32) return PixelFormats.Bgr32;
			if (format == GrfImageType.Bgra32) return PixelFormats.Bgra32;
			if (format == GrfImageType.Indexed8) return PixelFormats.Indexed8;

			throw new Exception("Invalid image format : " + format);
		}

		public static WriteableBitmap ConvertToBgra32(BitmapSource image) {
			return new WriteableBitmap(new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0));
		}

		public static BitmapSource FixDpi(BitmapSource bitImage) {
			const double DPI = 96;

			if (Methods.CanUseIndexed8 || bitImage.Format != PixelFormats.Indexed8) {
				int width = bitImage.PixelWidth;
				int height = bitImage.PixelHeight;

				if (bitImage.Width == width && bitImage.Height == height) return bitImage;

				int stride = (int) Math.Ceiling(width * bitImage.Format.BitsPerPixel / 8f);
				byte[] pixelData = new byte[stride * height];
				bitImage.CopyPixels(pixelData, stride, 0);

				return BitmapSource.Create(width, height, DPI, DPI, bitImage.Format, bitImage.Palette, pixelData, stride);
			}

			{
				List<Color> colors = new List<Color>(bitImage.Palette.Colors);
				byte[] pixelData = new byte[bitImage.PixelWidth * bitImage.PixelHeight * bitImage.Format.BitsPerPixel / 8];
				bitImage.CopyPixels(pixelData, bitImage.PixelWidth * bitImage.Format.BitsPerPixel / 8, 0);

				return ToBgra32FromIndexed8(pixelData, colors, bitImage.PixelWidth, bitImage.PixelHeight);
			}
		}

		public static WriteableBitmap FixDPI(BitmapSource bitImage) {
			const double DPI = 96;

			if (Methods.CanUseIndexed8 || bitImage.Format != PixelFormats.Indexed8) {
				int width = bitImage.PixelWidth;
				int height = bitImage.PixelHeight;

				if (bitImage.Width == width && bitImage.Height == height) return new WriteableBitmap(bitImage);

				int stride = (int) Math.Ceiling(width * bitImage.Format.BitsPerPixel / 8f);
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

		public static GrfImage ToGrfImage(this BitmapSource bitmap) {
			int stride = bitmap.Format.BitsPerPixel / 8 * bitmap.PixelWidth;
			byte[] data = new byte[bitmap.Format.BitsPerPixel / 8 * bitmap.PixelHeight * bitmap.PixelWidth];
			bitmap.CopyPixels(data, stride, 0);
			return new GrfImage(data, bitmap.PixelWidth, bitmap.PixelHeight, bitmap.Format.ToGrfImageType());
		}

		#endregion

		#region Palette related

		public static void RgbaToBgra(byte[] palette) {
			byte[] sourcePalette = new byte[palette.Length];
			Buffer.BlockCopy(palette, 0, sourcePalette, 0, palette.Length);

			for (int i = 0, size = palette.Length / 4; i < size; i++) {
				palette[4 * i + 0] = sourcePalette[4 * i + 2];
				palette[4 * i + 2] = sourcePalette[4 * i + 0];
			}
		}

		public static byte[] Get256BytePaletteRGBA(BitmapPalette pal, bool setTransparency = true) {
			if (pal == null)
				return null;

			byte[] palette = GetBytePaletteRGBA(pal);
			byte[] toReturn = new byte[256 * 4];

			if (palette.Length / 4 > 255) {
				Buffer.BlockCopy(palette, 0, toReturn, 0, 1024);
			}
			else {
				if (setTransparency) {
					for (int i = 0; i < 256; i++) {
						toReturn[4 * i + 3] = 255;
					}
				}

				Buffer.BlockCopy(palette, 0, toReturn, 0, palette.Length);
			}

			return toReturn;
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

		public static byte[] GetBytePaletteRGBFromRGBA(byte[] pal) {
			int colorsCount = pal.Length / 4;
			byte[] palette = new byte[colorsCount * 3];
			int indexS;
			int indexD;

			for (int i = 0; i < colorsCount; i++) {
				indexS = 4 * i;
				indexD = 3 * i;
				palette[indexD] = pal[indexS];
				palette[indexD + 1] = pal[indexS + 1];
				palette[indexD + 2] = pal[indexS + 2];
			}

			return palette;
		}

		#endregion

		#region Extension methods for colors

		public static byte[] ToBytesRgba(this Color color) {
			byte[] buffer = new byte[4];
			buffer[0] = color.R;
			buffer[1] = color.G;
			buffer[2] = color.B;
			buffer[3] = color.A;
			return buffer;
		}

		public static Color ToColor(this GrfColor color) {
			return Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		public static GrfColor ToGrfColor(this Color color) {
			return new GrfColor(color.A, color.R, color.G, color.B);
		}

		#endregion

		#region Others

		public static Point ToGrfPoint(this System.Windows.Point point) {
			return new Point(point.X, point.Y);
		}

		#endregion
	}
}