using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class ScaleBiasCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private float _bias;
		private float _oldBias = float.NaN;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public ScaleBiasCommand(int layerIndex, int keyIndex, float bias) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_bias = bias;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Scale bias changed to {_bias}";

		public void Execute(Str str) {
			if (float.IsNaN(_oldBias)) {
				_oldBias = str[_layerIndex, _keyIndex].ScaleBias;
			}

			str[_layerIndex, _keyIndex].ScaleBias = _bias;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].ScaleBias = _oldBias;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is ScaleBiasCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is ScaleBiasCommand cmd) {
				_bias = cmd._bias;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldBias == _bias;
		}
	}
}
