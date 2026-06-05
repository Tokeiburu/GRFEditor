using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.Image;
using GRF.IO;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.StrFormat {
	/// <summary>
	/// The layer of a STR object.
	/// </summary>
	public class StrLayer : IWriteableObject {
		public Str StrSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="StrLayer" /> class.
		/// </summary>
		/// <param name="str">The STR source.</param>
		/// <param name="reader">The reader.</param>
		public StrLayer(Str str, IBinaryReader reader) {
			StrSource = str;
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

		public StrLayer(Str str) {
			StrSource = str;
			TextureNames = new List<string>();
			KeyFrames = new List<StrKeyFrame>();
		}

		public StrLayer(StrLayer layer) {
			StrSource = layer.StrSource;
			TextureNames = new List<string>();
			KeyFrames = new List<StrKeyFrame>();

			for (int i = 0; i < layer.KeyFrames.Count; i++) {
				KeyFrames.Add(new StrKeyFrame(layer[i]));
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

			for (int i = frameIndex; i < FrameIndex2KeyIndex.Length; i++) {
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

			for (int i = frameIndex; i >= 0; i--) {
				if (FrameIndex2KeyIndex[i] != -1)
					return FrameIndex2KeyIndex[i];
			}

			return -1;
		}

		public int GetKeyFrameLength(int keyIndex) {
			var keyFrame = this[keyIndex];
			var nextKeyFrame = this[keyIndex + 1];

			if (keyFrame == null)
				return 0;

			if (nextKeyFrame != null)
				return nextKeyFrame.FrameIndex - keyFrame.FrameIndex;

			if (keyFrame.IsInterpolated)
				return StrSource.KeyFrameCount - keyFrame.FrameIndex;

			return 1;
		}

		public unsafe void RebuildIndex() {
			int frameCount = StrSource.KeyFrameCount;

			_frameIndexToKeyIndex = new int[frameCount];

			fixed (int* pDstBase = _frameIndexToKeyIndex) {
				int* pDst = pDstBase;
				NativeMethods.memset((IntPtr)pDst, -1, (UIntPtr)(frameCount * sizeof(int)));

				int listIdx = 0;
				int previous = 0;
				
				if (KeyFrames.Count > 0) {
					listIdx += KeyFrames[0].FrameIndex;

					for (int i = 1; i < KeyFrames.Count; i++) {
						int count = KeyFrames[i].FrameIndex - listIdx;

						if (count == 1) {
							pDst[listIdx] = previous;
						}
						else {
							for (int j = listIdx; j < KeyFrames[i].FrameIndex; j++) {
								pDst[j] = previous;
							}
						}

						listIdx += count;
						previous = i;
					}

					if (KeyFrames.Last().IsInterpolated) {
						for (int j = listIdx; j < frameCount; j++) {
							pDst[j] = previous;
						}
					}
					else {
						pDst[listIdx] = previous;
					}
				}
			}
		}

		private int[] _frameIndexToKeyIndex;
		private bool _indexDirty = true;
		public bool FrameIndexDirty => _indexDirty;

		public int[] FrameIndex2KeyIndex {
			get {
				EnsureIndexed();
				return _frameIndexToKeyIndex;
			}
		}

		private void EnsureIndexed() {
			if (!_indexDirty)
				return;

			RebuildIndex();
			_indexDirty = false;
		}

		public void InvalidateIndex() {
			_indexDirty = true;
		}

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

		public enum RangeSearchMode {
			Intersect,
			Contained
		}

		public List<int> GetKeyIndexesInRange(int frameIndexStart, int frameCount, RangeSearchMode mode = RangeSearchMode.Intersect) {
			int frameIndexEnd = frameIndexStart + frameCount;

			if (frameIndexStart < 0)
				frameIndexStart = 0;

			if (frameIndexEnd > StrSource.KeyFrameCount)
				frameIndexEnd = StrSource.KeyFrameCount;

			List<int> indexes = new List<int>();

			if (frameIndexEnd - frameIndexStart == 0)
				return indexes;

			EnsureIndexed();
			var frameIndex2KeyIndex = _frameIndexToKeyIndex;

			int keyIndex = frameIndex2KeyIndex[frameIndexStart];

			if (keyIndex == -1) {
				for (int i = frameIndexStart; i < frameIndex2KeyIndex.Length && keyIndex == -1; i++) {
					keyIndex = frameIndex2KeyIndex[i];
				}
			}

			if (keyIndex == -1)
				return indexes;

			for (int i = keyIndex; i < KeyFrames.Count; i++) {
				if (KeyFrames[i].FrameIndex < frameIndexEnd)
					indexes.Add(i);
				else
					break;
			}

			if (mode == RangeSearchMode.Contained) {
				if (indexes.Count > 0 && KeyFrames[indexes[0]].FrameIndex < frameIndexStart)
					indexes.RemoveAt(0);
			}

			return indexes;
		}
	}
}