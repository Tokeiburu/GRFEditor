using System;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class SetPositionsCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private float[] _vertices = new float[8];
		private float[] _oldVertices = new float[8];
		private int _changed;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public SetPositionsCommand(int layerIndex, int keyIndex, float[] vertices) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;

			for (int i = 0; i < 8; i++) {
				_vertices[i] = vertices[i];
			}
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Positions changed";

		public void Execute(Str str) {
			if (!_isSet) {
				for (int i = 0; i < 4; i++) {
					if (Math.Abs(str[_layerIndex, _keyIndex].Positions[i] - _vertices[i]) > 0.01 ||
						Math.Abs(str[_layerIndex, _keyIndex].Positions[i + 4] - _vertices[i + 4]) > 0.01) {
						_changed = i;
						break;
					}
				}

				for (int i = 0; i < 8; i++) {
					_oldVertices[i] = str[_layerIndex, _keyIndex].Positions[i];
				}

				_isSet = true;
			}

			for (int i = 0; i < 8; i++) {
				str[_layerIndex, _keyIndex].Positions[i] = _vertices[i];
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 8; i++) {
				str[_layerIndex, _keyIndex].Positions[i] = _oldVertices[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is SetPositionsCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex &&
					cmd._changed == _changed)
					return false;

				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is SetPositionsCommand cmd) {
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
