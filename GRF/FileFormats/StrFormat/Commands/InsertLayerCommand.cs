namespace GRF.FileFormats.StrFormat.Commands {
	public class InsertLayerCommand : IStrCommand {
		private readonly int _layerIndex;
		private readonly StrLayer _layer;
		private bool _isExecute;

		private readonly int _mode;

		public bool IsExecute => _isExecute;
		public int LayerIndex => _layerIndex;
		public int Mode => _mode;

		public InsertLayerCommand(int layerIndex) {
			_layerIndex = layerIndex;
			_mode = 0;
		}

		public InsertLayerCommand(int layerIndex, StrLayer layer) {
			_layerIndex = layerIndex;
			_layer = layer;
			_mode = 1;
		}

		public string CommandDescription => $"[{_layerIndex}] Insert layer";

		public void Execute(Str str) {
			StrLayer layer;

			if (_mode == 1) {
				layer = _layer;
			}
			else {
				layer = new StrLayer(str);
				layer.GenerateTexturesHash();
			}

			str.Layers.Insert(_layerIndex, layer);
			_isExecute = true;
		}

		public void Undo(Str str) {
			str.Layers.RemoveAt(_layerIndex);
			_isExecute = false;
		}
	}
}
