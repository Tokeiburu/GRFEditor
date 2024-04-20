using System;
using System.Collections.Generic;
using System.Diagnostics;
using GRF.FileFormats.RswFormat;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapRenderers {
	public class WaterRenderer : Renderer {
		private readonly RendererLoadRequest _request;
		private readonly Gnd _gnd;
		private readonly WaterData _water;
		private readonly RenderInfo _ri = new RenderInfo();
		private readonly Stopwatch _watch = new Stopwatch();

		public WaterRenderer(RendererLoadRequest request, Shader shader, Rsw rsw, Gnd gnd) {
			Shader = shader;
			_request = request;
			_gnd = gnd;

			if (_gnd.Water != null && _gnd.Header.Version >= 1.8) {
				_water = _gnd.Water;
			}
			else if (rsw.Header.Version < 2.6) {
				_water = new WaterData(rsw.Water);
			}
		}

		private bool _verticesLoaded;
		private bool _isMinimap;

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded || _water == null)
				return;

			if (_request.CancelRequired())
				return;

			int perWidth = _gnd.Width / _water.WaterSplitWidth;
			int perHeight = _gnd.Height / _water.WaterSplitHeight;
			_ri.Vertices = new List<Vertex>();

			for (int yy = _water.WaterSplitHeight - 1; yy >= 0; yy--) {
				for (int xx = 0; xx < _water.WaterSplitWidth; xx++) {
					var water = _water.Zones[(_water.WaterSplitHeight - yy - 1) * _water.WaterSplitWidth + xx];

					for (int i = 0; i < 32; i++) {
						string texture = String.Format(@"data\texture\¿öÅÍ\water{0}{1:00}{2}", water.Type, i, ".jpg");
						Textures.Add(TextureManager.LoadTextureAsync(texture, texture, TextureRenderMode.RsmTexture, _request));
					}

					float waveHeight = water.Level - water.WaveHeight;

					int xmax = perWidth * (xx + 1);

					if (xx == _water.WaterSplitWidth - 1)
						xmax = _gnd.Width;

					int ymax = perHeight * (yy + 1);

					if (ymax == _water.WaterSplitHeight - 1)
						ymax = _gnd.Height;

					List<Vertex> verts = new List<Vertex>();
					for (int x = perWidth * xx; x < xmax; x++) {
						for (int y = perHeight * yy; y < ymax; y++) {
							Cube cube = _gnd[x, (_gnd.Height - y - 1)];

							if (cube.TileUp == -1)
								continue;

							if (cube[0] <= waveHeight && cube[1] <= waveHeight && cube[2] <= waveHeight && cube[3] <= waveHeight)
								continue;

							verts.Add(new Vertex(new Vector3(10 * x, 0, 10 * (y + 1)), new Vector2((x % 4) * 0.25f + 0.00f, (y % 4) * 0.25f + 0.00f)));
							verts.Add(new Vertex(new Vector3(10 * (x + 1), 0, 10 * (y + 1)), new Vector2((x % 4) * 0.25f + 0.25f, (y % 4) * 0.25f + 0.00f)));
							verts.Add(new Vertex(new Vector3(10 * (x + 1), 0, 10 * (y + 2)), new Vector2((x % 4) * 0.25f + 0.25f, (y % 4) * 0.25f + 0.25f)));
							verts.Add(new Vertex(new Vector3(10 * x, 0, 10 * (y + 2)), new Vector2((x % 4) * 0.25f + 0.00f, (y % 4) * 0.25f + 0.25f)));
						}
					}

					_ri.Indices.Add(new VboIndex { Begin = _ri.Vertices.Count, Texture = 0, Count = verts.Count });
					_ri.Vertices.AddRange(verts);
				}
			}

			if (_request.CancelRequired())
				return;

			_ri.RawVertices = Vbo.Vertex2Data(_ri.Vertices);
			_verticesLoaded = true;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || _water == null || !viewport.RenderOptions.Water)
				return;

			if (!_verticesLoaded) {
				Load(viewport);
			}

			if (!_ri.VaoCreated()) {
				if (_request.CancelRequired())
					return;

				_ri.CreateVao();
				_ri.Vbo = new Vbo();
				_ri.Vbo.SetData(_ri.RawVertices, BufferUsageHint.StaticDraw, 5);
				_ri.Vertices = null;

				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

				Shader.Use();

				if (_water.Zones.Count == 1) {
					Shader.SetFloat("waterHeight", -_water.Zones[0].Level);
					Shader.SetFloat("amplitude", _water.Zones[0].WaveHeight);
					Shader.SetFloat("waveSpeed", _water.Zones[0].WaveSpeed);
					Shader.SetFloat("wavePitch", _water.Zones[0].WavePitch);
				}

				Shader.SetMatrix4("modelMatrix", Matrix4.Identity);
				_watch.Start();
			}

			float time = _watch.ElapsedMilliseconds / 1000f;

			Shader.Use();
			Shader.SetMatrix4("projectionMatrix", viewport.Projection);
			Shader.SetMatrix4("viewMatrix", viewport.View);
			Shader.SetFloat("time", time);

			GL.DepthMask(false);

			_ri.BindVao();

			if (viewport.RenderOptions.MinimapMode && viewport.RenderOptions.MinimapWaterOverride) {
				_isMinimap = true;
			}
			else {
				if (_isMinimap) {
					Shader.SetInt("colorMode", 0);

					if (_water.Zones.Count == 1) {
						Shader.SetFloat("waterHeight", -_water.Zones[0].Level);
						Shader.SetFloat("amplitude", _water.Zones[0].WaveHeight);
						Shader.SetFloat("waveSpeed", _water.Zones[0].WaveSpeed);
						Shader.SetFloat("wavePitch", _water.Zones[0].WavePitch);
					}
				}

				_isMinimap = false;
			}

			if (_isMinimap) {
				Shader.SetVector4("colorWater", viewport.RenderOptions.MinimapWaterColor);
				Shader.SetInt("colorMode", 1);
				Shader.SetFloat("amplitude", 0);
				Shader.SetFloat("waveSpeed", 0);
				Shader.SetFloat("wavePitch", 0);

				for (int i = 0; i < _water.Zones.Count; i++) {
					if (_ri.Indices[i].Count == 0)
						continue;

					if (_water.Zones.Count > 1) {
						Shader.SetFloat("waterHeight", -_water.Zones[i].Level);
					}

					GL.DrawArrays(PrimitiveType.Quads, _ri.Indices[i].Begin, _ri.Indices[i].Count);
				}
			}
			else {
				for (int i = 0; i < _water.Zones.Count; i++) {
					if (_ri.Indices[i].Count == 0)
						continue;

					if (_water.Zones.Count > 1) {
						Shader.SetFloat("waterHeight", -_water.Zones[i].Level);
						Shader.SetFloat("amplitude", _water.Zones[i].WaveHeight);
						Shader.SetFloat("waveSpeed", _water.Zones[i].WaveSpeed);
						Shader.SetFloat("wavePitch", _water.Zones[i].WavePitch);
					}

					int offset = 32 * i;
					int index = ((int)(time * 60 / _water.Zones[i].TextureCycling)) % 32;

					Textures[offset + index].Bind();

					GL.DrawArrays(PrimitiveType.Quads, _ri.Indices[i].Begin, _ri.Indices[i].Count);
				}
			}

			GL.DepthMask(true);
		}

		public override void Unload() {
			IsUnloaded = true;

			foreach (var texture in Textures) {
				TextureManager.UnloadTexture(texture.Resource, _request.Context);
			}

			_ri.Unload();
		}
	}
}
