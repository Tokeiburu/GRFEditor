using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class ColorCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private float[] _colors = new float[4];
		private float[] _oldColors = null;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public ColorCommand(int layerIndex, int keyIndex, float r, float g, float b, float a) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_colors[0] = r;
			_colors[1] = g;
			_colors[2] = b;
			_colors[3] = a;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Color changed to " + new GrfColor((byte)_colors[3], (byte)_colors[0], (byte)_colors[1], (byte)_colors[2]);

		public void Execute(Str str) {
			if (_oldColors == null) {
				_oldColors = new float[4];

				for (int i = 0; i < 4; i++) {
					_oldColors[i] = str.Layers[_layerIndex].KeyFrames[_keyIndex].Color[i];
				}
			}

			for (int i = 0; i < 4; i++) {
				str.Layers[_layerIndex].KeyFrames[_keyIndex].Color[i] = _colors[i];
			}
		}

		public void Undo(Str str) {
			for (int i = 0; i < 4; i++) {
				str.Layers[_layerIndex].KeyFrames[_keyIndex].Color[i] = _oldColors[i];
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is ColorCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is ColorCommand cmd) {
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
