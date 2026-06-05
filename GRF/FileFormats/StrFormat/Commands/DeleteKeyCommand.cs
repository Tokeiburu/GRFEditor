namespace GRF.FileFormats.StrFormat.Commands {
	public class DeleteKeyCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private StrKeyFrame _frame;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public DeleteKeyCommand(int layerIndex, int keyIndex) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Deleted key at {_keyIndex}";

		public void Execute(Str str) {
			if (_frame == null) {
				_frame = str[_layerIndex, _keyIndex];
			}

			str[_layerIndex].KeyFrames.RemoveAt(_keyIndex);
			str[_layerIndex].InvalidateIndex();
		}

		public void Undo(Str str) {
			str[_layerIndex].KeyFrames.Insert(_keyIndex, _frame);
			str[_layerIndex].InvalidateIndex();
		}
	}
}
