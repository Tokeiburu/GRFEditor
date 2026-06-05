namespace GRF.FileFormats.StrFormat.Commands {
	public class SetInterpolatedCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private bool _isInterpolated;
		private bool _oldIsInterpolated;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public SetInterpolatedCommand(int layerIndex, int frameIndex, bool isInterpolated) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;
			_isInterpolated = isInterpolated;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Changed interpolation";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldIsInterpolated = str[_layerIndex, _keyIndex].IsInterpolated;
				_isSet = true;
			}

			str[_layerIndex, _keyIndex].IsInterpolated = _isInterpolated;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].IsInterpolated = _oldIsInterpolated;
		}
	}
}
