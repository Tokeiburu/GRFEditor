using System;
using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CoordsCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private bool _isSet = false;
		private float[] _vertices = new float[8];
		private float[] _oldVertices = new float[8];
		private int _changed;
		private int _point;
		private int _mode;
		private float _offset;
		private float _oldOffset;

		public CoordsCommand(int layerIdx, int frameIdx, int point, float offset) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;

			_point = point;
			_offset = offset;
			_mode = 1;
		}

		public CoordsCommand(int layerIdx, int frameIdx, float[] vertices) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;

			for (int i = 0; i < 8; i++) {
				_vertices[i] = vertices[i];
			}
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Position changed";
			}
		}

		public void Execute(Str str) {
			if (_mode == 1) {
				if (!_isSet) {
					_oldOffset = str[_layerIdx, _frameIdx].Xy[_point];
					_isSet = true;
				}

				str[_layerIdx, _frameIdx].Xy[_point] = _offset;
			}
			else {
				if (!_isSet) {
					for (int i = 0; i < 4; i++) {
						if (Math.Abs(str[_layerIdx, _frameIdx].Xy[i] - _vertices[i]) > 0.01 ||
							Math.Abs(str[_layerIdx, _frameIdx].Xy[i + 4] - _vertices[i + 4]) > 0.01) {
							_changed = i;
							break;
						}
					}

					for (int i = 0; i < 8; i++) {
						_oldVertices[i] = str[_layerIdx, _frameIdx].Xy[i];
					}

					_isSet = true;
				}

				for (int i = 0; i < 8; i++) {
					str[_layerIdx, _frameIdx].Xy[i] = _vertices[i];
				}
			}
		}

		public void Undo(Str str) {
			if (_mode == 1) {
				str[_layerIdx, _frameIdx].Xy[_point] = _oldOffset;
			}
			else {
				for (int i = 0; i < 8; i++) {
					str[_layerIdx, _frameIdx].Xy[i] = _oldVertices[i];
				}
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as CoordsCommand;
			if (cmd != null) {
				if (cmd._mode != _mode)
					return false;

				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx &&
					cmd._changed == _changed &&
					_mode == 0)
					return false;

				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx &&
					cmd._point == _point &&
					_mode == 1)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as CoordsCommand;
			if (cmd != null) {
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
