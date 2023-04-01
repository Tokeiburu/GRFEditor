namespace GRF.FileFormats.ActFormat.Commands {
	public class RotateCommand : IActCommand {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly int _mode;
		private readonly int _rotate;

		public RotateCommand(int rotate) {
			_rotate = rotate;
			_mode = 0;
		}

		public RotateCommand(int rotate, int actionIndex) {
			_rotate = rotate;
			_actionIndex = actionIndex;
			_mode = 1;
		}

		public RotateCommand(int rotate, int actionIndex, int frameIndex) {
			_rotate = rotate;
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_mode = 2;
		}

		public RotateCommand(int rotate, int actionIndex, int frameIndex, int layerIndex) {
			_rotate = rotate;
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_mode = 3;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_mode) {
				case 0:
					act.Rotate(_rotate);
					break;
				case 1:
					act[_actionIndex].Rotate(_rotate);
					break;
				case 2:
					act[_actionIndex, _frameIndex].Rotate(_rotate);
					break;
				case 3:
					act[_actionIndex, _frameIndex, _layerIndex].Rotate(_rotate);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_mode) {
				case 0:
					act.Rotate(-_rotate);
					break;
				case 1:
					act[_actionIndex].Rotate(-_rotate);
					break;
				case 2:
					act[_actionIndex, _frameIndex].Rotate(-_rotate);
					break;
				case 3:
					act[_actionIndex, _frameIndex, _layerIndex].Rotate(-_rotate);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_mode) {
					case 0:
						return "Rotation value changed " + _rotate;
					case 1:
						return CommandsHolder.GetId(_actionIndex) + " Rotation value changed " + _rotate;
					case 2:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Rotation value changed " + _rotate;
					case 3:
						return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Rotation value changed " + _rotate;
				}

				return "";
			}
		}

		#endregion
	}
}