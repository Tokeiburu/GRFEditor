using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Graphics;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.RsmFormat {
	public class Rsm : IPrintable, IWriteableFile {
		public const string RsmTexturePath = @"data\texture";
		public const string RsmModelPath = @"data\model";

		/// <summary>
		/// Gets or sets the loaded file path of this object.
		/// </summary>
		public string LoadedPath { get; set; }

		public RsmHeader Header { get; private set; }
		public List<string> MainMeshNames = new List<string>();
		private readonly List<ScaleKeyFrame> _scaleKeyFrames = new List<ScaleKeyFrame>();
		public Mesh MainMesh { get; set; }
		public int AnimationLength { get; set; }
		public int ShadeType { get; set; }
		public byte Alpha { get; set; }
		public BoundingBox Box { get; private set; }
		public byte[] Reserved { get; internal set; }
		public float FrameRatePerSecond { get; set; }
		private readonly List<Mesh> _meshes = new List<Mesh>();
		private readonly List<Mesh> _parents = new List<Mesh>();
		private readonly List<string> _textures = new List<string>();
		private readonly List<VolumeBox> _volumeBoxes = new List<VolumeBox>();

		public double Version {
			get { return Header.Version; }
		}

		public List<Mesh> Meshes {
			get { return _meshes; }
		}

		public List<VolumeBox> VolumeBoxes {
			get { return _volumeBoxes; }
		}

		public List<string> Textures {
			get { return _textures; }
		}

		public List<ScaleKeyFrame> ScaleKeyFrames {
			get { return _scaleKeyFrames; }
		}

		public Rsm(IBinaryReader reader) {
			int count;

			Header = new RsmHeader(reader);
			AnimationLength = reader.Int32();
			ShadeType = reader.Int32();
			Alpha = 0xFF;

			if (Version >= 1.4) {
				Alpha = reader.Byte();
			}

			if (Version >= 2.3) {
				FrameRatePerSecond = reader.Float();
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					MainMeshNames.Add(reader.String(reader.Int32(), '\0'));
				}

				count = reader.Int32();
			}
			else if (Version >= 2.2) {
				FrameRatePerSecond = reader.Float();
				int numberOfTextures = reader.Int32();

				for (int i = 0; i < numberOfTextures; i++) {
					_textures.Add(reader.String(reader.Int32(), '\0'));
				}

				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					MainMeshNames.Add(reader.String(reader.Int32(), '\0'));
				}

				count = reader.Int32();
			}
			else {
				Reserved = reader.Bytes(16);
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					_textures.Add(reader.String(40, '\0'));
				}

				MainMeshNames.Add(reader.String(40, '\0'));
				count = reader.Int32();
			}

			for (int i = 0; i < count; i++) {
				_meshes.Add(new Mesh(this, reader));
			}

			// Resolve parent/child associations
			if (MainMeshNames.Count == 0) {
				MainMeshNames.Add(_meshes[0].Name);
			}

			MainMesh = _meshes.FirstOrDefault(m => m.Name == MainMeshNames[0]) ?? _meshes[0];

			_setParents();

			foreach (Mesh mesh in _meshes) {
				if (String.IsNullOrEmpty(mesh.ParentName)) {
					_parents.Add(mesh);
				}
				else {
					var meshParent = _meshes.FirstOrDefault(p => p.Name == mesh.ParentName);

					if (meshParent == null) {
						// no parent
						_parents.Add(mesh);
					}
					else {
						mesh.Parent = meshParent;
					}
				}
			}

			if (Version < 1.6) {
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					_scaleKeyFrames.Add(new ScaleKeyFrame(reader));
				}
			}

			count = reader.CanRead ? reader.Int32() : 0;

			if (Version >= 1.3) {
				for (int i = 0; i < count; i++) {
					VolumeBoxes.Add(new VolumeBox(reader));
				}
			}
			else {
				for (int i = 0; i < count; i++) {
					VolumeBoxes.Add(new VolumeBox(reader, true));
				}
			}

			Box = new BoundingBox();
		}

		public Rsm(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

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

		public void Save(string file) {
			GrfPath.CreateDirectoryFromFile(file);

			using (var stream = File.Create(file)) {
				Save(stream);
			}
		}

		public void Save(Stream stream) {
			Save(new BinaryWriter(stream));
		}

		#endregion

		private void _setParents() {
			// Bandaid, as we really want only 1 root mesh
			if (MainMeshNames.Count > 1) {
				foreach (var mesh in Meshes) {
					mesh.ParentName = "__ROOT";
				}

				Mesh root = new Mesh(this) { Name = "__ROOT" };
				MainMesh = root;
				Meshes.Add(root);
			}

			// Fix : 2015-04-23
			// Sets the parents in each mesh by using their references.
			foreach (var mesh in Meshes) {
				// No parent, they are ignored
				if (String.IsNullOrEmpty(mesh.ParentName) || mesh == MainMesh) {
					continue;
				}

				List<Mesh> parents = Meshes.Where(p => p.Name == mesh.ParentName && mesh != p).ToList();

				if (parents.Count == 0) continue;
				mesh.Parent = parents[0];
				parents[0].Children.Add(mesh);
			}
		}

		public void Downgrade() {
			Rsm2Converter.Downgrade(this);
		}

		public void Flatten() {
			Rsm2Converter.Flatten(this);
		}

		internal void Save(BinaryWriter writer) {
			Header.Write(writer);
			writer.Write(AnimationLength);
			writer.Write(ShadeType);

			if (Version >= 1.4) {
				writer.Write(Alpha);
			}

			if (Version >= 2.3) {
				writer.Write(FrameRatePerSecond);
				writer.Write(MainMeshNames.Count);

				foreach (var name in MainMeshNames) {
					writer.Write(name.Length);
					writer.WriteANSI(name, name.Length);
				}

				writer.Write(_meshes.Count);
			}
			else if (Version >= 2.2) {
				writer.Write(FrameRatePerSecond);
				writer.Write(_textures.Count);

				foreach (string texture in _textures) {
					writer.Write(texture.Length);
					writer.WriteANSI(texture, texture.Length);
				}

				writer.Write(MainMeshNames.Count);

				foreach (var name in MainMeshNames) {
					writer.Write(name.Length);
					writer.WriteANSI(name, name.Length);
				}

				writer.Write(_meshes.Count);
			}
			else {
				if (Reserved == null || Reserved.Length != 16) {
					Reserved = new byte[16];
				}

				writer.Write(Reserved);
				writer.Write(_textures.Count);

				foreach (string texture in _textures) {
					writer.WriteANSI(texture, 40);
				}

				writer.WriteANSI(MainMeshNames[0], 40);
				writer.Write(_meshes.Count);
			}

			foreach (var mesh in _meshes) {
				mesh.Write(writer);
			}

			if (Version < 1.6) {
				writer.Write(ScaleKeyFrames.Count);

				for (int i = 0; i < ScaleKeyFrames.Count; i++) {
					ScaleKeyFrames[i].Write(writer);
				}
			}

			int count = VolumeBoxes.Count;
			writer.Write(count);

			if (Version >= 1.3) {
				for (int i = 0; i < count; i++) {
					VolumeBoxes[i].Write(writer, false);
				}
			}
			else {
				for (int i = 0; i < count; i++) {
					VolumeBoxes[i].Write(writer, true);
				}
			}
		}
	}
}