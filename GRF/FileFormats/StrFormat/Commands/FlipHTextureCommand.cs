namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipHTextureCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public FlipHTextureCommand(int layerIndex, int keyIndex) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Mirror texture horizontal";

		public void Execute(Str str) {
			var layer = str[_layerIndex, _keyIndex];
			
			float t0 = layer.Positions[0];
			float t2 = layer.Positions[2];

			layer.Positions[0] = layer.Positions[1];
			layer.Positions[1] = t0;
			layer.Positions[2] = layer.Positions[3];
			layer.Positions[3] = t2;
		}

		public void Undo(Str str) {
			var layer = str[_layerIndex, _keyIndex];

			float t0 = layer.Positions[0];
			float t2 = layer.Positions[2];

			layer.Positions[0] = layer.Positions[1];
			layer.Positions[1] = t0;
			layer.Positions[2] = layer.Positions[3];
			layer.Positions[3] = t2;
		}
	}
}
