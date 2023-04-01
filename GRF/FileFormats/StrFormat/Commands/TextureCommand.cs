using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class TextureCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private bool _isSet = false;
		private int _textureId;
		private int _oldTextureId;

		public TextureCommand(int layerIdx, int frameIdx, int textureId) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_textureId = textureId;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Changed texture to " + _textureId;
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldTextureId = (int)str[_layerIdx, _frameIdx].TextureIndex;
				_isSet = true;
			}

			str[_layerIdx, _frameIdx].TextureIndex = _textureId;
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].TextureIndex = _oldTextureId;
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as TextureCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as TextureCommand;
			if (cmd != null) {
				_textureId = cmd._textureId;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldTextureId == _textureId;
		}
	}
}
