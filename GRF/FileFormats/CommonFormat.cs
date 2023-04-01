using GRF.Image;

namespace GRF.FileFormats {
	public class CommonImageFormat : IImageable {
		public CommonImageFormat(MultiType data) {
			_loadImage(data.Data);
		}

		#region IImageable Members

		public GrfImage Image { get; set; }

		#endregion

		private void _loadImage(byte[] data) {
			Image = new GrfImage(ref data);
		}
	}
}