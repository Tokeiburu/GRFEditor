using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.RswFormat {
	public class RswHeader : FileHeader, IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="RswHeader" /> class.
		/// </summary>
		/// <param name="filename">The filename.</param>
		public RswHeader(string filename) {
			byte[] magic = new byte[] { 0x47, 0x52, 0x53, 0x57 };
			Magic = Encoding.Default.GetString(magic);

			MajorVersion = 2;
			MinorVersion = 1;
			AltitudeFile = filename + ".gat";
			GroundFile = filename + ".gnd";
			SourceFile = "";
			IniFile = "";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RswHeader" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public RswHeader(IBinaryReader reader) {
			Magic = reader.StringANSI(4);

			if (Magic != "GRSW")
				GrfExceptions.__FileFormatException.Create("RSW");

			MajorVersion = reader.Byte();
			MinorVersion = reader.Byte();

			UnknownData = 1;

			if (IsCompatibleWith(2, 5)) {
				BuildNumber = reader.Int32();
			}

			if (IsCompatibleWith(2, 2)) {
				UnknownData = reader.Byte();
			}

			IniFile = reader.String(40, '\0');
			GroundFile = reader.String(40, '\0');

			if (IsCompatibleWith(1, 4)) {
				AltitudeFile = reader.String(40, '\0');
			}
			else {
				AltitudeFile = "";
			}

			SourceFile = reader.String(40, '\0');
		}

		/// <summary>
		/// Gets the ini file.
		/// </summary>
		public string IniFile { get; private set; }

		/// <summary>
		/// Gets or sets the ground file.
		/// </summary>
		public string GroundFile { get; set; }

		/// <summary>
		/// Gets or sets the altitude file.
		/// </summary>
		public string AltitudeFile { get; set; }

		/// <summary>
		/// Gets the source file.
		/// </summary>
		public string SourceFile { get; private set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Encoding.ASCII.GetBytes(Magic));
			writer.Write(MajorVersion);
			writer.Write(MinorVersion);

			if (IsCompatibleWith(2, 5)) {
				writer.Write(BuildNumber);
			}
			
			if (IsCompatibleWith(2, 2)) {
				writer.Write(UnknownData);
			}

			writer.WriteANSI(IniFile, 40);
			writer.WriteANSI(GroundFile, 40);

			if (IsCompatibleWith(1, 4)) {
				writer.WriteANSI(AltitudeFile, 40);
			}

			writer.WriteANSI(SourceFile, 40);
		}
	}
}