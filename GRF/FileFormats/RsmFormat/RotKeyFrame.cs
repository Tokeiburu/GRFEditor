using System;
using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	public struct RotKeyFrame : IWriteableObject {
		public int Frame;
		public TkQuaternion Quaternion;

		/// <summary>
		/// Initializes a new instance of the <see cref="RotKeyFrame"/> struct.
		/// </summary>
		/// <param name="rkf">The RKF.</param>
		public RotKeyFrame(RotKeyFrame rkf) {
			Frame = rkf.Frame;
			Quaternion = rkf.Quaternion;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RotKeyFrame"/> struct.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public RotKeyFrame(IBinaryReader reader) {
			Frame = reader.Int32();
			Quaternion = new TkQuaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RotKeyFrame"/> struct.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset.</param>
		public RotKeyFrame(byte[] data, int offset) {
			Frame = BitConverter.ToInt32(data, offset);
			Quaternion = new TkQuaternion(
				BitConverter.ToSingle(data, offset + 4),
				BitConverter.ToSingle(data, offset + 8),
				BitConverter.ToSingle(data, offset + 12),
				BitConverter.ToSingle(data, offset + 16)
			);
		}

		public float this[int index] {
			get {
				if (index == 0)
					return Quaternion.X;
				if (index == 1)
					return Quaternion.Y;
				if (index == 2)
					return Quaternion.Z;
				if (index == 3)
					return Quaternion.W;

				throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Frame);
			writer.Write(Quaternion.X);
			writer.Write(Quaternion.Y);
			writer.Write(Quaternion.Z);
			writer.Write(Quaternion.W);
		}
	}
}