using System.IO;
using GRF.ContainerFormat;
using GRF.IO;

namespace GRF.FileFormats.GatFormat {
	public class GatHeader : FileHeader, IWriteableObject {
		public GatHeader(int width, int height) {
			Width = width;
			Height = height;
			MajorVersion = 1;
			MinorVersion = 2;
		}

		public GatHeader(IBinaryReader data) {
			Magic = data.StringANSI(4);

			if (Magic != "GRAT")
				throw GrfExceptions.__FileFormatException.Create("GAT");

			MajorVersion = data.Byte();
			MinorVersion = data.Byte();
			Width = data.Int32();
			Height = data.Int32();
		}

		/// <summary>
		/// Gets the width.
		/// </summary>
		public int Width { get; internal set; }

		/// <summary>
		/// Gets the height.
		/// </summary>
		public int Height { get; internal set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			byte[] magic = new byte[] { 0x47, 0x52, 0x41, 0x54 };
			writer.Write(magic);
			writer.Write(MajorVersion);
			writer.Write(MinorVersion);
			writer.Write(Width);
			writer.Write(Height);
		}
	}
}