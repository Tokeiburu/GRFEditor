using System;
using System.IO;
using System.Runtime.InteropServices;
using Encryption;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities;

namespace GRF.Core {
	public interface IEncryption {
		bool Success { get; }
		Exception LastException { get; }
		void Encrypt(byte[] key, byte[] data, int uncompressLength);
		void Decrypt(byte[] key, byte[] data, int uncompressLength);
	}

	public class GrfEditorEncryption : IEncryption {
		public bool Success {
			get { return true; }
		}

		public Exception LastException {
			get { return null; }
		}

		public void Encrypt(byte[] key, byte[] data, int uncompressLength) {
			Ee322.ebfd0ac060c6a005e565726f05d6aac8(key, data, uncompressLength);
		}

		public void Decrypt(byte[] key, byte[] data, int uncompressLength) {
			Ee322.f8881b1c7355d58161c07ae1c35cfb13(key, data, uncompressLength);
		}
	}

	public class CustomEncryption : IEncryption {
		public delegate int EncryptionMethod(byte[] key, int key_len, byte[] data, int data_len, int uncomp_len);
		public delegate int DecryptionMethod(byte[] key, int key_len, byte[] data, int data_len, int uncomp_len);
		private readonly string _path;

		protected EncryptionMethod _encrypt;
		protected DecryptionMethod _decrypt;

		protected IntPtr _hModule;

		public CustomEncryption(string path, Setting setting) {
			_path = path;
			LastException = null;

			try {
				// Fix : 2015-04-04
				// The setting acts as a guard against DLL that crashes the application
				if ((bool)setting.Get()) {
					throw GrfExceptions.__CompressionDllGuard.Create();
				}

				setting.Set(true);
				_hModule = NativeMethods.LoadLibrary(_path);

				if (_hModule == IntPtr.Zero) {
					uint error = NativeMethods.GetLastError();
					ImageFileHeader.IMAGE_FILE_HEADER imageFileHeader = new ImageFileHeader.IMAGE_FILE_HEADER();
					bool fileHeaderLoaded = false;

					// Check if there's a x86 vs x64 issue first
					try {
						imageFileHeader = ImageFileHeader.GetImageFileHeader(File.ReadAllBytes(_path));
						fileHeaderLoaded = true;
					}
					catch {
					}

					if (fileHeaderLoaded) {
						if (Wow.Is64BitProcess == ImageFileHeader.Is32BitHeader(imageFileHeader)) {
							throw GrfExceptions.__EncryptionDllFailed2.Create(_path, error, Wow.Is64BitProcess ? "64" : "32", Wow.Is64BitProcess ? "32" : "64");
						}
					}

					throw GrfExceptions.__EncryptionDllFailed.Create(_path, error);
				}

				IntPtr intPtr = NativeMethods.GetProcAddress(_hModule, "encrypt");

				if (intPtr == IntPtr.Zero)
					throw GrfExceptions.__DllMissingFunction.Create("encrypt");

				_encrypt = (EncryptionMethod)Marshal.GetDelegateForFunctionPointer(intPtr, typeof(EncryptionMethod));

				intPtr = NativeMethods.GetProcAddress(_hModule, "decrypt");

				if (intPtr == IntPtr.Zero)
					throw GrfExceptions.__DllMissingFunction.Create("decrypt");

				_decrypt = (DecryptionMethod)Marshal.GetDelegateForFunctionPointer(intPtr, typeof(DecryptionMethod));
				Success = true;
			}
			catch (Exception err) {
				LastException = err;
				Success = false;
			}
			finally {
				setting.Set(false);
			}
		}

		public bool Success { get; private set; }
		public Exception LastException { get; private set; }

		public void Encrypt(byte[] key, byte[] data, int uncompressLength) {
			_encrypt(key, key.Length, data, data.Length, uncompressLength);
		}

		public void Decrypt(byte[] key, byte[] data, int uncompressLength) {
			_decrypt(key, key.Length, data, data.Length, uncompressLength);
		}
	}

	public sealed class Encryption {
		public static IEncryption DefaultEncryption = new GrfEditorEncryption();
		
		public static IEncryption Encryptor = DefaultEncryption;

		public static void Decrypt(byte[] key, byte[] data, int sizeDecompressed) {
			Encryptor.Decrypt(key, data, sizeDecompressed);
		}

		public static void Encrypt(byte[] key, byte[] data, int sizeDecompressed) {
			Encryptor.Encrypt(key, data, sizeDecompressed);
		}
	}
}
