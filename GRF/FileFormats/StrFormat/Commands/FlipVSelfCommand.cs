namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipVSelfCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public FlipVSelfCommand(int layerIndex, int frameIndex) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Flip vertical (self)";

		public void Execute(Str str) {
			// Self transformation
			str[_layerIndex, _keyIndex].UVs[1] += str[_layerIndex, _keyIndex].UVs[3];
			str[_layerIndex, _keyIndex].UVs[3] = -str[_layerIndex, _keyIndex].UVs[3];
		}

		public void Undo(Str str) {
			// Self transformation
			str[_layerIndex, _keyIndex].UVs[1] += str[_layerIndex, _keyIndex].UVs[3];
			str[_layerIndex, _keyIndex].UVs[3] = -str[_layerIndex, _keyIndex].UVs[3];
		}
	}
}
