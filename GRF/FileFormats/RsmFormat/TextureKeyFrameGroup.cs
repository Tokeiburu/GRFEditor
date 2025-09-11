using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GRF.FileFormats.RsmFormat {
	public enum TextureTransformTypes {
		None = -1,
		TranslateX = 0,
		TranslateY = 1,
		ScaleX = 2,
		ScaleY = 3,
		RotateZ = 4,
	}

	public class TextureKeyFrameGroup {
		public HashSet<TextureTransformTypes> _types = new HashSet<TextureTransformTypes>();

		public Dictionary<int, Dictionary<TextureTransformTypes, List<TextureKeyFrame>>> _offsets = new Dictionary<int, Dictionary<TextureTransformTypes, List<TextureKeyFrame>>>();

		public int Count {
			get { return _offsets.Count; }
		}

		public IEnumerable<TextureTransformTypes> Types {
			get { return _types.ToList(); }
		}

		public TextureKeyFrameGroup() {
		}

		public TextureKeyFrameGroup(TextureKeyFrameGroup tkfg) {
			foreach (var entry in tkfg._offsets) {
				var d = new Dictionary<TextureTransformTypes, List<TextureKeyFrame>>();
				_offsets[entry.Key] = d;

				foreach (var entry2 in entry.Value) {
					var l = new List<TextureKeyFrame>();
					d[entry2.Key] = l;

					foreach (var entry3 in entry2.Value) {
						l.Add(new TextureKeyFrame(entry3));
					}
				}
			}

			foreach (var type in tkfg._types) {
				_types.Add(type);
			}
		}

		public float GetValue(int textureId, TextureTransformTypes type, int keyFrameIndex) {
			return _offsets[textureId][type][keyFrameIndex].Offset;
		}

		public bool TryGetValue(int textureId, TextureTransformTypes type, int frame, out float value) {
			value = 0;

			if (_offsets.ContainsKey(textureId)) {
				var offsets = _offsets[textureId];

				if (!offsets.ContainsKey(type))
					return false;

				var l = offsets[type];

				for (int i = 0; i < l.Count; i++) {
					if (l[i].Frame == frame) {
						value = l[i].Offset;
						return true;
					}

					if (l[i].Frame > frame)
						return false;
				}
			}

			return false;
		}

		public int AddTextureKeyFrame(int textureId, TextureTransformTypes type, TextureKeyFrame frame) {
			_types.Add(type);

			if (!_offsets.ContainsKey(textureId)) {
				_offsets[textureId] = new Dictionary<TextureTransformTypes, List<TextureKeyFrame>>();
			}

			var offsets = _offsets[textureId];

			if (!offsets.ContainsKey(type)) {
				offsets[type] = new List<TextureKeyFrame>();
			}

			var keyFrames = offsets[type];
			int ti;
			for (ti = 0; ti < keyFrames.Count; ti++) {
				if (keyFrames[ti].Frame > frame.Frame)
					break;
			}

			keyFrames.Insert(ti, frame);
			frame.Type = type;
			return ti;
		}

		public bool HasTextureAnimation(int textureId, TextureTransformTypes type) {
			return GetTextureKeyFrames(textureId, type) != null;
		}

		public List<MergedTextureKeyFrame> GetTextureKeyFrames(int textureId) {
			var dframes = new Dictionary<int, MergedTextureKeyFrame>();

			if (_offsets.ContainsKey(textureId)) {
				var textures = _offsets[textureId];

				foreach (var group in textures) {
					foreach (var keyframe in group.Value) {
						if (!dframes.ContainsKey(keyframe.Frame)) {
							dframes[keyframe.Frame] = new MergedTextureKeyFrame { Frame = keyframe.Frame };
						}

						dframes[keyframe.Frame].Offsets[(int)group.Key] = keyframe.Offset;
					}
				}
			}

			return dframes.OrderBy(p => p.Key).Select(p => p.Value).ToList();
		}

		public List<TextureKeyFrame> GetTextureKeyFrames(int textureId, TextureTransformTypes type) {
			if (_offsets.ContainsKey(textureId)) {
				var offsets = _offsets[textureId];

				if (!offsets.ContainsKey(type))
					return null;

				return offsets[type];
			}

			return new List<TextureKeyFrame>();
		}

		public void Write(BinaryWriter writer) {
			writer.Write(_offsets.Count);

			foreach (var textureEntry in _offsets) {
				writer.Write(textureEntry.Key);
				writer.Write(textureEntry.Value.Count);

				foreach (var typeEntry in textureEntry.Value) {
					writer.Write((int)typeEntry.Key);
					writer.Write(typeEntry.Value.Count);

					foreach (var frame in typeEntry.Value) {
						writer.Write(frame.Frame);
						writer.Write(frame.Offset);
					}
				}
			}
		}
	}
}
