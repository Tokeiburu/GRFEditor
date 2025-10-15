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
using Matrix4 = OpenTK.Matrix4;

namespace GRFEditor.OpenGL.MapRenderers {
	public class LubEffect {
		public Vector3 Dir1;
		public Vector3 Dir2;
		public Vector3 Gravity;
		public Vector3 Radius;
		public Vector4 Color;
		public Vector2 Rate;
		public Vector2 Scale = new Vector2(1);
		public Vector2 Size;
		public Vector2 Life;
		public string Texture;
		public float Speed;
		public int Srcmode;
		public int Destmode;
		public int Maxcount;
		public bool Zenable = true;
		public bool Eternity;
		public int Billboard_off;
		public Vector3 Rotate_angle;
		public Matrix4 ModelMatrix;
		public Texture Texture2D { get; set; }
		public float NextEmitTick;
		public Vector3 Pos { get; set; }
		public bool IsAnimated { get; set; }

		public List<Particle> Particles = new List<Particle>();
		public bool VboAssigned;
		public bool IsEnabled = true;
	}

	public class Particle {
		public Vector3 BasePosition;
		public Vector3 Position;
		public Vector3 BaseSpeed;
		public Vector3 Speed;
		public Vector3 Dir;
		public Vector2 Size;
		public float TickStart;
		public float Life;
		public float Duration;
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

	public class LubRenderer : Renderer {
		private readonly Gnd _gnd;
		private readonly List<LubEffect> _effects = new List<LubEffect>();
		private readonly Stopwatch _watch = new Stopwatch();
		private bool _verticesLoaded;
		private readonly RendererLoadRequest _request;
		private readonly Rsw _rsw;
		private readonly Dictionary<Texture, RenderInfoEffects> _groups = new Dictionary<Texture, RenderInfoEffects>();
		private readonly RenderInfoEffects _animatedGroup = new RenderInfoEffects();
		const int LubEffectVertexSize = 6;

