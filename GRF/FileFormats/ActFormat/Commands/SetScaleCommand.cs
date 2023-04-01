namespace GRF.FileFormats.ActFormat.Commands {
	public class SetScaleCommand : IActCommand {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly float _scaleX;
		private readonly float _scaleY;
		private float? _oldScaleX;
		private float? _oldScaleY;

		public SetScaleCommand(int actIndex, int frameIndex, int layerIndex, float scaleX, float scaleY) {
			_actionIndex = actIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_scaleX = scaleX;
			_scaleY = scaleY;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_oldScaleX == null)
				_oldScaleX = act[_actionIndex, _frameIndex, _layerIndex].ScaleX;

			act[_actionIndex, _frameIndex, _layerIndex].ScaleX = _scaleX;

			if (_oldScaleY == null)
				_oldScaleY = act[_actionIndex, _frameIndex, _layerIndex].ScaleY;

			act[_actionIndex, _frameIndex, _layerIndex].ScaleY = _scaleY;
		}

		public void Undo(Act act) {
			if (_oldScaleX == null || _oldScaleY == null) return;

			act[_actionIndex, _frameIndex, _layerIndex].ScaleX = _oldScaleX.Value;
			act[_actionIndex, _frameIndex, _layerIndex].ScaleY = _oldScaleY.Value;
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Scale value set to (" + _scaleX + ", " + _scaleY + ")"; }
		}

		#endregion
	}
}