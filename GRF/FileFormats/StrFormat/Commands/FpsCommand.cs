using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class FpsCommand : IStrCommand, IAutoReverse {
		private bool _isSet = false;
		private int _fps;
		private int _oldFps;

		public FpsCommand(int fps) {
			_fps = fps;
		}

		public string CommandDescription {
			get {
				return "Changed FPS to " + _fps;
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldFps = str.Fps;
				_isSet = true;
			}

			str.Fps = _fps;
		}

		public void Undo(Str str) {
			str.Fps = _oldFps;
		}

		public bool CanCombine(ICombinableCommand command) {
			return true;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as FpsCommand;
			if (cmd != null) {
				_fps = cmd._fps;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldFps == _fps;
		}
	}
}
