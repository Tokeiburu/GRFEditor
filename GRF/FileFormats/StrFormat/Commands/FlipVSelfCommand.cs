namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipVSelfCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;

		public FlipVSelfCommand(int layerIdx, int frameIdx) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Flip vertical (self)";
			}
		}

		public void Execute(Str str) {
			// Self transformation
			str[_layerIdx, _frameIdx].Uv[1] += str[_layerIdx, _frameIdx].Uv[3];
			str[_layerIdx, _frameIdx].Uv[3] = -str[_layerIdx, _frameIdx].Uv[3];
		}

		public void Undo(Str str) {
			// Self transformation
			str[_layerIdx, _frameIdx].Uv[1] += str[_layerIdx, _frameIdx].Uv[3];
			str[_layerIdx, _frameIdx].Uv[3] = -str[_layerIdx, _frameIdx].Uv[3];
		}
	}
}
