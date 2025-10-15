using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Threading;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Matrix4 = OpenTK.Matrix4;
using Vertex = GRFEditor.OpenGL.MapComponents.Vertex;

namespace GRFEditor.OpenGL.MapRenderers {
	public class RenderDataInfo {
		public int TextureId;
		public List<ModelRenderer> ModelRenderers = new List<ModelRenderer>();
		public List<VboIndex> Indices = new List<VboIndex>();
		public List<Mesh> MeshReferences = new List<Mesh>();
		public RenderInfo RenderInfo = new RenderInfo();
		public List<VertexP3N2N3[]> TempVertexBuffer = new List<VertexP3N2N3[]>();
		public int VboOffset = 0;

		public void AddVertexArray(VertexP3N2N3[] array) {
			TempVertexBuffer.Add(array);
			VboOffset += array.Length;
		}
	};

	public class MapRenderer : Renderer {
		private readonly RendererLoadRequest _request;
		private readonly Rsw _rsw;
		public Dictionary<(int shadeType, int textureId), RenderInfo> ShadeGroups = new Dictionary<(int shadeType, int textureId), RenderInfo>();
		public Dictionary<(int shadeType, int textureId), int> ShadeGroupsSizes = new Dictionary<(int shadeType, int textureId), int>();
		private bool _verticesLoaded;
		private bool _vaoCreated;
		private bool _directionalLightSetup;
		public bool ReloadLight { get; set; }
		public static int VertexStructSize;

		public bool _newMethod = false;
		public bool _firstCall = false;
		public readonly Dictionary<int, RenderDataInfo> TextureGroups = new Dictionary<int, RenderDataInfo>();

		private readonly Stopwatch _watch = new Stopwatch();
		private readonly Dictionary<SharedRsmRenderer, List<ModelRenderer>> _modelGroups = new Dictionary<SharedRsmRenderer, List<ModelRenderer>>();
		internal readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);
		public static TextBlock FpsTextBlock;
		public static Vector3 LookAt = new Vector3(0);

		private List<ModelRsm> _modelsToLoad = new List<ModelRsm>();
		private Shader _computerShader;
		private int _ssbo;

		static MapRenderer() {
			VertexStructSize = Marshal.SizeOf<VertexP3N2N3>() / sizeof(float);
		}

		public MapRenderer(RendererLoadRequest request, Shader shader, Rsw rsw) {
			_request = request;
			_rsw = rsw;
			Shader = shader;
		}

