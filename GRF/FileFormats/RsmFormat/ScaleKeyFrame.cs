using System;
using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	public struct ScaleKeyFrame : IWriteableObject {
		public int Frame;
		public float Sx;
		public float Sy;
		public float Sz;
		public float Data;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScaleKeyFrame"/> struct.
		/// </summary>
		/// <param name="pkf">The PKF.</param>
		public ScaleKeyFrame(ScaleKeyFrame pkf) {
			Frame = pkf.Frame;
			Sx = pkf.Sx;
			Sy = pkf.Sy;
			Sz = pkf.Sz;
			Data = pkf.Data;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScaleKeyFrame"/> struct.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public ScaleKeyFrame(IBinaryReader reader) {
			Frame = reader.Int32();
			Sx = reader.Float();
			Sy = reader.Float();
			Sz = reader.Float();
			Data = reader.Float();
		}

		/// <summary>
		/// Gets the scale.
		/// </summary>
		/// <value>The scale.</value>
		public Vertex Scale {
			get { return new Vertex(Sx, Sy, Sz); }
		}

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Frame);
			writer.Write(Sx);
			writer.Write(Sy);
			writer.Write(Sz);
			writer.Write(Data);
		}
	}
}