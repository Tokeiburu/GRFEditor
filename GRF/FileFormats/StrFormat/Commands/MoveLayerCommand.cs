namespace GRF.FileFormats.StrFormat.Commands {
	public class MoveLayerCommand : IStrCommand {
		private readonly int _layerIndexSrc;
		private readonly int _layerIndexDst;
		private bool _isExecute;

		public bool IsExecute => _isExecute;
		public int LayerIndexSource => _layerIndexSrc;
		public int LayerIndexDestination => _layerIndexDst;

		public MoveLayerCommand(int layerIndexSource, int layerIndexDest) {
			_layerIndexSrc = layerIndexSource;
			_layerIndexDst = layerIndexDest;
		}

		public string CommandDescription => $"[{_layerIndexSrc}] to [{_layerIndexDst}] layer moved";

		public void Execute(Str str) {
			var layer = str[_layerIndexSrc];
			str.Layers.RemoveAt(_layerIndexSrc);

			if (_layerIndexDst > _layerIndexSrc) {
				str.Layers.Insert(_layerIndexDst - 1, layer);
			}
			else {
				str.Layers.Insert(_layerIndexDst, layer);
			}

			_isExecute = true;
		}

		public void Undo(Str str) {
			var src = _layerIndexDst;
			var dst = _layerIndexSrc;

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
