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
			int bgB = backgroundColor.B;
			int bgG = backgroundColor.G;
			int bgR = backgroundColor.R;

			unsafe {
				fixed (byte* pBase = image.Pixels) {
					byte* p = pBase;
					byte* pEnd = pBase + image.Pixels.Length;

					if (KeepFullyTransparentBackground) {
						while (p < pEnd) {
							byte a = p[3];
							int invA = 255 - a;
							if (a != 0) {
								p[0] = (byte)((invA * bgB + a * p[0]) / 255);
								p[1] = (byte)((invA * bgG + a * p[1]) / 255);
								p[2] = (byte)((invA * bgR + a * p[2]) / 255);
								p[3] = 255;
							}

							p += 4;
						}
					}
					else {
						while (p < pEnd) {
							byte a = p[3];
							int invA = 255 - a;
							p[0] = (byte)((invA * bgB + a * p[0]) / 255);
							p[1] = (byte)((invA * bgG + a * p[1]) / 255);
							p[2] = (byte)((invA * bgR + a * p[2]) / 255);
							p[3] = 255;
							p += 4;
						}
					}
				}
			}
		}
	}
}
