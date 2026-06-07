namespace GRF.FileFormats.StrFormat.Commands {
	public class FlipVTextureCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public FlipVTextureCommand(int layerIndex, int keyIndex) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Mirror texture vertical";

		public void Execute(Str str) {
			var layer = str[_layerIndex, _keyIndex];
			
			float t4 = layer.Positions[4];
			float t5 = layer.Positions[5];

			layer.Positions[4] = layer.Positions[6];
			layer.Positions[5] = layer.Positions[7];
			layer.Positions[6] = t4;
			layer.Positions[7] = t5;
		}

		public void Undo(Str str) {
			var layer = str[_layerIndex, _keyIndex];

			float t4 = layer.Positions[4];
			float t5 = layer.Positions[5];

			layer.Positions[4] = layer.Positions[6];
			layer.Positions[5] = layer.Positions[7];
			layer.Positions[6] = t4;
			layer.Positions[7] = t5;
		}
	}
}
