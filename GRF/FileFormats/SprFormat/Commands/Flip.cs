using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.FileFormats.SprFormat.Builder;
using GRF.Image;

namespace GRF.FileFormats.SprFormat.Commands {
	public class Flip : ICommand, IActCommand {
		private readonly int _absoluteIndex;
		private readonly FlipDirection _dir;
		private readonly SprBuilderImageView _view;

		public Flip(int absoluteIndex, FlipDirection dir) {
			_absoluteIndex = absoluteIndex;
			_dir = dir;
		}

		public Flip(SprBuilderImageView view, FlipDirection dir) {
			_view = view;
			_dir = dir;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			act.Sprite.Images[_absoluteIndex].Flip(_dir);
		}

		public void Undo(Act act) {
			act.Sprite.Images[_absoluteIndex].Flip(_dir);
		}

		public string CommandDescription {
			get { return "Flip sprite image " + _absoluteIndex; }
		}

		#endregion

		#region ICommand Members

		public void Execute(SprBuilderInterface sbi) {
			sbi.Flip(_view, _dir);
		}

		public void Undo(SprBuilderInterface sbi) {
			sbi.Flip(_view, _dir);
		}

		#endregion
	}
}