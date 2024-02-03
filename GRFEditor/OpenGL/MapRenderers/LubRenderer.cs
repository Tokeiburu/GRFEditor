using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.FileFormats.LubFormat;
using GRF.FileFormats.RswFormat;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using Lua;
using Lua.Structure;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Utilities.Services;

namespace GRFEditor.OpenGL.MapRenderers {
	public class LubEffect {
		public Vector3 Dir1;
		public Vector3 Dir2;
		public Vector3 Gravity;
		public Vector3 Radius;
		public Vector4 Color;
		public Vector2 Rate;
		public Vector2 Size;
		public Vector2 Life;
		public string Texture;
		public float Speed;
		public int Srcmode;
		public int Destmode;
		public int Maxcount;
		public bool Zenable;
		public int Billboard_off;
		public Vector3 Rotate_angle;
		public Matrix4 ModelMatrix;
		public Texture Texture2D { get; set; }
		public float EmitTime { get; set; }
		public Vector3 Pos { get; set; }
		public bool IsAnimated { get; set; }

		public List<Particle> Particles = new List<Particle>();
		public bool VboAssigned;
		public CloudEffectSettings CloudEffect;
		public bool IsEnabled = true;
	}

	public class Particle {
		public Vector3 Position;
		public Vector3 Speed;
		public Vector3 Dir;
		public float Size;
		public float Life;
		public float StartLife;
		public float Alpha;
		public float CloudAlphaIncreaseDuration;
		public float CloudAlphaDecreaseDuration;
		public float CloudAlphaIncreaseFactor;
		public float CloudAlphaDecreaseFactor;
		public float CloudAlphaDecreaseOffset;
		public float CloudOriginalSize;
		public float CloudExpandTime;
		public float CloudExpandFactor;
		public float TextureLife { get; set; }
		public int TextureIndex { get; set; }
		public int TextureType { get; set; }
	}

	public class RenderInfoEffects {
		public List<LubEffect> Effects = new List<LubEffect>();
		public RenderInfo RenderInfo = new RenderInfo();
	}

	public class CloudEffectSettings {
		public int Num = 1000;
		public int CullDist;
		public Vector3 Color = new Vector3(1f);
		public float Size = 20f;
		public float Size_Extra = 20f;
		public float Expand_Rate = 0.05f;
		public float Alpha_Inc_Time = 80f;
		public float Alpha_Inc_Time_Extra = 50f;
		public float Alpha_Inc_Speed = 50f;
		public float Alpha_Dec_Time = 300f;
		public float Alpha_Dec_Time_Extra = 200f;
		public float Alpha_Dec_Speed = 0.5f;
		public float Height = 50f;
		public float Height_Extra = 10f;
		public bool FlipImage = true;
		public Vector3 Dir1 = new Vector3(-5, 0, -5);
		public Vector3 Dir2 = new Vector3(5, 0, 5);
		public Vector4? Bg_Color;
		public List<string> TexturesResources = new List<string>();
		public bool IsChanged { get; set; }
		public bool StarEffect { get; set; }
		public bool CloudEffect { get; set; }
	}

	public class LubRenderer : Renderer {
		private readonly Gnd _gnd;
		private readonly List<LubEffect> _effects = new List<LubEffect>();
		private readonly Stopwatch _watch = new Stopwatch();
		private bool _verticesLoaded;
		private readonly RendererLoadRequest _request;
		private readonly Rsw _rsw;
		private static bool _hasMapSkyData = true;
		private static SimplifiedLuaElement _skyMapLub;
		private readonly Dictionary<Texture, RenderInfoEffects> _groups = new Dictionary<Texture, RenderInfoEffects>();
		private readonly Dictionary<Texture, RenderInfoEffects> _skymapGroups = new Dictionary<Texture, RenderInfoEffects>();
		private readonly RenderInfoEffects _animatedGroup = new RenderInfoEffects();
		private float _previousTime;
		const int LubEffectVertexSize = 6;

