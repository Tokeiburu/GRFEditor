using System.IO;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.RsmFormat {
	public class RsmHeader : FileHeader, IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="RsmHeader" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public RsmHeader(IBinaryReader data) {
			Magic = data.StringANSI(4);

			if (Magic != "GRSM")
				GrfExceptions.__FileFormatException.Create("RSM");

			MajorVersion = data.Byte();
			MinorVersion = data.Byte();
		}

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.WriteANSI("GRSM", 4);
			writer.Write(MajorVersion);
			writer.Write(MinorVersion);
		}
	}
}