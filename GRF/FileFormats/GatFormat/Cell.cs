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
			BottomLeft = cell.BottomLeft;
			BottomRight = cell.BottomRight;
			TopLeft = cell.TopLeft;
			TopRight = cell.TopRight;
			Average = cell.Average;
			Type = cell.Type;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Cell" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Cell(IBinaryReader data) {
			BottomLeft = data.Float();
			BottomRight = data.Float();
			TopLeft = data.Float();
			TopRight = data.Float();
			Average = TopLeft;
			Type = (GatType) data.Int32();
		}

		/// <summary>
		/// Gets or sets the bottom left position of the cell.
		/// </summary>
		public float BottomLeft { get; set; }

		/// <summary>
		/// Gets or sets the bottom right position of the cell.
		/// </summary>
		public float BottomRight { get; set; }

		/// <summary>
		/// Gets or sets the top left position of the cell.
		/// </summary>
		public float TopLeft { get; set; }

		/// <summary>
		/// Gets or sets the top right position of the cell.
		/// </summary>
		public float TopRight { get; set; }

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

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(BottomLeft);
			writer.Write(BottomRight);
			writer.Write(TopLeft);
			writer.Write(TopRight);
			writer.Write((int) Type);
		}

		#endregion

		/// <summary>
		/// Sets the height.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetHeight(float height) {
			TopLeft = height;
			TopRight = height;
			BottomLeft = height;
			BottomRight = height;
		}

		/// <summary>
		/// Sets the height.
		/// </summary>
		/// <param name="topLeft">Top left.</param>
		/// <param name="topRight">Top right.</param>
		/// <param name="bottomLeft">Bottom left.</param>
		/// <param name="bottomRight">Bottom right.</param>
		public void SetHeight(float topLeft, float topRight, float bottomLeft, float bottomRight) {
			TopLeft = topLeft;
			TopRight = topRight;
			BottomLeft = bottomLeft;
			BottomRight = bottomRight;
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
			BottomLeft += y;
			BottomRight += y;
			TopLeft += y;
			TopRight += y;
		}
	}
}