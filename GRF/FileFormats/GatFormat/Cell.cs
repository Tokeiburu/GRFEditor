using System.IO;
using GRF.Graphics;
using GRF.IO;
using System;
using System.Runtime.InteropServices;

namespace GRF.FileFormats.GatFormat {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Cell : IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="Cell" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Cell(IBinaryReader data) {
			H0 = data.Float();
			H1 = data.Float();
			H2 = data.Float();
			H3 = data.Float();
			Type = (GatType) data.Int32();

			if (Gat.AutomaticallyFixNegativeGatTypes && (int)Type < 0)
				Type = (GatType)((int)Type & ~0x80000000);
		}

		/// <summary>
		/// The heights of the cell positions
		/// 2-----3
		/// | \   |
		/// |  \  |
		/// |   \ |
		/// 0-----1
		/// </summary>
		public float H0;
		public float H1;
		public float H2;
		public float H3;

		/// <summary>
		/// Gets the average height of the cell.
		/// </summary>
		public float Average => H2;

		/// <summary>
		/// Gets or sets the cell type.
		/// </summary>
		public GatType Type;

		public float this[int index] {
			get {
				switch (index) {
					case 0:
						return H0;
					case 1:
						return H1;
					case 2:
						return H2;
					case 3:
						return H3;
					default:
						throw new ArgumentOutOfRangeException("index");
				}
			}
			set {
				switch (index) {
					case 0:
						H0 = value;
						break;
					case 1:
						H1 = value;
						break;
					case 2:
						H2 = value;
						break;
					case 3:
						H3 = value;
						break;
					default:
						throw new ArgumentOutOfRangeException("index");
				}
			}
		}

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(H0);
			writer.Write(H1);
			writer.Write(H2);
			writer.Write(H3);
			writer.Write((int) Type);
		}

		#endregion

		/// <summary>
		/// Sets the height.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetHeight(float height) {
			H0 = height;
			H1 = height;
			H2 = height;
			H3 = height;
		}

		/// <summary>
		/// Sets the height.
		/// </summary>
		/// <param name="topLeft">Top left.</param>
		/// <param name="topRight">Top right.</param>
		/// <param name="bottomLeft">Bottom left.</param>
		/// <param name="bottomRight">Bottom right.</param>
		public void SetHeight(float topLeft, float topRight, float bottomLeft, float bottomRight) {
			H0 = bottomLeft;
			H1 = bottomRight;
			H2 = topLeft;
			H3 = topRight;
		}

		/// <summary>
		/// Moves the cell up or down.
		/// </summary>
		/// <param name="y">The distance.</param>
		public void Move(float y) {
			H0 += y;
			H1 += y;
			H2 += y;
			H3 += y;
		}

		public TkVector3 CalcNormal() {
			var v1 = new TkVector3(10, -this[0], 0);
			var v2 = new TkVector3(0, -this[1], 0);
			var v3 = new TkVector3(10, -this[2], 10);
			var v4 = new TkVector3(0, -this[3], 10);

			var normal1 = TkVector3.Normalize(TkVector3.Cross(v4 - v3, v1 - v3));
			var normal2 = TkVector3.Normalize(TkVector3.Cross(v1 - v2, v4 - v2));
			return TkVector3.Normalize(normal1 + normal2);
		}
	}
}