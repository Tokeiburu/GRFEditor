using System.Collections.Generic;
using System.Collections.ObjectModel;
using GRF.Image;

namespace GRF.FileFormats.GndFormat {
	/// <summary>
	/// Represents a lightmap for a tile
	/// </summary>
	public class Lightmap {
		public byte[] Data;
		private readonly List<GrfColor> _colors = new List<GrfColor>();
		private Gnd _gnd;

		/// <summary>
		/// Initializes a new instance of the <see cref="Lightmap" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Lightmap(byte[] data) {
			Data = data;
		}

		/// <summary>
		/// Gets the colors.
		/// </summary>
		public ReadOnlyCollection<GrfColor> Colors {
			get { return _colors.AsReadOnly(); }
		}

		/// <summary>
		/// Gets the <see cref="GrfColor" /> at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The color at the specified index.</returns>
		public GrfColor this[int index] {
			get { return _colors[index]; }
			set {
				Data[index] = value.A;
				Data[3 * index + _gnd.PerCell + 0] = value.R;
				Data[3 * index + _gnd.PerCell + 1] = value.G;
				Data[3 * index + _gnd.PerCell + 2] = value.B;
			}
		}

		public int Count {
			get { return _colors.Count; }
		}

		internal bool IsLoaded { get; set; }

		internal void Load(Gnd gnd) {
			int offset = 0;
			_gnd = gnd;

			for (int i = 0; i < gnd.PerCell; i++) {
				_colors.Add(new GrfColor());
			}

			for (int i = 0; i < gnd.PerCell; i++) {
				_colors[i].A = Data[offset++];
			}

			for (int i = 0; i < gnd.PerCell; i++) {
				_colors[i].R = Data[offset++];
				_colors[i].G = Data[offset++];
				_colors[i].B = Data[offset++];
			}
		}
	}
}