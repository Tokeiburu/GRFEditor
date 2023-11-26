using System.IO;
using GRF.IO;

namespace GRF.FileFormats.RswFormat {
	public class RswLight {
		/// <summary>
		/// Initializes a new instance of the <see cref="RswLight" /> class.
		/// </summary>
		public RswLight() {
			Longitude = 45;
			Latitude = 45;
			AmbientBlue = 1f;
			AmbientGreen = 1f;
			AmbientRed = 1f;
			DiffuseBlue = 1f;
			DiffuseGreen = 1f;
			DiffuseRed = 1f;
			Intensity = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RswLight" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="header">The header.</param>
		public RswLight(IBinaryReader reader, RswHeader header) {
			Intensity = 0.5f;

			if (header.IsCompatibleWith(1, 5)) {
				Longitude = reader.Int32();
				Latitude = reader.Int32();
				DiffuseRed = reader.Float();
				DiffuseGreen = reader.Float();
				DiffuseBlue = reader.Float();
				AmbientRed = reader.Float();
				AmbientGreen = reader.Float();
				AmbientBlue = reader.Float();
			}
			else {
				Longitude = 45;
				Latitude = 45;
				DiffuseRed = 1;
				DiffuseGreen = 1;
				DiffuseBlue = 1;
				AmbientRed = 0.3f;
				AmbientGreen = 0.3f;
				AmbientBlue = 0.3f;
			}

			if (header.IsCompatibleWith(1, 7)) {
				Intensity = reader.Float();
			}
		}

		/// <summary>
		/// Gets or sets the longitude.
		/// </summary>
		public int Longitude { get; set; }
		
		/// <summary>
		/// Gets or sets the latitude.
		/// </summary>
		public int Latitude { get; set; }
		
		/// <summary>
		/// Gets or sets the diffuse red.
		/// </summary>
		public float DiffuseRed { get; set; }
		
		/// <summary>
		/// Gets or sets the diffuse green.
		/// </summary>
		public float DiffuseGreen { get; set; }
		
		/// <summary>
		/// Gets or sets the diffuse blue.
		/// </summary>
		public float DiffuseBlue { get; set; }
		
		/// <summary>
		/// Gets or sets the ambient red.
		/// </summary>
		public float AmbientRed { get; set; }
		
		/// <summary>
		/// Gets or sets the ambient green.
		/// </summary>
		public float AmbientGreen { get; set; }
		
		/// <summary>
		/// Gets or sets the ambient blue.
		/// </summary>
		public float AmbientBlue { get; set; }

		public float Intensity { get; set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="header">The header.</param>
		public void Write(BinaryWriter writer, RswHeader header) {
			if (header.IsCompatibleWith(1, 5)) {
				writer.Write(Longitude);
				writer.Write(Latitude);
				writer.Write(DiffuseRed);
				writer.Write(DiffuseGreen);
				writer.Write(DiffuseBlue);
				writer.Write(AmbientRed);
				writer.Write(AmbientGreen);
				writer.Write(AmbientBlue);
			}

			if (header.IsCompatibleWith(1, 7)) {
				writer.Write(Intensity);
			}
		}
	}
}