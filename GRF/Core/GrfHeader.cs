using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.FileFormats;
using GRF.IO;
using Utilities.Extension;
using Utilities.Tools;

namespace GRF.Core {
	public class GrfHeader : FileHeader {
		public const int StructSize = 46;
		private readonly List<string> _errors = new List<string>();

		internal GrfHeader(IBinaryReader reader, Container container) {
			Container = container;

			if (reader.LengthLong < StructSize)
				throw GrfExceptions.__HeaderLengthInvalid.Create(reader.Length, StructSize);

			byte[] data = reader.Bytes(StructSize);

			Magic = Encoding.ASCII.GetString(data, 0, 16);

			if (Magic.ToLower() != "master of magic\0") {
				// Attempt to read as alpha GRF
				reader.PositionUInt = (UInt32)reader.LengthLong - 9;
				FileTableOffset = reader.UInt32();
				RealFilesCount = reader.Int32();
				RealFilesCount = (RealFilesCount << 16) | (RealFilesCount >> 16);
				MajorVersion = 0;
				MinorVersion = reader.Byte();
				Seed = 0;

				if (FileTableOffset < reader.LengthLong) {
					Magic = "Master of Magic\0";
					Key = Encoding.ASCII.GetString(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }); // Enable encryption
					return;
				}

				SetError(GrfStrings.FailedGrfHeader, Magic);
			}

			Key = Encoding.ASCII.GetString(data, 16, 14);
			FileTableOffset = BitConverter.ToUInt32(data, 30);
			Seed = BitConverter.ToInt32(data, 34);
			_filesCount = BitConverter.ToInt32(data, 38);
			int version = BitConverter.ToInt32(data, 42);
			MajorVersion = (byte) (version >> 8);
			MinorVersion = (byte) (version & 0x000000FF);
			RealFilesCount = _filesCount - Seed - 7;
		}

		internal GrfHeader(Container container) {
			Container = container;
			Magic = "Master of Magic\0";
			Key = Encoding.ASCII.GetString(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14}); // Enable encryption
			Seed = 0;
			FileTableOffset = StructSize;
			MajorVersion = 2;
			MinorVersion = 0;
			RealFilesCount = 0;
		}

		public bool EncryptFileTable { get; private set; }
		internal Container Container { get; private set; }

		public string Key { get; set; }
		public uint FileTableOffset { get; set; }
		public int Seed { get; set; }
		private int _filesCount { get; set; }
		internal int RealFilesCount { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the GrfWriter should encrypt all data using the assigned encryption key.
		/// </summary>
		public bool IsEncrypting { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the GrfWriter should decrypt all data using the assigned encryption key.
		/// </summary>
		public bool IsDecrypting { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the GRF should enable the encryption feature. It is enabled if the hidden encryption file is present (GrfStrings.EncryptionFilename) or if the EncryptFiles/DecryptFiles commands have been used. Do not set manually, and rather use EncryptionCheckFlag for personal use.
		/// </summary>
		public bool IsEncrypted { get; set; }

		public bool FoundErrors {
			get { return _errors.Count > 0; }
		}

		/// <summary>
		/// Gets or sets the 256-byte encryption key.
		/// </summary>
		public byte[] EncryptionKey { get; set; }

		/// <summary>
		/// If enabled, FileEntry will check for potential encrypted content when being decompressed. This flag is required whether a key is set or not (use the SetKey function first).
		/// If disabled, the encryption feature will not be available.
		/// GRF Editor will turn off this flag after scanning the entire GRF for encrypted files and will use a different system to check whether an entry is encrypted or not. Therefore, this flag is only relevant if you aren't using the UI and rather the GRF library directly.
		/// </summary>
		public bool EncryptionCheckFlag { get; set; }

		public ReadOnlyCollection<string> Errors {
			get { return _errors.AsReadOnly(); }
		}

		public void Write(Stream grfStream) {
			using (BinaryWriter writer = new BinaryWriter(new MemoryStream())) {
				writer.Write("Master of Magic\0".Bytes(16, Encoding.ASCII), 0, 16);
				writer.Write(Key.Bytes(14, Encoding.ASCII), 0, 14);
				writer.Write(FileTableOffset);
				writer.Write(Seed);
				writer.Write(RealFilesCount + 7 + Seed);
				writer.Write((MajorVersion << 8) + MinorVersion);
				grfStream.Write(((MemoryStream) writer.BaseStream).GetBuffer(), 0, (int) writer.BaseStream.Position);
			}
		}

		internal void SetError(string error, params object[] args) {
			_errors.Add(String.Format(error, args));
		}

		public override sealed void SetVersion(byte major, byte minor) {
			throw GrfExceptions.__ChangeVersionNotAllowed.Create();
		}

		internal void SetGrfVersion(byte major, byte minor) {
			MajorVersion = major;
			MinorVersion = minor;
		}

		public void SetKey(byte[] key, GrfHolder grf) {
			try {
				EncryptionKey = key;
				if (key == null) return;

				if (grf.FileTable.Contains(GrfStrings.EncryptionFilename) && !grf.FileTable[GrfStrings.EncryptionFilename].Added) {
					if (grf.FileTable[GrfStrings.EncryptionFilename].SizeDecompressed == 0)
						throw GrfExceptions.__UnsupportedEncryption.Create();

					if (Crc32.Compute(key) != BitConverter.ToUInt32(grf.FileTable[GrfStrings.EncryptionFilename].GetDecompressedData(), 0))
						throw GrfExceptions.__WrongKeyFile.Create();
				}
			}
			catch {
				EncryptionKey = null;
				throw;
			}
		}

		/// <summary>
		/// Sets the encryption key for encrypting the entire GRF. This is a special function only used for forcing the the GrfWriter to encrypt all the entries after saving.
		/// Since it ignores the command stack, the GRF should be reloaded after using this function.
		/// </summary>
		/// <param name="key">The 256-byte encryption key.</param>
		/// <param name="grf">The GRF.</param>
		public void SetEncryption(byte[] key, GrfHolder grf) {
			try {
				IsDecrypting = false;
				IsEncrypting = true;
				SetKey(key, grf);
			}
			catch (Exception ex) {
				IsDecrypting = false;
				IsEncrypting = false;
				throw new Exception(GrfStrings.EncryptionNotSet, ex);
			}
		}

		/// <summary>
		/// Sets the encryption key for decrypting the entire GRF. This is a special function only used for forcing the the GrfWriter to decrypt all the entries after saving.
		/// Since it ignores the command stack, the GRF should be reloaded after using this function.
		/// </summary>
		/// <param name="key">The 256-byte encryption key.</param>
		/// <param name="grf">The GRF.</param>
		public void SetDecryption(byte[] key, GrfHolder grf) {
			try {
				IsDecrypting = true;
				IsEncrypting = false;
				SetKey(key, grf);
			}
			catch (Exception ex) {
				IsDecrypting = false;
				IsEncrypting = false;
				throw new Exception(GrfStrings.DecryptionNotSet, ex);
			}
		}

		/// <summary>
		/// Sets whether or not the file table should be encrypted.
		/// </summary>
		/// <param name="value">Flag.</param>
		public void SetFileTableEncryption(bool value) {
			EncryptFileTable = value;
		}
	}
}