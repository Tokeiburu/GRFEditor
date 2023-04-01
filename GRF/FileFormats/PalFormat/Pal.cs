using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.Image;

namespace GRF.FileFormats.PalFormat {
	public class Pal : IImageable {
		#region Delegates

		public delegate void PalEventHandler(object sender);

		#endregion

		public static byte[] PixelsPalette; // Default image with pixels already indexed (loads faster)

		private bool _enableRaiseEvents = true;
		private byte[] _palette = new byte[1024];

		static Pal() {
			PixelsPalette = new byte[256 * 256];

			int index;
			for (int i = 0; i < 256; i++) {
				for (int j = 0; j < 256; j++) {
					index = i * 256 + j;

					PixelsPalette[index] = (byte) ((i / 16) * 16 + (j / 16));
				}
			}
		}

		public Pal(byte[] dataDecompressed, bool reformatPalette = true) : this() {
			int offset = 0;

			if (reformatPalette) {
				for (int j = 0; j < 256; j++) {
					dataDecompressed[offset + 3] = 255;
					offset += 4;
				}
			}

			if (dataDecompressed.Length > 1024) {
				// What kind of shit software made palettes with more than 256 colors...
				byte[] temp = new byte[1024];
				Buffer.BlockCopy(dataDecompressed, 0, temp, 0, 1024);
				dataDecompressed = temp;
			}

			Buffer.BlockCopy(dataDecompressed, 0, _palette, 0, 1024);
		}

		public Pal(string path) : this(File.ReadAllBytes(path)) {
			LoadedPath = path;
		}

		public Pal() {
			Commands = new CommandsHolder(this);
		}

		public CommandsHolder Commands { get; private set; }
		public string LoadedPath { get; set; }

		public bool EnableRaiseEvents {
			get { return _enableRaiseEvents; }
			set { _enableRaiseEvents = value; }
		}

		/// <summary>
		/// Gets the palette, in the RGBA format.
		/// </summary>
		public byte[] BytePalette {
			get { return _palette; }
		}

		public byte this[int offset1024] {
			get { return _palette[offset1024]; }
			set {
				_palette[offset1024] = value;
				OnPaletteChanged();
			}
		}

		public byte[] this[int row16, int column16] {
			get {
				if (row16 > 15 || column16 > 15)
					throw new Exception("Palette index is invalid, row and column must be below 16.");

				byte[] color = new byte[4];
				Buffer.BlockCopy(_palette, (row16 * 16 + column16) * 4, color, 0, 4);
				return color;
			}
			set {
				if (row16 > 15 || column16 > 15)
					throw new Exception("Palette index is invalid, row and column must be below 16.");

				int basePosition = (row16 * 16 + column16) * 4;
				if (value.Length + basePosition > 1024)
					throw new Exception("Byte array too large.");

				Buffer.BlockCopy(value, 0, _palette, basePosition, value.Length);
				OnPaletteChanged();
			}
		}

		public IEnumerable<GrfColor> Colors {
			get {
				for (int i = 0; i < 256; i++) {
					yield return GetColor(i);
				}
			}
		}

		#region IImageable Members

		public GrfImage Image {
			get { return new GrfImage(ref PixelsPalette, 256, 256, GrfImageType.Indexed8, ref _palette); }
			set { }
		}

		#endregion

		public void DeleteEvents() {
			PaletteChanged = null;
		}

		public void MakeFirstColorUnique() {
			GrfColor color = new GrfColor(this[0, 0], 0);
			color.A = 255;

			while (Colors.Skip(1).Contains(color)) {
				color = GrfColor.Random();
			}

			this[0, 0] = color.ToRgbaBytes();
		}

		public bool Contains(GrfColor color) {
			return Colors.Contains(color);
		}

		public event PalEventHandler PaletteChanged;

		public virtual void OnPaletteChanged() {
			if (!EnableRaiseEvents)
				return;

			PalEventHandler handler = PaletteChanged;
			if (handler != null) handler(this);
		}

		public GrfColor GetColor(int row16, int column16) {
			if (row16 > 15 || column16 > 15)
				throw new Exception("Palette index is invalid, row and column must be below 16.");

			int baseIndex = (row16 * 16 + column16) * 4;
			return new GrfColor(_palette[baseIndex + 3], _palette[baseIndex], _palette[baseIndex + 1], _palette[baseIndex + 2]);
		}

		public GrfColor GetColor(int index256) {
			if (index256 >= 256)
				throw new Exception("Palette index is invalid, index must be below 256.");

			if (index256 < 0)
				throw new Exception("Palette index is invalid, index must be above -1.");

			int baseIndex = index256 * 4;
			return new GrfColor(_palette[baseIndex + 3], _palette[baseIndex], _palette[baseIndex + 1], _palette[baseIndex + 2]);
		}

		public void SetColor(int index256, GrfColor color) {
			if (index256 >= 256)
				throw new Exception("Palette index is invalid, index must be below 256.");

			if (index256 < 0)
				throw new Exception("Palette index is invalid, index must be above -1.");

			SetBytes(index256 * 4, color.ToRgbaBytes());
		}

		public List<GrfColor> GetColors(IEnumerable<int> indexes) {
			return indexes.Select(index => GetColor(index)).ToList();
		}

		public bool Save(string path) {
			try {
				File.WriteAllBytes(path, BytePalette);
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return false;
		}

		public void SaveThrow(string path) {
			File.WriteAllBytes(path, BytePalette);
		}

		public void Save(Stream stream) {
			stream.Write(BytePalette, 0, 1024);
		}

		public void SetPalette(byte[] dataRgba1024) {
			if (dataRgba1024.Length < 1024)
				throw new Exception("The length of the palette byte array must be equal to 1024.");

			Buffer.BlockCopy(dataRgba1024, 0, _palette, 0, 1024);
			OnPaletteChanged();
		}

		public void SetBytes(int offset1024, byte[] newBytesRgba) {
			Buffer.BlockCopy(newBytesRgba, 0, _palette, offset1024, newBytesRgba.Length);
			OnPaletteChanged();
		}

		public static int Offset1024ToOffset256(int offset1024) {
			return offset1024 / 4;
		}

		public static int Offset1024ToOffset32(int offset1024) {
			return offset1024 / 32;
		}

		public static int Offset32ToOffset1024(int offset32) {
			return offset32 * 32;
		}
	}
}