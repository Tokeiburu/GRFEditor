namespace GRF.FileFormats.StrFormat.Commands {
	public class CreateNewKeyCommand : IStrCommand, IFrameCommand {
		private readonly int _layerIndex;
		private readonly int _frameIndex;
		private readonly bool _interpolate;

		private StrKeyFrame _addedFrame;
		private StrKeyFrame _conflictFrame;
		private bool _firstExecute = true;
		private int _keyIndex;

		public int LayerIndex => _frameIndex;
		public int FrameIndex => _frameIndex;

		public CreateNewKeyCommand(int layerIndex, int frameIndex, bool interpolate) {
			_layerIndex = layerIndex;
			_frameIndex = frameIndex;
			_interpolate = interpolate;
		}

		public string CommandDescription => $"[{_layerIndex}] New key at frame {_frameIndex}";

		public void Execute(Str str) {
			var layer = str[_layerIndex];

			if (!_firstExecute) {
				if (_conflictFrame != null)
					layer.KeyFrames[_keyIndex] = _addedFrame;
				else
					layer.KeyFrames.Insert(_keyIndex, _addedFrame);
				
				layer.InvalidateIndex();
				return;
			}

			_keyIndex = layer.FrameIndex2KeyIndex[_frameIndex];

			if (_keyIndex == -1) {
				_keyIndex = layer.GetNextKeyIndex(_frameIndex);

				if (_keyIndex == -1)
					_keyIndex = layer.KeyFrames.Count;

				_addedFrame = StrKeyFrame.CreateDefaultFrame(_frameIndex);
				_addedFrame.IsInterpolated = _interpolate;
				layer.KeyFrames.Insert(_keyIndex, _addedFrame);
			}
			else {
				if (layer[_keyIndex].FrameIndex == _frameIndex) {
					_conflictFrame = layer[_keyIndex];

					_addedFrame = StrKeyFrame.CreateDefaultFrame(_frameIndex);
					_addedFrame.IsInterpolated = _conflictFrame.IsInterpolated;
					layer.KeyFrames[_keyIndex] = _addedFrame;
				}
				else {
					_addedFrame = InterpolatedKeyFrame.InterpolateSub(str, _layerIndex, _frameIndex, layer[_keyIndex], layer[_keyIndex + 1]).ToKeyFrame();
					_addedFrame.IsInterpolated = true;
					layer.KeyFrames.Insert(++_keyIndex, _addedFrame);
				}
			}

			layer.InvalidateIndex();
			_firstExecute = false;
		}

		public void Undo(Str str) {
			var layer = str[_layerIndex];

			if (_conflictFrame != null)
				layer.KeyFrames[_keyIndex] = _conflictFrame;
			else
				layer.KeyFrames.RemoveAt(_keyIndex);

			layer.InvalidateIndex();
		}
	}
}
