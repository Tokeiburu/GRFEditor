using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class MaxFrameCommand : IStrCommand, IAutoReverse {
		private bool _isSet = false;
		private int _maxFrame;
		private int _oldMaxFrame;

		public MaxFrameCommand(int maxFrame) {
			_maxFrame = maxFrame;
		}

		public string CommandDescription => $"Changed FPS to {_maxFrame}";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldMaxFrame = str.MaxKeyFrame;
				_isSet = true;
			}

			str.MaxKeyFrame = _maxFrame;
			str.InvalidateIndex();
		}

		public void Undo(Str str) {
			str.MaxKeyFrame = _oldMaxFrame;
			str.InvalidateIndex();
		}

		public bool CanCombine(ICombinableCommand command) {
			return true;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is MaxFrameCommand cmd) {
				_maxFrame = cmd._maxFrame;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldMaxFrame == _maxFrame;
		}
	}
}
