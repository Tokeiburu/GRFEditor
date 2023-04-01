namespace GRF.Image {
	/// <summary>
	/// Interface used for classes that can be viewed as an image
	/// </summary>
	public interface IImageable {
		/// <summary>
		/// Gets or sets the preview image.
		/// </summary>
		GrfImage Image { get; set; }
	}
}
