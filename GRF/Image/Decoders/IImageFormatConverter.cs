namespace GRF.Image.Decoders {
	public interface IImageFormatConverter {
		void ToBgra32(GrfImage image);
		void Convert(GrfImage image);
	}
}
