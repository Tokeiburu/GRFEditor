using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats.GndFormat;
using GRF.Graphics;
using GRF.IO;
using GRF.Image;

namespace GRF.FileFormats.GatFormat {
	/// <summary>
	/// Gat object, contains information about the walkable cells type and position
	/// </summary>
	public class Gat : IImageable, IPrintable, IEnumerable<Cell>, IWriteableFile {
		// Cells are reversed, they start from bottom to top
		private Cell[] _cells;
		private GrfImage _image;

		/// <summary>
		/// Initializes a new instance of the <see cref="Gat" /> class.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public Gat(int width, int height) {
			Header = new GatHeader(width, height);
			_cells = new Cell[Header.Width * Header.Height];

			for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
				_cells[i] = new Cell();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gat" /> class.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="def">The default cell value.</param>
		internal Gat(int width, int height, Cell def) {
			Header = new GatHeader(width, height);
			_cells = new Cell[Header.Width * Header.Height];

			for (int i = 0, count = Header.Width * Header.Height; i < count; i++) {
				_cells[i] = def;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gat" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Gat(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		private Gat(IBinaryReader data) {
			Header = new GatHeader(data);
			_loadCells(data);
		}

		/// <summary>
		/// Gets or sets the loaded file path of this object.
		/// </summary>
		public string LoadedPath { get; set; }

		/// <summary>
		/// Gets the header.
		/// </summary>
		public GatHeader Header { get; private set; }

		/// <summary>
		/// Gets the width.
		/// </summary>
		public int Width {
			get { return Header.Width; }
			internal set { Header.Width = value; }
		}

		/// <summary>
		/// Gets the height.
		/// </summary>
		public int Height {
			get { return Header.Height; }
			internal set { Header.Height = value; }
		}

		/// <summary>
		/// Gets the cells.
		/// </summary>
		public Cell[] Cells {
			get { return _cells; }
			internal set { _cells = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="Cell" /> at the specified index.
		/// </summary>
		/// <param name="index">The index of the cell.</param>
		/// <returns>The cell at the specified index.</returns>
		public Cell this[int index] {
			get { return _cells[index]; }
			set { _cells[index] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="Cell" /> with the specified coordinate (x, y).
		/// </summary>
		/// <param name="x">The x offset.</param>
		/// <param name="y">The y offset.</param>
		/// <returns>The cell at the specified index.</returns>
		public Cell this[int x, int y] {
			get { return _cells[y * Header.Width + x]; }
			set { _cells[y * Header.Width + x] = value; }
		}

		#region IEnumerable<GatCell> Members

		public IEnumerator<Cell> GetEnumerator() {
			return _cells.ToList().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region IImageable Members

		public GrfImage Image {
			get {
				if (_image == null) {
					GatPreviewImageMaker.LoadImage(this, GatPreviewFormat.GrayBlock, 0, null, null);
				}

				return _image;
			}
			set { _image = value; }
		}

		#endregion

		#region IPrintable Members

		public string GetInformation() {
			return FileFormatParser.DisplayObjectProperties(this);
		}

		#endregion

		#region IWriteableFile Members

		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "_loadedPath");
			Save(LoadedPath);
		}

		public void Save(string filename) {
			GrfExceptions.IfNullThrow(filename, "filename");
			using (BinaryWriter stream = new BinaryWriter(new FileStream(filename, FileMode.Create))) {
				_save(stream);
			}
		}

		public void Save(Stream stream) {
			GrfExceptions.IfNullThrow(stream, "stream");
			_save(new BinaryWriter(stream));
		}

		#endregion

		/// <summary>
		/// Sets the height of the cells.
		/// </summary>
		/// <param name="height">The height.</param>
		public void SetCellsHeight(float height) {
			for (int index = 0; index < _cells.Length; index++) {
				_cells[index].SetHeight(height);
			}
		}

		/// <summary>
		/// Identifies the water cells.
		/// </summary>
		/// <param name="height">The water height.</param>
		public void IdentifyWaterCells(float height) {
			foreach (Cell cell in _cells) {
				if ((cell.Type == GatType.Walkable || cell.Type == GatType.Unknown) && cell.BottomLeft > height)
					cell.IsWater = true;
			}
		}

		/// <summary>
		/// Identifies the gutter lines.
		/// </summary>
		public void IdentifyGutterLines() {
			int index;
			int lenX = (int) Math.Ceiling(Header.Width / 40f);
			int lenY = (int) Math.Ceiling(Header.Height / 40f);
			int posX;
			int posY;

			for (int y = 1; y < lenY; y++) {
				posY = 40 * y;

				for (int ys = 0; ys < 40; ys++) {
					if (posY + ys >= Header.Height)
						break;

					index = (posY + ys) * Header.Width;
					for (int x = 0; x < Header.Width; x++) {
						if (ys == 0) {
							_cells[index].SetInnerGutterLine();
						}
						else if (ys < 5) {
							_cells[index].SetOutterGutterLine();
						}

						index++;
					}
				}
			}

			for (int x = 1; x < lenX; x++) {
				posX = 40 * x;

				for (int xs = 0; xs < 40; xs++) {
					if (posX + xs >= Header.Width)
						break;

					index = posX + xs;
					for (int y = 0; y < Header.Height; y++) {
						if (xs == 0) {
							_cells[index].SetInnerGutterLine();
						}
						else if (xs < 5) {
							_cells[index].SetOutterGutterLine();
						}

						index += Header.Width;
					}
				}
			}
		}

		/// <summary>
		/// Adds a red circle on the map at the location.
		/// </summary>
		/// <param name="point">The center offset of the portal.</param>
		public void AddPortal(Point point) {
			const int MapSize = 512;

			int max = Math.Max(Header.Width, Header.Height);
			double multplier = MapSize / (float) max;

			int newHeight = (int) Math.Floor((Header.Height * multplier));
			int newWidth = (int) Math.Floor((Header.Width * multplier));

			var relX = point.X / Header.Width;
			var relY = 1f - point.Y / Header.Height;

			var x = (int) (relX * newWidth);
			var y = (int) (relY * newHeight);

			x += (MapSize - newWidth) / 2;
			y += (MapSize - newHeight) / 2;

			byte palIndex = 20;

			Image.Palette[palIndex * 4] = 255;
			Image.Palette[palIndex * 4 + 1] = 0;
			Image.Palette[palIndex * 4 + 2] = 0;
			Image.Palette[palIndex * 4 + 3] = 255;

			for (int i = x - 2; i <= x + 2; i++) {
				for (int j = y - 2; j <= y + 2; j++) {
					if (i < 0) continue;
					if (j < 0) continue;
					if (i >= Image.Width) continue;
					if (j >= Image.Height) continue;

					if ((i == x - 2 && j == y - 2) ||
					    (i == x + 2 && j == y - 2) ||
					    (i == x - 2 && j == y + 2) ||
					    (i == x + 2 && j == y + 2)) {
					}
					else {
						Image.Pixels[j * Image.Width + i] = palIndex;
					}
				}
			}
		}

		/// <summary>
		/// Adjusts the specified gat cells to the ground file.
		/// </summary>
		/// <param name="gnd">The ground object.</param>
		public void Adjust(Gnd gnd) {
			for (int y = 0; y < gnd.Header.Height; y++) {
				for (int x = 0; x < gnd.Header.Width; x++) {
					gnd[x, y].AdjustGat(this, x, y);
				}
			}
		}

		/// <summary>
		/// Generates and sets an image in Image with the options.
		/// </summary>
		/// <param name="previewFormat">The preview format.</param>
		/// <param name="options">The options.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="grfData">The GRF data.</param>
		public void LoadImage(GatPreviewFormat previewFormat, GatPreviewOptions options, string fileName, GrfHolder grfData) {
			GatPreviewImageMaker.LoadImage(this, previewFormat, options, fileName, grfData);
		}

		private void _save(BinaryWriter stream) {
			Header.Write(stream);

			foreach (Cell cell in _cells) {
				cell.Write(stream);
			}
		}

		private void _loadCells(IBinaryReader data) {
			_cells = new Cell[Header.Width * Header.Height];

			for (int i = 0; i < Header.Width * Header.Height; i++) {
				_cells[i] = new Cell(data);
			}
		}
	}
}