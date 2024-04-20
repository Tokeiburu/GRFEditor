using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.FileFormats.StrFormat.Commands;
using GRF.IO;

namespace GRF.FileFormats.StrFormat {
	public partial class Str : IPrintable, IWriteableFile {
		/// <summary>
		/// Initializes a new instance of the <see cref="Str" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Str(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		private Str(IBinaryReader reader) {
			Header = new StrHeader(reader, this);
			Layers = new List<StrLayer>(Header.NumberOfLayers);

			for (int i = 0, count = Header.NumberOfLayers; i < count; i++) {
				Layers.Add(new StrLayer(reader));
			}

			Commands = new CommandsHolder(this);

			if (MaxKeyFrame == 0x6d617246) {
				MaxKeyFrame = 0;

				foreach (var layer in Layers) {
					foreach (var keyFrame in layer.KeyFrames) {
						MaxKeyFrame = Math.Max(MaxKeyFrame, keyFrame.FrameIndex);
					}
				}
			}
		}

		public Str() {
			Header = new StrHeader();
			Header.SetVersion(148, 0);

			Layers = new List<StrLayer>();
			Layers.Add(new StrLayer());

			Fps = 60;

			Commands = new CommandsHolder(this);
		}

		public Str(Str str) {
			Header = new StrHeader();
			Header.SetVersion(str.Header.MajorVersion, str.Header.MinorVersion);
			Layers = new List<StrLayer>(str.Layers.Count);
			LoadedPath = str.LoadedPath;

			Fps = str.Fps;
			MaxKeyFrame = str.MaxKeyFrame;

			foreach (var layer in str.Layers) {
				Layers.Add(new StrLayer(layer));
			}

			Commands = new CommandsHolder(this);
		}

		public delegate void InvalidateVisualDelegate(object sender);

		public event InvalidateVisualDelegate VisualInvalidated;
		public event InvalidateVisualDelegate VisualInvalidatedRedraw;
		
		public void OnVisualInvalidated() {
			InvalidateVisualDelegate handler = VisualInvalidated;
			if (handler != null) handler(this);
		}

		public void InvalidateVisual() {
			OnVisualInvalidated();
		}

		public void OnVisualInvalidatedRedraw() {
			InvalidateVisualDelegate handler = VisualInvalidatedRedraw;
			if (handler != null) handler(this);
		}

		public void InvalidateVisualRedraw() {
			OnVisualInvalidatedRedraw();
		}

		public int Fps { get; set; }
		public int MaxKeyFrame { get; set; }

		public int KeyFrameCount {
			get { return MaxKeyFrame + 1; }
		}

		public CommandsHolder Commands { get; private set; }

		public StrHeader Header { get; set; }
		public List<StrLayer> Layers { get; set; }

		public int NumberOfLayers {
			get { return Layers.Count; }
		}

		public List<string> Textures {
			get {
				List<string> textures = new List<string>();

				foreach (StrLayer layer in Layers) {
					textures.AddRange(layer.TextureNames);
				}

				return textures.Distinct().ToList();
			}
		}

		public string LoadedPath { get; set; }

		#region IPrintable Members

		public string GetInformation() {
			return FileFormatParser.DisplayObjectProperties(this);
		}

		#endregion

		#region IWriteableFile Members

		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "LoadedPath");
			Save(LoadedPath);
		}

		public void Save(string path) {
			using (BinaryWriter stream = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
				_save(stream);
			}
		}

		public void Save(Stream stream) {
			_save(new BinaryWriter(stream));
		}

		#endregion

		private void _save(BinaryWriter writer) {
			Header.Write(writer);
			writer.Write(Fps);
			writer.Write(MaxKeyFrame);
			writer.Write(NumberOfLayers);
			writer.Write(new byte[16]);

			foreach (var layer in Layers) {
				layer.Write(writer);
			}
		}

		public StrLayer this[int layerIndex] {
			get { return Layers[layerIndex]; }
		}

		public StrKeyFrame this[int layerIndex, int keyIndex] {
			get {
				return Layers[layerIndex].KeyFrames[keyIndex];
			}
		}

		public void Translate(float x, float y) {
			foreach (var layer in Layers) {
				layer.Translate(x, y);
			}
		}

		public void Scale(float x, float y) {
			foreach (var layer in Layers) {
				layer.Scale(x, y);
			}
		}

		public void Scale(float scale) {
			foreach (var layer in Layers) {
				layer.Scale(scale);
			}
		}

		public void Rotate(float angle) {
			foreach (var layer in Layers) {
				layer.Rotate(angle);
			}
		}
	}
}