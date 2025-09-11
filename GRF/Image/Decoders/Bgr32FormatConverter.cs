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

			unsafe {
				fixed (byte* pBase = image.Pixels) {
					byte* p = pBase;
					byte* pEnd = pBase + image.Pixels.Length;

					while (p < pEnd) {
						p[3] = 255;
						p += 4;
					}
				}
			}

			image.SetGrfImageType(GrfImageType.Bgra32);
		}

		public void Convert(GrfImage image) {
			if (image.GrfImageType != GrfImageType.Bgra32) throw new Exception("Expected pixel format is Bgra32, found " + image.GrfImageType);

			if (UseBackgroundColor) {
				_applyBackgroundColor(image, BackgroundColor);
			}

			image.SetGrfImageType(GrfImageType.Bgr32);
		}

		#endregion
	}
}
