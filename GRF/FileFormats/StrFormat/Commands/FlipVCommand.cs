using GRF.Graphics;

namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipVCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private readonly TkVector2 _origin;

		public FlipVCommand(int layerIdx, int frameIdx, TkVector2 origin) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_origin = origin;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Flip Vertical";
			}
		}

		public void Execute(Str str) {
			str[_layerIdx, _frameIdx].Offset = new TkVector2(
				str[_layerIdx, _frameIdx].Offset.X,
				2 * _origin.Y - str[_layerIdx, _frameIdx].Offset.Y);

			// Self transformation
			str[_layerIdx, _frameIdx].Xy[4] = -str[_layerIdx, _frameIdx].Xy[4];
			str[_layerIdx, _frameIdx].Xy[5] = -str[_layerIdx, _frameIdx].Xy[5];
			str[_layerIdx, _frameIdx].Xy[6] = -str[_layerIdx, _frameIdx].Xy[6];
			str[_layerIdx, _frameIdx].Xy[7] = -str[_layerIdx, _frameIdx].Xy[7];
			str[_layerIdx, _frameIdx].Angle = -str[_layerIdx, _frameIdx].Angle;
			str[_layerIdx, _frameIdx].Bezier[1] = -str[_layerIdx, _frameIdx].Bezier[1];
			str[_layerIdx, _frameIdx].Bezier[3] = -str[_layerIdx, _frameIdx].Bezier[3];
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].Offset = new TkVector2(
				str[_layerIdx, _frameIdx].Offset.X,
				2 * _origin.Y - str[_layerIdx, _frameIdx].Offset.Y);

			// Self transformation
			str[_layerIdx, _frameIdx].Xy[4] = -str[_layerIdx, _frameIdx].Xy[4];
			str[_layerIdx, _frameIdx].Xy[5] = -str[_layerIdx, _frameIdx].Xy[5];
			str[_layerIdx, _frameIdx].Xy[6] = -str[_layerIdx, _frameIdx].Xy[6];
			str[_layerIdx, _frameIdx].Xy[7] = -str[_layerIdx, _frameIdx].Xy[7];
			str[_layerIdx, _frameIdx].Angle = -str[_layerIdx, _frameIdx].Angle;
			str[_layerIdx, _frameIdx].Bezier[1] = -str[_layerIdx, _frameIdx].Bezier[1];
			str[_layerIdx, _frameIdx].Bezier[3] = -str[_layerIdx, _frameIdx].Bezier[3];
		}
	}
}
