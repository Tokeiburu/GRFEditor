using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class BlendCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private int _blend;
		private int _oldBlend;
		private bool _isSet = false;
		private readonly int _mode;

		public BlendCommand(int layerIdx, int frameIdx, int blend, int mode) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_blend = blend;
			_mode = mode;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] " + (_mode == 0? "Str" : "Dst") + " blend option changed to " + _blend;
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				if (_mode == 0) {
					_oldBlend = str[_layerIdx, _frameIdx].SourceAlpha;
				}
				else {
					_oldBlend = str[_layerIdx, _frameIdx].DestinationAlpha;
				}

				_isSet = true;
			}

			if (_mode == 0) {
				str[_layerIdx, _frameIdx].SourceAlpha = _blend;
			}
			else {
				str[_layerIdx, _frameIdx].DestinationAlpha = _blend;
			}
		}

		public void Undo(Str str) {
			if (_mode == 0) {
				str[_layerIdx, _frameIdx].SourceAlpha = _oldBlend;
			}
			else {
				str[_layerIdx, _frameIdx].DestinationAlpha = _oldBlend;
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as BlendCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._mode == _mode &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as BlendCommand;
			if (cmd != null) {
				_blend = cmd._blend;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldBlend == _blend;
		}
	}
}
