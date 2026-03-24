using System;
using System.Collections.Generic;
using GRF.ContainerFormat;
using GRF.Image.Decoders;
using Utilities;

namespace GRF.Image {
	/// <summary>
	/// Image used by the GRF library
	/// </summary>
	public partial class GrfImage {
		private bool _isClosed;
		private int? _hashCode;

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public GrfImage(MultiType data) {
			byte[] fileData = data.UniqueData;
			Width = -1;
			Height = -1;

			Pixels = fileData;
			GrfImageType = GrfImageAnalysis.GetGrfImageType(Pixels);

			SelfAny();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="type">The type.</param>
		public GrfImage(byte[] pixels, int width, int height, GrfImageType type) {
			Width = width;
			Height = height;
			GrfImageType = type;

			Pixels = pixels;

			if (type >= GrfImageType.NotEvaluated) {
				SelfAny();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="type">The type.</param>
		/// <param name="paletteRgba">The palette.</param>
		public GrfImage(byte[] pixels, int width, int height, GrfImageType type, byte[] paletteRgba) {
			Width = width;
			Height = height;
			GrfImageType = type;

			Pixels = pixels;
			Palette = paletteRgba;

			if (type >= GrfImageType.NotEvaluated) {
				SelfAny();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfImage" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public GrfImage(byte[] data) {
			Width = -1;
			Height = -1;

			Pixels = data;
			GrfImageType = GrfImageAnalysis.GetGrfImageType(Pixels);

			SelfAny();
		}

		/// <summary>
		/// Gets the width of the image.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Gets the height of the image.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets the pixels of the image, either indexed or Bgr(a).
		/// </summary>
		public byte[] Pixels { get; private set; }

		internal bool[] TransparentPixels { get; private set; }

		/// <summary>
		/// Gets the type of the image.
		/// </summary>
		public GrfImageType GrfImageType { get; private set; }

		/// <summary>
		/// Gets the palette, in the RGBA format.
		/// </summary>
		public byte[] Palette { get; private set; }

		public int NumberOfPixels {
			get {
				if (GrfImageType == GrfImageType.Indexed8)
					return Pixels.Length;
				if (GrfImageType == GrfImageType.Bgr24)
					return Pixels.Length / 3;
				if (GrfImageType == GrfImageType.Bgra32 ||
				    GrfImageType == GrfImageType.Bgr32)
					return Pixels.Length / 4;
				return -1;
			}
		}

		public IEnumerable<GrfColor> Colors {
			get {
				if (GrfImageType == GrfImageType.Indexed8) {
					for (int i = 0; i < 256; i++) {
						yield return GrfColor.FromByteArray(Palette, i * 4, GrfImageType);
					}
				}
				else if (GrfImageType == GrfImageType.Bgr24) {
					for (int i = 0; i < Pixels.Length; i += 3) {
						yield return GrfColor.FromByteArray(Pixels, i, GrfImageType);
					}
				}
				else if (GrfImageType == GrfImageType.Bgra32 || GrfImageType == GrfImageType.Bgr32) {
					for (int i = 0; i < Pixels.Length; i += 4) {
						yield return GrfColor.FromByteArray(Pixels, i, GrfImageType);
					}
				}
			}
		}

		public static implicit operator GrfImage(string value) {
			return new GrfImage(value);
		}

		/// <summary>
		/// Sets the palette.
		/// </summary>
		/// <param name="newPalette">The new palette.</param>
		public void SetPalette(byte[] newPalette) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			Palette = newPalette;
			InvalidateHash();
		}

		#region Public methods
		/// <summary>
		/// Sets the type of the GRF image.
		/// </summary>
		/// <param name="type">The type.</param>
		public void SetGrfImageType(GrfImageType type) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfImageType = type;
			InvalidateHash();
		}

		/// <summary>
		/// Dispose the object immediatly.
		/// </summary>
		public void Close() {
			_isClosed = true;
			Pixels = null;
			Palette = null;
			InvalidateHash();
		}

		/// <summary>
		/// Copy an image at the specified location and return the content.
		/// </summary>
		/// <param name="x">x.</param>
		/// <param name="y">y.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <returns>The copied image at the specified location</returns>
		public GrfImage Extract(int x, int y, int width, int height) {
			if (x < 0) throw new ArgumentOutOfRangeException("x");
			if (y < 0) throw new ArgumentOutOfRangeException("y");

			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			GrfExceptions.IfNullThrowNonLoadedImage(Pixels);
			int bpp = GetBpp();
			GrfExceptions.IfLtZeroThrowUnsupportedImageFormat(bpp);

			byte[] pixels = new byte[width * height * bpp];

			byte defaultByte = (byte)(GrfImageType == GrfImageType.Indexed8 ? 0 : 255);

			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = defaultByte;

			int actualWidth = (x + width) > Width ? (Width - x) : width;
			int actualHeight = (y + height) > Height ? (Height - y) : height;

			if (actualWidth < 0 || actualHeight < 0) {
				return new GrfImage(pixels, width, height, GrfImageType);
			}

			for (int y2 = 0; y2 < actualHeight; y2++) {
				Buffer.BlockCopy(Pixels, ((y2 + y) * Width + x) * bpp, pixels, y2 * bpp * width, actualWidth * bpp);
			}

			if (GrfImageType == GrfImageType.Indexed8) {
				return new GrfImage(pixels, width, height, GrfImageType, Methods.Copy(Palette));
			}

			return new GrfImage(pixels, width, height, GrfImageType);
		}

		/// <summary>
		/// Gets the bit per pixel rate.
		/// </summary>
		/// <returns></returns>
		public int GetBpp() {
			switch (GrfImageType) {
				case GrfImageType.Bgr24:
					return 3;
				case GrfImageType.Bgr32:
					return 4;
				case GrfImageType.Bgra32:
					return 4;
				case GrfImageType.Indexed8:
					return 1;
			}

			return -1;
		}

		/// <summary>
		/// Creates a copy of the image.
		/// </summary>
		/// <returns></returns>
		public GrfImage Copy() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			if (Palette != null) {
				return new GrfImage(Methods.Copy(Pixels), Width, Height, GrfImageType, Methods.Copy(Palette)) { TransparentPixels = Methods.Copy(TransparentPixels) };
			}

			return new GrfImage(Methods.Copy(Pixels), Width, Height, GrfImageType) { TransparentPixels = Methods.Copy(TransparentPixels) };
		}

		/// <summary>
		/// Creates a copy of the image.
		/// </summary>
		/// <returns></returns>
		public GrfImage Clone() {
			return Copy();
		}

		#endregion

		#region Conversion methods

		/// <summary>
		/// Reads the image and converts it to a readable format.
		/// </summary>
		/// <typeparam name="T">The image parser</typeparam>
		public void Self<T>() where T : class {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			GrfImage image = ImageConverterManager.Self<T>(this);
			SetGrfImageType(image.GrfImageType);

			byte[] pixels = image.Pixels;
			byte[] palette = image.Palette;

			SetPalette(palette);
			SetPixels(ref pixels);
		}

		/// <summary>
		/// Reads the image and converts it to a readable format by using the first image parser.
		/// </summary>
		public void SelfAny() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			GrfImage image = ImageConverterManager.SelfAny(this);

			GrfImageType = image.GrfImageType;

			Pixels = image.Pixels;
			Palette = image.Palette;
			Width = image.Width;
			Height = image.Height;
			InvalidateHash();
		}

