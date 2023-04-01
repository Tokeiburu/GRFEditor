using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class RotateCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private float _angle;
		private float _oldAngle = float.NaN;

		public RotateCommand(int layerIdx, int frameIdx, float angle) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_angle = angle;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Angle changed to " + _angle;
			}
		}

		public void Execute(Str str) {
			if (float.IsNaN(_oldAngle)) {
				_oldAngle = str[_layerIdx, _frameIdx].Angle;
			}

			str[_layerIdx, _frameIdx].Angle = _angle;
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].Angle = _oldAngle;
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as RotateCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as RotateCommand;
			if (cmd != null) {
				_angle = cmd._angle;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldAngle == _angle;
		}
	}
}
