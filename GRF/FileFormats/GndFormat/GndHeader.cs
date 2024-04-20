using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;

namespace GRF.FileFormats.GndFormat {
	/// <summary>
	/// The GND header
	/// </summary>
	public class GndHeader : FileHeader, IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="GndHeader" /> class.
		/// </summary>
		/// <param name="sizeX">The size X.</param>
		/// <param name="sizeY">The size Y.</param>
		public GndHeader(int sizeX, int sizeY) {
			byte[] magic = new byte[] { 0x47, 0x52, 0x47, 0x4e };
			Magic = Encoding.Default.GetString(magic);
			MajorVersion = 1;
			MinorVersion = 7;
			Width = sizeX;
			Height = sizeY;
			ProportionRatio = 10.0f;
			TextureCount = 0;
			TexturePathSize = 80;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GndHeader" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public GndHeader(IBinaryReader reader) {
			GrfExceptions.ValidateHeaderLength(reader, "GND", 26);

			Magic = reader.StringANSI(4);

			if (Magic != "GRGN") {
				// Try to load alpha GND
				try {
					reader.Position = 0;
					TextureCount = reader.Int32();
					Width = reader.Int32();
					Height = reader.Int32();
					MajorVersion = 1;
					MinorVersion = 0;
					TexturePathSize = 80;

					if (Width > 5000 || Height > 5000)
						throw GrfExceptions.__FileFormatException.Create("GND");

					return;
				}
				catch {
					throw GrfExceptions.__FileFormatException.Create("GND");
				}
			}

			MajorVersion = reader.Byte();
			MinorVersion = reader.Byte();
			Width = reader.Int32();
			Height = reader.Int32();
			ProportionRatio = reader.Float();
			TextureCount = reader.Int32();
			TexturePathSize = reader.Int32();
		}

		/// <summary>
		/// Gets the width of the map.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Gets the height of the map.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets the proportion ratio of the map grid.
		/// </summary>
		public float ProportionRatio { get; private set; }

		/// <summary>
		/// Gets the texture count.
		/// </summary>
		public int TextureCount { get; internal set; }

		/// <summary>
		/// Gets the size of the texture paths (default to 80).
		/// </summary>
		public int TexturePathSize { get; private set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Encoding.ASCII.GetBytes("GRGN"));
			writer.Write(MajorVersion);
			writer.Write(MinorVersion);
			writer.Write(Width);
			writer.Write(Height);
			writer.Write(ProportionRatio);
			writer.Write(TextureCount);
			writer.Write(TexturePathSize);
		}

		#endregion
	}
}