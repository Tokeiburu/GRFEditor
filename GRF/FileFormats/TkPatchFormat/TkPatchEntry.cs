using System;
using System.IO;
using GRF.Core;
using GRF.IO;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.FileFormats.TkPatchFormat {
	public class TkPatchEntry {
		private readonly ByteReaderStream _stream;

		internal TkPatchEntry() {
			Flags = EntryType.File;
		}

		public TkPatchEntry(string file) : this() {
			SourceFilePath = file;
			SizeDecompressed = (int) new FileInfo(file).Length;
		}

		public TkPatchEntry(byte[] compressedData, int sizeDecompressed) : this() {
			SourceFilePath = null;
			CompressedData = compressedData;
			SizeDecompressed = sizeDecompressed;
		}

		public TkPatchEntry(IBinaryReader reader, ByteReaderStream stream) : this() {
			_stream = stream;

			TkPath = new TkPath(reader.String(reader.Byte()));
			byte flags = reader.Byte();

			if ((flags & 0x01) == 0x01) {
				Flags |= EntryType.RemoveFile;
				Offset = 0;
				SizeCompressed = -1;
				SizeDecompressed = -1;
				return;
			}

			Offset = reader.UInt32();
			SizeCompressed = reader.Int32();
			SizeDecompressed = reader.Int32();
		}

		public TkPath TkPath { get; set; }
		public long Offset { get; set; }
		public int SizeCompressed { get; set; }
		public int SizeDecompressed { get; set; }
		public EntryType Flags { get; set; }
		public string SourceFilePath { get; set; }
		public byte[] CompressedData { get; set; }

		public byte[] GetDecompressedData() {
			_stream.PositionLong = Offset;
			return Compression.Decompress(_stream.Bytes(SizeCompressed), SizeDecompressed);
		}

		public byte[] GetCompressedData() {
			if (Flags.HasFlags(EntryType.RemoveFile)) {
				return null;
			}

			if (CompressedData != null) {
				return CompressedData;
			}

			if (SourceFilePath != null) {
				return Compression.CompressDotNet(File.ReadAllBytes(SourceFilePath));
			}

			_stream.PositionLong = Offset;
			return _stream.Bytes(SizeCompressed);
		}

		public void Extract(string path) {
			string fullPath = GrfPath.Combine(path, TkPath.RelativePath ?? TkPath.FilePath);
			GrfPath.CreateDirectoryFromFile(fullPath);

			if (!Directory.Exists(fullPath))
				File.WriteAllBytes(fullPath, GetDecompressedData());
		}

		public override string ToString() {
			return "Name = " + TkPath + "; DecompressedLength = " + SizeDecompressed;
		}

		public FileEntry ToFileEntry(GrfHeader header) {
			FileEntry entry = new FileEntry();

			entry.RelativePath = TkPath.RelativePath;
			entry.SizeCompressed = SizeCompressed;
			entry.SizeCompressedAlignment = Methods.Align(SizeCompressed);
			entry.SizeDecompressed = SizeDecompressed;
			entry.Flags = Flags;
			entry.TemporaryOffset = Offset;
			entry.FileExactOffset = Offset;
			entry.TemporarySizeCompressedAlignment = Methods.Align(SizeCompressed);
			entry.NewSizeCompressed = SizeCompressed;
			entry.NewSizeDecompressed = SizeDecompressed;
			entry.ExtractionFilePath = null;
			entry.SourceFilePath = null;
			entry.Cycle = -1;
			entry.Header = header;

			return entry;
		}

		public void Write(MemoryStream mem) {
			string ansiName = EncodingService.ConvertStringToAnsi(TkPath.GetFullPath());
			byte[] name = EncodingService.Ansi.GetBytes(ansiName);
			mem.WriteByte((byte) name.Length);
			mem.Write(name, 0, name.Length);

			if (Flags.HasFlags(EntryType.RemoveFile)) {
				mem.WriteByte(0x01);
			}
			else {
				mem.WriteByte(0x00);
				mem.Write(BitConverter.GetBytes(Offset), 0, 4);
				mem.Write(BitConverter.GetBytes(SizeCompressed), 0, 4);
				mem.Write(BitConverter.GetBytes(SizeDecompressed), 0, 4);
			}
		}
	}
}