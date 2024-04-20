using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using GRF.Core.GrfCompression;
using GRF.Core.GrfCompression.GZip;
using GRF.IO;
using GRF.System;
using GRF.Threading;

namespace GRF.Core {
	public class Compression {
		private static ICompression _compressionAlgorithm;

		private static readonly ICompression _dotNetCompression = new DotNetCompression();
		private static readonly ICompression _recoveryCompression = new RecoveryCompression();
		private static readonly ICompression _zlibCompression = new CpsCompression();
		private static readonly ICompression _lzmaCompression = new LzmaCompression();

		private static bool _isNormalCompression = true;

		public static List<ICompression> CompressionMethods = new List<ICompression> {
			_zlibCompression,
			_dotNetCompression,
			_lzmaCompression,
			//_recoveryCompression,
		};

		public static bool EnsureChecksum { get; set; }

		public static bool IsNormalCompression {
			get { return _isNormalCompression; }
			set { _isNormalCompression = value; }
		}

		public static bool IsLzma {
			get { return _compressionAlgorithm is LzmaCompression; }
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

		public static byte[] Compress(Stream inMemoryStream) {
			return CompressionAlgorithm.Compress(inMemoryStream);
		}

		public static byte[] CompressDotNet(Stream inMemoryStream) {
			return _dotNetCompression.Compress(inMemoryStream);
		}

		public static byte[] CompressDotNet(byte[] uncompressed) {
			return _dotNetCompression.Compress(uncompressed);
		}

		public static byte[] DecompressDotNet(byte[] compressed) {
			return _dotNetCompression.Decompress(compressed, -1);
		}

		public static byte[] Compress(byte[] uncompressed) {
			return CompressionAlgorithm.Compress(uncompressed);
		}

		public static byte[] CompressLzma(byte[] uncompressed) {
			return _lzmaCompression.Compress(uncompressed);
		}

		public static byte[] CompressZlib(byte[] uncompressed) {
			return _zlibCompression.Compress(uncompressed);
		}

		public static byte[] CompressZlib(Stream inMemoryStream) {
			return _zlibCompression.Compress(inMemoryStream);
		}

		public static byte[] DecompressLzma(byte[] compressed, long uncompressedLength) {
			return _lzmaCompression.Decompress(compressed, uncompressedLength);
		}

		public static byte[] CompressRecovery(byte[] uncompressed) {
			return _recoveryCompression.Compress(uncompressed);
		}

		public static byte[] DecompressRecovery(byte[] compressed, long uncompressedLength) {
			return _recoveryCompression.Decompress(compressed, uncompressedLength);
		}

		public static void CopyStream(Stream input, Stream output) {
			byte[] buffer = new byte[131072];
			int len;
			while ((len = input.Read(buffer, 0, 131072)) > 0) {
				output.Write(buffer, 0, len);
			}
			output.Flush();
		}

		public static byte[] DecompressZlib(byte[] arrCompressed, long uncompressedLength) {
			return _zlibCompression.Decompress(arrCompressed, uncompressedLength);
		}

		public static byte[] Decompress(byte[] arrCompressed, long uncompressedLength) {
			return CompressionAlgorithm.Decompress(arrCompressed, uncompressedLength);
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

		public static void GZipCompress(IProgress grfData, string source, string destination) {
			using (GZipStream compressing = new GZipStream(File.OpenWrite(Path.Combine(Settings.TempPath, "~tmp.gz")), CompressionMode.Compress))
			using (FileStream file = File.OpenRead(source)) {
				byte[] buffer = new byte[131072];
				int len;
				long totalRead = 0;
				long totalLength = file.Length;

				while ((len = file.Read(buffer, 0, buffer.Length)) > 0) {
					compressing.Write(buffer, 0, buffer.Length);
					totalRead += len;
					grfData.Progress = 50.0f + (totalRead / (float) totalLength * 100.0f) / 2;

					if (grfData.IsCancelling)
						throw new OperationCanceledException();
				}
			}

			File.Delete(source);
			File.Move(Path.Combine(Settings.TempPath, "~tmp.gz"), destination);
		}

		public static void GZipDecompress(IProgress container, string fileName, string decompressedFileName) {
			using (FileStream reader = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			using (GZipStream stream = new GZipStream(reader, CompressionMode.Decompress)) {
				const int Size = 8192;
				int fileSize = (int) reader.Length;

				byte[] buffer = new byte[Size];
				using (FileStream writer = new FileStream(decompressedFileName, FileMode.Create, FileAccess.Write)) {
					int count;
					while ((count = stream.Read(buffer, 0, Size)) > 0) {
						writer.Write(buffer, 0, count);
						container.Progress = stream.BaseStream.Position / (float) fileSize * 50f;

						if (container.IsCancelling)
							throw new OperationCanceledException();
					}
				}
			}
		}

		public static void GZipDecompress(IProgress container, ByteReaderStream readerB, string decompressedFileName) {
			using (Stream reader = readerB.Stream)
			using (var stream = new GZipInputStream(reader)) {
				const int Size = 8192;
				int fileSize = (int) reader.Length;

				byte[] buffer = new byte[Size];
				using (FileStream writer = new FileStream(decompressedFileName, FileMode.Create, FileAccess.Write)) {
					int count;
					while ((count = stream.Read(buffer, 0, Size)) > 0) {
						writer.Write(buffer, 0, count);
						container.Progress = stream.Position / (float) fileSize * 50f;

						if (container.IsCancelling)
							throw new OperationCanceledException();
					}
				}
			}
		}
	}
}