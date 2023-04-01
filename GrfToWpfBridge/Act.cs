using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using Frame = GRF.FileFormats.ActFormat.Frame;

namespace GrfToWpfBridge {
	public static partial class Imaging {
		public static GrfImage Render(this Frame frame, Act act) {
			try {
				for (int i = 0; i < act.Sprite.NumberOfIndexed8Images; i++) {
					act.Sprite.Images[i].Palette[3] = 0;
				}

				ImageSource res = ActImaging.Imaging.GenerateFrameImage(act, frame);
				Image image = new Image { Source = res };

				image.Measure(new Size(res.Width, res.Height));
				image.Arrange(new Rect(0, 0, res.Width, res.Height));
				image.UpdateLayout();

				return ConvertToBitmapSource(image);
			}
			finally {
				for (int i = 0; i < act.Sprite.NumberOfIndexed8Images; i++) {
					act.Sprite.Images[i].Palette[3] = 255;
				}
			}
		}

		public static GrfImage ConvertToBitmapSource(UIElement element) {
			var target = new RenderTargetBitmap(
				(int) element.RenderSize.Width, (int) element.RenderSize.Height,
				96, 96, PixelFormats.Pbgra32);
			target.Render(element);

			var encoder = new PngBitmapEncoder();
			var outputFrame = BitmapFrame.Create(target);
			encoder.Frames.Add(outputFrame);

			using (MemoryStream stream = new MemoryStream()) {
				encoder.Save(stream);

				stream.Seek(0, SeekOrigin.Begin);
				byte[] imData = new byte[stream.Length];
				stream.Read(imData, 0, imData.Length);

				return new GrfImage(ref imData);
			}
		}
	}
}