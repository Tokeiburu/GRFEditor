using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class AnimationTypeCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private bool _isSet = false;
		private int _animType;
		private int _oldAnimType;

		public int LayerIdx {
			get { return _layerIdx; }
		}

		public AnimationTypeCommand(int layerIdx, int frameIdx, int animType) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_animType = animType;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Changed anim type to " + _animType;
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldAnimType = str[_layerIdx, _frameIdx].AnimationType;
				_isSet = true;
			}

			str[_layerIdx, _frameIdx].AnimationType = _animType;
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].AnimationType = _oldAnimType;
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as AnimationTypeCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as AnimationTypeCommand;
			if (cmd != null) {
				_animType = cmd._animType;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldAnimType == _animType;
		}
	}
}
