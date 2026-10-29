using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats;
using GRF.IO;
using Utilities.Extension;
using Utilities.Tools;

namespace GRF.Core {
	public class GrfHeader : FileHeader {
		public const int DataByteSize = 46;
		private readonly List<GrfLoadError> _errors = new List<GrfLoadError>();
		private object _errorLock = new object();

		static GrfHeader() {
			KRODefaultProtectedKeys.Add(1, new byte[] { 0x2c, 0x94, 0x21, 0x96, 0x40, 0x67, 0x35, 0xfe, 0xd0, 0xc6, 0xfc, 0x9e, 0x15, 0xb9, 0x48, 0x06, 0x17, 0x19, 0x6b, 0x53, 0x4e, 0xaf, 0x25, 0xac, 0x03, 0x50, 0x81, 0xf2, 0xb6, 0xd4, 0xa3, 0x71, 0x51, 0xcd, 0xaf, 0x68, 0x47, 0x78, 0x6d, 0xd3, 0xe4, 0x04, 0x03, 0x2f, 0x86, 0x03, 0x38, 0x3e, 0xc7, 0x49, 0x20, 0x03, 0x8a, 0x9e, 0x63, 0x04, 0x07, 0x57, 0x7e, 0x0b });
			KRODefaultProtectedKeys.Add(2, new byte[] { 0x2a, 0x08, 0xda, 0x55, 0x41, 0x0a, 0x5d, 0xd3, 0x4a, 0xb5, 0xc1, 0xa4, 0x04, 0xb1, 0x67, 0xd9, 0xcd, 0x6b, 0x60, 0xe3, 0x49, 0xfa, 0x8a, 0x60, 0x25, 0xbd, 0x0d, 0x28, 0x2c, 0x05, 0x50, 0xbd, 0x4c, 0x60, 0x67, 0x59, 0xc3, 0xdc, 0x9e, 0xb2, 0x56, 0xb1, 0x45, 0x5b, 0x74, 0x3e, 0x81, 0xf9, 0xbc, 0x8a, 0x06, 0x6e, 0x76, 0xb6, 0x05, 0x4f, 0x11, 0xc1, 0xa3, 0x01 });
			KRODefaultProtectedKeys.Add(3, new byte[] { 0x1e, 0x5a, 0xb5, 0x22, 0xd9, 0x73, 0x34, 0xc9, 0x9b, 0xe5, 0x41, 0x33, 0x36, 0x17, 0x7a, 0xbe, 0x59, 0xe6, 0x77, 0x8c, 0xe6, 0xd5, 0x4a, 0xa3, 0xdf, 0x2b, 0x26, 0x56, 0xb1, 0x80, 0xde, 0xfa, 0xe6, 0x7c, 0x0a, 0x11, 0xf5, 0x54, 0xce, 0x76, 0x15, 0xbf, 0xe6, 0xc4, 0x13, 0x02, 0xfe, 0x05, 0x9c, 0xe0, 0xec, 0x6f, 0x59, 0xec, 0x7e, 0xd3, 0x49, 0x3b, 0x3d, 0x93 });
			KRODefaultProtectedKeys.Add(4, new byte[] { 0x58, 0x15, 0x32, 0x06, 0x74, 0x9f, 0x6d, 0x2a, 0x33, 0xa2, 0x0d, 0x4e, 0x51, 0xc7, 0xf5, 0x65, 0xb3, 0xc8, 0x04, 0xc8, 0xd7, 0x61, 0x71, 0x76, 0x64, 0xdb, 0x6b, 0x81, 0xdd, 0xbf, 0x54, 0xe1, 0x4b, 0x07, 0x7c, 0x72, 0x8b, 0xad, 0x59, 0x0a, 0x69, 0xe7, 0x6e, 0xa0, 0xd3, 0x80, 0x5c, 0xcb, 0x17, 0x28, 0x35, 0xd2, 0x1a, 0xa6, 0xec, 0x46, 0xfd, 0x95, 0x29, 0x8c });
			
			JRODefaultProtectedKeys.Add(1, new byte[] { 0xfb, 0x04, 0x3c, 0x63, 0x2c, 0x60, 0xa9, 0x33, 0xe4, 0x00, 0x0d, 0xf9, 0x4d, 0xd7, 0x72, 0xf4, 0xc6, 0xe7, 0x89, 0xe0, 0xb3, 0x20, 0x77, 0xb0, 0x01, 0x5e, 0x38, 0x12, 0x3d, 0x19, 0xe9, 0x63, 0xb1, 0xd3, 0x05, 0x78, 0x74, 0x99, 0x0d, 0x62, 0x6a, 0x58, 0x4b, 0xe0, 0xa9, 0x90, 0xcb, 0x8f, 0x1c, 0x43, 0x6f, 0x53, 0xc3, 0xc1, 0xc8, 0x8f, 0xb9, 0x07, 0x3b, 0xd1 });
			JRODefaultProtectedKeys.Add(2, new byte[] { 0x4f, 0x7a, 0xef, 0xb5, 0xbb, 0xae, 0x61, 0x67, 0x52, 0x96, 0xaa, 0x46, 0x6d, 0x2f, 0x5d, 0x7c, 0xfb, 0x6a, 0x79, 0xcf, 0xee, 0x46, 0x04, 0x81, 0xae, 0xb2, 0x30, 0x67, 0x87, 0x4a, 0x94, 0x8b, 0x6f, 0x36, 0xb7, 0xcc, 0x76, 0x20, 0xe2, 0x0f, 0xc3, 0x96, 0x0e, 0xfd, 0x29, 0x8b, 0x3e, 0xca, 0xba, 0x0b, 0x8f, 0xe7, 0x62, 0x6d, 0xd2, 0xb2, 0x93, 0xdc, 0xee, 0x17 });
			JRODefaultProtectedKeys.Add(3, new byte[] { 0xe4, 0xe6, 0x9f, 0xe3, 0x9b, 0x8c, 0xd0, 0x79, 0xaa, 0xe0, 0x2e, 0x7c, 0x8f, 0xfc, 0xf2, 0xf4, 0xa8, 0xcc, 0xfa, 0xb1, 0xa9, 0x4b, 0xd2, 0xca, 0xae, 0x5f, 0x74, 0x7c, 0x94, 0x5f, 0x8f, 0x80, 0xab, 0xe1, 0xe2, 0xf8, 0xfe, 0xb4, 0x3e, 0x22, 0xdf, 0xe8, 0x96, 0x58, 0x03, 0x89, 0x50, 0x32, 0xd4, 0x76, 0x9a, 0x35, 0x51, 0x74, 0x19, 0xed, 0xff, 0x0b, 0xfa, 0x97 });
		}

