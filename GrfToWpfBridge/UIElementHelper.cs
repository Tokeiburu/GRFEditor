using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GrfToWpfBridge {
	public static class UIElementHelper {
		public static void Save(this UIElement source, string path) {
			double actualHeight = source.RenderSize.Height;
			double actualWidth = source.RenderSize.Width;

			RenderTargetBitmap renderTarget = new RenderTargetBitmap((int) actualWidth, (int) actualHeight, 96, 96, PixelFormats.Pbgra32);
			VisualBrush sourceBrush = new VisualBrush(source);

			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			using (drawingContext) {
				drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
			}
			renderTarget.Render(drawingVisual);

			PngBitmapEncoder enc = new PngBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(renderTarget));

			using (FileStream writer = new FileStream(path, FileMode.Create, FileAccess.Write)) {
				enc.Save(writer);
			}
		}

		public static void Save(this UIElement source, string path, Rect location) {
			RenderTargetBitmap renderTarget = new RenderTargetBitmap((int) location.Width, (int) location.Height, 96, 96, PixelFormats.Pbgra32);
			VisualBrush sourceBrush = new VisualBrush(source);

			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			using (drawingContext) {
				drawingContext.DrawRectangle(sourceBrush, null, location);
			}
			renderTarget.Render(drawingVisual);

			PngBitmapEncoder enc = new PngBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(renderTarget));

			using (FileStream writer = new FileStream(path, FileMode.Create, FileAccess.Write)) {
				enc.Save(writer);
			}
		}
	}
}