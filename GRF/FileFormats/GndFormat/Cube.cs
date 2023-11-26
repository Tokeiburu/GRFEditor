using System;
using System.IO;
using GRF.FileFormats.GatFormat;
using GRF.IO;

namespace GRF.FileFormats.GndFormat {
	public class Cube : IWriteableObject {
		private float? _average;

		/// <summary>
		/// Initializes a new instance of the <see cref="Cube" /> class.
		/// </summary>
		public Cube() {
			TileUp = 0;
			TileFront = -1;
			TileRight = -1;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Cube" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Cube(IBinaryReader data) {
			// 0 == bottom left
			// 1 == bottom right
			// 2 == top left
			// 3 == top right
			BottomLeft = data.Float();
			BottomRight = data.Float();
			TopLeft = data.Float();
			TopRight = data.Float();

			TileUp = data.Int32();
			TileFront = data.Int32();
			TileRight = data.Int32();
		}

		public float this[int index] {
			get {
				switch(index) {
					case 0:
						return BottomLeft;
					case 1:
						return BottomRight;
					case 2:
						return TopLeft;
					case 3:
						return TopRight;
					default:
						throw new ArgumentOutOfRangeException("index");
				}
			}
		}

		/// <summary>
		/// Gets or sets the bottom left position.
		/// </summary>
		public float BottomLeft { get; set; }

		/// <summary>
		/// Gets or sets the bottom right position.
		/// </summary>
		public float BottomRight { get; set; }

		/// <summary>
		/// Gets or sets the top left position.
		/// </summary>
		public float TopLeft { get; set; }

		/// <summary>
		/// Gets or sets the top right position.
		/// </summary>
		public float TopRight { get; set; }

		/// <summary>
		/// Gets or sets the tile up texture index.
		/// </summary>
		public int TileUp { get; set; }

		/// <summary>
		/// Gets or sets the tile front texture index.
		/// </summary>
		public int TileFront { get; set; }

		/// <summary>
		/// Gets or sets the tile right texture index.
		/// </summary>
		public int TileRight { get; set; }

		/// <summary>
		/// Gets the average height of a cell in the center.
		/// </summary>
		public float Average {
			get {
				if (_average == null)
					_average = (BottomLeft + BottomRight + TopLeft + TopRight) / 4f;
				return _average.Value;
			}
		}

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
			writer.Write(TileUp);
			writer.Write(TileFront);
			writer.Write(TileRight);
		}

		#endregion

		/// <summary>
		/// Sets the height on all corners of the cube.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetHeight(float height) {
			TopLeft = height;
			TopRight = height;
			BottomLeft = height;
			BottomRight = height;
		}

		public void AdjustRight(Cube cube) {
			TopRight = cube.TopLeft;
			BottomRight = cube.BottomLeft;
		}

		public void AdjustLeft(Cube cube) {
			BottomLeft = cube.BottomRight;
			TopLeft = cube.TopRight;
		}

		public void AdjustBottom(Cube cube) {
			BottomLeft = cube.TopLeft;
			BottomRight = cube.TopRight;
		}

		public void AdjustTop(Cube cube) {
			TopLeft = cube.BottomLeft;
			TopRight = cube.BottomRight;
		}

		public void AdjustTopLeft(Cube cube) {
			TopLeft = cube.BottomRight;
		}

		public void AdjustTopRight(Cube cube) {
			TopRight = cube.BottomLeft;
		}

		public void AdjustBottomLeft(Cube cube) {
			BottomLeft = cube.TopRight;
		}

		public void AdjustBottomRight(Cube cube) {
			BottomRight = cube.TopLeft;
		}

		public void Adjust(Gnd gnd, int x, int y) {
			Cube cube;

			if ((cube = gnd[x - 1, y - 1]) != null) cube.AdjustTopRight(this);
			if ((cube = gnd[x, y - 1]) != null) cube.AdjustTop(this);
			if ((cube = gnd[x + 1, y - 1]) != null) cube.AdjustTopLeft(this);
			if ((cube = gnd[x - 1, y]) != null) cube.AdjustRight(this);
			if ((cube = gnd[x + 1, y]) != null) cube.AdjustLeft(this);
			if ((cube = gnd[x - 1, y + 1]) != null) cube.AdjustBottomRight(this);
			if ((cube = gnd[x, y + 1]) != null) cube.AdjustBottom(this);
			if ((cube = gnd[x + 1, y + 1]) != null) cube.AdjustBottomLeft(this);
		}

		/// <summary>
		/// Adjusts the gat cells from the cubes.
		/// </summary>
		/// <param name="gat">The gat object.</param>
		/// <param name="x">The x offset.</param>
		/// <param name="y">The y offset.</param>
		public void AdjustGat(Gat gat, int x, int y) {
			int offset = 2 * y * gat.Header.Width + 2 * x;

			_average = null;

			float middleLeft = (TopLeft - BottomLeft) / 2f + BottomLeft;
			float middleRight = (BottomRight - TopRight) / 2f + TopRight;

			float middleTop = (TopLeft - TopRight) / 2f + TopRight;
			float middleBottom = (BottomLeft - BottomRight) / 2f + BottomRight;

			gat.Cells[offset].SetHeight(middleLeft, Average, BottomLeft, middleBottom);
			gat.Cells[offset + gat.Header.Width].SetHeight(TopLeft, middleTop, middleLeft, Average);
			gat.Cells[offset + 1].SetHeight(Average, middleRight, middleBottom, BottomRight);
			gat.Cells[offset + gat.Header.Width + 1].SetHeight(middleTop, TopRight, Average, middleRight);
		}
	}
}