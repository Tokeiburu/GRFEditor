using GRF.Graphics;

namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipVCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private readonly TkVector2 _origin;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public FlipVCommand(int layerIndex, int frameIndex, TkVector2 origin) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;
			_origin = origin;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Flip vertical";

		public void Execute(Str str) {
			str[_layerIndex, _keyIndex].Offset = new TkVector2(
				str[_layerIndex, _keyIndex].Offset.X,
				2 * _origin.Y - str[_layerIndex, _keyIndex].Offset.Y);

			// Self transformation
			str[_layerIndex, _keyIndex].Xy[4] = -str[_layerIndex, _keyIndex].Xy[4];
			str[_layerIndex, _keyIndex].Xy[5] = -str[_layerIndex, _keyIndex].Xy[5];
			str[_layerIndex, _keyIndex].Xy[6] = -str[_layerIndex, _keyIndex].Xy[6];
			str[_layerIndex, _keyIndex].Xy[7] = -str[_layerIndex, _keyIndex].Xy[7];
			str[_layerIndex, _keyIndex].Angle = -str[_layerIndex, _keyIndex].Angle;
			str[_layerIndex, _keyIndex].Bezier[1] = -str[_layerIndex, _keyIndex].Bezier[1];
			str[_layerIndex, _keyIndex].Bezier[3] = -str[_layerIndex, _keyIndex].Bezier[3];
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].Offset = new TkVector2(
				str[_layerIndex, _keyIndex].Offset.X,
				2 * _origin.Y - str[_layerIndex, _keyIndex].Offset.Y);

			// Self transformation
			str[_layerIndex, _keyIndex].Xy[4] = -str[_layerIndex, _keyIndex].Xy[4];
			str[_layerIndex, _keyIndex].Xy[5] = -str[_layerIndex, _keyIndex].Xy[5];
			str[_layerIndex, _keyIndex].Xy[6] = -str[_layerIndex, _keyIndex].Xy[6];
			str[_layerIndex, _keyIndex].Xy[7] = -str[_layerIndex, _keyIndex].Xy[7];
			str[_layerIndex, _keyIndex].Angle = -str[_layerIndex, _keyIndex].Angle;
			str[_layerIndex, _keyIndex].Bezier[1] = -str[_layerIndex, _keyIndex].Bezier[1];
			str[_layerIndex, _keyIndex].Bezier[3] = -str[_layerIndex, _keyIndex].Bezier[3];
		}
	}
}
