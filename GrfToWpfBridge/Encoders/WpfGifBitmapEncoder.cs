using System.Drawing;
using System.IO;
using GRF.Image;
using Gif.Components;

namespace GrfToWpfBridge.Encoders {
	public sealed class WpfGifBitmapEncoder : IWpfEncoder {
		#region IWpfEncoder Members

		public GrfImage Frame { get; set; }

		public void Save(Stream stream) {
			AnimatedGifEncoder e = new AnimatedGifEncoder();
			e.Start(stream as FileStream);
			e.SetRepeat(0);

			Color transparent = Color.FromArgb(255, 255, 0, 255);

			byte[] pixels = Frame.Pixels;
			byte[] palette = Imaging.GetBytePaletteRGBFromRGBA(Frame.Palette);

			e.SetTransparent(transparent);
			e.AddFrame(Frame.Width, Frame.Height, pixels, palette);

			e.Finish();
		}

		#endregion
	}
}