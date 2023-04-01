using System;

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
		public byte ImageType; // type of image 0=none,1=indexed,2=rgb,3=grey,+8=rle packed
		public int Width; // image width in pixels

		public int XStart; // image x origin
		public int YStart; // image y origin

		public TgaHeader(byte[] data) {
			IdEntSize = data[0];
			ColourMapType = data[1];
			ImageType = data[2];
			ColourMapStart = BitConverter.ToInt16(data, 3);
			ColourMapLength = BitConverter.ToInt16(data, 5);
			ColourMapbits = data[7];
			XStart = BitConverter.ToInt16(data, 8);
			YStart = BitConverter.ToInt16(data, 10);
			Width = BitConverter.ToUInt16(data, 12);
			Height = BitConverter.ToUInt16(data, 14);
			Bits = data[16];
			Descriptor = data[17];
		}

		/// <summary>
		/// Validates the TGA header.
		/// </summary>
		/// <returns>True if valid, false otherwise.</returns>
		public bool ValidateHeader() {
			if (Width <= 0) return false;
			if (Height <= 0) return false;
			if (Bits != 32 && Bits != 24) return false;
			if (!(ImageType == 2 || ImageType >= 8)) return false;

			return true;
		}
	}
}