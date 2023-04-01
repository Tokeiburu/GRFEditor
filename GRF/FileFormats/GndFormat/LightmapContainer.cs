using System.Collections.Generic;

namespace GRF.FileFormats.GndFormat {
	public class LightmapContainer {
		private readonly Gnd _gnd;
		private readonly List<Lightmap> _lightmaps = new List<Lightmap>();

		/// <summary>
		/// Initializes a new instance of the <see cref="LightmapContainer" /> class.
		/// </summary>
		/// <param name="gnd">The GND.</param>
		public LightmapContainer(Gnd gnd) {
			_gnd = gnd;
		}

		/// <summary>
		/// Gets the lightmaps.
		/// </summary>
		public List<Lightmap> Lightmaps {
			get { return _lightmaps; }
		}

		/// <summary>
		/// Gets the <see cref="Lightmap" /> at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The lightmap at the specified index.</returns>
		public Lightmap this[int index] {
			get {
				if (!_lightmaps[index].IsLoaded) {
					_lightmaps[index].Load(_gnd);
					_lightmaps[index].IsLoaded = true;
				}

				return _lightmaps[index];
			}
		}

		/// <summary>
		/// Adds the specified data.
		/// </summary>
		/// <param name="data">The data.</param>
		public void Add(byte[] data) {
			_lightmaps.Add(new Lightmap(data));
		}
	}
}