namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipHSelfCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;

		public FlipHSelfCommand(int layerIdx, int frameIdx) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Flip horizontal (self)";
			}
		}

		public void Execute(Str str) {
			// Self transformation
			str[_layerIdx, _frameIdx].Uv[0] += str[_layerIdx, _frameIdx].Uv[2];
			str[_layerIdx, _frameIdx].Uv[2] = -str[_layerIdx, _frameIdx].Uv[2];
		}

		public void Undo(Str str) {
			// Self transformation
			str[_layerIdx, _frameIdx].Uv[0] += str[_layerIdx, _frameIdx].Uv[2];
			str[_layerIdx, _frameIdx].Uv[2] = -str[_layerIdx, _frameIdx].Uv[2];
		}
	}
}
