using System;
using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	public struct TextureKeyFrame : IWriteableObject {
		public int Frame;
		public float Offset;

		public TextureKeyFrame(TextureKeyFrame tkf) {
			Frame = tkf.Frame;
			Offset = tkf.Offset;
		}

		public TextureKeyFrame(IBinaryReader reader) {
			Frame = reader.Int32();
			Offset = reader.Float();
		}

		public TextureKeyFrame(byte[] data, int offset) {
			Frame = BitConverter.ToInt32(data, offset);
			Offset = BitConverter.ToSingle(data, offset + 4);
		}

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Frame);
			writer.Write(Offset);
		}
	}
}