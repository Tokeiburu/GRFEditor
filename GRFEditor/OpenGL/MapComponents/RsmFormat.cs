using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GRF;
using GRF.FileFormats.RsmFormat;
using GRF.IO;
using OpenTK;
using Utilities;

namespace GRFEditor.OpenGL.MapComponents {
	public class Face {
		public UInt16 Padding;
		public int[] SmoothGroup = new int[3];
		public UInt16 TextureId;
		public UInt16[] TextureVertexIds = new UInt16[3];
		public int TwoSide;
		public UInt16[] VertexIds = new UInt16[3];
		public Vector3 Normal;
		public Vector3[] VertexNormals = new Vector3[3];

		public Face() {
			SmoothGroup[0] = SmoothGroup[1] = SmoothGroup[2] = -1;
		}
	}

	public class Mesh {
		public readonly List<Vector3> Vertices = new List<Vector3>();
		public readonly List<int> TextureIndexes = new List<int>();
		public readonly List<Vector2> TextureVertices = new List<Vector2>();
		public readonly List<Face> Faces = new List<Face>();
		public Mesh Parent;
		public HashSet<Mesh> Children = new HashSet<Mesh>();
		public Vector3 Position;
		public Vector3 Position_;
		public object AttachedNormals;
		public int Index { get; set; }

		public float RotationAngle;
		public Vector3 RotationAxis;
		public Vector3 Scale;
		public Matrix4 TransformationMatrix = new Matrix4();
		public Matrix4 InvertTransformationMatrix = new Matrix4();

		public List<string> Textures = new List<string>();

		public readonly List<ScaleKeyFrame> ScaleKeyFrames = new List<ScaleKeyFrame>();
		public readonly List<RotKeyFrame> RotationKeyFrames = new List<RotKeyFrame>();
		public readonly List<PosKeyFrame> PosKeyFrames = new List<PosKeyFrame>();
		private readonly TextureKeyFrameGroup _textureKeyFrameGroup = new TextureKeyFrameGroup();

		public Rsm Model { get; set; }
		public string Name { get; set; }
		public string ParentName { get; set; }

		public TextureKeyFrameGroup TextureKeyFrameGroup {
			get { return _textureKeyFrameGroup; }
		}

		public Matrix4 Matrix1 { get; set; }
		public Matrix4 Matrix2 { get; set; }
		public bool IsAnimated { get; set; }
		public RsmBoundingBox Box { get; set; }
		public RsmBoundingBox LocalBox { get; set; }
		public Matrix4 RenderMatrix;
		public Matrix4 RenderMatrixSub;
		public int VboOffset;
		public int VboOffsetTransparent;

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		public Mesh() {
			TransformationMatrix = Matrix4.Identity;
			InvertTransformationMatrix = Matrix4.Invert(TransformationMatrix);
			Position = new Vector3();
			Position_ = new Vector3();
			RotationAngle = 0;
			RotationAxis = new Vector3(0, 0, 0);
			Scale = new Vector3(1, 1, 1);
			ParentName = "";
			Name = "";
		}

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

					var lastTexture = Textures.Last();
					var lastIndex = Model.Textures.FirstOrDefault(p => String.Compare(p, lastTexture, StringComparison.OrdinalIgnoreCase) == 0);

