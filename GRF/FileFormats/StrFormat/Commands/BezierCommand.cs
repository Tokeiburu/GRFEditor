using System;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class BezierCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private bool _isSet = false;
		private readonly float[] _vertices = new float[4];
		private readonly float[] _oldVertices = new float[4];

		public int LayerIdx {
			get { return _layerIdx; }
		}

		public BezierCommand(int layerIdx, int frameIdx, float[] vertices) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;

			for (int i = 0; i < 4; i++) {
				_vertices[i] = vertices[i];
			}
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Bezier curve changed";
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				for (int i = 0; i < 4; i++) {
					_oldVertices[i] = str[_layerIdx, _frameIdx].Bezier[i];
				}

				_isSet = true;
			}

			for (int i = 0; i < 4; i++) {
				str[_layerIdx, _frameIdx].Bezier[i] = _vertices[i];
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 4; i++) {
				str[_layerIdx, _frameIdx].Bezier[i] = _oldVertices[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as BezierCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as BezierCommand;
			if (cmd != null) {
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
