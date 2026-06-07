using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class SetBlendCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private int _blend;
		private int _oldBlend;
		private bool _isSet = false;
		private readonly int _mode;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public SetBlendCommand(int layerIndex, int keyIndex, int blend, int mode) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_blend = blend;
			_mode = mode;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] {(_mode == 0 ? "Str" : "Dst")} blend option changed to {_blend}";

		public void Execute(Str str) {
			if (!_isSet) {
				if (_mode == 0) {
					_oldBlend = str[_layerIndex, _keyIndex].BlendSrc;
				}
				else {
					_oldBlend = str[_layerIndex, _keyIndex].BlendDst;
				}

				_isSet = true;
			}

			if (_mode == 0) {
				str[_layerIndex, _keyIndex].BlendSrc = _blend;
			}
			else {
				str[_layerIndex, _keyIndex].BlendDst = _blend;
			}
		}

		public void Undo(Str str) {
			if (_mode == 0) {
				str[_layerIndex, _keyIndex].BlendSrc = _oldBlend;
			}
			else {
				str[_layerIndex, _keyIndex].BlendDst = _oldBlend;
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is SetBlendCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._mode == _mode &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is SetBlendCommand cmd) {
				_blend = cmd._blend;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldBlend == _blend;
		}
	}
}
