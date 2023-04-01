namespace GRF.FileFormats.ActFormat.Commands {
	public class ScaleCommand : IActCommand {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly int _mode;
		private readonly float _scaleX;
		private readonly float _scaleY;

		public ScaleCommand(float scaleX, float scaleY) {
			_scaleX = scaleX;
			_scaleY = scaleY;
			_mode = 0;
		}

		public ScaleCommand(int actIndex, float scaleX, float scaleY) {
			_actionIndex = actIndex;
			_scaleX = scaleX;
			_scaleY = scaleY;
			_mode = 1;
		}

		public ScaleCommand(int actIndex, int frameIndex, float scaleX, float scaleY) {
			_actionIndex = actIndex;
			_frameIndex = frameIndex;
			_scaleX = scaleX;
			_scaleY = scaleY;
			_mode = 2;
		}

		public ScaleCommand(int actIndex, int frameIndex, int layerIndex, float scaleX, float scaleY) {
			_actionIndex = actIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_scaleX = scaleX;
			_scaleY = scaleY;
			_mode = 3;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_mode) {
				case 0:
					act.Scale(_scaleX, _scaleY);
					break;
				case 1:
					act[_actionIndex].Scale(_scaleX, _scaleY);
					break;
				case 2:
					act[_actionIndex, _frameIndex].Scale(_scaleX, _scaleY);
					break;
				case 3:
					act[_actionIndex, _frameIndex, _layerIndex].Scale(_scaleX, _scaleY);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_mode) {
				case 0:
					act.Scale(1f / _scaleX, 1f / _scaleY);
					break;
				case 1:
					act[_actionIndex].Scale(1f / _scaleX, 1f / _scaleY);
					break;
				case 2:
					act[_actionIndex, _frameIndex].Scale(1f / _scaleX, 1f / _scaleY);
					break;
				case 3:
					act[_actionIndex, _frameIndex, _layerIndex].Scale(1f / _scaleX, 1f / _scaleY);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_mode) {
					case 0:
						return "Scale value multiplied by (" + _scaleX + ", " + _scaleY + ")";
					case 1:
						return CommandsHolder.GetId(_actionIndex) + " Scale value multiplied by (" + _scaleX + ", " + _scaleY + ")";
					case 2:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Scale value multiplied by (" + _scaleX + ", " + _scaleY + ")";
					case 3:
						return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Scale value multiplied by (" + _scaleX + ", " + _scaleY + ")";
				}

				return "";
			}
		}

		#endregion
	}
}