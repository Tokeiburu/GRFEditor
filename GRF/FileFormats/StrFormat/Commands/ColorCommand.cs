using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class ColorCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private float[] _colors = new float[4];
		private float[] _oldColors = null;

		public ColorCommand(int layerIdx, int frameIdx, float r, float g, float b, float a) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_colors[0] = r;
			_colors[1] = g;
			_colors[2] = b;
			_colors[3] = a;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Color changed to " + new GrfColor((byte)_colors[3], (byte)_colors[0], (byte)_colors[1], (byte)_colors[2]);
			}
		}

		public void Execute(Str str) {
			if (_oldColors == null) {
				_oldColors = new float[4];

				for (int i = 0; i < 4; i++) {
					_oldColors[i] = str.Layers[_layerIdx].KeyFrames[_frameIdx].Color[i];
				}
			}

			for (int i = 0; i < 4; i++) {
				str.Layers[_layerIdx].KeyFrames[_frameIdx].Color[i] = _colors[i];
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 4; i++) {
				str.Layers[_layerIdx].KeyFrames[_frameIdx].Color[i] = _oldColors[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as ColorCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ColorCommand;
			if (cmd != null) {
				for (int i = 0; i < 4; i++) {
					_colors[i] = cmd._colors[i];
				}

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			for (int i = 0; i < 4; i++) {
				if (_colors[i] != _oldColors[i])
					return false;
			}

			return true;
		}
	}
}