		public override void Render(OpenGLViewport viewport) {
			if (!_verticesLoaded || IsUnloaded || !viewport.RenderOptions.Objects) {
				return;
			}

			if (viewport.RenderPass != RenderMode.OpaqueTextures && 
				viewport.RenderPass != RenderMode.TransparentTextures && 
				viewport.RenderPass != RenderMode.OpaqueTransparentTextures &&
				viewport.RenderPass != RenderMode.AnimatedTransparentTextures)
				return;

			if (!_directionalLightSetup || ReloadLight) {
				SharedRsmRenderer.SetupRswLight(Shader, _rsw);
				_directionalLightSetup = true;
				ReloadLight = false;
			}

			if (!_vaoCreated) {
				_loadVaos(viewport);
			}

			Shader.Use();
			Shader.SetMatrix4("instanceMatrix", Matrix4.Identity);
			Shader.SetMatrix4("m", Matrix4.Identity);
			Shader.SetVector2("texTranslate", new Vector2(0));
			Shader.SetVector2("texMult", new Vector2(1));
			Shader.SetMatrix4("texRot", Matrix4.Identity);
			Shader.SetFloat("textureAnimToggle", 0);
			Shader.SetVector3("lightPosition", ref viewport.Camera.Position);

			if (viewport.RenderOptions.ShowWireframeView) {
				if (_subPass == 0) {
					Shader.SetVector3("lightPosition", viewport.Camera.Position);
					Shader.SetVector4("wireframeColor", new Vector4(0.8f, 0.8f, 0.8f, 1));
					GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
				}
				else if (_subPass == 1) {
					Shader.SetVector4("wireframeColor", new Vector4(0, 0, 0, 1));
					GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				}
			}
			else if (viewport.RenderOptions.ShowPointView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);

			if (viewport.RenderOptions.ShowWireframeView || viewport.RenderOptions.ShowPointView) {
				Shader.SetBool("wireframe", true);
			}
			else {
				Shader.SetBool("wireframe", false);
			}

			switch (viewport.RenderPass) {
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

			if (viewport.RenderPass == RenderMode.OpaqueTextures || 
				viewport.RenderPass == RenderMode.TransparentTextures || 
				viewport.RenderPass == RenderMode.OpaqueTransparentTextures) {
				foreach (var shadeGroup in ShadeGroups.OrderBy(p => Textures[p.Key.textureId].IsSemiTransparent)) {
					Shader.SetInt("shadeType", shadeGroup.Key.shadeType);
			
					var ri = shadeGroup.Value;
			
					if (ri.Vao == 0)
						continue;
			
					ri.BindVao();
			
					var texture = Textures[shadeGroup.Key.textureId];
			
					if (viewport.RenderPass == RenderMode.OpaqueTextures && texture.IsSemiTransparent)
						continue;
					if ((viewport.RenderPass == RenderMode.TransparentTextures || viewport.RenderPass == RenderMode.OpaqueTransparentTextures) && !texture.IsSemiTransparent)
						continue;
			
					texture.Bind();
			
					if (viewport.RenderPass == RenderMode.OpaqueTextures) {
						GL.DrawArrays(PrimitiveType.Triangles, 0, ri.Vbo.Length);
					}
					else if (viewport.RenderPass == RenderMode.TransparentTextures ||
						viewport.RenderPass == RenderMode.OpaqueTransparentTextures) {
						GL.DrawArrays(PrimitiveType.Triangles, 0, ri.Vbo.Length);
					}
#if DEBUG	
					viewport.Stats.DrawArrays_Calls++;
					viewport.Stats.DrawArrays_Calls_VertexLength += ri.Vbo.Length;
#endif		
				}
			}

			var sTick = _watch.ElapsedMilliseconds;

			if (_newMethod) {
				if (!_firstCall) {
					foreach (var entry in _modelGroups) {
						entry.Key.LoadSpecial(viewport, this);
					}

					_firstCall = true;
				}

				// 
				foreach (var textureGroup in TextureGroups) {
					Textures[textureGroup.Key].Bind();
					Matrix4[] matrices = new Matrix4[textureGroup.Value.Indices.Count];

					bool ccw = true;

					for (int i = 0; i < textureGroup.Value.Indices.Count; i++) {
						matrices[i] = textureGroup.Value.MeshReferences[i].Render.Matrix;
					}



				}
			}
			else {
				Shader.SetBool("useInstances", true);

				if (!_firstCall) {
					foreach (var entry in _modelGroups) {
						var sharedModel = entry.Key;
						sharedModel.Instance = new Vbo();
						sharedModel.InstanceMatrices = new Matrix4[entry.Value.Count];
						Matrix4[] instanceMatrixes = sharedModel.InstanceMatrices;

						// Load models
						for (int i = 0; i < entry.Value.Count; i++) {
							var model = entry.Value[i];

							if (!model.IsLoaded)
								model.Load(viewport);

							instanceMatrixes[i] = model.MatrixCache;
							instanceMatrixes[i].Transpose();
						}

						sharedModel.Instance.Bind();
						GL.BufferData(BufferTarget.ArrayBuffer, instanceMatrixes.Length * 64, instanceMatrixes, BufferUsageHint.StaticDraw);

						RenderInfo[] renderInfos = new[] { sharedModel.RenderInfo, sharedModel.RenderInfoTransparent };

						foreach (var ri in renderInfos) {
							if (ri.Vao != 0) {
								ri.BindVao();
								sharedModel.Instance.Bind();

								int location = 4;

								for (int i = 0; i < 4; i++) {
									GL.EnableVertexAttribArray(location + i);
									GL.VertexAttribPointer(location + i, 4, VertexAttribPointerType.Float, false, 64, i * 16);
									GL.VertexAttribDivisor(location + i, 1);
								}
							}
						}
					}

					_firstCall = true;
				}

				foreach (var entry in _modelGroups) {
					var sharedModel = entry.Key;
					var tick = sTick;

					//if (viewport.RenderPass == 0) {
						sharedModel.Rsm.SetAnimationIndex(tick, entry.Value.First().Model.AnimationSpeed);
						sharedModel.Rsm.Dirty();
					//}

					sharedModel.RenderDynamicModels(viewport);
				}

				Shader.SetBool("useInstances", false);
			}

			if (viewport.RenderOptions.ShowWireframeView && _subPass == 0) {
				_subPass = 1;
				Render(viewport);
				_subPass = 0;
				return;
			}

			if (viewport.RenderOptions.ShowWireframeView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

			if (viewport.RenderOptions.ShowPointView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
		}

		public override void Unload() {
			IsUnloaded = true;

			try {
				foreach (var texture in Textures) {
					TextureManager.UnloadTexture(texture.Resource, _request.Context);
				}

				//foreach (var texture in _textures.Values) {
				//	TextureManager.UnloadTexture(texture.Resource, _request.Context);
				//}

				foreach (var ri in ShadeGroups.Values) {
					ri.Unload();
				}

				foreach (var sharedRsmRenderer in _modelGroups.Keys) {
					foreach (var texture in sharedRsmRenderer.Textures)
						TextureManager.UnloadTexture(texture.Resource, _request.Context);
				}

				Textures.Clear();
			}
			catch {
			}
		}

		public override void Load(OpenGLViewport viewport) {
		}

		private void _loadVaos(OpenGLViewport viewport) {
			if (viewport.OpenGLVersion >= 4.3) {
				_loadFromGPU(viewport);
				return;
			}

			// This is split at maxVertices per draw because loading mipmap textures is very expensive and will lag too much
			int maxVertices = 20000;

			foreach (var ri in ShadeGroups.Values) {
				if (ri.Vao != 0)
					continue;

				if (_request.CancelRequired())
					return;

				ri.CreateVao();
				ri.Vbo = new Vbo();
				ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.StaticDraw, VertexStructSize);
				ri.RawVertices = null;

				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.EnableVertexAttribArray(2);
				GL.EnableVertexAttribArray(3);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 0);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 3 * sizeof(float));
				GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 5 * sizeof(float));
				GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 8 * sizeof(float));
				maxVertices -= ri.Vbo.Length;

