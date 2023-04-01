using System.IO;
using GRF.ContainerFormat;
using GRF.IO;

namespace GRF.FileFormats.ActFormat {
	public class ActHeader : FileHeader {
		public ActHeader() {
			MajorVersion = 2;
			MinorVersion = 5;

			Magic = "AC";
		}

		public ActHeader(IBinaryReader data) {
			Magic = data.StringANSI(2);

			if (Magic != "AC")
				throw GrfExceptions.__FileFormatException.Create("ACT");

			MinorVersion = data.Byte();
			MajorVersion = data.Byte();
		}

		public void Write(BinaryWriter writer) {
			writer.Write(new byte[] { 0x41, 0x43 });
		}
	}
}