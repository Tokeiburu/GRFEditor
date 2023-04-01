using System.IO;

namespace GRF.FileFormats.SprFormat {
	public class SprConverterV2M0 : SprAbstract, ISprConverter {
		public SprConverterV2M0(SprHeader header) : base(header) {
		}

		#region ISprConverter Members

		/// <summary>
		/// Converter's name
		/// </summary>
		public string DisplayName {
			get { return "Version 0x200"; }
		}

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="filename">The filename.</param>
		public void Save(Spr spr, string filename) {
			_saveAs(spr, filename, 2, 0);
		}

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="close">if set to <c>true</c> [close the stream].</param>
		public void Save(Spr spr, Stream stream, bool close) {
			_saveAs(spr, stream, 2, 0, close);
		}

		#endregion
	}
}