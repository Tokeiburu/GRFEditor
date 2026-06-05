using System;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class TextCoordsCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private float[] _vertices = new float[8];
		private float[] _oldVertices = new float[8];

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public TextCoordsCommand(int layerIndex, int frameIndex, float[] vertices) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;

			for (int i = 0; i < 8; i++) {
				_vertices[i] = vertices[i];
			}
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Position changed";

		public void Execute(Str str) {
			if (!_isSet) {
				for (int i = 0; i < 8; i++) {
					_oldVertices[i] = str[_layerIndex, _keyIndex].Uv[i];
				}

				_isSet = true;
			}

			for (int i = 0; i < 8; i++) {
				str[_layerIndex, _keyIndex].Uv[i] = _vertices[i];
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 8; i++) {
				str[_layerIndex, _keyIndex].Uv[i] = _oldVertices[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is TextCoordsCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is TextCoordsCommand cmd) {
				for (int i = 0; i < 8; i++) {
					_vertices[i] = cmd._vertices[i];
				}

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			for (int i = 0; i < 8; i++) {
				if (Math.Abs(_oldVertices[i] - _vertices[i]) > 0.01)
					return false;
			}

			return true;
		}
	}
}
