using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class AnimationSpeedCommand : IActCommand, IAutoReverse {
		private readonly int _actionIndex;
		private float? _oldSpeed;
		private float _speed;

		public AnimationSpeedCommand(int actionIndex, float speed) {
			_actionIndex = actionIndex;
			_speed = speed;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_oldSpeed == null)
				_oldSpeed = act[_actionIndex].AnimationSpeed;

			act[_actionIndex].AnimationSpeed = _speed;
		}

		public void Undo(Act act) {
			if (_oldSpeed == null)
				_oldSpeed = act[_actionIndex].AnimationSpeed;

			act[_actionIndex].AnimationSpeed = _oldSpeed.Value;
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex) + " Animation speed changed to " + (int) (_speed * 25f); }
		}

		#endregion

		#region IAutoReverse Members

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as AnimationSpeedCommand;
			if (cmd != null) {
				if (cmd._actionIndex == _actionIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as AnimationSpeedCommand;
			if (cmd != null) {
				_speed = cmd._speed;
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _speed == _oldSpeed;
		}

		#endregion
	}
}