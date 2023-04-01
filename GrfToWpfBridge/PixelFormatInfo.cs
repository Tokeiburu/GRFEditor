using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.FileFormats.TgaFormat;
using GRF.Image;
using Utilities.Extension;

namespace GrfToWpfBridge {
	public sealed class PixelFormatInfo {
		private static readonly List<PixelFormatInfo> _formats = new List<PixelFormatInfo>();

		public static PixelFormatInfo GifIndexed8 = new PixelFormatInfo("gif", PixelFormats.Indexed8);
		public static PixelFormatInfo BmpBgra32 = new PixelFormatInfo("bmp", PixelFormats.Bgra32);
		public static PixelFormatInfo BmpBgr32 = new PixelFormatInfo("bmp", PixelFormats.Bgr32);
		public static PixelFormatInfo BmpBgr24 = new PixelFormatInfo("bmp", PixelFormats.Bgr24);
		public static PixelFormatInfo BmpIndexed8 = new PixelFormatInfo("bmp", PixelFormats.Indexed8);
		public static PixelFormatInfo PngBgra32 = new PixelFormatInfo("png", PixelFormats.Bgra32);
		public static PixelFormatInfo PngBgr32 = new PixelFormatInfo("png", PixelFormats.Bgr32);
		public static PixelFormatInfo PngBgr24 = new PixelFormatInfo("png", PixelFormats.Bgr24);
		public static PixelFormatInfo JpegBgr24 = new PixelFormatInfo("jpg", PixelFormats.Bgr24);
		public static PixelFormatInfo JpegBgr32 = new PixelFormatInfo("jpg", PixelFormats.Bgr32);
		public static PixelFormatInfo TgaBgra32 = new PixelFormatInfo("tga", PixelFormats.Bgra32);
		public static PixelFormatInfo TgaBgr24 = new PixelFormatInfo("tga", PixelFormats.Bgr24);

		private PixelFormatInfo(string extension, PixelFormat format) {
			CutExtension = extension;
			Extension = "." + extension;
			Format = format;
			DisplayName = String.Format("{0} - {1}", CutExtension.ToUpper(), Format);
			AssemblyName = CutExtension[0].ToString(CultureInfo.InvariantCulture).ToUpper() + CutExtension.Substring(1) + Format;
			Filter = DisplayName + " (*" + Extension + ")|*" + Extension;

			_formats.Add(this);
		}

		public static List<PixelFormatInfo> Formats {
			get { return _formats; }
		}

		public string DisplayName { get; private set; }
		public string Filter { get; private set; }
		public string Extension { get; set; }
		public string CutExtension { get; set; }
		public string AssemblyName { get; private set; }
		public PixelFormat Format { get; set; }

		public override string ToString() {
			return DisplayName;
		}

		public static BitmapDecoder GetDecoder(string filename) {
			if (filename.GetExtension() == ".bmp")
				return new BmpBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			if (filename.GetExtension() == ".png")
				return new PngBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			if (filename.GetExtension() == ".jpg")
				return new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			if (filename.GetExtension() == ".tga")
				throw new Exception("No decoder available for Targa image files.");
			if (filename.GetExtension() == ".gif")
				throw new Exception("No decoder available for Gif image files.");

			throw new Exception("Failed to retrieve decoder.");
		}

		public bool IsCompatible(string extension, PixelFormat format) {
			if (extension == Extension) {
				return format == Format;
			}

			return false;
		}

		public static PixelFormatInfo GetFormatFromAssembly(string name) {
			foreach (PixelFormatInfo info in _formats) {
				if (info.AssemblyName.ToLowerInvariant() == name)
					return info;
			}

			throw new Exception("Couldn't find the target format.");
		}

		public static PixelFormat GetFormat(string file) {
			if (file.GetExtension() == ".tga") {
				return new Tga(File.ReadAllBytes(file)).Header.Bits == 24 ? PixelFormats.Bgr24 : PixelFormats.Bgra32;
			}
			return GetDecoder(file).Frames[0].Format;
		}

		public static PixelFormatInfo GetFormat(string extension, BitmapSource image) {
			PixelFormat format = image.Format;

			foreach (PixelFormatInfo info in _formats) {
				if (info.IsCompatible(extension, format)) {
					return info;
				}
			}

			return BmpBgra32;
		}

		public static PixelFormatInfo GetFormat(string extension, GrfImage image) {
			PixelFormat format = image.GrfImageType.ToPixelFormat();

			foreach (PixelFormatInfo info in _formats) {
				if (info.IsCompatible(extension, format)) {
					return info;
				}
			}

			return BmpBgra32;
		}

		public static PixelFormatInfo GetFormat(string extension, PixelFormat format) {
			foreach (PixelFormatInfo info in _formats) {
				if (info.IsCompatible(extension, format)) {
					return info;
				}
			}

			return BmpBgra32;
		}
	}
}