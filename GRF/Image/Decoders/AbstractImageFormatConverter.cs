namespace GRF.Image.Decoders {
	public abstract class AbstractImageFormatConverter {
		private GrfColor _backgroundColor = GrfColor.White;
		private bool _keepFullyTransparentBackground = true;

		public GrfColor BackgroundColor {
			get { return _backgroundColor; }
			set {
				_backgroundColor = value;
				UseBackgroundColor = true;
			}
		}

		public bool UseBackgroundColor { get; set; }
		public bool KeepFullyTransparentBackground {
			get { return _keepFullyTransparentBackground; }
			set { _keepFullyTransparentBackground = value; }
		}

		protected byte[] _toBgraPalette(byte[] palette) {
			byte[] pal = new byte[palette.Length];

			for (int i = 0, size = palette.Length; i < size; i += 4) {
				pal[i + 0] = palette[i + 2];
				pal[i + 1] = palette[i + 1];
				pal[i + 2] = palette[i + 0];
				pal[i + 3] = palette[i + 3];
			}

			return pal;
		}

		protected virtual void _applyBackgroundColor(GrfImage image, GrfColor backgroundColor) {
			byte alpha;
			int position;

			for (int i = 0, size = image.Pixels.Length / 4; i < size; i++) {
				alpha = image.Pixels[4 * i + 3];
				position = 4 * i;

				if (!KeepFullyTransparentBackground || alpha != 0) {
					image.Pixels[position] = (byte) (((255 - alpha) * backgroundColor.B + alpha * image.Pixels[position]) / 255f);
					image.Pixels[position + 1] = (byte) (((255 - alpha) * backgroundColor.G + alpha * image.Pixels[position + 1]) / 255f);
					image.Pixels[position + 2] = (byte) (((255 - alpha) * backgroundColor.R + alpha * image.Pixels[position + 2]) / 255f);
					image.Pixels[position + 3] = 255;
				}
			}
		}
	}
}
