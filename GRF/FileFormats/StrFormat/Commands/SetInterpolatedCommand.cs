namespace GRF.FileFormats.StrFormat.Commands {
	public class SetInterpolatedCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private bool _isSet = false;
		private bool _isInterpolated;
		private bool _oldIsInterpolated;

		public int LayerIdx {
			get { return _layerIdx; }
		}

		public SetInterpolatedCommand(int layerIdx, int frameIdx, bool isInterpolated) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_isInterpolated = isInterpolated;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Changed interpolation";
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldIsInterpolated = str[_layerIdx, _frameIdx].IsInterpolated;
				_isSet = true;
			}

			str[_layerIdx, _frameIdx].IsInterpolated = _isInterpolated;
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].IsInterpolated = _oldIsInterpolated;
		}
	}
}
