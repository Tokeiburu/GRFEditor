using GRF.ContainerFormat;

namespace GRF.Core.GrfCompression {
	/// <summary>
	/// Doesn't compress or uncompress any files.
	/// </summary>
	public class RecoveryCompression : CpsCompression {
		/// <summary>
		/// Initializes a new instance of the <see cref="RecoveryCompression" /> class.
		/// </summary>
		public RecoveryCompression() {
			_init();
		}

		/// <summary>
		/// Decompresses the specified data, using a known length.
		/// </summary>
		/// <param name="compressed">The compressed data.</param>
		/// <param name="uncompressedLength">Length of the uncompressed data.</param>
		/// <returns>
		/// The uncompressed data.
		/// </returns>
		public override byte[] Decompress(byte[] compressed, long uncompressedLength) {
			if (uncompressedLength == 0)
				return new byte[] { };

			int ptrLength = (int)uncompressedLength;
			byte[] decompressed = new byte[ptrLength];

			int result = _decompress(decompressed, ref ptrLength, compressed, compressed.Length);

			if (result != 0 && result != -3) {
				throw GrfExceptions.__FailedToDecompressData.Create();
			}

			return decompressed;
		}

		public override string ToString() {
			return GrfStrings.RecoveryCompression;
		}
	}
}