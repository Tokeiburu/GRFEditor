using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat.Builder;
using GRF.IO;
using GRF.Image;
using GRF.System;

namespace GRF.FileFormats.SprFormat {
	public class SprAbstract {
		protected readonly SprHeader _header;

		static SprAbstract() {
			TemporaryFilesManager.UniquePattern(Process.GetCurrentProcess().Id + "_spr_writer_{0:0000}.spr");
		}

		protected SprAbstract(SprHeader header) {
			_header = header;
		}

		/// <summary>
		/// Gets the images.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="loadFirstImageOnly">if set to <c>true</c> [load first image only].</param>
		/// <returns>
		/// A list of the loaded images.
		/// </returns>
		public List<GrfImage> GetImages(Spr spr, IBinaryReader reader, bool loadFirstImageOnly) {
			return _getImages(spr, reader, loadFirstImageOnly);
		}

		protected List<GrfImage> _getImages(Spr spr, IBinaryReader reader, bool loadFirstImageOnly) {
			spr.RleImages = new List<Rle>(spr.NumberOfIndexed8Images);

			if (spr.Header.IsCompatibleWith(2, 0)) {
				reader.Position = 8;
			}
			else if (spr.Header.IsCompatibleWith(1, 0)) {
				reader.Position = 6;
			}
			else {
				reader.Position = 8;
			}

			int palOffset = reader.Length - 1024;

			for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
				if (reader.Position < palOffset) {
					_readAsIndexed8(spr.RleImages, reader);
				}
				else {
					spr.NumberOfIndexed8Images = i;
					break;
				}
			}

			for (int i = 0; i < spr.NumberOfBgra32Images; i++) {
				if (reader.Position < palOffset) {
					_readAsBgra32(spr.RleImages, reader);
				}
				else {
					spr.NumberOfBgra32Images = i;
					break;
				}
			}

			List<GrfImage> imageSources = new List<GrfImage>();

			if (spr.NumberOfIndexed8Images > 0) {
				byte[] pal = _loadPalette(spr, reader);

				for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
					imageSources.Add(_loadIndexed8Image(spr.RleImages[i], pal));

					if (loadFirstImageOnly)
						return imageSources;
				}
			}

			for (int i = 0; i < spr.NumberOfBgra32Images; i++) {
				imageSources.Add(_loadBgra32Image(spr.RleImages[spr.NumberOfIndexed8Images + i]));

				if (loadFirstImageOnly)
					return imageSources;
			}

