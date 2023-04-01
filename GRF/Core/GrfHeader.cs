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
		public bool IsEncrypting { get; set; }
		public bool IsDecrypting { get; set; }
		public bool IsEncrypted { get; set; }

		public bool FoundErrors {
			get { return _errors.Count > 0; }
		}

		public byte[] EncryptionKey { get; set; }

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

		public void SetFileTableEncryption(bool value) {
			EncryptFileTable = value;
		}
	}
}