using System;
using System.Collections.Generic;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using static GRFEditor.OpenGL.MapComponents.Mesh;
using Mesh = GRFEditor.OpenGL.MapComponents.Mesh;
using Rsm = GRFEditor.OpenGL.MapComponents.Rsm;

namespace GRFEditor.OpenGL.MapRenderers {
	public class SharedRsmRenderer : Renderer {
		private readonly RendererLoadRequest _request;
		private readonly Rsm _rsm;
		public readonly Gnd Gnd;
		private readonly Rsw _rsw;
		public RenderInfo RenderInfo = new RenderInfo();
		public RenderInfo RenderInfoTransparent = new RenderInfo();
		public Dictionary<int, RenderData> RenderDatas = new Dictionary<int, RenderData>();

		public Vbo Instance;
		public Matrix4[] InstanceMatrices;

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

		public override void Load(OpenGLViewport viewport) {
			if (IsLoaded) {
				return;
			}

			foreach (var texture in _rsm.Textures) {
				Textures.Add(TextureManager.LoadTextureAsync(texture, Rsm.RsmTexturePath + texture, TextureRenderMode.RsmTexture, _request));
			}

			_initMeshInfo(_rsm.MainMesh);
			_rsm.Dirty();
			IsLoaded = true;
		}

		private List<int> _textureRedirect = new List<int>();

		public void LoadSpecial(OpenGLViewport viewport, MapRenderer mapRenderer) {
			if (IsLoaded)
				return;

			_textureRedirect.Clear();
			var textures = mapRenderer._textures;

			for (int i = 0; i < _rsm.Textures.Count; i++) {
				var texture = _rsm.Textures[i];

				if (textures.TryGetValue(texture, out Texture foundTexture)) {
					_textureRedirect.Add(mapRenderer.Textures.IndexOf(foundTexture));
				}
				else {
					var tex = TextureManager.LoadTextureAsync(texture, Rsm.RsmTexturePath + texture, TextureRenderMode.RsmTexture, _request);
					mapRenderer._textures[texture] = tex;
					mapRenderer.Textures.Add(tex);
					_textureRedirect.Add(mapRenderer.Textures.Count - 1);
				}
			}

			_initMeshInfoSpecial(_rsm.MainMesh, mapRenderer);

			_rsm.Dirty();
			IsLoaded = true;
		}

		private void _initMeshInfo(Mesh mesh) {
			RenderInfo.Vertices = new List<Vertex>();
			RenderInfoTransparent.Vertices = new List<Vertex>();

			Matrix4 matrix = Matrix4.Identity;
			_initMeshInfoSub(mesh, ref matrix);

			_addRenderInfo(RenderInfo.Vertices, RenderInfo);
			_addRenderInfo(RenderInfoTransparent.Vertices, RenderInfoTransparent);
			RenderInfo.Vertices = null;
			RenderInfoTransparent.Vertices = null;
		}

		private void _initMeshInfoSpecial(Mesh mesh, MapRenderer mapRenderer) {
			Matrix4 matrix = Matrix4.Identity;
			_initMeshInfoSubSpecial(mesh, ref matrix, mapRenderer);
		}

		private void _initMeshInfoSubSpecial(Mesh mesh, ref Matrix4 matrix, MapRenderer mapRenderer) {
			var dict = new Dictionary<int, List<MapComponents.Face>>();
			Dictionary<int, VertexP3N2N3[]> verts = new Dictionary<int, VertexP3N2N3[]>();

			foreach (var face in mesh.Faces) {
				var key = face.TextureId;
				if (!dict.TryGetValue(key, out var list)) {
					list = new List<MapComponents.Face>(64);
					dict[key] = list;
				}
				list.Add(face);
			}

			foreach (var entry in dict) {
				var l = new VertexP3N2N3[entry.Value.Count * 3];
				verts[entry.Key] = l;
				int idx = 0;

				foreach (var face in entry.Value) {
					for (int ii = 0; ii < 3; ii++) {
						l[idx++] = new VertexP3N2N3(
						mesh.Vertices[face.VertexIds[ii]],
						mesh.TextureVertices[face.TextureVertexIds[ii]],
						face.VertexNormals[ii],
						face.TwoSide);
					}
				}
			}

			foreach (var vert in verts) {
				var tri = mapRenderer.TextureGroups[_textureRedirect[vert.Key]];
				tri.Indices.Add(new VboIndex { Texture = -1, Begin = tri.VboOffset, Count = vert.Value.Length });
				tri.AddVertexArray(vert.Value);
			}

			if (mesh.IsBakedRenderMatrix) {
				RenderDatas[mesh.Index].MatrixSub = Matrix4.Identity;
				RenderDatas[mesh.Index].Matrix = mesh.Matrix2;
			}
			else {
				RenderDatas[mesh.Index].MatrixSub = mesh.Matrix1 * matrix;
				RenderDatas[mesh.Index].Matrix = mesh.Matrix2 * RenderDatas[mesh.Index].MatrixSub;
			}

			foreach (var child in mesh.Children) {
				_initMeshInfoSub(child, ref RenderDatas[mesh.Index].MatrixSub);
			}
		}

