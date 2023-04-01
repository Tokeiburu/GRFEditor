using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using GRF.Image;

namespace ExampleProject.ImageConverters {
	public class ImageConverter1 : AbstractImageConverter {
		public override object[] ReturnTypes {
			get { return new object[] { typeof(Bitmap) }; }
		}

		public override object Convert(GrfImage image) {
			Bitmap bit;
			IntPtr data;
			switch (image.GrfImageType) {
				case GrfImageType.Bgra32:
					data = Marshal.AllocHGlobal(image.Pixels.Length);
					Marshal.Copy(image.Pixels, 0, data, image.Pixels.Length);
					bit = new Bitmap(image.Width, image.Height, 4 * image.Width, PixelFormat.Format32bppArgb, data);
					return bit;
				case GrfImageType.Bgr32:
					data = Marshal.AllocHGlobal(image.Pixels.Length);
					Marshal.Copy(image.Pixels, 0, data, image.Pixels.Length);
					bit = new Bitmap(image.Width, image.Height, 4 * image.Width, PixelFormat.Format32bppRgb, data);
					return bit;
				case GrfImageType.Bgr24:
					data = Marshal.AllocHGlobal(image.Pixels.Length);
					Marshal.Copy(image.Pixels, 0, data, image.Pixels.Length);
					bit = new Bitmap(image.Width, image.Height, 4 * image.Width, PixelFormat.Format24bppRgb, data);
					return bit;
				case GrfImageType.Indexed8:
					if (image.Width % 4 != 0 || image.Height % 4 != 0)
						image = _fixImage(image);
					List<Color> colors = _loadColors(image.Palette);
					data = Marshal.AllocHGlobal(image.Pixels.Length);
					Marshal.Copy(image.Pixels, 0, data, image.Pixels.Length);
					bit = new Bitmap(image.Width, image.Height, image.Width, PixelFormat.Format8bppIndexed, data);
					ColorPalette palette = bit.Palette;
					Color[] entries = palette.Entries;

					for (int i = 0; i < colors.Count; i++) {
						entries[i] = colors[i];
					}
					bit.Palette = palette;
					return bit;
				case GrfImageType.NotEvaluatedBmp:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedPng:
					return _readAsCommonFormat(image);
				case GrfImageType.NotEvaluatedJpg:
					return _readAsCommonFormat(image);
				default:
					throw new Exception("Unsupported pixel format");
			}
		}

		public override GrfImage ConvertToSelf(GrfImage image) {
			return image;
		}

		private GrfImage _fixImage(GrfImage image) {
			int newWidth = image.Width % 4 != 0 ? image.Width + (4 - image.Width % 4) : image.Width;
			int newHeight = image.Height % 4 != 0 ? image.Height + (4 - image.Height % 4) : image.Height;

			byte[] pixels = new byte[newWidth * newHeight];

			for (int i = 0; i < image.Height; i++) {
				Buffer.BlockCopy(image.Pixels, image.Width * i, pixels, newWidth * i, image.Width);
			}

			byte[] pal = new byte[image.Palette.Length];
			Buffer.BlockCopy(image.Palette, 0, pal, 0, pal.Length);

			return new GrfImage(ref pixels, newWidth, newHeight, image.GrfImageType, ref pal);
		}

		private Bitmap _readAsCommonFormat(GrfImage image) {
			MemoryStream mStream = new MemoryStream();
			byte[] pData = image.Pixels;
			mStream.Write(pData, 0, pData.Length);
			Bitmap bm = new Bitmap(mStream, false);
			mStream.Dispose();
			return bm;
		}
		private List<Color> _loadColors(byte[] palette) {
			List<Color> colors = new List<Color>();
			int index;

			for (int i = 0; i < palette.Length / 4; i++) {
				index = 4 * i;
				colors.Add(Color.FromArgb(palette[index + 3], palette[index], palette[index + 1], palette[index + 2]));
			}
			return colors;
		}
	}
}
