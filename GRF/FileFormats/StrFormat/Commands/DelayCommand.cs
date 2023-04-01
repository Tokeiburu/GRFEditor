using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class DelayCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private bool _isSet = false;
		private float _delay;
		private float _oldDelay;

		public DelayCommand(int layerIdx, int frameIdx, float delay) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_delay = delay;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Changed delay to " + (1 / _delay).ToString("0.##");
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldDelay = str[_layerIdx, _frameIdx].Delay;
				_isSet = true;
			}

			str[_layerIdx, _frameIdx].Delay = _delay;
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].Delay = _oldDelay;
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as DelayCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as DelayCommand;
			if (cmd != null) {
				_delay = cmd._delay;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldDelay == _delay;
		}
	}
}
