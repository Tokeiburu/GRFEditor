using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class RotateCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private float _angle;
		private float _oldAngle = float.NaN;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public RotateCommand(int layerIndex, int keyIndex, float angle) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_angle = angle;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Angle changed to {_angle}";

		public void Execute(Str str) {
			if (float.IsNaN(_oldAngle)) {
				_oldAngle = str[_layerIndex, _keyIndex].Angle;
			}

			str[_layerIndex, _keyIndex].Angle = _angle;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].Angle = _oldAngle;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is RotateCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is RotateCommand cmd) {
				_angle = cmd._angle;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldAngle == _angle;
		}
	}
}
