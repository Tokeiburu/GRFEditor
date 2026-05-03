using System;
using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;

namespace GRF.Image.Decoders {
	public class BmpHeader {
		public string Magic { get; internal set; }
		public int Size { get; internal set; }
		public short Reserved1 { get; internal set; }
		public short Reserved2 { get; internal set; }
		public int OffsetBits { get; internal set; }

		public BmpHeader(ByteReader reader) {
			if (reader.LengthLong - reader.PositionLong < 14)
				throw GrfExceptions.__InvalidImageFormat.Create();

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
		public const int MAX_HEIGHT = 60000;
		public const int MAX_WIDTH = 60000;
		public uint[] RgbaMask;

		public DibData(ByteReader reader) {
			if (reader.LengthLong - reader.PositionLong < 40)
				throw GrfExceptions.__InvalidImageFormat.Create();

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

			if (Compression == DibCompression.BI_BITFIELDS) {
				if (reader.LengthLong - reader.PositionLong < 16)
					throw GrfExceptions.__InvalidImageFormat.Create();

				RgbaMask = new uint[4];

				RgbaMask[0] = reader.UInt32();
				RgbaMask[1] = reader.UInt32();
				RgbaMask[2] = reader.UInt32();

				if (DibSize >= 56) {
					RgbaMask[3] = reader.UInt32();
				}
			}
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
			byte[] palette = _readPalette();
			(int bpp, GrfImageType type) = _retrieveOutputFormat();

			if (Header.OffsetBits >= _reader.LengthLong)
				throw GrfExceptions.__InvalidImageFormat.Create();

			_reader.Position = Header.OffsetBits;

			switch(Dib.Compression) {
				case DibCompression.BI_RGB:
					switch (Dib.BitCount) {
						case 1:
							return _readBI_RGB_1(bpp, type, palette);
						case 4:
							return _readBI_RGB_4(bpp, type, palette);
						case 16:
							return _readBI_RGB_16(bpp, type, palette);
						case 8:
						case 24:
						case 32:
							return _readBI_RGB_24(bpp, type, palette);
						default:
							throw GrfExceptions.__InvalidImageFormat.Create();
					}
				case DibCompression.BI_BITFIELDS:
					return _readBitFields(bpp, type, palette);
				case DibCompression.BI_RLE4:
					return _readRle4(bpp, type, palette);
				case DibCompression.BI_RLE8:
					return _readRle8(bpp, type, palette);
				default:
					throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}

		private GrfImage _readRle8(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			if (Dib.BitCount != 8)
				throw GrfExceptions.__InvalidImageFormat.Create();

			int width = Dib.Width;
			int height = _dstHeight;
			int x = 0;
			int y = 0;

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* p = pSrcBase + _reader.PositionLong;
					byte* end = pSrcBase + _reader.LengthLong;

					while (p < end) {
						byte count = *p++;
						if (p >= end) break;

						byte value = *p++;

						if (count > 0) {
							// Encoded mode
							for (int i = 0; i < count; i++) {
								if (x < width && y < height) {
									int dstY = _flipImage ? y : (height - y - 1);
									_pixels[dstY * _dstStride + x] = value;
								}
								x++;
							}
						}
						else {
							// Escape mode
							switch (value) {
								case 0: // End of line
									x = 0;
									y++;
									break;
								case 1:
									return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);

								case 2: { // Delta
										if (p + 2 > end)
											return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);

										byte dx = *p++;
										byte dy = *p++;

										x += dx;
										y += dy;
										break;
									}

								default: {
										// Absolute mode
										int n = value;

										if (p + n > end)
											return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);

										for (int i = 0; i < n; i++) {
											if (x < width && y < height) {
												int dstY = _flipImage ? y : (height - y - 1);
												_pixels[dstY * _dstStride + x] = p[i];
											}
											x++;
										}

										p += n;

										// WORD alignment
										if ((n & 1) == 1)
											p++;

										break;
									}
							}
						}

						if (y >= height)
							break;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private GrfImage _readRle4(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			if (Dib.BitCount != 4)
				throw GrfExceptions.__InvalidImageFormat.Create();

			int width = Dib.Width;
			int height = _dstHeight;

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* pBase = pSrcBase + _reader.PositionLong;
					byte* pSrcEnd = pSrcBase + _reader.LengthLong;
					byte* p = pBase;
					int x = 0;
					int y = 0;

					while (p < pSrcEnd) {
						byte count = *p++;

						if (p >= pSrcEnd)
							break;

						byte value = *p++;

						if (count > 0) {
							// Encoded mode
							byte hi = (byte)(value >> 4);
							byte lo = (byte)(value & 0x0F);

							for (int i = 0; i < count; i++) {
								byte index = ((i & 1) == 0) ? hi : lo;

								if (x < width && y < height) {
									int dstY = _flipImage ? y : (height - y - 1);
									_pixels[dstY * _dstStride + x] = index;
								}

								x++;
							}
						}
						else {
							// Escape mode
							switch (value) {
								case 0: // End of line
									x = 0;
									y++;
									break;
								case 1:
									return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
								case 2: {
										byte dx = *p++;
										byte dy = *p++;
										x += dx;
										y += dy;
										break;
									}

								default: {
										// Absolute mode
										int n = value;

										int byteCount = (n + 1) / 2;

										if (p + byteCount > pSrcEnd)
											return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);

										for (int i = 0; i < n; i++) {
											byte b = p[i >> 1];
											byte index = ((i & 1) == 0) ? (byte)(b >> 4) : (byte)(b & 0x0F);

											if (x < width && y < height) {
												int dstY = _flipImage ? y : (height - y - 1);
												_pixels[dstY * _dstStride + x] = index;
											}

											x++;
										}

										p += byteCount;

										if ((byteCount & 1) == 1)
											p++;
										break;
									}
							}
						}

						if (y >= height)
							break;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private GrfImage _readBitFields(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			_analyzeMask(Dib.RgbaMask[0], out int rShift, out int rBits);
			_analyzeMask(Dib.RgbaMask[1], out int gShift, out int gBits);
			_analyzeMask(Dib.RgbaMask[2], out int bShift, out int bBits);
			_analyzeMask(Dib.RgbaMask[3], out int aShift, out int aBits);

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* pSrcRow = pSrcBase + _reader.PositionLong;

					for (int y = 0; y < _dstHeight; y++) {
						byte* pDst = pDstBase + (_flipImage ? y : (_dstHeight - y - 1)) * _dstStride;
						byte* pSrc = pSrcRow;

						for (int x = 0; x < Dib.Width; x++) {
							uint value;

							if (_srcBpp == 2)
								value = *(ushort*)pSrc;
							else
								value = *(uint*)pSrc;

							byte r = _extract(value, rShift, rBits);
							byte g = _extract(value, gShift, gBits);
							byte b = _extract(value, bShift, bBits);
							byte a = aBits > 0 ? _extract(value, aShift, aBits) : (byte)255;

							pDst[0] = b;
							pDst[1] = g;
							pDst[2] = r;

							if (bpp == 4)
								pDst[3] = a;

							pSrc += _srcBpp;
							pDst += bpp;
						}

						pSrcRow += _srcStride;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private byte _extract(uint value, int shift, int bits) {
			if (bits == 0) return 0;

			uint v = (value >> shift) & ((1u << bits) - 1);

			// Scale to 0–255
			return (byte)((v * 255 + ((1 << bits) - 1) / 2) / ((1 << bits) - 1));
		}

		private int _dstStride;
		private int _dstHeight;
		private bool _flipImage;
		private byte[] _pixels;
		private int _srcBpp;
		private int _bitsPerRow;
		private int _srcStride;
		private int _bytesPerRow;
		private int _srcPadding;

		private void _calculateImage(int bpp) {
			if (Dib.Width >= DibData.MAX_WIDTH || Dib.Height >= DibData.MAX_HEIGHT)
				throw new ArgumentOutOfRangeException("Image dimension is too high.");
			if (Dib.Width < 0)
				throw new ArgumentOutOfRangeException("Image width is negative.");

			_dstStride = bpp * Dib.Width;
			_dstHeight = Math.Abs(Dib.Height);
			_flipImage = Dib.Height < 0;
			_pixels = new byte[bpp * Dib.Width * _dstHeight];
			_srcBpp = Dib.BitCount / 8;

			_bitsPerRow = Dib.Width * Dib.BitCount;
			_srcStride = ((_bitsPerRow + 31) / 32) * 4;

			_bytesPerRow = (_bitsPerRow + 7) / 8;
			_srcPadding = _srcStride - _bytesPerRow;

			if (Dib.Compression == DibCompression.BI_RLE4 ||
				Dib.Compression == DibCompression.BI_RLE8)
				return;

			if (_reader.LengthLong - _reader.PositionLong < Dib.Height * _bytesPerRow)
				throw GrfExceptions.__InvalidImageFormat.Create();
		}

		private GrfImage _readBI_RGB_1(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* pSrcRow = pSrcBase + _reader.PositionLong;
					byte* pSrcEnd = pSrcBase + _reader.LengthLong;

					if (pSrcRow + _srcStride * _dstHeight > pSrcEnd)
						throw GrfExceptions.__InvalidImageFormat.Create();

					for (int y = 0; y < _dstHeight; y++) {
						byte* pDst = pDstBase + (_flipImage ? y : (_dstHeight - y - 1)) * _dstStride;
						byte* pSrc = pSrcRow;

						for (int x = 0; x < Dib.Width; x++) {
							int pos = x & 7;

							*pDst = (byte)((*pSrc >> (8 - pos - 1)) & 0x01);
							pDst++;

							if (pos == 7)
								pSrc++;
						}

						pSrcRow += _srcStride;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private GrfImage _readBI_RGB_4(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* pSrcRow = pSrcBase + _reader.PositionLong;
					byte* pSrcEnd = pSrcBase + _reader.LengthLong;

					if (pSrcRow + _srcStride * _dstHeight > pSrcEnd)
						throw GrfExceptions.__InvalidImageFormat.Create();

					for (int y = 0; y < _dstHeight; y++) {
						byte* pDst = pDstBase + (_flipImage ? y : (_dstHeight - y - 1)) * _dstStride;
						byte* pSrc = pSrcRow;

						for (int x = 0; x < Dib.Width; x++) {
							if ((x & 1) == 0) {
								*pDst = (byte)(*pSrc >> 4);
							}
							else {
								*pDst = (byte)(*pSrc & 0x0F);
								pSrc++;
							}

							pDst++;
						}

						pSrcRow += _srcStride;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private GrfImage _readBI_RGB_16(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* pSrcRow = pSrcBase + _reader.PositionLong;
					byte* pSrcEnd = pSrcBase + _reader.LengthLong;

					if (pSrcRow + _srcStride * _dstHeight > pSrcEnd)
						throw GrfExceptions.__InvalidImageFormat.Create();

					for (int y = 0; y < _dstHeight; y++) {
						byte* pDst = pDstBase + (_flipImage ? y : (_dstHeight - y - 1)) * _dstStride;
						byte* pDstEnd = pDst + _dstStride;
						ushort* pSrc = (ushort*)pSrcRow;

						while (pDst < pDstEnd) {
							int b5 = (*pSrc >> 0) & 0x1F;
							int g5 = (*pSrc >> 5) & 0x1F;
							int r5 = (*pSrc >> 10) & 0x1F;

							pDst[0] = (byte)((b5 << 3) | (b5 >> 2));
							pDst[1] = (byte)((g5 << 3) | (g5 >> 2));
							pDst[2] = (byte)((r5 << 3) | (r5 >> 2));

							pSrc++;
							pDst += bpp;
						}

						pSrcRow += _srcStride;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private GrfImage _readBI_RGB_32(int bpp, GrfImageType type, byte[] palette) {
			return _readBI_RGB_24(bpp, type, palette);
		}

		private GrfImage _readBI_RGB_24(int bpp, GrfImageType type, byte[] palette) {
			_calculateImage(bpp);

			unsafe {
				fixed (byte* pDstBase = _pixels)
				fixed (byte* pSrcBase = _reader._data) {
					byte* pSrcRow = pSrcBase + _reader.PositionLong;
					byte* pSrcEnd = pSrcBase + _reader.LengthLong;

					if (pSrcRow + _dstStride * _dstHeight > pSrcEnd)
						throw GrfExceptions.__InvalidImageFormat.Create();

					for (int y = 0; y < _dstHeight; y++) {
						byte* pDst = pDstBase + (_flipImage ? y : (_dstHeight - y - 1)) * _dstStride;
						Buffer.MemoryCopy(pSrcRow, pDst, _dstStride, _dstStride);
						pSrcRow += _srcStride;
					}
				}
			}

			return new GrfImage(_pixels, Dib.Width, _dstHeight, type, palette);
		}

		private (int bpp, GrfImageType type) _retrieveOutputFormat() {
			switch (Dib.BitCount) {
				case 1:
				case 4:
				case 8:
					return (1, GrfImageType.Indexed8);
				case 16:
				case 24:
					return (3, GrfImageType.Bgr24);
				case 32:
					return (4, GrfImageType.Bgr32);
				default:
					throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}

		private byte[] _readPalette() {
			byte[] palette = null;
			int colorCount = Dib.ClrUsed > 0 ? (int)Dib.ClrUsed : 1 << Dib.BitCount;
			int paletteByteSize = Header.OffsetBits - _reader.Position;

			switch (Dib.BitCount) {
				case 1:
				case 4:
				case 8: // Convert from BGR to RGBA palette format
					if (paletteByteSize <= 0 || 4 * colorCount > paletteByteSize || colorCount < 0 || colorCount > 256)
						throw GrfExceptions.__InvalidImageFormat.Create();

					palette = new byte[1024];

					int srcPaletteLength = colorCount * 4;

					if (srcPaletteLength < 0 || _reader.LengthLong - _reader.PositionLong < srcPaletteLength)
						throw GrfExceptions.__InvalidImageFormat.Create();

					unsafe {
						fixed (byte* pDst = palette)
						fixed (byte* pSrcBase = _reader._data) {
							byte* pSrc = pSrcBase + _reader.PositionLong;

							for (int i = 0; i < 1024; i += 4) {
								if (i < srcPaletteLength) {
									pDst[i + 0] = pSrc[i + 2];
									pDst[i + 1] = pSrc[i + 1];
									pDst[i + 2] = pSrc[i + 0];
								}

								pDst[i + 3] = 255;
							}
						}
					}

					break;
			}

			return palette;
		}

		private void _analyzeMask(uint mask, out int shift, out int bits) {
			if (mask == 0) {
				shift = 0;
				bits = 0;
				return;
			}

			shift = 0;
			while (((mask >> shift) & 1) == 0)
				shift++;

			bits = 0;
			while (((mask >> (shift + bits)) & 1) == 1)
				bits++;
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
					throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}

		public static void Save(GrfImage image, string path) {
			using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)) {
				Save(image, stream);
			}
		}
	}
}
