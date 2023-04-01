using System;
using System.IO;

namespace GRF.Core.GrfCompression {
	public interface ICompression : IDisposable {
		/// <summary>
		/// Gets or sets a value indicating whether loading this compression has been a success.
		/// </summary>
		bool Success { get; set; }

		/// <summary>
		/// Compresses the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>The compressed data.</returns>
		byte[] Compress(Stream stream);

		/// <summary>
		/// Compresses the byte array.
		/// </summary>
		/// <param name="uncompressed">The uncompressed byte array.</param>
		/// <returns>The compressed data.</returns>
		byte[] Compress(byte[] uncompressed);

		/// <summary>
		/// Decompresses the specified data, using a known length.
		/// </summary>
		/// <param name="compressed">The compressed data.</param>
		/// <param name="uncompressedLength">Length of the uncompressed data.</param>
		/// <returns>The uncompressed data.</returns>
		byte[] Decompress(byte[] compressed, long uncompressedLength);
	}
}