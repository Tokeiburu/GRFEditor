using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GRF.FileFormats.LubFormat;
using GRF.FileFormats.SprFormat;
using GRF.Graphics;
using GRF.Image;

namespace GRF.FileFormats.ActFormat {
	[Serializable]
	public class Frame : IEnumerable<Layer> {
		public List<Layer> Layers = new List<Layer>();
		private List<Anchor> _anchors = new List<Anchor>();
		private int _soundId = -1;

		public Frame() {
			Layers = new List<Layer>();
		}

		public Frame(Frame frame) {
			SoundId = frame.SoundId;
			Layers = new List<Layer>();

			foreach (Layer layer in frame.Layers) {
				Layers.Add(new Layer(layer));
			}

			foreach (Anchor anchor in frame.Anchors) {
				Anchors.Add(new Anchor(anchor));
			}
		}

		public int NumberOfLayers {
			get { return Layers == null ? 0 : Layers.Count; }
		}

		public int SoundId {
			get { return _soundId; }
			set { _soundId = value; }
		}

		public List<Anchor> Anchors {
			get { return _anchors; }
			set { _anchors = value; }
		}

		public Layer this[int layerIndex] {
			get { return Layers[layerIndex]; }
			set { Layers[layerIndex] = value; }
		}

		#region IEnumerable<Layer> Members

		public IEnumerator<Layer> GetEnumerator() {
			if (Layers == null)
				return new List<Layer>().GetEnumerator();

			return Layers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		public override int GetHashCode() {
			unchecked {
				int hashCode = (Layers != null ? Layers.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ _soundId;
				hashCode = (hashCode * 397) ^ (_anchors != null ? _anchors.GetHashCode() : 0);
				return hashCode;
			}
		}

		public void Rotate(int rotate) {
			foreach (Layer layer in Layers) {
				layer.Rotate(rotate);
			}
		}

		public void Rotate(int layerIndex, int rotate) {
			this[layerIndex].Rotate(rotate);
		}

		public void Rotate(int angle, Point center) {
			foreach (Layer layer in Layers) {
				Vertex pt = new Vertex(layer.OffsetX, layer.OffsetY);
				pt.RotateZ(-angle, center);
				layer.Rotate(angle);
				layer.OffsetX = (int) pt.X;
				layer.OffsetY = (int) pt.Y;
			}
		}

		public void Scale(float scale) {
			Scale(scale, scale);
		}

		public void Scale(int layerIndex, float scale) {
			Scale(layerIndex, scale, scale);
		}

		public void Scale(float scaleX, float scaleY) {
			foreach (Layer layer in Layers) {
				layer.Scale(scaleX, scaleY);
			}
		}

		public void Scale(int layerIndex, float scaleX, float scaleY) {
			this[layerIndex].Scale(scaleX, scaleY);
		}

		public void Translate(int offsetX, int offsetY) {
			foreach (Layer layer in Layers) {
				layer.Translate(offsetX, offsetY);
			}
		}

		public void Translate(int layerIndex, int offsetX, int offsetY) {
			this[layerIndex].Translate(offsetX, offsetY);
		}

		public void Write(BinaryWriter writer, Spr sprite) {
			writer.Write(new byte[32]);
			writer.Write(NumberOfLayers);
			Layers.ForEach(p => p.Write(writer, sprite));

			writer.Write(SoundId);
			writer.Write(Anchors.Count);

			foreach (var pass in Anchors) {
				writer.Write(pass.Unknown);
				writer.Write(pass.OffsetX);
				writer.Write(pass.OffsetY);
				writer.Write(pass.Other);
			}
		}

		public override bool Equals(object obj) {
			var frame = obj as Frame;

			if (frame != null) {
				if (SoundId == frame.SoundId && Anchors.Count == frame.Anchors.Count && NumberOfLayers == frame.NumberOfLayers) {
					for (int i = 0; i < Anchors.Count; i++) {
						if (!Anchors[i].Equals(frame.Anchors[i])) {
							return false;
						}
					}

					for (int i = 0; i < NumberOfLayers; i++) {
						if (!Layers[i].Equals(frame.Layers[i])) {
							return false;
						}
					}

					return true;
				}
			}

			return false;
		}

		public void ApplyMirror() {
			foreach (Layer layer in Layers) {
				layer.ApplyMirror();
			}
		}

		public void ApplyMirror(bool mirror) {
			foreach (Layer layer in Layers) {
				layer.ApplyMirror(mirror);
			}
		}

		public void SetColor(string color) {
			SetColor(new GrfColor(color));
		}

		public void SetColor(int layerIndex, string color) {
			SetColor(layerIndex, new GrfColor(color));
		}

		public void SetColor(GrfColor color) {
			foreach (var layer in this) {
				layer.Color = color;
			}
		}

		public void SetColor(int layerIndex, GrfColor color) {
			this[layerIndex].Color = color;
		}

		public override string ToString() {
			return String.Format("Layers = {0}", NumberOfLayers);
		}

		public void Print(StringBuilder builder, int indent) {
			builder.AppendLine("SoundId = " + SoundId + (Anchors.Count == 0 ? ", Anchors = 0" : "") + (NumberOfLayers == 0 ? ", Layers = 0" : ""));
			string toAdd = LineHelper.GenerateIndent(indent);

			if (Anchors.Count != 0) {
				builder.Append(toAdd);
				builder.AppendLine("\tAnchors");

				foreach (Anchor anchor in Anchors) {
					builder.Append(toAdd);
					builder.Append("\t");
					builder.AppendLine(anchor.ToString());
				}
			}

			if (NumberOfLayers != 0) {
				builder.Append(toAdd);
				builder.AppendLine("\tLayers");

				for (int i = 0; i < NumberOfLayers; i++) {
					builder.Append(toAdd);
					builder.Append("\t[");
					builder.Append(i);
					builder.Append("] ");
					builder.AppendLine(this[i].ToString());
				}
			}
		}

		public Layer TryGetLayer(int layerIndex) {
			if (layerIndex < NumberOfLayers)
				return this[layerIndex];

			return null;
		}

		public void Magnify(float value) {
			Layers.ForEach(p => p.Magnify(value));
		}
	}
}