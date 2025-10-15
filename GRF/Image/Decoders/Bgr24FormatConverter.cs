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
			int i = 0;

			unsafe {
				fixed (byte* pNewPixelsBase = newPixels)
				fixed (byte* pSourcePixelsBase = image.Pixels) {
					byte* pSourcePixels = pSourcePixelsBase;
					byte* pEnd = pSourcePixelsBase + image.Pixels.Length;
					byte* pNewPixels = pNewPixelsBase;
			
					while (pSourcePixels < pEnd) {
						pNewPixels[0] = pSourcePixels[0];
						pNewPixels[1] = pSourcePixels[1];
						pNewPixels[2] = pSourcePixels[2];
			
						if (image.TransparentPixels != null) {
							pNewPixels[3] = (byte)(image.TransparentPixels[i] ? 0 : 255);
						}
						else {
							pNewPixels[3] = 255;
						}
			
						pSourcePixels += 3;
						pNewPixels += 4;
						i++;
					}
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