		private void _initMeshInfoSub(Mesh mesh, ref Matrix4 matrix) {
			// VertexP3N2N3
			var dict = new Dictionary<int, List<MapComponents.Face>>();
			Dictionary<int, Vertex[]> verts = new Dictionary<int, Vertex[]>();

			foreach (var face in mesh.Faces) {
				var key = face.TextureId;
				if (!dict.TryGetValue(key, out var list)) {
					list = new List<MapComponents.Face>(64);
					dict[key] = list;
				}
				list.Add(face);
			}
			
			foreach (var entry in dict) {
				var l = new Vertex[entry.Value.Count * 3];
				verts[entry.Key] = l;
				int idx = 0;
			
				foreach (var face in entry.Value) {
					for (int ii = 0; ii < 3; ii++) {
						l[idx++] = new Vertex(
						mesh.Vertices[face.VertexIds[ii]],
						mesh.TextureVertices[face.TextureVertexIds[ii]],
						face.VertexNormals[ii],
						face.TwoSide);
					}
				}
			}

			mesh.VboOffset = RenderInfo.Vertices.Count;
			mesh.VboOffsetTransparent = RenderInfoTransparent.Vertices.Count;
			RenderDatas[mesh.Index] = new RenderData();

			foreach (var vert in verts) {
				var ri = Textures[mesh.TextureIndexes[vert.Key]].IsSemiTransparent ? RenderInfoTransparent : RenderInfo;
				ri.Indices.Add(new VboIndex { Texture = mesh.TextureIndexes[vert.Key], MeshTextureIndice = vert.Key, Begin = ri.Vertices.Count, Count = vert.Value.Length });
				ri.Vertices.AddRange(vert.Value);
			}

			if (mesh.IsBakedRenderMatrix) {
				RenderDatas[mesh.Index].MatrixSub = Matrix4.Identity;
				RenderDatas[mesh.Index].Matrix = mesh.Matrix2;
			}
			else {
				RenderDatas[mesh.Index].MatrixSub = mesh.Matrix1 * matrix;
				RenderDatas[mesh.Index].Matrix = mesh.Matrix2 * RenderDatas[mesh.Index].MatrixSub;
			}

			foreach (var child in mesh.Children) {
				_initMeshInfoSub(child, ref RenderDatas[mesh.Index].MatrixSub);
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
				GL.EnableVertexAttribArray(3);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
				GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));
				GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 8 * sizeof(float));
			}
		}

		public void Render(OpenGLViewport viewport, ref Matrix4 modelMatrixCache) {
			if (viewport.RenderPass != RenderMode.OpaqueTextures &&
				viewport.RenderPass != RenderMode.TransparentTextures &&
				viewport.RenderPass != RenderMode.OpaqueTransparentTextures &&
				viewport.RenderPass != RenderMode.AnimatedTransparentTextures)
				return;

			Shader.Use();

			if (viewport.RenderOptions.ShowWireframeView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

			if (viewport.RenderOptions.ShowPointView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);

			if (viewport.RenderOptions.ShowWireframeView || viewport.RenderOptions.ShowPointView) {
				Shader.SetBool("fixedColor", true);
			}
			else {
				Shader.SetBool("fixedColor", false);
			}

			switch(viewport.RenderPass) {
				case RenderMode.OpaqueTextures:
					GL.DepthMask(true);
					Shader.SetFloat("discardValue", 0.8f);
					Shader.SetInt("discardAlphaMode", 0);
					break;
				case RenderMode.OpaqueTransparentTextures:
					GL.DepthMask(true);
					Shader.SetFloat("discardValue", 0.0f);
					Shader.SetInt("discardAlphaMode", 1);
					break;
				case RenderMode.TransparentTextures:
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

					GL.DepthMask(false);
					Shader.SetFloat("discardValue", 0.0f);
					Shader.SetInt("discardAlphaMode", 2);
					break;
				case RenderMode.AnimatedTransparentTextures:
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

					GL.DepthMask(false);
					Shader.SetFloat("discardValue", 0.0f);
					Shader.SetInt("discardAlphaMode", 0);
					break;
			}
			
			Shader.SetMatrix4("instanceMatrix", ref modelMatrixCache);
			Shader.SetInt("shadeType", viewport.RenderOptions.ForceShader > 0 ? viewport.RenderOptions.ForceShader : _rsm.ShadeType);

			Render(viewport);

			if (viewport.RenderOptions.ShowWireframeView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

			if (viewport.RenderOptions.ShowPointView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
		}

		public override void Render(OpenGLViewport viewport) {
			if (viewport.RenderPass != RenderMode.OpaqueTextures && 
				viewport.RenderPass != RenderMode.OpaqueTransparentTextures && 
				viewport.RenderPass != RenderMode.TransparentTextures &&
				viewport.RenderPass != RenderMode.AnimatedTransparentTextures)
				return;

			Shader.Use();
			Shader.SetInt("shadeType", viewport.RenderOptions.ForceShader > 0 ? viewport.RenderOptions.ForceShader : _rsm.ShadeType);

			if (_rsw != null) {
				
			}
			else {
				Vector3 lightDirection = viewport.LightDirection;
				lightDirection *= -1;
				
				Shader.SetVector3("lightAmbient", ref viewport.LightAmbient);
				Shader.SetVector3("lightDiffuse", new Vector3(1f, 1f, 1f));
				Shader.SetFloat("lightIntensity", 0.5f);
				Shader.SetVector3("lightDirection", ref lightDirection);
				Shader.SetVector3("lightPosition", ref viewport.Camera.Position);
			}

			// Gotta reverse triangles
			if (_rsm.Version >= 2.2)
				GL.FrontFace(FrontFaceDirection.Cw);

			_render(viewport);

			if (_rsm.Version >= 2.2)
				GL.FrontFace(FrontFaceDirection.Ccw);
		}

		private void _render(OpenGLViewport viewport) {
			if (_rsm.MeshesDirty) {
				var matrix = Matrix4.Identity;
				_updateMeshMatrix(_rsm.MainMesh, ref matrix, true);
				_rsm.MeshesDirty = false;
			}

			var meshes = _rsm.GetOrdererMeshes();

			switch(viewport.RenderPass) {
				case RenderMode.OpaqueTextures:	// Draw opaque textures only
					_renderMesh(viewport, meshes, RenderInfo, false);
					break;
				case RenderMode.TransparentTextures:
				case RenderMode.OpaqueTransparentTextures:
				case RenderMode.AnimatedTransparentTextures:
					if (RenderInfoTransparent.Indices.Count == 0)
						return;

					_renderMesh(viewport, meshes, RenderInfoTransparent, true);
					break;
			}
		}

		public void RenderDynamicModels(OpenGLViewport viewport) {
			if (!IsLoaded)
				Load(viewport);

			Shader.SetInt("shadeType", viewport.RenderOptions.ForceShader > 0 ? viewport.RenderOptions.ForceShader : _rsm.ShadeType);

			_render(viewport);
		}

		private void _updateMeshMatrix(Mesh mesh, ref Matrix4 matrix, bool calcMatrix) {
			if (mesh.IsAnimated) {
				if (calcMatrix) {
					calcMatrix = false;
					mesh.CalcMatrix1();
				}

				if (mesh.IsBakedRenderMatrix) {
					RenderDatas[mesh.Index].MatrixSub = Matrix4.Identity;
					RenderDatas[mesh.Index].Matrix = mesh.Matrix2;
				}
				else {
					RenderDatas[mesh.Index].MatrixSub = mesh.Matrix1 * matrix;
					RenderDatas[mesh.Index].Matrix = mesh.Matrix2 * RenderDatas[mesh.Index].MatrixSub;
				}
			}

			foreach (var child in mesh.Children) {
				_updateMeshMatrix(child, ref RenderDatas[mesh.Index].MatrixSub, calcMatrix);
			}
		}

		private void _renderMesh(OpenGLViewport viewport, List<Mesh> meshes, RenderInfo ri, bool transparent) {
			if (ri.Vbo != null) {
				bool repeat = false;
				ri.BindVao();

				Mesh mesh = null;
				int meshIndex = -1;
				int startVboOffset;
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
							Shader.SetMatrix4("m", ref RenderDatas[mesh.Index].Matrix);
					}

					if (meshIndex >= meshes.Count)
						break;

					if (!Textures[vboIndex.Texture].IsLoaded && Textures[vboIndex.Texture].Image == null) {
						vboCount -= vboIndex.Count;
						continue;
					}

					var texture = Textures[vboIndex.Texture];

					if (viewport.RenderPass == RenderMode.AnimatedTransparentTextures && (!texture.IsSemiTransparent || mesh.TextureKeyFrameGroup.Count <= 0)) {
						vboCount -= vboIndex.Count;
						continue;
					}
					if ((viewport.RenderPass == RenderMode.TransparentTextures || viewport.RenderPass == RenderMode.OpaqueTransparentTextures) && (!texture.IsSemiTransparent || mesh.TextureKeyFrameGroup.Count > 0)) {
						vboCount -= vboIndex.Count;
						continue;
					}
					if (viewport.RenderPass == RenderMode.OpaqueTextures && texture.IsSemiTransparent) {
						vboCount -= vboIndex.Count;
						continue;
					}

					if (mesh.Model.Version >= 2.3 && mesh.TextureKeyFrameGroup.Count > 0) {
						Vector2 texTranslate = new Vector2(0);
						Vector2 texMult = new Vector2(1);
						Matrix4 texRot = Matrix4.Identity;
						float rotOffset = 0;
						texRot = GLHelper.Translate(ref texRot, new Vector3(0.5f, 0.5f, 0));

						foreach (var type in mesh.TextureKeyFrameGroup.Types) {
							if (mesh.TextureKeyFrameGroup.HasTextureAnimation(vboIndex.MeshTextureIndice, type)) {
								float offset = mesh.GetTexture(vboIndex.MeshTextureIndice, type);
								repeat = true;

								switch (type) {
									case TextureTransformTypes.TranslateX:
										texTranslate.X += offset;
										break;
									case TextureTransformTypes.TranslateY:
										texTranslate.Y += offset;
										break;
									case TextureTransformTypes.ScaleX:
										texMult.X = offset;
										break;
									case TextureTransformTypes.ScaleY:
										texMult.Y = offset;
										break;
									case TextureTransformTypes.RotateZ:
										rotOffset = offset;
										break;
								}
							}
						}

						texRot = GLHelper.Scale(ref texRot, new Vector3(texMult.X, texMult.Y, 1));
						texRot = GLHelper.Rotate(ref texRot, rotOffset, new Vector3(0, 0, 1));
						texRot[3, 0] += texTranslate.X;
						texRot[3, 1] += texTranslate.Y;
						texRot = GLHelper.Translate(ref texRot, new Vector3(-0.5f, -0.5f, 0));

						Shader.SetFloat("textureAnimToggle", 1);
						Shader.SetMatrix4("texRot", ref texRot);
					}

					texture.Bind();

					if (repeat) {
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
					}

					// modelMatrixCaches are always used for the MapRenderer (map preview)
					if (InstanceMatrices != null) {
						GL.DrawArraysInstanced(PrimitiveType.Triangles, vboIndex.Begin, vboIndex.Count, InstanceMatrices.Length);
#if DEBUG
						viewport.Stats.DrawArrays_Calls++;
						viewport.Stats.DrawArrays_Calls_VertexLength += vboIndex.Count;
#endif
					}
					// This part is only used by the RsmRenderer (rsm preview)
					else {
						GL.DrawArrays(PrimitiveType.Triangles, vboIndex.Begin, vboIndex.Count);
#if DEBUG
						viewport.Stats.DrawArrays_Calls++;
						viewport.Stats.DrawArrays_Calls_VertexLength += vboIndex.Count;
#endif
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
			shader.SetMatrix4("vp", ref viewport.ViewProjection);
			shader.SetFloat("enableCullFace", viewport.RenderOptions.EnableFaceCulling ? 1.0f : 0.0f);
		}

		public static void SetupRswLight(Shader shader, Rsw rsw) {
			shader.Use();

			Matrix3 mat = Matrix3.Identity;
			mat = GLHelper.Rotate(ref mat, -GLHelper.ToRad(rsw.Light.Latitude), new Vector3(1, 0, 0));
			mat = GLHelper.Rotate(ref mat, GLHelper.ToRad(rsw.Light.Longitude), new Vector3(0, 1, 0));

			Vector3 lightDirection = mat * new Vector3(0, 1, 0);
			shader.SetVector3("lightAmbient", new Vector3(rsw.Light.AmbientRed, rsw.Light.AmbientGreen, rsw.Light.AmbientBlue));
			shader.SetVector3("lightDiffuse", new Vector3(rsw.Light.DiffuseRed, rsw.Light.DiffuseGreen, rsw.Light.DiffuseBlue));
			shader.SetFloat("lightIntensity", rsw.Light.Intensity);
			shader.SetVector3("lightDirection", ref lightDirection);
		}

		public override void Unload() {
			foreach (var texture in Textures) {
				TextureManager.UnloadTexture(texture.Resource, _request.Context);
			}

			if (Instance != null)
				Instance.Unload();
		}

		public override int GetHashCode() {
			return _rsm != null ? _rsm.GetHashCode() : base.GetHashCode();
		}
	}
}
