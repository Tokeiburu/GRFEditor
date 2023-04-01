namespace GRF.FileFormats.StrFormat.Commands {
	public class InsertLayerCommand : IStrCommand {
		private readonly int _layerIdx;
		private readonly StrLayer _layer;
		private bool _isExecute;

		public object Object0;
		private readonly int _mode;

		public bool IsExecute {
			get { return _isExecute; }
		}

		public int LayerIndex {
			get { return _layerIdx; }
		}

		public int Mode {
			get { return _mode; }
		}

		public InsertLayerCommand(int layerIdx) {
			_layerIdx = layerIdx;
			_mode = 0;
		}

		public InsertLayerCommand(int layerIdx, StrLayer layer) {
			_layerIdx = layerIdx;
			_layer = layer;
			_mode = 1;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "] Insert layer";
			}
		}

		public void Execute(Str str) {
			StrLayer layer;

			if (_mode == 1) {
				layer = _layer;
			}
			else {
				layer = new StrLayer();
				layer.GenerateTexturesHash();
				layer.Index(str.KeyFrameCount);
			}

			str.Layers.Insert(_layerIdx, layer);
			_isExecute = true;
		}

		public void Undo(Str str) {
			str.Layers.RemoveAt(_layerIdx);
			_isExecute = false;
		}
	}
}
