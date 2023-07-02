using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class FlipCommand : IActCommand, IAutoReverse {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly int _offset;
		private readonly FlipDirection _direction;
		private readonly int _mode;

		public FlipCommand(int actionIndex, int frameIndex, int layerIndex, int offset, FlipDirection direction) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_offset = offset;
			_direction = direction;
			_mode = 0;
		}

		public FlipCommand(int actionIndex, int frameIndex, int offset, FlipDirection direction) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_offset = offset;
			_direction = direction;
			_mode = 1;
		}

		public FlipCommand(int actionIndex, int offset, FlipDirection direction) {
			_actionIndex = actionIndex;
			_offset = offset;
			_direction = direction;
			_mode = 2;
		}

		public FlipCommand(int offset, FlipDirection direction) {
			_offset = offset;
			_direction = direction;
			_mode = 3;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch(_mode) {
				case 0:
					_applyFlip(act[_actionIndex, _frameIndex, _layerIndex]);
					break;
				case 1:
					foreach (var layer in act[_actionIndex, _frameIndex]) {
						_applyFlip(layer);
					}

					break;
				case 2:
					foreach (var frame in act[_actionIndex]) {
						foreach (var layer in frame.Layers) {
							_applyFlip(layer);
						}
					}

					break;
				case 3:
					foreach (var layer in act.GetAllLayers()) {
						_applyFlip(layer);
					}

					break;
			}
		}

		public void Undo(Act act) {
			Execute(act);
		}

		private void _applyFlip(Layer layer) {
			if (_direction == FlipDirection.Vertical) {
				layer.OffsetX -= _offset;
				layer.OffsetX *= -1;
				int rotation = 360 - layer.Rotation;
				layer.Rotation = rotation < 0 ? rotation + 360 : rotation;
				layer.Mirror = !layer.Mirror;
			}
			else {
				layer.OffsetY -= _offset;
				layer.OffsetY *= -1;
				int rotation = 180 - layer.Rotation;
				layer.Rotation = rotation < 0 ? rotation + 360 : rotation;
				layer.Mirror = !layer.Mirror;
			}
		}

		public string CommandDescription {
			get {
				switch(_mode) {
					default:
						return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Mirror layer " + (_direction == FlipDirection.Horizontal ? "(horizontal)" : "(vertical)");
					case 1:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Mirror frame " + (_direction == FlipDirection.Horizontal ? "(horizontal)" : "(vertical)");
					case 2:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Mirror action " + (_direction == FlipDirection.Horizontal ? "(horizontal)" : "(vertical)");
					case 3:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Mirror act " + (_direction == FlipDirection.Horizontal ? "(horizontal)" : "(vertical)");
				}
			}
		}

		#endregion

		#region IAutoReverse Members

		public bool CanCombine(ICombinableCommand command) {
			if (_mode != 0) return false;

			var cmd = command as FlipCommand;
			if (cmd != null) {
				switch(_mode) {
					case 0:
						if (cmd._mode == _mode &&
						    cmd._actionIndex == _actionIndex &&
						    cmd._frameIndex == _frameIndex &&
						    cmd._layerIndex == _layerIndex)
							return true;
						return false;
					case 1:
						if (cmd._mode == _mode &&
						    cmd._actionIndex == _actionIndex &&
						    cmd._frameIndex == _frameIndex)
							return true;
						return false;
					case 2:
						if (cmd._mode == _mode &&
						    cmd._actionIndex == _actionIndex)
							return true;
						return false;
					case 3:
						if (cmd._mode == _mode)
							return true;
						return false;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as AnimationSpeedCommand;
			if (cmd != null) {
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return true;
		}

		#endregion
	}
}