using GRF.Core;
using GRF.Image;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GrfToWpfBridge;

namespace GRFEditor.WPF.PreviewTabs.Controls {
	public static class GifFormat {
		public static GrfImage LoadAsGrfImage(FileEntry entry) {
			using (MemoryStream stream = new MemoryStream(entry.GetDecompressedData())) {
				GifBitmapDecoder decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

				int maxHeight = 0;
				int totalWidth = 0;

				for (int i = 0; i < decoder.Frames.Count; i++) {
					maxHeight = Math.Max(maxHeight, decoder.Frames[i].PixelHeight);
					totalWidth += decoder.Frames[i].PixelWidth;
				}

				GrfImage output = new GrfImage(new byte[maxHeight * totalWidth * 4], totalWidth, maxHeight, GrfImageType.Bgra32);

				int offsetX = 0;

				for (int i = 0; i < decoder.Frames.Count; i++) {
					var frame = decoder.Frames[i];

					byte[] pixels = new byte[frame.PixelHeight * frame.PixelWidth * frame.Format.BitsPerPixel / 8];
					frame.CopyPixels(pixels, frame.PixelWidth * frame.Format.BitsPerPixel / 8, 0);
					byte[] palette = new byte[frame.Palette.Colors.Count * 4];

					for (int k = 0; k < frame.Palette.Colors.Count; k++) {
						Color color = frame.Palette.Colors[k];
						palette[4 * k + 0] = color.R;
						palette[4 * k + 1] = color.G;
						palette[4 * k + 2] = color.B;
						palette[4 * k + 3] = color.A;
					}

					palette[3] = 0;
					GrfImage frameImage = new GrfImage(pixels, frame.PixelWidth, frame.PixelHeight, frame.Format.ToGrfImageType(), palette);
					frameImage.MakePinkShadeTransparent();
					output.SetPixels(offsetX, 0, frameImage);
					offsetX += decoder.Frames[i].PixelWidth;
				}

				return output;
			}
		}
	}
}
