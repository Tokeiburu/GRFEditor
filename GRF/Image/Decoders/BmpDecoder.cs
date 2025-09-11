using System;
using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities;

namespace GRF.Image.Decoders {
	public class BmpHeader {
		public string Magic { get; internal set; }
		public int Size { get; internal set; }
		public short Reserved1 { get; internal set; }
		public short Reserved2 { get; internal set; }
		public int OffsetBits { get; internal set; }

		public BmpHeader(ByteReader reader) {
			Magic = reader.String(2);
			Size = reader.Int32();
			Reserved1 = reader.Int16();
			Reserved2 = reader.Int16();
			OffsetBits = reader.Int32();
		}

		public BmpHeader() {
			Magic = "BM";
		}

		public void Save(BinaryWriter writer) {
			writer.Write(Encoding.ASCII.GetBytes(Magic));
			writer.Write(Size);
			writer.Write(Reserved1);
			writer.Write(Reserved2);
			writer.Write(OffsetBits);
		}
	}

	public enum DibCompression {
		BI_RGB = 0x0000,
		BI_RLE8 = 0x0001,
		BI_RLE4 = 0x0002,
		BI_BITFIELDS = 0x0003,
		BI_JPEG = 0x0004,
		BI_PNG = 0x0005,
		BI_CMYK = 0x000B,
		BI_CMYKRLE8 = 0x000C,
		BI_CMYKRLE4 = 0x000D
	}

	public class DibData {
		public uint DibSize { get; internal set; }
		public int Width { get; internal set; }
		public int Height { get; internal set; }
		public ushort Planes { get; internal set; }
		public ushort BitCount { get; internal set; }
		public DibCompression Compression { get; internal set; }
		public uint SizeImage { get; internal set; }
		public uint XPelsPerMeter { get; internal set; }
		public uint YPelsPerMeter { get; internal set; }
		public uint ClrUsed { get; internal set; }
		public uint ClrImportant { get; internal set; }

		public DibData(ByteReader reader) {
			DibSize = reader.UInt32();
			Width = reader.Int32();
			Height = reader.Int32();
			Planes = reader.UInt16();
			BitCount = reader.UInt16();
			Compression = (DibCompression)reader.UInt32();
			SizeImage = reader.UInt32();
			XPelsPerMeter = reader.UInt32();
			YPelsPerMeter = reader.UInt32();
			ClrUsed = reader.UInt32();
			ClrImportant = reader.UInt32();
		}

		public DibData() {
			DibSize = 40;
			Planes = 1;
			Compression = DibCompression.BI_RGB;
		}

		public void Save(BinaryWriter writer) {
			writer.Write(DibSize);
			writer.Write(Width);
			writer.Write(Height);
			writer.Write(Planes);
			writer.Write(BitCount);
			writer.Write((uint)Compression);
			writer.Write(SizeImage);
			writer.Write(XPelsPerMeter);
			writer.Write(YPelsPerMeter);
			writer.Write(ClrUsed);
			writer.Write(ClrImportant);
		}
	}

	public class BmpDecoder {
		public BmpHeader Header { get; private set; }
		public DibData Dib { get; private set; }

		private ByteReader _reader;

		public BmpDecoder(byte[] data) : this(new ByteReader(data)) {
		}

		public BmpDecoder(ByteReader reader) {
			Header = new BmpHeader(reader);

			if (Header.Magic != "BM")
				throw GrfExceptions.__FileFormatException.Create("BMP");

			Dib = new DibData(reader);
			_reader = reader;
		}

		public BmpDecoder() {
			Header = new BmpHeader();
			Dib = new DibData();
		}

