using System;

namespace GRF.Image.Decoders {
	public class ImageFormatProvider {
		public static IImageFormatConverter GetFormatConverter(GrfImageType type) {
			switch (type) {
				case GrfImageType.Bgr24:
					return new Bgr24FormatConverter();
				case GrfImageType.Bgr32:
					return new Bgr32FormatConverter();
				case GrfImageType.Bgra32:
					return new Bgra32FormatConverter();
				case GrfImageType.Indexed8:
					return new Indexed8FormatConverter();
				default:
					throw new Exception("No format converter was found for this format : " + type);
			}
		}
	}
}