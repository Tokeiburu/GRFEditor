using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Graphics;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.RsmFormat {
	public class Mesh : IWriteableFile {
		private readonly List<TkVector3> _vertices = new List<TkVector3>();
		private readonly List<int> _textureIndexes = new List<int>();
		private readonly List<TextureVertex> _tvertices = new List<TextureVertex>();
		private readonly List<Face> _faces = new List<Face>();
		public BoundingBox BoundingBox = new BoundingBox();
		public Mesh Parent;
		public HashSet<Mesh> Children = new HashSet<Mesh>();
		public TkVector3 Position;
		public TkVector3 Position_;
		public object AttachedNormals;

		public float RotationAngle;
		public TkVector3 RotationAxis;
		public TkVector3 Scale;

		public List<string> Textures = new List<string>();
		public TkMatrix4 Matrix2 = TkMatrix4.Identity;
		public TkMatrix4 Matrix1 = TkMatrix4.Identity;

		private readonly List<ScaleKeyFrame> _scaleKeyFrames = new List<ScaleKeyFrame>();
		private readonly List<RotKeyFrame> _rotFrames = new List<RotKeyFrame>();
		private readonly List<PosKeyFrame> _posKeyFrames = new List<PosKeyFrame>();
		private readonly TextureKeyFrameGroup _textureKeyFrameGroup = new TextureKeyFrameGroup();

		public List<int> TextureIndexes {
			get { return _textureIndexes; }
		}

		public Rsm Model { get; set; }
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
		public TkMatrix3 TransformationMatrix = TkMatrix3.Identity;
		public TkMatrix4 InvertTransformationMatrix = TkMatrix4.Identity;

		/// <summary>
		/// Gets the vertices.
		/// </summary>
		/// <value>The vertices.</value>
		public List<TkVector3> Vertices {
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
		public Mesh(Rsm rsm) {
			Position = new TkVector3();
			Position_ = new TkVector3();
			RotationAngle = 0;
			RotationAxis = new TkVector3(0, 0, 0);
			Scale = new TkVector3(1, 1, 1);
			ParentName = "";
			Name = "";
			Model = rsm;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		/// <param name="mesh">The mesh.</param>
		public Mesh(Mesh mesh) {
			TransformationMatrix = mesh.TransformationMatrix;

			foreach (var skf in mesh._scaleKeyFrames) {
				_scaleKeyFrames.Add(skf);
			}

			foreach (var rkf in mesh._rotFrames) {
				_rotFrames.Add(rkf);
			}

			foreach (var pkf in mesh._posKeyFrames) {
				_posKeyFrames.Add(pkf);
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
				TransformationMatrix[i] = reader.Float();
			}

			Position_ = new TkVector3(reader);

			if (version >= 2.2) {
				Position = new TkVector3(0, 0, 0);
				RotationAngle = 0;
				RotationAxis = new TkVector3(0, 0, 0);
				Scale = new TkVector3(1, 1, 1);
			}
			else {
				Position = new TkVector3(reader);
				RotationAngle = reader.Float();
				RotationAxis = new TkVector3(reader);
				Scale = new TkVector3(reader);
			}

			_vertices.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				_vertices.Add(new TkVector3(reader));
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
					_scaleKeyFrames.Add(new ScaleKeyFrame(reader));
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

		public int GetAbsoluteTextureId(int relativeId) {
			return _textureIndexes[relativeId];
		}

		public string GetAbsoluteTexture(int relativeId) {
			if (Model.Header.IsCompatibleWith(2, 3)) {
				return Textures[_textureIndexes[relativeId]];
			}
			else {
				return Model.Textures[_textureIndexes[relativeId]];
			}
		}

		public override string ToString() {
			return "Name = " + Name;
		}

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
				writer.Write(TransformationMatrix[i]);
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
					_scaleKeyFrames[i].Write(writer);
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
				writer.Write(TransformationMatrix[i]);
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

		public BoundingBox CalculateBoundingBox() {
			BoundingBox box = new BoundingBox();

			foreach (var v in Vertices) {
				box.AddVertex(v);
			}

			return box;
		}
	}
}