		public GrfImage ToGrfImage() {
			_reader.Position = 54;
			int bpp = -1;
			byte[] palette = null;
			GrfImageType type = GrfImageType.NotEvaluated;

			switch(Dib.BitCount) {
				case 8:	// Convert from RGB to BGRA palette format
					palette = new byte[1024];

					if (Header.OffsetBits < _reader.Position)
						throw GrfExceptions.__InvalidImageFormat.Create();

					byte[] srcPalette = new byte[(Dib.ClrImportant == 0 ? Dib.BitCount * 32 : (int)Dib.ClrImportant) * 4];
					_reader.Bytes(srcPalette, 0, srcPalette.Length);

					for (int i = 0; i < 1024; i += 4) {
						if (i < srcPalette.Length) {
							palette[i + 0] = srcPalette[i + 2];
							palette[i + 1] = srcPalette[i + 1];
							palette[i + 2] = srcPalette[i + 0];
						}

						palette[i + 3] = 255;
					}

					type = GrfImageType.Indexed8;
					bpp = 1;
					break;
				case 16:
				case 24:
					type = GrfImageType.Bgr24;
					bpp = 3;
					break;
				case 32:
					type = GrfImageType.Bgr32;
					bpp = 4;
					break;
				default:
					throw GrfExceptions.__InvalidImageFormat.Create();
			}

			byte[] pixels = new byte[bpp * Dib.Width * Math.Abs(Dib.Height)];

			_reader.Position = Header.OffsetBits;

			if (type == GrfImageType.NotEvaluated) {
				throw GrfExceptions.__InvalidImageFormat.Create();
			}

			switch(Dib.Compression) {
				case DibCompression.BI_RGB:
					int padding = (4 - (Dib.Width * bpp) % 4) % 4;
					int stride = Dib.Width * bpp;

					if (Dib.BitCount == 16) {	// Convert to bgr24
						byte[] data = new byte[2];
						int offset = 0;

						for (int y = 0; y < Dib.Height; y++) {
							offset = (Dib.Height - y - 1) * stride;

							for (int x = 0; x < Dib.Width; x++, offset += bpp) {
								try {
									if (_reader.LengthLong - _reader.PositionLong < 2)
										break;

									_reader.Bytes(data, 0, 2);

									pixels[offset + 0] = (byte)((data[0] << 3) & 0xF8);
									pixels[offset + 1] = (byte)((data[1] << 6) & 0xC0 | (data[0] >> 2) & 0x38);
									pixels[offset + 2] = (byte)((data[1] << 1) & 0xF8);
								}
								catch (Exception err) {
									Z.F(err);
								}
							}

							if (padding > 0)
								_reader.Forward(padding);
						}

						return new GrfImage(ref pixels, Dib.Width, Math.Abs(Dib.Height), type, ref palette);
					}

					for (int y = 0; y < Dib.Height; y++) {
						_reader.Bytes(pixels, (Dib.Height - y - 1) * stride, stride);

						if (padding > 0)
							_reader.Forward(padding);
					}

					return new GrfImage(ref pixels, Dib.Width, Math.Abs(Dib.Height), type, ref palette);
				default:
					throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}

		public static void Save(GrfImage image, Stream stream) {
			BmpDecoder decoder = new BmpDecoder();
			int bpp = image.GetBpp();
			int width_padding = (4 - ((image.Width * bpp) % 4)) % 4;
			int stride = image.Width * bpp;
			var writer = new BinaryWriter(stream);

			switch (image.GrfImageType) {
				case GrfImageType.Bgr32:
				case GrfImageType.Bgr24:
					decoder.Dib.Width = image.Width;
					decoder.Dib.Height = image.Height;
					decoder.Dib.BitCount = (ushort)(bpp * 8);
					decoder.Dib.SizeImage = (uint)((image.Width * bpp + width_padding) * image.Height);
					decoder.Header.OffsetBits = 14 + 40;
					decoder.Header.Size = decoder.Header.OffsetBits + (int)decoder.Dib.SizeImage;

					decoder.Header.Save(writer);
					decoder.Dib.Save(writer);

					for (int y = 0; y < decoder.Dib.Height; y++) {
						writer.Write(image.Pixels, (decoder.Dib.Height - y - 1) * stride, stride);

						if (width_padding > 0) {
							writer.Write(new byte[width_padding], 0, width_padding);
						}
					}

					break;
				case GrfImageType.Indexed8:
					decoder.Dib.Width = image.Width;
					decoder.Dib.Height = image.Height;
					decoder.Dib.BitCount = (ushort)(bpp * 8);
					decoder.Dib.SizeImage = (uint)((image.Width * bpp + width_padding) * image.Height);
					decoder.Dib.ClrImportant = 256;
					decoder.Dib.ClrUsed = 256;
					decoder.Header.OffsetBits = 14 + 40 + 1024;
					decoder.Header.Size = decoder.Header.OffsetBits + (int)decoder.Dib.SizeImage;

					decoder.Header.Save(writer);
					decoder.Dib.Save(writer);

					for (int off = 0; off < 1024; off += 4) {
						writer.Write(image.Palette[off + 2]);
						writer.Write(image.Palette[off + 1]);
						writer.Write(image.Palette[off + 0]);
						writer.Write((byte)0);
					}

					for (int y = 0; y < decoder.Dib.Height; y++) {
						writer.Write(image.Pixels, (decoder.Dib.Height - y - 1) * stride, stride);

						if (width_padding > 0) {
							writer.Write(new byte[width_padding], 0, width_padding);
						}
					}

					break;
				default:
					return;
				//throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}

		public static void Save(GrfImage image, string path) {
			using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)) {
				Save(image, stream);
			}
		}
	}
}
