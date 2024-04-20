using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class SpriteIdCommand : IActCommand, IAutoReverse {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private Layer _oldLayer;
		private int _spriteId;

		public SpriteIdCommand(int actionIndex, int frameIndex, int layerIndex, int spriteId) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_spriteId = spriteId;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			Layer layer = act[_actionIndex, _frameIndex, _layerIndex];

			if (_oldLayer == null) {
				_oldLayer = new Layer(layer);
			}

			if (act.Sprite != null && _spriteId > -1) {
				if (layer.IsIndexed8() && _spriteId < act.Sprite.NumberOfIndexed8Images ||
				    layer.IsBgra32() && _spriteId < act.Sprite.NumberOfBgra32Images) {
					GrfImage image = act.Sprite.GetImage(_spriteId, layer.SpriteType);
					layer.Width = image.Width;
					layer.Height = image.Height;
				}
				else {
					layer.Width = 0;
					layer.Height = 0;
				}
			}
			else {
				layer.Width = 0;
				layer.Height = 0;
			}

			layer.SpriteIndex = _spriteId;
		}

		public void Undo(Act act) {
			if (_oldLayer == null) return;
			act[_actionIndex, _frameIndex, _layerIndex] = new Layer(_oldLayer);
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Sprite id changed " + _spriteId; }
		}

		#endregion

		#region IAutoReverse Members

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as SpriteIdCommand;
			if (cmd != null) {
				if (cmd._actionIndex == _actionIndex &&
				    cmd._frameIndex == _frameIndex &&
				    cmd._layerIndex == _layerIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as SpriteIdCommand;
			if (cmd != null) {
				_spriteId = cmd._spriteId;
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldLayer != null && _spriteId == _oldLayer.SpriteIndex;
		}

		#endregion
	}
}