using System.Collections.Generic;

namespace GRF.FileFormats.RsmFormat {
	public class TextureKeyFrameGroup {
		public Dictionary<int, Dictionary<int, List<TextureKeyFrame>>> _offsets = new Dictionary<int, Dictionary<int, List<TextureKeyFrame>>>();

		public int Count {
			get { return _offsets.Count; }
		}

		public IEnumerable<int> Types {
			get { return _offsets.Keys; }
		}

		public void AddTextureKeyFrame(int textureId, int type, TextureKeyFrame frame) {
			if (!_offsets.ContainsKey(type)) {
				_offsets[type] = new Dictionary<int, List<TextureKeyFrame>>();
			}

			var offsets = _offsets[type];

			if (!offsets.ContainsKey(textureId)) {
				offsets[textureId] = new List<TextureKeyFrame>();
			}

			offsets[textureId].Add(frame);
		}

		public bool HasTextureAnimation(int textureId, int type) {
			return GetTextureKeyFrames(textureId, type) != null;
		}

		public List<TextureKeyFrame> GetTextureKeyFrames(int textureId, int type) {
			if (_offsets.ContainsKey(type)) {
				var offsets = _offsets[type];

				if (!offsets.ContainsKey(textureId))
					return null;

				return offsets[textureId];
			}

			return null;
		}
	}
}
