using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class TextureCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private int _textureId;
		private int _oldTextureId;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public TextureCommand(int layerIndex, int frameIndex, int textureId) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;
			_textureId = textureId;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Changed texture to {_textureId}";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldTextureId = (int)str[_layerIndex, _keyIndex].TextureIndex;
				_isSet = true;
			}

			str[_layerIndex, _keyIndex].TextureIndex = _textureId;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].TextureIndex = _oldTextureId;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is TextureCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is TextureCommand cmd) {
				_textureId = cmd._textureId;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldTextureId == _textureId;
		}
	}
}
