using System.Collections.Generic;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class TexturesCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private readonly List<string> _textures = new List<string>();
		private readonly List<string> _oldTextures = new List<string>();
		private bool _isSet = false;
		private int _newHash;
		private int _oldHash;

		public TexturesCommand(int layerIdx, int frameIdx, List<string> textures) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_textures = textures;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Layer textures changed";
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldHash = str.Layers[_layerIdx].TexturesHash;
				_oldTextures.AddRange(str.Layers[_layerIdx].TextureNames);
				_isSet = true;
			}

			_newHash = StrLayer.GenerateTexturesHash(_textures);
			str[_layerIdx].TextureNames.Clear();
			str[_layerIdx].TextureNames.AddRange(_textures);
			str.Layers[_layerIdx].TexturesHash = _newHash;
		}

		public void Undo(Str str) {
			str[_layerIdx].TextureNames.Clear();
			str[_layerIdx].TextureNames.AddRange(_oldTextures);
			str.Layers[_layerIdx].TexturesHash = _oldHash;
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as TexturesCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as TexturesCommand;
			if (cmd != null) {
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
