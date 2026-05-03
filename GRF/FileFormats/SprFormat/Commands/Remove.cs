using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.Image;

namespace GRF.FileFormats.SprFormat.Commands {
	public class RemoveCommand : IActCommand {
		private readonly int _absoluteIndex;
		private GrfImage _conflict;

		public RemoveCommand(int absoluteIndex) {
			_absoluteIndex = absoluteIndex;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_conflict == null) {
				_conflict = act.Sprite.Images[_absoluteIndex];
			}

			GrfImageType type = _conflict.GrfImageType;
			act.Sprite.Remove(type == GrfImageType.Indexed8 ? _absoluteIndex : _absoluteIndex - act.Sprite.NumberOfIndexed8Images, type);
		}

		public void Undo(Act act) {
			GrfImageType type = _conflict.GrfImageType;
			act.Sprite.AddImage(_conflict, type == GrfImageType.Indexed8 ? _absoluteIndex : _absoluteIndex - act.Sprite.NumberOfIndexed8Images);
		}

		public string CommandDescription {
			get { return "Remove sprite " + _absoluteIndex; }
		}

		#endregion
	}
}