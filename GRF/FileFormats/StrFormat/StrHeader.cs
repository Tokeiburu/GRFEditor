using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;

namespace GRF.FileFormats.StrFormat {
	/// <summary>
	/// Header of a STR file.
	/// </summary>
	public class StrHeader : FileHeader, IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="StrHeader" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="str">The STR.</param>
		public StrHeader(IBinaryReader reader, Str str) {
			Magic = reader.String(4);

			if (Magic != "STRM")
				GrfExceptions.__FileFormatException.Create("STR");

			MajorVersion = reader.Byte();
			MinorVersion = reader.Byte();

			reader.Forward(2);

			str.Fps = reader.Int32();
			str.MaxKeyFrame = reader.Int32();
			NumberOfLayers = reader.Int32();

			reader.Forward(16);
		}

		internal StrHeader() {
			Magic = "STRM";
		}

		/// <summary>
		/// Gets or sets the number of layers.
		/// </summary>
		internal int NumberOfLayers { get; set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Encoding.ASCII.GetBytes(Magic));
			writer.Write(MajorVersion);
			writer.Write(MinorVersion);
			writer.Write(new byte[2]);
		}

		#endregion
	}
}