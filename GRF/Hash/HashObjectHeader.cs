using System.IO;
using GRF.ContainerFormat;
using GRF.Core.Exceptions;
using GRF.FileFormats;
using GRF.IO;
using Utilities.Services;

namespace GRF.Hash {
	public class HashObjectHeader : FileHeader {
		public HashObjectHeader(IBinaryReader data) {
			Magic = data.StringANSI(4);

			if (Magic != "TKHO") {
				throw GrfExceptions.__FileFormatException.Create("TKHO");
			}

			MajorVersion = data.Byte();
			MinorVersion = data.Byte();
		}

		public HashObjectHeader() {
			Magic = "TKHO";
			MajorVersion = 1;
			MinorVersion = 0;
		}

		public void Write(Stream stream) {
			byte[] header = EncodingService.ANSI.GetBytes(Magic);
			stream.Write(header, 0, header.Length);
			stream.WriteByte(MajorVersion);
			stream.WriteByte(MinorVersion);
		}
	}
}