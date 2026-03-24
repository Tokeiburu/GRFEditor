namespace GRF.Image.Decoders {
	public abstract class AbstractImageFormatConverter {
		private GrfColor _backgroundColor = GrfColors.White;
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

		protected virtual void _applyBackgroundColor(GrfImage image, in GrfColor backgroundColor) {
			uint bgB = backgroundColor.B;
			uint bgG = backgroundColor.G;
			uint bgR = backgroundColor.R;

			unsafe {
				fixed (byte* pBase = image.Pixels) {
					uint* p = (uint*)pBase;
					uint* pEnd = p + image.Pixels.Length / 4;

					if (KeepFullyTransparentBackground) {
						while (p < pEnd) {
							uint a = *p >> 24;

							if (a > 0) {
								uint pixel = *p;

								if (a == 255) {
									*p = pixel | 0xFF000000;
								}
								else {
									uint newB = ((((pixel & 0xFF) - bgB) * a) >> 8) + bgB;
									uint newG = (((((pixel >> 8) & 0xFF) - bgG) * a) >> 8) + bgG;
									uint newR = (((((pixel >> 16) & 0xFF) - bgR) * a) >> 8) + bgR;

									*p = 0xFF000000 | (newR << 16) | (newG << 8) | newB;
								}
							}

							p++;
						}
					}
					else {
						while (p < pEnd) {
							uint a = *p >> 24;
							uint pixel = *p;

							uint newB = ((((pixel & 0xFF) - bgB) * a) >> 8) + bgB;
							uint newG = (((((pixel >> 8) & 0xFF) - bgG) * a) >> 8) + bgG;
							uint newR = (((((pixel >> 16) & 0xFF) - bgR) * a) >> 8) + bgR;

							*p = 0xFF000000 | (newR << 16) | (newG << 8) | newB;
							p++;
						}
					}
				}
			}
		}
	}
}
