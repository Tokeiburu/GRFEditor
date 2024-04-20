using System.IO;
using GRF.IO;
using GRF.Image;

namespace GRF.FileFormats.GatFormat {
	public class Cell : IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="Cell" /> class.
		/// </summary>
		public Cell() {
			Type = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Cell" /> class.
		/// </summary>
		/// <param name="cell">The cell.</param>
		public Cell(Cell cell) {
			for (int i = 0; i < 4; i++)
				Heights[i] = cell.Heights[i];

			Average = cell.Average;
			Type = cell.Type;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Cell" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Cell(IBinaryReader data) {
			Heights = data.ArrayFloat(4);
			Average = Heights[2];
			Type = (GatType) data.Int32();
		}

		/// <summary>
		/// The heights of the cell positions
		/// 2-----3
		/// | \   |
		/// |  \  |
		/// |   \ |
		/// 0-----1
		/// </summary>
		public float[] Heights = new float[4];

		/// <summary>
		/// Gets the average height of the cell.
		/// </summary>
		public float Average { get; internal set; }

		/// <summary>
		/// Gets or sets the cell type.
		/// </summary>
		public GatType Type { get; set; }

		public bool? IsWater { get; internal set; }
		public bool? IsOutterGutterLine { get; private set; }
		public bool? IsInnerGutterLine { get; private set; }

		public float this[int index] {
			get { return Heights[index]; }
			set { Heights[index] = value; }
		}

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			for (int i = 0; i < 4; i++)
				writer.Write(Heights[i]);

			writer.Write((int) Type);
		}

		#endregion

		/// <summary>
		/// Sets the height.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetHeight(float height) {
			for (int i = 0; i < 4; i++)
				Heights[i] = height;
		}

		/// <summary>
		/// Sets the height.
		/// </summary>
		/// <param name="topLeft">Top left.</param>
		/// <param name="topRight">Top right.</param>
		/// <param name="bottomLeft">Bottom left.</param>
		/// <param name="bottomRight">Bottom right.</param>
		public void SetHeight(float topLeft, float topRight, float bottomLeft, float bottomRight) {
			Heights[0] = bottomLeft;
			Heights[1] = bottomRight;
			Heights[2] = topLeft;
			Heights[3] = topRight;
		}

		internal GrfColor GetColorHeightMap() {
			return _colorMult(GrfColor.FromArgb(255, 255, 255, 255), Average);
		}

		private GrfColor _colorMult(GrfColor color, float mult) {
			return GrfColor.FromArgb(255, (byte) (255 - color.R * mult), (byte) (255 - color.G * mult), (byte) (255 - color.B * mult));
		}

		/// <summary>
		/// Sets the inner gutter line.
		/// </summary>
		internal void SetInnerGutterLine() {
			if (Type == GatType.Walkable || Type == GatType.Walkable2 || Type == GatType.Walkable3)
				IsInnerGutterLine = true;
		}

		/// <summary>
		/// Sets the outter gutter line.
		/// </summary>
		internal void SetOutterGutterLine() {
			if (Type == GatType.Walkable || Type == GatType.Walkable2 || Type == GatType.Walkable3)
				IsOutterGutterLine = true;
		}

		/// <summary>
		/// Moves the cell up or down.
		/// </summary>
		/// <param name="y">The distance.</param>
		public void Move(float y) {
			for (int i = 0; i < 4; i++)
				Heights[i] += y;
		}
	}
}