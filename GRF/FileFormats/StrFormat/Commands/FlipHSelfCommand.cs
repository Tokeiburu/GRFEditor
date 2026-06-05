namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipHSelfCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public FlipHSelfCommand(int layerIndex, int frameIndex) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Flip horizontal (self)";

		public void Execute(Str str) {
			// Self transformation
			str[_layerIndex, _keyIndex].Uv[0] += str[_layerIndex, _keyIndex].Uv[2];
			str[_layerIndex, _keyIndex].Uv[2] = -str[_layerIndex, _keyIndex].Uv[2];
		}

		public void Undo(Str str) {
			// Self transformation
			str[_layerIndex, _keyIndex].Uv[0] += str[_layerIndex, _keyIndex].Uv[2];
			str[_layerIndex, _keyIndex].Uv[2] = -str[_layerIndex, _keyIndex].Uv[2];
		}
	}
}
