using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GRF.Image;
using Utilities;

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

		public int Hash(Gnd gnd) {
			const uint poly = 0x82f63b78;

			long crc = ~0;
			int size = gnd.LightmapWidth * gnd.LightmapHeight * 4;
			if (size < 4)
				return 0;
			for (int i = 0; i < size; i++) {
				crc ^= Data[i];
				crc = (crc & 1) == 1 ? (crc >> 1) ^ poly : crc >> 1;
			}
			return (int)~crc;
		}

		public static bool operator ==(Lightmap a, Lightmap b) {
			return NativeMethods.memcmp(a.Data, b.Data, Math.Max(a.Data.Length, b.Data.Length)) == 0;
		}

		public static bool operator !=(Lightmap a, Lightmap b) {
			return !(a == b);
		}

		public override bool Equals(object obj) {
			return base.Equals(obj);
		}

		public override int GetHashCode() {
			return -301143667 + EqualityComparer<byte[]>.Default.GetHashCode(Data);
		}
	}
}