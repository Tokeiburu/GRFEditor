using GRF.Graphics;

namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipHCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private readonly TkVector2 _origin;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public FlipHCommand(int layerIndex, int keyIndex, TkVector2 origin) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_origin = origin;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Flip horizontal";

		public void Execute(Str str) {
			str[_layerIndex, _keyIndex].Offset = new TkVector2(
				2 * _origin.X - str[_layerIndex, _keyIndex].Offset.X,
				str[_layerIndex, _keyIndex].Offset.Y);

			// Self transformation
			str[_layerIndex, _keyIndex].Positions[0] = -str[_layerIndex, _keyIndex].Positions[0];
			str[_layerIndex, _keyIndex].Positions[1] = -str[_layerIndex, _keyIndex].Positions[1];
			str[_layerIndex, _keyIndex].Positions[2] = -str[_layerIndex, _keyIndex].Positions[2];
			str[_layerIndex, _keyIndex].Positions[3] = -str[_layerIndex, _keyIndex].Positions[3];
			str[_layerIndex, _keyIndex].Angle = -str[_layerIndex, _keyIndex].Angle;
			str[_layerIndex, _keyIndex].BezierPositions[0] = -str[_layerIndex, _keyIndex].BezierPositions[0];
			str[_layerIndex, _keyIndex].BezierPositions[2] = -str[_layerIndex, _keyIndex].BezierPositions[2];
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].Offset = new TkVector2(
				2 * _origin.X - str[_layerIndex, _keyIndex].Offset.X,
				str[_layerIndex, _keyIndex].Offset.Y);

			// Self transformation
			str[_layerIndex, _keyIndex].Positions[0] = -str[_layerIndex, _keyIndex].Positions[0];
			str[_layerIndex, _keyIndex].Positions[1] = -str[_layerIndex, _keyIndex].Positions[1];
			str[_layerIndex, _keyIndex].Positions[2] = -str[_layerIndex, _keyIndex].Positions[2];
			str[_layerIndex, _keyIndex].Positions[3] = -str[_layerIndex, _keyIndex].Positions[3];
			str[_layerIndex, _keyIndex].Angle = -str[_layerIndex, _keyIndex].Angle;
			str[_layerIndex, _keyIndex].BezierPositions[0] = -str[_layerIndex, _keyIndex].BezierPositions[0];
			str[_layerIndex, _keyIndex].BezierPositions[2] = -str[_layerIndex, _keyIndex].BezierPositions[2];
		}
	}
}