		/// <summary>
		/// Converts an image to a .net format (or a custom one) defined by the 
		/// ImageConverterManager class.
		/// </summary>
		/// <typeparam name="T">Image format</typeparam>
		/// <returns>The image converted</returns>
		public T Cast<T>() where T : class {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			return ImageConverterManager.Convert<T>(this);
		}

		/// <summary>
		/// Converts the image to the specified destination format.
		/// </summary>
		/// <param name="destinationFormat">The destination format.</param>
		/// <param name="sourceFormat">The source format.</param>
		public void Convert(IImageFormatConverter destinationFormat, IImageFormatConverter sourceFormat = null) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			if (sourceFormat == null)
				sourceFormat = ImageFormatProvider.GetFormatConverter(GrfImageType);

			sourceFormat.ToBgra32(this);
			destinationFormat.Convert(this);
			InvalidateHash();
		}

		/// <summary>
		/// Converts the image to the specified new format.
		/// </summary>
		/// <param name="newFormat">The new image format.</param>
		/// <param name="palette">The palette used for Indexed8 conversion.</param>
		public void Convert(GrfImageType newFormat, byte[] palette = null) {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);

			switch (newFormat) {
				case GrfImageType.Bgr24:
					Convert(new Bgr24FormatConverter());
					break;
				case GrfImageType.Bgr32:
					Convert(new Bgr32FormatConverter());
					break;
				case GrfImageType.Bgra32:
					if (GrfImageType == GrfImageType.Indexed8)
						Palette[3] = 0;

					Convert(new Bgra32FormatConverter());
					break;
				case GrfImageType.Indexed8:
					var converter = new Indexed8FormatConverter { Options = Indexed8FormatConverter.PaletteOptions.AutomaticallyGeneratePalette };

					if (palette != null) {
						converter.Options &= ~Indexed8FormatConverter.PaletteOptions.AutomaticallyGeneratePalette;
						converter.Options |= Indexed8FormatConverter.PaletteOptions.UseExistingLabPalette;
						converter.ExistingPalette = palette;
					}

					Convert(converter);
					break;
				default:
					throw new Exception("Image format not supported. Use the method requiring an IImageFormatConverter provider instead.");
			}

