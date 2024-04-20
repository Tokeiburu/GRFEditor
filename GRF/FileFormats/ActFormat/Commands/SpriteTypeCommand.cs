using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class SpriteTypeCommand : IActCommand, IAutoReverse {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private int _oldSpriteType;
		private int _spriteType;

		public SpriteTypeCommand(int actionIndex, int frameIndex, int layerIndex, int spriteType) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_spriteType = spriteType;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			_oldSpriteType = act[_actionIndex, _frameIndex, _layerIndex].SpriteTypeInt;
			act[_actionIndex, _frameIndex, _layerIndex].SpriteTypeInt = _spriteType;
		}

		public void Undo(Act act) {
			act[_actionIndex, _frameIndex, _layerIndex].SpriteTypeInt = _oldSpriteType;
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Sprite type changed " + _spriteType; }
		}

		#endregion

		#region IAutoReverse Members

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as SpriteTypeCommand;
			if (cmd != null) {
				if (cmd._actionIndex == _actionIndex &&
				    cmd._frameIndex == _layerIndex &&
				    cmd._layerIndex == _layerIndex) {
					return true;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as SpriteTypeCommand;
			if (cmd != null) {
				cmd._spriteType = _spriteType;
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _spriteType == _oldSpriteType;
		}

		#endregion
	}
}