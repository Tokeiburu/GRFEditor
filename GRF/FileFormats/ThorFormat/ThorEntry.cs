using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.ThorFormat {
	public sealed class ThorEntry : ContainerEntry {
		/// <summary>
		/// Initializes a new instance of the <see cref="ThorEntry"/> class.
		/// </summary>
		public ThorEntry() {
			Flags = EntryType.File;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThorEntry"/> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="stream">The stream.</param>
		public ThorEntry(IBinaryReader reader, ByteReaderStream stream) : this(reader, stream, false) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThorEntry"/> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="singleFile">if set to <c>true</c> [single file].</param>
		public ThorEntry(IBinaryReader reader, ByteReaderStream stream, bool singleFile) : this() {
			Stream = stream;

			if (singleFile) {
				SizeCompressed = reader.Int32();
				SizeDecompressed = reader.Int32();
				RelativePath = reader.String(reader.Byte());
				Offset = reader.PositionUInt;
			}
			else {
				RelativePath = reader.String(reader.Byte());
				byte flags = reader.Byte();

				if ((flags & 0x01) == 0x01) {
					Flags |= EntryType.RemoveFile;
					Offset = 0;
					SizeCompressed = 0;
					SizeDecompressed = 0;
					return;
				}

				Offset = reader.UInt32();
				SizeCompressed = reader.Int32();
				SizeDecompressed = reader.Int32();
			}
		}

		public override string ToString() {
			return "Name = " + RelativePath + "; DecompressedLength = " + SizeDecompressed;
		}

		internal override ContainerEntry Copy() {
			ThorEntry entry = new ThorEntry();

			entry.Offset = Offset;
			entry.SizeCompressed = SizeCompressed;
			entry.SizeDecompressed = SizeDecompressed;
			entry.RemovedFlagCount = RemovedFlagCount;
			entry.Flags = Flags;
			entry.SourceFilePath = SourceFilePath;
			entry.RelativePath = RelativePath;
			entry.Modification = Modification;
			entry.Stream = Stream;

			return entry;
		}

		protected override byte[] _getDecompressedData() {
			Stream.PositionLong = Offset;

			byte[] data = Stream.Bytes(SizeCompressed);

			if (Compression.IsNormalCompression || Compression.IsLzma || Compression.IsCustom) {
				if (data.Length > 1 && data[0] == 0) {
					Flags |= EntryType.CustomCompressed;

					if (Compression.IsCustom)
						return Compression.Decompress(data, SizeDecompressed);

					return Compression.DecompressLzma(data, SizeDecompressed);
				}
			}

			if ((Flags & EntryType.RawDataFile) == EntryType.RawDataFile) {
				return Compression.RawDecompress(data, SizeDecompressed);
			}

			return Compression.Decompress(data, SizeDecompressed);
		}

		protected override byte[] _getCompressedData() {
			Stream.PositionLong = Offset;
			return Stream.Bytes(SizeCompressed);
		}

		protected override byte[] _compress(byte[] data) {
			return Compression.Compress(data);
		}

		protected override byte[] _decompress(byte[] data) {
			return Compression.Decompress(data, SizeDecompressed);
		}

		/// <summary>
		/// Extracts the content of this entry to the specified path.
		/// </summary>
		/// <param name="filepath">The filepath.</param>
		public override void ExtractFromAbsolute(string filepath) {
			if (Flags.HasFlags(EntryType.RemoveFile)) {
				GrfPath.Delete(filepath);
			}
			else {
				base.ExtractFromAbsolute(filepath);
			}
		}
	}
}