			InvalidateHash();
		}

		#endregion

		public override bool Equals(object obj) {
			if (obj == null) return false;
			if (ReferenceEquals(obj, this)) return true;

			var grfImage = obj as GrfImage;

			if (grfImage != null) {
				if (grfImage.GrfImageType == GrfImageType) {
					if (grfImage.Width != Width || grfImage.Height != Height) return false;

					if (grfImage.GrfImageType == GrfImageType.Indexed8) {
						return Methods.ByteArrayCompare(grfImage.Pixels, Pixels) && Methods.ByteArrayCompare(grfImage.Palette, Palette);
					}

					return Methods.ByteArrayCompare(grfImage.Pixels, Pixels);
				}
			}

			return false;
		}

		public override int GetHashCode() {
			if (_hashCode != null)
				return _hashCode.Value;

			unchecked {
				const ulong FNV_OFFSET = 14695981039346656037UL;
				const ulong FNV_PRIME = 1099511628211UL;

				ulong hash = FNV_OFFSET;

				void HashByte(byte b) => hash = (hash ^ b) * FNV_PRIME;
				void HashInt(int v) {
					HashByte((byte)v);
					HashByte((byte)(v >> 8));
					HashByte((byte)(v >> 16));
					HashByte((byte)(v >> 24));
				}

				HashInt((int)GrfImageType);
				HashInt(Width);
				HashInt(Height);

				var pixels = Pixels;
				for (int i = 0; i < pixels.Length; i++)
					hash = (hash ^ pixels[i]) * FNV_PRIME;

				if (GrfImageType == GrfImageType.Indexed8 && Palette != null) {
					for (int i = 0; i < Palette.Length; i++)
						hash = (hash ^ Palette[i]) * FNV_PRIME;
				}

				_hashCode = (int)hash;
				return _hashCode.Value;
			}
		}

		public void InvalidateHash() => _hashCode = null;

		public override string ToString() {
			GrfExceptions.IfTrueThrowClosedImage(_isClosed);
			return String.Format("Width = {0}; Height = {1}; ImageType = {2}", Width, Height, GrfImageType);
		}

		public static GrfImage Empty(GrfImageType type) {
			switch (type) {
				case GrfImageType.Bgr24:
				case GrfImageType.Bgr32:
				case GrfImageType.Bgra32:
					return new GrfImage(new byte[0], 0, 0, type);
				case GrfImageType.Indexed8:
					return new GrfImage(new byte[0], 0, 0, type, new byte[1024]);
				default:
					throw GrfExceptions.__UnsupportedImageFormatMethod.Create("Empty");
			}
		}

		public unsafe void ChangePinkToBlack(byte rT, byte gT, byte bT) {
			int bpp = GetBpp();

			if (bpp != 4)
				return;

			fixed (byte* ptr = Pixels) {
				byte* pPixels = ptr;
				byte* pPixelsEnd = ptr + Pixels.Length;

				while (pPixels < pPixelsEnd) {
					if (pPixels[0] > rT && pPixels[1] < gT && pPixels[2] > bT) {
						*(int*)pPixels = 0;
					}

					pPixels += bpp;
				}
			}
		}

		public unsafe void DitherAndChangePinkToBlack(byte rT, byte gT, byte bT, int ditherDividerShift, float ditherMultiplier) {
			int bpp = GetBpp();

			if (bpp != 4)
				return;

			int ditherMultiplierInt = (int)(ditherMultiplier * 1024);
			byte[] lut = new byte[256];

			for (int i = 0; i < 256; i++) {
				int r = ((i >> ditherDividerShift) * ditherMultiplierInt) >> 10;
				lut[i] = (byte)(r > 255 ? 255 : r);
			}

			fixed (byte* l =  lut)
			fixed (byte* ptr = Pixels) {
				uint* pPixels = (uint*)ptr;
				uint* pPixelsEnd = (uint*)(ptr + Pixels.Length);

				while (pPixels < pPixelsEnd) {
					uint px = *pPixels;

					if (((px >> 16) & 0xFF) > rT && ((px >> 8) & 0xFF) < gT && (px & 0xFF) > bT) {
						*pPixels++ = 0;
					}
					else {
						*pPixels++ =
									((uint)l[(px >> 24) & 0xFF] << 24) |
									((uint)l[(px >> 16) & 0xFF] << 16) |
									((uint)l[(px >> 8) & 0xFF] << 8) |
									 (uint)l[px & 0xFF];
					}
				}
			}
		}
	}
}