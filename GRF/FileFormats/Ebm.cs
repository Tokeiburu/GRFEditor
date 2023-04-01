using GRF.Core;
using GRF.Image;

namespace GRF.FileFormats {
	public class Ebm : IImageable {
		public Ebm(byte[] data) {
			Image = new CommonImageFormat(Compression.Decompress(data, 32768)).Image;
		}

		#region IImageable Members

		public GrfImage Image { get; set; }

		#endregion
	}
}