using System.IO;
using GRF.ContainerFormat;
using GRF.IO;

namespace GRF.FileFormats.SprFormat {
	public class SprHeader : FileHeader {
		public SprHeader() {
			Magic = "SP";
			MinorVersion = 1;
			MajorVersion = 2;
		}

		public SprHeader(IBinaryReader reader) {
			Magic = reader.StringANSI(2);

			if (Magic != "SP")
				GrfExceptions.__FileFormatException.Create("SPR");

			MinorVersion = reader.Byte();
			MajorVersion = reader.Byte();
		}

		public SprHeader(SprHeader sprHeader) {
			Magic = "SP";
			MinorVersion = sprHeader.MinorVersion;
			MajorVersion = sprHeader.MajorVersion;
		}

		public void Write(BinaryWriter writer) {
			writer.Write(new byte[] { 0x53, 0x50 });
		}
	}
}