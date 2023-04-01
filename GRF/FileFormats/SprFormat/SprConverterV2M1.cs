using System.IO;
using GRF.FileFormats.SprFormat.Builder;

namespace GRF.FileFormats.SprFormat {
	public class SprConverterV2M1 : SprAbstract, ISprConverter {
		public SprConverterV2M1(SprHeader header) : base(header) {
		}

		public static bool AutomaticDowngradeOnRleException { get; set; }

		#region ISprConverter Members

		/// <summary>
		/// Converter's name
		/// </summary>
		public string DisplayName {
			get { return "Version 0x201 (rle encoded)"; }
		}

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="filename">The filename.</param>
		public void Save(Spr spr, string filename) {
			try {
				_save(spr, filename);
			}
			catch (SprRleBufferOverflowException) {
				if (AutomaticDowngradeOnRleException) {
					var conv = new SprConverterV2M0(null);
					conv.Save(spr, filename);
				}
				else
					throw;
			}
		}

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="close">if set to <c>true</c> [close the stream].</param>
		public void Save(Spr spr, Stream stream, bool close) {
			try {
				_save(spr, stream, close);
			}
			catch (SprRleBufferOverflowException) {
				if (AutomaticDowngradeOnRleException) {
					var conv = new SprConverterV2M0(null);
					conv.Save(spr, stream, close);
				}
				else
					throw;
			}
		}

		#endregion

		private void _save(Spr spr, string filename) {
			_saveAs(spr, filename, 2, 1);
		}

		private void _save(Spr spr, Stream stream, bool close) {
			_saveAs(spr, stream, 2, 1, close);
		}
	}
}