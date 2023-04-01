namespace GRF.Image {
	public enum GrfImageType {
		Indexed8 = 1,
		Bgra32 = 2,
		Bgr32 = 3,
		Bgr24 = 4,
		NotEvaluated = 255,
		NotEvaluatedPng = 256,	// Information from non evluated image types are invalid on purpose,
		NotEvaluatedJpg = 257,	// all the data is actually stored in the Pixels array
		NotEvaluatedBmp = 258,
		NotEvaluatedTga = 259,
	}

	public enum FlipDirection {
		Horizontal,
		Vertical
	}

	public enum RotateDirection {
		Left,
		Right
	}

	public enum Indexed8ConverterMode {
		Dithering,
		DirectMatching
	}

	public enum GrfScalingMode {
		NearestNeighbor,
		LinearScaling
	}
}