		internal GrfHeader(ByteReaderStream reader, Container container) {
			Container = container;

			if (reader.LengthLong < DataByteSize)
				throw GrfExceptions.__HeaderLengthInvalid.Create(reader.Length, DataByteSize);

			byte[] data = reader.Bytes(DataByteSize);

			Magic = Encoding.ASCII.GetString(data, 0, 16);
			Key = Encoding.ASCII.GetString(data, 16, 14);

			int version = BitConverter.ToInt32(data, 42);
			MajorVersion = (byte)(version >> 8);
			MinorVersion = (byte)(version & 0x000000FF);

			// Fix : 2024-10-25
			// Added support for int64 size GRFs
			if (this.Is(3, 0) && data[35] == 0 && data[36] == 0 && data[37] == 0) {
				FileTableOffset = BitConverter.ToInt64(data, 30);
				Seed = 0;
				RealFilesCount = _filesCount = BitConverter.ToInt32(data, 38);
			}
			else {
				FileTableOffset = BitConverter.ToUInt32(data, 30);
				Seed = BitConverter.ToInt32(data, 34);
				_filesCount = BitConverter.ToInt32(data, 38);
				RealFilesCount = _filesCount - Seed - 7;
			}

			// This is a GRF header, don't check further
			if (this.Is(1, 2) ||
				this.Is(1, 3) ||
				this.Is(2, 0) ||
				this.Is(3, 0)) {
				return;
			}

			// Attempt to read as alpha GRF
			if (Magic.ToLower() != GrfStrings.MasterOfMagic.ToLowerInvariant()) {
				reader.PositionUInt = (UInt32)reader.LengthLong - 9;
				FileTableOffset = reader.UInt32();
				RealFilesCount = reader.Int32();
				RealFilesCount = (RealFilesCount << 16) | (RealFilesCount >> 16);
				MajorVersion = 0;
				MinorVersion = reader.Byte();
				Seed = 0;

				if (FileTableOffset < reader.LengthLong) {
					Magic = GrfStrings.MasterOfMagic;
					Key = Encoding.ASCII.GetString(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }); // Enable encryption
					return;
				}

				SetError(GrfStrings.FailedData, "Magic (0x00): " + Magic + "\n\tExpected 'Master of Magic\\0'");
				SetError(GrfStrings.FailedData, "Unknown GRF version\n\tExpected 0x102, 0x103, 0x200 or 0x300.");
				SetError(GrfStrings.FailedData, "Attempted to read as Alpha GRF, but FileTableOffset value is invalid.\n\tFound " + FileTableOffset + " / " + reader.LengthLong);
				SetError(GrfStrings.FailedData, "Additional header data:");
				SetError(GrfStrings.FailedData, "- FileTableOffset (0x1e): " + BitConverter.ToUInt32(data, 30));
				SetError(GrfStrings.FailedData, "- Seed (0x22): " + BitConverter.ToInt32(data, 34));
				SetError(GrfStrings.FailedData, "- _filesCount (0x26): " + BitConverter.ToInt32(data, 38));
				SetError(GrfStrings.FailedData, "- MajorVersion (0x2b): " + (byte)(version >> 8));
				SetError(GrfStrings.FailedData, "- MinorVersion (0x2a): " + (byte)(version & 0x000000FF));
			}
		}

