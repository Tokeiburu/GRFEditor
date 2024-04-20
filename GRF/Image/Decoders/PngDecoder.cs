/*
namespace GRF.Image.Decoders {
	public class PngHeader {
		public byte[] Magic { get; internal set; }

		public PngHeader(ByteReader reader) {
			Magic = reader.Bytes(8);

			if (NativeMethods.memcmp(Magic, new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00 }, 8) != 0)
				throw GrfExceptions.__FileFormatException.Create("PNG");
		}

		public PngHeader() {
			Magic = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00 };
		}

		public void Save(BinaryWriter writer) {
			writer.Write(Magic);
		}
	}

	public class ChunkHeader {
		public string Name { get; private set; }
		public int StreamPosition { get; private set; }
		public int Length { get; private set; }

		public bool IsCritical {
			get { return char.IsUpper(Name[0]); }
		}

		public bool IsPublic {
			get { return char.IsUpper(Name[1]); }
		}

		public bool IsSafeToCopy {
			get { return char.IsUpper(Name[3]); }
		}

		public ChunkHeader(ByteReader reader) {
			StreamPosition = reader.Position;
			Length = reader.Int32BigEndian();
			Name = reader.String(4);
		}

		public ChunkHeader() {
		}

		public void Save(BinaryWriter writer) {
			writer.Write(Length);
			writer.Write(Name);
		}
	}

	//public enum DibCompression {
	//	BI_RGB = 0x0000,
	//	BI_RLE8 = 0x0001,
	//	BI_RLE4 = 0x0002,
	//	BI_BITFIELDS = 0x0003,
	//	BI_JPEG = 0x0004,
	//	BI_PNG = 0x0005,
	//	BI_CMYK = 0x000B,
	//	BI_CMYKRLE8 = 0x000C,
	//	BI_CMYKRLE4 = 0x000D
	//}
	//
	public class IhdrData {
		public int Width { get; internal set; }
		public int Height { get; internal set; }
		public byte BitDepth { get; internal set; }
		public ColorType ColorType { get; internal set; }
		public byte CompressionMethod { get; internal set; }
		public FilterType FilterMethod { get; internal set; }
		public InterlaceMethod InterlaceMethod { get; internal set; }
		public int CRC { get; internal set; }
	
		public IhdrData(ByteReader reader) {
			Width = reader.Int32BigEndian();
			Height = reader.Int32BigEndian();
			BitDepth = reader.Byte();
			ColorType = (ColorType)reader.Byte();
			CompressionMethod = reader.Byte();
			FilterMethod = (FilterType)reader.Byte();
			InterlaceMethod = (InterlaceMethod)reader.Byte();
			CRC = reader.Int32();
		}

		public IhdrData() {
			//DibSize = 40;
			//Planes = 1;
			//Compression = DibCompression.BI_RGB;
		}
	
		public void Save(BinaryWriter writer) {
			//writer.Write(DibSize);
			//writer.Write(Width);
			//writer.Write(Height);
			//writer.Write(Planes);
			//writer.Write(BitCount);
			//writer.Write((uint)Compression);
			//writer.Write(SizeImage);
			//writer.Write(XPelsPerMeter);
			//writer.Write(YPelsPerMeter);
			//writer.Write(ClrUsed);
			//writer.Write(ClrImportant);
		}
	}

    [Flags]
    public enum ColorType : byte
    {
        /// <summary>
        /// Grayscale.
        /// </summary>
        None = 0,
        /// <summary>
        /// Colors are stored in a palette rather than directly in the data.
        /// </summary>
        PaletteUsed = 1,
        /// <summary>
        /// The image uses color.
        /// </summary>
        ColorUsed = 2,
        /// <summary>
        /// The image has an alpha channel.
        /// </summary>
        AlphaChannelUsed = 4
    }

	public class PngDecoder {
		public PngHeader Header { get; private set; }
		public IhdrData Ihdr { get; private set; }

		private ByteReader _reader;

		public PngDecoder(byte[] data) : this(new ByteReader(data)) {
		}

		public PngDecoder(ByteReader reader) {
			Header = new PngHeader(reader);
			ChunkHeader ihdrChunk = new ChunkHeader(reader);

			if (ihdrChunk.Name != "IHDR")
				throw GrfExceptions.__FileFormatException.Create("IHDR");

			Ihdr = new IhdrData(reader);

			var hasEncounteredImageEnd = false;
			byte[] palette = null;
			byte[] crc = new byte[4];

			//DisallowTrailingData
			using (var output = new MemoryStream()) {
				using (var memoryStream = new MemoryStream()) {
					ChunkHeader header;

					while (TryReadChunkHeader(reader, out header)) {
						if (hasEncounteredImageEnd) {
                            break;
                        }

						var bytes = new byte[header.Length];
						reader.Bytes(bytes, 0, bytes.Length);

						if (header.IsCritical) {
							switch(header.Name) {
								case "PLTE":
									if (header.Length % 3 != 0) {
										throw new InvalidOperationException("Palette data must be multiple of 3, got " + header.Length + ".");
									}

									// Ignore palette data unless the header.ColorType indicates that the image is paletted.
									if ((Ihdr.ColorType & ColorType.PaletteUsed) == ColorType.PaletteUsed) {
										int off = 0;
										palette = new byte[bytes.Length / 3 * 4];

										for (int i = 0; i < bytes.Length; i += 3) {
											palette[off++] = bytes[i + 2];
											palette[off++] = bytes[i + 1];
											palette[off++] = bytes[i + 0];
											palette[off++] = 255;
										}
									}

									break;
								case "IDAT":
									memoryStream.Write(bytes, 0, bytes.Length);
									break;
								case "IEND":
									hasEncounteredImageEnd = true;
									break;
								default:
									throw new NotSupportedException("Encountered critical header {header} which was not recognised.");
							}
						}
						else {
							switch(header.Name) {
								case "tRNS":
									// Add transparency to palette, if the PLTE chunk has been read.
									if (palette != null) {
										for (int i = 0; i < palette.Length; i += 4) {
											palette[i + 3] = 255;
										}
									}

									break;
							}
						}

						byte[] crcData = new byte[4 + bytes.Length];

						reader.Position = header.StreamPosition + 4;
						reader.Bytes(crcData, 0, crcData.Length);
						reader.Bytes(crc, 0, 4);

                        var result = (int)Crc32.Compute(crcData);
                        var crcActual = (crc[0] << 24) + (crc[1] << 16) + (crc[2] << 8) + crc[3];

						if (result != crcActual) {
							throw new InvalidOperationException("CRC calculated " + result + " did not match file " + crcActual + " for chunk: " + header.Name + ".");
						}
					}

					memoryStream.Flush();
					memoryStream.Seek(2, SeekOrigin.Begin);

					using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress)) {
						deflateStream.CopyTo(output);
						deflateStream.Close();
					}
				}

				var bytesOut = output.ToArray();
				byte bytesPerPixel;
				byte samplesPerPixel;

				PngDataDecoder.GetBytesAndSamplesPerPixel(Ihdr, out bytesPerPixel, out samplesPerPixel);
				bytesOut = PngDataDecoder.Decode(bytesOut, Ihdr, bytesPerPixel, samplesPerPixel);

				Z.F();
				//return new Png(imageHeader, new RawPngData(bytesOut, bytesPerPixel, palette, imageHeader), palette?.HasAlphaValues ?? false);
			}

			_reader = reader;
		}

		public PngDecoder() {
			Header = new PngHeader();
			//Dib = new DibData();
		}

		private bool TryReadChunkHeader(ByteReader reader, out ChunkHeader header) {
			if (reader.Length - reader.Position >= 8) {
				header = new ChunkHeader(reader);
				return true;
			}

			header = null;
			return false;
		}

		public GrfImage ToGrfImage() {
			//_reader.Position = 54;
			//int bpp = -1;
			//byte[] palette = null;
			//GrfImageType type = GrfImageType.NotEvaluated;
			//
			//switch(Dib.BitCount) {
			//	case 8:	// Convert from RGB to BGRA palette format
			//		palette = new byte[1024];
			//
			//		if (Header.OffsetBits < _reader.Position)
			//			throw GrfExceptions.__InvalidImageFormat.Create();
			//
			//		byte[] srcPalette = new byte[(Dib.ClrImportant == 0 ? Dib.BitCount * 32 : (int)Dib.ClrImportant) * 4];
			//		_reader.Bytes(srcPalette, 0, srcPalette.Length);
			//
			//		for (int i = 0; i < 1024; i += 4) {
			//			if (i < srcPalette.Length) {
			//				palette[i + 0] = srcPalette[i + 2];
			//				palette[i + 1] = srcPalette[i + 1];
			//				palette[i + 2] = srcPalette[i + 0];
			//			}
			//
			//			palette[i + 3] = 255;
			//		}
			//
			//		type = GrfImageType.Indexed8;
			//		bpp = 1;
			//		break;
			//	case 16:
			//	case 24:
			//		type = GrfImageType.Bgr24;
			//		bpp = 3;
			//		break;
			//	case 32:
			//		type = GrfImageType.Bgr32;
			//		bpp = 4;
			//		break;
			//	default:
			//		throw GrfExceptions.__InvalidImageFormat.Create();
			//}
			//
			//byte[] pixels = new byte[bpp * Dib.Width * Math.Abs(Dib.Height)];
			//
			//_reader.Position = Header.OffsetBits;
			//
			//if (type == GrfImageType.NotEvaluated) {
			//	throw GrfExceptions.__InvalidImageFormat.Create();
			//}
			//
			//switch(Dib.Compression) {
			//	case DibCompression.BI_RGB:
			//		int padding = (4 - (Dib.Width * bpp) % 4) % 4;
			//		int stride = Dib.Width * bpp;
			//
			//		if (Dib.BitCount == 16) {	// Convert to bgr24
			//			byte[] data = new byte[2];
			//			int offset = 0;
			//
			//			for (int y = 0; y < Dib.Height; y++) {
			//				offset = (Dib.Height - y - 1) * stride;
			//
			//				for (int x = 0; x < Dib.Width; x++, offset += bpp) {
			//					_reader.Bytes(data, 0, 2);
			//
			//					pixels[offset + 0] = (byte)((data[0] << 3) & 0xF8);
			//					pixels[offset + 1] = (byte)((data[1] << 6) & 0xC0 | (data[0] >> 2) & 0x38);
			//					pixels[offset + 2] = (byte)((data[1] << 1) & 0xF8);
			//				}
			//
			//				if (padding > 0)
			//					_reader.Forward(padding);
			//			}
			//
			//			return new GrfImage(ref pixels, (int)Dib.Width, Math.Abs(Dib.Height), type, ref palette);
			//		}
			//
			//		for (int y = 0; y < Dib.Height; y++) {
			//			_reader.Bytes(pixels, (Dib.Height - y - 1) * stride, stride);
			//
			//			if (padding > 0)
			//				_reader.Forward(padding);
			//		}
			//
			//		return new GrfImage(ref pixels, (int)Dib.Width, Math.Abs(Dib.Height), type, ref palette);
			//	default:
			//		throw GrfExceptions.__InvalidImageFormat.Create();
			//}

			return null;
		}

		public static void Save(GrfImage image, string path) {
			BmpDecoder decoder = new BmpDecoder();
			int bpp = image.GetBpp();
			int width_padding = (4 - ((image.Width * bpp) % 4)) % 4;
			int stride = image.Width * bpp;

			switch(image.GrfImageType) {
				case GrfImageType.Bgr32:
				case GrfImageType.Bgr24:
					decoder.Dib.Width = image.Width;
					decoder.Dib.Height = image.Height;
					decoder.Dib.BitCount = (ushort)(bpp * 8);
					decoder.Dib.SizeImage = (uint)((image.Width * bpp + width_padding) * image.Height);
					decoder.Header.OffsetBits = 14 + 40;
					decoder.Header.Size = decoder.Header.OffsetBits + (int)decoder.Dib.SizeImage;

					using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
						decoder.Header.Save(writer);
						decoder.Dib.Save(writer);

						for (int y = 0; y < decoder.Dib.Height; y++) {
							writer.Write(image.Pixels, (decoder.Dib.Height - y - 1) * stride, stride);

							if (width_padding > 0) {
								writer.Write(new byte[width_padding], 0, width_padding);
							}
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

					using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
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
					}

					break;
				default:
					return;
					//throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}
	}
}
*/