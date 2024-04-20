using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class OffsetBiasCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private float _bias;
		private float _oldBias = float.NaN;

		public int LayerIdx {
			get { return _layerIdx; }
		}

		public OffsetBiasCommand(int layerIdx, int frameIdx, float bias) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_bias = bias;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Offset bias changed to " + _bias;
			}
		}

		public void Execute(Str str) {
			if (float.IsNaN(_oldBias)) {
				_oldBias = str[_layerIdx, _frameIdx].OffsetBias;
			}

			str[_layerIdx, _frameIdx].OffsetBias = _bias;
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].OffsetBias = _oldBias;
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as OffsetBiasCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as OffsetBiasCommand;
			if (cmd != null) {
				_bias = cmd._bias;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldBias == _bias;
		}
	}
}
