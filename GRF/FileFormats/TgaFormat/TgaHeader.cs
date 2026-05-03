using System;
using Utilities.Services;

namespace GRF.FileFormats.TgaFormat {
	/// <summary>
	/// TGA header.
	/// </summary>
	public class TgaHeader {
		public const int StructSize = 18;
		public byte Bits; // image bits per pixel 8,16,24,32
		public int ColourMapLength; // number of colours in palette
		public int ColourMapStart; // first colour map entry in palette
		public byte ColourMapType; // type of colour map 0=none, 1=has palette
		public byte ColourMapbits; // number of bits per palette entry 15,16,24,32
		public byte Descriptor; // image descriptor bits (vh flip bits)
		public int Height; // image height in pixels
		public byte IdEntSize; // size of ID field that follows 18 byte header (0 usually)
		public TgaFormat ImageType; // type of image 0=none,1=indexed,2=rgb,3=grey,+8=rle packed
		public int Width; // image width in pixels

		public int DeveloperOffset = 0;
		public int ExtensionOffset = 0;
		public byte AttributeType = 0;

		public bool HasAlpha = true;

		public int XStart; // image x origin
		public int YStart; // image y origin

		public TgaHeader(byte[] data) {
			IdEntSize = data[0];
			ColourMapType = data[1];
			ImageType = (TgaFormat)data[2];
			ColourMapStart = BitConverter.ToInt16(data, 3);
			ColourMapLength = BitConverter.ToInt16(data, 5);
			ColourMapbits = data[7];
			XStart = BitConverter.ToInt16(data, 8);
			YStart = BitConverter.ToInt16(data, 10);
			Width = BitConverter.ToUInt16(data, 12);
			Height = BitConverter.ToUInt16(data, 14);
			Bits = data[16];
			Descriptor = data[17];

			string footerMagic = EncodingService.Ansi.GetString(data, data.Length - 18, 16);

			if (footerMagic == "TRUEVISION-XFILE") {
				ExtensionOffset = BitConverter.ToInt32(data, data.Length - 26);
				DeveloperOffset = BitConverter.ToInt32(data, data.Length - 22);

				ushort extensionSize = BitConverter.ToUInt16(data, ExtensionOffset);

				if (extensionSize >= 495) {
					AttributeType = data[ExtensionOffset + 494];

					switch (AttributeType) {
						case 0:
						case 1:
						case 2:
							HasAlpha = false;
							break;
						case 3:
						case 4:
							HasAlpha = true;
							break;
					}
				}
			}
		}

		/// <summary>
		/// Validates the TGA header.
		/// </summary>
		/// <returns>True if valid, false otherwise.</returns>
		public bool ValidateHeader() {
			if (Width <= 0) return false;
			if (Height <= 0) return false;
			if (Bits != 32 && Bits != 24 && Bits != 8 && Bits != 15 && Bits != 16) return false;
			
			switch (ImageType) {
				case TgaFormat.Indexed:
				case TgaFormat.TrueColor:
				case TgaFormat.BlackAndWhite:
				case TgaFormat.RleIndexed:
				case TgaFormat.RleTrueColor:
				case TgaFormat.RleBlackAndWhite:
					return true;
			}

			return false;
		}
	}

	public enum TgaFormat {
		None = 0,
		Indexed = 1,
		TrueColor = 2,
		BlackAndWhite = 3,
		RleIndexed = 9,
		RleTrueColor = 10,
		RleBlackAndWhite = 11,
	}
}