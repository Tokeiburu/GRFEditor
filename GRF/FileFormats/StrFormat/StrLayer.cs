using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.StrFormat {
	/// <summary>
	/// The layer of a STR object.
	/// </summary>
	public class StrLayer : IWriteableObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="StrLayer" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public StrLayer(IBinaryReader reader) {
			int count = reader.Int32();
			TextureNames = new List<string>(count);

			for (int i = 0; i < count; i++) {
				TextureNames.Add(reader.String(128, '\0'));
			}

			count = reader.Int32();
			KeyFrames = new List<StrKeyFrame>(count);

			for (int i = 0; i < count; i++) {
				KeyFrames.Add(new StrKeyFrame(reader));
			}
		}

		public StrLayer() {
			TextureNames = new List<string>();
			KeyFrames = new List<StrKeyFrame>();
		}

		public StrLayer(StrLayer layer) {
			TextureNames = new List<string>();
			KeyFrames = new List<StrKeyFrame>();

			for (int i = 0; i < layer.KeyFrames.Count; i++) {
				KeyFrames.Add(new StrKeyFrame(layer[i]));
			}

			if (layer.FrameIndex2KeyIndex != null) {
				FrameIndex2KeyIndex = new List<int>();

				for (int i = 0; i < layer.FrameIndex2KeyIndex.Count; i++) {
					FrameIndex2KeyIndex.Add(layer.FrameIndex2KeyIndex[i]);
				}
			}

			for (int i = 0; i < layer.TextureNames.Count; i++) {
				TextureNames.Add(layer.TextureNames[i]);
			}

			TexturesHash = layer.TexturesHash;
		}

		/// <summary>
		/// Gets or sets the texture names.
		/// </summary>
		public List<string> TextureNames { get; set; }

		/// <summary>
		/// Gets or sets the key frames.
		/// </summary>
		public List<StrKeyFrame> KeyFrames { get; set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(TextureNames.Count);

			foreach (var texture in TextureNames) {
				writer.WriteANSI(texture, 128);
			}

			writer.Write(KeyFrames.Count);

			foreach (var frame in KeyFrames) {
				frame.Write(writer);
			}
		}

		#endregion

		public override string ToString() {
			return "Texture count = " + TextureNames.Count + "; Keys = " + KeyFrames.Count;
		}

		public int TexturesHash;

		public static int GenerateTexturesHash(List<string> textures) {
			int texturesHash = 0;

			for (int i = 0; i < textures.Count; i++) {
				texturesHash += (i + 1) * textures[i].GetHashCode() * 7;
			}

			return texturesHash;
		}

		public int GenerateTexturesHash() {
			TexturesHash = 0;

			for (int i = 0; i < TextureNames.Count; i++) {
				TexturesHash += (i + 1) * TextureNames[i].GetHashCode() * 7;
			}

			return TexturesHash;
		}

		public bool IsType(int keyIndex, int type) {
			if (this[keyIndex] == null)
				return false;

			return this[keyIndex].Type == type;
		}

		public bool IsInter(int keyIndex) {
			if (this[keyIndex] == null)
				return false;

			return this[keyIndex].IsInterpolated;
		}

		public int GetNextKeyIndex(int frameIndex) {
			var baseKeyIndex = FrameIndex2KeyIndex[frameIndex];

			if (baseKeyIndex > -1) {
				if (IsType(baseKeyIndex + 1, 0))
					return baseKeyIndex + 1;

				if (IsType(baseKeyIndex + 2, 0))
					return baseKeyIndex + 2;

				if (IsType(baseKeyIndex + 3, 0))
					return baseKeyIndex + 3;

				return -1;
			}

			for (int i = frameIndex; i < FrameIndex2KeyIndex.Count; i++) {
				if (FrameIndex2KeyIndex[i] != -1)
					return FrameIndex2KeyIndex[i];
			}

			return -1;
		}

		public int GetPreviousKeyIndex(int frameIndex) {
			var baseKeyIndex = FrameIndex2KeyIndex[frameIndex];

			if (baseKeyIndex > -1) {
				if (this[baseKeyIndex].FrameIndex < frameIndex)
					return baseKeyIndex;

				if (IsType(baseKeyIndex - 1, 0))
					return baseKeyIndex - 1;

				if (IsType(baseKeyIndex - 2, 0))
					return baseKeyIndex - 2;

				if (IsType(baseKeyIndex - 3, 0))
					return baseKeyIndex - 3;

				return -1;
			}

			for (int i = frameIndex; i >= FrameIndex2KeyIndex.Count; i--) {
				if (FrameIndex2KeyIndex[i] != -1)
					return FrameIndex2KeyIndex[i];
			}

			return -1;
		}

		public void Index(int frameCount) {
			FrameIndex2KeyIndex = new List<int>();
			
			int listIdx = 0;
			int previous = -1;

			if (KeyFrames.Count > 0) {
				for (int i = 0; i < KeyFrames[0].FrameIndex; i++) {
					FrameIndex2KeyIndex.Add(-1);
					listIdx++;
				}

				for (int i = 0; i < KeyFrames.Count; i++) {
					for (int j = listIdx; j < KeyFrames[i].FrameIndex; j++) {
						FrameIndex2KeyIndex.Add(previous);
						listIdx++;
					}

					previous = i;
				}

				if (KeyFrames.Last().IsInterpolated) {
					for (int j = listIdx; j < frameCount; j++) {
						FrameIndex2KeyIndex.Add(previous);
						listIdx++;
					}
				}
				else {
					FrameIndex2KeyIndex.Add(previous);
					listIdx++;
				}
			}

			// Fill empty
			for (int j = listIdx; j < frameCount; j++) {
				FrameIndex2KeyIndex.Add(-1);
			}
		}

		public List<int> FrameIndex2KeyIndex;

		public StrKeyFrame this[int keyIndex] {
			get {
				if (keyIndex < 0 || keyIndex >= KeyFrames.Count)
					return null;

				return KeyFrames[keyIndex];
			}
		}

		public void Translate(float x, float y) {
			foreach (var frame in KeyFrames) {
				frame.Translate(x, y);
			}
		}

		public void Scale(float x, float y) {
			foreach (var frame in KeyFrames) {
				frame.Scale(x, y);
			}
		}

		public void Scale(float scale) {
			foreach (var frame in KeyFrames) {
				frame.Scale(scale);
			}
		}

		public void Rotate(float angle) {
			foreach (var frame in KeyFrames) {
				frame.Rotate(angle);
			}
		}
	}
}