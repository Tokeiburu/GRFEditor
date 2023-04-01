using System;

namespace GRF.Image.Decoders {
	public class Bgr32FormatConverter : AbstractImageFormatConverter, IImageFormatConverter {
		public Bgr32FormatConverter() {
			KeepFullyTransparentBackground = false;
			UseBackgroundColor = true;
		}

		#region IImageFormatConverter Members

		public void ToBgra32(GrfImage image) {
			int size = image.Width * image.Height;

			for (int i = 0; i < size; i++) {
				image.Pixels[4 * i + 3] = 255;
			}

			image.SetGrfImageType(GrfImageType.Bgra32);
		}

		public void Convert(GrfImage image) {
			if (image.GrfImageType != GrfImageType.Bgra32) throw new Exception("Expected pixel format is Bgra32, found " + image.GrfImageType);

			if (UseBackgroundColor) {
				_applyBackgroundColor(image, BackgroundColor);
			}

			int size = image.Width * image.Height;
			byte[] newPixels = new byte[size * 4];
			Buffer.BlockCopy(image.Pixels, 0, newPixels, 0, size * 4);

			image.SetPixels(ref newPixels);
			image.SetGrfImageType(GrfImageType.Bgr32);
		}

		#endregion
	}
}