		public LubRenderer(RendererLoadRequest request, Shader shader, Gnd gnd, Rsw rsw, byte[] lubData, OpenGLViewport viewport) {
			Shader = shader;
			_gnd = gnd;
			_rsw = rsw;
			_request = request;

			if (lubData != null) {
				if (Methods.ByteArrayCompare(lubData, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0)) {
					Lub lub = new Lub(lubData);
					var text = lub.Decompile();
					lubData = EncodingService.DisplayEncoding.GetBytes(text);
				}

				SimplifiedLuaElement lua;
				SimplifiedLuaElement luaEmitter;

				using (LuaReader reader = new LuaReader(new MemoryStream(lubData))) {
					lua = reader.ReadSimplified();
				}

				if ((luaEmitter = lua["_" + Path.GetFileName(request.Resource.Replace("@", "")) + "_emitterInfo"]) != null) {
					try {
						_parseEmitter(luaEmitter, false);
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}

				if ((luaEmitter = lua["_" + Path.GetFileName(request.Resource) + "_animatedEmitterInfo"]) != null) {
					_parseEmitter(luaEmitter, true);
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\shockwave_b.bmp", Rsm.RsmTexturePath + @"effect\shockwave_b.bmp", TextureRenderMode.RsmTexture, _request));
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\shockwave_c.bmp", Rsm.RsmTexturePath + @"effect\shockwave_c.bmp", TextureRenderMode.RsmTexture, _request));
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\shockwave_d.bmp", Rsm.RsmTexturePath + @"effect\shockwave_d.bmp", TextureRenderMode.RsmTexture, _request));
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\shockwave_e.bmp", Rsm.RsmTexturePath + @"effect\shockwave_e.bmp", TextureRenderMode.RsmTexture, _request));
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\plazma_a.bmp", Rsm.RsmTexturePath + @"effect\plazma_a.bmp", TextureRenderMode.RsmTexture, _request));
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\plazma_b.bmp", Rsm.RsmTexturePath + @"effect\plazma_b.bmp", TextureRenderMode.RsmTexture, _request));
					Textures.Add(TextureManager.LoadTextureAsync(@"effect\plazma_c.bmp", Rsm.RsmTexturePath + @"effect\plazma_c.bmp", TextureRenderMode.RsmTexture, _request));
				}
			}

			_skyMap(viewport);
			_watch.Start();
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			foreach (var effect in _effects) {
				var texture = TextureManager.LoadTextureAsync(effect.Texture, Rsm.RsmTexturePath + effect.Texture.Replace("\\\\", "\\"), effect.CloudEffect != null ? TextureRenderMode.CloudTexture : TextureRenderMode.RsmTexture, _request);
				Textures.Add(texture);
				effect.Texture2D = texture;

				if (effect.CloudEffect != null && effect.CloudEffect.FlipImage)
					effect.Texture2D.Reverse = true;

				effect.ModelMatrix = Matrix4.Identity;
				effect.ModelMatrix = GLHelper.Scale(effect.ModelMatrix, new Vector3(1, 1, -1));

				effect.ModelMatrix = GLHelper.Translate(effect.ModelMatrix, new Vector3(5 * _gnd.Width + effect.Pos.X, -effect.Pos.Y + 11f, -9 - 5 * _gnd.Height + effect.Pos.Z));

				if (effect.Billboard_off == 1) {
					effect.ModelMatrix = GLHelper.Rotate(effect.ModelMatrix, -GLHelper.ToRad(effect.Rotate_angle.Z), new Vector3(0, 0, 1));
					effect.ModelMatrix = GLHelper.Rotate(effect.ModelMatrix, GLHelper.ToRad(effect.Rotate_angle.Y), new Vector3(0, 1, 0));
					effect.ModelMatrix = GLHelper.Rotate(effect.ModelMatrix, -GLHelper.ToRad(effect.Rotate_angle.X), new Vector3(1, 0, 0));
				}

				if (effect.IsAnimated) {
					_animatedGroup.Effects.Add(effect);
				}
				else if (effect.CloudEffect != null) {
					if (!_skymapGroups.ContainsKey(effect.Texture2D)) {
						_skymapGroups[effect.Texture2D] = new RenderInfoEffects();
					}

					_skymapGroups[effect.Texture2D].Effects.Add(effect);
				}
				else {
					if (!_groups.ContainsKey(effect.Texture2D)) {
						_groups[effect.Texture2D] = new RenderInfoEffects();
					}

					_groups[effect.Texture2D].Effects.Add(effect);
				}
			}

			foreach (var group in _groups) {
				group.Value.RenderInfo.RawVertices = new float[LubEffectVertexSize * 4 * group.Value.Effects.Sum(p => p.Maxcount)];
			}

			foreach (var group in _skymapGroups) {
				group.Value.RenderInfo.RawVertices = new float[LubEffectVertexSize * 4 * group.Value.Effects.Sum(p => p.Maxcount)];
			}

			_animatedGroup.RenderInfo.RawVertices = new float[LubEffectVertexSize * 4 * _animatedGroup.Effects.Sum(p => p.Maxcount)];

			_verticesLoaded = true;
		}
		
		private bool _skipRender = false;

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.LubEffect || viewport.RenderOptions.MinimapMode)
				return;

			if (!_verticesLoaded) {
				Load(viewport);
			}

			if (!_watch.IsRunning) {
				_watch.Start();
			}

			_skipRender = !_skipRender;

			Shader.Use();
			Shader.SetMatrix4("cameraMatrix", viewport.View);
			Shader.SetMatrix4("projectionMatrix", viewport.Projection);

			GL.Enable(EnableCap.Blend);
			GL.DepthMask(false);

			float elapsedTime = _watch.ElapsedMilliseconds / 1000f;
			float interval = elapsedTime - _previousTime;
			_previousTime = elapsedTime;

			foreach (var renderGroup in _groups) {
				_renderGroup(renderGroup.Key, renderGroup.Value, interval);
			}

			GL.Enable(EnableCap.DepthTest);

			if (_animatedGroup.Effects.Count > 0) {
				_renderAnimatedGroup(Textures, interval);
			}

