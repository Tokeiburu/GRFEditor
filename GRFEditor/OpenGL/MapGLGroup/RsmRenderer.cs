using System;
using System.Collections.Generic;
using GRF.FileFormats.RswFormat;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapGLGroup {
	public class SharedRsmRenderer : MapGLObject {
		private readonly RendererLoadRequest _request;
		private readonly Rsm _rsm;
		public readonly Gnd Gnd;
		private readonly Rsw _rsw;
		public Dictionary<int, RenderInfo> RenderInfos = new Dictionary<int, RenderInfo>();
		public RenderInfo RenderInfo = new RenderInfo();
		public RenderInfo RenderInfoTransparent = new RenderInfo();

		static SharedRsmRenderer() {
			ForceShader = -1;
		}

		public SharedRsmRenderer(RendererLoadRequest request, Shader shader, Rsm rsm, Gnd gnd = null, Rsw rsw = null) {
			Shader = shader;
			_request = request;
			_rsm = rsm;
			Gnd = gnd;
			_rsw = rsw;
		}

		public Rsm Rsm {
			get { return _rsm; }
		}

		public static int ForceShader { get; set; }

		public override void Load(OpenGLViewport viewport) {
			if (IsLoaded) {
				return;
			}

			foreach (var texture in _rsm.Textures) {
				Textures.Add(TextureManager.LoadTextureAsync(texture, Rsm.RsmTexturePath + texture, TextureRenderMode.RsmTexture, _request));
			}

			_initMeshInfo(_rsm.MainMesh);
			IsLoaded = true;
		}

		private void _initMeshInfo(Mesh mesh) {
			RenderInfo.Vertices = new List<Vertex>();
			RenderInfoTransparent.Vertices = new List<Vertex>();

			Matrix4 matrix = Matrix4.Identity;
			_initMeshInfo(mesh, ref matrix);

			_addRenderInfo(RenderInfo.Vertices, RenderInfo);
			_addRenderInfo(RenderInfoTransparent.Vertices, RenderInfoTransparent);
			RenderInfo.Vertices = null;
			RenderInfoTransparent.Vertices = null;
		}

		private void _initMeshInfo(Mesh mesh, ref Matrix4 matrix) {
			Dictionary<int, List<Vertex>> verts = new Dictionary<int, List<Vertex>>();

			for (int i = 0; i < mesh.Faces.Count; i++) {
				for (int ii = 0; ii < 3; ii++) {
					List<Vertex> l;

					if (!verts.TryGetValue(mesh.Faces[i].TextureId, out l)) {
						l = new List<Vertex>(1024);
						verts[mesh.Faces[i].TextureId] = l;
					}

					l.Add(new Vertex(
						mesh.Vertices[mesh.Faces[i].VertexIds[ii]],
						mesh.TextureVertices[mesh.Faces[i].TextureVertexIds[ii]],
						mesh.Faces[i].VertexNormals[ii])
					);
				}
			}

			RenderInfos[mesh.Index] = new RenderInfo();

			mesh.VboOffset = RenderInfo.Vertices.Count;
			mesh.VboOffsetTransparent = RenderInfoTransparent.Vertices.Count;

			foreach (var vert in verts) {
				var ri = Textures[mesh.TextureIndexes[vert.Key]].IsSemiTransparent ? RenderInfoTransparent : RenderInfo;
				ri.Indices.Add(new VboIndex { Texture = vert.Key, Begin = ri.Vertices.Count, Count = vert.Value.Count });
				ri.Vertices.AddRange(vert.Value);
			}

			if (mesh.Model.Version >= 2.2) {
				RenderInfos[mesh.Index].Matrix = mesh.Matrix2;
				RenderInfos[mesh.Index].MatrixSub = Matrix4.Identity;
			}
			else {
				RenderInfos[mesh.Index].Matrix = mesh.Matrix2 * mesh.Matrix1 * matrix;
				RenderInfos[mesh.Index].MatrixSub = mesh.Matrix1 * matrix;
			}

			foreach (var child in mesh.Children) {
				_initMeshInfo(child, ref RenderInfos[mesh.Index].MatrixSub);
			}
		}

		private void _addRenderInfo(List<Vertex> allVerts, RenderInfo ri) {
			if (allVerts.Count > 0) {
				if (_request.CancelRequired())
					return;

				ri.CreateVao();
				ri.Vbo = new Vbo();
				ri.Vbo.SetData(allVerts, BufferUsageHint.StaticDraw);

				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.EnableVertexAttribArray(2);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
				GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));
			}
		}

		public void Render(OpenGLViewport viewport, ref Matrix4 modelMatrixCache) {
			Shader.Use();
			Shader.SetMatrix4("modelMatrix2", modelMatrixCache);
			Shader.SetInt("shadeType", ForceShader > 0 ? ForceShader : _rsm.ShadeType);

			Render(viewport);
		}

		public override void Render(OpenGLViewport viewport) {
			Shader.Use();
			Shader.SetInt("shadeType", ForceShader > 0 ? ForceShader : _rsm.ShadeType);

			if (_rsw != null) {
				
			}
			else {
				Vector3 lightDirection = viewport.LightDirection;
				lightDirection *= -1;
				
				Shader.SetVector3("lightAmbient", viewport.LightAmbient);
				Shader.SetVector3("lightDiffuse", new Vector3(1f, 1f, 1f));
				Shader.SetFloat("lightIntensity", 0.5f);
				Shader.SetVector3("lightDirection", lightDirection);
				Shader.SetVector3("lightPosition", viewport.CameraPosition);
			}

			//_rsm.Dirty();
			//_rsm.Meshes.ForEach(p => p.MatrixDirty = true);

			if (_rsm.MeshesDirty) {
				var matrix = Matrix4.Identity;
				_updateMeshMatrix(_rsm.MainMesh, ref matrix);
				_rsm.MeshesDirty = false;
			}

			var meshes = _rsm.GetOrdererMeshes();
			_renderMesh(meshes, RenderInfo, false);

			if (RenderInfoTransparent.Indices.Count > 0) {
				Shader.SetFloat("discardValue", 0.02f);
				_renderMesh(meshes, RenderInfoTransparent, true);
				Shader.SetFloat("discardValue", 0.8f);
			}
		}

		public void RenderDynamicModels(OpenGLViewport viewport, List<Matrix4> modelMatrixCaches) {
			if (!IsLoaded)
				Load(viewport);

			Shader.Use();
			Shader.SetInt("shadeType", ForceShader > 0 ? ForceShader : _rsm.ShadeType);

			if (_rsm.MeshesDirty) {
				var matrix = Matrix4.Identity;
				_updateMeshMatrix(_rsm.MainMesh, ref matrix);
				_rsm.MeshesDirty = false;
			}

			var meshes = _rsm.GetOrdererMeshes();
			_renderMesh(meshes, RenderInfo, false, modelMatrixCaches);

			if (RenderInfoTransparent.Indices.Count > 0) {
				Shader.SetFloat("discardValue", 0.02f);
				_renderMesh(meshes, RenderInfoTransparent, true, modelMatrixCaches);
				Shader.SetFloat("discardValue", 0.8f);
			}
		}

		private void _updateMeshMatrix(Mesh mesh, ref Matrix4 matrix) {
			if (mesh.MatrixDirty) {
				mesh.MatrixDirty = false;
				mesh.CalcMatrix1(Environment.TickCount);

				if (mesh.Model.Version >= 2.2) {
					RenderInfos[mesh.Index].Matrix = mesh.Matrix2;
					RenderInfos[mesh.Index].MatrixSub = Matrix4.Identity;
				}
				else {
					RenderInfos[mesh.Index].Matrix = mesh.Matrix2 * mesh.Matrix1 * matrix;
					RenderInfos[mesh.Index].MatrixSub = mesh.Matrix1 * matrix;
				}
			}

			foreach (var child in mesh.Children) {
				_updateMeshMatrix(child, ref RenderInfos[mesh.Index].MatrixSub);
			}
		}

		private void _renderMesh(List<Mesh> meshes, RenderInfo ri, bool transparent, List<Matrix4> modelMatrixCaches = null) {
			if (ri.Vbo != null) {
				bool repeat = false;
				ri.BindVao();
				ri.Vbo.Bind();

				Mesh mesh = null;
				int meshIndex = -1;
				int startVboOffset = 0;
				int vboCount = 0;

				foreach (var vboIndex in ri.Indices) {
					while (vboCount == 0) {
						meshIndex++;

						if (meshIndex >= meshes.Count)
							break;

						mesh = meshes[meshIndex];
						startVboOffset = transparent ? meshes[meshIndex].VboOffsetTransparent : meshes[meshIndex].VboOffset;
						vboCount = meshIndex + 1 < meshes.Count ? (transparent ? meshes[meshIndex + 1].VboOffsetTransparent : meshes[meshIndex + 1].VboOffset) - startVboOffset : int.MaxValue;

						if (vboCount > 0)
							Shader.SetMatrix4("modelMatrix", RenderInfos[mesh.Index].Matrix);
					}

					if (meshIndex >= meshes.Count)
						break;

					if (mesh.Model.Version >= 2.3 && mesh.TextureKeyFrameGroup.Count > 0) {
						Vector2 texTranslate = new Vector2(0);
						Vector2 texMult = new Vector2(1);
						Matrix4 texRot = Matrix4.Identity;

						foreach (var type in mesh.TextureKeyFrameGroup.Types) {
							if (mesh.TextureKeyFrameGroup.HasTextureAnimation(vboIndex.Texture, type)) {
								float offset = mesh.GetTexture(vboIndex.Texture, type);
								repeat = true;

								switch (type) {
									case 0:
										texTranslate.X += offset;
										break;
									case 1:
										texTranslate.Y += offset;
										break;
									case 2:
										texMult.X = offset;
										break;
									case 3:
										texMult.Y = offset;
										break;
									case 4:
										texRot = GLHelper.Rotate(texRot, offset, new Vector3(0, 0, 1));
										break;
								}
							}
						}

						Shader.SetFloat("textureAnimToggle", 1);
						Shader.SetVector2("texTranslate", texTranslate);
						Shader.SetVector2("texMult", texMult);
						Shader.SetMatrix4("texRot", texRot);
					}

					if (!Textures[mesh.TextureIndexes[vboIndex.Texture]].IsLoaded && Textures[mesh.TextureIndexes[vboIndex.Texture]].Image == null) {
						vboCount -= vboIndex.Count;
						continue;
					}

					Textures[mesh.TextureIndexes[vboIndex.Texture]].Bind();

					if (repeat) {
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
					}

					if (modelMatrixCaches != null) {
						foreach (var matrix in modelMatrixCaches) {
							Shader.SetMatrix4("modelMatrix2", matrix);
							GL.DrawArrays(PrimitiveType.Triangles, vboIndex.Begin, vboIndex.Count);
						}
					}
					else {
						GL.DrawArrays(PrimitiveType.Triangles, vboIndex.Begin, vboIndex.Count);
					}

					if (mesh.Model.Version >= 2.3 && mesh.TextureKeyFrameGroup.Count > 0) {
						Shader.SetFloat("textureAnimToggle", 0);

						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
					}

					vboCount -= vboIndex.Count;
				}
				
			}
		}

		public static void UpdateShader(Shader shader, OpenGLViewport viewport) {
			shader.Use();
			shader.SetMatrix4("cameraMatrix", viewport.View);
			shader.SetMatrix4("projectionMatrix", viewport.Projection);
		}

		public static void SetupRswLight(Shader shader, Rsw rsw) {
			shader.Use();

			Matrix3 mat = Matrix3.Identity;
			mat = GLHelper.Rotate(mat, -GLHelper.ToRad(rsw.Light.Latitude), new Vector3(1, 0, 0));
			mat = GLHelper.Rotate(mat, GLHelper.ToRad(rsw.Light.Longitude), new Vector3(0, 1, 0));

			Vector3 lightDirection = mat * new Vector3(0, 1, 0);
			shader.SetVector3("lightAmbient", new Vector3(rsw.Light.AmbientRed, rsw.Light.AmbientGreen, rsw.Light.AmbientBlue));
			shader.SetVector3("lightDiffuse", new Vector3(rsw.Light.DiffuseRed, rsw.Light.DiffuseGreen, rsw.Light.DiffuseBlue));
			shader.SetFloat("lightIntensity", rsw.Light.Intensity);
			shader.SetVector3("lightDirection", lightDirection);
		}

		public override void Unload() {
			foreach (var texture in Textures) {
				TextureManager.UnloadTexture(texture.Resource);
			}

			foreach (var ri in RenderInfos.Values) {
				ri.Unload();
			}
		}

		public override int GetHashCode() {
			return _rsm != null ? _rsm.GetHashCode() : base.GetHashCode();
		}
	}
}
