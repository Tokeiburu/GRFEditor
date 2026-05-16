using System;
using System.Collections.Generic;
using System.IO;
using GRF.Core.GrfCompression;
using GRF.IO;

namespace GRF.Core {
	public sealed class Compression {
		private static ICompression _compressionAlgorithm;

		public static readonly DotNetCompression ZlibDotNet = new DotNetCompression();
		public static readonly CpsCompression ZlibGravity = new CpsCompression();
		public static readonly LzmaCompression LzmaCompression = new LzmaCompression();
		public static readonly GZipCompression GZipCompression = new GZipCompression();

		private static bool _isNormalCompression = true;

		public static List<ICompression> CompressionMethods = new List<ICompression> {
			ZlibGravity,
			ZlibDotNet,
			LzmaCompression,
		};

		public static bool EnsureChecksum { get; set; }

		public static bool IsNormalCompression {
			get { return _isNormalCompression; }
			set { _isNormalCompression = value; }
		}

		public static bool IsLzma {
			get { return _compressionAlgorithm is LzmaCompression; }
		}

		public static bool IsCustom {
			get { return _compressionAlgorithm is CustomCompression; }
		}

		public static ICompression CompressionAlgorithm {
			get { return _compressionAlgorithm ?? (_compressionAlgorithm = new CpsCompression()); }
			set {
				if (value == _compressionAlgorithm) return;

				if (_compressionAlgorithm != null && !CompressionMethods.Contains(_compressionAlgorithm))
					_compressionAlgorithm.Dispose();

				if (value == null || !value.Success)
					_compressionAlgorithm = new CpsCompression();
				else
					_compressionAlgorithm = value;

				if (_compressionAlgorithm is DotNetCompression || _compressionAlgorithm is CpsCompression) {
					IsNormalCompression = true;
				}
				else {
					IsNormalCompression = false;
				}
			}
		}

		#region Current
		public static byte[] Compress(Stream stream) => CompressionAlgorithm.Compress(stream);
		public static byte[] Compress(byte[] uncompressed) => CompressionAlgorithm.Compress(uncompressed);
		public static byte[] Decompress(byte[] compressed, long uncompressedLength) => CompressionAlgorithm.Decompress(compressed, compressed.Length, uncompressedLength);
		public static byte[] Decompress(byte[] compressed, long compressedLength, long uncompressedLength) => CompressionAlgorithm.Decompress(compressed, compressedLength, uncompressedLength);
		#endregion

		#region ZlibDotNet
		public static byte[] CompressZlibDotNet(Stream stream) => ZlibDotNet.Compress(stream);
		public static byte[] CompressZlibDotNet(byte[] uncompressed) => ZlibDotNet.Compress(uncompressed);
		public static byte[] DecompressZlibDotNet(byte[] compressed) => ZlibDotNet.Decompress(compressed, compressed.Length, -1);
		#endregion

		#region Lzma
		public static byte[] CompressLzma(Stream stream) => LzmaCompression.Compress(stream);
		public static byte[] CompressLzma(byte[] uncompressed) => LzmaCompression.Compress(uncompressed);
		public static byte[] DecompressLzma(byte[] compressed, long uncompressedLength) => LzmaCompression.Decompress(compressed, compressed.Length, uncompressedLength);
		#endregion

		#region Zlib (Gravity)
		public static byte[] CompressZlib(Stream stream) => ZlibGravity.Compress(stream);
		public static byte[] CompressZlib(byte[] uncompressed) => ZlibGravity.Compress(uncompressed);
		public static byte[] DecompressZlib(byte[] arrCompressed, long uncompressedLength) => ZlibGravity.Decompress(arrCompressed, arrCompressed.Length, uncompressedLength);
		#endregion

		public static void CopyStream(Stream input, Stream output) {
			byte[] buffer = new byte[131072];
			int len;
			while ((len = input.Read(buffer, 0, 131072)) > 0) {
				output.Write(buffer, 0, len);
			}
			output.Flush();
		}

		public static byte[] LzssDecompress(byte[] arrCompressed, long uncompressedLength) {
			byte[] output = new byte[uncompressedLength];
			var output_offset = 0;
			ByteReader input = new ByteReader(arrCompressed);

			if (input.Length == 0 || uncompressedLength == 0)
				return new byte[0];

			byte control = input.Byte();
			int control_count = 0;

			while (true) {
				if ((control & 1) == 0) {
					output[output_offset] = input.Byte();
					output_offset++;
				}
				else {
					ushort codeword = input.UInt16();
					int phrase_length = ((codeword & 0xf000) >> 12) + 2;
					int phrase_index = (codeword & 0x0fff);

					for (int i = 0; i < phrase_length; i++) {
						output[output_offset] = output[output_offset - phrase_index];
						output_offset++;
					}
				}

				control = (byte)(control >> 1);
				control_count++;

				if (!input.CanRead)
					break;

				if (control_count >= 8) {
					control = input.Byte();
					control_count = 0;
				}
			}

			return output;
		}

		public static byte[] RawDecompress(byte[] arrCompressed, long uncompressedLength) {
			if (uncompressedLength != arrCompressed.Length) {
				byte[] decompData = new byte[uncompressedLength];
				Buffer.BlockCopy(arrCompressed, 0, decompData, 0, (int)uncompressedLength);
				return decompData;
			}

			return arrCompressed;
		}

		
	}
}