			if (viewport.RenderOptions.RenderSkymapFeature && viewport.RenderOptions.RenderSkymapDetected) {
				//if (!_skymapDataLoaded) {
				//	_addStarCloudEffect();
				//	_addCloudEffect();
				//	_skymapDataLoaded = true;
				//}

				foreach (var renderGroup in _skymapGroups) {
					bool amountChanged = false;

					if (MapRenderer.SkyMap.IsChanged) {
						if (renderGroup.Key.Resource.Contains("cloud")) {
							foreach (var effect in renderGroup.Value.Effects) {
								effect.Color = new Vector4(effect.CloudEffect.Color, 1);

								if (effect.CloudEffect.Num / 4 != effect.Maxcount) {
									amountChanged = true;
									effect.Particles.Clear();
								}

								effect.Maxcount = effect.CloudEffect.Num / 4;
								effect.Pos = new Vector3(0, effect.CloudEffect.Height, 0);
								effect.IsEnabled = MapRenderer.SkyMap.CloudEffect;
							}

							if (amountChanged)
								renderGroup.Value.RenderInfo.RawVertices = new float[LubEffectVertexSize * 4 * renderGroup.Value.Effects.Sum(p => p.Maxcount)];
						}
						else {
							foreach (var effect in renderGroup.Value.Effects) {
								effect.IsEnabled = MapRenderer.SkyMap.StarEffect;
								effect.CloudEffect.Height = MapRenderer.SkyMap.Height;
								effect.CloudEffect.Height_Extra = MapRenderer.SkyMap.Height_Extra;
							}
						}
					}

					if (renderGroup.Value.Effects.First().IsEnabled)
						_renderGroup(renderGroup.Key, renderGroup.Value, interval);
				}

				MapRenderer.SkyMap.IsChanged = false;
			}

