using System.Collections.Generic;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CreateEndKeyCommand : IStrCommand, IFrameCommand {
		private readonly int _layerIndex;
		private int _keyIndex;
		private readonly int _frameIndex;

		private StrKeyFrame _addedFrame;
		private StrKeyFrame _conflictFrame;
		private bool _firstExecute = true;

		public int LayerIndex => _frameIndex;
		public int KeyIndex => _keyIndex;
		public int FrameIndex => _frameIndex;

		public CreateEndKeyCommand(int layerIndex, int frameIndex) {
			_layerIndex = layerIndex;
			_frameIndex = frameIndex;
		}

		public string CommandDescription => $"[{_layerIndex}] New end key at frame {_frameIndex}";

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

			try {
				_keyIndex = layer.FrameIndex2KeyIndex[_frameIndex];

				if (_keyIndex == -1) {
					_keyIndex = layer.GetPreviousKeyIndex(_frameIndex);

					if (_keyIndex == -1) {
						_keyIndex = 0;
						_addedFrame = StrKeyFrame.CreateDefaultFrame(_frameIndex);
					}
					else {
						_addedFrame = new StrKeyFrame(layer[_keyIndex++]);
						_addedFrame.FrameIndex = _frameIndex;
					}

					layer.KeyFrames.Insert(_keyIndex, _addedFrame);
				}
				else {
					if (layer[_keyIndex].FrameIndex == _frameIndex) {
						_conflictFrame = layer[_keyIndex];

						_addedFrame = new StrKeyFrame(_conflictFrame);
						layer.KeyFrames[_keyIndex] = _addedFrame;
					}
					else {
						_addedFrame = InterpolatedKeyFrame.InterpolateSub(str, _layerIndex, _frameIndex, layer[_keyIndex], layer[_keyIndex + 1]).ToKeyFrame();
						layer.KeyFrames.Insert(++_keyIndex, _addedFrame);
					}
				}

				_addedFrame.IsInterpolated = false;
				_addedFrame.Color[3] = 0;
			}
			finally {
				layer.InvalidateIndex();
				_firstExecute = false;
			}
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
