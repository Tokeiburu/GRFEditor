using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class AnimationTypeCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private int _animType;
		private int _oldAnimType;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public AnimationTypeCommand(int layerIndex, int keyIndex, int animType) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_animType = animType;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Changed anim type to {_animType}";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldAnimType = str[_layerIndex, _keyIndex].AnimationType;
				_isSet = true;
			}

			str[_layerIndex, _keyIndex].AnimationType = _animType;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].AnimationType = _oldAnimType;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is AnimationTypeCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is AnimationTypeCommand cmd) {
				_animType = cmd._animType;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldAnimType == _animType;
		}
	}
}
