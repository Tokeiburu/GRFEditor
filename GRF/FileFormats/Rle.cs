using System;

namespace GRF.FileFormats {
	public class Rle {
		public int Width { get; set; }
		public int Height { get; set; }
		public bool? EarlyEndingEncoding { get; set; }
		public byte[] FrameData { get; set; }

		public byte[] Decompress() {
			int decompressedLength = Width * Height;
			byte[] data = FrameData;
			byte[] decompressed = new byte[decompressedLength];
			int position = 0;

			for (int k = 0; k < data.Length; k++) {
				byte byteRead = data[k];

				if (byteRead == 0) {
					position += data[++k];
				}
				else {
					decompressed[position] = byteRead;
					position++;
				}
			}

			if (position < decompressed.Length) {
				EarlyEndingEncoding = true;
			}

			return decompressed;
		}

		public static byte[] Decompress(byte[] data, int decompressedLength) {
			byte[] decompressed = new byte[decompressedLength];
			int position = 0;

			for (int k = 0; k < data.Length; k++) {
				byte byteRead = data[k];

				if (byteRead == 0) {
					position += data[++k];
				}
				else {
					decompressed[position] = byteRead;
					position++;
				}
			}

			return decompressed;
		}

		public static byte[] Compress(byte[] data) {
			byte[] compressed = new byte[2 * data.Length + 1024];

			int offset = 0;

			for (int j = 0; j < data.Length; j++) {
				if (data[j] == 0) {
					compressed[offset++] = 0;

					byte mult = 0;

					while (j < data.Length && mult < 255) {
						if (data[j] != 0)
							break;

						mult++;
						j++;
					}

					compressed[offset++] = mult;
					j--;
				}
				else {
					compressed[offset++] = data[j];
				}
			}

			byte[] realCompressed = new byte[offset];
			Buffer.BlockCopy(compressed, 0, realCompressed, 0, offset);

			return realCompressed;
		}
	}
}