		internal GrfHeader(Container container) {
			Container = container;
			Magic = GrfStrings.MasterOfMagic;
			Key = Encoding.ASCII.GetString(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }); // Enable encryption
			Seed = 0;
			FileTableOffset = DataByteSize;
			MajorVersion = 2;
			MinorVersion = 0;
			RealFilesCount = 0;
		}

		public bool EncryptFileTable { get; private set; }
		public bool DecryptFileTable { get; internal set; }
		internal Container Container { get; private set; }
		public ThorSettings ThorSettings = new ThorSettings();

		// Gravity Encryption data
		internal byte[] ProtectedKeyBlock = new byte[0];
		public static Dictionary<uint, byte[]> KRODefaultProtectedKeys = new Dictionary<uint, byte[]>();
		public static Dictionary<uint, byte[]> JRODefaultProtectedKeys = new Dictionary<uint, byte[]>();
		public static Dictionary<uint, byte[]> CnRODefaultProtectedKeys = new Dictionary<uint, byte[]>();
		public IDictionary<uint, byte[]> ProtectedKeys = new Dictionary<uint, byte[]>();
		public bool HasProtectedKeys => ProtectedKeys.Count > 0;

		public string Key { get; set; }
		public long FileTableOffset { get; set; }
		public int Seed { get; set; }
		private int _filesCount;
		internal int RealFilesCount { get; set; }
		public GrfFormatView FormatView => GrfFormatViews.Get(MajorVersion, MinorVersion);

		/// <summary>
		/// Gets or sets a value indicating whether the GRF should enable the encryption feature. It is enabled if the hidden encryption file is present (GrfStrings.EncryptionFilename) or if the EncryptFiles/DecryptFiles commands have been used. Do not set manually, and rather use EncryptionCheckFlag for personal use.
		/// </summary>
		public bool IsEncrypted { get; set; }

		public bool HasErrors {
			get { return _errors.Any(p => p.Severity == ErrorLevel.Critical); }
		}

		public bool HasWarnings {
			get { return _errors.All(p => p.Severity == ErrorLevel.Warning); }
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
		internal bool EncryptionCheckFlag { get; set; }

		/// <summary>
		/// If enabled, FileEntry will check for potential encrypted content when being decompressed.
		/// </summary>
		internal bool EncryptionManualSet { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the FileTable requires decryption.
		/// </summary>
		public bool FileTableRequiresDecryption { get; private set; }
		
		internal uint EncryptionHashValue;

		public ReadOnlyCollection<GrfLoadError> Errors {
			get { return _errors.AsReadOnly(); }
		}

		public void Write(Stream grfStream) {
			using (BinaryWriter writer = new BinaryWriter(new MemoryStream())) {
				writer.Write(Magic.Bytes(16, Encoding.ASCII), 0, 16);
				writer.Write(Key.Bytes(14, Encoding.ASCII), 0, 14);

				if (this.IsCompatibleWith(3, 0)) {
					writer.Write(FileTableOffset);
					writer.Write(RealFilesCount);
				}
				else {
					writer.Write((uint)FileTableOffset);
					writer.Write(Seed);
					writer.Write(RealFilesCount + 7 + Seed);
				}

				writer.Write((MajorVersion << 8) + MinorVersion);
				grfStream.Write(((MemoryStream) writer.BaseStream).GetBuffer(), 0, (int) writer.BaseStream.Position);
			}
		}

		internal void SetError(string error, params object[] args) {
			lock (_errorLock) {
				_errors.Add(new GrfLoadError(String.Format(error, args)));
			}
		}

		internal void SetError(string error, ErrorLevel severity, params object[] args) {
			lock (_errorLock) {
				_errors.Add(new GrfLoadError(String.Format(error, args), severity));
			}
		}

		public override sealed void SetVersion(byte major, byte minor) {
			throw GrfExceptions.__ChangeVersionNotAllowed.Create();
		}

		internal void SetGrfVersion(byte major, byte minor) {
			MajorVersion = major;
			MinorVersion = minor;
		}

		internal void SetRequiresFileTableDecryption() {
			FileTableRequiresDecryption = true;
		}

		public void SetKey(byte[] key, FileTable fileTable) {
			try {
				EncryptionKey = key;
				if (key == null) return;

				if (fileTable.Contains(GrfStrings.EncryptionFilename) && !fileTable[GrfStrings.EncryptionFilename].Added) {
					if (fileTable[GrfStrings.EncryptionFilename].SizeDecompressed == 0)
						throw GrfExceptions.__UnsupportedEncryptionOperation.Create();

					if (Crc32.Compute(key) != EncryptionHashValue)
						throw GrfExceptions.__WrongKeyFile.Create();
				}
			}
			catch {
				EncryptionKey = null;
				throw;
			}
		}

		public void SetKey(byte[] key, GrfHolder grf) {
			SetKey(key, grf.FileTable);
		}

		/// <summary>
		/// Sets whether or not the file table should be encrypted.
		/// </summary>
		/// <param name="value">Flag.</param>
		public void SetFileTableEncryption(bool value) {
			if (Version < 2.0)
				throw GrfExceptions.__UnsupportedEncryptionOperation.Create();

			EncryptFileTable = value;
		}
	}
}
