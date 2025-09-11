using System;
using System.IO;
using System.IO.Compression;
using ComponentAce.Compression.Libs.zlib;
using GRF.GrfSystem;

namespace GRF.Core.GrfCompression {
	/// <summary>
	/// DotNet's compression.
	/// </summary>
	public class DotNetCompression : ICompression, IDisposable {
		/// <summary>
		/// Initializes a new instance of the <see cref="DotNetCompression" /> class.
		/// </summary>
		public DotNetCompression() {
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
		/// <param name="inMemoryStream">The in memory stream.</param>
		/// <returns></returns>
		public byte[] Compress(Stream inMemoryStream) {
			MemoryStream oOutStream = new MemoryStream();
			ZOutputStream ostream = new ZOutputStream(oOutStream, Settings.CompressionLevel);

			try {
				Compression.CopyStream(inMemoryStream, ostream);
				ostream.finish();
				return oOutStream.ToArray();
			}
			finally {
				ostream.Close();
				oOutStream.Close();
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

			using (MemoryStream decompressStream = new MemoryStream())
			using (DeflateStream decompressionStream = new DeflateStream(new MemoryStream(compressed, 2, (int)compressedLength - 4), CompressionMode.Decompress)) {
				Compression.CopyStream(decompressionStream, decompressStream);
				return decompressStream.ToArray();
			}
		}

		public void Dispose() {
		}

		#endregion

		public override string ToString() {
			return GrfStrings.DisplayGrfEditorDll;
		}
	}
}