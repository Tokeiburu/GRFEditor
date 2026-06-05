namespace GRF.FileFormats.StrFormat.Commands {
	public class SetKeyCommand : IStrCommand, IFrameCommand {
		private readonly int _layerIndex;
		private readonly int _frameIndex;
		private readonly StrKeyFrame _frame;
		private StrKeyFrame _conflict;
		private int _insertKeyIndex;

		public int LayerIndex => _layerIndex;
		public int FrameIndex => _frameIndex;

		public enum ScaleMode {
			KeyFrame,
			Layer,
			Str
		}

		public SetKeyCommand(int layerIndex, int frameIndex, StrKeyFrame frame) {
			_layerIndex = layerIndex;
			_frameIndex = frameIndex;
			_frame = frame;
		}

		public string CommandDescription => $"[{_layerIndex}:{_frameIndex}] Set frame at {_frameIndex}";

		public void Execute(Str str) {
			var layer = str[_layerIndex];
			int keyIndex = layer.FrameIndex2KeyIndex[_frameIndex];
			
			if (keyIndex > -1 && layer.KeyFrames[keyIndex].FrameIndex == _frameIndex) {
				_conflict = layer.KeyFrames[keyIndex];
				str[_layerIndex].KeyFrames[keyIndex] = _frame;
				_insertKeyIndex = keyIndex;
				return;
			}

			if (keyIndex == -1) {
				if (layer.KeyFrames.Count > 0 && layer.KeyFrames[0].FrameIndex > _frameIndex)
					_insertKeyIndex = 0;
				else
					_insertKeyIndex = layer.KeyFrames.Count;
			}
			else {
				_insertKeyIndex = keyIndex + 1;
			}

			str[_layerIndex].KeyFrames.Insert(_insertKeyIndex, _frame);
			str[_layerIndex].InvalidateIndex();
		}

		public void Undo(Str str) {
			if (_conflict != null) {
				str[_layerIndex].KeyFrames[_insertKeyIndex] = _conflict;
				return;
			}

			str[_layerIndex].KeyFrames.RemoveAt(_insertKeyIndex);
			str[_layerIndex].InvalidateIndex();
		}
	}
}
