using GRF.FileFormats.LubFormat;
using GRF.FileFormats.RswFormat;
using GRF.Image;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using Lua;
using Lua.Structure;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Boolean = System.Boolean;

namespace GRFEditor.OpenGL.MapRenderers {
	public class SkyMapRenderer : Renderer {
		private readonly Gnd _gnd;
		private readonly Stopwatch _watch = new Stopwatch();
		private readonly RendererLoadRequest _request;
		public bool IsValidSkyMap = false;

		private Texture _cloudTex;
		private Texture _starTex;
		private Texture _fogTex;
		private int _uboHandle;
		private float _time;

		public SkymapSettings SkyMap = new SkymapSettings();

		// Avoid decompiling skymap lub file more than once...
		private static SimplifiedLuaElement _loadedSkyMapLub;
		private static bool _parsedSkyMapLub;

		public SkyMapRenderer(RendererLoadRequest request, Shader shader, Gnd gnd, OpenGLViewport viewport) {
			Shader = shader;
			_gnd = gnd;
			_request = request;

			//var template = SkyMapEffectTemplates.GetTemplate(-1);
			//template.Num = 10000;
			//template.ShaderParameters.Height = -90;
			//SkyMap.SkyEffects.Add(template);
			//IsValidSkyMap = true;
			//_watch.Start();
			//return;

			if (viewport.OpenGLVersion < 4.2)
				return;

			SimplifiedLuaElement skyMapLub;

			if (_parsedSkyMapLub) {
				skyMapLub = _loadedSkyMapLub;
			}
			else {
				skyMapLub = _loadSkyMapLub();
				_loadedSkyMapLub = skyMapLub;
				_parsedSkyMapLub = true;
			}

			if (skyMapLub == null)
				return;

			_parseSkyMapData(viewport, skyMapLub, Path.GetFileName(_request.Resource) + ".rsw");
			_watch.Start();
		}

