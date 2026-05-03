using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using Frame = GRF.FileFormats.ActFormat.Frame;

namespace GrfToWpfBridge {
	public static partial class Imaging {
		public static GrfImage Render(this Frame frame, Act act, GrfImage guide = null) {
			var layers = frame.Layers.ToList();

			SpriteIndex idx = SpriteIndex.Null;
			var box = ActImaging.Imaging.GenerateBoundingBox(act, layers, ceilingAwayFromZero: false);

			if (guide != null) {
				idx = act.Sprite.InsertAny(guide);
				Layer layerGuide = new Layer(idx);
				layerGuide.OffsetX = (int)box.Center.X;
				layerGuide.OffsetY = (int)box.Center.Y;
				layers.Add(layerGuide);
			}

			for (int i = 0; i < act.Sprite.NumberOfIndexed8Images; i++) {
				act.Sprite.Images[i].Palette[3] = 0;
			}

			ImageSource res = ActImaging.Imaging.GenerateImage(act, layers);
			Image image = new Image { Source = res };

			image.Measure(new Size(res.Width, res.Height));
			image.Arrange(new Rect(0, 0, res.Width, res.Height));
			image.UpdateLayout();

			if (guide != null) {
				act.Sprite.Remove(idx);
			}

			return ConvertToGrfImage(image);
		}

		public static GrfImage ConvertToGrfImage(UIElement element) {
			int width = (int)Math.Ceiling(element.RenderSize.Width);
			int height = (int)Math.Ceiling(element.RenderSize.Height);

			var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
			rtb.Render(element);

			int stride = width * 4;
			byte[] pixels = new byte[height * stride];
			rtb.CopyPixels(pixels, stride, 0);

			var converted = new FormatConvertedBitmap(rtb, PixelFormats.Bgra32, null, 0);
			converted.CopyPixels(pixels, stride, 0);
			return new GrfImage(pixels, width, height, GrfImageType.Bgra32);
		}
	}
}