			return imageSources;
		}

		protected virtual byte[] _loadPalette(Spr spr, IBinaryReader reader) {
			reader.Position = reader.Length - 1024;
			byte[] palette = reader.Bytes(1024);

			for (int i = 0; i < 256; i++) {
				palette[4 * i + 3] = 255;
			}

			palette[3] = 0;

			spr.Palette = new Pal(palette, false);

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

			if (_header.IsCompatibleWith(2, 1)) {
				frameData = reader.Bytes(reader.UInt16());
			}
			else {
				frameData = reader.Bytes(width * height);
			}

			rleImages.Add(new Rle { FrameData = frameData, Height = height, Width = width });
		}

		protected void _writeAsIndexed8(BinaryWriter writer, GrfImage image, int imageIndex, byte major, byte minor) {
			if (Spr.EnableImageSizeCheck)
				if (image.Width * image.Height > UInt16.MaxValue) throw new SprImageOverflowException(imageIndex, image.Width, image.Height);
			//if (image.Height > 256) throw new SprImageOverflowException(imageIndex, image.Width, image.Height);

			writer.Write((UInt16) image.Width);
			writer.Write((UInt16) image.Height);

			byte[] sourcePixels = image.Pixels;

			if (major >= 2 && minor >= 1) {
				sourcePixels = Rle.Compress(image.Pixels);

				if (sourcePixels.Length > UInt16.MaxValue) {
					throw new SprRleBufferOverflowException();
				}

				writer.Write((ushort) sourcePixels.Length);
			}

			writer.Write(sourcePixels);
		}

		protected void _writeAsBgra32(BinaryWriter writer, GrfImage image) {
			writer.Write((UInt16) image.Width);
			writer.Write((UInt16) image.Height);

			byte[] sourcePixels = image.Pixels;
			byte[] realPixels = new byte[image.Width * image.Height * 4];
			int offset = 0;

			for (int y = 0; y < image.Height; y++) {
				for (int x = 0; x < image.Width; x++) {
					int index = 4 * ((image.Height - y - 1) * image.Width + x);

					realPixels[index + 1] = sourcePixels[offset++];
					realPixels[index + 2] = sourcePixels[offset++];
					realPixels[index + 3] = sourcePixels[offset++];
					realPixels[index + 0] = sourcePixels[offset++];
				}
			}

			writer.Write(realPixels);
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
			if (_header.MajorVersion >= 2 && _header.MinorVersion >= 1) {
				byte[] realData = rleImage.Decompress();
				return new GrfImage(ref realData, rleImage.Width, rleImage.Height, GrfImageType.Indexed8, ref pal);
			}

			byte[] realDataArray = rleImage.FrameData;
			return new GrfImage(ref realDataArray, rleImage.Width, rleImage.Height, GrfImageType.Indexed8, ref pal);
		}

		protected void _saveAs(Spr spr, string filename, byte major, byte minor) {
			string tempFile = TemporaryFilesManager.GetTemporaryFilePath(Process.GetCurrentProcess().Id + "_spr_writer_{0:0000}.spr");

			using (BinaryWriter writer = new BinaryWriter(File.Create(tempFile))) {
				spr.Header.Write(writer);
				writer.Write(minor);
				writer.Write(major);

				if (spr.NumberOfIndexed8Images > UInt16.MaxValue)
					throw new OverflowException("The number of indexed8 (palette) must be below " + UInt16.MaxValue);

				if (spr.NumberOfBgra32Images > UInt16.MaxValue)
					throw new OverflowException("The number of indexed8 (palette) must be below " + UInt16.MaxValue);

				writer.Write((ushort) spr.Images.Count(p => p.GrfImageType == GrfImageType.Indexed8));
				writer.Write((ushort) spr.Images.Count(p => p.GrfImageType == GrfImageType.Bgra32));

				for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
					_writeAsIndexed8(writer, spr.Images[i], i, major, minor);
				}

				for (int i = 0; i < spr.NumberOfBgra32Images; i++) {
					_writeAsBgra32(writer, spr.Images[i + spr.NumberOfIndexed8Images]);
				}

				if (spr.NumberOfIndexed8Images > 0) {
					byte[] palette = new byte[1024];

					if (spr.Palette == null) {
						// Compatibility fix
						Buffer.BlockCopy(spr.Images[0].Palette, 0, palette, 0, 1024);
					}
					else {
						Buffer.BlockCopy(spr.Palette.BytePalette, 0, palette, 0, 1024);
					}

					palette[3] = 255;
					writer.Write(palette);
				}
			}

			GrfPath.Delete(filename);
			File.Copy(tempFile, filename);
			GrfPath.Delete(tempFile);
		}

		protected void _saveAs(Spr spr, Stream stream, byte major, byte minor, bool close) {
			BinaryWriter writer = new BinaryWriter(stream);

			try {
				spr.Header.Write(writer);
				writer.Write(minor);
				writer.Write(major);

				if (spr.NumberOfIndexed8Images > UInt16.MaxValue)
					throw new OverflowException("The number of indexed8 (palette) must be below " + UInt16.MaxValue);

				if (spr.NumberOfBgra32Images > UInt16.MaxValue)
					throw new OverflowException("The number of indexed8 (palette) must be below " + UInt16.MaxValue);

				writer.Write((ushort) spr.Images.Count(p => p.GrfImageType == GrfImageType.Indexed8));
				writer.Write((ushort) spr.Images.Count(p => p.GrfImageType == GrfImageType.Bgra32));

				for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
					_writeAsIndexed8(writer, spr.Images[i], i, major, minor);
				}

				for (int i = 0; i < spr.NumberOfBgra32Images; i++) {
					_writeAsBgra32(writer, spr.Images[i + spr.NumberOfIndexed8Images]);
				}

				if (spr.NumberOfIndexed8Images > 0) {
					byte[] palette = new byte[1024];

					if (spr.Palette == null) {
						// Compatibility fix
						Buffer.BlockCopy(spr.Images[0].Palette, 0, palette, 0, 1024);
					}
					else {
						Buffer.BlockCopy(spr.Palette.BytePalette, 0, palette, 0, 1024);
					}

					palette[3] = 255;
					writer.Write(palette);
				}
			}
			finally {
				if (close)
					writer.Close();
			}
		}
	}
}