using GRF.Graphics;

namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipHCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private readonly Point _origin;

		public FlipHCommand(int layerIdx, int frameIdx, Graphics.Point origin) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_origin = origin;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Flip Horizontal";
			}
		}

		public void Execute(Str str) {
			str[_layerIdx, _frameIdx].Offset = new Point(
				2 * _origin.X - str[_layerIdx, _frameIdx].Offset.X,
				str[_layerIdx, _frameIdx].Offset.Y);

			// Self transformation
			str[_layerIdx, _frameIdx].Xy[0] = -str[_layerIdx, _frameIdx].Xy[0];
			str[_layerIdx, _frameIdx].Xy[1] = -str[_layerIdx, _frameIdx].Xy[1];
			str[_layerIdx, _frameIdx].Xy[2] = -str[_layerIdx, _frameIdx].Xy[2];
			str[_layerIdx, _frameIdx].Xy[3] = -str[_layerIdx, _frameIdx].Xy[3];
			str[_layerIdx, _frameIdx].Angle = -str[_layerIdx, _frameIdx].Angle;
			str[_layerIdx, _frameIdx].Bezier[0] = -str[_layerIdx, _frameIdx].Bezier[0];
			str[_layerIdx, _frameIdx].Bezier[2] = -str[_layerIdx, _frameIdx].Bezier[2];
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].Offset = new Point(
				2 * _origin.X - str[_layerIdx, _frameIdx].Offset.X,
				str[_layerIdx, _frameIdx].Offset.Y);

			// Self transformation
			str[_layerIdx, _frameIdx].Xy[0] = -str[_layerIdx, _frameIdx].Xy[0];
			str[_layerIdx, _frameIdx].Xy[1] = -str[_layerIdx, _frameIdx].Xy[1];
			str[_layerIdx, _frameIdx].Xy[2] = -str[_layerIdx, _frameIdx].Xy[2];
			str[_layerIdx, _frameIdx].Xy[3] = -str[_layerIdx, _frameIdx].Xy[3];
			str[_layerIdx, _frameIdx].Angle = -str[_layerIdx, _frameIdx].Angle;
			str[_layerIdx, _frameIdx].Bezier[0] = -str[_layerIdx, _frameIdx].Bezier[0];
			str[_layerIdx, _frameIdx].Bezier[2] = -str[_layerIdx, _frameIdx].Bezier[2];
		}
	}
}
