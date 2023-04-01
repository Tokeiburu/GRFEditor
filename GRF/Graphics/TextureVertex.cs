using System;
using System.IO;
using GRF.IO;

namespace GRF.Graphics {
	public struct TextureVertex {
		public uint Color;
		public float U;
		public float V;

		public TextureVertex(IBinaryReader reader) {
			Color = reader.UInt32();
			U = reader.Float();
			V = reader.Float();
		}

		public TextureVertex(IBinaryReader reader, uint color) {
			Color = color;
			U = reader.Float();
			V = reader.Float();
		}

		public TextureVertex(byte[] data, int offset) {
			Color = BitConverter.ToUInt32(data, offset);
			U = BitConverter.ToSingle(data, offset + 4);
			V = BitConverter.ToSingle(data, offset + 8);
		}

		public TextureVertex(byte[] data, int offset, uint color) {
			Color = color;
			U = BitConverter.ToSingle(data, offset);
			V = BitConverter.ToSingle(data, offset + 4);
		}

		public void Write(BinaryWriter writer, bool color) {
			if (color) {
				writer.Write(Color);
			}

			writer.Write(U);
			writer.Write(V);
		}
	}
}
