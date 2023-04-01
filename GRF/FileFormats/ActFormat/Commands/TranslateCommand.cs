namespace GRF.FileFormats.ActFormat.Commands {
	public class TranslateCommand : IActCommand {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly int _mode;
		private readonly int _offsetX;
		private readonly int _offsetY;

		public TranslateCommand(int offsetX, int offsetY) {
			_offsetX = offsetX;
			_offsetY = offsetY;
			_mode = 0;
		}

		public TranslateCommand(int actIndex, int offsetX, int offsetY) {
			_actionIndex = actIndex;
			_offsetX = offsetX;
			_offsetY = offsetY;
			_mode = 1;
		}

		public TranslateCommand(int actIndex, int frameIndex, int offsetX, int offsetY) {
			_actionIndex = actIndex;
			_frameIndex = frameIndex;
			_offsetX = offsetX;
			_offsetY = offsetY;
			_mode = 2;
		}

		public TranslateCommand(int actIndex, int frameIndex, int layerIndex, int offsetX, int offsetY) {
			_actionIndex = actIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_offsetX = offsetX;
			_offsetY = offsetY;
			_mode = 3;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_mode) {
				case 0:
					act.Translate(_offsetX, _offsetY);
					break;
				case 1:
					act[_actionIndex].Translate(_offsetX, _offsetY);
					break;
				case 2:
					act[_actionIndex, _frameIndex].Translate(_offsetX, _offsetY);
					break;
				case 3:
					act[_actionIndex, _frameIndex, _layerIndex].Translate(_offsetX, _offsetY);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_mode) {
				case 0:
					act.Translate(-_offsetX, -_offsetY);
					break;
				case 1:
					act[_actionIndex].Translate(-_offsetX, -_offsetY);
					break;
				case 2:
					act[_actionIndex, _frameIndex].Translate(-_offsetX, -_offsetY);
					break;
				case 3:
					act[_actionIndex, _frameIndex, _layerIndex].Translate(-_offsetX, -_offsetY);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_mode) {
					case 0:
						return "Translate (" + _offsetX + ", " + _offsetY + ")";
					case 1:
						return CommandsHolder.GetId(_actionIndex) + " Translate (" + _offsetX + ", " + _offsetY + ")";
					case 2:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Translate (" + _offsetX + ", " + _offsetY + ")";
					case 3:
						return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Translate (" + _offsetX + ", " + _offsetY + ")";
				}

				return "";
			}
		}

		#endregion
	}
}