using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class DelayCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private float _delay;
		private float _oldDelay;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public DelayCommand(int layerIndex, int keyIndex, float delay) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_delay = delay;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Changed delay to {(1 / _delay):0.##}";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldDelay = str[_layerIndex, _keyIndex].Delay;
				_isSet = true;
			}

			str[_layerIndex, _keyIndex].Delay = _delay;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].Delay = _oldDelay;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is DelayCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is DelayCommand cmd) {
				_delay = cmd._delay;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldDelay == _delay;
		}
	}
}
