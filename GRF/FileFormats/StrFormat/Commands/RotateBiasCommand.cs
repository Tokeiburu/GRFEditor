using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class RotateBiasCommand : IStrCommand, IAutoReverse, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private float _bias;
		private float _oldBias = float.NaN;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public RotateBiasCommand(int layerIndex, int keyIndex, float bias) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_bias = bias;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Angle bias changed to {_bias}";

		public void Execute(Str str) {
			if (float.IsNaN(_oldBias)) {
				_oldBias = str[_layerIndex, _keyIndex].AngleBias;
			}

			str[_layerIndex, _keyIndex].AngleBias = _bias;
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].AngleBias = _oldBias;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is RotateBiasCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is RotateBiasCommand cmd) {
				_bias = cmd._bias;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldBias == _bias;
		}
	}
}
