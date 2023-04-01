using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.Graphics;
using GRF.IO;
using Utilities;
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
		public Mesh MainMesh { get; private set; }
		public int AnimationLength { get; private set; }
		public int ShadeType { get; set; }
		public byte Alpha { get; set; }
		public BoundingBox Box { get; private set; }
		public byte[] Reserved { get; private set; }
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

		private Rsm(IBinaryReader reader) {
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
					_scaleKeyFrames.Add(new ScaleKeyFrame {
						Frame = reader.Int32(),
						Sx = reader.Float(),
						Sy = reader.Float(),
						Sz = reader.Float(),
						Data = reader.Float()
					});
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

			_uniqueTextures();
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
			using (var stream = File.Create(file)) {
				Save(stream);
			}
		}

		public void Save(Stream stream) {
			Save(new BinaryWriter(stream));
		}

		#endregion

		private void _uniqueTextures() {
			HashSet<string> textures = new HashSet<string>();

			for (int i = 0; i < Textures.Count; i++) {
				if (!textures.Add(Textures[i])) {
					Textures[i] = "Duplicate_[" + Methods.StringLimit(Textures[i], 18) + "]" + Methods.RandomString(128);
					Textures[i] = Methods.StringLimit(Textures[i], 39);
					i--;
				}
			}
		}

		private void _setParents() {
			// Bandaid, as we really want only 1 root mesh
			if (MainMeshNames.Count > 1) {
				foreach (var mesh in Meshes) {
					mesh.ParentName = "__ROOT";
				}

				Mesh root = new Mesh { Name = "__ROOT" };
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

		private void _calcBoundingBox() {
			Box = new BoundingBox();
		}

		public void CalculateBoundingBox(bool apply = true) {
			MainMesh.Calculate(Matrix4.Identity, apply);
			
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < Meshes.Count; j++) {
					Box.Max[i] = Math.Max(Box.Max[i], Meshes[j].BoundingBox.Max[i]);
					Box.Min[i] = Math.Min(Box.Min[i], Meshes[j].BoundingBox.Min[i]);
				}

				Box.Offset[i] = (Box.Max[i] + Box.Min[i]) / 2.0f;
				Box.Range[i] = (Box.Max[i] - Box.Min[i]) / 2.0f;
				Box.Center[i] = Box.Min[i] + Box.Range[i];
			}
		}

		public Dictionary<string, MeshRawData> Compile(Matrix4 matrix, int shader = -1, int flag = 0) {
			List<MeshRawData> meshesData;
			Dictionary<string, MeshRawData> allMeshData = new Dictionary<string, MeshRawData>();

			foreach (Mesh mesh in Meshes) {
				meshesData = mesh.Compile(this, matrix, shader, flag);

				foreach (MeshRawData meshData in meshesData) {
					if (allMeshData.ContainsKey(meshData.Texture)) {
						var mt = allMeshData[meshData.Texture];
						var newArray = new MeshTriangle[mt.MeshTriangles.Length + meshData.MeshTriangles.Length];
						Array.Copy(mt.MeshTriangles, newArray, mt.MeshTriangles.Length);
						Array.Copy(meshData.MeshTriangles, 0, newArray, mt.MeshTriangles.Length, meshData.MeshTriangles.Length);
						allMeshData[meshData.Texture].MeshTriangles = newArray;
					}
					else {
						allMeshData[meshData.Texture] = meshData;
					}
				}
			}

			return allMeshData;
		}

		public void Downgrade() {
			Header.SetVersion(1, 4);

			// Move model to match the bounding box at 0,0
			_calcBoundingBox();

			AnimationLength = AnimationLength * 50;

			CalculateBoundingBox(false);
			double diff = -Box.Center[1];

			for (int k = 0; k < _meshes.Count; k++) {
				var node = _meshes[k];

				for (int index = 0; index < node.Vertices.Count; index++) {
					var vertex = node.Vertices[index];
					node.Vertices[index] = new Vertex(vertex.X, -(vertex.Y + (k == 0 ? diff : 0)), vertex.Z);
					//node.Vertices[index] = new Vertex(vertex.X, -(vertex.Y + diff), vertex.Z);
				}

				//if (!(k == 0 || node.RotFrames.Count > 0)) {
				//	_meshes.RemoveAt(k);
				//	k--;
				//	continue;
				//}

				//for (int index = 0; index < node.Faces.Count; index++) {
				//	node.Faces[index].TwoSide = 1;
				//}

				for (int i = 0; i < node.RotationKeyFrames.Count; i++) {
					node.RotationKeyFrames[i] = new RotKeyFrame {
						Frame = node.RotationKeyFrames[i].Frame * 50,
						Quaternion = new TkQuaternion(
							-node.RotationKeyFrames[i].Quaternion.X,
							node.RotationKeyFrames[i].Quaternion.Y,
							-node.RotationKeyFrames[i].Quaternion.Z,
							node.RotationKeyFrames[i].Quaternion.W)
					};
				}

				for (int i = 0; i < node.PosKeyFrames.Count; i++) {
					node.PosKeyFrames[i] = new PosKeyFrame {
						Frame = node.PosKeyFrames[i].Frame * 50,
						X = node.PosKeyFrames[i].X,
						Y = -node.PosKeyFrames[i].Y,
						Z = node.PosKeyFrames[i].Z,
					};
				}

				foreach (var face in node.Faces) {
					var temp = face.VertexIds[1];
					face.VertexIds[1] = face.VertexIds[2];
					face.VertexIds[2] = temp;
				
					temp = face.TextureVertexIds[1];
					face.TextureVertexIds[1] = face.TextureVertexIds[2];
					face.TextureVertexIds[2] = temp;
				}

				node.Position = new Vertex(node.Position_[0], -node.Position_[1], node.Position_[2]);
				node.Position_ = new Vertex(0, 0, 0);
			}

			_downgradeSub(null, MainMesh);

			for (int k = 0; k < _meshes.Count; k++) {
				var node = _meshes[k];

				if (node == MainMesh) {
					
				}
				else if (node.Parent == MainMesh) {
					double scaleY = new Vertex(node.TransformationMatrix[3], node.TransformationMatrix[4], node.TransformationMatrix[5]).Length;
					node.Position = new Vertex(node.Position[0], -(-node.Position[1] + diff * scaleY), node.Position[2]);
					Z.F();
				}
				else {
					
				}
			}

			Reserved = new byte[16];
		}

		private void _downgradeSub(Mesh parent, Mesh child) {
			if (parent != null) {
				//if (child.Name.Contains("_e_05")) {
				//	Z.F();
				//}
				//Matrix4 m = new Matrix4(parent.OffsetMatrix);
				//m = Matrix4.Multiply(m, new Matrix4(child.OffsetMatrix));
				//child.OffsetMatrix[0] = m[0];
				//child.OffsetMatrix[1] = m[1];
				//child.OffsetMatrix[2] = m[2];
				//child.OffsetMatrix[3] = m[4];
				//child.OffsetMatrix[4] = m[5];
				//child.OffsetMatrix[5] = m[6];
				//child.OffsetMatrix[6] = m[8];
				//child.OffsetMatrix[7] = m[9];
				//child.OffsetMatrix[8] = m[10];
			}

			foreach (var node in child.Children) {
				_downgradeSub(child, node);
			}

			if (parent != null) {
				child.Position -= parent.Position;
			}
		}

		public void ClearBuffers() {
			foreach (var mesh in Meshes) {
				mesh.ClearBuffer();
			}
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