		private void _parseSkyMapData(OpenGLViewport viewport, SimplifiedLuaElement skyMapLub, string mapName) {
			if (skyMapLub.KeyValues.ContainsKey(mapName)) {
				var skymap = skyMapLub[mapName];
				if (skymap["BG_Color"] != null) {
					SkyMap.Bg_Color = new Vector4(skymap["BG_Color"][0].Cast<float>() / 255f, skymap["BG_Color"][1].Cast<float>() / 255f, skymap["BG_Color"][2].Cast<float>() / 255f, 1f);
				}
				else {
					SkyMap.Bg_Color = new Vector4(0, 0, 0, 1);
				}

				if (skymap["Star_Effect"] != null) {
					SkyMap.Star_Effect = Boolean.Parse(skymap["Star_Effect"].Value);

					if (SkyMap.Star_Effect) {
						SkyMap.SkyEffects.Add(SkyMapEffectTemplates.GetStarTemplate());
					}
				}

				if (skymap["BG_Fog"] != null)
					SkyMap.BG_Fog = Boolean.Parse(skymap["BG_Fog"].Value);

				if (skymap["Old_Cloud_Effect"] != null) {
					foreach (var entry in skymap["Old_Cloud_Effect"].Elements) {
						var cloudType = Int32.Parse(entry.Value);

						if (cloudType == 11) {
							var effect = new SkyEffect();
							effect.OldCloudEffect = cloudType;
							effect.SubEffects.Add(SkyMapEffectTemplates.GetTemplate(11, 0));
							effect.SubEffects.Add(SkyMapEffectTemplates.GetTemplate(11, 1));
							effect.SubEffects.Add(SkyMapEffectTemplates.GetTemplate(11, 2));
							effect.SubEffects.Add(SkyMapEffectTemplates.GetTemplate(11, 3));
						}
						else {
							var effect = SkyMapEffectTemplates.GetTemplate(cloudType);
							if (effect != null)
								SkyMap.SkyEffects.Add(effect);
						}
					}
				}

				if (skymap["Cloud_Effect"] != null) {
					var cloudEffects = skymap["Cloud_Effect"];

					foreach (var cloudEffect in cloudEffects.KeyValues.Values) {
						SkyEffect effect = new SkyEffect();

						if (cloudEffect["Num"] != null)
							effect.Num = int.Parse(cloudEffect["Num"].Value);
						if (cloudEffect["Color"] != null)
							effect.Color = new Vector3(cloudEffect["Color"][0].Cast<float>() / 255f, cloudEffect["Color"][1].Cast<float>() / 255f, cloudEffect["Color"][2].Cast<float>() / 255f);
						if (cloudEffect["Size"] != null)
							effect.ShaderParameters.Size = FormatConverters.SingleConverter(cloudEffect["Size"].Value);
						if (cloudEffect["Size_Extra"] != null)
							effect.ShaderParameters.Size_Extra = FormatConverters.SingleConverter(cloudEffect["Size_Extra"].Value);
						if (cloudEffect["Expand_Rate"] != null)
							effect.ShaderParameters.Expand_Rate = FormatConverters.SingleConverter(cloudEffect["Expand_Rate"].Value);
						if (cloudEffect["Alpha_Inc_Time"] != null)
							effect.ShaderParameters.Alpha_Inc_Time = FormatConverters.SingleConverter(cloudEffect["Alpha_Inc_Time"].Value);
						if (cloudEffect["Alpha_Inc_Time_Extra"] != null)
							// There is a bug with the lub file; it uses Dec_Time_Extra instead of Inc_...
							effect.ShaderParameters.Alpha_Inc_Time_Extra = FormatConverters.SingleConverter(cloudEffect["Alpha_Inc_Time_Extra"].Value);
						if (cloudEffect["Alpha_Inc_Speed"] != null)
							effect.ShaderParameters.Alpha_Inc_Speed = FormatConverters.SingleConverter(cloudEffect["Alpha_Inc_Speed"].Value);
						if (cloudEffect["Alpha_Dec_Time"] != null)
							effect.ShaderParameters.Alpha_Dec_Time = FormatConverters.SingleConverter(cloudEffect["Alpha_Dec_Time"].Value);
						if (cloudEffect["Alpha_Dec_Time_Extra"] != null)
							effect.ShaderParameters.Alpha_Dec_Time_Extra = FormatConverters.SingleConverter(cloudEffect["Alpha_Dec_Time_Extra"].Value);
						if (cloudEffect["Alpha_Dec_Speed"] != null)
							effect.ShaderParameters.Alpha_Dec_Speed = FormatConverters.SingleConverter(cloudEffect["Alpha_Dec_Speed"].Value);
						if (cloudEffect["Height"] != null)
							effect.ShaderParameters.Height = FormatConverters.SingleConverter(cloudEffect["Height"].Value);
						if (cloudEffect["Height_Extra"] != null)
							effect.ShaderParameters.Height_Extra = FormatConverters.SingleConverter(cloudEffect["Height_Extra"].Value);

						SkyMap.SkyEffects.Add(effect);
					}
				}

				IsValidSkyMap = true;
			}
		}

		private SimplifiedLuaElement _loadSkyMapLub() {
			try {
				var mapskydata = ResourceManager.GetData(@"data\luafiles514\lua files\mapskydata\mapskydata.lub");

				if (mapskydata == null) {
					return null;
				}

				if (Methods.ByteArrayCompare(mapskydata, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0)) {
					Lub lub = new Lub(mapskydata);
					var text = lub.Decompile();
					mapskydata = EncodingService.DisplayEncoding.GetBytes(text);
				}

				using (LuaReader reader = new LuaReader(new MemoryStream(mapskydata))) {
					return reader.ReadSimplified()["MapSkyData"];
				}
			}
			catch {
				return null;
			}
		}

		public void UnloadSkyEffect(SkyEffect skyEffect) {
			if (skyEffect.SubEffects.Count > 0) {
				foreach (var subEffect in skyEffect.SubEffects) {
					UnloadSkyEffect(subEffect);
				}
			}

			if (skyEffect.IsUnloaded)
				return;

			if (skyEffect.RenderInfo != null)
				skyEffect.RenderInfo.Unload();

			skyEffect.Particles = null;
			skyEffect.IsUnloaded = true;
			skyEffect.IsLoaded = false;
		}

