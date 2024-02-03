using System.Collections.Generic;
using System.IO;
using GRF.IO;

namespace GRF.FileFormats.RswFormat {
	public class WaterData {
		public int WaterSplitWidth = 1;
		public int WaterSplitHeight = 1;
		public List<RswWater> Zones = new List<RswWater>();

		public WaterData() {
		}

		public WaterData(RswWater water) {
			Zones.Add(water);
		}
	};

	/// <summary>
	/// The RSW water information.
	/// </summary>
	public class RswWater {
		/// <summary>
		/// Initializes a new instance of the <see cref="RswWater" /> class.
		/// </summary>
		public RswWater() {
			WaveHeight = 0.2f;
			Level = 100.0f;
			WaveSpeed = 1.0f;
			WavePitch = 100.0f;
			Type = 3;
			TextureCycling = 3;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RswWater" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="header">The header.</param>
		public RswWater(IBinaryReader data, RswHeader header) {
			if (header.IsCompatibleWith(2, 6)) {
				return;
			}

			if (header.IsCompatibleWith(1, 3)) {
				Level = data.Float();
			}
			else
				Level = 0.0f;

			if (header.IsCompatibleWith(1, 8)) {
				Type = data.Int32();
				WaveHeight = data.Float();
				WaveSpeed = data.Float();
				WavePitch = data.Float();
			}
			else {
				Type = 0;
				WaveHeight = 1.0f;
				WaveSpeed = 2.0f;
				WavePitch = 50.0f;
			}

			if (header.IsCompatibleWith(1, 9)) {
				TextureCycling = data.Int32();
			}
			else {
				TextureCycling = 3;
			}
		}

		public RswWater(RswWater water) {
			WaveHeight = water.WaveHeight;
			Level = water.Level;
			WaveSpeed = water.WaveSpeed;
			WavePitch = water.WavePitch;
			Type = water.Type;
			TextureCycling = water.TextureCycling;
		}

		/// <summary>
		/// Gets or sets the level.
		/// </summary>
		public float Level { get; set; }

		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		public int Type { get; set; }

		/// <summary>
		/// Gets or sets the height of the wave.
		/// </summary>
		public float WaveHeight { get; set; }

		/// <summary>
		/// Gets or sets the wave speed.
		/// </summary>
		public float WaveSpeed { get; set; }

		/// <summary>
		/// Gets or sets the wave pitch.
		/// </summary>
		public float WavePitch { get; set; }

		/// <summary>
		/// Gets or sets the texture cycling.
		/// </summary>
		public int TextureCycling { get; set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="header">The RSW header.</param>
		public void Write(BinaryWriter writer, RswHeader header) {
			if (header.Version >= 2.6)
				return;

			if (header.Version >= 1.3)
				writer.Write(Level);

			if (header.Version >= 1.8) {
				writer.Write(Type);
				writer.Write(WaveHeight);
				writer.Write(WaveSpeed);
				writer.Write(WavePitch);
			}

			if (header.Version >= 1.9)
				writer.Write(TextureCycling);
		}

		#endregion

		/// <summary>
		/// Resets this instance.
		/// </summary>
		public void Reset() {
			WaveHeight = 0.2f;
			Level = 100.0f;
			WaveSpeed = 1.0f;
			WavePitch = 100.0f;
			Type = 3;
			TextureCycling = 3;
		}
	}
}