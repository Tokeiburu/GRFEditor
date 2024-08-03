using System;
using System.ComponentModel;
using System.IO;
using Encryption;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;
using Utilities.Services;

namespace GRF.Core {
	public sealed class FileEntry : ContainerEntry, INotifyPropertyChanged {
		private string _fileName;
		private GrfHeader _header;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileEntry" /> class.
		/// This is a totally new FileEntry with empty fields, it's discouraged
		/// </summary>
		public FileEntry() {
			Cycle = -1;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileEntry" /> class.
		/// </summary>
		/// <param name="entry">The entry.</param>
		internal FileEntry(FileEntry entry) {
			RelativePath = entry.RelativePath;
			SizeCompressed = entry.SizeCompressed;
			SizeCompressedAlignment = entry.SizeCompressedAlignment;
			SizeDecompressed = entry.SizeDecompressed;
			Flags = entry.Flags;
			FileExactOffset = entry.FileExactOffset;
			Modification = entry.Modification;
			TemporarySizeCompressedAlignment = entry.TemporarySizeCompressedAlignment;
			TemporaryOffset = entry.TemporaryOffset;
			NewSizeCompressed = entry.NewSizeCompressed;
			NewSizeDecompressed = entry.NewSizeDecompressed;
			ExtractionFilePath = entry.ExtractionFilePath;
			SourceFilePath = entry.SourceFilePath;
			RawDataSource = entry.RawDataSource;
			Header = entry.Header;
			Cycle = entry.Cycle;
			Stream = entry.Stream;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileEntry" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="header">The header.</param>
		/// <param name="stream">The stream.</param>
		internal FileEntry(byte[] data, ref int offset, GrfHeader header, ByteReaderStream stream) {
			Header = header;
			Stream = stream;
			int endCharPosition = offset;

			while (data[endCharPosition] != 0x00) {
				endCharPosition++;
			}

			_fileName = EncodingService.DisplayEncoding.GetString(data, offset, endCharPosition - offset).Replace("/", "\\");

			if (_fileName.IndexOf('?', 0, _fileName.Length) > -1) {
				header.SetError(GrfStrings.FailedEncodingString, _fileName);
			}

			offset = endCharPosition + 1;

			NewSizeCompressed = SizeCompressed = BitConverter.ToInt32(data, offset);
			TemporarySizeCompressedAlignment = SizeCompressedAlignment = BitConverter.ToInt32(data, offset + 4);
			NewSizeDecompressed = SizeDecompressed = BitConverter.ToInt32(data, offset + 8);
			Flags = (EntryType) data[offset + 12];
			FileExactOffset = TemporaryOffset = BitConverter.ToUInt32(data, offset + 13) + GrfHeader.StructSize;

			switch (Flags) {
				case EntryType.FileAndHeaderCrypted:
					Cycle = 1;
					for (int i = 10; SizeCompressed >= i; i *= 10)
						Cycle++;
					break;
				case EntryType.FileAndDataCrypted:
					Cycle = 0;
					break;
				default:
					Cycle = -1;
					break;
			}
			offset = offset + 17;
		}

		/// <summary>
		/// Gets the header.
		/// </summary>
		internal GrfHeader Header {
			get { return _header; }
			set { _header = value; }
		}

		/// <summary>
		/// Gets the path of the entry in the container.
		/// </summary>
		public override string RelativePath {
			get { return _fileName; }
			internal set { _fileName = value.Replace("/", "\\"); }
		}

		/// <summary>
		/// Gets the compressed aligned size of the entry's content.<para></para>
		/// The aligned size is divisible by 8.
		/// </summary>
		public int SizeCompressedAlignment { get; internal set; }

		/// <summary>
		/// Gets the file exact offset in the stream.
		/// </summary>
		public uint FileExactOffset { get; internal set; }

		/// <summary>
		/// Value used to retrieve decompress data from GetDecompressData<para></para>
		/// by ignoring the lock on the 'container is saving' check.
		/// </summary>
		internal bool BypassSaveCheck { get; set; }

		/// <summary>
		/// Gets the new size of the compressed entry.<para></para>
		/// Used by the GrfWriter object only.
		/// </summary>
		public int NewSizeCompressed { get; internal set; }

		/// <summary>
		/// Gets the new size of the decompressed entry.<para></para>
		/// Used by the GrfWriter object only.
		/// </summary>
		public int NewSizeDecompressed { get; internal set; }

		/// <summary>
		/// Gets the new compressed entry data.<para></para>
		/// Used by the GrfWriter object only.
		/// </summary>
		internal byte[] NewCompressedData {
			get {
				if (SourceFilePath == GrfStrings.DataStreamId) {
					var data = RawDataSource.Data;
					NewSizeDecompressed = data.Length;
					byte[] dataCompressed = Compression.Compress(data);
					NewSizeCompressed = dataCompressed.Length;
					TemporarySizeCompressedAlignment = Methods.Align(NewSizeCompressed);
					return dataCompressed;
				}

				using (FileStream stream = File.OpenRead(SourceFilePath)) {
					NewSizeDecompressed = (int) stream.Length;
					byte[] dataCompressed = Compression.Compress(stream);
					NewSizeCompressed = dataCompressed.Length;
					TemporarySizeCompressedAlignment = Methods.Align(NewSizeCompressed);
					return dataCompressed;
				}
			}
		}

		public string ExtractionFilePath { get; set; }

		public object DataImage { get; set; }

		public int Cycle { get; internal set; }

		#region Display properties

		public string DisplayRelativePath {
			get {
				return Path.GetFileName(RelativePath);
			}
		}

		public string FileType {
			get {
				var extension = RelativePath.GetExtension();
				if (extension != null) return extension.Remove(0, 1).ToUpper();
				return GrfStrings.DisplayNoFileType;
			}
		}

		public string DisplaySize {
			get {
				if ((Flags & EntryType.RemoveFile) == EntryType.RemoveFile) {
					return "DELETE";
				}
				if ((Modification & Modification.Added) == Modification.Added) {
					if (SourceFilePath == GrfStrings.DataStreamId)
						return Methods.FileSizeToString(RawDataSource.Data.Length);

					return Methods.FileSizeToString(new FileInfo(SourceFilePath).Length);
				}
				return Methods.FileSizeToString(NewSizeDecompressed);
			}
		}

		public bool Added {
			get { return IsAdded; }
		}

		public bool Encrypted {
			get {
				return (Modification & Modification.Encrypt) == Modification.Encrypt ||
				       ((Flags & EntryType.GrfEditorCrypted) == EntryType.GrfEditorCrypted &&
				        (((Modification & Modification.Encrypt) != Modification.Encrypt &&
				          (Modification & Modification.Decrypt) != Modification.Decrypt) ||
				         (Modification & Modification.Encrypt) == Modification.Encrypt));
			}
		}

		public bool Lzma {
			get { return (Flags & EntryType.LzmaCompressed) == EntryType.LzmaCompressed; }
		}

		public bool Removed {
			get { return (Flags & EntryType.RemoveFile) == EntryType.RemoveFile; }
		}

		public bool Modified {
			get { return Modification.HasFlags(Modification.FileNameRenamed); }
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		internal void SetStream(ByteReaderStream stream) {
			Stream = stream;
		}

		/// <summary>
		/// Creates a copy of this instance.
		/// </summary>
		internal override ContainerEntry Copy() {
			return new FileEntry(this);
		}

		protected override byte[] _getDecompressedData() {
			if (NewSizeDecompressed == 0)
				return new byte[] {};

			byte[] data;

			// Fix : 2015-06-27
			// The container must NOT be saving while accessing decompressed data
			if (Header.Container != null && !BypassSaveCheck)
				GrfExceptions.IfSavingThrow(Header.Container);

			if (BypassSaveCheck)
				BypassSaveCheck = false;

			lock (Stream.SharedLock) {
				Stream.PositionUInt = FileExactOffset;
				data = Stream.Bytes(SizeCompressedAlignment);
			}

			if ((Header.IsEncrypted && Flags.HasFlags(EntryType.GrfEditorCrypted)) || Modification.HasFlags(Modification.Decrypt)) {
				if (Header.EncryptionKey != null) {
					Encryption.Decrypt(Header.EncryptionKey, data, SizeDecompressed);
				}
				else {
					throw GrfExceptions.__NoKeyFileSet.Create();
				}
			}
			else if (Header.EncryptionCheckFlag) {
				// Fix : 2015-04-04
				// This header property means that the flags haven't been all set yet.
				// (The flags are set asynchronously to avoid UI lags)
				if (Ee322.a184e9055afb92382b66a5d5b739e726(data) && (data.Length > 2 && data[0] != 0)) {
					if (Header.EncryptionKey != null) {
						Encryption.Decrypt(Header.EncryptionKey, data, SizeDecompressed);
					}
					else {
						throw GrfExceptions.__NoKeyFileSet.Create();
					}
				}
			}

			if (Cycle >= 0) {
				DesDecryption.DecryptFileData(data, Cycle == 0, Cycle);
			}

			if ((Flags & EntryType.LZSS) == EntryType.LZSS) {
				return Compression.LzssDecompress(data, SizeDecompressed);
			}

			if ((Flags & EntryType.RawDataFile) == EntryType.RawDataFile) {
				return Compression.RawDecompress(data, SizeDecompressed);
			}

			// Fix : 2015-04-04
			// Compression detection.
			if (Compression.IsNormalCompression || Compression.IsLzma) {
				if (data.Length > 1 && data[0] == 0) {
					Flags |= EntryType.LzmaCompressed;
					OnPropertyChanged("Lzma");

					return Compression.DecompressLzma(data, SizeDecompressed);
				}
			}

			if (data[0] != 0x78) {
				throw GrfExceptions.__CorruptedOrEncryptedEntry.Create(RelativePath);
			}

			return Compression.Decompress(data, SizeDecompressed);
		}

		protected override byte[] _getCompressedData() {
			byte[] data;

			lock (Stream.SharedLock) {
				Stream.PositionLong = FileExactOffset;
				data = Stream.Bytes(SizeCompressedAlignment);
			}

			if (Cycle >= 0) {
				DesDecryption.DecryptFileData(data, Cycle == 0, Cycle);
			}

			return data;
		}

		/// <summary>
		/// Gets the data from the stream.
		/// </summary>
		/// <returns>The raw data from the stream</returns>
		internal byte[] GetStreamData() {
			byte[] data;

			lock (Stream.SharedLock) {
				Stream.PositionLong = FileExactOffset;
				data = Stream.Bytes(SizeCompressedAlignment);
			}

			return data;
		}

		protected override byte[] _compress(byte[] data) {
			return Compression.Compress(data);
		}

		protected override byte[] _decompress(byte[] data) {
			return Compression.Decompress(data, SizeDecompressed);
		}

		internal int GetSizeDecompressed() {
			if (SourceFilePath == null)
				return SizeDecompressed;

			if (SourceFilePath == GrfStrings.DataStreamId)
				return RawDataSource.Length;

			return (int) GrfPath.GetFileSize(SourceFilePath);
		}

		public bool IsEmpty() {
			if (Modification.HasFlags(Modification.Added)) {
				if (SourceFilePath == GrfStrings.DataStreamId)
					return RawDataSource.Data.Length == 0;

				return new FileInfo(SourceFilePath).Length == 0;
			}
			if (Flags.HasFlags(EntryType.RemoveFile)) {
				return true;
			}
			return NewSizeDecompressed == 0;
		}

		internal void WriteMetadata(GrfHeader header, Stream fileEntryBuffer, bool overwriteFlags = true) {
			if (overwriteFlags && (Flags & EntryType.RemoveFile) == EntryType.RemoveFile) {
				// The entry added is removed, only the Thor maker sets the overwriteFlags to false
				return;
			}

			// Fix : 2015-04-06
			// Negative offsets are no longer possible
			if (TemporaryOffset < GrfHeader.StructSize) {
				TemporaryOffset = FileExactOffset;

				if (TemporaryOffset < GrfHeader.StructSize) {
					throw GrfExceptions.__EntryDataInvalid.Create(RelativePath);
				}
			}

			if (header.IsMajorVersion(1)) {
				if (!Modification.HasFlags(Modification.Removed)) {
					string realString = EncodingService.GetAnsiString(GetFixedFileName()) + "\0";
					while (realString.Length % 8 != 0) {
						realString += "\0";
					}
					realString = DesDecryption.EncodeFileName(EncodingService.Ansi.GetBytes(realString));
					byte[] fileName = EncodingService.Ansi.GetBytes(realString);
					byte[] data = new byte[fileName.Length + 27];

					Buffer.BlockCopy(BitConverter.GetBytes(realString.Length + 6), 0, data, 0, 4);
					Buffer.BlockCopy(fileName, 0, data, 6, fileName.Length);
					Buffer.BlockCopy(BitConverter.GetBytes(NewSizeCompressed + NewSizeDecompressed + 715), 0, data, fileName.Length + 10, 4);
					Buffer.BlockCopy(BitConverter.GetBytes(TemporarySizeCompressedAlignment + 37579), 0, data, fileName.Length + 14, 4);
					Buffer.BlockCopy(BitConverter.GetBytes(NewSizeDecompressed), 0, data, fileName.Length + 18, 4);

					if (!overwriteFlags && (Flags & EntryType.RemoveFile) == EntryType.RemoveFile) {
						data[fileName.Length + 22] = (byte) (EntryType.File | EntryType.RemoveFile);
					}
					else {
						if ((Flags & EntryType.RawDataFile) == EntryType.RawDataFile) {
							data[fileName.Length + 22] = 0;
						}
						else {
							data[fileName.Length + 22] = (byte) EntryType.File;
						}
					}

					Buffer.BlockCopy(BitConverter.GetBytes(TemporaryOffset - GrfHeader.StructSize), 0, data, fileName.Length + 23, 4);

					fileEntryBuffer.Write(data, 0, data.Length);
				}
			}
			else {
				if (!Modification.HasFlags(Modification.Removed)) {
					byte[] fileName = EncodingService.Ansi.GetBytes(EncodingService.GetAnsiString(GetFixedFileName()));
					byte[] data = new byte[18 + fileName.Length];

					Buffer.BlockCopy(fileName, 0, data, 0, fileName.Length);
					Buffer.BlockCopy(BitConverter.GetBytes(NewSizeCompressed), 0, data, fileName.Length + 1, 4);
					Buffer.BlockCopy(BitConverter.GetBytes(TemporarySizeCompressedAlignment), 0, data, fileName.Length + 5, 4);
					Buffer.BlockCopy(BitConverter.GetBytes(NewSizeDecompressed), 0, data, fileName.Length + 9, 4);

					EntryType baseFlag = ((Flags & EntryType.RawDataFile) == EntryType.RawDataFile) ? EntryType.Directory : EntryType.File;

					if (!overwriteFlags && (Flags & EntryType.RemoveFile) == EntryType.RemoveFile) {
						data[fileName.Length + 13] = (byte)(baseFlag | EntryType.RemoveFile);
					}
					else {
						data[fileName.Length + 13] = (byte)baseFlag;
					}

					Buffer.BlockCopy(BitConverter.GetBytes(TemporaryOffset - GrfHeader.StructSize), 0, data, fileName.Length + 14, 4);

					fileEntryBuffer.Write(data, 0, data.Length);
				}
			}
		}

		internal string GetFixedFileName() {
			if (RelativePath.StartsWith(GrfStrings.RgzRoot)) {
				return RelativePath.Remove(0, GrfStrings.RgzRoot.Length);
			}
			return RelativePath;
		}

		public override string ToString() {
			return RelativePath;
		}

		//internal void SetModificationFlags(Modification flags) {
		//	Modification = flags;
		//}

		internal int GenerateCycle() {
			string ext = _fileName.GetExtension();
			int cycle = 0;

			if (ext != null && ext != ".gnd" && ext != ".gat" && ext != ".act" && ext != ".str") {
				cycle = 1;
				for (int k = 10; NewSizeCompressed >= k; k *= 10)
					cycle++;
			}

			return cycle;
		}

		internal void DesDecrypt(byte[] dataStream, int dataOffset) {
			if (Cycle >= 0) {
				DesDecryption.DecryptFileData(dataStream, Cycle == 0, Cycle, dataOffset, SizeCompressedAlignment);
			}
		}

		internal void DesDecryptPrealigned(byte[] dataStream, int dataOffset) {
			if (Cycle >= 0) {
				DesDecryption.DecryptFileData(dataStream, Cycle == 0, Cycle, dataOffset, TemporarySizeCompressedAlignment);
			}
		}

		internal void GrfEditorDecryptRequested(byte[] dataStream) {
			if ((Header.IsEncrypted && (Flags & EntryType.GrfEditorCrypted) == EntryType.GrfEditorCrypted)) {
				if (Header.EncryptionKey != null) {
					byte[] dataTmp = new byte[SizeCompressedAlignment];
					Buffer.BlockCopy(dataStream, (int) TemporaryOffset, dataTmp, 0, dataTmp.Length);
					Encryption.Decrypt(Header.EncryptionKey, dataTmp, SizeDecompressed);
					Buffer.BlockCopy(dataTmp, 0, dataStream, (int) TemporaryOffset, dataTmp.Length);
				}
			}
		}

		internal void Align(byte[] dataStream, int dataOffset, out byte[] output) {
			output = new byte[SizeCompressedAlignment];
			Buffer.BlockCopy(dataStream, dataOffset, output, 0, SizeCompressed);
		}

		internal void Align(byte[] data, out byte[] output) {
			output = new byte[Methods.Align(data.Length)];
			Buffer.BlockCopy(data, 0, output, 0, data.Length);
		}

		internal void DesEncrypt(byte[] dataTmp, bool headerCheck = true) {
			if ((headerCheck && Header.IsMajorVersion(1)) || !headerCheck) {
				if (Cycle >= 0) // Already encrypted
					return;

				Cycle = GenerateCycle();
				DesDecryption.EncryptFileData(dataTmp, Cycle == 0, Cycle);
			}
		}

		internal bool HasToDesEncrypt(bool headerCheck = true) {
			return ((headerCheck && Header.IsMajorVersion(1)) || !headerCheck) && Cycle < 0;
		}

		internal void DesEncrypt(byte[] dataStream, int offset, bool headerCheck = true) {
			if ((headerCheck && Header.IsMajorVersion(1)) || !headerCheck) {
				// Fix : 2015-04-07
				// The DES enryption wasn't being updated when renaming a file.
				var ignored = RelativePath.IsExtension(".gnd", ".gat", ".act", ".str");

				if ((ignored && Cycle > 0) || (!ignored && Cycle == 0)) {
					DesDecryptPrealigned(dataStream, offset);
					Cycle = GenerateCycle();
					DesDecryption.EncryptFileData(dataStream, Cycle == 0, Cycle, offset, SizeCompressedAlignment);
					return;
				}

				if (Cycle >= 0) // Already encrypted
					return;

				Cycle = GenerateCycle();
				DesDecryption.EncryptFileData(dataStream, Cycle == 0, Cycle, offset, SizeCompressedAlignment);
			}
		}

		internal void DesEncryptPrealigned(byte[] dataStream, int offset, bool headerCheck = true) {
			if ((headerCheck && Header.IsMajorVersion(1)) || !headerCheck) {
				// Fix : 2015-04-07
				// The DES enryption wasn't being updated when renaming a file.
				var ignored = RelativePath.IsExtension(".gnd", ".gat", ".act", ".str");

				if ((ignored && Cycle > 0) || (!ignored && Cycle == 0)) {
					DesDecryptPrealigned(dataStream, offset);
					Cycle = GenerateCycle();
					DesDecryption.EncryptFileData(dataStream, Cycle == 0, Cycle, offset, TemporarySizeCompressedAlignment);
					return;
				}

				if (Cycle >= 0) // Already encrypted
					return;

				Cycle = GenerateCycle();
				DesDecryption.EncryptFileData(dataStream, Cycle == 0, Cycle, offset, TemporarySizeCompressedAlignment);
			}
		}

		internal void GrfEditorEncrypt(byte[] dataTmp) {
			if ((Modification & Modification.Encrypt) == Modification.Encrypt) {
				if ((Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
					return;

				// Fix : 2015-04-04
				// Added lzma compression support
				if (Ee322.ad0bbddf4b9c6de7b6f99a036deb2be2(dataTmp) || (dataTmp.Length > 2 && dataTmp[0] == 0x0)) {
					Encryption.Encrypt(Header.EncryptionKey, dataTmp, NewSizeDecompressed);

					if (Ee322.ad0bbddf4b9c6de7b6f99a036deb2be2(dataTmp) || dataTmp[0] == 0x0)
						Encryption.Decrypt(Header.EncryptionKey, dataTmp, NewSizeDecompressed);
				}
			}
		}

		internal bool GrfEditorEncrypt(byte[] dataStream, int offset) {
			if (Header.IsEncrypting || (Modification & Modification.Encrypt) == Modification.Encrypt) {
				if ((Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
					return false;

				byte[] dataTmp = new byte[SizeCompressedAlignment];
				Buffer.BlockCopy(dataStream, offset, dataTmp, 0, offset + dataTmp.Length > dataStream.Length ? dataStream.Length - offset : dataTmp.Length);

				// Fix : 2015-04-04
				// Added lzma compression support
				if (Ee322.ad0bbddf4b9c6de7b6f99a036deb2be2(dataTmp) || (dataTmp.Length > 2 && dataTmp[0] == 0x0)) {
					Encryption.Encrypt(Header.EncryptionKey, dataTmp, NewSizeDecompressed);

					if (Ee322.ad0bbddf4b9c6de7b6f99a036deb2be2(dataTmp) || dataTmp[0] == 0x0) {
						Encryption.Decrypt(Header.EncryptionKey, dataTmp, NewSizeDecompressed);
						return false;
					}
					else {
						Buffer.BlockCopy(dataTmp, 0, dataStream, offset, offset + dataTmp.Length > dataStream.Length ? dataStream.Length - offset : dataTmp.Length);
						return true;
					}
				}
			}

			return false;
		}

		internal void GrfEditorDecrypt(byte[] dataStream, int offset) {
			if (Header.IsDecrypting || (Modification & Modification.Decrypt) == Modification.Decrypt) {
				byte[] dataTmp = new byte[SizeCompressedAlignment];
				Buffer.BlockCopy(dataStream, offset, dataTmp, 0, offset + dataTmp.Length > dataStream.Length ? dataStream.Length - offset : dataTmp.Length);

				// Fix : 2015-04-04
				// Added lzma compression support
				if (Ee322.a184e9055afb92382b66a5d5b739e726(dataTmp) && (dataTmp.Length > 2 && dataTmp[0] != 0)) {
					Encryption.Decrypt(Header.EncryptionKey, dataTmp, SizeDecompressed);

					if (Ee322.a184e9055afb92382b66a5d5b739e726(dataTmp) && dataTmp[0] != 0)
						Encryption.Encrypt(Header.EncryptionKey, dataTmp, SizeDecompressed);
					else
						Buffer.BlockCopy(dataTmp, 0, dataStream, offset, offset + dataTmp.Length > dataStream.Length ? dataStream.Length - offset : dataTmp.Length);
				}
			}
		}

		/// <summary>
		/// Determines wheter or not this file's can be read with the current encryption.
		/// </summary>
		/// <returns>True if the file can be read with the encryption.</returns>
		public bool EncryptionSafe() {
			if (IsAdded)
				return true;

			if ((Flags & EntryType.RemoveFile) == EntryType.RemoveFile)
				return true;

			if ((Header.IsEncrypted && Flags.HasFlags(EntryType.GrfEditorCrypted)) || Modification.HasFlags(Modification.Decrypt)) {
				return Header.EncryptionKey != null;
			}

			return true;
		}

		internal override void Delete() {
			RawDataSource = null;
			base.Delete();
		}

		/// <summary>
		/// Changes the source stream of the entry.
		/// </summary>
		/// <param name="reader">The reader.</param>
		internal void RefreshStream(ByteReaderStream reader) {
			if (Stream == null || Stream.FileName == reader.FileName)
				Stream = reader;
		}

		/// <summary>
		/// Gets the hash from the compressed data.
		/// </summary>
		/// <param name="hash">The hash generator.</param>
		/// <returns>The hash.</returns>
		internal string GetDataHashFromCompressed(IHash hash) {
			return hash.ComputeHash(GetCompressedData());
		}

		/// <summary>
		/// Gets the hash from the decompressed data.
		/// </summary>
		/// <param name="hash">The hash generator.</param>
		/// <returns>The hash.</returns>
		internal string GetDataHashFromDecompressed(IHash hash) {
			return hash.ComputeHash(GetDecompressedData());
		}

		/// <summary>
		/// Called when [property changed].
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		public void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public static FileEntry CreateBufferedEntry(string name, string relativePath, uint offset, int compressedLength, int alignment, int uncompressedLength) {
			FileEntry entry = new FileEntry();
			entry.RelativePath = relativePath;
			entry.SourceFilePath = name;
			entry.TemporaryOffset = offset;
			entry.NewSizeCompressed = compressedLength;
			entry.NewSizeDecompressed = uncompressedLength;
			entry.TemporarySizeCompressedAlignment = alignment;
			return entry;
		}
	}
}