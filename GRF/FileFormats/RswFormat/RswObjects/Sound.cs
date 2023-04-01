using System.IO;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.RswFormat.RswObjects {
	/// <summary>
	/// Represents the sound object type found in a RSW map.
	/// </summary>
	public class Sound : RswObject {
		private readonly RswHeader _header;

		/// <summary>
		/// Initializes a new instance of the <see cref="Sound" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="header">The header.</param>
		public Sound(IBinaryReader reader, RswHeader header) : base(RswObjectType.Sound) {
			_header = header;

			Name = reader.String(80, '\0');
			WaveName = reader.String(80, '\0');
			Position = reader.Vertex();
			Volume = reader.Float();
			Width = reader.Int32();
			Height = reader.Int32();
			Range = reader.Float();
			Cycle = 4.0f;

			if (_header.IsCompatibleWith(2, 0)) {
				Cycle = reader.Float();
			}
			else if (_header.IsCompatibleWith(1, 9)) {
				// We cheat a little since float variables are encoded using the IEEE
				// convention and therefore they're not likely to range between 1-4
				if (reader.CanRead) {
					int possibleType = reader.Int32();
					reader.Forward(-4);

					if (possibleType < 1 || possibleType > 4) {
						Cycle = reader.Float();
					}
				}
			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the name of the wave.
		/// </summary>
		public string WaveName { get; private set; }

		/// <summary>
		/// Gets the volume.
		/// </summary>
		public float Volume { get; private set; }

		/// <summary>
		/// Gets the width.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Gets the height.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets the range.
		/// </summary>
		public float Range { get; private set; }

		/// <summary>
		/// Gets the cycle.
		/// </summary>
		public float Cycle { get; private set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			writer.WriteANSI(Name, 80);
			writer.WriteANSI(WaveName, 80);
			Position.Write(writer);
			writer.Write(Volume);
			writer.Write(Width);
			writer.Write(Height);
			writer.Write(Range);

			if (_header.IsCompatibleWith(2, 0))
				writer.Write(Cycle);
		}
	}
}