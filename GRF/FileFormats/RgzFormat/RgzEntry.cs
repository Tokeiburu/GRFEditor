using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using Utilities.Services;

namespace GRF.FileFormats.RgzFormat {
	public sealed class RgzEntry : ContainerEntry {
		public RgzEntry() {
		}

		public RgzEntry(ByteReaderStream stream) {
			Stream = stream;

			RelativePath = stream.StringANSI(stream.Byte());
			RelativePath = EncodingService.FromAnsiToDisplayEncoding(RelativePath);
			RelativePath = RelativePath.Substring(0, RelativePath.IndexOf('\0'));

			int length = stream.Int32();
			Offset = stream.PositionUInt;
			SizeCompressed = -1;
			SizeDecompressed = length;
			stream.Forward(length);
		}

		internal override ContainerEntry Copy() {
			RgzEntry entry = new RgzEntry();

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
			return Stream.Bytes(SizeDecompressed);
		}

		protected override byte[] _getCompressedData() {
			return _compress(GetDecompressedData());
		}

		protected override byte[] _compress(byte[] data) {
			return Compression.Compress(data);
		}

		protected override byte[] _decompress(byte[] data) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}
	}
}