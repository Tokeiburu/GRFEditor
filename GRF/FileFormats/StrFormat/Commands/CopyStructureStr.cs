using System.Collections.Generic;
using System.Linq;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CopyStructureStr {
		public List<StrLayer> Layers;

		private Dictionary<int, StrLayer> _changedLayers;
		private int _maxKeyFrames;
		private bool _hasBeenCleaned;

		public CopyStructureStr(Str str) {
			Layers = new List<StrLayer>();

			foreach (var layer in str.Layers) {
				Layers.Add(new StrLayer(layer));
			}

			_maxKeyFrames = str.MaxKeyFrame;
		}

		public void Apply(Str str) {
			if (!_hasBeenCleaned) {
				if (Layers != null)
					str.Layers = Layers.Select(action => new StrLayer(action)).ToList();

				str.MaxKeyFrame = _maxKeyFrames;
			}
			else {
				if (Layers != null) {
					if (_changedLayers != null) {
						foreach (var pair in _changedLayers) {
							str.Layers[pair.Key] = new StrLayer(pair.Value);
						}
					}
					else {
						str.Layers = Layers.Select(action => new StrLayer(action)).ToList();
					}
				}

				str.MaxKeyFrame = _maxKeyFrames;
			}
		}

		public void Clean(Str str) {
			if (!_hasBeenCleaned) {
				foreach (var layer in str.Layers) {
					layer.KeyFrames = layer.KeyFrames.OrderBy(p => p.FrameIndex).ToList();
				}

				if (Layers != null && str.Layers.Count == Layers.Count) {
					_changedLayers = new Dictionary<int, StrLayer>();

					for (int i = 0; i < str.Layers.Count; i++) {
						if (!str[i].Equals(Layers[i])) {
							_changedLayers[i] = Layers[i];
						}
					}

					Layers.Clear();
				}

				_hasBeenCleaned = true;
			}
		}

		public bool HasChanged(Str str) {
			if (!_hasBeenCleaned) {
				return Layers != null;
			}

			if (Layers != null) {
				if (Layers.Count > 0) return true;
				if (_changedLayers == null) return true;
				if (_changedLayers.Any()) return true;
				// Else : inconclusive
			}

			return false;
		}
	}
}