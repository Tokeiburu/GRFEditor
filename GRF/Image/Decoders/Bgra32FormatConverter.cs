using System;

namespace GRF.Image.Decoders {
	public class Bgra32FormatConverter : AbstractImageFormatConverter, IImageFormatConverter {
		#region IImageFormatConverter Members

		public void ToBgra32(GrfImage image) { }

		public void Convert(GrfImage image) {
			if (image.GrfImageType != GrfImageType.Bgra32) throw new Exception("Expected pixel format is Bgra32, found " + image.GrfImageType);

			if (UseBackgroundColor) {
				_applyBackgroundColor(image, BackgroundColor);
			}
		}

		#endregion
	}
}
