using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.Image;

namespace GRF.FileFormats.SprFormat.Commands {
	public class Flip : IActCommand {
		private readonly int _absoluteIndex;
		private readonly FlipDirection _dir;

		public Flip(int absoluteIndex, FlipDirection dir) {
			_absoluteIndex = absoluteIndex;
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
	}
}