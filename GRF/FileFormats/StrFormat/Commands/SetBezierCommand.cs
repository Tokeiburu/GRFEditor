using System;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class SetBezierCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private readonly float[] _vertices = new float[4];
		private readonly float[] _oldVertices = new float[4];

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public SetBezierCommand(int layerIdx, int keyIndex, float[] vertices) {
			_layerIndex = layerIdx;
			_keyIndex = keyIndex;

			for (int i = 0; i < 4; i++) {
				_vertices[i] = vertices[i];
			}
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Bezier curve changed";

		public void Execute(Str str) {
			if (!_isSet) {
				for (int i = 0; i < 4; i++) {
					_oldVertices[i] = str[_layerIndex, _keyIndex].BezierPositions[i];
				}

				_isSet = true;
			}

			for (int i = 0; i < 4; i++) {
				str[_layerIndex, _keyIndex].BezierPositions[i] = _vertices[i];
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 4; i++) {
				str[_layerIndex, _keyIndex].BezierPositions[i] = _oldVertices[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is SetBezierCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is SetBezierCommand cmd) {
				for (int i = 0; i < 4; i++) {
					_vertices[i] = cmd._vertices[i];
				}

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			for (int i = 0; i < 4; i++) {
				if (Math.Abs(_oldVertices[i] - _vertices[i]) > 0.01)
					return false;
			}

			return true;
		}
	}
}
