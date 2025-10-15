using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GRF;
using GRF.FileFormats.RsmFormat;
using GRF.IO;
using OpenTK;
using Matrix3 = OpenTK.Matrix3;
using Matrix4 = OpenTK.Matrix4;
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
		public readonly Vector3[] Vertices = new Vector3[0];
		public readonly int[] TextureIndexes;
		public readonly Vector2[] TextureVertices = new Vector2[0];
		public readonly Face[] Faces = new Face[0];
		public Mesh Parent;
		public HashSet<Mesh> Children = new HashSet<Mesh>();
		public Vector3 GlobalPosition;
		public Vector3 LocalPosition;
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

		public Matrix4 Matrix1;
		public Matrix4 Matrix2;
		public Matrix4 RenderMatrix;
		public Matrix4 TransformMatrix;
		public bool IsAnimated { get; set; }
		public bool IsMatrixCalculated { get; set; }
		public bool IsBakedRenderMatrix { get; set; }
		public RsmBoundingBox LocalBox { get; set; }
		public Matrix4 TempMatrix;
		public Matrix4 TempMatrixSub;
		public int VboOffset;
		public int VboOffsetTransparent;
		public int VboOffsetAnimatedTransparent;

		public class RenderData {
			public Matrix4 Matrix = Matrix4.Identity;
			public Matrix4 MatrixSub = Matrix4.Identity;
		}

		public RenderData Render = new RenderData();

		/// <summary>
		/// Initializes a new instance of the <see cref="Mesh"/> class.
		/// </summary>
		public Mesh() {
			TransformationMatrix = Matrix4.Identity;
			InvertTransformationMatrix = Matrix4.Invert(TransformationMatrix);
			GlobalPosition = new Vector3();
			LocalPosition = new Vector3();
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
				TextureIndexes = new int[count = reader.Int32()];

				for (int i = 0; i < count; i++) {
					Textures.Add(reader.String(reader.Int32(), '\0'));

					var lastTexture = Textures.Last();
					var lastIndex = Model.Textures.FirstOrDefault(p => String.Compare(p, lastTexture, StringComparison.OrdinalIgnoreCase) == 0);

					if (lastIndex != null) {
						TextureIndexes[i] = Model.Textures.IndexOf(lastIndex);
					}
					else {
						TextureIndexes­[i] = Model.Textures.Count;
						Model.Textures.Add(lastTexture);
					}
				}
			}
			else {
				TextureIndexes = new int[count = reader.Int32()];

				for (int i = 0; i < count; i++) {
					TextureIndexes[i] = reader.Int32();
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

			LocalPosition = new Vector3(reader.Float(), reader.Float(), reader.Float());

			if (version >= 2.2) {
				GlobalPosition = new Vector3(0, 0, 0);
				RotationAngle = 0;
				RotationAxis = new Vector3(0, 0, 0);
				Scale = new Vector3(1, 1, 1);
			}
			else {
				GlobalPosition = new Vector3(reader.Float(), reader.Float(), reader.Float());
				RotationAngle = reader.Float();
				RotationAxis = new Vector3(reader.Float(), reader.Float(), reader.Float());
				Scale = new Vector3(reader.Float(), reader.Float(), reader.Float());
			}

			Vertices = new Vector3[count = reader.Int32()];

			var bytes = reader.Bytes(count * 12);
			GCHandle handle = GCHandle.Alloc(Vertices, GCHandleType.Pinned);
			var ptr = handle.AddrOfPinnedObject();
			Marshal.Copy(bytes, 0, ptr, bytes.Length);
			handle.Free();

			TextureVertices = new Vector2[count = reader.Int32()];

			for (int i = 0; i < count; i++) {
				if (version >= 1.2)
					reader.Forward(4);  // Color, skip

				TextureVertices[i] = new Vector2(reader.Float(), reader.Float());
			}

			Faces = new Face[count = reader.Int32()];

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
				face.TwoSide = reader.Int32() > 0 ? 1 : 0;

				if (version >= 1.2) {
					face.SmoothGroup[0] = face.SmoothGroup[1] = face.SmoothGroup[2] = reader.Int32();

					if (len > 24)
						face.SmoothGroup[1] = reader.Int32();
					if (len > 28)
						face.SmoothGroup[2] = reader.Int32();
					if (len > 32)
						reader.Forward(len - 32);
				}

				Faces[i] = face;
			}

			CalculateNormals();

			if (TextureVertices.Length == 0 && Faces.Length > 0) {
				int max = Faces.Max(p => p.TextureVertexIds.Max(g => g));
				TextureVertices = new Vector2[max + 1];

				for (int i = 0; i <= max; i++)
					TextureVertices[i] = new Vector2(0, 0);
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
				
				foreach (var face in Faces) {
					for (int i = 0; i < 3; i++) {
						face.VertexNormals[i] = Vector3.NormalizeFast(groups[face.SmoothGroup[0]][face.VertexIds[i]]);
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
							_textureKeyFrameGroup.AddTextureKeyFrame(textureId, (TextureTransformTypes)type, new TextureKeyFrame {
								Frame = reader.Int32(),
								Offset = reader.Float()
							});
						}
					}
				}
			}
		}

		public void CalculateNormals() {
			if (Faces.Length == 0)
				return;

			for (int i = 0; i < Faces.Length; i++) {
				var face = Faces[i];
				face.Normal = Vector3.NormalizeFast(Vector3.Cross(
					Vertices[face.VertexIds[1]] - Vertices[face.VertexIds[0]],
					Vertices[face.VertexIds[2]] - Vertices[face.VertexIds[0]]
				));
			}

			if (Model.ShadeType == 0) {
				// It doesn't care about normals at all, it's all handled with the shader in the end
				return;
			}

			if (Model.ShadeType == 1) {
				for (int i = 0; i < Faces.Length; i++) {
					var face = Faces[i];

					for (int ii = 0; ii < 3; ii++) {
						face.VertexNormals[ii] = face.Normal;
					}
				}
			}
			else if (Model.ShadeType == 2) {
				int maxGroup = Faces.Max(f => f.SmoothGroup.Max()) + 1;
				int maxVertex = Vertices.Length;

				if ((double)maxGroup * maxVertex > 1000000.0) {
					// Slowest algorithm, but works well
					var groups = new Dictionary<(int smoothGroup, int vertexId), Vector3>(Faces.Length * 3);

					for (int k = 0; k < Faces.Length; k++) {
						var face = Faces[k];
						var normal = face.Normal;

						for (int i = 0; i < 3; i++) {
							int vertexId = face.VertexIds[i];
							for (int ii = 0; ii < 3; ii++) {
								var key = (face.SmoothGroup[ii], vertexId);

								if (groups.TryGetValue(key, out var sum))
									groups[key] = sum + normal;
								else
									groups[key] = normal;
							}
						}
					}

					foreach (var face in Faces) {
						var sg = face.SmoothGroup[0];
						for (int i = 0; i < 3; i++) {
							face.VertexNormals[i] = Vector3.NormalizeFast(groups[(sg, face.VertexIds[i])]);
						}
					}
				}
				else if (maxGroup * maxVertex > 100000 || maxGroup <= 0) {
					// Hybrid algorithm, supports high values of smooth groups
					var groupMap = new Dictionary<int, int>();
					int groupCounter = 0;

					foreach (var face in Faces)
						foreach (var sg in face.SmoothGroup)
							if (!groupMap.ContainsKey(sg))
								groupMap[sg] = groupCounter++;

					maxGroup = groupCounter;
					Vector3[] accum = new Vector3[maxGroup * maxVertex];

					foreach (var face in Faces) {
						var n = face.Normal;
						for (int i = 0; i < 3; i++) {
							int v = face.VertexIds[i];
							int sg = groupMap[face.SmoothGroup[0]];
							for (int ii = 0; ii < 3; ii++) {
								accum[sg * maxVertex + v] += n;
							}
						}
					}

					foreach (var face in Faces) {
						var sg = groupMap[face.SmoothGroup[0]];
						for (int i = 0; i < 3; i++) {
							int v = face.VertexIds[i];
							face.VertexNormals[i] = Vector3.NormalizeFast(accum[sg * maxVertex + v]);
						}
					}
				}
				else {
					// Fastest algorithm, takes a lot of memory though
					Vector3[] accum = new Vector3[maxGroup * maxVertex];

					foreach (var face in Faces) {
						var n = face.Normal;
						for (int i = 0; i < 3; i++) {
							int v = face.VertexIds[i];
							for (int ii = 0; ii < 3; ii++) {
								int sg = face.SmoothGroup[ii];
								accum[sg * maxVertex + v] += n;
							}
						}
					}

					foreach (var face in Faces) {
						var sg = face.SmoothGroup[0];
						for (int i = 0; i < 3; i++) {
							int v = face.VertexIds[i];
							face.VertexNormals[i] = Vector3.NormalizeFast(accum[sg * maxVertex + v]);
						}
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
				Matrix1 = GLHelper.Translate(ref Matrix1, ref GlobalPosition);

				if (RotationKeyFrames.Count == 0) {
					if (Math.Abs(RotationAngle) > 0.01) {
						Matrix1 = GLHelper.Rotate(ref Matrix1, RotationAngle, RotationAxis);
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

						if (next >= RotationKeyFrames.Count && RotationKeyFrames.Count == 1) {
							next = 0;
						}

						if (next < RotationKeyFrames.Count) {
							float interval = (tick - RotationKeyFrames[current].Frame) / (RotationKeyFrames[next].Frame - RotationKeyFrames[current].Frame);
							Quaternion quat = Quaternion.Slerp(RotationKeyFrames[current].Quaternion, RotationKeyFrames[next].Quaternion, interval);
							quat = Quaternion.Normalize(quat);
							Matrix1 = Matrix4.CreateFromQuaternion(quat) * Matrix1;
						}
					}
				}

				Matrix1 = GLHelper.Scale(ref Matrix1, Scale);

				//TransformMatrix = Matrix1 * (Parent != null ? Parent.RenderMatrix : Matrix4.Identity);
				//RenderMatrix = Matrix2 * TransformMatrix;
				//
				//IsBakedRenderMatrix = true;
			}
			else {
				Matrix2 = Matrix4.Identity;

				if (ScaleKeyFrames.Count > 0) {
					float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);
					
					int prevIndex = -1;
					int nextIndex = -1;

					_findIndex(animationFrame, ref prevIndex, ref nextIndex, ScaleKeyFrames);

					float prevTick = prevIndex < 0 ? 0 : ScaleKeyFrames[prevIndex].Frame;
					float nextTick = nextIndex == ScaleKeyFrames.Count ? Model.AnimationLength : ScaleKeyFrames[nextIndex].Frame;
					Vector3 prev = prevIndex < 0 ? new Vector3(1) : ScaleKeyFrames[prevIndex].Scale;
					Vector3 next = nextIndex == ScaleKeyFrames.Count ? ScaleKeyFrames[nextIndex - 1].Scale : ScaleKeyFrames[nextIndex].Scale;

					float mult = (animationFrame - prevTick) / (nextTick - prevTick);
					Matrix1 = GLHelper.Scale(ref Matrix1, mult * (next - prev) + prev);
				}

				if (RotationKeyFrames.Count > 0) {
					float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);

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
				
				Vector3 position;

				if (PosKeyFrames.Count > 0) {
					float animationFrame = Model.AnimationIndexFloat % Math.Max(1, Model.AnimationLength);

					int prevIndex = -1;
					int nextIndex = -1;

					_findIndex(animationFrame, ref prevIndex, ref nextIndex, PosKeyFrames);

					float prevTick = prevIndex < 0 ? 0 : PosKeyFrames[prevIndex].Frame;
					float nextTick = nextIndex == PosKeyFrames.Count ? Model.AnimationLength : PosKeyFrames[nextIndex].Frame;
					Vector3 prev = prevIndex < 0 ? LocalPosition : PosKeyFrames[prevIndex].Position;
					Vector3 next = nextIndex == PosKeyFrames.Count ? PosKeyFrames[nextIndex - 1].Position : PosKeyFrames[nextIndex].Position;

					float mult = (animationFrame - prevTick) / (nextTick - prevTick);
					position = mult * (next - prev) + prev;
				}
				else {
					if (Parent != null) {
						position = LocalPosition - Parent.LocalPosition;
						position = new Vector3(new Vector4(position.X, position.Y, position.Z, 0) * Parent.InvertTransformationMatrix);
					}
					else {
						position = LocalPosition;
					}
				}

				Matrix1 = Matrix1 * Matrix4.CreateTranslation(position);

				// Use Matrix2 as the final matrix, this is actually faster since RSM2 has no local transform
				Matrix2 = Matrix1;
				
				if (Parent != null)
					Matrix2 = Matrix2 * Parent.Matrix2;

				IsBakedRenderMatrix = true;
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
					Matrix2 = GLHelper.Translate(ref Matrix2, LocalPosition);
				}

				if (Parent != null || Children.Count > 0) {
					Matrix2 = GLHelper.Translate(ref Matrix2, LocalPosition);
				}

				Matrix2 = TransformationMatrix * Matrix2;

				foreach (var child in Children) {
					child.CalcMatrix2();
				}
			}
		}

		public void FlattenModel(Matrix4 mat) {
			if (this.Model.Version >= 2.2) {
				Matrix4 mat2 = Matrix2;

				for (int i = 0; i < Vertices.Length; i++) {
					Vertices[i] = new Vector3(new Vector4(Vertices[i], 1) * mat2);
				}

				foreach (var mesh in Children) {
					mesh.FlattenModel(mat);
				}
			}
			else {
				Matrix4 mat1 = Matrix1 * mat;
				Matrix4 mat2 = Matrix2 * mat1;

				for (int i = 0; i < Vertices.Length; i++) {
					Vertices[i] = new Vector3(new Vector4(Vertices[i], 1) * mat2);
				}

				foreach (var mesh in Children) {
					mesh.FlattenModel(mat1);
				}
			}
		}

		public List<Vector3> GetAllDrawnVertices(Matrix4 mat) {
			Matrix4 mat1 = Matrix1 * mat;
			Matrix4 mat2 = Matrix2 * mat1;
			LocalBox = new RsmBoundingBox();
			List<Vector3> vertices = new List<Vector3>();

			for (int i = 0; i < Faces.Length; i++) {
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

		public float GetTexture(int textureId, TextureTransformTypes type) {
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

		/// <summary>
		/// Retrieves the BoundingBox for all vertices, even unused ones.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="box"></param>
		public void SetVerticesBoundingBox(Matrix4 mat, RsmBoundingBox box) {
			var renderMatrix = Model.Version >= 2.2 ? Matrix2 * mat : Matrix2 * Matrix1 * mat;
			
			foreach (var vertice in Vertices) {
				var v = GLHelper.MultiplyWithTranslate(ref renderMatrix, vertice);
				box.AddVector(ref v);
			}

			foreach (var child in Children) {
				child.SetVerticesBoundingBox(Model.Version >= 2.2 ? Matrix4.Identity : Matrix1 * mat, box);
			}
		}

		public struct RVerticeCalc {
			public int VerticesCount;
			public Matrix4 Matrix;
			public Vector3 Offset;

			public bool IsEqual(RVerticeCalc rvc) {
				return rvc.VerticesCount == VerticesCount &&
					rvc.Matrix == Matrix &&
					rvc.Offset == Offset;
			}
		};

		private RVerticeCalc _lastDrawnLocalBB = new RVerticeCalc();

		public void SetDrawnAndLocalBoundingBox(ref Matrix4 mat, RsmBoundingBox box) {
			if (Model.Version >= 2.2) {
				RVerticeCalc current = new RVerticeCalc { Matrix = Matrix2, Offset = GlobalPosition, VerticesCount = Vertices.Length };
			
				if (!_lastDrawnLocalBB.IsEqual(current)) {
					LocalBox = new RsmBoundingBox();
			
					for (int i = 0; i < Faces.Length; i++) {
						for (int ii = 0; ii < 3; ii++) {
							Vector3 v = GLHelper.MultiplyWithTranslate(ref Matrix2, Vertices[Faces[i].VertexIds[ii]] + GlobalPosition);
							box.AddVector(ref v);
							LocalBox.AddVector(ref v);
						}
					}

					_lastDrawnLocalBB = current;
				}
				else {
					box.AddVectorMin(ref LocalBox.Min);
					box.AddVectorMax(ref LocalBox.Max);
				}
			
				foreach (var child in Children) {
					child.SetDrawnAndLocalBoundingBox(ref mat, box);
				}
			}
			else {
				Matrix4 mat1 = Matrix1 * mat;
				Matrix4 mat2 = Matrix2 * mat1;

				RVerticeCalc current = new RVerticeCalc { Matrix = mat2, Offset = GlobalPosition, VerticesCount = Vertices.Length };

				if (!_lastDrawnLocalBB.IsEqual(current)) {
					LocalBox = new RsmBoundingBox();

					for (int i = 0; i < Faces.Length; i++) {
						for (int ii = 0; ii < 3; ii++) {
							//Vector4 v = new Vector4(Vertices[Faces[i].VertexIds[ii]], 1) * mat2;
							Vector3 v = GLHelper.MultiplyWithTranslate(ref mat2, Vertices[Faces[i].VertexIds[ii]]);
							box.AddVector(ref v);
							LocalBox.AddVector(ref v);
						}
					}

					_lastDrawnLocalBB = current;
				}
				else {
					box.AddVectorMin(ref LocalBox.Min);
					box.AddVectorMax(ref LocalBox.Max);
				}

				foreach (var child in Children) {
					child.SetDrawnAndLocalBoundingBox(ref mat1, box);
				}
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
		public RsmBoundingBox VerticesBox = new RsmBoundingBox();
		public RsmBoundingBox DrawnBox = new RsmBoundingBox();
		public float MaxRange { get; set; }
		public float FramesPerSecond { get; set; }
		public readonly List<Mesh> Meshes = new List<Mesh>();
		public readonly List<string> Textures = new List<string>();
		public int AnimationIndex { get; private set; }
		public float AnimationIndexFloat { get; private set; }
		public bool MeshesDirty { get; set; }
		public bool NeedsSorting { get; set; }

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
			SetupBoundingBoxes();
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

		public void SetupBoundingBoxes() {
			if (MainMesh == null)
				return;

			MainMesh.CalcMatrix1();
			
			if (Version < 2.2) {
				MainMesh.CalcMatrix2();
			}
			
			Matrix4 mat = GLHelper.Scale(Matrix4.Identity, new Vector3(1, -1, 1));
			
			// **VerticesBox is only used for RSM1**
			// RSM1 centers the Model around the Vertices, not the Faces
			if (Version < 2.2) {
				VerticesBox = new RsmBoundingBox();
				MainMesh.SetVerticesBoundingBox(mat, VerticesBox);
			}
			
			DrawnBox = new RsmBoundingBox();
			MainMesh.SetDrawnAndLocalBoundingBox(ref mat, DrawnBox);
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

		public void AddVector(ref Vector4 v) {
			for (int c = 0; c < 3; c++) {
				Min[c] = Math.Min(Min[c], v[c]);
				Max[c] = Math.Max(Max[c], v[c]);
			}
		}

		public void AddVector(ref Vector3 v) {
			for (int c = 0; c < 3; c++) {
				Min[c] = Math.Min(Min[c], v[c]);
				Max[c] = Math.Max(Max[c], v[c]);
			}
		}

		public void AddVectorMin(ref Vector3 v) {
			for (int c = 0; c < 3; c++) {
				Min[c] = Math.Min(Min[c], v[c]);
			}
		}

		public void AddVectorMax(ref Vector3 v) {
			for (int c = 0; c < 3; c++) {
				Max[c] = Math.Max(Max[c], v[c]);
			}
		}
	}
}