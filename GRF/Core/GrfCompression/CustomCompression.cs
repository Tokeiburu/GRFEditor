using System;
using System.IO;
using System.Runtime.InteropServices;
using GRF.ContainerFormat;
using Utilities;

namespace GRF.Core.GrfCompression {
	/// <summary>
	/// Custom compression, from any dll.
	/// </summary>
	public class CustomCompression : ICompression, IDisposable {
		#region Delegates

		public delegate int CompressMethod(byte[] buffer, ref int compressedLength, byte[] uncompressed, int uncompressedLength, int level);

		public delegate int DecompressMethod(byte[] uncompressed, ref int uncompressedLength, byte[] compressed, int compressedLength);

		#endregion

		private readonly string _path;

		protected CompressMethod _compress;
		protected DecompressMethod _decompress;
		protected int _compressionLevel = 0;

		private bool _disposed;
		protected IntPtr _hModule;

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomCompression" /> class.
		/// </summary>
		public CustomCompression() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomCompression" /> class.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="setting">The setting.</param>
		public CustomCompression(string path, Setting setting) {
			_path = path;
			_init(setting);
		}

		public string FilePath {
			get { return _path; }
		}

		#region ICompression Members

		public void Dispose() {
			Dispose(_disposed);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets or sets a value indicating whether loading this compression has been a success.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Compresses the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>
		/// The compressed data.
		/// </returns>
		public byte[] Compress(Stream stream) {
			byte[] uncompressed = new byte[stream.Length];
			stream.Read(uncompressed, 0, uncompressed.Length);

			int length = 1024 + 2 * uncompressed.Length;
			byte[] compressedBuffer = new byte[length];

			if (_compress(compressedBuffer, ref length, uncompressed, uncompressed.Length, _compressionLevel) != 0) {
				throw GrfExceptions.__FailedToCompressData.Create();
			}

			byte[] compressed = new byte[length];
			Buffer.BlockCopy(compressedBuffer, 0, compressed, 0, length);
			return compressed;
		}

		/// <summary>
		/// Compresses the byte array.
		/// </summary>
		/// <param name="uncompressed">The uncompressed byte array.</param>
		/// <returns>
		/// The compressed data.
		/// </returns>
		public byte[] Compress(byte[] uncompressed) {
			int length = 1024 + 2 * uncompressed.Length;
			byte[] compressedBuffer = new byte[length];

			if (_compress(compressedBuffer, ref length, uncompressed, uncompressed.Length, _compressionLevel) != 0) {
				throw GrfExceptions.__FailedToCompressData.Create();
			}

			byte[] compressed = new byte[length];
			Buffer.BlockCopy(compressedBuffer, 0, compressed, 0, length);
			return compressed;
		}

		/// <summary>
		/// Decompresses the specified data, using a known length.
		/// </summary>
		/// <param name="compressed">The compressed data.</param>
		/// <param name="uncompressedLength">Length of the uncompressed data.</param>
		/// <returns>
		/// The uncompressed data.
		/// </returns>
		public virtual byte[] Decompress(byte[] compressed, long uncompressedLength) {
			return Decompress(compressed, compressed.Length, uncompressedLength);
		}

		/// <summary>
		/// Decompresses the specified data, using a known length.
		/// </summary>
		/// <param name="compressed">The compressed data.</param>
		/// <param name="compressedLength">Length of the compressed data (not aligned).</param>
		/// <param name="uncompressedLength">Length of the uncompressed data.</param>
		/// <returns>The uncompressed data.</returns>
		public virtual byte[] Decompress(byte[] compressed, long compressedLength, long uncompressedLength) {
			if (uncompressedLength == 0)
				return new byte[] { };

			int ptrLength = (int)uncompressedLength;
			byte[] decompressed = new byte[ptrLength];

			int result = _decompress(decompressed, ref ptrLength, compressed, (int)compressedLength);

			if (result != 0) {
				// Fix : 2015-07-21
				// Allow badly formed entry only if DotNet allows it.
				// The DotNet decompression doesn't look at the checksum.
				if (result == -3) {
					if (Compression.EnsureChecksum) {
						bool error = false;

						if (this is CpsCompression || (this is LzmaCompression && compressed.Length > 0 && compressed[0] != 0)) {
							try {
								Compression.DecompressDotNet(compressed);
							}
							catch {
								error = true;
							}

							if (!error) {
								throw GrfExceptions.__ChecksumFailed.Create();
							}
						}
					}
					else if (this is CpsCompression || (this is LzmaCompression && compressed.Length > 0 && compressed[0] != 0)) {
						try {
							return Compression.DecompressDotNet(compressed);
						}
						catch {
						}
					}
				}

				throw GrfExceptions.__FailedToDecompressData.Create();
			}

			return decompressed;
		}

		#endregion

		private void _init(Setting setting) {
			try {
				// Fix : 2015-04-04
				// The setting acts as a guard against DLL that crashes the application
				if ((bool) setting.Get()) {
					throw GrfExceptions.__CompressionDllGuard.Create();
				}

				setting.Set(true);
				_hModule = NativeMethods.LoadLibrary(_path);

				if (_hModule == IntPtr.Zero) {
					Success = false;
					throw GrfExceptions.__LoadLibraryFailed.Create(_path, NativeMethods.GetLastError(), NativeMethods.GetLastError());
				}

				IntPtr intPtr = NativeMethods.GetProcAddress(_hModule, "uncompress");

				if (intPtr == IntPtr.Zero)
					throw GrfExceptions.__CompressionDllFailed3.Create(_path, "uncompress", "uncompress(unsigned char* output_data, int* output_len, unsigned char* compressed_data, int compressed_len)");

				_decompress = (DecompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (DecompressMethod));

				intPtr = NativeMethods.GetProcAddress(_hModule, "compress2");

				if (intPtr == IntPtr.Zero)
					intPtr = NativeMethods.GetProcAddress(_hModule, "compress");

				if (intPtr == IntPtr.Zero)
					throw GrfExceptions.__CompressionDllFailed3.Create(_path, "compress (and compress2)", "compress(unsigned char* output_data, int* output_len, unsigned char* uncompressed_data, int uncompressed_len)");

				_compress = (CompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (CompressMethod));

				Success = true;
			}
			catch {
				Success = false;
				throw;
			}
			finally {
				setting.Set(false);
			}
		}

		~CustomCompression() {
			Dispose(_disposed);
		}

		public void Dispose(bool disposing) {
			if (!_disposed) {
				NativeMethods.FreeLibrary(_hModule);
				_disposed = true;
			}
		}

		public override string ToString() {
			try {
				if (!String.IsNullOrEmpty(_path))
					return GrfStrings.CustomCompression + " (" + Path.GetFileName(_path) + ")";

				return GrfStrings.CustomCompression;
			}
			catch {
				return GrfStrings.CustomCompression;
			}
		}
	}
}