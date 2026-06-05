using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class BlendCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private int _blend;
		private int _oldBlend;
		private bool _isSet = false;
		private readonly int _mode;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public BlendCommand(int layerIndex, int keyIndex, int blend, int mode) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_blend = blend;
			_mode = mode;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] {(_mode == 0 ? "Str" : "Dst")} blend option changed to {_blend}";

		public void Execute(Str str) {
			if (!_isSet) {
				if (_mode == 0) {
					_oldBlend = str[_layerIndex, _keyIndex].SourceAlpha;
				}
				else {
					_oldBlend = str[_layerIndex, _keyIndex].DestinationAlpha;
				}

				_isSet = true;
			}

			if (_mode == 0) {
				str[_layerIndex, _keyIndex].SourceAlpha = _blend;
			}
			else {
				str[_layerIndex, _keyIndex].DestinationAlpha = _blend;
			}
		}

		public void Undo(Str str) {
			if (_mode == 0) {
				str[_layerIndex, _keyIndex].SourceAlpha = _oldBlend;
			}
			else {
				str[_layerIndex, _keyIndex].DestinationAlpha = _oldBlend;
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is BlendCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._mode == _mode &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is BlendCommand cmd) {
				_blend = cmd._blend;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldBlend == _blend;
		}
	}
}
