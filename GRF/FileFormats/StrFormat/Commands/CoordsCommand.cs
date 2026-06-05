using System;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CoordsCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private bool _isSet = false;
		private float[] _vertices = new float[8];
		private float[] _oldVertices = new float[8];
		private int _changed;
		private int _point;
		private int _mode;
		private float _offset;
		private float _oldOffset;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public CoordsCommand(int layerIndex, int keyIndex, int point, float offset) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;

			_point = point;
			_offset = offset;
			_mode = 1;
		}

		public CoordsCommand(int layerIndex, int keyIndex, float[] vertices) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;

			for (int i = 0; i < 8; i++) {
				_vertices[i] = vertices[i];
			}
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] UV changed";

		public void Execute(Str str) {
			if (_mode == 1) {
				if (!_isSet) {
					_oldOffset = str[_layerIndex, _keyIndex].Xy[_point];
					_isSet = true;
				}

				str[_layerIndex, _keyIndex].Xy[_point] = _offset;
			}
			else {
				if (!_isSet) {
					for (int i = 0; i < 4; i++) {
						if (Math.Abs(str[_layerIndex, _keyIndex].Xy[i] - _vertices[i]) > 0.01 ||
							Math.Abs(str[_layerIndex, _keyIndex].Xy[i + 4] - _vertices[i + 4]) > 0.01) {
							_changed = i;
							break;
						}
					}

					for (int i = 0; i < 8; i++) {
						_oldVertices[i] = str[_layerIndex, _keyIndex].Xy[i];
					}

					_isSet = true;
				}

				for (int i = 0; i < 8; i++) {
					str[_layerIndex, _keyIndex].Xy[i] = _vertices[i];
				}
			}
		}

		public void Undo(Str str) {
			if (_mode == 1) {
				str[_layerIndex, _keyIndex].Xy[_point] = _oldOffset;
			}
			else {
				for (int i = 0; i < 8; i++) {
					str[_layerIndex, _keyIndex].Xy[i] = _oldVertices[i];
				}
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is CoordsCommand cmd) {
				if (cmd._mode != _mode)
					return false;

				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex &&
					cmd._changed == _changed &&
					_mode == 0)
					return false;

				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex &&
					cmd._point == _point &&
					_mode == 1)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is CoordsCommand cmd) {
				if (_mode == 1) {
					_offset = cmd._offset;
					_point = cmd._point;
				}
				else {
					for (int i = 0; i < 8; i++) {
						_vertices[i] = cmd._vertices[i];
					}
				}

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			if (_mode == 1) {
				if (Math.Abs(_offset - _oldOffset) > 0.01)
					return false;
			}
			else {
				for (int i = 0; i < 8; i++) {
					if (Math.Abs(_oldVertices[i] - _vertices[i]) > 0.01)
						return false;
				}
			}

			return true;
		}
	}
}
