using System.IO;
using GRF.IO;

namespace GRF.FileFormats.RswFormat {
	public class RswGround {
		/// <summary>
		/// Initializes a new instance of the <see cref="RswGround" /> class.
		/// </summary>
		public RswGround() {
			Top = 0;
			Bottom = 0;
			Left = 0;
			Right = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RswGround" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="header">The header.</param>
		public RswGround(IBinaryReader reader, RswHeader header) {
			if (header.IsCompatibleWith(1, 6)) {
				// This data couldn't be more wrong; it's probably not used anymore and
				// it's been filled with remains of memory, so reading it is kind of pointless
				Top = reader.Int32();
				Bottom = reader.Int32();
				Left = reader.Int32();
				Right = reader.Int32();
			}
			else {
				Top = -500;
				Bottom = 500;
				Left = -500;
				Right = 500;
			}
		}

		/// <summary>
		/// Gets or sets the top position.
		/// </summary>
		public int Top { get; set; }

		/// <summary>
		/// Gets or sets the bottom position.
		/// </summary>
		public int Bottom { get; set; }

		/// <summary>
		/// Gets or sets the left position.
		/// </summary>
		public int Left { get; set; }

		/// <summary>
		/// Gets or sets the right position.
		/// </summary>
		public int Right { get; set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="header">The header.</param>
		public void Write(BinaryWriter writer, RswHeader header) {
			if (header.IsCompatibleWith(1, 6)) {
				writer.Write(Top);
				writer.Write(Bottom);
				writer.Write(Left);
				writer.Write(Right);
			}
		}
	}
}