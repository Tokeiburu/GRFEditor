using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.Image;
using Utilities;

namespace GrfToWpfBridge.Encoders {
	public class BridgeEncoder {
		private BitmapEncoder _encoder;
		private IWpfEncoder _wpfEncoder;

		public void Set(BitmapEncoder encoder) {
			_encoder = encoder;
		}

		public void Set(IWpfEncoder encoder) {
			_wpfEncoder = encoder;
		}

		public void Save(string file) {
			using (FileStream stream = File.OpenWrite(file)) {
				if (_encoder != null)
					_encoder.Save(stream);
				else if (_wpfEncoder != null)
					_wpfEncoder.Save(stream);
			}
		}

		public static BridgeEncoder GetEncoder(PixelFormatInfo info) {
			BridgeEncoder encoder = new BridgeEncoder();

			if (info.Extension == ".tga") {
				encoder.Set(new TargaBitmapEncoder());
			}
			else if (info.Extension == ".gif") {
				encoder.Set(new WpfGifBitmapEncoder());
			}
			else {
				if (info.Format == PixelFormats.Indexed8 && !Methods.CanUseIndexed8) {
					encoder.Set(new Indexed8BmpBitmapEncoder());
				}
				else {
					switch (info.Extension) {
						case ".bmp":
							encoder.Set(new BmpBitmapEncoder());
							break;
						case ".png":
							encoder.Set(new PngBitmapEncoder());
							break;
						case ".jpg":
							encoder.Set(new JpegBitmapEncoder());
							break;
						case ".tga":
							encoder.Set(new TargaBitmapEncoder());
							break;
					}
				}
			}

			return encoder;
		}

		public void AddFrame(GrfImage image) {
			if (_encoder != null)
				_encoder.Frames.Add(BitmapFrame.Create(image.Cast<BitmapSource>()));
			else if (_wpfEncoder != null)
				_wpfEncoder.Frame = image;
		}
	}
}