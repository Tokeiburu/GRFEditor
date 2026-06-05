using System.Collections.Generic;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class TexturesCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIndex;
		private readonly List<string> _textures = new List<string>();
		private readonly List<string> _oldTextures = new List<string>();
		private bool _isSet = false;
		private int _newHash;
		private int _oldHash;

		public int LayerIndex => _layerIndex;

		public TexturesCommand(int layerIndex, List<string> textures) {
			_layerIndex = layerIndex;
			_textures = textures;
		}

		public string CommandDescription => $"[{_layerIndex}] Layer textures changed";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldHash = str.Layers[_layerIndex].TexturesHash;
				_oldTextures.AddRange(str.Layers[_layerIndex].TextureNames);
				_isSet = true;
			}

			_newHash = StrLayer.GenerateTexturesHash(_textures);
			str[_layerIndex].TextureNames.Clear();
			str[_layerIndex].TextureNames.AddRange(_textures);
			str.Layers[_layerIndex].TexturesHash = _newHash;
		}

		public void Undo(Str str) {
			str[_layerIndex].TextureNames.Clear();
			str[_layerIndex].TextureNames.AddRange(_oldTextures);
			str.Layers[_layerIndex].TexturesHash = _oldHash;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is TexturesCommand cmd) {
				if (cmd._layerIndex == _layerIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is TexturesCommand cmd) {
				_textures.Clear();
				_textures.AddRange(cmd._textures);
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			for (int i = 0; i < _textures.Count && i < _oldTextures.Count; i++) {
				if (_textures[i] != _oldTextures[i])
					return false;
			}

			return true;
		}
	}
}