					if (lastIndex != null) {
						TextureIndexes.Add(Model.Textures.IndexOf(lastIndex));
					}
					else {
						TextureIndexes.Add(Model.Textures.Count);
						Model.Textures.Add(lastTexture);
					}
				}
			}
			else {
				TextureIndexes.Capacity = count = reader.Int32();

				for (int i = 0; i < count; i++) {
					TextureIndexes.Add(reader.Int32());
				}
			}

			TransformationMatrix = Matrix4.Identity;
			TransformationMatrix[0, 0] = reader.Float();
			TransformationMatrix[0, 1] = reader.Float();
			TransformationMatrix[0, 2] = reader.Float();

			TransformationMatrix[1, 0] = reader.Float();
			TransformationMatrix[1, 1] = reader.Float();
			TransformationMatrix[1, 2] = reader.Float();

			TransformationMatrix[2, 0] = reader.Float();
			TransformationMatrix[2, 1] = reader.Float();
			TransformationMatrix[2, 2] = reader.Float();
			InvertTransformationMatrix = Matrix4.Invert(TransformationMatrix);

			Position_ = new Vector3(reader.Float(), reader.Float(), reader.Float());

			if (version >= 2.2) {
				Position = new Vector3(0, 0, 0);
				RotationAngle = 0;
				RotationAxis = new Vector3(0, 0, 0);
				Scale = new Vector3(1, 1, 1);
			}
			else {
				Position = new Vector3(reader.Float(), reader.Float(), reader.Float());
				RotationAngle = reader.Float();
				RotationAxis = new Vector3(reader.Float(), reader.Float(), reader.Float());
				Scale = new Vector3(reader.Float(), reader.Float(), reader.Float());
			}

			Vertices.Capacity = count = reader.Int32();

			var bytes = reader.Bytes(count * 12);
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var h = handle.AddrOfPinnedObject();
			
			for (int i = 0; i < count; i++) {
				Vertices.Add((Vector3)Marshal.PtrToStructure(h + 12 * i, typeof(Vector3)));
			}
			
			handle.Free();

			//for (int i = 0; i < count; i++) {
			//	Vertices.Add(new Vector3(reader.Float(), reader.Float(), reader.Float()));
			//}

			TextureVertices.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				if (version >= 1.2)
					reader.Forward(4);	// Color, skip

				TextureVertices.Add(new Vector2(reader.Float(), reader.Float()));
			}

			Faces.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				Face face = new Face();
				int len = -1;

				if (version >= 2.2) {
					len = reader.Int32();
				}

				face.VertexIds[0] = reader.UInt16();
				face.VertexIds[1] = reader.UInt16();
				face.VertexIds[2] = reader.UInt16();
				face.TextureVertexIds[0] = reader.UInt16();
				face.TextureVertexIds[1] = reader.UInt16();
				face.TextureVertexIds[2] = reader.UInt16();
				face.TextureId = reader.UInt16();
				face.Padding = reader.UInt16();
				face.TwoSide = reader.Int32();

				if (version >= 1.2) {
					face.SmoothGroup[0] = face.SmoothGroup[1] = face.SmoothGroup[2] = reader.Int32();

					if (len > 24)
						face.SmoothGroup[1] = reader.Int32();
					if (len > 28)
						face.SmoothGroup[2] = reader.Int32();
					if (len > 32)
						reader.Forward(len - 32);
				}

				face.Normal = Vector3.NormalizeFast(Vector3.Cross(
					Vertices[face.VertexIds[1]] - Vertices[face.VertexIds[0]],
					Vertices[face.VertexIds[2]] - Vertices[face.VertexIds[0]]
				));

				Faces.Add(face);
			}

			if (Rsm.ForceShadeType > 0) {
				Model.ShadeType = Rsm.ForceShadeType;
			}

			if (Model.ShadeType == 1) {
				foreach (var face in Faces) {
					for (int ii = 0; ii < 3; ii++) {
						face.VertexNormals[ii] = face.Normal;
					}
				}
			}
			else if (Model.ShadeType == 2) {
				Dictionary<int, Dictionary<int, Vector3>> groups = new Dictionary<int, Dictionary<int, Vector3>>();
				
				foreach (var face in Faces) {
					for (int i = 0; i < 3; i++) {
						for (int ii = 0; ii < 3; ii++) {
							if (face.SmoothGroup[ii] != -1) {
								Dictionary<int, Vector3> groupNormals;
				
								if (!groups.TryGetValue(face.SmoothGroup[ii], out groupNormals)) {
									groupNormals = new Dictionary<int, Vector3>();
									groups[face.SmoothGroup[ii]] = groupNormals;
								}
				
								if (!groupNormals.ContainsKey(face.VertexIds[i])) {
									groupNormals[face.VertexIds[i]] = new Vector3();
								}
				
								groupNormals[face.VertexIds[i]] += face.Normal;
							}
						}
					}
				}
				
				foreach (var face in Faces) {
					for (int ii = 0; ii < 3; ii++) {
						if (face.SmoothGroup[ii] != -1) {
							for (int i = 0; i < 3; i++) {
								face.VertexNormals[i] += groups[face.SmoothGroup[0]][face.VertexIds[i]];
							}
						}
					}
				
					for (int i = 0; i < 3; i++) {
						face.VertexNormals[i] = Vector3.NormalizeFast(face.VertexNormals[i]);
					}
				}
			}
			else if (Model.ShadeType == 5) {
				var groups = TextureIndexes.Select(texture => new Dictionary<int, Vector3>()).ToList();

				foreach (var face in Faces) {
					for (int i = 0; i < 3; i++) {
						if (!groups[face.TextureId].ContainsKey(face.VertexIds[i]))
							groups[face.TextureId][face.VertexIds[i]] = new Vector3(0);

						groups[face.TextureId][face.VertexIds[i]] += face.Normal;
					}
				}

				foreach (var face in Faces) {
					for (int i = 0; i < 3; i++) {
						face.VertexNormals[i] = Vector3.NormalizeFast(groups[face.TextureId][face.VertexIds[i]]);
					}
				}
			}

			if (version >= 1.6) {
				ScaleKeyFrames.Capacity = count = reader.Int32();

				for (int i = 0; i < count; i++) {
					ScaleKeyFrames.Add(new ScaleKeyFrame {
						Frame = reader.Int32(),
						Scale = new Vector3(reader.Float(), reader.Float(), reader.Float())
					});

					reader.Forward(4);
				}
			}

			RotationKeyFrames.Capacity = count = reader.Int32();

			for (int i = 0; i < count; i++) {
				RotationKeyFrames.Add(new RotKeyFrame {
					Frame = reader.Int32(),
					Quaternion = new Quaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float())
				});
			}

			if (version < 2.0) {
				if (RotationKeyFrames.Count > 0)
					rsm.AnimationLength = Math.Max(rsm.AnimationLength, RotationKeyFrames.Last().Frame);
				if (ScaleKeyFrames.Count > 0)
					rsm.AnimationLength = Math.Max(rsm.AnimationLength, ScaleKeyFrames.Last().Frame);
				rsm.FramesPerSecond = 1000f;
			}

			if (version >= 2.2) {
				PosKeyFrames.Capacity = count = reader.Int32();

				for (int i = 0; i < count; i++) {
					PosKeyFrames.Add(new PosKeyFrame {
						Frame = reader.Int32(),
						Position = new Vector3(reader.Float(), reader.Float(), reader.Float())
					});

					reader.Forward(4);
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
		}

		public Mesh(Rsm rsm, IBinaryReader reader)
			: this(rsm, reader, rsm.Version) {
		}

		public override string ToString() {
			return "Name = " + Name;
		}

		public void CalcMatrix1() {
			Matrix1 = Matrix4.Identity;

			if (Model.Version < 2.2) {
				if (Parent == null) {
					if (Children.Count > 0) {
						Matrix1 = GLHelper.Translate(Matrix1, new Vector3(-Model.LocalBox.Center.X, -Model.LocalBox.Max.Y, -Model.LocalBox.Center.Z));
					}
					else {
						Matrix1 = GLHelper.Translate(Matrix1, new Vector3(0, -Model.LocalBox.Max.Y + Model.LocalBox.Center.Y, 0));
					}
				}
				else {
					Matrix1 = GLHelper.Translate(Matrix1, Position);
				}

				if (RotationKeyFrames.Count == 0) {
					if (Math.Abs(RotationAngle) > 0.01) {
						Matrix1 = GLHelper.Rotate(Matrix1, GLHelper.ToRad(RotationAngle * 180.0f / Math.PI), RotationAxis);
					}
				}
				else {
					if (RotationKeyFrames.Count > 0) {
						float tick = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);
						int current = RotationKeyFrames.Count - 1;

						for (int i = 0; i < RotationKeyFrames.Count; i++) {
							if (RotationKeyFrames[i].Frame > tick) {
								current = i - 1;
								break;
							}
						}

						if (current < 0)
							current = 0;

						int next = current + 1;

						if (next < RotationKeyFrames.Count) {
							float interval = (tick - RotationKeyFrames[current].Frame) / (RotationKeyFrames[next].Frame - RotationKeyFrames[current].Frame);
							Quaternion quat = Quaternion.Slerp(RotationKeyFrames[current].Quaternion, RotationKeyFrames[next].Quaternion, interval);
							quat = Quaternion.Normalize(quat);
							Matrix1 = new Matrix4(Matrix3.CreateFromQuaternion(quat)) * Matrix1;
						}
					}
				}

				Matrix1 = GLHelper.Scale(Matrix1, Scale);
			}
			else {
				Matrix2 = Matrix4.Identity;

				if (ScaleKeyFrames.Count > 0) {
					float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);
					//int prevIndex = -1;
					//int nextIndex = -1;
					//
					//while (true) {
					//	nextIndex++;
					//	
					//	if (nextIndex == ScaleKeyFrames.Count || animationFrame < ScaleKeyFrames[nextIndex].Frame)
					//		break;
					//
					//	prevIndex++;
					//}

					int prevIndex = -1;
					int nextIndex = -1;

					_findIndex(animationFrame, ref prevIndex, ref nextIndex, ScaleKeyFrames);

					float prevTick = prevIndex < 0 ? 0 : ScaleKeyFrames[prevIndex].Frame;
					float nextTick = nextIndex == ScaleKeyFrames.Count ? Model.AnimationLength : ScaleKeyFrames[nextIndex].Frame;
					Vector3 prev = prevIndex < 0 ? new Vector3(1) : ScaleKeyFrames[prevIndex].Scale;
					Vector3 next = nextIndex == ScaleKeyFrames.Count ? ScaleKeyFrames[nextIndex - 1].Scale : ScaleKeyFrames[nextIndex].Scale;

					float mult = (animationFrame - prevTick) / (nextTick - prevTick);
					Matrix1 = GLHelper.Scale(Matrix1, mult * (next - prev) + prev);
				}

				if (RotationKeyFrames.Count > 0) {
					float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);
					//int prevIndex = -1;
					//int nextIndex = -1;
					//
					//while (true) {
					//	nextIndex++;
					//
					//	if (nextIndex == RotationKeyFrames.Count || animationFrame < RotationKeyFrames[nextIndex].Frame)
					//		break;
					//
					//	prevIndex++;
					//}

					int prevIndex = -1;
					int nextIndex = -1;

					_findIndex(animationFrame, ref prevIndex, ref nextIndex, RotationKeyFrames);

					float prevTick = prevIndex < 0 ? 0 : RotationKeyFrames[prevIndex].Frame;
					float nextTick = nextIndex == RotationKeyFrames.Count ? Model.AnimationLength : RotationKeyFrames[nextIndex].Frame;
					Quaternion prev = prevIndex < 0 ? Quaternion.Identity : RotationKeyFrames[prevIndex].Quaternion;
					Quaternion next = nextIndex == RotationKeyFrames.Count ? RotationKeyFrames[nextIndex - 1].Quaternion : RotationKeyFrames[nextIndex].Quaternion;

					float mult = (animationFrame - prevTick) / (nextTick - prevTick);
					Matrix1 = Matrix1 * Matrix4.CreateFromQuaternion(Quaternion.Slerp(prev, next, mult));
				}
				else {
					Matrix1 = Matrix1 * TransformationMatrix;

					if (Parent != null) {
						Matrix1 = Matrix1 * Parent.InvertTransformationMatrix;
					}
				}
				
				Matrix2 = Matrix1;
				Vector3 position;

				if (PosKeyFrames.Count > 0) {
					float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);
					//int prevIndex = -1;
					//int nextIndex = -1;
					//
					//while (true) {
					//	nextIndex++;
					//
					//	if (nextIndex == PosKeyFrames.Count || animationFrame < PosKeyFrames[nextIndex].Frame)
					//		break;
					//
					//	prevIndex++;
					//}

					int prevIndex = -1;
					int nextIndex = -1;

					_findIndex(animationFrame, ref prevIndex, ref nextIndex, PosKeyFrames);

					float prevTick = prevIndex < 0 ? 0 : PosKeyFrames[prevIndex].Frame;
					float nextTick = nextIndex == PosKeyFrames.Count ? Model.AnimationLength : PosKeyFrames[nextIndex].Frame;
					Vector3 prev = prevIndex < 0 ? Position_ : PosKeyFrames[prevIndex].Position;
					Vector3 next = nextIndex == PosKeyFrames.Count ? PosKeyFrames[nextIndex - 1].Position : PosKeyFrames[nextIndex].Position;

					float mult = (animationFrame - prevTick) / (nextTick - prevTick);
					position = mult * (next - prev) + prev;
				}
				else {
					if (Parent != null) {
						position = Position_ - Parent.Position_;
						position = new Vector3(new Vector4(position.X, position.Y, position.Z, 0) * Parent.InvertTransformationMatrix);
					}
					else {
						position = Position_;
					}
				}
				
				var mat = Matrix2;
				mat.Row3 = new Vector4(position.X, position.Y, position.Z, mat.Row3.W);
				Matrix2 = mat;
				
				Mesh mesh = this;

				while (mesh.Parent != null) {
					mesh = mesh.Parent;
					Matrix2 = Matrix2 * mesh.Matrix1;
				}

				if (Parent != null) {
					var mat2 = Matrix2;
					mat2.Row3.X += Parent.Matrix2.Row3.X;
					mat2.Row3.Y += Parent.Matrix2.Row3.Y;
					mat2.Row3.Z += Parent.Matrix2.Row3.Z;
					Matrix2 = mat2;
				}
			}

			foreach (var child in Children) {
				child.CalcMatrix1();
			}
		}

		private void _findIndex(float animationFrame, ref int prevIndex, ref int nextIndex, List<ScaleKeyFrame> frames) {
			int left = 0;
			int count = frames.Count;
			int mid = left + count / 2;

			// Handle weirdo cases first
			if (animationFrame < frames[0].Frame) {
				nextIndex++;
				return;
			}

			while (count > 1) {
				if (animationFrame < frames[mid].Frame) {
					count = mid - left;
					mid = left + count / 2;
				}
				else {
					count -= mid - left;
					left = mid;
					mid = left + count / 2;
				}
			}

			prevIndex = left;
			nextIndex = left + 1;
		}

		private void _findIndex(float animationFrame, ref int prevIndex, ref int nextIndex, List<RotKeyFrame> frames) {
			int left = 0;
			int count = frames.Count;
			int mid = left + count / 2;

			// Handle weirdo cases first
			if (animationFrame < frames[0].Frame) {
				nextIndex++;
				return;
			}

			while (count > 1) {
				if (animationFrame < frames[mid].Frame) {
					count = mid - left;
					mid = left + count / 2;
				}
				else {
					count -= mid - left;
					left = mid;
					mid = left + count / 2;
				}
			}

			prevIndex = left;
			nextIndex = left + 1;
		}

		private void _findIndex(float animationFrame, ref int prevIndex, ref int nextIndex, List<PosKeyFrame> frames) {
			int left = 0;
			int count = frames.Count;
			int mid = left + count / 2;

			// Handle weirdo cases first
			if (animationFrame < frames[0].Frame) {
				nextIndex++;
				return;
			}

			while (count > 1) {
				if (animationFrame < frames[mid].Frame) {
					count = mid - left;
					mid = left + count / 2;
				}
				else {
					count -= mid - left;
					left = mid;
					mid = left + count / 2;
				}
			}

			prevIndex = left;
			nextIndex = left + 1;
		}

		public void CalcMatrix2() {
			Matrix2 = Matrix4.Identity;

			if (Model.Version < 2.2) {
				if (Parent == null && Children.Count == 0) {
					Matrix2 = GLHelper.Translate(Matrix2, -1.0f * Model.LocalBox.Center);
				}

				if (Parent != null || Children.Count > 0) {
					Matrix2 = GLHelper.Translate(Matrix2, Position_);
				}

				Matrix2 = TransformationMatrix * Matrix2;

				foreach (var child in Children) {
					child.CalcMatrix2();
				}
			}
		}

		public void SetBoundingBox(RsmBoundingBox modelBox) {
			Box = new RsmBoundingBox();

			if (Parent != null)
				Box.Min = Box.Max = new Vector3(0);

			Matrix4 myMat = TransformationMatrix;

			for (int i = 0; i < Faces.Count; i++) {
				for (int ii = 0; ii < 3; ii++) {
					Vector4 v = new Vector4(Vertices[Faces[i].VertexIds[ii]], 1);
					v = v * myMat;
					
					if (Parent != null || Children.Count > 0) {
						v += new Vector4(Position + Position_, 1);
					}

					Box.AddVector(v);
				}
			}

			modelBox.AddVector(Box.Min);
			modelBox.AddVector(Box.Max);

			foreach (var child in Children) {
				child.SetBoundingBox(modelBox);
			}
		}

		public void SetBoundingBox2(Matrix4 mat, RsmBoundingBox box) {
			var renderMatrix = (Model.Version >= 2.2 ? Matrix2 : Matrix2 * Matrix1) * mat;

			foreach (var vertice in Vertices) {
				box.AddVector(GLHelper.MultiplyWithTranslate(renderMatrix, vertice));
			}

			foreach (var child in Children) {
				child.SetBoundingBox2(Model.Version >= 2.2 ? Matrix4.Identity : Matrix1 * mat, box);
			}
		}

		public void SetBoundingBox3(Matrix4 mat, RsmBoundingBox box) {
			Matrix4 mat1 = Matrix1 * mat;
			Matrix4 mat2 = Matrix2 * mat1;
			LocalBox = new RsmBoundingBox();

			for (int i = 0; i < Faces.Count; i++) {
				for (int ii = 0; ii < 3; ii++) {
					Vector4 v = new Vector4(Vertices[Faces[i].VertexIds[ii]], 1) * mat2;
					box.AddVector(v);
					LocalBox.AddVector(v);
				}
			}

			foreach (var child in Children) {
				child.SetBoundingBox3(mat1, box);
			}
		}

		public List<Vector3> GetAllDrawnVertices(Matrix4 mat) {
			Matrix4 mat1 = Matrix1 * mat;
			Matrix4 mat2 = Matrix2 * mat1;
			LocalBox = new RsmBoundingBox();
			List<Vector3> vertices = new List<Vector3>();

			for (int i = 0; i < Faces.Count; i++) {
				for (int ii = 0; ii < 3; ii++) {
					Vector4 v = new Vector4(Vertices[Faces[i].VertexIds[ii]], 1) * mat2;
					vertices.Add(new Vector3(v));
				}
			}

			foreach (var child in Children) {
				vertices.AddRange(child.GetAllDrawnVertices(mat1));
			}

			return vertices;
		}

		public float GetTexture(int textureId, int type) {
			var frames = _textureKeyFrameGroup.GetTextureKeyFrames(textureId, type);

			if (frames == null || frames.Count == 0)
				return 0;

			float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);
			int prevIndex = -1;
			int nextIndex = -1;

			while (true) {
				nextIndex++;

				if (nextIndex == frames.Count || animationFrame < frames[nextIndex].Frame)
					break;

				prevIndex++;
			}

			float prevTick = prevIndex < 0 ? 0 : frames[prevIndex].Frame;
			float nextTick = nextIndex == frames.Count ? Model.AnimationLength : frames[nextIndex].Frame;
			float prev = prevIndex < 0 ? 0 : frames[prevIndex].Offset;
			float next = nextIndex == frames.Count ? frames[nextIndex - 1].Offset : frames[nextIndex].Offset;

			float mult = (animationFrame - prevTick) / (nextTick - prevTick);
			return mult * (next - prev) + prev;
		}

		public void SetAnimated(bool isAnimated) {
			if (TextureKeyFrameGroup.Count > 0 || RotationKeyFrames.Count > 0 || ScaleKeyFrames.Count > 0 || PosKeyFrames.Count > 0) {
				isAnimated = true;
			}

			IsAnimated = isAnimated;

			foreach (var child in Children) {
				child.SetAnimated(isAnimated);
			}
		}
	}

	public class Rsm {
		public const string RsmTexturePath = @"data\texture\";
		public const string RsmModelPath = @"data\model\";

		public string LoadedPath { get; set; }
		public RsmHeader Header { get; private set; }
		public List<string> MainMeshNames = new List<string>();
		public readonly List<ScaleKeyFrame> ScaleKeyFrames = new List<ScaleKeyFrame>();
		public Mesh MainMesh { get; private set; }
		public int AnimationLength { get; internal set; }
		public int ShadeType { get; set; }
		public byte Alpha { get; set; }
		public RsmBoundingBox RealBox = new RsmBoundingBox();
		public RsmBoundingBox DrawnBox = new RsmBoundingBox();
		public RsmBoundingBox LocalBox = new RsmBoundingBox();
		public float MaxRange { get; set; }
		public float FramesPerSecond { get; set; }
		public readonly List<Mesh> Meshes = new List<Mesh>();
		public readonly List<string> Textures = new List<string>();
		public int LastAnimationTick { get; set; }
		public int AnimationIndex { get; private set; }
		public float AnimationIndexFloat { get; private set; }
		public bool MeshesDirty { get; set; }

		public static int ForceShadeType = -1;

		public double Version {
			get { return Header.Version; }
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
				FramesPerSecond = reader.Float();
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					MainMeshNames.Add(reader.String(reader.Int32(), '\0'));
				}

				count = reader.Int32();
			}
			else if (Version >= 2.2) {
				FramesPerSecond = reader.Float();
				int numberOfTextures = reader.Int32();

				for (int i = 0; i < numberOfTextures; i++) {
					Textures.Add(reader.String(reader.Int32(), '\0'));
				}

				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					MainMeshNames.Add(reader.String(reader.Int32(), '\0'));
				}

				count = reader.Int32();
			}
			else {
				reader.Forward(16);
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					Textures.Add(reader.String(40, '\0'));
				}

				MainMeshNames.Add(reader.String(40, '\0'));
				count = reader.Int32();
			}

			for (int i = 0; i < count; i++) {
				Meshes.Add(new Mesh(this, reader) { Index = i });
			}

			// Resolve parent/child associations
			if (MainMeshNames.Count == 0) {
				MainMeshNames.Add(Meshes[0].Name);
			}

			MainMesh = Meshes.FirstOrDefault(m => m.Name == MainMeshNames[0]) ?? Meshes[0];

			_setParents(this);

			foreach (Mesh mesh in Meshes) {
				if (!String.IsNullOrEmpty(mesh.ParentName)) {
					var meshParent = Meshes.FirstOrDefault(p => p.Name == mesh.ParentName);

					if (meshParent != null) {
						mesh.Parent = meshParent;
					}
				}
			}

			if (Version < 1.6) {
				count = reader.Int32();

				for (int i = 0; i < count; i++) {
					ScaleKeyFrames.Add(new ScaleKeyFrame {
						Frame = reader.Int32(),
						Scale = new Vector3(reader.Float(), reader.Float(), reader.Float())
					});

					reader.Forward(4);
				}
			}

			// Skip volume boxes
			_updateMatrices();
			MainMesh.SetAnimated(false);
			MeshesDirty = true;
		}

		public void SetAnimationIndex(int animationIndex, float animationIndexFloat) {
			AnimationIndexFloat = animationIndexFloat;
			AnimationIndex = animationIndex;
		}

		public void SetAnimationIndex(long tick, float animationSpeed) {
			if (Version >= 2.2) {
				float fps = FramesPerSecond * animationSpeed;
				float pos = tick / (1000f / fps * AnimationLength);
				float animIndex = (pos - (int)pos) * AnimationLength;
				SetAnimationIndex((int)animIndex, animIndex);
			}
			else {
				tick = (long)(tick * animationSpeed);
				int frame = (int)(tick % AnimationLength);
				SetAnimationIndex(frame, frame);
			}
		}

		private void _updateMatrices() {
			LocalBox = new RsmBoundingBox();
			MainMesh.SetBoundingBox(LocalBox);

			MainMesh.CalcMatrix1();
			
			if (Version < 2.2) {
				MainMesh.CalcMatrix2();
			}

			Matrix4 mat = GLHelper.Scale(Matrix4.Identity, new Vector3(1, -1, 1));

			RealBox = new RsmBoundingBox();
			MainMesh.SetBoundingBox2(mat, RealBox);

			DrawnBox = new RsmBoundingBox();
			MainMesh.SetBoundingBox3(mat, DrawnBox);

			if (Version >= 2.2) {
				MainMesh.CalcMatrix1();
			}
		}

		public Rsm(MultiType data)
			: this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		private void _setParents(Rsm rsm) {
			// Bandaid, as we really want only 1 root mesh
			if (MainMeshNames.Count > 1) {
				foreach (var mesh in Meshes) {
					mesh.ParentName = "__ROOT";
				}

				Mesh root = new Mesh { Name = "__ROOT" };
				MainMesh = root;
				MainMesh.Model = rsm;
				MainMesh.Index = Meshes.Count;
				Meshes.Add(root);
			}

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

		public void Dirty() {
			MeshesDirty = true;
		}

		private List<Mesh> _orderedMeshes;

		public List<Mesh> GetOrdererMeshes() {
			if (_orderedMeshes != null)
				return _orderedMeshes;

			List<Mesh> meshes = new List<Mesh>();

			_traverse(MainMesh, meshes);
			_orderedMeshes = meshes;
			return meshes;
		}

		private void _traverse(Mesh mesh, List<Mesh> meshes) {
			meshes.Add(mesh);

			foreach (var child in mesh.Children) {
				_traverse(child, meshes);
			}
		}
	}

	public struct RotKeyFrame {
		public int Frame;
		public Quaternion Quaternion;
	}

	public struct ScaleKeyFrame {
		public int Frame;
		public Vector3 Scale;
	}

	public struct PosKeyFrame {
		public int Frame;
		public Vector3 Position;
	}

	public struct KeyFrame {
		public int Frame;
		public object Value;
	}

	public class RsmBoundingBox {
		public Vector3 Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		public Vector3 Max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

		public Vector3 Center {
			get { return (Min + Max) / 2.0f; }
		}

		public Vector3 Range {
			get { return (Max - Min) / 2.0f; }
		}

		public void AddVector(Vector4 v) {
			for (int c = 0; c < 3; c++) {
				Min[c] = Math.Min(Min[c], v[c]);
				Max[c] = Math.Max(Max[c], v[c]);
			}
		}

		public void AddVector(Vector3 v) {
			for (int c = 0; c < 3; c++) {
				Min[c] = Math.Min(Min[c], v[c]);
				Max[c] = Math.Max(Max[c], v[c]);
			}
		}
	}
}