using System;
using System.IO;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	[Serializable]
	public class TextureKeyFrame : KeyFrame {
		public float Offset;
		public TextureTransformTypes Type = TextureTransformTypes.None;

		public TextureKeyFrame() {
		}

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

		public override KeyFrame Copy() {
			return new TextureKeyFrame(this);
		}
	}

	[Serializable]
	public class MergedTextureKeyFrame : KeyFrame {
		public float[] Offsets = new float[5];

		public MergedTextureKeyFrame() {
			for (int i = 0; i < Offsets.Length; i++)
				Offsets[i] = float.NaN;
		}

		public MergedTextureKeyFrame(MergedTextureKeyFrame kf) {
			Frame = kf.Frame;

			for (int i = 0; i < Offsets.Length; i++)
				Offsets[i] = kf.Offsets[i];
		}

		public override KeyFrame Copy() {
			return new MergedTextureKeyFrame(this);
		}
	}
}