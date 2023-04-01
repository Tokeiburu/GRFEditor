using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.Graphics;
using GRF.IO;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.RsmFormat {
	public class Mesh : IWriteableFile {
		private Matrix3 _offsetMT = new Matrix3();
		private readonly List<ScaleKeyFrame> _scaleKeyFrames = new List<ScaleKeyFrame>();
		private readonly List<RotKeyFrame> _rotFrames = new List<RotKeyFrame>();
		private readonly List<PosKeyFrame> _posKeyFrames = new List<PosKeyFrame>();
		private readonly TextureKeyFrameGroup _textureKeyFrameGroup = new TextureKeyFrameGroup();
		private readonly List<int> _textureIndexes = new List<int>();
		private readonly List<TextureVertex> _tvertices = new List<TextureVertex>();
		private List<Vertex> _vertices = new List<Vertex>();
		private readonly List<Vertex> _verticesOriginal = new List<Vertex>();
		public BoundingBox BoundingBox = new BoundingBox();
		public HashSet<Mesh> Children = new HashSet<Mesh>();
		public Matrix4 Matrix = Matrix4.Identity;
		public Vertex Flip;
		public Mesh Parent;
		public Vertex Position;
		public Vertex Position_;
		public Vertex Position_V = new Vertex();
		public float RotAngle;
		public Vertex RotAxis;
		public Vertex Scale;
		private List<Face> _faces = new List<Face>();
		public Matrix4 MeshMatrix;
		public Matrix4 MeshMatrixSelf;
		private UInt16 _textOffset;
		private TkQuaternion? _bufferedRot;
		private Vertex? _bufferedScale;
		private Vertex? _bufferedPos;
		private Dictionary<int, float> _bufferedTextureOffset = new Dictionary<int, float>();
		public List<string> Textures = new List<string>();

		public List<int> TextureIndexes {
			get { return _textureIndexes; }
		}

		public Rsm Model { get; private set; }
		public string Name { get; set; }
		public string ParentName { get; set; }

		/// <summary>
		/// Gets or sets the loaded file path of this object.
		/// </summary>
		public string LoadedPath { get; set; }

		public Matrix3 OffsetMatrix {
			get { return _offsetMT; }
			set { _offsetMT = value; }
		}

		public List<Vertex> Vertices {
			get { return _vertices; }
		}

		public List<TextureVertex> TextureVertices {
			get { return _tvertices; }
		}

		public List<Face> Faces {
			get { return _faces; }
			set { _faces = value; }
		}

		public List<ScaleKeyFrame> ScaleKeyFrames {
			get { return _scaleKeyFrames; }
		}

		public List<RotKeyFrame> RotFrames {
			get { return _rotFrames; }
		}

		public List<PosKeyFrame> PosKeyFrames {
			get { return _posKeyFrames; }
		}

		public TextureKeyFrameGroup TextureKeyFrameGroup {
			get { return _textureKeyFrameGroup; }
		}

		public Mesh(Mesh mesh) {
			_offsetMT = new Matrix3(mesh._offsetMT);

			foreach (var skf in mesh._scaleKeyFrames) {
				_scaleKeyFrames.Add(new ScaleKeyFrame(skf));
			}

			foreach (var rf in mesh._rotFrames) {
				_rotFrames.Add(new RotKeyFrame(rf));
			}

			foreach (var psk in mesh._posKeyFrames) {
				_posKeyFrames.Add(new PosKeyFrame(psk));
			}

			_textureIndexes = new List<int>(mesh._textureIndexes);
			_tvertices = new List<TextureVertex>(mesh._tvertices);
			_vertices = new List<Vertex>(mesh._vertices);
			_verticesOriginal = new List<Vertex>(mesh._verticesOriginal);

			foreach (var child in mesh.Children) {
				Children.Add(new Mesh(child));
			}

			Parent = mesh.Parent;
			Position = mesh.Position;
			Position_ = mesh.Position_;
			RotAngle = mesh.RotAngle;
			RotAxis = mesh.RotAxis;
			Scale = mesh.Scale;

			foreach (var f in mesh._faces) {
				_faces.Add(new Face(f));
			}

			Model = mesh.Model;
			Name = mesh.Name;
			ParentName = mesh.ParentName;
		}

		public Mesh(Rsm rsm, IBinaryReader data, int majorVersion, int minorVersion) {
			int amount;

			Model = rsm;

			if (rsm.Header.IsCompatibleWith(2, 2)) {
				Name = data.String(data.Int32(), '\0');
				ParentName = data.String(data.Int32(), '\0');
			}
			else {
				Name = data.String(40, '\0');
				ParentName = data.String(40, '\0');
			}

			if (rsm.Header.IsCompatibleWith(2, 3)) {
				amount = data.Int32();

				for (int i = 0; i < amount; i++) {
					Textures.Add(data.String(data.Int32(), '\0'));
				}

				_textureIndexes.Capacity = amount;

				for (int i = 0; i < amount; i++) {
					_textureIndexes.Add(i);
				}
			}
			else {
				_textureIndexes.Capacity = amount = data.Int32();

				for (int i = 0; i < amount; i++) {
					_textureIndexes.Add(data.Int32());
				}
			}

			for (int i = 0; i < 9; i++) {
				_offsetMT[i] = data.Float();
			}

			Position_ = new Vertex(data);

			if (rsm.Header.IsCompatibleWith(2, 2)) {
				Position = new Vertex(0, 0, 0);
				RotAngle = 0;
				RotAxis = new Vertex(0, 0, 0);
				//Scale = new Vertex(1, -1, 1);
				Scale = new Vertex(1, 1, 1);
				Flip = new Vertex(1, -1, 1);
			}
			else {
				Position = new Vertex(data);
				RotAngle = data.Float();
				RotAxis = new Vertex(data);
				Scale = new Vertex(data);
				Flip = new Vertex(1, 1, 1);
			}

			_vertices.Capacity = amount = data.Int32();

			for (int i = 0; i < amount; i++) {
				_vertices.Add(new Vertex(data));
				_verticesOriginal.Add(_vertices[i]);
			}

			_tvertices.Capacity = amount = data.Int32();

			for (int i = 0; i < amount; i++) {
				if (majorVersion > 1 || (majorVersion == 1 && minorVersion >= 2)) {
					_tvertices.Add(new TextureVertex(data));
				}
				else {
					_tvertices.Add(new TextureVertex(data, 0xFFFFFFFF));
				}
			}

			_faces.Capacity = amount = data.Int32();

			for (int i = 0; i < amount; i++) {
				if (majorVersion > 1 || (majorVersion == 1 && minorVersion >= 2)) {
					_faces.Add(new Face(rsm.Header, data));
				}
				else {
					_faces.Add(new Face(rsm.Header, data, 0));
				}
			}

			if (majorVersion > 1 || (majorVersion == 1 && minorVersion >= 6)) {
				_scaleKeyFrames.Capacity = amount = data.Int32();

				for (int i = 0; i < amount; i++) {
					_scaleKeyFrames.Add(new ScaleKeyFrame(data));
				}
			}

			_rotFrames.Capacity = amount = data.Int32();

			for (int i = 0; i < amount; i++) {
				_rotFrames.Add(new RotKeyFrame(data));
			}

			if (rsm.Header.IsCompatibleWith(2, 3)) {
				_posKeyFrames.Capacity = amount = data.Int32();

				for (int i = 0; i < amount; i++) {
					_posKeyFrames.Add(new PosKeyFrame(data));
				}

				amount = data.Int32();

				if (amount > 0) {
					for (int i = 0; i < amount; i++) {
						int textureId = data.Int32(); // target texture
						int amountTextureAnimations = data.Int32();

						for (int j = 0; j < amountTextureAnimations; j++) {
							int type = data.Int32();	// u, or v
							int amountFrames = data.Int32();

							for (int k = 0; k < amountFrames; k++) {
								_textureKeyFrameGroup.AddTextureKeyFrame(textureId, type, new TextureKeyFrame(data));
							}
						}
					}
				}
			}
			else if (rsm.Header.IsCompatibleWith(2, 2)) {
				// ?? Unknown
				amount = data.Int32();

				if (amount > 0) {
					data.Forward(20 * amount);
				}
			}

			_uniqueTextures();
		}

		public Mesh(Rsm rsm, string file) : this(rsm, new ByteReader(file, 6), 1, 4) {
			LoadedPath = file;
		}

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
			var rsm = Model;
			HashSet<string> textures = new HashSet<string>();

			for (int i = 0; i < rsm.Textures.Count; i++) {
				if (!textures.Add(rsm.Textures[i])) {
					var newTextureIndex = -1;

					for (int j = 0; j < i; j++) {
						//if (textures.Contains(rsm.Textures[i])) {
						if (String.CompareOrdinal(rsm.Textures[j], rsm.Textures[i]) == 0) {
							// Make sure the texture is never used
							newTextureIndex = j;
							break;
						}
					}

					if (newTextureIndex < 0) { // Should never happen
						newTextureIndex = 0;
					}

					if (_textureIndexes.Contains(newTextureIndex)) {
						newTextureIndex = _textureIndexes.IndexOf(newTextureIndex);
					}
					else {
						_textureIndexes.Add(newTextureIndex);
						newTextureIndex = _textureIndexes.Count - 1;
					}

					for (int k = 0; k < Faces.Count; k++) {
						if (GetAbsoluteTextureId(Faces[k].TextureId) == i)
							Faces[k].TextureId = (ushort) newTextureIndex;
					}
				}
			}
		}

		// Compilation for RSM2 only
		public void Calc(Rsm rsm, int animationFrame) {
			MeshMatrix = Matrix4.Identity;
			MeshMatrixSelf = Matrix4.Identity;
			Position_V = new Vertex(0, 0, 0);

			if (Parent != null && Parent == rsm.MainNode) {
				MeshMatrixSelf = Matrix4.Multiply(MeshMatrixSelf, new Matrix4(Parent.OffsetMatrix));
			}

			if (_scaleKeyFrames.Count > 0) {
				Vertex scale = GetScale(animationFrame);
				MeshMatrixSelf = Matrix4.Scale(MeshMatrixSelf, scale);
			}

			if (_rotFrames.Count > 0) {
				MeshMatrix = Matrix4.Rotate(MeshMatrix, GetRotQuaternion(animationFrame));
				MeshMatrixSelf = Matrix4.Rotate(MeshMatrixSelf, GetRotQuaternion(animationFrame));

				TkQuaternion quat = RotFrames[0].Quaternion;
				quat.Invert();
				MeshMatrix = Matrix4.Rotate(MeshMatrix, quat);
			}

			if (_posKeyFrames.Count > 0) {
				if (Parent != null) {
					Vertex position = Matrix4.Multiply(Parent.MeshMatrixSelf, GetPosition(animationFrame));
					Position_ = Parent.Position_ + position;
				}
				else {
					Position_ = GetPosition(animationFrame);
				}
			}
			else if (Parent != null) {
				Position_V = Position_ - Parent.Position_;

				Matrix4 mat2 = Matrix4.Identity;

				mat2 = Matrix4.Multiply2(mat2, Parent.MeshMatrix);
				mat2.SelfTranslate(Position_V);

				Position_V = mat2.Offset - Position_V;
				Position_V += Parent.Position_V;

				if (Parent.PosKeyFrames.Count > 0 && Parent == rsm.MainNode) {
					Position_V += Parent.GetPosition(animationFrame);
				}
			}

			if (Parent != null) {
				MeshMatrix = Matrix4.Multiply2(MeshMatrix, Parent.MeshMatrix);

				if (this.RotFrames.Count == 0) {
					MeshMatrixSelf = Matrix4.Multiply2(MeshMatrixSelf, Parent.MeshMatrix);
				}
				else {
					MeshMatrixSelf = Matrix4.Multiply2(MeshMatrixSelf, Parent.MeshMatrixSelf);
				}
			}

			foreach (var child in Children) {
				child.Calc(rsm, animationFrame);
			}
		}

		public void Calculate(Matrix4 parentMatrix, bool apply) {
			// Calculate from children to parent.
			Vertex vertex = new Vertex();

			if (Children.Count == 0 || parentMatrix == null) {
				Matrix = Matrix4.Identity;
				Matrix.Offset = Position_;	// Affected by scale
				Matrix = Matrix4.Scale(Matrix, Scale);
				//if (Flip.Y == -1) {
				//	Matrix = Matrix4.RotateZ(Matrix, (float)(180f * (Math.PI / 180f)));
				//}
				Matrix = Matrix4.Scale(Matrix, Flip);
				if (RotFrames.Count > 0) {
					Matrix = Matrix4.RotateQuat(Matrix, RotFrames[0]);
				}
				else {
					Matrix = Matrix4.Rotate2(Matrix, RotAxis, RotAngle);
				}
				Matrix = Matrix4.Multiply(Matrix, new Matrix4(OffsetMatrix));

				// Apply changes to the vertices
				Vertex vert3;

				List<Vertex> vertices;

				if (apply) {
					vertices = Vertices;
				}
				else {
					vertices = new List<Vertex>(Vertices.Count);
					vertices.AddRange(Vertices);
				}

				for (int i = 0; i < vertices.Count; i++) {
					vert3 = vertices[i];

					vertex[0] = Matrix[0] * vert3[0] + Matrix[4] * vert3[1] + Matrix[8] * vert3[2] + Matrix[12];
					vertex[1] = Matrix[1] * vert3[0] + Matrix[5] * vert3[1] + Matrix[9] * vert3[2] + Matrix[13];
					vertex[2] = Matrix[2] * vert3[0] + Matrix[6] * vert3[1] + Matrix[10] * vert3[2] + Matrix[14];

					vertices[i] = vertex;
				}

				// Reset the matrix...!
				Matrix = Matrix4.Identity;
				Matrix = Matrix4.Multiply(Matrix, _retrieveParentsMatrix());

				Matrix4 matrix2 = new Matrix4(Matrix);

				foreach (Vertex vert in vertices) {
					vertex[0] = matrix2[0] * vert[0] + matrix2[4] * vert[1] + matrix2[8] * vert[2] + matrix2[12];
					vertex[1] = matrix2[1] * vert[0] + matrix2[5] * vert[1] + matrix2[9] * vert[2] + matrix2[13];
					vertex[2] = matrix2[2] * vert[0] + matrix2[6] * vert[1] + matrix2[10] * vert[2] + matrix2[14];

					for (int j = 0; j < 3; j++) {
						BoundingBox.Min[j] = Math.Min(vertex[j], BoundingBox.Min[j]);
						BoundingBox.Max[j] = Math.Max(vertex[j], BoundingBox.Max[j]);
					}
				}

				for (int i = 0; i < 3; i++) {
					BoundingBox.Offset[i] = (BoundingBox.Max[i] + BoundingBox.Min[i]) / 2.0f;
					BoundingBox.Range[i] = (BoundingBox.Max[i] - BoundingBox.Min[i]) / 2.0f;
					BoundingBox.Center[i] = BoundingBox.Min[i] + BoundingBox.Range[i];
				}
			}
			else {
				foreach (var child in Children) {
					child.Calculate(parentMatrix, apply);
				}

				Calculate(null, apply);
			}
		}

		private Matrix4 _retrieveParentsMatrix() {
			if (Parent == null) {
				return Matrix4.BufferedIdentity;
			}

			CalculateMeshMatrix();

			if (Parent.MeshMatrix != null)
				return Matrix4.Translate(Parent.MeshMatrix, Position);

			return Parent.MeshMatrix;
		}

		public void CalculateMeshMatrix() {
			Matrix4 parent;

			if (Parent != null) {
				Parent.CalculateMeshMatrix();
				parent = Parent.MeshMatrix;
			}
			else {
				parent = Matrix4.BufferedIdentity;
			}

			if (Children.Count == 0)
				return;

			MeshMatrix = Matrix4.Identity;

			MeshMatrix = Matrix4.Scale(MeshMatrix, Scale);
			MeshMatrix = Matrix4.Rotate2(MeshMatrix, RotAxis, RotAngle);

			if (Parent != null)
				MeshMatrix = Matrix4.Translate(MeshMatrix, Position);

			MeshMatrix = Matrix4.Multiply(MeshMatrix, parent);
		}

		public List<MeshRawData> Compile(Rsm rsm, Matrix4 instance, int forceShader = -1, int flag = 0) {
			if (forceShader != -1) {
				rsm.ShadeType = forceShader;
			}

			Matrix4 matrix = Matrix4.Identity;
			List<Vertex> vertices = Vertices;

			if ((flag & 1) == 1) {
				matrix.SelfTranslate(-rsm.Box.Center);
			}
			else {
				if (rsm.Header.IsCompatibleWith(2, 2)) {
					//matrix.SelfTranslate(-rsm.Box.Center);
				}
				else {
					matrix.SelfTranslate(-rsm.Box.Center);
				}
			}

			matrix = Matrix4.Multiply(matrix, Matrix);

			Matrix4 modelViewMat = Matrix4.Multiply(instance, matrix);
			Matrix4 normalMat = Matrix4.ExtractRotation(modelViewMat);

			int count = Vertices.Count;
			Vertex[] vert = new Vertex[count];
			float x, y, z;

			for (int i = 0; i < count; i++) {
				x = vertices[i].X;
				y = vertices[i].Y;
				z = vertices[i].Z;

				vert[i] = new Vertex(
					modelViewMat[0] * x + modelViewMat[4] * y + modelViewMat[8] * z + modelViewMat[12],
					modelViewMat[1] * x + modelViewMat[5] * y + modelViewMat[9] * z + modelViewMat[13],
					modelViewMat[2] * x + modelViewMat[6] * y + modelViewMat[10] * z + modelViewMat[14]
					);
			}

			Vertex[] faceNormals = new Vertex[Faces.Count];

			Dictionary<string, int> meshSize = new Dictionary<string, int>();
			List<string> textures = rsm.Textures;

			if (rsm.Header.IsCompatibleWith(2, 3)) {
				textures = this.Textures;
			}

			for (int i = 0; i < textures.Count; i++) {
				meshSize[textures[i]] = 0;
			}

			for (int i = 0; i < Faces.Count; i++) {
				meshSize[GetAbsoluteTexture(Faces[i].TextureId)]++;
			}

			Dictionary<string, MeshTriangle[]> mesh = new Dictionary<string, MeshTriangle[]>();

			foreach (string texture in textures) {
				MeshTriangle[] meshFace;

				if (mesh.ContainsKey(texture)) {
					continue;
				}

				mesh[texture] = meshFace = new MeshTriangle[meshSize[texture]];

				for (int j = 0; j < meshFace.Length; j++) {
					meshFace[j] = new MeshTriangle();
				}
			}

			Dictionary<int, bool> shadeGroupUsed = new Dictionary<int, bool>();
			Dictionary<int, Vertex[]> shadeGroup = new Dictionary<int, Vertex[]>();

			switch (rsm.ShadeType) {
				case 0:
					_calculateNormals_None(faceNormals);
					_generateMesh_Flat(vert, faceNormals, mesh);
					break;
				case 1:
					_calculateNormals_Flat(faceNormals, normalMat, shadeGroupUsed);
					_generateMesh_Flat(vert, faceNormals, mesh);
					break;
				case 2:
					_calculateNormals_Flat(faceNormals, normalMat, shadeGroupUsed);
					_calculateNormals_Smooth(faceNormals, shadeGroupUsed, shadeGroup);
					_generateMesh_Smooth(vert, shadeGroup, mesh);
					break;
			}

			return _explode(mesh);
		}

		public int GetAbsoluteTextureId(int relativeId) {
			return _textureIndexes[relativeId];
		}

		public string GetAbsoluteTexture(int relativeId) {
			if (Model.Header.IsCompatibleWith(2, 3)) {
				return this.Textures[_textureIndexes[relativeId]];
			}
			else {
				return Model.Textures[_textureIndexes[relativeId]];
			}
		}

		private List<MeshRawData> _explode(Dictionary<string, MeshTriangle[]> mesh) {
			return mesh.Select(pair => new MeshRawData { MeshTriangles = pair.Value, Texture = pair.Key }).ToList();
		}

		private void _generateMesh_Smooth(Vertex[] vert, Dictionary<int, Vertex[]> shadeGroup, Dictionary<string, MeshTriangle[]> mesh) {
			int vertexId, textureVertexId;
			string texture;

			Dictionary<string, int> offsets = Model.Textures.ToDictionary(t => t, t => 0);

			for (int i = 0; i < Faces.Count; i++) {
				Face face = Faces[i];

				Vertex[] normals = shadeGroup[face.SmoothGroup];

				texture = GetAbsoluteTexture(face.TextureId); // Main.Textures[_textureIndexes[face.TextureId]];
				MeshTriangle[] output = mesh[texture];
				int offset = offsets[texture];

				for (int j = 0; j < 3; j++) {
					vertexId = face.VertexIds[j];
					textureVertexId = face.TextureVertexIds[j];
					output[offset].Positions[j] = vert[vertexId];
					output[offset].Normals[j] = normals[vertexId];
					output[offset].TextureCoords[j] = new Point(TextureVertices[textureVertexId]);
				}

				offsets[texture] = ++offset;
			}
		}

		private void _generateMesh_Flat(Vertex[] vert, Vertex[] norm, Dictionary<string, MeshTriangle[]> mesh) {
			Face face;
			string texture;

			Dictionary<string, int> offsets = new Dictionary<string, int>();

			if (Model.Header.IsCompatibleWith(2, 3)) {
				for (int i = 0; i < this.Textures.Count; i++) {
					offsets[this.Textures[i]] = 0;
				}
			}
			else {	
				for (int i = 0; i < Model.Textures.Count; i++) {
					offsets[Model.Textures[i]] = 0;
				}
			}

			int vertexId, textureVertexId;

			for (int i = 0; i < Faces.Count; i++) {
				try {
					face = Faces[i];
					texture = GetAbsoluteTexture(face.TextureId); // Main.Textures[_textureIndexes[face.TextureId]];

					MeshTriangle[] output = mesh[texture];
					int offset = offsets[texture];

					for (int j = 0; j < 3; j++) {
						vertexId = face.VertexIds[j];
						textureVertexId = face.TextureVertexIds[j];
						output[offset].Positions[j] = vert[vertexId];
						output[offset].Normals[j] = norm[i];
						output[offset].TextureCoords[j] = new Point(TextureVertices[textureVertexId]);
						//output[offset][j].Alpha = Main.Alpha;
					}

					offsets[texture] = ++offset;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		private void _calculateNormals_Smooth(IList<Vertex> normal, IDictionary<int, bool> groupUsed, IDictionary<int, Vertex[]> group) {
			float x, y, z, len;
			var size = Vertices.Count;
			var faces = Faces;
			Vertex[] norm;
			Face face;
			var count = Faces.Count;

			for (int j = 0; j < 32; j++) {
				if (!groupUsed.ContainsKey(j))
					continue;

				if (!groupUsed[j])
					continue;

				group[j] = new Vertex[size];
				norm = group[j];
				Vertex temp;

				for (int v = 0; v < size; ++v) {
					x = 0;
					y = 0;
					z = 0;

					for (int i = 0; i < count; i++) {
						face = faces[i];
						if (face.SmoothGroup == j && (face.VertexIds[0] == v || face.VertexIds[1] == v || face.VertexIds[2] == v)) {
							temp = normal[i];
							x += temp.X;
							y += temp.Y;
							z += temp.Z;
						}
					}

					len = (float) Math.Exp(-0.5 * Math.Log(x * x + y * y + z * z));
					norm[v].X = x * len;
					norm[v].Y = y * len;
					norm[v].Z = z * len;
				}
			}
		}

		private void _calculateNormals_Flat(Vertex[] faceNormals, Matrix4 normalMat, Dictionary<int, bool> groupUsed) {
			int i, j, count;
			var faces = Faces;
			Face face;
			Vertex tempVector;

			for (i = 0, j = 0, count = faces.Count; i < count; ++i, j += 3) {
				face = faces[i];
				tempVector = Vertex.CalculateNormal(_vertices[face.VertexIds[0]], _vertices[face.VertexIds[1]], _vertices[face.VertexIds[2]]);
				faceNormals[i][0] = normalMat[0] * tempVector[0] + normalMat[4] * tempVector[1] + normalMat[8] * tempVector[2] + normalMat[12];
				faceNormals[i][1] = normalMat[1] * tempVector[0] + normalMat[5] * tempVector[1] + normalMat[9] * tempVector[2] + normalMat[13];
				faceNormals[i][2] = normalMat[2] * tempVector[0] + normalMat[6] * tempVector[1] + normalMat[10] * tempVector[2] + normalMat[14];

				groupUsed[face.SmoothGroup] = true;
			}
		}

		private void _calculateNormals_None(IList<Vertex> normals) {
			Vertex d = new Vertex(-1, -1, -1);

			for (int i = 0; i < normals.Count; i++) {
				normals[i] = d;
			}
		}

		public override string ToString() {
			return "Name = " + Name;
		}

		#region Rendering
		public void ClearBuffer() {
			_bufferedScale = null;
			_bufferedRot = null;
			_bufferedPos = null;
			_bufferedTextureOffset.Clear();
		}

		public TkQuaternion GetRotQuaternion(int animationFrame) {
			if (_bufferedRot == null) {
				for (int i = 0; i < _rotFrames.Count - 1; i++) {
					if (animationFrame >= _rotFrames[i].Frame && _rotFrames[i + 1].Frame < animationFrame)
						continue;

					if (_rotFrames[i].Frame == animationFrame) {
						_bufferedRot = _rotFrames[i].Quaternion;
						return _bufferedRot.Value;
					}

					if (_rotFrames[i + 1].Frame == animationFrame) {
						_bufferedRot = _rotFrames[i + 1].Quaternion;
						return _bufferedRot.Value;
					}

					int dist = _rotFrames[i + 1].Frame - _rotFrames[i].Frame;
					animationFrame = animationFrame - _rotFrames[i].Frame;
					float mult = (animationFrame / (float)dist);

					var curFrame = _rotFrames[i];
					var nexFrame = _rotFrames[i + 1];

					_bufferedRot = TkQuaternion.Slerp(curFrame.Quaternion, nexFrame.Quaternion, mult);
					return _bufferedRot.Value;
				}

				if (animationFrame >= _rotFrames[_rotFrames.Count - 1].Frame)
					return _rotFrames[_rotFrames.Count - 1].Quaternion;

				return _rotFrames[0].Quaternion;
			}

			return _bufferedRot.Value;
		}

		public Vertex GetScale(int animationFrame) {
			if (_bufferedScale == null) {
				for (int i = 0; i < _scaleKeyFrames.Count - 1; i++) {
					if (animationFrame >= _scaleKeyFrames[i].Frame && _scaleKeyFrames[i + 1].Frame < animationFrame)
						continue;

					if (_scaleKeyFrames[i].Frame == animationFrame) {
						_bufferedScale = _scaleKeyFrames[i].Scale;
						return _bufferedScale.Value;
					}

					if (_scaleKeyFrames[i + 1].Frame == animationFrame) {
						_bufferedScale = _scaleKeyFrames[i + 1].Scale;
						return _bufferedScale.Value;
					}

					int dist = _scaleKeyFrames[i + 1].Frame - _scaleKeyFrames[i].Frame;
					animationFrame = animationFrame - _scaleKeyFrames[i].Frame;
					float mult = (animationFrame / (float)dist);

					var curFrame = _scaleKeyFrames[i];
					var nexFrame = _scaleKeyFrames[i + 1];


					_bufferedScale = mult * (nexFrame.Scale - curFrame.Scale) + curFrame.Scale;
					return _bufferedScale.Value;
				}

				if (animationFrame >= _scaleKeyFrames[_scaleKeyFrames.Count - 1].Frame)
					return _scaleKeyFrames[_scaleKeyFrames.Count - 1].Scale;

				return _scaleKeyFrames[0].Scale;
			}

			return _bufferedScale.Value;
		}

		public Vertex GetPosition(int animationFrame) {
			if (_bufferedPos == null) {
				for (int i = 0; i < _posKeyFrames.Count - 1; i++) {
					if (animationFrame >= _posKeyFrames[i].Frame && _posKeyFrames[i + 1].Frame < animationFrame)
						continue;

					if (_posKeyFrames[i].Frame == animationFrame) {
						_bufferedPos = _posKeyFrames[i].Position;
						return _bufferedPos.Value;
					}

					if (_posKeyFrames[i + 1].Frame == animationFrame) {
						_bufferedPos = _posKeyFrames[i + 1].Position;
						return _bufferedPos.Value;
					}

					int dist = _posKeyFrames[i + 1].Frame - _posKeyFrames[i].Frame;
					animationFrame = animationFrame - _posKeyFrames[i].Frame;
					float mult = (animationFrame / (float)dist);

					var curFrame = _posKeyFrames[i];
					var nexFrame = _posKeyFrames[i + 1];

					_bufferedPos = mult * (nexFrame.Position - curFrame.Position) + curFrame.Position;
					return _bufferedPos.Value;
				}

				if (animationFrame >= _posKeyFrames[_posKeyFrames.Count - 1].Frame)
					return _posKeyFrames[_posKeyFrames.Count - 1].Position;

				return _posKeyFrames[0].Position;
			}

			return _bufferedPos.Value;
		}

		public float GetTexture(int animationFrame, int textureId, int type) {
			var frames = _textureKeyFrameGroup.GetTextureKeyFrames(textureId, type);

			if (frames == null || frames.Count == 0)
				return 0;

			int uid = 100 * (textureId + 1) + type;

			if (_bufferedTextureOffset.ContainsKey(uid))
				return _bufferedTextureOffset[uid];

			for (int i = 0; i < frames.Count - 1; i++) {
				if (animationFrame >= frames[i].Frame && frames[i + 1].Frame < animationFrame)
					continue;

				if (frames[i].Frame == animationFrame) {
					_bufferedTextureOffset[uid] = frames[i].Offset;
					return frames[i].Offset;
				}

				if (frames[i + 1].Frame == animationFrame) {
					_bufferedTextureOffset[uid] = frames[i + 1].Offset;
					return frames[i + 1].Offset;
				}

				int dist = frames[i + 1].Frame - frames[i].Frame;
				animationFrame = animationFrame - frames[i].Frame;
				float mult = (animationFrame / (float)dist);

				var curFrame = frames[i];
				var nexFrame = frames[i + 1];

				float res = mult * (nexFrame.Offset - curFrame.Offset) + curFrame.Offset;
				_bufferedTextureOffset[uid] = res;
				return res;
			}

			if (animationFrame >= frames[frames.Count - 1].Frame)
				return frames[frames.Count - 1].Offset;

			return frames[0].Offset;
		}
		#endregion

		public void Write(BinaryWriter writer) {
			writer.WriteANSI(Name, 40);
			writer.WriteANSI(ParentName, 40);

			writer.Write(_textureIndexes.Count);

			foreach (int index in _textureIndexes) {
				writer.Write(index);
			}

			if (Name.Contains("_e_05")) {
				Z.F();
			}

			for (int i = 0; i < 9; i++) {
				writer.Write(_offsetMT[i]);
			}

			Position_.Write(writer);
			Position.Write(writer);
			writer.Write(RotAngle);
			RotAxis.Write(writer);
			Scale.Write(writer);

			writer.Write(_vertices.Count);

			for (int i = 0; i < _vertices.Count; i++) {
				_vertices[i].Write(writer);
			}

			writer.Write(_tvertices.Count);

			for (int i = 0; i < _tvertices.Count; i++) {
				if (Model.Header.MajorVersion > 1 || (Model.Header.MajorVersion == 1 && Model.Header.MinorVersion >= 2)) {
					_tvertices[i].Write(writer, true);
				}
				else {
					_tvertices[i].Write(writer, false);
				}
			}

			writer.Write(_faces.Count);

			for (int i = 0; i < _faces.Count; i++) {
				if (Model.Header.MajorVersion > 1 || (Model.Header.MajorVersion == 1 && Model.Header.MinorVersion >= 2)) {
					_faces[i].Write(writer, true);
				}
				else {
					_faces[i].Write(writer, false);
				}
			}

			if (Model.Header.MajorVersion > 1 || (Model.Header.MajorVersion == 1 && Model.Header.MinorVersion >= 6)) {
				writer.Write(_posKeyFrames.Count);

				for (int i = 0; i < _posKeyFrames.Count; i++) {
					_posKeyFrames[i].Write(writer);
				}
			}

			writer.Write(_rotFrames.Count);

			for (int i = 0; i < _rotFrames.Count; i++) {
				_rotFrames[i].Write(writer);
			}
		}

		internal void Save(BinaryWriter writer) {
			writer.WriteANSI("MESH", 4);
			writer.Write(Model.Header.MajorVersion);
			writer.Write(Model.Header.MinorVersion);
			writer.WriteANSI(Name, 40);
			writer.WriteANSI(ParentName, 40);

			writer.Write(_textureIndexes.Count);

			foreach (int index in _textureIndexes) {
				writer.Write(index);
			}

			for (int i = 0; i < 9; i++) {
				writer.Write(_offsetMT[i]);
			}

			Position_.Write(writer);
			Position.Write(writer);
			writer.Write(RotAngle);
			RotAxis.Write(writer);
			Scale.Write(writer);

			writer.Write(_vertices.Count);

			for (int i = 0; i < _vertices.Count; i++) {
				_vertices[i].Write(writer);
			}

			writer.Write(_tvertices.Count);

			for (int i = 0; i < _tvertices.Count; i++) {
				if (Model.Header.MajorVersion > 1 || (Model.Header.MajorVersion == 1 && Model.Header.MinorVersion >= 2)) {
					_tvertices[i].Write(writer, true);
				}
				else {
					_tvertices[i].Write(writer, false);
				}
			}

			writer.Write(_faces.Count);

			for (int i = 0; i < _faces.Count; i++) {
				if (Model.Header.MajorVersion > 1 || (Model.Header.MajorVersion == 1 && Model.Header.MinorVersion >= 2)) {
					_faces[i].Write(writer, true);
				}
				else {
					_faces[i].Write(writer, false);
				}
			}

			if (Model.Header.MajorVersion > 1 || (Model.Header.MajorVersion == 1 && Model.Header.MinorVersion >= 6)) {
				writer.Write(_posKeyFrames.Count);

				for (int i = 0; i < _posKeyFrames.Count; i++) {
					_posKeyFrames[i].Write(writer);
				}
			}

			writer.Write(_rotFrames.Count);

			for (int i = 0; i < _rotFrames.Count; i++) {
				_rotFrames[i].Write(writer);
			}
		}
	}
}