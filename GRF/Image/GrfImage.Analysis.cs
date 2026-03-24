using GRF.ContainerFormat;
using GRF.FileFormats.TgaFormat;
using System;
using System.Collections.Generic;
using Utilities;

namespace GRF.Image {
	public static class GrfImageAnalysis {
		public static byte[] PngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47 };
		public static byte[] BmpHeader = new byte[] { 0x42, 0x4d };
		public static byte[] JpgHeader = new byte[] { 0xff, 0xd8 };
		private static readonly Dictionary<GrfImage, List<int>> _bufferedSimilarities = new Dictionary<GrfImage, List<int>>();

		/// <summary>
		/// Clears the cache used for identifying the image similarities.
		/// </summary>
		public static void ClearBufferedData() {
			_bufferedSimilarities.Clear();
		}

		/// <summary>
		/// Compares two images and return the degrees of similarity between them.
		/// </summary>
		/// <param name="image1">The source image.</param>
		/// <param name="image2">The image to compare with.</param>
		/// <returns>The similarity ratio, between 0 and 1</returns>
		public static double SimilarityWith(GrfImage image1, GrfImage image2) {
			var data1 = CalculateSimilarityData(image1);
			var data2 = CalculateSimilarityData(image2);

			double similarity = 0;
			double factor = 1 / 6d;

			double maxPixels = Math.Max(image1.NumberOfPixels, image2.NumberOfPixels);
			double minPixels = Math.Min(image1.NumberOfPixels, image2.NumberOfPixels);

			double globalFactor = 1;

			if (maxPixels - minPixels > 0) {
				globalFactor = (1d - (maxPixels - minPixels) / maxPixels);
			}

			for (int i = 0; i < 6; i++) {
				double max = Math.Max(data1[i], data2[i]);

				if (max == 0)
					similarity += factor;
				else
					similarity += (1d - Math.Abs((data1[i] - data2[i]) / max)) * factor;
			}

			return similarity * globalFactor;
		}

		/// <summary>
		/// Computes the image similarity data.
		/// </summary>
		/// <param name="grfImage">The image.</param>
		/// <returns>A list of hue amount.</returns>
		public static List<int> CalculateSimilarityData(GrfImage grfImage) {
			if (!_bufferedSimilarities.ContainsKey(grfImage)) {
				List<int> huesCount = new List<int>(6) { 0, 0, 0, 0, 0, 0 };
				GrfColor toIgnore = grfImage.GrfImageType == GrfImageType.Indexed8 ? GrfColor.FromByteArray(grfImage.Palette, 0, GrfImageType.Indexed8) : default(GrfColor);

				foreach (GrfColor color in grfImage.Colors) {
					if (grfImage.GrfImageType == GrfImageType.Indexed8 && toIgnore.Equals(color)) {
						continue;
					}

					if (color.A == 0) continue;

					var hsvColor = color.Hsv;

					var hue = hsvColor.Hue * 360d + 30;

					int container = (int)(hue / 60) % 6;

					huesCount[container]++;
				}

				_bufferedSimilarities[grfImage] = huesCount;
			}

			return _bufferedSimilarities[grfImage];
		}

		/// <summary>
		/// Creates the transparency image using two composites.
		/// The black composite is the original image using a black background, while the white one uses a white background.
		/// </summary>
		/// <param name="blackComposite">The black composite.</param>
		/// <param name="whiteComposite">The white composite.</param>
		/// <returns></returns>
		public static GrfImage CreateTransparencyImage(GrfImage blackComposite, GrfImage whiteComposite) {
			if (blackComposite.GrfImageType != GrfImageType.Bgr24 && blackComposite.GrfImageType != GrfImageType.Bgr32)
				throw GrfExceptions.__UnsupportedImageFormat.Create();

			if (whiteComposite.GrfImageType != GrfImageType.Bgr24 && whiteComposite.GrfImageType != GrfImageType.Bgr32)
				throw GrfExceptions.__UnsupportedImageFormat.Create();

			if (whiteComposite.GrfImageType != blackComposite.GrfImageType)
				throw GrfExceptions.__UnsupportedImageFormat.Create();

			GrfImage result = new GrfImage(new byte[blackComposite.Width * blackComposite.Height * 4], blackComposite.Width, blackComposite.Height, GrfImageType.Bgra32);

			int bpp = blackComposite.GetBpp();
			int count = blackComposite.Pixels.Length / bpp;
			int idx = 0;
			int idxR = 0;

			for (int j = 0; j < count; j++) {
				byte transparency = 0;

				for (int i = 0; i < 3; i++) {
					transparency = Math.Max(transparency, (byte)(255 - whiteComposite.Pixels[idx + i] + blackComposite.Pixels[idx + i]));
				}

				if (transparency > 0) {
					for (int i = 0; i < 3; i++) {
						result.Pixels[idxR + i] = (byte)(Math.Round(255f * blackComposite.Pixels[idx + i] / transparency, MidpointRounding.AwayFromZero));
					}
				}

				result.Pixels[idxR + 3] = transparency;
				idx += bpp;
				idxR += 4;
			}

			return result;
		}

		public static GrfImageType GetGrfImageType(byte[] data) {
			if (data.Length > 3) {
				if (Methods.ByteArrayCompare(data, 0, 4, PngHeader, 0)) return GrfImageType.NotEvaluatedPng;
				if (Methods.ByteArrayCompare(data, 0, 2, BmpHeader, 0)) return GrfImageType.NotEvaluatedBmp;
				if (Methods.ByteArrayCompare(data, 0, 2, JpgHeader, 0)) return GrfImageType.NotEvaluatedJpg;

				// Might be a TGA, but... since TGAs don't have
				// have a header (sigh, that, is a bad design) we have
				// to try and partially read it.

				if (data.Length > TgaHeader.StructSize) {
					TgaHeader tgaHeader = new TgaHeader(data);

					if (tgaHeader.ValidateHeader()) {
						return GrfImageType.NotEvaluatedTga;
					}
				}

				return GrfImageType.NotEvaluatedJpg;
			}

			return GrfImageType.NotEvaluated;
		}
	}
}
