using GRF.FileFormats.SprFormat.Builder;
using GRF.Image;

namespace GRF.FileFormats.SprFormat.Commands {
	public class ChangeId : ICommand {
		private readonly int _indexSource;
		private readonly GrfImageType _type;
		private readonly SprBuilderImageView _view;
		private int _targetId;

		public ChangeId(SprBuilderImageView view, int targetId) {
			_view = view;
			_targetId = targetId;
			_indexSource = view.DisplayID;
			_type = view.Image.GrfImageType;
		}

		#region ICommand Members

		public void Execute(SprBuilderInterface sbi) {
			if (_type == GrfImageType.Indexed8)
				_targetId = _targetId < 0 ? 0 : _targetId >= sbi.ImagesIndexed8.Count ? sbi.ImagesIndexed8.Count - 1 : _targetId;
			else
				_targetId = _targetId < 0 ? 0 : _targetId >= sbi.ImagesBgra32.Count ? sbi.ImagesBgra32.Count - 1 : _targetId;

			sbi.ChangeImageIndex(_view.DisplayID, _type, _targetId, _type);
		}

		public void Undo(SprBuilderInterface sbi) {
			sbi.ChangeImageIndex(_targetId, _type, _indexSource, _type);
		}

		#endregion
	}
}