namespace GRF.FileFormats.StrFormat.Commands {
	public class AddKeyCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private readonly StrKeyFrame _frame;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public AddKeyCommand(int layerIndex, int keyIndex, StrKeyFrame frame) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_frame = frame;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Added key at {_keyIndex}";

		public void Execute(Str str) {
			str[_layerIndex].KeyFrames.Insert(_keyIndex, _frame);
			str[_layerIndex].InvalidateIndex();
		}

		public void Undo(Str str) {
			str[_layerIndex].KeyFrames.RemoveAt(_keyIndex);
			str[_layerIndex].InvalidateIndex();
		}
	}
}
