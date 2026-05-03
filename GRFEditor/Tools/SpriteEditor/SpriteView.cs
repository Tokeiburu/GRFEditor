using GRF.FileFormats.SprFormat;
using GRF.Image;
using System.Windows.Media.Imaging;

namespace GRFEditor.Tools.SpriteEditor {
	public class SpriteView {
		public int AbsoluteId { get; set; }
		public GrfImageType ImageType { get; set; }
		public string DisplayName => $"{_spriteName}{AbsoluteId:0000}";
		public GrfImage GrfImage {
			get {
				if (DisplayImage != null)
					return _cachedGrfImage;

				return null;
			}
		}
		public object DisplayImage {
			get {
				if (_cachedImage != null)
					return _cachedImage;

				_cachedGrfImage = _spr.GetImage(AbsoluteId);

				if (_cachedGrfImage == null)
					return null;

				_cachedImage = _cachedGrfImage.Cast<BitmapSource>();
				return _cachedImage;
			}
		}

		private GrfImage _cachedGrfImage;
		private object _cachedImage;
		private string _spriteName;
		private Spr _spr;

		public SpriteView(Spr spr, int id, GrfImageType type, string spriteName) {
			AbsoluteId = id;
			_spr = spr;
			ImageType = type;
			_spriteName = spriteName;
		}
	}
}