			GL.DepthMask(true);
			GL.Enable(EnableCap.DepthTest);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		private void _renderGroup(Texture key, RenderInfoEffects rie, float interval) {
			int vboIndex = 0;
			var ri = rie.RenderInfo;

			if (ri.Vao == 0) {
				ri.CreateVao();
				ri.Vbo = new Vbo();
				ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.DynamicDraw, LubEffectVertexSize);
				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.EnableVertexAttribArray(2);
				GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 0);
				GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 2 * sizeof(float));
				GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 3 * sizeof(float));
			}

			if (!_skipRender) {
				foreach (var effect in rie.Effects) {
					effect.EmitTime -= interval;

					if (effect.CloudEffect != null) {
						for (int i = 0; i < effect.Particles.Count; i++) {
							var p = effect.Particles[i];
							p.Life -= interval;

							if (p.Life < 0) {
								effect.Particles.RemoveAt(i);
								i--;
								continue;
							}

							p.Position += p.Dir * interval * (p.CloudExpandFactor > 0 ? 0.8f : 1f) * effect.Speed + p.Speed * interval;
							p.CloudExpandTime += interval * p.CloudExpandFactor;

							p.Size = p.CloudOriginalSize + p.CloudOriginalSize * effect.CloudEffect.Expand_Rate * p.CloudExpandTime / 2;

							if (p.CloudExpandTime > 2) {
								p.CloudExpandFactor = -1;
							}
							else if (p.CloudExpandTime < 0) {
								p.CloudExpandFactor = 1;
							}

							if (p.Life > p.StartLife - p.CloudAlphaIncreaseDuration) {
								p.Alpha += p.CloudAlphaIncreaseFactor * interval;
							}
							else if (p.Life < p.CloudAlphaDecreaseOffset) {
								p.Alpha -= p.CloudAlphaDecreaseFactor * interval;
							}

							p.Alpha = (float)GLHelper.Clamp(0, 1, p.Alpha);
						}
					}
					else {
						for (int i = 0; i < effect.Particles.Count; i++) {
							effect.Particles[i].Life -= interval;

							if (effect.Particles[i].Life < 0) {
								effect.Particles.RemoveAt(i);
								i--;
								continue;
							}

							effect.Particles[i].Position += effect.Particles[i].Dir * interval * effect.Speed + effect.Particles[i].Speed * interval;
							effect.Particles[i].Speed += effect.Speed * effect.Gravity * interval;

							if (effect.Particles[i].StartLife > 1) {
								if (effect.Particles[i].Life < Math.Min(1f, effect.Particles[i].StartLife / 2)) {
									effect.Particles[i].Alpha -= interval;
								}
								else {
									effect.Particles[i].Alpha += interval;
								}

								effect.Particles[i].Alpha = (float)GLHelper.Clamp(0, 1, effect.Particles[i].Alpha);
							}
						}
					}

					if (effect.CloudEffect != null) {
						while (effect.Particles.Count < effect.Maxcount) {
							Particle p = new Particle();

							p.Position = new Vector3(
								TkRandom.Rand(-effect.Radius.X, effect.Radius.X) + MapRenderer.LookAt.X - _request.Gnd.Width * 5,
								-effect.CloudEffect.Height - TkRandom.Rand(0, effect.CloudEffect.Height_Extra),
								TkRandom.Rand(-effect.Radius.Z, effect.Radius.Z) - MapRenderer.LookAt.Z + _request.Gnd.Height * 5);
							p.Dir = new Vector3(
								TkRandom.Rand(effect.Dir1.X, effect.Dir2.X),
								TkRandom.Rand(effect.Dir1.Y, effect.Dir2.Y),
								TkRandom.Rand(effect.Dir1.Z, effect.Dir2.Z));
							p.Size = TkRandom.Rand(effect.CloudEffect.Size, effect.CloudEffect.Size + effect.CloudEffect.Size_Extra);
							p.CloudAlphaIncreaseDuration = TkRandom.Rand(effect.CloudEffect.Alpha_Inc_Time, effect.CloudEffect.Alpha_Inc_Time + effect.CloudEffect.Alpha_Inc_Time_Extra) * 0.015f; // 1.5 / 100
							p.CloudAlphaIncreaseFactor = 1f / (4f / effect.CloudEffect.Alpha_Inc_Speed);
							p.CloudAlphaDecreaseDuration = TkRandom.Rand(effect.CloudEffect.Alpha_Dec_Time, effect.CloudEffect.Alpha_Dec_Time + effect.CloudEffect.Alpha_Dec_Time_Extra) * 0.015f; // 1.5 / 100
							p.CloudAlphaDecreaseOffset = Math.Min(4f / effect.CloudEffect.Alpha_Dec_Speed, p.CloudAlphaDecreaseDuration);
							p.CloudAlphaDecreaseFactor = 1f / (p.CloudAlphaDecreaseOffset);
							p.CloudExpandFactor = 1;
							p.CloudExpandTime = TkRandom.Rand(0, 2);
							p.CloudOriginalSize = p.Size;
							p.Life = p.CloudAlphaIncreaseDuration + p.CloudAlphaDecreaseDuration;
							p.StartLife = p.Life;
							effect.Particles.Add(p);
						}
					}
					else {
						if (effect.EmitTime < 0 && effect.Particles.Count < effect.Maxcount) {
							Particle p = new Particle();

							p.Position = new Vector3(
								TkRandom.Rand(-effect.Radius.X, -effect.Radius.X + 2 * Math.Abs(effect.Radius.X)),
								TkRandom.Rand(-effect.Radius.Y, -effect.Radius.Y + 2 * Math.Abs(effect.Radius.Y)),
								TkRandom.Rand(-effect.Radius.Z, -effect.Radius.Z + 2 * Math.Abs(effect.Radius.Z)));
							p.Dir = new Vector3(
								TkRandom.Rand(effect.Dir1.X, effect.Dir2.X),
								TkRandom.Rand(effect.Dir1.Y, effect.Dir2.Y),
								TkRandom.Rand(effect.Dir1.Z, effect.Dir2.Z));
							p.Life = TkRandom.Rand(effect.Life.X, effect.Life.Y);
							p.StartLife = p.Life;
							p.Size = TkRandom.Rand(effect.Size.X, effect.Size.Y);
							effect.Particles.Add(p);
							effect.EmitTime = 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
						}
					}

					for (int i = 0; i < effect.Particles.Count; i++) {
						var p = effect.Particles[i];
						float alpha = p.Alpha;

						ri.RawVertices[vboIndex + 24 * i + 0] = -p.Size;
						ri.RawVertices[vboIndex + 24 * i + 1] = -p.Size;
						ri.RawVertices[vboIndex + 24 * i + 2] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 3] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 4] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 5] = p.Position.Z;

						ri.RawVertices[vboIndex + 24 * i + 6] = -p.Size;
						ri.RawVertices[vboIndex + 24 * i + 7] = p.Size;
						ri.RawVertices[vboIndex + 24 * i + 8] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 9] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 10] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 11] = p.Position.Z;

						ri.RawVertices[vboIndex + 24 * i + 12] = p.Size;
						ri.RawVertices[vboIndex + 24 * i + 13] = p.Size;
						ri.RawVertices[vboIndex + 24 * i + 14] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 15] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 16] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 17] = p.Position.Z;

						ri.RawVertices[vboIndex + 24 * i + 18] = p.Size;
						ri.RawVertices[vboIndex + 24 * i + 19] = -p.Size;
						ri.RawVertices[vboIndex + 24 * i + 20] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 21] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 22] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 23] = p.Position.Z;
					}

					vboIndex += LubEffectVertexSize * effect.Maxcount * 4;
				}
			}

			vboIndex = 0;

			ri.BindVao();

			if (!_skipRender) {
				ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.DynamicDraw, LubEffectVertexSize);
			}

			key.Bind();

			Vector4? prevColor = null;
			int? prevBillboard = null;
			bool? prevZenable = null;
			int? prevBlend = null;

			foreach (var effect in rie.Effects) {
				if (prevZenable == null || prevZenable != effect.Zenable) {
					if (effect.Zenable)
						GL.Enable(EnableCap.DepthTest);
					else
						GL.Disable(EnableCap.DepthTest);
					prevZenable = effect.Zenable;
				}

				if (prevColor == null || prevColor != effect.Color) {
					Shader.SetVector4("color", effect.Color);
					prevColor = effect.Color;
				}

				if (prevBillboard == null || prevBillboard != effect.Billboard_off) {
					Shader.SetFloat("billboard_off", effect.Billboard_off);
					prevBillboard = effect.Billboard_off;
				}

				Shader.SetMatrix4("m", effect.ModelMatrix);

				if (prevBlend == null || prevBlend != (effect.Srcmode << 16 | effect.Destmode)) {
					GL.BlendFunc(GLHelper.GetOpenGlBlendFromDirectXSrc(effect.Srcmode), GLHelper.GetOpenGlBlendFromDirectXDest(effect.Destmode));
					prevBlend = (effect.Srcmode << 16 | effect.Destmode);
				}

				GL.DrawArrays(PrimitiveType.Quads, vboIndex, effect.Particles.Count * 4);
				vboIndex += effect.Maxcount * 4;
			}
		}

		private void _renderAnimatedGroup(List<Texture> textures, float interval) {
			int vboIndex = 0;
			var rie = _animatedGroup;
			var ri = rie.RenderInfo;

			if (ri.Vao == 0) {
				ri.CreateVao();
				ri.Vbo = new Vbo();
				ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.DynamicDraw, LubEffectVertexSize);
				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.EnableVertexAttribArray(2);
				GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 0);
				GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 2 * sizeof(float));
				GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 3 * sizeof(float));
			}

			foreach (var effect in rie.Effects) {
				effect.EmitTime -= interval;

				for (int i = 0; i < effect.Particles.Count; i++) {
					var p = effect.Particles[i];

					p.Life -= interval;
					p.TextureLife -= interval;

					if (p.Life < 0) {
						effect.Particles.RemoveAt(i);
						i--;
						continue;
					}

					if (p.TextureLife < 0) {
						p.TextureLife += 0.2f;
						p.TextureIndex++;

						if (p.TextureType == 0 && p.TextureIndex > 3)
							p.TextureIndex = 0;
						else if (p.TextureType == 1 && p.TextureIndex > 6)
							p.TextureIndex = 4;
					}

					p.Position += p.Dir * interval * effect.Speed + p.Speed * interval;
					p.Speed += effect.Speed * effect.Gravity * interval;

					if (p.StartLife > 1) {
						if (p.Life < Math.Min(1f, p.StartLife / 2)) {
							p.Alpha -= interval;
						}
						else {
							p.Alpha += interval;
						}

						p.Alpha = (float)GLHelper.Clamp(0, 1, p.Alpha);
					}
				}

				if (effect.EmitTime < 0 && effect.Particles.Count < effect.Maxcount) {
					Particle p = new Particle();

					p.Position = new Vector3(
						TkRandom.Rand(-effect.Radius.X, -effect.Radius.X + 2 * Math.Abs(effect.Radius.X)),
						TkRandom.Rand(-effect.Radius.Y, -effect.Radius.Y + 2 * Math.Abs(effect.Radius.Y)),
						TkRandom.Rand(-effect.Radius.Z, -effect.Radius.Z + 2 * Math.Abs(effect.Radius.Z)));
					p.Dir = new Vector3(
						TkRandom.Rand(effect.Dir1.X, effect.Dir2.X),
						TkRandom.Rand(effect.Dir1.Y, effect.Dir2.Y),
						TkRandom.Rand(effect.Dir1.Z, effect.Dir2.Z));
					p.Life = TkRandom.Rand(effect.Life.X, effect.Life.Y);
					p.StartLife = p.Life;
					p.Size = TkRandom.Rand(effect.Size.X, effect.Size.Y);
					effect.Particles.Add(p);
					effect.EmitTime = 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
					p.TextureType = TkRandom.Next() % 2;
					p.TextureLife = 0.2f;

					if (p.TextureType == 0)
						p.TextureIndex = TkRandom.Next() % 4;
					else
						p.TextureIndex = (TkRandom.Next() % 3) + 4;
				}

				for (int i = 0; i < effect.Particles.Count; i++) {
					var p = effect.Particles[i];
					float alpha = p.Alpha;

					ri.RawVertices[vboIndex + 24 * i + 0] = -p.Size;
					ri.RawVertices[vboIndex + 24 * i + 1] = -p.Size;
					ri.RawVertices[vboIndex + 24 * i + 2] = alpha;
					ri.RawVertices[vboIndex + 24 * i + 3] = p.Position.X;
					ri.RawVertices[vboIndex + 24 * i + 4] = p.Position.Y;
					ri.RawVertices[vboIndex + 24 * i + 5] = p.Position.Z;

					ri.RawVertices[vboIndex + 24 * i + 6] = -p.Size;
					ri.RawVertices[vboIndex + 24 * i + 7] = p.Size;
					ri.RawVertices[vboIndex + 24 * i + 8] = alpha;
					ri.RawVertices[vboIndex + 24 * i + 9] = p.Position.X;
					ri.RawVertices[vboIndex + 24 * i + 10] = p.Position.Y;
					ri.RawVertices[vboIndex + 24 * i + 11] = p.Position.Z;

					ri.RawVertices[vboIndex + 24 * i + 12] = p.Size;
					ri.RawVertices[vboIndex + 24 * i + 13] = p.Size;
					ri.RawVertices[vboIndex + 24 * i + 14] = alpha;
					ri.RawVertices[vboIndex + 24 * i + 15] = p.Position.X;
					ri.RawVertices[vboIndex + 24 * i + 16] = p.Position.Y;
					ri.RawVertices[vboIndex + 24 * i + 17] = p.Position.Z;

					ri.RawVertices[vboIndex + 24 * i + 18] = p.Size;
					ri.RawVertices[vboIndex + 24 * i + 19] = -p.Size;
					ri.RawVertices[vboIndex + 24 * i + 20] = alpha;
					ri.RawVertices[vboIndex + 24 * i + 21] = p.Position.X;
					ri.RawVertices[vboIndex + 24 * i + 22] = p.Position.Y;
					ri.RawVertices[vboIndex + 24 * i + 23] = p.Position.Z;
				}

				vboIndex += LubEffectVertexSize * effect.Maxcount * 4;
			}

			vboIndex = 0;

			ri.BindVao();
			ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.DynamicDraw, LubEffectVertexSize);

			foreach (var effect in rie.Effects) {
				Shader.SetMatrix4("m", effect.ModelMatrix);
				Shader.SetVector4("color", effect.Color);
				Shader.SetFloat("billboard_off", effect.Billboard_off);
				GL.BlendFunc(GLHelper.GetOpenGlBlendFromDirectXSrc(effect.Srcmode), GLHelper.GetOpenGlBlendFromDirectXDest(effect.Destmode));
				
				foreach (var particle in effect.Particles) {
					textures[particle.TextureIndex].Bind();
					GL.DrawArrays(PrimitiveType.Quads, vboIndex, 4);
					vboIndex += 4;
				}

				vboIndex += (effect.Maxcount - effect.Particles.Count) * 4;
			}
		}

		private void _parseEmitter(SimplifiedLuaElement lua, bool isAnimated) {
			foreach (var effectKeyValue in lua.KeyValues) {
				LubEffect effect = new LubEffect();
				var key = Int32.Parse(effectKeyValue.Key.Trim('[', ']'));

				if (!isAnimated && key >= _rsw.LubEffects.Count) {
					continue;
				}

				if (_request.CancelRequired())
					break;

				_effects.Add(effect);

				if (!isAnimated) {
					effect.Pos = new Vector3(_rsw.LubEffects[key].Position.X, _rsw.LubEffects[key].Position.Y, _rsw.LubEffects[key].Position.Z);
					_rsw.LubEffects[key].LubEffectAttached = effect;
				}

				foreach (var property in effectKeyValue.Value.KeyValues) {
					var pv = property.Value;

					switch (property.Key.Trim('[', ']', '\"')) {
						case "dir1":
							effect.Dir1 = new Vector3(pv[0].Cast<float>(), -pv[1].Cast<float>(), pv[2].Cast<float>());
							break;
						case "dir2":
							effect.Dir2 = new Vector3(pv[0].Cast<float>(), -pv[1].Cast<float>(), pv[2].Cast<float>());
							break;
						case "gravity":
							effect.Gravity = new Vector3(pv[0].Cast<float>(), -pv[1].Cast<float>(), pv[2].Cast<float>());
							break;
						case "pos":
							if (isAnimated)
								effect.Pos = new Vector3(pv[0].Cast<float>(), pv[1].Cast<float>(), pv[2].Cast<float>());
							break;
						case "radius":
							effect.Radius = new Vector3(pv[0].Cast<float>(), pv[1].Cast<float>(), pv[2].Cast<float>());
							break;
						case "color":
							// ?? The alpha is ignored... ish?
							effect.Color = new Vector4(pv[0].Cast<float>() / 255f, pv[1].Cast<float>() / 255f, pv[2].Cast<float>() / 255f, 1f);
							break;
						case "rate":
							effect.Rate = new Vector2(pv[0].Cast<float>(), pv[1].Cast<float>());
							break;
						case "size":
							effect.Size = new Vector2(pv[0].Cast<float>(), pv[1].Cast<float>());
							break;
						case "life":
							effect.Life = new Vector2(pv[0].Cast<float>(), pv[1].Cast<float>());
							break;
						case "texture":
							effect.Texture = pv.Value.Trim('\"', '[', ']').Replace("\\\\", "\\");
							break;
						case "speed":
							effect.Speed = pv[0].Cast<float>();
							break;
						case "srcmode":
							effect.Srcmode = pv[0].Cast<int>();
							break;
						case "destmode":
							effect.Destmode = pv[0].Cast<int>();
							break;
						case "maxcount":
							effect.Maxcount = pv[0].Cast<int>();
							break;
						case "zenable":
							effect.Zenable = pv[0].Cast<int>() != 0;
							break;
						case "billboard_off":
							effect.Billboard_off = pv[0].Cast<int>();
							break;
						case "rotate_angle":
							effect.Rotate_angle = new Vector3(pv[0].Cast<float>(), pv[1].Cast<float>(), pv[2].Cast<float>());
							break;
					}
				}

				if (effect.Billboard_off != 0) {
					effect.Dir1.Z *= -1;
					effect.Dir2.Z *= -1;
					effect.Gravity.Z *= -1;
				}

				effect.EmitTime = 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
				effect.IsAnimated = isAnimated;
			}
		}

		private void _addCloudEffect() {
			CloudEffectSettings cs = MapRenderer.SkyMap;

			cs.TexturesResources.Add("effect\\cloud1.tga");
			cs.TexturesResources.Add("effect\\cloud2.tga");
			cs.TexturesResources.Add("effect\\cloud3.tga");
			cs.TexturesResources.Add("effect\\cloud4.tga");

			for (int i = 0; i < cs.TexturesResources.Count; i++) {
				LubEffect effect = new LubEffect();
				effect.Billboard_off = 0;
				effect.Color = new Vector4(cs.Color, 1f);
				effect.Srcmode = 5;
				effect.Destmode = 6;
				effect.Dir1 = cs.Dir1;
				effect.Dir2 = cs.Dir2;
				effect.Speed = 0.5f;
				effect.Gravity = new Vector3(0);
				effect.Life = new Vector2(1.5f * (cs.Alpha_Inc_Time + cs.Alpha_Dec_Time) / 10f, 1.5f * (cs.Alpha_Inc_Time + cs.Alpha_Dec_Time_Extra + cs.Alpha_Dec_Time + cs.Alpha_Inc_Time_Extra) / 10f);
				effect.Maxcount = cs.Num / cs.TexturesResources.Count;
				effect.Radius = new Vector3(500, 0, 500);
				effect.Rate = new Vector2(cs.Num, cs.Num);
				effect.Size = new Vector2(cs.Size, cs.Size + cs.Size_Extra);
				effect.Texture = cs.TexturesResources[i];
				effect.EmitTime = 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
				effect.Pos = new Vector3(0, 0, 0);
				effect.Rotate_angle = new Vector3(0, 0, 0);
				effect.Zenable = true;
				effect.IsEnabled = cs.CloudEffect;
				effect.CloudEffect = MapRenderer.SkyMap;
				_effects.Add(effect);
			}
		}

		private void _addStarCloudEffect() {
			CloudEffectSettings cs = new CloudEffectSettings();

			if (!MapRenderer.SkyMap.CloudEffect) {
				cs.Height = 100f;
				cs.Height_Extra = 0;
			}
			else {
				cs.Height = 0;
			}

			cs.Num = 700;
			cs.Size = 30f;
			cs.Size_Extra = 0f;
			cs.TexturesResources.Add("effect\\star01.bmp");
			cs.TexturesResources.Add("effect\\star02.bmp");
			cs.TexturesResources.Add("effect\\star03.bmp");
			cs.TexturesResources.Add("effect\\star04.bmp");
			cs.TexturesResources.Add("effect\\star05.bmp");
			cs.TexturesResources.Add("effect\\star06.bmp");

			for (int i = 0; i < cs.TexturesResources.Count; i++) {
				LubEffect effect = new LubEffect();
				effect.Billboard_off = 0;
				effect.Color = new Vector4(cs.Color, 1f);
				effect.Srcmode = 5;
				effect.Destmode = 7;
				effect.Dir1 = cs.Dir1;
				effect.Dir2 = cs.Dir2;
				effect.Speed = 0.5f;
				effect.Gravity = new Vector3(0);
				effect.Life = new Vector2(1.5f * (cs.Alpha_Inc_Time + cs.Alpha_Dec_Time) / 10f, 1.5f * (cs.Alpha_Inc_Time + cs.Alpha_Dec_Time_Extra + cs.Alpha_Dec_Time + cs.Alpha_Inc_Time_Extra) / 10f);
				effect.Maxcount = cs.Num / cs.TexturesResources.Count;
				effect.Radius = new Vector3(500, 0, 500);
				effect.Rate = new Vector2(cs.Num, cs.Num);
				effect.Size = new Vector2(cs.Size, cs.Size + cs.Size_Extra);
				effect.Texture = cs.TexturesResources[i];
				effect.EmitTime = 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
				effect.Pos = new Vector3(0, 0, 0);
				effect.Rotate_angle = new Vector3(0, 0, 0);
				effect.Zenable = true;
				effect.IsEnabled = MapRenderer.SkyMap.StarEffect;
				effect.CloudEffect = cs;
				_effects.Add(effect);
			}
		}

		private void _skyMap(OpenGLViewport viewport) {
			if (_hasMapSkyData) {
				if (_skyMapLub == null) {
					try {
						var mapskydata = ResourceManager.GetData(@"data\luafiles514\lua files\mapskydata\mapskydata.lub");

						if (mapskydata == null) {
							_hasMapSkyData = false;
						}
						else {
							if (Methods.ByteArrayCompare(mapskydata, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0)) {
								Lub lub = new Lub(mapskydata);
								var text = lub.Decompile();
								mapskydata = EncodingService.DisplayEncoding.GetBytes(text);
							}

							using (LuaReader reader = new LuaReader(new MemoryStream(mapskydata))) {
								_skyMapLub = reader.ReadSimplified();
							}

							_skyMapLub = _skyMapLub["MapSkyData"];
						}
					}
					catch {
						_hasMapSkyData = false;
					}
				}

				if (_skyMapLub != null) {
					var mapName = Path.GetFileName(_request.Resource) + ".rsw";

					if (_skyMapLub.KeyValues.ContainsKey(mapName)) {
						var skymap = _skyMapLub[mapName];
						Vector4 bgColor = new Vector4(skymap["BG_Color"][0].Cast<float>() / 255f, skymap["BG_Color"][1].Cast<float>() / 255f, skymap["BG_Color"][2].Cast<float>() / 255f, 1f);

						viewport.RenderOptions.SkymapBackgroundColor = bgColor;

						if (skymap["Old_Cloud_Effect"] != null) {
							//int cloud_effect = skymap["Old_Cloud_Effect"].Cast<int>();
							//
							//switch(cloud_effect) {
							//	case 1:
							//		cloudSetting.Num = 1000;
							//		cloudSetting.Color = new Vector3(1f);
							//		cloudSetting.Size = 20f;
							//		cloudSetting.Size_Extra = 20f;
							//		cloudSetting.Expand_Rate = 0.05f;
							//		cloudSetting.Alpha_Inc_Time = 80f;
							//		cloudSetting.Alpha_Inc_Time_Extra = 50f;
							//		cloudSetting.Alpha_Inc_Speed = 50f;
							//		cloudSetting.Alpha_Dec_Time = 300f;
							//		cloudSetting.Alpha_Dec_Time_Extra = 200f;
							//		cloudSetting.Alpha_Dec_Speed = 0.5f;
							//		cloudSetting.Height = 50f;
							//		cloudSetting.Height_Extra = 10f;
							//		cloudSetting.FlipImage = true;
							//		break;
							//}
							MapRenderer.SkyMap = new CloudEffectSettings();
							MapRenderer.SkyMap.CloudEffect = true;
						}
						else if (skymap["Cloud_Effect"] != null) {
							var lua = skymap["Cloud_Effect"].KeyValues.First().Value;

							if (lua != null) {
								if (skymap["Num"] != null)
									MapRenderer.SkyMap.Num = int.Parse(skymap["Num"].Value);
								if (skymap["Color"] != null)
									MapRenderer.SkyMap.Color = new Vector3(skymap["Color"][0].Cast<float>() / 255f, skymap["Color"][1].Cast<float>() / 255f, skymap["Color"][2].Cast<float>() / 255f);
								if (skymap["Size"] != null)
									MapRenderer.SkyMap.Size = float.Parse(skymap["Size"].Value);
								if (skymap["Size_Extra"] != null)
									MapRenderer.SkyMap.Size_Extra = float.Parse(skymap["Size_Extra"].Value);
								if (skymap["Expand_Rate"] != null)
									MapRenderer.SkyMap.Expand_Rate = float.Parse(skymap["Expand_Rate"].Value);
								if (skymap["Alpha_Inc_Time"] != null)
									MapRenderer.SkyMap.Alpha_Inc_Time = float.Parse(skymap["Alpha_Inc_Time"].Value);
								if (skymap["Alpha_Inc_Time_Extra"] != null)
									MapRenderer.SkyMap.Alpha_Inc_Time_Extra = float.Parse(skymap["Alpha_Inc_Time_Extra"].Value);
								if (skymap["Alpha_Inc_Speed"] != null)
									MapRenderer.SkyMap.Alpha_Inc_Speed = float.Parse(skymap["Alpha_Inc_Speed"].Value);
								if (skymap["Alpha_Dec_Time"] != null)
									MapRenderer.SkyMap.Alpha_Dec_Time = float.Parse(skymap["Alpha_Dec_Time"].Value);
								if (skymap["Alpha_Dec_Time_Extra"] != null)
									MapRenderer.SkyMap.Alpha_Dec_Time_Extra = float.Parse(skymap["Alpha_Dec_Time_Extra"].Value);
								if (skymap["Alpha_Dec_Speed"] != null)
									MapRenderer.SkyMap.Alpha_Dec_Speed = float.Parse(skymap["Alpha_Dec_Speed"].Value);
								if (skymap["Height"] != null)
									MapRenderer.SkyMap.Height = float.Parse(skymap["Height"].Value);
								if (skymap["Height_Extra"] != null)
									MapRenderer.SkyMap.Height_Extra = float.Parse(skymap["Height_Extra"].Value);
							}

							MapRenderer.SkyMap.CloudEffect = true;
						}
						else {
							MapRenderer.SkyMap = new CloudEffectSettings();
							MapRenderer.SkyMap.CloudEffect = false;
						}

						if (skymap["Star_Effect"] != null)
							MapRenderer.SkyMap.StarEffect = bool.Parse(skymap["Star_Effect"].Value);

						viewport.RenderOptions.RenderSkymapDetected = true;
					}
				}
			}

			_addStarCloudEffect();
			_addCloudEffect();
		}

		public override void Unload() {
			IsUnloaded = true;

			foreach (var texture in Textures) {
				TextureManager.UnloadTexture(texture.Resource, _request.Context);
			}

			foreach (var renderGroup in _groups) {
				renderGroup.Value.Effects.Clear();
				renderGroup.Value.RenderInfo.Unload();
			}

			foreach (var renderGroup in _skymapGroups) {
				renderGroup.Value.Effects.Clear();
				renderGroup.Value.RenderInfo.Unload();
			}
		}
	}
}
