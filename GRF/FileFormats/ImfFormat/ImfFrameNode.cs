using System.IO;
using GRF.IO;

namespace GRF.FileFormats.ImfFormat {
	public class ImfFrameNode : IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="ImfFrameNode" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public ImfFrameNode(IBinaryReader reader) {
			Priority = reader.Int32();
			X = reader.Int32();
			Y = reader.Int32();
		}

		/// <summary>
		/// Gets or sets the priority.
		/// </summary>
		public int Priority { get; set; }

		/// <summary>
		/// Gets or sets the X.
		/// </summary>
		public int X { get; set; }

		/// <summary>
		/// Gets or sets the Y.
		/// </summary>
		public int Y { get; set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Priority);
			writer.Write(X);
			writer.Write(Y);
		}

		#endregion

		public override string ToString() {
			return "X = " + X + "; Y = " + Y;
		}
	}
}