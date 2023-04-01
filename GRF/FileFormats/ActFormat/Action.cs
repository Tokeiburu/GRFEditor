using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GRF.FileFormats.LubFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;

namespace GRF.FileFormats.ActFormat {
	[Serializable]
	public class Action : IEnumerable<Frame> {
		public float AnimationSpeed = 6;
		public List<Frame> Frames = new List<Frame>();

		public Action() {
		}

		public Action(Action action) {
			AnimationSpeed = action.AnimationSpeed;
			Frames = new List<Frame>();

			foreach (Frame frame in action.Frames) {
				Frames.Add(new Frame(frame));
			}
		}

		public int NumberOfFrames {
			get { return Frames == null ? 0 : Frames.Count; }
		}

		public int Interval {
			get { return (int) (AnimationSpeed * 25f); }
			set { AnimationSpeed = value / 25f; }
		}

		public Frame this[int frameIndex] {
			get { return Frames[frameIndex]; }
			set { Frames[frameIndex] = value; }
		}

		public Layer this[int frameIndex, int layerIndex] {
			get { return this[frameIndex][layerIndex]; }
			set { Frames[frameIndex][layerIndex] = value; }
		}

		#region IEnumerable<Frame> Members

		public IEnumerator<Frame> GetEnumerator() {
			if (Frames == null)
				return new List<Frame>().GetEnumerator();

			return Frames.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		public override int GetHashCode() {
			unchecked {
				int hashCode = AnimationSpeed.GetHashCode();
				hashCode = (hashCode * 397) ^ (Frames != null ? Frames.GetHashCode() : 0);
				return hashCode;
			}
		}

		public void Translate(int offsetX, int offsetY) {
			foreach (Frame frame in Frames) {
				foreach (Layer layer in frame.Layers) {
					layer.OffsetX += offsetX;
					layer.OffsetY += offsetY;
				}
			}
		}

		public void Translate(int frameIndex, int offsetX, int offsetY) {
			foreach (Layer layer in Frames[frameIndex].Layers) {
				layer.OffsetX += offsetX;
				layer.OffsetY += offsetY;
			}
		}

		public void Translate(int frameIndex, int layerIndex, int offsetX, int offsetY) {
			Layer layer = Frames[frameIndex].Layers[layerIndex];
			layer.OffsetX += offsetX;
			layer.OffsetY += offsetY;
		}

		public void Rotate(int rotate) {
			foreach (Frame frame in Frames) {
				frame.Rotate(rotate);
			}
		}

		public void Rotate(int frameIndex, int rotate) {
			this[frameIndex].Rotate(rotate);
		}

		public void Rotate(int frameIndex, int layerIndex, int rotate) {
			this[frameIndex, layerIndex].Rotate(rotate);
		}

		public void Scale(float scaleX, float scaleY) {
			foreach (Frame frame in Frames) {
				frame.Scale(scaleX, scaleY);
			}
		}

		public void Scale(int frameIndex, float scaleX, float scaleY) {
			Frames[frameIndex].Scale(scaleX, scaleY);
		}

		public void Scale(int frameIndex, int layerIndex, float scaleX, float scaleY) {
			Frames[frameIndex][layerIndex].Scale(scaleX, scaleY);
		}

		public void Scale(float scale) {
			Scale(scale, scale);
		}

		public void Scale(int frameIndex, float scale) {
			Scale(frameIndex, scale, scale);
		}

		public void Scale(int frameIndex, int layerIndex, float scale) {
			Scale(frameIndex, layerIndex, scale, scale);
		}

		public void Write(BinaryWriter writer, Spr sprite) {
			writer.Write(NumberOfFrames);
			Frames.ForEach(p => p.Write(writer, sprite));
		}

		public override bool Equals(object obj) {
			var action = obj as Action;
			if (action != null) {
				if (Interval == action.Interval && NumberOfFrames == action.NumberOfFrames) {
					for (int i = 0; i < NumberOfFrames; i++) {
						if (!this[i].Equals(action[i]))
							return false;
					}

					return true;
				}
			}

			return false;
		}

		public void ApplyMirror() {
			foreach (var layer in Frames) {
				layer.ApplyMirror();
			}
		}

		public void ApplyMirror(bool mirror) {
			foreach (var layer in Frames) {
				layer.ApplyMirror(mirror);
			}
		}

		public void SetColor(string color) {
			SetColor(new GrfColor(color));
		}

		public void SetColor(int frameIndex, string color) {
			SetColor(frameIndex, new GrfColor(color));
		}

		public void SetColor(int frameIndex, int layerIndex, string color) {
			SetColor(frameIndex, layerIndex, new GrfColor(color));
		}

		public void SetColor(GrfColor color) {
			AllLayers(p => p.Color = color);
		}

		public void SetColor(int frameIndex, GrfColor color) {
			this[frameIndex].SetColor(color);
		}

		public void SetColor(int frameIndex, int layerIndex, GrfColor color) {
			this[frameIndex, layerIndex].Color = color;
		}

		public void AllLayers(Action<Layer> func) {
			foreach (var frame in this) {
				foreach (var layer in frame) {
					func(layer);
				}
			}
		}

		public void Print(StringBuilder builder, int indent) {
			string toAdd = LineHelper.GenerateIndent(indent);

			builder.AppendLine("AnimationSpeed = " + AnimationSpeed + (NumberOfFrames == 0 ? ", Frames = 0" : ""));

			if (NumberOfFrames != 0) {
				builder.Append(toAdd);
				builder.AppendLine("\tFrames (" + NumberOfFrames + ")");

				for (int i = 0; i < NumberOfFrames; i++) {
					builder.Append(toAdd);
					builder.Append("\t[");
					builder.Append(i);
					builder.Append("] ");
					this[i].Print(builder, indent + 1);
					builder.AppendLine();
				}
			}
		}

		public override string ToString() {
			return String.Format("Frames = {0}", NumberOfFrames);
		}

		public Frame TryGetFrame(int frameIndex) {
			if (frameIndex >= 0 && frameIndex < NumberOfFrames) {
				return this[frameIndex];
			}

			return null;
		}

		public Layer TryGetLayer(int frameIndex, int layerIndex) {
			if (frameIndex >= 0 && frameIndex < NumberOfFrames) {
				var frame = this[frameIndex];

				if (layerIndex >= 0 && layerIndex < frame.NumberOfLayers)
					return frame[layerIndex];
			}

			return null;
		}

		public void AllFrames(Action<Frame> func) {
			foreach (Frame frame in this) {
				func(frame);
			}
		}

		public void Magnify(float value) {
			AllFrames(p => p.Magnify(value));
		}
	}
}