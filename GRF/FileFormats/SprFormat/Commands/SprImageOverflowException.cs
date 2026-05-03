using GRF.ContainerFormat;
using System;

namespace GRF.FileFormats.SprFormat.Commands {
	public class SprImageOverflowException : GrfException {
		public SprImageOverflowException(int imageIndex, int width, int height) :
			base(GrfExceptions.__SprSizeLimitReached, String.Format(GrfExceptions.__SprSizeLimitReached, width, height, imageIndex, width * height)) {
			
			Width = width;
			Height = height;
			ImageIndex = imageIndex;
		}

		public int Width { get; private set; }
		public int Height { get; private set; }
		public int ImageIndex { get; private set; }
	}
}