		public LubRenderer(RendererLoadRequest request, Shader shader, Gnd gnd, Rsw rsw, byte[] lubData, OpenGLViewport viewport) {
			Shader = shader;
			_gnd = gnd;
			_rsw = rsw;
			_request = request;

			if (lubData != null && rsw.LubEffects.Count > 0) {
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

			_watch.Start();
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			foreach (var effect in _effects) {
				var texture = TextureManager.LoadTextureAsync(effect.Texture, Rsm.RsmTexturePath + effect.Texture.Replace("\\\\", "\\"), TextureRenderMode.RsmTexture, _request);
				Textures.Add(texture);
				effect.Texture2D = texture;

				effect.ModelMatrix = Matrix4.Identity;
				effect.ModelMatrix = GLHelper.Scale(ref effect.ModelMatrix, new Vector3(1, 1, -1));
				effect.ModelMatrix = GLHelper.Translate(ref effect.ModelMatrix, new Vector3(5 * _gnd.Width + effect.Pos.X, -effect.Pos.Y + 10f, -10 - 5 * _gnd.Height + effect.Pos.Z));

				if (effect.Billboard_off == 1) {
					effect.ModelMatrix = GLHelper.Rotate(ref effect.ModelMatrix, -GLHelper.ToRad(effect.Rotate_angle.Z), new Vector3(0, 0, 1));
					effect.ModelMatrix = GLHelper.Rotate(ref effect.ModelMatrix, GLHelper.ToRad(effect.Rotate_angle.Y), new Vector3(0, 1, 0));
					effect.ModelMatrix = GLHelper.Rotate(ref effect.ModelMatrix, -GLHelper.ToRad(effect.Rotate_angle.X), new Vector3(1, 0, 0));
				}

				// Gravity, why...!?
				effect.ModelMatrix = GLHelper.Translate(ref effect.ModelMatrix, new Vector3(0, 0, 1));

				if (effect.IsAnimated) {
					_animatedGroup.Effects.Add(effect);
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

			_animatedGroup.RenderInfo.RawVertices = new float[LubEffectVertexSize * 4 * _animatedGroup.Effects.Sum(p => p.Maxcount)];

			_verticesLoaded = true;
		}
		
		private bool _skipRender = false;
		private float _time;

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.LubEffect || viewport.RenderOptions.MinimapMode)
				return;

			if (viewport.RenderPass != RenderMode.LubTextures)
				return;

			if (!_verticesLoaded) {
				Load(viewport);
			}

			if (!_watch.IsRunning) {
				_watch.Start();
			}

			_skipRender = !_skipRender;

			Shader.Use();
			Shader.SetMatrix4("view", ref viewport.View);
			Shader.SetMatrix4("vp", ref viewport.ViewProjection);

			GL.Enable(EnableCap.Blend);
			GL.DepthMask(false);

			_time = _watch.ElapsedMilliseconds / 1000f;

			foreach (var renderGroup in _groups) {
				_renderGroup(viewport, renderGroup.Key, renderGroup.Value);
			}

			GL.Enable(EnableCap.DepthTest);

			//if (_animatedGroup.Effects.Count > 0) {
			//	_renderAnimatedGroup(viewport, Textures);
			//}

			GL.DepthMask(true);
			GL.Enable(EnableCap.DepthTest);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		private void _renderGroup(OpenGLViewport viewport, Texture key, RenderInfoEffects rie) {
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
					// Update effect particles
					for (int i = 0; i < effect.Particles.Count; i++) {
						var p = effect.Particles[i];
						
						if (!effect.Eternity && _time >= p.TickStart + p.Duration) {
							effect.Particles.RemoveAt(i);
							i--;
							continue;
						}

						var t = _time - p.TickStart;
						p.Position = p.BasePosition + p.Dir * effect.Speed * t;
						p.Position += effect.Gravity * effect.Speed * t * t * 0.5f;

						if (effect.Eternity)
							p.Alpha = effect.Color.W;
						else if (p.Duration > 1) {
							var tRemaining = p.TickStart + p.Duration - _time;

							if (t < 1) {
								p.Alpha = t;
							}
							else if (tRemaining < 1) {
								p.Alpha = tRemaining;
							}
							else {
								p.Alpha = 1;
							}
						}
					}

					if (_time >= effect.NextEmitTick && effect.Particles.Count < effect.Maxcount) {
						Particle p = new Particle();

						p.Position = p.BasePosition = new Vector3(
							TkRandom.Rand(-effect.Radius.X, -effect.Radius.X + 2 * Math.Abs(effect.Radius.X)),
							TkRandom.Rand(-effect.Radius.Y, -effect.Radius.Y + 2 * Math.Abs(effect.Radius.Y)),
							TkRandom.Rand(-effect.Radius.Z, -effect.Radius.Z + 2 * Math.Abs(effect.Radius.Z)));
						p.Dir = new Vector3(
							TkRandom.Rand(effect.Dir1.X, effect.Dir2.X),
							TkRandom.Rand(effect.Dir1.Y, effect.Dir2.Y),
							TkRandom.Rand(effect.Dir1.Z, effect.Dir2.Z));
						p.Duration = TkRandom.Rand(effect.Life.X, effect.Life.Y);
						p.TickStart = _time;
						p.Size = new Vector2(TkRandom.Rand(effect.Size.X, effect.Size.Y));
						effect.Particles.Add(p);
						effect.NextEmitTick = _time + 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
					}

					for (int i = 0; i < effect.Particles.Count; i++) {
						var p = effect.Particles[i];
						float alpha = p.Alpha;

						ri.RawVertices[vboIndex + 24 * i + 0] = -p.Size.X;
						ri.RawVertices[vboIndex + 24 * i + 1] = -p.Size.Y;
						ri.RawVertices[vboIndex + 24 * i + 2] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 3] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 4] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 5] = p.Position.Z;

						ri.RawVertices[vboIndex + 24 * i + 6] = -p.Size.X;
						ri.RawVertices[vboIndex + 24 * i + 7] = p.Size.Y;
						ri.RawVertices[vboIndex + 24 * i + 8] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 9] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 10] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 11] = p.Position.Z;

						ri.RawVertices[vboIndex + 24 * i + 12] = p.Size.X;
						ri.RawVertices[vboIndex + 24 * i + 13] = p.Size.Y;
						ri.RawVertices[vboIndex + 24 * i + 14] = alpha;
						ri.RawVertices[vboIndex + 24 * i + 15] = p.Position.X;
						ri.RawVertices[vboIndex + 24 * i + 16] = p.Position.Y;
						ri.RawVertices[vboIndex + 24 * i + 17] = p.Position.Z;

						ri.RawVertices[vboIndex + 24 * i + 18] = p.Size.X;
						ri.RawVertices[vboIndex + 24 * i + 19] = -p.Size.Y;
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
			Vector2? prevScale = null;

