using System;
using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	public struct PosKeyFrame : IWriteableObject {
		public int Frame;
		public float X;
		public float Y;
		public float Z;
		public int Data;

		/// <summary>
		/// Gets the position.
		/// </summary>
		/// <value>The position.</value>
		public TkVector3 Position {
			get { return new TkVector3(X, Y, Z); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PosKeyFrame"/> struct.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public PosKeyFrame(IBinaryReader reader) {
			Frame = reader.Int32();
			X = reader.Float();
			Y = reader.Float();
			Z = reader.Float();
			Data = reader.Int32();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PosKeyFrame"/> struct.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset.</param>
		public PosKeyFrame(byte[] data, int offset) {
			Frame = BitConverter.ToInt32(data, offset);
			X = BitConverter.ToSingle(data, offset + 4);
			Y = BitConverter.ToSingle(data, offset + 8);
			Z = BitConverter.ToSingle(data, offset + 12);
			Data = BitConverter.ToInt32(data, offset + 14);
		}

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Frame);
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
			writer.Write(Data);
		}
	}
}