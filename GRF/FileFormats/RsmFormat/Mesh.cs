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
		private readonly List<Vertex> _vertices = new List<Vertex>();
		private readonly List<int> _textureIndexes = new List<int>();
		private readonly List<TextureVertex> _tvertices = new List<TextureVertex>();
		private readonly List<Face> _faces = new List<Face>();
		public BoundingBox BoundingBox = new BoundingBox();
		public Mesh Parent;
		public HashSet<Mesh> Children = new HashSet<Mesh>();
		public Vertex Position;
		public Vertex Position_;
		public object AttachedNormals;

		public float RotationAngle;
		public Vertex RotationAxis;
		public Vertex Scale;
		public Vertex Flip;
		private Matrix3 _transformationMatrix = new Matrix3();

		public List<string> Textures = new List<string>();
		public Matrix4 MeshMatrixSelf;
		internal Matrix4 MeshMatrix;
		public Matrix4 Matrix = Matrix4.Identity;

		private readonly List<ScaleKeyFrame> _scaleKeyFrames = new List<ScaleKeyFrame>();
		private readonly List<RotKeyFrame> _rotFrames = new List<RotKeyFrame>();
		private readonly List<PosKeyFrame> _posKeyFrames = new List<PosKeyFrame>();
		private readonly TextureKeyFrameGroup _textureKeyFrameGroup = new TextureKeyFrameGroup();
		private TkQuaternion? _bufferedRot;
		private Vertex? _bufferedScale;
		private Vertex? _bufferedPos;
		private readonly Dictionary<int, float> _bufferedTextureOffset = new Dictionary<int, float>();

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

		/// <summary>
		/// Gets or sets the transformation matrix.
		/// </summary>
		/// <value>The transformation matrix.</value>
		public Matrix3 TransformationMatrix {
			get { return _transformationMatrix; }
			set { _transformationMatrix = value; }
		}

		/// <summary>
		/// Gets the vertices.
		/// </summary>
		/// <value>The vertices.</value>
		public List<Vertex> Vertices {
			get { return _vertices; }
		}

		/// <summary>
		/// Gets the texture vertices.
		/// </summary>
		/// <value>The texture vertices.</value>
		public List<TextureVertex> TextureVertices {
			get { return _tvertices; }
		}

		/// <summary>
		/// Gets the faces.
		/// </summary>
		/// <value>The faces.</value>
		public List<Face> Faces {
			get { return _faces; }
		}

		/// <summary>
		/// Gets the scale key frames.
		/// </summary>
		/// <value>The scale key frames.</value>
		public List<ScaleKeyFrame> ScaleKeyFrames {
			get { return _scaleKeyFrames; }
		}

		/// <summary>
		/// Gets the rotation key frames.
		/// </summary>
		/// <value>The rotation key frames.</value>
		public List<RotKeyFrame> RotationKeyFrames {
			get { return _rotFrames; }
		}

		/// <summary>
		/// Gets the position key frames.
		/// </summary>
		/// <value>The position key frames.</value>
		public List<PosKeyFrame> PosKeyFrames {
			get { return _posKeyFrames; }
		}

		/// <summary>
		/// Gets the texture key frame group.
		/// </summary>
		/// <value>The texture key frame group.</value>
		public TextureKeyFrameGroup TextureKeyFrameGroup {
			get { return _textureKeyFrameGroup; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		public Mesh() {
			_transformationMatrix = new Matrix3();
			_transformationMatrix[0] = _transformationMatrix[4] = _transformationMatrix[8] = 1f;
			Position = new Vertex();
			Position_ = new Vertex();
			RotationAngle = 0;
			RotationAxis = new Vertex(0, 0, 0);
			Scale = new Vertex(1, 1, 1);
			ParentName = "";
			Name = "";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		/// <param name="mesh">The mesh.</param>
		public Mesh(Mesh mesh) {
			_transformationMatrix = new Matrix3(mesh._transformationMatrix);

			foreach (var skf in mesh._scaleKeyFrames) {
				_scaleKeyFrames.Add(new ScaleKeyFrame(skf));
			}

			foreach (var rf in mesh._rotFrames) {
				_rotFrames.Add(new RotKeyFrame(rf));
			}

			foreach (var psk in mesh._posKeyFrames) {
				_posKeyFrames.Add(new PosKeyFrame(psk));
			}

			foreach (var texture in mesh.Textures) {
				Textures.Add(texture);
			}

			_textureIndexes = mesh._textureIndexes.ToList();
			_tvertices = mesh._tvertices.ToList();
			_vertices = mesh._vertices.ToList();

			foreach (var child in mesh.Children) {
				Children.Add(new Mesh(child));
			}

			Parent = mesh.Parent;
			Position = mesh.Position;
			Position_ = mesh.Position_;
			RotationAngle = mesh.RotationAngle;
			RotationAxis = mesh.RotationAxis;
			Scale = mesh.Scale;

			foreach (var f in mesh._faces) {
				_faces.Add(new Face(f));
			}

			Model = mesh.Model;
			Name = mesh.Name;
			ParentName = mesh.ParentName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		/// <param name="rsm">The RSM.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="version">The version.</param>
		public Mesh(Rsm rsm, IBinaryReader reader, double version) {
			int count;

			Model = rsm;

			if (version >= 2.2) {
				Name = reader.String(reader.Int32(), '\0');
				ParentName = reader.String(reader.Int32(), '\0');
			}
			else {
				Name = reader.String(40, '\0');
				ParentName = reader.String(40, '\0');
			}

			if (version >= 2.3) {
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					Textures.Add(reader.String(reader.Int32(), '\0'));
				}

				_textureIndexes.Capacity = count;

				for (int i = 0; i < count; i++) {
					_textureIndexes.Add(i);
				}
			}
			else {
				_textureIndexes.Capacity = count = reader.Int32();

				for (int i = 0; i < count; i++) {
					_textureIndexes.Add(reader.Int32());
				}
			}

			for (int i = 0; i < 9; i++) {
				_transformationMatrix[i] = reader.Float();
			}

			Position_ = new Vertex(reader);

			if (version >= 2.2) {
				Position = new Vertex(0, 0, 0);
				RotationAngle = 0;
				RotationAxis = new Vertex(0, 0, 0);
				Scale = new Vertex(1, 1, 1);
				Flip = new Vertex(-1, -1, 1);
			}
			else {
				Position = new Vertex(reader);
				RotationAngle = reader.Float();
				RotationAxis = new Vertex(reader);
				Scale = new Vertex(reader);
				Flip = new Vertex(1, 1, 1);
			}

			_vertices.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				_vertices.Add(new Vertex(reader));
			}

			_tvertices.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				_tvertices.Add(new TextureVertex {
					Color = version >= 1.2 ? reader.UInt32() : 0xFFFFFFFF,
					U = reader.Float(),
					V = reader.Float()
				});
			}

			_faces.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				Face face = new Face();
				int len = -1;

				if (version >= 2.2) {
					len = reader.Int32();
				}

				face.VertexIds = reader.ArrayUInt16(3);
				face.TextureVertexIds = reader.ArrayUInt16(3);
				face.TextureId = reader.UInt16();
				face.Padding = reader.UInt16();
				face.TwoSide = reader.Int32();

				if (version >= 1.2) {
					face.SmoothGroup[0] = face.SmoothGroup[1] = face.SmoothGroup[2] = reader.Int32();

					if (len > 24) {
						face.SmoothGroup[1] = reader.Int32();
					}

					if (len > 28) {
						face.SmoothGroup[2] = reader.Int32();
					}

					if (len > 32) {
						reader.Forward(len - 32);
					}
				}

				_faces.Add(face);
			}

			if (version >= 1.6) {
				_scaleKeyFrames.Capacity = count = reader.Int32();

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

			_rotFrames.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				_rotFrames.Add(new RotKeyFrame {
					Frame = reader.Int32(),
					Quaternion = new TkQuaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float())
				});
			}

			if (version >= 2.2) {
				_posKeyFrames.Capacity = count = reader.Int32();

				for (int i = 0; i < count; i++) {
					_posKeyFrames.Add(new PosKeyFrame {
						Frame = reader.Int32(),
						X = reader.Float(),
						Y = reader.Float(),
						Z = reader.Float(),
						Data = reader.Int32()
					});
				}
			}

			if (version >= 2.3) {
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					int textureId = reader.Int32();
					int amountTextureAnimations = reader.Int32();

					for (int j = 0; j < amountTextureAnimations; j++) {
						int type = reader.Int32();
						int amountFrames = reader.Int32();

						for (int k = 0; k < amountFrames; k++) {
							_textureKeyFrameGroup.AddTextureKeyFrame(textureId, type, new TextureKeyFrame {
								Frame = reader.Int32(),
								Offset = reader.Float()
							});
						}
					}
				}
			}

			_uniqueTextures();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		/// <param name="rsm">The RSM.</param>
		/// <param name="file">The file.</param>
		public Mesh(Rsm rsm, string file) : this(rsm, new ByteReader(file, 6), 1.4) {
			LoadedPath = file;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		/// <param name="rsm">The RSM.</param>
		/// <param name="reader">The reader.</param>
		public Mesh(Rsm rsm, IBinaryReader reader)
			: this(rsm, reader, rsm.Version) {
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
			MeshMatrixSelf = Matrix4.Identity;
			MeshMatrix = Matrix4.Identity;

			// Calculate Matrix applied on the mesh itself
			if (ScaleKeyFrames.Count > 0) {
				MeshMatrix = Matrix4.Scale(MeshMatrix, GetScale(animationFrame));
			}

			if (RotationKeyFrames.Count > 0) {
				MeshMatrix = Matrix4.Rotate(MeshMatrix, GetRotQuaternion(animationFrame));
			}
			else {
				MeshMatrix = Matrix4.Multiply2(MeshMatrix, new Matrix4(TransformationMatrix));

				if (Parent != null) {
					MeshMatrix = Matrix4.Multiply2(MeshMatrix, new Matrix4(Parent.TransformationMatrix).Invert());
				}
			}

			MeshMatrixSelf = new Matrix4(MeshMatrix);

			Vertex position;

			// Calculate the position of the mesh from its parent
			if (PosKeyFrames.Count > 0) {
				position = GetPosition(animationFrame);
			}
			else {
				if (Parent != null) {
					position = Position_ - Parent.Position_;
					position = Matrix4.Multiply2(new Matrix4(Parent.TransformationMatrix).Invert(), position);
				}
				else {
					position = Position_;
				}
			}

			MeshMatrixSelf.Offset = position;

			// Apply parent transformations
			Mesh mesh = this;

			while (mesh.Parent != null) {
				mesh = mesh.Parent;
				MeshMatrixSelf = Matrix4.Multiply2(MeshMatrixSelf, mesh.MeshMatrix);
			}

			// Set the final position relative to the parent's position
			if (Parent != null) {
				MeshMatrixSelf.Offset += Parent.MeshMatrixSelf.Offset;
			}

			// Calculate children
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
				if (RotationKeyFrames.Count > 0) {
					Matrix = Matrix4.Rotate(Matrix, GetRotQuaternion(0));
				}
				else {
					Matrix = Matrix4.Rotate2(Matrix, RotationAxis, RotationAngle);
				}
				Matrix = Matrix4.Multiply(Matrix, new Matrix4(TransformationMatrix));

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

			if (RotationKeyFrames.Count > 0) {
				MeshMatrix = Matrix4.RotateQuat(MeshMatrix, RotationKeyFrames[0]);
			}
			else {
				MeshMatrix = Matrix4.Rotate2(MeshMatrix, RotationAxis, RotationAngle);
			}

			if (Parent != null)
				MeshMatrix.Offset = Position;

			MeshMatrix = Matrix4.Multiply(MeshMatrix, parent);
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

				Vertex[] normals = shadeGroup[face.SmoothGroup[0]];

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
						if (face.SmoothGroup[0] == j && (face.VertexIds[0] == v || face.VertexIds[1] == v || face.VertexIds[2] == v)) {
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

				groupUsed[face.SmoothGroup[0]] = true;
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
			if (Model.Version >= 2.2) {
				writer.Write(Name.Length);
				writer.WriteANSI(Name, Name.Length);
				writer.Write(ParentName.Length);
				writer.WriteANSI(ParentName, ParentName.Length);
			}
			else {	
				writer.WriteANSI(Name, 40);
				writer.WriteANSI(ParentName, 40);
			}

			if (Model.Version >= 2.3) {
				writer.Write(Textures.Count);

				for (int i = 0; i < Textures.Count; i++) {
					writer.Write(Textures[i].Length);
					writer.WriteANSI(Textures[i], Textures[i].Length);
				}
			}
			else {	
				writer.Write(_textureIndexes.Count);

				foreach (int index in _textureIndexes) {
					writer.Write(index);
				}
			}

			for (int i = 0; i < 9; i++) {
				writer.Write(_transformationMatrix[i]);
			}

			Position_.Write(writer);

			if (Model.Version < 2.2) {
				Position.Write(writer);
				writer.Write(RotationAngle);
				RotationAxis.Write(writer);
				Scale.Write(writer);
			}

			writer.Write(_vertices.Count);

			for (int i = 0; i < _vertices.Count; i++) {
				_vertices[i].Write(writer);
			}

			writer.Write(_tvertices.Count);

			for (int i = 0; i < _tvertices.Count; i++) {
				if (Model.Version >= 1.2) {
					_tvertices[i].Write(writer, true);
				}
				else {
					_tvertices[i].Write(writer, false);
				}
			}

			writer.Write(_faces.Count);

			for (int i = 0; i < _faces.Count; i++) {
				_faces[i].Write(Model, writer);
			}

			if (Model.Version >= 1.6) {
				writer.Write(_scaleKeyFrames.Count);

				for (int i = 0; i < _scaleKeyFrames.Count; i++) {
					writer.Write(_scaleKeyFrames[i].Frame);
					writer.Write(_scaleKeyFrames[i].Sx);
					writer.Write(_scaleKeyFrames[i].Sy);
					writer.Write(_scaleKeyFrames[i].Sz);
					writer.Write(_scaleKeyFrames[i].Data);
				}
			}

			writer.Write(_rotFrames.Count);

			for (int i = 0; i < _rotFrames.Count; i++) {
				_rotFrames[i].Write(writer);
			}

			if (Model.Version >= 2.2) {
				writer.Write(_posKeyFrames.Count);

				for (int i = 0; i < _posKeyFrames.Count; i++) {
					_posKeyFrames[i].Write(writer);
				}
			}

			if (Model.Version >= 2.3) {
				//writer.Write(_textureKeyFrameGroup.Count);
				writer.Write(0);

				//foreach (var group in _textureKeyFrameGroup.Types) {
				//	writer.Write(group);
				//
				//	var entry = _textureKeyFrameGroup.GetTextureKeyFrames(group);
				//}
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
				writer.Write(_transformationMatrix[i]);
			}

			Position_.Write(writer);
			Position.Write(writer);
			writer.Write(RotationAngle);
			RotationAxis.Write(writer);
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
				_faces[i].Write(Model, writer);
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