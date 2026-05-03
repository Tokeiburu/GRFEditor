using System.IO;
using GRF.IO;

namespace GRF.FileFormats.RswFormat {
	public class RswLight {
		/// <summary>
		/// Initializes a new instance of the <see cref="RswLight" /> class.
		/// </summary>
		public RswLight() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RswLight" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="header">The header.</param>
		public RswLight(IBinaryReader reader, RswHeader header) {
			if (header.IsCompatibleWith(1, 5)) {
				Longitude = reader.Int32();
				Latitude = reader.Int32();
				DiffuseRed = reader.Float();
				DiffuseGreen = reader.Float();
				DiffuseBlue = reader.Float();
				AmbientRed = reader.Float();
				AmbientGreen = reader.Float();
				AmbientBlue = reader.Float();

				if (header.IsCompatibleWith(1, 7)) {
					Intensity = reader.Float();
				}
			}
		}

		/// <summary>
		/// Gets or sets the longitude.
		/// </summary>
		public int Longitude { get; set; } = 45;

		/// <summary>
		/// Gets or sets the latitude.
		/// </summary>
		public int Latitude { get; set; } = 45;

		/// <summary>
		/// Gets or sets the diffuse red.
		/// </summary>
		public float DiffuseRed { get; set; } = 1f;
		
		/// <summary>
		/// Gets or sets the diffuse green.
		/// </summary>
		public float DiffuseGreen { get; set; } = 1f;

		/// <summary>
		/// Gets or sets the diffuse blue.
		/// </summary>
		public float DiffuseBlue { get; set; } = 1f;

		/// <summary>
		/// Gets or sets the ambient red.
		/// </summary>
		public float AmbientRed { get; set; } = 1f;

		/// <summary>
		/// Gets or sets the ambient green.
		/// </summary>
		public float AmbientGreen { get; set; } = 1f;

		/// <summary>
		/// Gets or sets the ambient blue.
		/// </summary>
		public float AmbientBlue { get; set; } = 1f;

		/// <summary>
		/// Gets or sets the shadow intensity (property not used anymore).
		/// </summary>
		public float Intensity { get; set; } = 0.5f;

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="header">The header.</param>
		public void Write(BinaryWriter writer, RswHeader header) {
			if (header.Version >= 1.5) {
				writer.Write(Longitude);
				writer.Write(Latitude);
				writer.Write(DiffuseRed);
				writer.Write(DiffuseGreen);
				writer.Write(DiffuseBlue);
				writer.Write(AmbientRed);
				writer.Write(AmbientGreen);
				writer.Write(AmbientBlue);

				if (header.Version >= 1.7) {
					writer.Write(Intensity);
				}
			}
		}
	}
}