				if (maxVertices < 0)
					return;
			}

			_vaoCreated = true;
		}

		private void _loadFromGPU(OpenGLViewport viewport) {
			Stopwatch watch = new Stopwatch();
			watch.Start();

			if (_computerShader == null) {
				_computerShader = new Shader("rsm_load.comp");
			}

			_computerShader.Use();

			while (_modelsToLoad.Count > 0) {
				var model = _modelsToLoad[0];
				_modelsToLoad.RemoveAt(0);

				var textures = new List<Texture>();

				foreach (var texture in model.Rsm.Textures) {
					if (_textures.ContainsKey(texture)) {
						textures.Add(_textures[texture]);
					}
					else {
						var tex = TextureManager.LoadTextureAsync(texture, Rsm.RsmTexturePath + texture, TextureRenderMode.RsmTexture, _request);
						textures.Add(tex);
						Textures.Add(tex);
						_textures[texture] = tex;
					}
				}

				_addStaticModelVertices(viewport, textures, model, model.Rsm.MainMesh, ref model.MatrixCache);

				if (watch.ElapsedMilliseconds > 8)
					return;
			}

			_vaoCreated = true;
		}

		private void _addStaticModelVerticesCreateGroupsOnly(List<Texture> textures, ModelRsm model, Mesh mesh) {
			foreach (var vertKeyValue in model.Verts.Where(p => p.Key.meshIndex == mesh.Index)) {
				var texture = textures[mesh.TextureIndexes[vertKeyValue.Key.textureId]];

				int textureId = Textures.IndexOf(texture);

				if (textureId == -1) {
					Textures.Add(texture);
					textureId = Textures.Count - 1;
				}

				var shadeKey = (mesh.Model.ShadeType, textureId);

				if (!ShadeGroups.ContainsKey(shadeKey)) {
					ShadeGroups[shadeKey] = new RenderInfo();
					ShadeGroupsSizes[shadeKey] = 0;
				}

				ShadeGroupsSizes[shadeKey] += vertKeyValue.Value.Length;
			}

			foreach (var child in mesh.Children) {
				_addStaticModelVerticesCreateGroupsOnly(textures, model, child);
			}
		}

		public void _addStaticModel(ModelRsm model, Gnd gnd) {
			if (model.Rsm == null)
				return;

			Vector3 position = new Vector3(5 * gnd.Width + model.Model.Position.X, 0, 5 * gnd.Height + model.Model.Position.Z);
			if (position.X < 0 || position.X > gnd.Header.Width * 10 ||
				position.Z < 0 || position.Z > gnd.Header.Height * 10) {
				return;
			}

			model.MatrixCache = Matrix4.Identity;
			model.MatrixCache = GLHelper.Scale(ref model.MatrixCache, new Vector3(1, 1, -1));

			model.MatrixCache = GLHelper.Translate(ref model.MatrixCache, new Vector3(5 * gnd.Width + model.Model.Position.X, -model.Model.Position.Y, -10 - 5 * gnd.Height + model.Model.Position.Z));
			model.MatrixCache = GLHelper.Rotate(ref model.MatrixCache, -GLHelper.ToRad(model.Model.Rotation.Z), new Vector3(0, 0, 1));
			model.MatrixCache = GLHelper.Rotate(ref model.MatrixCache, -GLHelper.ToRad(model.Model.Rotation.X), new Vector3(1, 0, 0));
			model.MatrixCache = GLHelper.Rotate(ref model.MatrixCache, GLHelper.ToRad(model.Model.Rotation.Y), new Vector3(0, 1, 0));
			model.MatrixCache = GLHelper.Scale(ref model.MatrixCache, new Vector3(model.Model.Scale.X, -model.Model.Scale.Y, model.Model.Scale.Z));

			if (model.Rsm.Version < 2.2) {
				model.MatrixCache = GLHelper.Translate(ref model.MatrixCache, new Vector3(-model.Rsm.VerticesBox.Center.X, model.Rsm.VerticesBox.Min.Y, -model.Rsm.VerticesBox.Center.Z));
			}
			else {
				model.MatrixCache = GLHelper.Scale(ref model.MatrixCache, new Vector3(1, -1, 1));
			}

			model.Load();
			
			// Used for calling _addStaticModelVertices after groups are made
			_modelsToLoad.Add(model);

			var textures = new List<Texture>();

			foreach (var texture in model.Rsm.Textures) {
				if (_textures.ContainsKey(texture)) {
					textures.Add(_textures[texture]);
				}
				else {
					var tex = TextureManager.LoadTextureAsync(texture, Rsm.RsmTexturePath + texture, TextureRenderMode.RsmTexture, _request);
					textures.Add(tex);
					Textures.Add(tex);
					_textures[texture] = tex;
				}
			}

			_addStaticModelVerticesCreateGroupsOnly(textures, model, model.Rsm.MainMesh);
		}

		public void _addStaticAllModelVertices(OpenGLViewport viewport) {
			foreach (var model in _modelsToLoad) {
				var textures = new List<Texture>();

				foreach (var texture in model.Rsm.Textures) {
					textures.Add(_textures[texture]);
				}

				_addStaticModelVertices(viewport, textures, model, model.Rsm.MainMesh, ref model.MatrixCache);
			}
		}

		private Dictionary<int, int> _vboOffsets = new Dictionary<int, int>();

		private void _addStaticModelVertices(OpenGLViewport viewport, List<Texture> textures, ModelRsm model, Mesh mesh, ref Matrix4 instance) {
			var m = mesh.TempMatrix * instance;

			foreach (var vertKeyValue in model.Verts.Where(p => p.Key.meshIndex == mesh.Index)) {
				var texture = textures[mesh.TextureIndexes[vertKeyValue.Key.textureId]];
				int textureId = Textures.IndexOf(texture);

				var shadeKey = (mesh.Model.ShadeType, textureId);

				if (ShadeGroups[shadeKey].RawVertices == null) {
					ShadeGroups[shadeKey].RawVertices = new float[ShadeGroupsSizes[shadeKey] * VertexStructSize];
					ShadeGroupsSizes[shadeKey] = 0;
				}

				var ri = ShadeGroups[shadeKey];

				if (viewport.OpenGLVersion >= 4.3) {
					int vertexCount = vertKeyValue.Value.Length;
					int bufferSize = VertexStructSize * sizeof(float) * vertexCount;

					if (_ssbo <= 0) {
						_ssbo = GL.GenBuffer();
					}

					if (!ri.VaoCreated()) {
						ri.CreateVao();
						ri.Vbo = new Vbo();
						ri.Vbo.Bind();
						GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * ShadeGroups[shadeKey].RawVertices.Length, IntPtr.Zero, BufferUsageHint.StaticDraw);

						GL.EnableVertexAttribArray(0);
						GL.EnableVertexAttribArray(1);
						GL.EnableVertexAttribArray(2);
						GL.EnableVertexAttribArray(3);
						GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 0);
						GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 3 * sizeof(float));
						GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 5 * sizeof(float));
						GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, VertexStructSize * sizeof(float), 8 * sizeof(float));
					}

					GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ri.Vbo.Id);

					if (!_vboOffsets.ContainsKey(ri.Vbo.Id)) {
						GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * ShadeGroups[shadeKey].RawVertices.Length, IntPtr.Zero, BufferUsageHint.DynamicCopy);
						_vboOffsets[ri.Vbo.Id] = 0;
					}

					var scale = model.Model.Scale.X * model.Model.Scale.Y * model.Model.Scale.Z * (model.Rsm.Version >= 2.2 ? -1 : 1) < 0 ? -1 : 0;
					GL.BufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)(_vboOffsets[ri.Vbo.Id] * VertexStructSize * sizeof(float)), bufferSize, vertKeyValue.Value);
					_computerShader.SetInt("baseVertex", _vboOffsets[ri.Vbo.Id]);
					_computerShader.SetInt("vertexCount", vertexCount);
					_computerShader.SetFloat("scale", scale);
					_computerShader.SetMatrix4("model", ref m);
					_vboOffsets[ri.Vbo.Id] += vertexCount;

					GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ri.Vbo.Id);

					int workGroupSize = 256;
					int numGroups = (vertexCount + workGroupSize - 1) / workGroupSize;
					GL.DispatchCompute(numGroups, 1, 1);

					ri.Vbo.Length = _vboOffsets[ri.Vbo.Id];
				}
				else {
					int baseOffset = ShadeGroupsSizes[shadeKey];
					var buffer = ShadeGroups[shadeKey].RawVertices;
					var scale = model.Model.Scale.X * model.Model.Scale.Y * model.Model.Scale.Z * (model.Rsm.Version >= 2.2 ? -1 : 1) < 0 ? -1 : 0;

					for (int i = 0; i < vertKeyValue.Value.Length; i++) {
						var v = vertKeyValue.Value[i];

						var pos = new Vector3(v.PosX, v.PosY, v.PosZ);
						var n = new Vector3(v.NormalX, v.NormalY, v.NormalZ);

						pos = GLHelper.MultiplyWithTranslate(ref m, ref pos);
						n = GLHelper.MultiplyWithoutTranslate(ref m, ref n);
						n = Vector3.NormalizeFast(n);

						buffer[baseOffset + VertexStructSize * i + 0] = pos.X;
						buffer[baseOffset + VertexStructSize * i + 1] = pos.Y;
						buffer[baseOffset + VertexStructSize * i + 2] = pos.Z;
						buffer[baseOffset + VertexStructSize * i + 3] = v.TexX;
						buffer[baseOffset + VertexStructSize * i + 4] = v.TexY;
						buffer[baseOffset + VertexStructSize * i + 5] = n.X;
						buffer[baseOffset + VertexStructSize * i + 6] = n.Y;
						buffer[baseOffset + VertexStructSize * i + 7] = n.Z;
						buffer[baseOffset + VertexStructSize * i + 8] = v.Cull < 1 ? scale : v.Cull;
					}

					ShadeGroupsSizes[shadeKey] += vertKeyValue.Value.Length * VertexStructSize;
				}
			}

			foreach (var child in mesh.Children) {
				_addStaticModelVertices(viewport, textures, model, child, ref instance);
			}
		}

		public void LoadModels(OpenGLViewport viewport, Rsw rsw, Gnd gnd, bool loadAnimation) {
			_watch.Start();

			GrfThread.Start(delegate {
				var models = new Dictionary<string, Rsm>();
				var sharedRsmRenderers = new Dictionary<(string, float), SharedRsmRenderer>();

				foreach (var model in rsw.Objects.OfType<Model>()) {
					var name = model.ModelName;

					if (models.ContainsKey(name))
						continue;

					var rsmEntry = ResourceManager.GetData(Rsm.RsmModelPath + model.ModelName);

					if (rsmEntry != null)
						try {
							models[name] = new Rsm(rsmEntry);
						}
						catch {
							models[name] = null;
						}
					else
						models[name] = null;
				}

				GLHelper.OnLog(() => "Message: Loaded " + models.Count + " RSM files (" + _watch.ElapsedMilliseconds + " ms)");

				foreach (var model in rsw.Objects.OfType<Model>()) {
					ModelRsm modelRsm = new ModelRsm();
					modelRsm.Model = model;
					modelRsm.Rsm = models[model.ModelName];
					bool handled = false;

					if (modelRsm.Rsm == null)
						continue;
					
					if (loadAnimation) {
						if (modelRsm.Rsm.AnimationLength > 0) {
							bool any = modelRsm.Rsm.Meshes.Any(p => p.IsAnimated);
							var key = (model.ModelName, model.AnimationSpeed);

							//if (true) {
							if (any) {
								Vector3 position = new Vector3(5 * gnd.Width + modelRsm.Model.Position.X, 0, 5 * gnd.Height + modelRsm.Model.Position.Z);
								if (position.X < 0 || position.X > gnd.Header.Width * 10 ||
									position.Z < 0 || position.Z > gnd.Header.Height * 10) {
									GLHelper.OnLog(() => "Message: Model omitted " + modelRsm.Model.ModelName + ", outside GND boundary (" + _watch.ElapsedMilliseconds + " ms)");
									continue;
								}

								if (!sharedRsmRenderers.ContainsKey(key)) {
									var rsmRenderer = new SharedRsmRenderer(_request, Shader, modelRsm.Rsm, gnd, rsw);
									sharedRsmRenderers[key] = rsmRenderer;
									_modelGroups[rsmRenderer] = new List<ModelRenderer>();
								}

								var modelRenderer = new ModelRenderer(Shader, modelRsm.Model, sharedRsmRenderers[key]);
								modelRenderer.CalculateCachedMatrix();

								_modelGroups[sharedRsmRenderers[key]].Add(modelRenderer);
								handled = true;
							}
						}
					}

					if (!handled) {
						_addStaticModel(modelRsm, gnd);
					}
				
					if (_request.CancelRequired())
						return;
				}

				foreach (var modelGroup in _modelGroups) {
					
				}

				// Does not support computer shader, load models on the CPU
				if (viewport.OpenGLVersion < 4.3) {
					_addStaticAllModelVertices(viewport);
				}

				GLHelper.OnLog(() => "Message: Loaded " + (rsw.Objects.OfType<Model>().Count() - _modelGroups.Sum(p => p.Value.Count)) + " static models, " + _modelGroups.Sum(p => p.Value.Count) + " dyanmic models (" + _watch.ElapsedMilliseconds + " ms)");
				_verticesLoaded = true;
				GLHelper.OnLog(() => "Message: Map loaded (" + _watch.ElapsedMilliseconds + " ms)");
			}, "Model loader thread");
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct VertexP3N2N3 {
		public float PosX, PosY, PosZ;
		public float TexX, TexY;
		public float NormalX, NormalY, NormalZ;
		public float Cull;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public VertexP3N2N3(Vector3 pos, Vector2 tex, Vector3 normal, float cull) {
			PosX = pos.X;
			PosY = pos.Y;
			PosZ = pos.Z;
			TexX = tex.X;
			TexY = tex.Y;
			NormalX = normal.X;
			NormalY = normal.Y;
			NormalZ = normal.Z;
			Cull = cull;
		}
	}

	public class ModelRsm {
		public Rsm Rsm { get; set; }
		public Model Model { get; set; }
		public Matrix4 MatrixCache;

		public bool RsmLoaded;
		public Dictionary<(int meshIndex, int textureId), VertexP3N2N3[]> Verts = new Dictionary<(int meshIndex, int textureId), VertexP3N2N3[]>();

		public void Load() {
			if (RsmLoaded)
				return;

			_initMeshInfo(Rsm.MainMesh);
			RsmLoaded = true;
		}

		private void _initMeshInfo(Mesh mesh) {
			Matrix4 matrix = Matrix4.Identity;
			_initMeshInfo(mesh, ref matrix);
		}

		private void _initMeshInfo(Mesh mesh, ref Matrix4 matrix) {
			var dict = new Dictionary<int, List<Face>>();

			foreach (var face in mesh.Faces) {
				var key = face.TextureId;
				if (!dict.TryGetValue(key, out var list)) {
					list = new List<Face>(64);
					dict[key] = list;
				}
				list.Add(face);
			}

			foreach (var entry in dict) {
				var l = new VertexP3N2N3[entry.Value.Count * 3];
				Verts[(mesh.Index, entry.Key)] = l;
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

			if (mesh.Model.Version >= 2.2) {
				mesh.TempMatrix = mesh.Matrix2;
				mesh.TempMatrixSub = Matrix4.Identity;
			}
			else {
				mesh.TempMatrixSub = mesh.Matrix1 * matrix;
				mesh.TempMatrix = mesh.Matrix2 * mesh.TempMatrixSub;
			}

			foreach (var child in mesh.Children) {
				_initMeshInfo(child, ref mesh.TempMatrixSub);
			}
		}
	}
}
