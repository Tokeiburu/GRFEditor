using System;
using System.IO;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities.Services;

namespace GRF.FileFormats.TkPatchFormat {
	public class TkPatchHeader : FileHeader {
		public const long StructSize = 10;

		public TkPatchHeader() {
			Magic = "TKPO";
			MajorVersion = 1;
			MinorVersion = 0;
		}

		public TkPatchHeader(ByteReaderStream reader) {
			Magic = reader.StringANSI(4);

			if (Magic != "TKPO")
				throw GrfExceptions.__FileFormatException.Create("TKPO");

			MajorVersion = reader.Byte();
			MinorVersion = reader.Byte();
			FileTableOffset = reader.Int32();
			DataOffset = reader.Position;
		}

		public byte Mode { get; set; }
		public int FileTableOffset { get; set; }
		public int DataOffset { get; set; }

		public void Write(FileStream stream) {
			stream.Write(EncodingService.Ansi.GetBytes(Magic), 0, 4);
			stream.WriteByte(MajorVersion);
			stream.WriteByte(MinorVersion);
			stream.Write(BitConverter.GetBytes(FileTableOffset), 0, 4);
		}
	}
}