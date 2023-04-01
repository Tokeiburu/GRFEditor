namespace GRF.FileFormats.StrFormat.Commands {
	public class MoveLayerCommand : IStrCommand {
		private readonly int _layerIdxSource;
		private readonly int _layerIdxDest;
		private bool _isExecute;

		public bool IsExecute {
			get { return _isExecute; }
		}

		public int LayerIndexSource {
			get { return _layerIdxSource; }
		}

		public int LayerIndexDestination {
			get { return _layerIdxDest; }
		}

		//public int LayerIdx {
		//	get { return _layerIdx; }
		//}
		//
		//public int FrameIdx {
		//	get { return _frameIdx; }
		//}

		public MoveLayerCommand(int layerIdxSource, int layerIdxDest) {
			_layerIdxSource = layerIdxSource;
			_layerIdxDest = layerIdxDest;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdxSource + "] to [" + _layerIdxDest + "] layer moved";
			}
		}

		public void Execute(Str str) {
			var layer = str[_layerIdxSource];
			str.Layers.RemoveAt(_layerIdxSource);

			if (_layerIdxDest > _layerIdxSource) {
				str.Layers.Insert(_layerIdxDest - 1, layer);
			}
			else {
				str.Layers.Insert(_layerIdxDest, layer);
			}

			_isExecute = true;
		}

		public void Undo(Str str) {
			var src = _layerIdxDest;
			var dst = _layerIdxSource;

			if (dst > src) {
				var layer = str[src];
				str.Layers.RemoveAt(src);
				str.Layers.Insert(dst, layer);
			}
			else {
				var layer = str[src - 1];
				str.Layers.RemoveAt(src - 1);
				str.Layers.Insert(dst, layer);
			}

			_isExecute = false;
		}
	}
}