		public void LoadSkyEffect(SkyEffect skyEffect) {
			if (skyEffect.OldCloudEffect == 11 && skyEffect.SubEffects.Count > 0) {
				foreach (var subEffect in skyEffect.SubEffects) {
					LoadSkyEffect(subEffect);
				}

				return;
			}

			if (skyEffect.IsLoaded)
				return;

			List<string> textures = new List<string>();

			switch (skyEffect.TextureType) {
				case CloudEffectTextureType.Cloud:
					textures.Add(@"effect\cloud1.tga");
					textures.Add(@"effect\cloud2.tga");
					textures.Add(@"effect\cloud3.tga");
					textures.Add(@"effect\cloud4.tga");
					_loadSkyTexture(textures, ref _cloudTex);
					skyEffect.Atlas = _cloudTex;
					break;
				case CloudEffectTextureType.Star:
					textures.Add(@"effect\star01.bmp");
					textures.Add(@"effect\star02.bmp");
					textures.Add(@"effect\star03.bmp");
					textures.Add(@"effect\star04.bmp");
					textures.Add(@"effect\star05.bmp");
					textures.Add(@"effect\star06.bmp");
					_loadSkyTexture(textures, ref _starTex);
					skyEffect.Atlas = _starTex;
					break;
				case CloudEffectTextureType.Fog:
					textures.Add(@"effect\fog1.tga");
					textures.Add(@"effect\fog2.tga");
					textures.Add(@"effect\fog3.tga");
					_loadSkyTexture(textures, ref _fogTex);
					skyEffect.Atlas = _fogTex;
					break;
			}

			if (skyEffect.Num < 0) {
				skyEffect.Num = (int)(skyEffect.NumPerSquared * _gnd.Header.Width * _gnd.Header.Height * 100f);
			}

			skyEffect.Num = Math.Min(10000, skyEffect.Num);

			skyEffect.ShaderParameters.UVSplit = textures.Count;
			skyEffect.SplitCount = textures.Count;

			RenderInfo ri = new RenderInfo();

			ri.Vertices = new List<Vertex>();
			ri.Vertices.Add(new Vertex(new Vector3(-1f, -1f, 0.0f), new Vector2(0, 0)));
			ri.Vertices.Add(new Vertex(new Vector3(1f, -1f, 0.0f), new Vector2(1, 0)));
			ri.Vertices.Add(new Vertex(new Vector3(-1f, 1f, 0.0f), new Vector2(0, 1)));
			ri.Vertices.Add(new Vertex(new Vector3(1f, 1f, 0.0f), new Vector2(1, 1)));

			ri.CreateVao();
			ri.Vbo = new Vbo();
			ri.Vbo.SetData(ri.Vertices, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float));
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

			uint[] quadIndices = { 0, 1, 2, 2, 3, 1 };
			ri.Ebo = new Ebo();
			ri.Ebo.SetData(quadIndices, BufferUsageHint.StaticDraw);

