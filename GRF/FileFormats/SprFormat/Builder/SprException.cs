using System;

namespace GRF.FileFormats.SprFormat.Builder {
	public enum SprExceptionType {
		Generic,
		InvalidImageFormat,
		InvalidImageSize,
		RleCompressionSize,
	}

	public class SprException : Exception {
		public SprException(string message) : base(message) {
			ExeptionType = SprExceptionType.Generic;
		}

		public SprException(string message, SprExceptionType type)
			: base(message) {
			ExeptionType = type;
		}

		public SprExceptionType ExeptionType { get; protected set; }
	}

	public class SprInvalidImageFormatException : SprException {
		public SprInvalidImageFormatException(string message) : base(message) {
			ExeptionType = SprExceptionType.InvalidImageFormat;
		}

		public SprInvalidImageFormatException() : base("The image is not of a valid type.") {
			ExeptionType = SprExceptionType.InvalidImageFormat;
		}
	}

	public class SprImageOverflowException : SprException {
		public SprImageOverflowException(int imageIndex, int width, int height)
			: base(String.Format("Image [index: {2}, {0}x{1}] has too many pixels (max: {3}), the limit is 65536. Consider converting the image to Bgra32 (no size limit).", width, height, imageIndex, width * height)) {
			ExeptionType = SprExceptionType.InvalidImageSize;
			Width = width;
			Height = height;
			ImageIndex = imageIndex;
		}

		public int Width { get; private set; }
		public int Height { get; private set; }
		public int ImageIndex { get; private set; }
	}

	public class SprRleBufferOverflowException : SprException {
		public SprRleBufferOverflowException() : base("Buffer overflow while executing the Rle compression or decompressing.") {
			ExeptionType = SprExceptionType.RleCompressionSize;
		}
	}
}