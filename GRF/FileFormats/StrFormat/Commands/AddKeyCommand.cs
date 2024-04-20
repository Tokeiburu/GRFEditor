namespace GRF.FileFormats.StrFormat.Commands {
	public class AddKeyCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private readonly StrKeyFrame _frame;

		public int LayerIdx {
			get { return _layerIdx; }
		}

		public int FrameIdx {
			get { return _frameIdx; }
		}

		public AddKeyCommand(int layerIdx, int frameIdx, StrKeyFrame frame) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_frame = frame;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Added key at " + _frameIdx;
			}
		}

		public void Execute(Str str) {
			//if (float.IsNaN(_oldAngle)) {
			//	_oldAngle = str[_layerIdx, _frameIdx].Angle;
			//}

			str[_layerIdx].KeyFrames.Insert(_frameIdx, _frame);
		}

		public void Undo(Str str) {
			str[_layerIdx].KeyFrames.RemoveAt(_frameIdx);
		}
	}
}
