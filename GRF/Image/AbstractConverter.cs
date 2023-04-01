namespace GRF.Image {
	public abstract class AbstractImageConverter {
		public abstract object[] ReturnTypes { get; }
		public abstract object Convert(GrfImage image);
		public abstract GrfImage ConvertToSelf(GrfImage image);
	}
}
