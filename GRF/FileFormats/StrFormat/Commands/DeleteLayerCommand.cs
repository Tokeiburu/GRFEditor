namespace GRF.FileFormats.StrFormat.Commands {
	public class DeleteLayerCommand : IStrCommand {
		private readonly int _layerIdx;
		private bool _isExecute;
		private StrLayer _layer;
		private bool _isSet;

		public object Object0;
		public object Object1;

		public bool IsExecute {
			get { return _isExecute; }
		}

		public int LayerIndex {
			get { return _layerIdx; }
		}

		public StrLayer Layer {
			get { return _layer; }
		}

		public DeleteLayerCommand(int layerIdx) {
			_layerIdx = layerIdx;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "] Deleted layer";
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_layer = str[_layerIdx];
				_isSet = true;
			}

			str.Layers.RemoveAt(_layerIdx);
			_isExecute = true;
		}

		public void Undo(Str str) {
			str.Layers.Insert(_layerIdx, _layer);
			_isExecute = false;
		}
	}
}
