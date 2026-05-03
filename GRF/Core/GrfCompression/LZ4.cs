using System;

namespace GRF.Core.GrfCompression {
	public class LZ4 {
		public static byte[] Decompress(byte[] input, int uncompressedSizeGuess = 0) {
			int outCap = uncompressedSizeGuess > 0 ? uncompressedSizeGuess : input.Length * 10;
			byte[] output = new byte[outCap];
			int ip = 0;
			int op = 0;

			while (ip < input.Length) {
				if (op >= output.Length - 300) {
					Array.Resize(ref output, output.Length * 2);
				}

				byte token = input[ip++];
				int literalLength = token >> 4;

				if (literalLength == 15) {
					byte len;
					do {
						len = input[ip++];
						literalLength += len;
					}
					while (len == 255);
				}

				// Copy literals
				Buffer.BlockCopy(input, ip, output, op, literalLength);
				ip += literalLength;
				op += literalLength;

				if (ip >= input.Length)
					break;

				// Read match offset
				int offset = input[ip++] | (input[ip++] << 8);

				int matchLength = token & 0x0F;

				// If match length is extended
				if (matchLength == 15) {
					byte len;
					do {
						len = input[ip++];
						matchLength += len;
					}
					while (len == 255);
				}

				matchLength += 4;
				int matchSrc = op - offset;

				if (matchSrc < 0)
					throw new Exception("Invalid LZ4 offset.");

				for (int i = 0; i < matchLength; i++) {
					output[op++] = output[matchSrc + i];
				}
			}

			Array.Resize(ref output, op);
			return output;
		}
	}
}
