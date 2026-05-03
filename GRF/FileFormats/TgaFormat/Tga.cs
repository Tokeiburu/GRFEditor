using System;
using GRF.ContainerFormat;
using GRF.Image;

namespace GRF.FileFormats.TgaFormat {
	/// <summary>
	/// TGA file
	/// Encoded with RGBA format
	/// </summary>
	public class Tga : IImageable {
		private int _dstHeight;
		private int _dstWidth;
		private int _bitsPerRow;
		private int _dstStride;
		private int _srcBpp;
		private int _srcStride;
		private bool _flipImage;
		private byte[] _dataDecompressed;
		private bool _isBlackAndwhite;

		/// <summary>
		/// Initializes a new instance of the <see cref="Tga" /> class.
		/// </summary>
		/// <param name="anyData">The data.</param>
		public Tga(MultiType anyData) {
			_dataDecompressed = anyData.Data;

			Header = new TgaHeader(_dataDecompressed);
			
			(int bpp, GrfImageType type) = _retrieveOutputFormat();
			_calculateImage(bpp);

			switch (Header.ImageType) {
				case TgaFormat.Indexed:
					Image = _readUncompressed(bpp, type, true);
					break;
				case TgaFormat.TrueColor:
					Image = _readUncompressed(bpp, type, false);
					break;
				case TgaFormat.BlackAndWhite:
					Image = _readUncompressed(bpp, type, false);
					break;
				case TgaFormat.RleIndexed:
					Image = _readRle(bpp, type, true);
					break;
				case TgaFormat.RleTrueColor:
					Image = _readRle(bpp, type, false);
					break;
				case TgaFormat.RleBlackAndWhite:
					Image = _readRle(bpp, type, false);
					break;
				default:
					throw GrfExceptions.__FileFormatException2.Create("TGA", string.Format(GrfStrings.TgaImageTypeExpected, Header.ImageType));
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static unsafe void _readPixel(byte* src, byte* dst, int bits, int bpp, bool isBlackAndwhite) {
			switch (bits) {
				case 8:
					dst[0] = src[0];
					if (bpp >= 3) {
						dst[1] = src[0];
						dst[2] = src[0];
						if (bpp == 4) dst[3] = 255;
					}
					break;
				case 24:
					dst[0] = src[0];
					dst[1] = src[1];
					dst[2] = src[2];
					if (bpp == 4) dst[3] = 255;
					break;
				case 32:
					if (bpp == 4) {
						*(uint*)dst = *(uint*)src;
						break;
					}

					dst[0] = src[0];
					dst[1] = src[1];
					dst[2] = src[2];
					break;
				case 15:
				case 16:
					ushort v = *(ushort*)src;

					if (isBlackAndwhite) {
						dst[2] = dst[1] = dst[0] = (byte)(v & 0xFF);
						dst[3] = (byte)(v >> 8);
						break;
					}

					int b5 = (v >> 0) & 0x1F;
					int g5 = (v >> 5) & 0x1F;
					int r5 = (v >> 10) & 0x1F;

					dst[0] = (byte)((b5 << 3) | (b5 >> 2));
					dst[1] = (byte)((g5 << 3) | (g5 >> 2));
					dst[2] = (byte)((r5 << 3) | (r5 >> 2));

					if (bpp == 4) {
						//int aBits = (bits == 16) ? 1 : 0;
						//dst[3] = aBits == 1 ? (byte)(((v >> 15) & 1) * 255) : (byte)255;
						dst[3] = 255;
					}
					break;
				default:
					throw GrfExceptions.__UnsupportedPixelFormat.Create(bits);
			}
		}

		private unsafe GrfImage _readRle(int bpp, GrfImageType type, bool useColorMap) {
			Pixels = new byte[_dstHeight * _dstStride];
			int bytes = (int)Math.Ceiling(Header.Bits / 8f);
			int bits = useColorMap ? Header.ColourMapbits : Header.Bits;
			int srcColourBpp = (int)Math.Ceiling(Header.ColourMapbits / 8f);

			fixed (byte* pDstBase = Pixels)
			fixed (byte* pSrcBase = _dataDecompressed) {
				byte* pColorMap = pSrcBase + TgaHeader.StructSize + Header.IdEntSize;
				int colorMapSizeInBytes = useColorMap ? Header.ColourMapLength * (Header.ColourMapbits / 8) : 0;
				byte* pImageData = pColorMap + colorMapSizeInBytes;

				byte* pSrcEnd = pSrcBase + _dataDecompressed.Length;
				byte* pDstEnd = pDstBase + Pixels.Length;
				byte* pSrc = pImageData;
				byte* pDst = pDstBase;

				while (pSrc < pSrcEnd && pDst < pDstEnd) {
					byte header = *pSrc++;
					int count = (header & 0x7F) + 1;

					if ((header & 0x80) != 0) {
						byte* pColor = useColorMap ? pColorMap + (*pSrc * srcColourBpp) : pSrc;

						if (pSrc + bytes > pSrcEnd)
							throw GrfExceptions.__InvalidImageFormat.Create();

						for (int i = 0; i < count; i++) {
							_readPixel(pColor, pDst, bits, bpp, _isBlackAndwhite);
							pDst += bpp;
						}

						pSrc += bytes;
					}
					else {
						if (pSrc + count * bytes > pSrcEnd)
							throw GrfExceptions.__InvalidImageFormat.Create();

						for (int i = 0; i < count; i++) {
							byte* pColor = useColorMap ? pColorMap + (*pSrc * srcColourBpp) : pSrc;
							_readPixel(pColor, pDst, bits, bpp, _isBlackAndwhite);
							pSrc += bytes;
							pDst += bpp;
						}
					}
				}
			}

			var image = new GrfImage(Pixels, Header.Width, Header.Height, type);

			// Image is already flipped
			if (!_flipImage)
				image.Flip(FlipDirection.Vertical);

			return image;
		}

		private unsafe GrfImage _readUncompressed(int bpp, GrfImageType type, bool useColorMap) {
			Pixels = new byte[_dstHeight * _dstStride];
			int bytes = (int)Math.Ceiling(Header.Bits / 8f);
			int bits = useColorMap ? Header.ColourMapbits : Header.Bits;
			int srcColourBpp = (int)Math.Ceiling(Header.ColourMapbits / 8f);

			fixed (byte* pSrcBase = _dataDecompressed)
			fixed (byte* pDstBase = Pixels) {
				byte* pColorMap = pSrcBase + TgaHeader.StructSize + Header.IdEntSize;
				int colorMapSizeInBytes = Header.ColourMapLength * (Header.ColourMapbits / 8);
				byte* pSrcRow = pColorMap + colorMapSizeInBytes;

				for (int y = 0; y < Header.Height; y++) {
					byte* pSrc = pSrcRow;
					byte* pSrcEnd = pSrcRow + _srcStride;
					byte* pDst = pDstBase + (_flipImage ? y : (_dstHeight - y - 1)) * _dstStride;

					while (pSrc < pSrcEnd) {
						byte* pColor = useColorMap ? pColorMap + (*pSrc * srcColourBpp) : pSrc;
						_readPixel(pColor, pDst, bits, bpp, _isBlackAndwhite);
						pDst += bpp;
						pSrc += bytes;
					}

					pSrcRow += _srcStride;
				}
			}

			return new GrfImage(Pixels, Header.Width, Header.Height, type);
		}

		private void _calculateImage(int bpp) {
			_dstHeight = Header.Height;
			_dstWidth = Header.Width;
			_bitsPerRow = _dstWidth * Header.Bits;
			_flipImage = (Header.Descriptor & 0x20) != 0;
			_dstStride = Header.Width * bpp;
			_srcBpp = (int)Math.Ceiling(Header.Bits / 8f);
			_srcStride = Header.Width * _srcBpp;

			switch (Header.ImageType) {
				case TgaFormat.BlackAndWhite:
				case TgaFormat.RleBlackAndWhite:
					_isBlackAndwhite = true;
					break;
			}
		}

		private (int bpp, GrfImageType type) _retrieveOutputFormat() {
			int bits = Header.Bits;
			bool isColorMap = false;

			if (Header.ImageType == TgaFormat.Indexed || 
				Header.ImageType == TgaFormat.RleIndexed) {
				bits = Header.ColourMapbits;
				isColorMap = true;
			}

			switch (bits) {
				case 1:
				case 4:
				case 8:
					return (3, GrfImageType.Bgr24);
				case 15:
					return (3, GrfImageType.Bgr24);
				case 16:
					return (4, GrfImageType.Bgra32);
				case 24:
					return (3, GrfImageType.Bgr24);
				case 32:
					if ((Header.Descriptor & 0x0F) == 0 && !isColorMap)
						return (3, GrfImageType.Bgr24);
					if (!Header.HasAlpha)
						return (3, GrfImageType.Bgr24);
					return (4, GrfImageType.Bgra32);
				default:
					throw GrfExceptions.__InvalidImageFormat.Create();
			}
		}

		/// <summary>
		/// Gets the pixels.
		/// </summary>
		public byte[] Pixels { get; private set; }

		/// <summary>
		/// Gets the header.
		/// </summary>
		public TgaHeader Header { get; private set; }

		/// <summary>
		/// Gets the type.
		/// </summary>
		public GrfImageType Type {
			get { return Image.GrfImageType; }
		}

		#region IImageable Members

		/// <summary>
		/// Gets or sets the image.
		/// </summary>
		public GrfImage Image { get; set; }

		#endregion
	}
}