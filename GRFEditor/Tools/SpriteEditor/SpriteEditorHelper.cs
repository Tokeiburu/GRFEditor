using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.Image;
using GRF.Image.Decoders;

namespace GRFEditor.Tools.SpriteEditor {
	public static class SpriteEditorHelper {
		public static ImageSource MakeFirstPaletteColorTransparent(GrfImage source) {
			if (source == null)
				return null;

			if (source.GrfImageType != GrfImageType.Indexed8)
				throw new Exception("Invalid format, expected Indexed8");

			GrfImage image = source.Copy();
			image.Palette[3] = 0;
			image.Convert(new Bgra32FormatConverter());

			return image.Cast<BitmapSource>();
		}
	}
}