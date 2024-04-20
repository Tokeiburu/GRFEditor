using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	public struct ScaleKeyFrame : IWriteableObject {
		public int Frame;
		public TkVector3 Scale;
		public float Data;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScaleKeyFrame"/> struct.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public ScaleKeyFrame(IBinaryReader reader) {
			Frame = reader.Int32();
			Scale = reader.Vector3();
			Data = reader.Float();
		}

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Frame);
			Scale.Write(writer);
			writer.Write(Data);
		}
	}
}