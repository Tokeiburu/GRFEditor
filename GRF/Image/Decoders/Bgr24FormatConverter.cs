using System;

namespace GRF.Image.Decoders {
	public class Bgr24FormatConverter : AbstractImageFormatConverter, IImageFormatConverter {
		public Bgr24FormatConverter() {
			KeepFullyTransparentBackground = false;
			UseBackgroundColor = true;
		}

		#region IImageFormatConverter Members

		public void ToBgra32(GrfImage image) {
			int size = image.Width * image.Height;
			byte[] newPixels = new byte[size * 4];

			for (int i = 0; i < size; i++) {
				Buffer.BlockCopy(image.Pixels, 3 * i, newPixels, 4 * i, 3);

				if (image.TransparentPixels != null) {
					newPixels[4 * i + 3] = (byte)(image.TransparentPixels[i] ? 0 : 255);
				}
				else {
					newPixels[4 * i + 3] = 255;
				}
			}

			image.SetPixels(ref newPixels);
			image.SetGrfImageType(GrfImageType.Bgra32);
		}

		public void Convert(GrfImage image) {
			if (image.GrfImageType != GrfImageType.Bgra32) throw new Exception("Expected pixel format is Bgra32, found " + image.GrfImageType);

			if (UseBackgroundColor) {
				_applyBackgroundColor(image, BackgroundColor);
			}

			int size = image.Width * image.Height;
			byte[] newPixels = new byte[size * 3];

			for (int i = 0; i < size; i++) {
				Buffer.BlockCopy(image.Pixels, 4 * i, newPixels, 3 * i, 3);
			}

			image.SetPixels(ref newPixels);
			image.SetGrfImageType(GrfImageType.Bgr24);
		}

		#endregion
	}
}
