using System.Collections.Generic;

namespace GRF.FileFormats.StrFormat.Commands {
	public class DeleteKeysCommand : IStrCommand {
		private readonly int _layerIndex;
		private readonly int _frameIndexStart;
		private readonly int _frameIndexEnd;
		private List<int> _keyIndexes;
		private List<(int KeyIndex, StrKeyFrame KeyFrame)> _conflicts;

		public int LayerIndex => _layerIndex;

		public DeleteKeysCommand(int layerIndex, int frameIndexStart, int frameIndexEnd) {
			_layerIndex = layerIndex;
			_frameIndexStart = frameIndexStart;
			_frameIndexEnd = frameIndexEnd;
		}

		public string CommandDescription => $"[{_layerIndex}] Deleted keys ({_keyIndexes.Count})";

		public void Execute(Str str) {
			var layer = str[_layerIndex];

			if (_keyIndexes == null) {
				_keyIndexes = str[_layerIndex].GetKeyIndexesInRange(_frameIndexStart, _frameIndexEnd - _frameIndexStart, StrLayer.RangeSearchMode.Contained);
			}

			if (_conflicts == null) {
				_conflicts = new List<(int KeyIndex, StrKeyFrame KeyFrame)>();

				for (int i = 0; i < _keyIndexes.Count; i++)
					_conflicts.Add((_keyIndexes[i], layer[_keyIndexes[i]]));
			}

			for (int i = _keyIndexes.Count - 1; i >= 0; i--)
				layer.KeyFrames.RemoveAt(_keyIndexes[i]);

			str[_layerIndex].InvalidateIndex();
		}

		public void Undo(Str str) {
			var layer = str[_layerIndex];

			for (int i = 0; i < _conflicts.Count; i++)
				layer.KeyFrames.Insert(_conflicts[i].KeyIndex, _conflicts[i].KeyFrame);

			str[_layerIndex].InvalidateIndex();
		}
	}
}
