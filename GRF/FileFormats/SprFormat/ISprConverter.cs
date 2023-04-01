using System.Collections.Generic;
using System.IO;
using GRF.IO;
using GRF.Image;

namespace GRF.FileFormats.SprFormat {
	public interface ISprConverter {
		/// <summary>
		/// Converter's name
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the images.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="loadFirstImageOnly">if set to <c>true</c> [load first image only].</param>
		/// <returns>A list of the loaded images.</returns>
		List<GrfImage> GetImages(Spr spr, IBinaryReader reader, bool loadFirstImageOnly);

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="filename">The filename.</param>
		void Save(Spr spr, string filename);

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="close">if set to <c>true</c> [close the stream].</param>
		void Save(Spr spr, Stream stream, bool close);
	}
}