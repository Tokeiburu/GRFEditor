using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GRF.FileFormats.SprFormat.Builder;
using GRF.Image;
using GRF.IO;
using GRF.GrfSystem;
using Utilities;

namespace GRF.FileFormats.SprFormat {
	internal class SprWriterConfig {
		/// <summary>
		/// Gets or sets the major version.
		/// </summary>
		public byte Major {
			get { return _major; }
			protected set {
				_major = value;
				_version = FormatConverters.DoubleConverter(Major + "." + Minor);
			}
		}

		/// <summary>
		/// Gets or sets the minor version.
		/// </summary>
		public byte Minor {
			get { return _minor; }
			protected set {
				_minor = value;
				_version = FormatConverters.DoubleConverter(Major + "." + Minor);
			}
		}

		public bool CloseStream { get; set; }
		public Stream OutputStream { get; set; }
		private double? _version = null;
		private byte _major;
		private byte _minor;

		public double Version {
			get {
				if (_version == null) {
					_version = FormatConverters.DoubleConverter(Major + "." + Minor);
				}

				return _version.Value;
			}
		}

		public SprWriterConfig(byte major, byte minor) {
			Major = major;
			Minor = minor;
		}

		public void SetVersion(byte major, byte minor) {
			Major = major;
			Minor = minor;
		}
	}

	public partial class Spr {
		public static bool AutomaticDowngradeOnRleException { get; set; }

		protected void _writeAsIndexed8(BinaryWriter writer, GrfImage image, int imageIndex, byte major, byte minor) {
			if (EnableImageSizeCheck && major >= 2 && minor >= 1)
				if (image.Width * image.Height > UInt16.MaxValue) throw new SprImageOverflowException(imageIndex, image.Width, image.Height);
			//if (image.Height > 256) throw new SprImageOverflowException(imageIndex, image.Width, image.Height);

			writer.Write((UInt16)image.Width);
			writer.Write((UInt16)image.Height);

			byte[] sourcePixels = image.Pixels;

			if (major >= 2 && minor >= 1) {
				sourcePixels = Rle.Compress(image.Pixels);

				if (sourcePixels.Length > UInt16.MaxValue) {
					throw new SprRleBufferOverflowException();
				}

				writer.Write((ushort)sourcePixels.Length);
			}

			writer.Write(sourcePixels);
		}

		protected void _writeAsBgra32(BinaryWriter writer, GrfImage image) {
			writer.Write((UInt16)image.Width);
			writer.Write((UInt16)image.Height);

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

		internal void Save(SprWriterConfig config) {
			BinaryWriter writer = _getWriterStream(config);

			try {
				RleSaveError = false;
				_save(writer, config);
			}
			catch (SprRleBufferOverflowException) {
				if (AutomaticDowngradeOnRleException) {
					RleSaveError = true;
					writer.BaseStream.Position = 0;
					writer.BaseStream.SetLength(0);
					config.SetVersion(2, 0);
					_save(writer, config);
				}
				else
					throw;
			}
			finally {
				if (config.CloseStream)
					writer.Close();
			}
		}

		private void _save(BinaryWriter writer, SprWriterConfig config) {
			Header.Write(writer);
			writer.Write(config.Minor);
			writer.Write(config.Major);

			if (NumberOfIndexed8Images > UInt16.MaxValue)
				throw new OverflowException("The number of indexed8 (palette) must be below " + UInt16.MaxValue);

			if (NumberOfBgra32Images > UInt16.MaxValue)
				throw new OverflowException("The number of indexed8 (palette) must be below " + UInt16.MaxValue);

			writer.Write((ushort)Images.Count(p => p.GrfImageType == GrfImageType.Indexed8));

			if (config.Version >= 2.0) {
				writer.Write((ushort)Images.Count(p => p.GrfImageType == GrfImageType.Bgra32));
			}

			for (int i = 0; i < NumberOfIndexed8Images; i++) {
				_writeAsIndexed8(writer, Images[i], i, config.Major, config.Minor);
			}

			if (config.Version >= 2.0) {
				for (int i = 0; i < NumberOfBgra32Images; i++) {
					_writeAsBgra32(writer, Images[i + NumberOfIndexed8Images]);
				}
			}

			if (NumberOfIndexed8Images > 0) {
				byte[] palette = new byte[1024];

				if (Palette == null) {
					// Compatibility fix
					Buffer.BlockCopy(Images[0].Palette, 0, palette, 0, 1024);
				}
				else {
					Buffer.BlockCopy(Palette.BytePalette, 0, palette, 0, 1024);
				}

				palette[3] = 255;
				writer.Write(palette);
			}
		}

		private BinaryWriter _getWriterStream(SprWriterConfig config) {
			return new BinaryWriter(config.OutputStream);
		}
	}
}
