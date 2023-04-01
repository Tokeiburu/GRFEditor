using System;
using System.IO;
using System.Linq;
using GRF.Image;

namespace GRF.FileFormats.SprFormat {
	public class SprConverterV1M0 : SprAbstract, ISprConverter {
		public SprConverterV1M0(SprHeader header) : base(header) {
		}

		#region ISprConverter Members

		/// <summary>
		/// Converter's name
		/// </summary>
		public string DisplayName {
			get { return "Version 0x100 (Indexed8)"; }
		}

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="close">if set to <c>true</c> [close the stream].</param>
		public void Save(Spr spr, Stream stream, bool close) {
			BinaryWriter writer = new BinaryWriter(stream);

			try {
				spr.Header.Write(writer);
				writer.Write((byte) 0);
				writer.Write((byte) 1);
				writer.Write((ushort) spr.Images.Count(p => p.GrfImageType == GrfImageType.Indexed8));

				for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
					_writeAsIndexed8(writer, spr.Images[i], i, 1, 0);
				}

				if (spr.NumberOfIndexed8Images > 0) {
					byte[] palette = new byte[1024];
					Buffer.BlockCopy(spr.Palette.BytePalette, 0, palette, 0, 1024);
					palette[3] = 255;
					writer.Write(palette);
				}
			}
			finally {
				if (close)
					writer.Close();
			}
		}

		/// <summary>
		/// Saves the specified SPR.
		/// </summary>
		/// <param name="spr">The SPR.</param>
		/// <param name="filename">The filename.</param>
		public void Save(Spr spr, string filename) {
			Save(spr, File.Create(filename), true);
		}

		#endregion
	}
}