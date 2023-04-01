using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class DeleteKeyCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private StrKeyFrame _frame;

		public int LayerIdx {
			get { return _layerIdx; }
		}

		public int FrameIdx {
			get { return _frameIdx; }
		}

		public DeleteKeyCommand(int layerIdx, int frameIdx) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Deleted key at " + _frameIdx;
			}
		}

		public void Execute(Str str) {
			if (_frame == null) {
				_frame = str[_layerIdx, _frameIdx];
			}

			str[_layerIdx].KeyFrames.RemoveAt(_frameIdx);
		}

		public void Undo(Str str) {
			str[_layerIdx].KeyFrames.Insert(_frameIdx, _frame);
		}
	}
}