			foreach (var effect in rie.Effects) {
				if (prevZenable == null || prevZenable != effect.Zenable) {
					if (effect.Zenable)
						GL.Enable(EnableCap.DepthTest);
					else
						GL.Disable(EnableCap.DepthTest);
					prevZenable = effect.Zenable;
				}

				if (prevColor == null || prevColor != effect.Color) {
					Shader.SetVector4("color", ref effect.Color);
					prevColor = effect.Color;
				}

				if (prevBillboard == null || prevBillboard != effect.Billboard_off) {
					Shader.SetFloat("billboard_off", effect.Billboard_off);
					prevBillboard = effect.Billboard_off;
				}

				if (prevScale == null || prevScale != effect.Scale) {
					Shader.SetVector2("scale", ref effect.Scale);
					prevScale = effect.Scale;
				}

				Shader.SetMatrix4("m", ref effect.ModelMatrix);

				if (prevBlend == null || prevBlend != (effect.Srcmode << 16 | effect.Destmode)) {
					//GL.BlendEquation(BlendEquationMode.FuncAdd);
					GL.BlendFunc(GLHelper.GetOpenGlBlendFromDirectXSrc(effect.Srcmode), GLHelper.GetOpenGlBlendFromDirectXDest(effect.Destmode));

					//GL.BlendFuncSeparate(
					//	GLHelper.GetOpenGlBlendFromDirectXSrc2(effect.Srcmode),
					//	GLHelper.GetOpenGlBlendFromDirectXDest2(effect.Destmode),
					//	BlendingFactorSrc.One,
					//	BlendingFactorDest.OneMinusSrcAlpha
					//);

					prevBlend = (effect.Srcmode << 16 | effect.Destmode);
				}

				GL.DrawArrays(PrimitiveType.Quads, vboIndex, effect.Particles.Count * 4);
#if DEBUG
				viewport.Stats.DrawArrays_Calls++;
				viewport.Stats.DrawArrays_Calls_VertexLength += effect.Particles.Count * 4;
#endif
				vboIndex += effect.Maxcount * 4;
			}
		}

		private void _renderAnimatedGroup(OpenGLViewport viewport, List<Texture> textures, float interval) {
			//int vboIndex = 0;
			//var rie = _animatedGroup;
			//var ri = rie.RenderInfo;
			//
			//if (ri.Vao == 0) {
			//	ri.CreateVao();
			//	ri.Vbo = new Vbo();
			//	ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.DynamicDraw, LubEffectVertexSize);
			//	GL.EnableVertexAttribArray(0);
			//	GL.EnableVertexAttribArray(1);
			//	GL.EnableVertexAttribArray(2);
			//	GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 0);
			//	GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 2 * sizeof(float));
			//	GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, LubEffectVertexSize * sizeof(float), 3 * sizeof(float));
			//}
			//
			//foreach (var effect in rie.Effects) {
			//	effect.EmitTime -= interval;
			//
			//	for (int i = 0; i < effect.Particles.Count; i++) {
			//		var p = effect.Particles[i];
			//
			//		p.Life -= interval;
			//		p.TextureLife -= interval;
			//
			//		if (p.Life < 0) {
			//			effect.Particles.RemoveAt(i);
			//			i--;
			//			continue;
			//		}
			//
			//		if (p.TextureLife < 0) {
			//			p.TextureLife += 0.2f;
			//			p.TextureIndex++;
			//
			//			if (p.TextureType == 0 && p.TextureIndex > 3)
			//				p.TextureIndex = 0;
			//			else if (p.TextureType == 1 && p.TextureIndex > 6)
			//				p.TextureIndex = 4;
			//		}
			//
			//		p.Position += p.Dir * interval * effect.Speed + p.Speed * interval;
			//		p.Speed += effect.Speed * effect.Gravity * interval;
			//
			//		if (p.StartLife > 1) {
			//			float t = p.StartLife - p.Life;
			//			
			//			if (t < 1) {
			//				p.Alpha = t;
			//			}
			//			else if (p.Life < 1) {
			//				p.Alpha = p.Life;
			//			}
			//
			//			p.Alpha = (float)GLHelper.Clamp(0, 1, p.Alpha);
			//		}
			//	}
			//
			//	if (effect.EmitTime < 0 && effect.Particles.Count < effect.Maxcount) {
			//		Particle p = new Particle();
			//
			//		p.Position = new Vector3(
			//			TkRandom.Rand(-effect.Radius.X, -effect.Radius.X + 2 * Math.Abs(effect.Radius.X)),
			//			TkRandom.Rand(-effect.Radius.Y, -effect.Radius.Y + 2 * Math.Abs(effect.Radius.Y)),
			//			TkRandom.Rand(-effect.Radius.Z, -effect.Radius.Z + 2 * Math.Abs(effect.Radius.Z)));
			//		p.Dir = new Vector3(
			//			TkRandom.Rand(effect.Dir1.X, effect.Dir2.X),
			//			TkRandom.Rand(effect.Dir1.Y, effect.Dir2.Y),
			//			TkRandom.Rand(effect.Dir1.Z, effect.Dir2.Z));
			//		p.Life = TkRandom.Rand(effect.Life.X, effect.Life.Y);
			//		p.StartLife = p.Life;
			//		p.Size = TkRandom.Rand(effect.Size.X, effect.Size.Y);
			//		effect.Particles.Add(p);
			//		effect.EmitTime = 1f / TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
			//		p.TextureType = TkRandom.Next() % 2;
			//		p.TextureLife = 0.2f;
			//
			//		if (p.TextureType == 0)
			//			p.TextureIndex = TkRandom.Next() % 4;
			//		else
			//			p.TextureIndex = (TkRandom.Next() % 3) + 4;
			//	}
			//
			//	for (int i = 0; i < effect.Particles.Count; i++) {
			//		var p = effect.Particles[i];
			//		float alpha = p.Alpha;
			//
			//		ri.RawVertices[vboIndex + 24 * i + 0] = -p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 1] = -p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 2] = alpha;
			//		ri.RawVertices[vboIndex + 24 * i + 3] = p.Position.X;
			//		ri.RawVertices[vboIndex + 24 * i + 4] = p.Position.Y;
			//		ri.RawVertices[vboIndex + 24 * i + 5] = p.Position.Z;
			//
			//		ri.RawVertices[vboIndex + 24 * i + 6] = -p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 7] = p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 8] = alpha;
			//		ri.RawVertices[vboIndex + 24 * i + 9] = p.Position.X;
			//		ri.RawVertices[vboIndex + 24 * i + 10] = p.Position.Y;
			//		ri.RawVertices[vboIndex + 24 * i + 11] = p.Position.Z;
			//
			//		ri.RawVertices[vboIndex + 24 * i + 12] = p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 13] = p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 14] = alpha;
			//		ri.RawVertices[vboIndex + 24 * i + 15] = p.Position.X;
			//		ri.RawVertices[vboIndex + 24 * i + 16] = p.Position.Y;
			//		ri.RawVertices[vboIndex + 24 * i + 17] = p.Position.Z;
			//
			//		ri.RawVertices[vboIndex + 24 * i + 18] = p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 19] = -p.Size;
			//		ri.RawVertices[vboIndex + 24 * i + 20] = alpha;
			//		ri.RawVertices[vboIndex + 24 * i + 21] = p.Position.X;
			//		ri.RawVertices[vboIndex + 24 * i + 22] = p.Position.Y;
			//		ri.RawVertices[vboIndex + 24 * i + 23] = p.Position.Z;
			//	}
			//
			//	vboIndex += LubEffectVertexSize * effect.Maxcount * 4;
			//}
			//
			//vboIndex = 0;
			//
			//ri.BindVao();
			//ri.Vbo.SetData(ri.RawVertices, BufferUsageHint.DynamicDraw, LubEffectVertexSize);
			//
			//foreach (var effect in rie.Effects) {
			//	Shader.SetMatrix4("m", ref effect.ModelMatrix);
			//	Shader.SetVector4("color", ref effect.Color);
			//	Shader.SetFloat("billboard_off", effect.Billboard_off);
			//	GL.BlendFunc(GLHelper.GetOpenGlBlendFromDirectXSrc(effect.Srcmode), GLHelper.GetOpenGlBlendFromDirectXDest(effect.Destmode));
			//	
			//	foreach (var particle in effect.Particles) {
			//		textures[particle.TextureIndex].Bind();
			//		GL.DrawArrays(PrimitiveType.Quads, vboIndex, 4);
#if DEBUG	//
			//		viewport.Stats.DrawArrays_Calls++;
			//		viewport.Stats.DrawArrays_Calls_VertexLength += 4;
#endif		//
			//		vboIndex += 4;
			//	}
			//
			//	vboIndex += (effect.Maxcount - effect.Particles.Count) * 4;
			//}
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
							effect.Color = new Vector4(pv[0].Cast<float>() / 255f, pv[1].Cast<float>() / 255f, pv[2].Cast<float>() / 255f, pv[3].Cast<float>() / 255f);
							//effect.Color = new Vector4(pv[0].Cast<float>() / 255f, pv[1].Cast<float>() / 255f, pv[2].Cast<float>() / 255f, 1f);
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
						case "eternity":
							effect.Eternity = pv[0].Cast<int>() != 0;
							break;
						case "scale":
							effect.Scale = new Vector2(pv[0].Cast<float>(), pv[1].Cast<float>());
							break;
					}
				}

				if (effect.Billboard_off != 0) {
					effect.Dir1.Z *= -1;
					effect.Dir2.Z *= -1;
					effect.Gravity.Z *= -1;
				}

				//effect.NextEmitTick = TkRandom.Rand(effect.Rate.X, effect.Rate.Y);
				effect.NextEmitTick = 0;
				effect.IsAnimated = isAnimated;
			}
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
		}
	}
}
