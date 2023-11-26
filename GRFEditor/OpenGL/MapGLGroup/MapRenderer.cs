using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Threading;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapGLGroup {
	public class MapRendererOptions {
		public bool Water { get; set; }
		public bool Ground { get; set; }
		public bool Objects { get; set; }
		public bool AnimateMap { get; set; }
		public bool Lightmap { get; set; }
		public bool Shadowmap { get; set; }
		public bool ShowFps { get; set; }
		public bool ShowBlackTiles { get; set; }
		public bool LubEffect { get; set; }
		public bool ViewStickToGround { get; set; }
		public bool UseClientPov { get; set; }
		public bool RenderSkymapFeature { get; set; }
		public bool RenderSkymapDetected { get; set; }
		public bool RenderingMap { get; set; }

		public Vector4 SkymapBackgroundColor = new Vector4(102, 152, 204, 255) / 255f;	// rbga

		public MapRendererOptions() {
			Water = true;
			Ground = true;
			Objects = true;
			AnimateMap = true;
			Lightmap = true;
			Shadowmap = true;
			ShowFps = false;
			ShowBlackTiles = false;
			LubEffect = true;
			ViewStickToGround = true;
			UseClientPov = false;
			RenderSkymapFeature = true;
			RenderSkymapDetected = true;
		}
	}

	public class MapRenderer : MapGLObject {
		private readonly RendererLoadRequest _request;
		private readonly Rsw _rsw;
		public Dictionary<int, Dictionary<int, RenderInfo>> ShadeGroups = new Dictionary<int, Dictionary<int, RenderInfo>>();
		private bool _verticesLoaded;
		private bool _vaoCreated;
		private bool _directionalLightSetup;
		public bool ReloadLight { get; set; }
		private static MapRendererOptions _renderOptions = new MapRendererOptions();
		private readonly Stopwatch _watch = new Stopwatch();
		private readonly Dictionary<SharedRsmRenderer, List<ModelRenderer>> _modelGroups = new Dictionary<SharedRsmRenderer, List<ModelRenderer>>();
		private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
		public static TextBlock FpsTextBlock;
		public static CloudEffectSettings SkyMap = new CloudEffectSettings();
		public static Vector3 LookAt = new Vector3(0);

		public static MapRendererOptions RenderOptions {
			get { return _renderOptions; }
			set { _renderOptions = value; }
		}

		public void ConvertVertices2Float() {
			foreach (var shadeGroup in ShadeGroups) {
				foreach (var ri in shadeGroup.Value.Values) {
					if (_request.CancelRequired())
						return;

					ri.RawVertices = Vbo.Vertex2Data(ri.Vertices);
				}
			}
		}

		private void _loadVaos() {
			// This is split at maxVertices per draw because loading mipmap textures is very expensive and will lag too much
			int maxVertices = 20000;

			foreach (var shadeGroup in ShadeGroups) {
				foreach (var ri in shadeGroup.Value.Values) {
					if (ri.Vao != 0)
						continue;

					if (_request.CancelRequired())
						return;

					ri.CreateVao();
					ri.Vbo = new Vbo();
					ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.StaticDraw, 8);
					ri.RawVertices = new float[0];
					ri.Vertices.Clear();

					GL.EnableVertexAttribArray(0);
					GL.EnableVertexAttribArray(1);
					GL.EnableVertexAttribArray(2);
					GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
					GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
					GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));
					maxVertices -= ri.Vbo.Length;

					if (maxVertices < 0)
						return;
				}
			}

			_vaoCreated = true;
		}

		public MapRenderer(RendererLoadRequest request, Shader shader, Rsw rsw) {
			_request = request;
			_rsw = rsw;
			Shader = shader;
		}

		public override void Render(OpenGLViewport viewport) {
			if (!_verticesLoaded || IsUnloaded || !RenderOptions.Objects) {
				return;
			}

			if (!_directionalLightSetup || ReloadLight) {
				SharedRsmRenderer.SetupRswLight(Shader, _rsw);
				_directionalLightSetup = true;
				ReloadLight = false;
			}

			if (!_vaoCreated) {
				_loadVaos();
			}

			Shader.Use();
			Shader.SetMatrix4("modelMatrix2", Matrix4.Identity);
			Shader.SetMatrix4("modelMatrix", Matrix4.Identity);
			Shader.SetVector2("texTranslate", new Vector2(0));
			Shader.SetVector2("texMult", new Vector2(1));
			Shader.SetMatrix4("texRot", Matrix4.Identity);
			Shader.SetFloat("textureAnimToggle", 0);

			foreach (var shadeGroup in ShadeGroups) {
				Shader.SetInt("shadeType", shadeGroup.Key);

				foreach (var riEntry in shadeGroup.Value.OrderBy(p => Textures[p.Key].IsSemiTransparent)) {
					if (riEntry.Value.Vao == 0)
						continue;

					riEntry.Value.BindVao();

					var texture = Textures[riEntry.Key];

					texture.Bind();

					if (texture.IsSemiTransparent) {
						Shader.SetFloat("discardValue", 0.02f);
					}

					GL.DrawArrays(PrimitiveType.Triangles, 0, riEntry.Value.Vbo.Length);
				}

				Shader.SetFloat("discardValue", 0.8f);
			}

			var sTick = _watch.ElapsedMilliseconds;

			foreach (var entry in _modelGroups) {
				var rsmRenderer = entry.Key;
				var tick = sTick;
				float previousAnimationSpeed = -1f;
				List<Matrix4> instanceMatrixes = new List<Matrix4>();

				foreach (var model in entry.Value.OrderBy(p => p.Model.AnimationSpeed)) {
					if (Math.Abs(previousAnimationSpeed - model.Model.AnimationSpeed) > 0.01) {
						if (instanceMatrixes.Count > 0) {
							rsmRenderer.Rsm.SetAnimationIndex(tick, previousAnimationSpeed);
							rsmRenderer.Rsm.Dirty();
							entry.Key.RenderDynamicModels(viewport, instanceMatrixes);
						}

						previousAnimationSpeed = model.Model.AnimationSpeed;
						instanceMatrixes.Clear();
					}

					if (!model.IsLoaded)
						model.Load(viewport);

					instanceMatrixes.Add(model.MatrixCache);
				}

				if (instanceMatrixes.Count > 0) {
					rsmRenderer.Rsm.SetAnimationIndex(tick, previousAnimationSpeed);
					rsmRenderer.Rsm.Dirty();
					entry.Key.RenderDynamicModels(viewport, instanceMatrixes);
				}
			}
		}

		public override void Unload() {
			IsUnloaded = true;

			try {
				foreach (var texture in Textures) {
					TextureManager.UnloadTexture(texture.Resource);
				}

				foreach (var shadeGroup in ShadeGroups) {
					foreach (var ri in shadeGroup.Value.Values) {
						ri.Unload();
					}
				}
			}
			catch {
			}
		}

		public override void Load(OpenGLViewport viewport) {
		}

		public void _addStaticModel(ModelRsm model, Gnd gnd) {
			if (model.Rsm == null)
				return;

			var MatrixCache = Matrix4.Identity;
			MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(1, 1, -1));

			MatrixCache = GLHelper.Translate(MatrixCache, new Vector3(5 * gnd.Width + model.Model.Position.X, -model.Model.Position.Y, -10 - 5 * gnd.Height + model.Model.Position.Z));
			MatrixCache = GLHelper.Rotate(MatrixCache, -GLHelper.ToRad(model.Model.Rotation.Z), new Vector3(0, 0, 1));
			MatrixCache = GLHelper.Rotate(MatrixCache, -GLHelper.ToRad(model.Model.Rotation.X), new Vector3(1, 0, 0));
			MatrixCache = GLHelper.Rotate(MatrixCache, GLHelper.ToRad(model.Model.Rotation.Y), new Vector3(0, 1, 0));
			MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(model.Model.Scale.X, -model.Model.Scale.Y, model.Model.Scale.Z));

			if (model.Rsm.Version < 2.2) {
				MatrixCache = GLHelper.Translate(MatrixCache, new Vector3(-model.Rsm.RealBox.Center.X, model.Rsm.RealBox.Min.Y, -model.Rsm.RealBox.Center.Z));
			}
			else {
				MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(1, -1, 1));
			}

			if (MatrixCache[3, 0] * 0.2f < 0 ||
				MatrixCache[3, 0] * 0.2f > gnd.Header.Width * 2 ||
				MatrixCache[3, 2] * 0.2f < 0 ||
				MatrixCache[3, 2] * 0.2f > gnd.Header.Height * 2) {
				return;
			}

			var textures = new List<Texture>();

			foreach (var texture in model.Rsm.Textures) {
				if (_textures.ContainsKey(texture)) {
					textures.Add(_textures[texture]);
				}
				else {
					textures.Add(TextureManager.LoadTextureAsync(texture, Rsm.RsmTexturePath + texture, TextureRenderMode.RsmTexture, _request));
					_textures[texture] = textures.Last();
				}
			}

			model.Load();
			_addStaticModelVertices(textures, model, model.Rsm.MainMesh, MatrixCache);
		}

		private void _addStaticModelVertices(List<Texture> textures, ModelRsm model, Mesh mesh, Matrix4 instance) {
			var transformMat = mesh.RenderMatrix * instance;

			foreach (var vert in model.Verts[mesh.Model.ShadeType][mesh.Index]) {
				var texture = textures[mesh.TextureIndexes[vert.Key]];

				int textureId = Textures.IndexOf(texture);

				if (textureId == -1) {
					Textures.Add(texture);
					textureId = Textures.Count - 1;
				}
				else {
					// Texture already loaded, remove it
					TextureManager.UnloadTexture(texture.Resource);
				}

				if (!ShadeGroups.ContainsKey(mesh.Model.ShadeType)) {
					ShadeGroups[mesh.Model.ShadeType] = new Dictionary<int, RenderInfo>();
				}

				if (!ShadeGroups[mesh.Model.ShadeType].ContainsKey(textureId)) {
					ShadeGroups[mesh.Model.ShadeType].Add(textureId, new RenderInfo());
					ShadeGroups[mesh.Model.ShadeType][textureId].Vertices = new List<Vertex>();
				}

				var ri = ShadeGroups[mesh.Model.ShadeType][textureId];
				var res = new List<Vertex>();

				for (int i = 0; i < vert.Value.Count; i++) {
					var v = vert.Value[i];

					var pos = new Vector3(v.data[0], v.data[1], v.data[2]);
					var n = new Vector3(v.data[5], v.data[6], v.data[7]);

					pos = GLHelper.MultiplyWithTranslate(transformMat, pos);
					n = GLHelper.MultiplyWithoutTranslate(transformMat, n);
					n = Vector3.Normalize(n);

					res.Add(new Vertex(pos, new Vector2(v.data[3], v.data[4]), n));
				}

				ri.Vertices.AddRange(res);
			}

			foreach (var child in mesh.Children) {
				_addStaticModelVertices(textures, model, child, instance);
			}
		}

		public void LoadModels(Rsw rsw, Gnd gnd, bool loadAnimation) {
			_watch.Start();

			GrfThread.Start(delegate {
				var models = new Dictionary<string, Rsm>();
				var sharedRsmRenderers = new Dictionary<string, SharedRsmRenderer>();

				foreach (var model in rsw.Objects.OfType<Model>()) {
					var name = model.ModelName;

					if (models.ContainsKey(name))
						continue;

					var rsmEntry = ResourceManager.GetData(Rsm.RsmModelPath + model.ModelName);

					if (rsmEntry != null)
						models[name] = new Rsm(rsmEntry);
					else
						models[name] = null;
				}

				foreach (var model in rsw.Objects.OfType<Model>()) {
					ModelRsm modelRsm = new ModelRsm();
					modelRsm.Model = model;
					modelRsm.Rsm = models[model.ModelName];

					if (modelRsm.Rsm == null)
						continue;

					if (loadAnimation) {
						if (modelRsm.Rsm.AnimationLength > 0) {
							bool any = modelRsm.Rsm.Meshes.Any(p => p.ScaleKeyFrames.Count > 1 || p.TextureKeyFrameGroup.Count > 0 || p.RotationKeyFrames.Count > 1 || p.PosKeyFrames.Count > 1);

							if (any) {
								if (!sharedRsmRenderers.ContainsKey(model.ModelName)) {
									sharedRsmRenderers[model.ModelName] = new SharedRsmRenderer(_request, Shader, modelRsm.Rsm, gnd, rsw);
									_modelGroups[sharedRsmRenderers[model.ModelName]] = new List<ModelRenderer>();
								}

								var modelRenderer = new ModelRenderer(Shader, modelRsm.Model, sharedRsmRenderers[model.ModelName]);
								modelRenderer.CalculateCachedMatrix();

								if (modelRenderer.MatrixCache[3, 0] * 0.2f < 0 ||
									modelRenderer.MatrixCache[3, 0] * 0.2f > gnd.Header.Width * 2 ||
									modelRenderer.MatrixCache[3, 2] * 0.2f < 0 ||
									modelRenderer.MatrixCache[3, 2] * 0.2f > gnd.Header.Height * 2) {
									continue;
								}

								_modelGroups[sharedRsmRenderers[model.ModelName]].Add(modelRenderer);
							}
							else {
								_addStaticModel(modelRsm, gnd);
							}
						}
					}
					else {
						_addStaticModel(modelRsm, gnd);
					}

					if (_request.CancelRequired())
						return;
				}

				ConvertVertices2Float();
				_verticesLoaded = true;
			});
		}
	}

	public class ModelRsm {
		public Rsm Rsm { get; set; }
		public Model Model { get; set; }
		public bool RsmLoaded;
		public Dictionary<int, Dictionary<int, Dictionary<int, List<Vertex>>>> Verts = new Dictionary<int, Dictionary<int, Dictionary<int, List<Vertex>>>>();

		public void Load() {
			if (RsmLoaded)
				return;

			Rsm.MainMesh.CalcMatrix1(0);
			_initMeshInfo(Rsm.MainMesh);
			RsmLoaded = true;
		}

		private void _initMeshInfo(Mesh mesh) {
			Matrix4 matrix = Matrix4.Identity;
			_initMeshInfo(mesh, ref matrix);
		}

		private void _initMeshInfo(Mesh mesh, ref Matrix4 matrix) {
			Dictionary<int, Dictionary<int, List<Vertex>>> allVerts;

			if (!Verts.TryGetValue(mesh.Model.ShadeType, out allVerts)) {
				allVerts = new Dictionary<int, Dictionary<int, List<Vertex>>>();
				Verts[mesh.Model.ShadeType] = allVerts;
			}

			allVerts[mesh.Index] = new Dictionary<int, List<Vertex>>();
			var verts = allVerts[mesh.Index];

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

			if (mesh.Model.Version >= 2.2) {
				mesh.RenderMatrix = mesh.Matrix2;
				mesh.RenderMatrixSub = Matrix4.Identity;
			}
			else {
				mesh.RenderMatrix = mesh.Matrix2 * mesh.Matrix1 * matrix;
				mesh.RenderMatrixSub = mesh.Matrix1 * matrix;
			}

			foreach (var child in mesh.Children) {
				_initMeshInfo(child, ref mesh.RenderMatrixSub);
			}
		}
	}
}
