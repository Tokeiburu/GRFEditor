using GRF.GrfSystem;
using GRF.Threading;
using System;
using System.IO;
using System.IO.Compression;

namespace GRF.Core.GrfCompression {
	/// <summary>
	/// DotNet's compression.
	/// </summary>
	public class GZipCompression : ICompression, IDisposable {
		/// <summary>
		/// Initializes a new instance of the <see cref="GZipCompression" /> class.
		/// </summary>
		public GZipCompression() {
			Success = true;
		}

		#region ICompression Members

		/// <summary>
		/// Gets or sets a value indicating whether loading this compression has been a success.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Compresses the specified in memory stream.
		/// </summary>
		/// <param name="stream">The in memory stream.</param>
		/// <returns></returns>
		public byte[] Compress(Stream stream) {
			using (MemoryStream output = new MemoryStream())
			using (GZipStream compressing = new GZipStream(output, CompressionMode.Compress))
			{
				byte[] buffer = new byte[131072];
				int len;
				long totalRead = 0;
				long totalLength = stream.Length;

				while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) {
					compressing.Write(buffer, 0, buffer.Length);
					totalRead += len;
				}

				return output.ToArray();
			}
		}

		/// <summary>
		/// Compresses the byte array.
		/// </summary>
		/// <param name="uncompressed">The uncompressed byte array.</param>
		/// <returns>
		/// The compressed data.
		/// </returns>
		public byte[] Compress(byte[] uncompressed) {
			return Compress(new MemoryStream(uncompressed));
		}

		/// <summary>
		/// Decompresses the specified data, using a known length.
		/// </summary>
		/// <param name="compressed">The compressed data.</param>
		/// <param name="uncompressedLength">Length of the uncompressed data.</param>
		/// <returns>
		/// The uncompressed data.
		/// </returns>
		public byte[] Decompress(byte[] compressed, long uncompressedLength) {
			return Decompress(compressed, compressed.Length, uncompressedLength);
		}

		/// <summary>
		/// Decompresses the specified data, using a known length.
		/// </summary>
		/// <param name="compressed">The compressed data.</param>
		/// <param name="compressedLength">Length of the compressed data (not aligned).</param>
		/// <param name="uncompressedLength">Length of the uncompressed data.</param>
		/// <returns>The uncompressed data.</returns>
		public byte[] Decompress(byte[] compressed, long compressedLength, long uncompressedLength) {
			if (uncompressedLength == 0)
				return new byte[] { };

			using (MemoryStream outputStream = new MemoryStream())
			using (MemoryStream inputStream = new MemoryStream(compressed))
			using (GZipStream stream = new GZipStream(inputStream, CompressionMode.Decompress)) {
				const int Size = 8192;
				byte[] buffer = new byte[Size];
				int count;
				while ((count = stream.Read(buffer, 0, Size)) > 0) {
					outputStream.Write(buffer, 0, count);
				}

				return outputStream.ToArray();
			}
		}

		public void CompressFile(IProgress grfData, string source, string destination) {
			using (FileStream output = File.OpenWrite(Path.Combine(Settings.TempPath, "~tmp.gz")))
			using (GZipStream compressing = new GZipStream(output, CompressionMode.Compress))
			using (FileStream file = File.OpenRead(source)) {
				byte[] buffer = new byte[131072];
				int len;
				long totalRead = 0;
				long totalLength = file.Length;

				while ((len = file.Read(buffer, 0, buffer.Length)) > 0) {
					compressing.Write(buffer, 0, buffer.Length);
					totalRead += len;
					grfData.Progress = 50.0f + (totalRead / (float)totalLength * 100.0f) / 2;

					if (grfData.IsCancelling)
						throw new OperationCanceledException();
				}
			}

			File.Delete(source);
			File.Move(Path.Combine(Settings.TempPath, "~tmp.gz"), destination);
		}

		public void DecompressFile(IProgress container, Stream intput, string decompressedFileName) {
			using (GZipStream stream = new GZipStream(intput, CompressionMode.Decompress)) {
				const int Size = 8192;
				int fileSize = (int)intput.Length;

				byte[] buffer = new byte[Size];
				using (FileStream writer = new FileStream(decompressedFileName, FileMode.Create, FileAccess.Write)) {
					int count;
					while ((count = stream.Read(buffer, 0, Size)) > 0) {
						writer.Write(buffer, 0, count);
						container.Progress = stream.BaseStream.Position / (float)fileSize * 50f;

						if (container.IsCancelling)
							throw new OperationCanceledException();
					}
				}
			}
		}

		public void DecompressFile(IProgress container, string fileName, string decompressedFileName) {
			using (FileStream input = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
				DecompressFile(container, input, decompressedFileName);
			}
		}

		public void Dispose() {
		}

		#endregion

		public override string ToString() {
			return GrfStrings.DisplayGZip;
		}
	}
}