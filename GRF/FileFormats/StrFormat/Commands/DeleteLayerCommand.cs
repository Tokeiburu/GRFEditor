namespace GRF.FileFormats.StrFormat.Commands {
	public class DeleteLayerCommand : IStrCommand {
		private readonly int _layerIndex;
		private bool _isExecute;
		private StrLayer _layer;
		private bool _isSet;

		public bool IsExecute => _isExecute;
		public int LayerIndex => _layerIndex;
		public StrLayer Layer => _layer;

		public DeleteLayerCommand(int layerIndex) {
			_layerIndex = layerIndex;
		}

		public string CommandDescription => $"[{_layerIndex}] Deleted layer";

		public void Execute(Str str) {
			if (!_isSet) {
				_layer = str[_layerIndex];
				_isSet = true;
			}

			str.Layers.RemoveAt(_layerIndex);
			_isExecute = true;
		}

		public void Undo(Str str) {
			str.Layers.Insert(_layerIndex, _layer);
			_isExecute = false;
		}
	}
}
