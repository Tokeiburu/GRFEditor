using System.Collections.Generic;
using ErrorManager;
using GRF.FileFormats.PalFormat;
using GRF.Image;
using GRF.IO;

namespace GRF.FileFormats.SprFormat {
	public partial class Spr {
		/// <summary>
		/// Gets the images.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="loadFirstImageOnly">if set to <c>true</c> [load first image only].</param>
		/// <returns>
		/// A list of the loaded images.
		/// </returns>
		internal List<GrfImage> GetImages(IBinaryReader reader, bool loadFirstImageOnly) {
// ReSharper disable CompareOfFloatsByEqualityOperator
			if (Header.Version != 1.0 && Header.Version != 2.0 && Header.Version != 2.1) {
// ReSharper restore CompareOfFloatsByEqualityOperator
				ErrorHandler.HandleException("Unsupported format, attempting version 2.1 : Major = " + Header.MajorVersion + " Minor = " + Header.MinorVersion, ErrorLevel.Low);
			}

			return _getImages(reader, loadFirstImageOnly);
		}

		protected List<GrfImage> _getImages(IBinaryReader reader, bool loadFirstImageOnly) {
			RleImages = new List<Rle>(NumberOfIndexed8Images);

			if (Header.Version >= 2.0) {
				reader.Position = 8;
			}
			else if (Header.Version >= 1.0) {
				reader.Position = 6;
			}
			else {
				reader.Position = 8;
			}

			int palOffset = NumberOfIndexed8Images > 0 ? reader.Length - 1024 : reader.Length;

			for (int i = 0; i < NumberOfIndexed8Images; i++) {
				if (reader.Position < palOffset) {
					_readAsIndexed8(RleImages, reader);
				}
				else {
					NumberOfIndexed8Images = i;
					break;
				}
			}

			for (int i = 0; i < NumberOfBgra32Images; i++) {
				if (reader.Position < palOffset) {
					_readAsBgra32(RleImages, reader);
				}
				else {
					NumberOfBgra32Images = i;
					break;
				}
			}

			List<GrfImage> imageSources = new List<GrfImage>();

			if (NumberOfIndexed8Images > 0) {
				byte[] pal = _loadPalette(reader);

				for (int i = 0; i < NumberOfIndexed8Images; i++) {
					imageSources.Add(_loadIndexed8Image(RleImages[i], pal));

					if (loadFirstImageOnly)
						return imageSources;
				}
			}

			for (int i = 0; i < NumberOfBgra32Images; i++) {
				imageSources.Add(_loadBgra32Image(RleImages[NumberOfIndexed8Images + i]));

				if (loadFirstImageOnly)
					return imageSources;
			}

			return imageSources;
		}

		protected virtual byte[] _loadPalette(IBinaryReader reader) {
			reader.Position = reader.Length - 1024;
			byte[] palette = reader.Bytes(1024);

			for (int i = 0; i < 256; i++) {
				palette[4 * i + 3] = 255;
			}

			palette[3] = 0;

			Palette = new Pal(palette, false);

			return palette;
		}

		protected void _readAsBgra32(List<Rle> rleImages, IBinaryReader reader) {
			int width = reader.UInt16();
			int height = reader.UInt16();
			byte[] frameData = reader.Bytes(width * height * 4);

			rleImages.Add(new Rle { FrameData = frameData, Height = height, Width = width });
		}

		protected void _readAsIndexed8(List<Rle> rleImages, IBinaryReader reader) {
			int width = reader.UInt16();
			int height = reader.UInt16();
			byte[] frameData;

			if (Header.Version >= 2.1) {
				frameData = reader.Bytes(reader.UInt16());
			}
			else {
				frameData = reader.Bytes(width * height);
			}

			rleImages.Add(new Rle { FrameData = frameData, Height = height, Width = width });
		}

		protected GrfImage _loadBgra32Image(Rle rleImage) {
			byte[] realData = new byte[rleImage.Width * rleImage.Height * 4];
			byte[] data = rleImage.FrameData;

			int height = rleImage.Height;
			int width = rleImage.Width;
			int index;
			int index2;

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					index = 4 * ((height - y - 1) * width + x);
					index2 = 4 * (width * y + x);
					realData[index2 + 0] = data[index + 1];
					realData[index2 + 1] = data[index + 2];
					realData[index2 + 2] = data[index + 3];
					realData[index2 + 3] = data[index + 0];
				}
			}

			return new GrfImage(ref realData, width, height, GrfImageType.Bgra32);
		}

		protected GrfImage _loadIndexed8Image(Rle rleImage, byte[] pal) {
			if (Header.Version >= 2.1) {
				byte[] realData = rleImage.Decompress();
				return new GrfImage(ref realData, rleImage.Width, rleImage.Height, GrfImageType.Indexed8, ref pal);
			}

			byte[] realDataArray = rleImage.FrameData;
			return new GrfImage(ref realDataArray, rleImage.Width, rleImage.Height, GrfImageType.Indexed8, ref pal);
		}
	}
}
