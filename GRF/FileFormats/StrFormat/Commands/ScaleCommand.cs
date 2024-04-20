using System;
using GRF.Graphics;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class ScaleCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private float _x;
		private float _y;
		private bool _isSet = false;
		private float[] _vertices = new float[8];
		private float[] _newVertices = new float[8];

		public ScaleCommand(int layerIdx, int frameIdx, float x, float y) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_x = x;
			_y = y;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Scale changed by (" + String.Format("{0:0.00}", _x) + ", " + String.Format("{0:0.00}", _y) + ")";
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				for (int i = 0; i < 8; i++) {
					_vertices[i] = str[_layerIdx, _frameIdx].Xy[i];
				}

				_isSet = true;
			}

			float x = 0;
			float y = 0;
			TkVector2[] points = new TkVector2[4];

			for (int i = 0; i < 4; i++) {
				x += str[_layerIdx, _frameIdx].Xy[i];
				y += str[_layerIdx, _frameIdx].Xy[i + 4];

				points[i] = new TkVector2(str[_layerIdx, _frameIdx].Xy[i], str[_layerIdx, _frameIdx].Xy[i + 4]);
			}

			x /= 4;
			y /= 4;

			TkVector2 m = new TkVector2(x, y);

			for (int i = 0; i < 4; i++) {
				TkVector2 p = (points[i] - m);
				p.X *= _x;
				p.Y *= _y;

				p += m;
				str[_layerIdx, _frameIdx].Xy[i] = p.X;
				str[_layerIdx, _frameIdx].Xy[i + 4] = p.Y;
				_newVertices[i] = p.X;
				_newVertices[i + 4] = p.Y;
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 8; i++) {
				str[_layerIdx, _frameIdx].Xy[i] = _vertices[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as ScaleCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ScaleCommand;
			if (cmd != null) {
				for (int i = 0; i < 8; i++) {
					_vertices[i] = cmd._vertices[i];
				}
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			for (int i = 0; i < 8; i++) {
				if (_vertices[i] != _newVertices[i]) {
					return false;
				}
			}

			return true;
		}
	}
}