			ri.InstanceVbo = new Vbo();
			ri.InstanceVbo.Bind();
			GL.BufferData(BufferTarget.ArrayBuffer, skyEffect.Num * Marshal.SizeOf<ParticleInstance>(), IntPtr.Zero, BufferUsageHint.StreamDraw);

			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);
			GL.EnableVertexAttribArray(4);
			GL.EnableVertexAttribArray(5);
			GL.EnableVertexAttribArray(6);
			GL.EnableVertexAttribArray(7);
			GL.EnableVertexAttribArray(8);
			GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 0 * sizeof(float));
			GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 3 * sizeof(float));
			GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 4 * sizeof(float));
			GL.VertexAttribPointer(5, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 5 * sizeof(float));
			GL.VertexAttribPointer(6, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 6 * sizeof(float));
			GL.VertexAttribPointer(7, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 7 * sizeof(float));
			GL.VertexAttribPointer(8, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<ParticleInstance>(), 8 * sizeof(float));
			GL.VertexAttribDivisor(2, 1);
			GL.VertexAttribDivisor(3, 1);
			GL.VertexAttribDivisor(4, 1);
			GL.VertexAttribDivisor(5, 1);
			GL.VertexAttribDivisor(6, 1);
			GL.VertexAttribDivisor(7, 1);
			GL.VertexAttribDivisor(8, 1);

			skyEffect.Particles = new ParticleInstance[skyEffect.Num];
			skyEffect.RenderInfo = ri;

			for (int i = 0; i < skyEffect.Particles.Length; i++) {
				skyEffect.Particles[i].ExpandDelay = 10f * TkRandom.NextFloat();
				_updateParticle(skyEffect, ref skyEffect.Particles[i], i);
			}

			GL.BufferData(BufferTarget.ArrayBuffer, Marshal.SizeOf<ParticleInstance>() * skyEffect.Particles.Length, skyEffect.Particles, BufferUsageHint.StreamDraw);
			skyEffect.IsLoaded = true;
			skyEffect.IsUnloaded = false;
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded || !IsValidSkyMap || !viewport.RenderOptions.RenderSkymapFeature)
				return;

			Shader.Use();

			_time = _watch.ElapsedMilliseconds / 1000f;

			if (_uboHandle == 0) {
				_uboHandle = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);
				GL.BufferData(BufferTarget.UniformBuffer, Marshal.SizeOf<ParticleParams>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
				GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _uboHandle);
			}

			for (int k = 0; k < SkyMap.SkyEffects.Count; k++) {
				LoadSkyEffect(SkyMap.SkyEffects[k]);
			}

			IsLoaded = true;
		}

		private void _loadSkyTexture(List<string> textures, ref Texture tex) {
			if (tex != null)
				return;

			List<GrfImage> images = new List<GrfImage>();
			foreach (var texture in textures) {
				GrfImage img = new GrfImage(ResourceManager.GetData(Rsm.RsmTexturePath + texture));

				if (img.GrfImageType == GrfImageType.Indexed8) {
					img.Convert(GrfImageType.Bgr24);
				}

				images.Add(img);
			}

			int width = images.Max(p => p.Width);
			int height = images.Sum(p => p.Height);

			GrfImage atlas = new GrfImage(new byte[width * height * images[0].GetBpp()], width, height, images[0].GrfImageType);

			int offsetY = 0;

			foreach (var image in images) {
				System.Buffer.BlockCopy(image.Pixels, 0, atlas.Pixels, offsetY, image.Pixels.Length);
				offsetY += image.Pixels.Length;
			}

			// Apply dithering
			{
				int ditherDivider = 8;
				int ditherDividerShift = 3;
				float ditherMultiplier = 8.25f;

				if (textures[0].IsExtension(".png", ".tga")) {
					ditherDividerShift = 4;
					ditherDivider = 16;
					ditherMultiplier = 17;
				}

				byte rT = (byte)(Math.Ceiling(ditherDivider / ditherMultiplier * 255) - 1);
				byte gT = (byte)(255 - rT);
				byte bT = rT;

				atlas.DitherAndChangePinkToBlack(rT, gT, bT, ditherDividerShift, ditherMultiplier);
			}

			tex = new Texture("_APP_SKYMAP", atlas);
		}

		private void _updateParticle(SkyEffect skyEffect, ref ParticleInstance particle, int i) {
			particle.PositionX = TkRandom.Rand(0, _gnd.Header.Width * 10);
			particle.PositionY = -skyEffect.ShaderParameters.Height;
			particle.PositionZ = TkRandom.Rand(0, _gnd.Header.Height * 10);

			if (skyEffect.AboveGroundOnly) {
				int x = (int)(particle.PositionX / 10f);
				int y = (int)(particle.PositionZ / 10f);

				particle.PositionY = -_gnd.Cubes[x + (_gnd.Height - y - 1) * _gnd.Width][0] + skyEffect.ShaderParameters.Size;
			}

			particle.Seed = TkRandom.NextFloat();

			float alphaIncTime = skyEffect.ShaderParameters.Alpha_Inc_Time + particle.Seed * skyEffect.ShaderParameters.Alpha_Inc_Time_Extra;
			float duration = alphaIncTime / 100.0f;
			float alpha = Math.Min(2.55f, duration * skyEffect.ShaderParameters.Alpha_Inc_Speed);
			float startDecreaseTime = skyEffect.ShaderParameters.Alpha_Dec_Time / 100.0f + TkRandom.Rand(0, skyEffect.ShaderParameters.Alpha_Dec_Time_Extra / 100.0f);

			if (startDecreaseTime > duration)
				duration = startDecreaseTime;

			if (skyEffect.ShaderParameters.Alpha_Dec_Speed <= 0) {
				duration = float.MaxValue;
			}
			else {
				duration += alpha / skyEffect.ShaderParameters.Alpha_Dec_Speed;
			}

			particle.LifeEnd = _time + duration * 1.6f;
			particle.AlphaDecTime = startDecreaseTime;
			particle.LifeStart = _time / 1.6f;

			if (skyEffect.TextureType == CloudEffectTextureType.Star) {
				if (skyEffect.OldCloudEffect == 15) {
					particle.UVStart = 0;
				}
				else {
					particle.UVStart = (int)(skyEffect.ShaderParameters.UVSplit * TkRandom.NextFloat());
				}
			}
			else {
				particle.UVStart = (i % skyEffect.SplitCount) / skyEffect.ShaderParameters.UVSplit;
			}
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.RenderSkymapFeature || !IsValidSkyMap || SkyMap == null)
				return;
	
			if (viewport.RenderPass != RenderMode.TransparentTextures)
				return;
	
			if (!IsLoaded) {
				Load(viewport);
			}

			if (!_watch.IsRunning) {
				_watch.Start();
			}

			_time = _watch.ElapsedMilliseconds / 1000f;
			
			Shader.Use();
			Shader.SetMatrix4("view", ref viewport.View);
			Shader.SetMatrix4("vp", ref viewport.ViewProjection);

			Shader.SetFloat("aGndWidth", _gnd.Header.Width * 10);
			Shader.SetFloat("aGndHeight", _gnd.Header.Height * 10);

			// Gravity time is just built different...
			Shader.SetFloat("uTime", _time / 1.6f);

			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask(false);
			
			foreach (var skyEffect in SkyMap.SkyEffects) {
				_renderSub(viewport, skyEffect);
			}

			GL.DepthMask(true);
			GL.Enable(EnableCap.DepthTest);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		private void _renderSub(OpenGLViewport viewport, SkyEffect skyEffect) {
			if (skyEffect.IsUnloaded || !skyEffect.IsEnabled || skyEffect.IsDeleted)
				return;

			if (skyEffect.OldCloudEffect == 11 && skyEffect.SubEffects.Count > 0) {
				foreach (var subEffect in skyEffect.SubEffects) {
					_renderSub(viewport, subEffect);
				}

				return;
			}

			if (skyEffect.IsModified) {
				UnloadSkyEffect(skyEffect);
				skyEffect.IsModified = false;
			}

			if (!skyEffect.IsLoaded) {
				LoadSkyEffect(skyEffect);
			}

			GL.BlendFunc(skyEffect.SrcMode, skyEffect.DstMode);

			Shader.SetVector4("color", new Vector4(skyEffect.Color, 1));

			GL.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);
			GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, Marshal.SizeOf<ParticleParams>(), ref skyEffect.ShaderParameters);

			if (skyEffect.ShaderParameters.DirMode == 2)
				Shader.SetVector3("aForcedDir", ref skyEffect.ShaderParameters.ForcedDir);

			skyEffect.Atlas.Bind();

			var ri = skyEffect.RenderInfo;
			//var texture = cloudEffect.Textures[k];
			var particles = skyEffect.Particles;

			ri.BindVao();
			ri.InstanceVbo.Bind();

			List<int> updatedIndices = new List<int>();

			for (int i = 0; i < particles.Length; i++) {
				if (_time > particles[i].LifeEnd) {
					_updateParticle(skyEffect, ref particles[i], i);
					updatedIndices.Add(i);
				}
			}

			if (updatedIndices.Count > 0) {
				var particleSize = Marshal.SizeOf<ParticleInstance>();
				var tempBuffer = new ParticleInstance[updatedIndices.Count];
				for (int i = 0; i < updatedIndices.Count; i++)
					tempBuffer[i] = particles[updatedIndices[i]];
			
				GL.BindBuffer(BufferTarget.CopyWriteBuffer, ri.InstanceVbo.Id);
				IntPtr basePtr = GL.MapBufferRange(
					BufferTarget.CopyWriteBuffer, IntPtr.Zero,
					(IntPtr)(particles.Length * particleSize),
					BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit
				);

				if (basePtr != IntPtr.Zero) {
					GRF.Graphics.Helper.Copy(basePtr, updatedIndices, particleSize, tempBuffer);
					GL.UnmapBuffer(BufferTarget.CopyWriteBuffer);
				}
			}

			ri.Ebo.Bind();
			GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero, particles.Length);
#if DEBUG
			viewport.Stats.DrawArrays_Calls++;
			viewport.Stats.DrawArrays_Calls_VertexLength += particles.Length;
#endif
		}

		public override void Unload() {
			IsUnloaded = true;

			if (_cloudTex != null)
				_cloudTex.Unload();

			if (_starTex != null)
				_starTex.Unload();

			if (_fogTex != null)
				_fogTex.Unload();

			if (_uboHandle > 0)
				GL.DeleteBuffer(_uboHandle);
			
			foreach (var texture in Textures) {
				TextureManager.UnloadTexture(texture.Resource, _request.Context);
			}

			if (SkyMap != null) {
				foreach (var skyEffect in SkyMap.SkyEffects) {
					UnloadSkyEffect(skyEffect);
				}
			}
		}
	}
}
