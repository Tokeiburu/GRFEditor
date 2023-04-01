using System;
using System.IO;
using System.Linq;
using GRF.IO;
using Utilities;

namespace GRF.FileFormats.RsmFormat {
	/// <summary>
	/// Represents a triangle face used in meshes
	/// </summary>
	public class Face {
		public UInt16 Padding;
		public int[] SmoothGroup = new int[3];
		public UInt16 TextureId;
		public UInt16[] TextureVertexIds = new UInt16[3];
		public int TwoSide;
		public UInt16[] VertexIds = new UInt16[3];

		/// <summary>
		/// Initializes a new instance of the <see cref="Face" /> class.
		/// </summary>
		/// <param name="header">Rsm Header</param>
		/// <param name="reader">The reader.</param>
		public Face(RsmHeader header, IBinaryReader reader) {
			int len = -1;

			if (header.Version >= 2.2) {
				len = reader.Int32();
			}

			VertexIds = reader.ArrayUInt16(3);
			TextureVertexIds = reader.ArrayUInt16(3);
			TextureId = reader.UInt16();
			Padding = reader.UInt16();
			TwoSide = reader.Int32();
			SmoothGroup[0] = SmoothGroup[1] = SmoothGroup[2] = reader.Int32();

			if (len > 24) {
				SmoothGroup[1] = reader.Int32();
			}

			if (len > 28) {
				SmoothGroup[2] = reader.Int32();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Face" /> class.
		/// </summary>
		/// <param name="header">Rsm Header</param>
		/// <param name="reader">The reader.</param>
		/// <param name="smoothGroup">The smooth group.</param>
		public Face(RsmHeader header, IBinaryReader reader, int smoothGroup) {
			VertexIds = reader.ArrayUInt16(3);
			TextureVertexIds = reader.ArrayUInt16(3);
			TextureId = reader.UInt16();
			Padding = reader.UInt16();
			TwoSide = reader.Int32();

			for (int i = 0; i < 3; i++) {
				SmoothGroup[i] = smoothGroup;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Face" /> class.
		/// </summary>
		public Face() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Face" /> class.
		/// </summary>
		/// <param name="face">The face.</param>
		public Face(Face face) {
			VertexIds[0] = face.VertexIds[0];
			VertexIds[1] = face.VertexIds[1];
			VertexIds[2] = face.VertexIds[2];

			TextureVertexIds[0] = face.TextureVertexIds[0];
			TextureVertexIds[1] = face.TextureVertexIds[1];
			TextureVertexIds[2] = face.TextureVertexIds[2];

			TextureId = face.TextureId;
			Padding = face.Padding;
			TwoSide = face.TwoSide;
			SmoothGroup = face.SmoothGroup.ToArray();
		}

		public void Write(Rsm model, BinaryWriter writer) {
			if (model.Version >= 2.2) {
				int len = 24;

				if (SmoothGroup[1] != SmoothGroup[0]) {
					len += 4;

					if (SmoothGroup[2] != SmoothGroup[0]) {
						len += 4;
					}
				}
				else if (SmoothGroup[2] != SmoothGroup[0]) {
					len += 8;
				}

				writer.Write(len);
			}

			for (int i = 0; i < 3; i++) {
				writer.Write(VertexIds[i]);
			}

			for (int i = 0; i < 3; i++) {
				writer.Write(TextureVertexIds[i]);
			}

			writer.Write(TextureId);
			writer.Write(Padding);
			writer.Write(TwoSide);
			writer.Write(SmoothGroup[0]);

			if (model.Version >= 2.2) {
				if (SmoothGroup[1] != SmoothGroup[0]) {
					writer.Write(SmoothGroup[1]);

					if (SmoothGroup[2] != SmoothGroup[0]) {
						writer.Write(SmoothGroup[2]);
					}
				}
				else if (SmoothGroup[2] != SmoothGroup[0]) {
					writer.Write(SmoothGroup[1]);
					writer.Write(SmoothGroup[2]);
				}
			}
		}
	}
}