using System;
using System.Collections.Generic;
using System.IO;
using GRF.FileFormats.RswFormat;
using GRF.Graphics;
using GRF.Image;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Matrix3 = OpenTK.Matrix3;
using Matrix4 = OpenTK.Matrix4;
using Vertex = GRFEditor.OpenGL.MapComponents.Vertex;

namespace GRFEditor.OpenGL.MapRenderers {
	public class GndRenderer : Renderer {
		private readonly RendererLoadRequest _request;
		private readonly Gnd _gnd;
		private readonly Rsw _rsw;
		public int ShadowmapSize = 0;
		private Texture _gndShadow;
		private readonly Texture _black;
		public List<VboIndex> VertIndices = new List<VboIndex>();
		private readonly RenderInfo _ri = new RenderInfo();

		private GrfImage _shadow;
		public bool ReloadLight { get; set; }

		public GndRenderer(RendererLoadRequest request, Shader shader, Gnd gnd, Rsw rsw) {
			Shader = shader;
			_request = request;
			_gnd = gnd;
			_rsw = rsw;

			_black = TextureManager.LoadTextureAsync("backside.bmp", Rsm.RsmTexturePath + "backside.bmp", TextureRenderMode.GndTexture, request);
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			foreach (var texture in _gnd.Textures) {
				Textures.Add(TextureManager.LoadTextureAsync(texture, Path.GetPathRoot(texture) != "" ? texture : Rsm.RsmTexturePath + texture, TextureRenderMode.GndTexture, _request));
			}

			if (_request.CancelRequired())
				return;

			ShadowmapSize = Math.Max(_gnd.Width * _gnd.LightmapWidth, _gnd.Height * _gnd.LightmapHeight) * 2;

			_loadGround(viewport);
			_loadShadowmap();

			if (_request.CancelRequired())
				return;

			IsLoaded = true;
		}

		private void _loadGround(OpenGLViewport viewport) {
			var vertices = new List<Vertex>();
			var verts = new Dictionary<int, List<Vertex>>();
			VertIndices.Clear();

			float lmsx = _gnd.LightmapWidth;
			float lmsy = _gnd.LightmapHeight;
			float lmsxu = lmsx - 2.0f;
			float lmsyu = lmsy - 2.0f;

			int shadowmapRowCount = ShadowmapSize / _gnd.LightmapWidth;

			for (int x = 0; x < _gnd.Header.Width; x++) {
				for (int y = 0; y < _gnd.Header.Height; y++) {
					Cube cube = _gnd[x, y];
					try {
						if (cube.TileUp != -1) {
							Tile tile = _gnd.Tiles[cube.TileUp];

							Vector2 lm1 = new Vector2((tile.LightmapIndex % shadowmapRowCount) * (lmsx / ShadowmapSize) + 1.0f / ShadowmapSize, (tile.LightmapIndex / shadowmapRowCount) * (lmsy / ShadowmapSize) + 1.0f / ShadowmapSize);
							Vector2 lm2 = lm1 + new Vector2(lmsxu / ShadowmapSize, lmsyu / ShadowmapSize);

							TkVector4 c1 = new TkVector4(1f);

							if (y < _gnd.Height - 1 && _gnd[x, y + 1].TileUp != -1)
								c1 = new TkVector4(_gnd.Tiles[_gnd[x, y + 1].TileUp].Color) / 255.0f;

							TkVector4 c2 = new TkVector4(1f);
							if (x < _gnd.Width - 1 && y < _gnd.Height - 1 && _gnd[x + 1, y + 1].TileUp != -1)
								c2 = new TkVector4(_gnd.Tiles[_gnd[x + 1, y + 1].TileUp].Color) / 255.0f;

							TkVector4 c3 = new TkVector4(tile.Color) / 255f;

							TkVector4 c4 = new TkVector4(1f);
							if (x < _gnd.Width - 1 && _gnd[x + 1, y].TileUp != -1)
								c4 = new TkVector4(_gnd.Tiles[_gnd[x + 1, y].TileUp].Color) / 255.0f;

							Vertex v1 = new Vertex(new Vector3(10 * x, -cube[2], 10 * _gnd.Height - 10 * y), tile[2], new Vector2(lm1.X, lm2.Y), c1, cube.Normals[2]);
							Vertex v2 = new Vertex(new Vector3(10 * x + 10, -cube[3], 10 * _gnd.Height - 10 * y), tile[3], new Vector2(lm2.X, lm2.Y), c2, cube.Normals[3]);
							Vertex v3 = new Vertex(new Vector3(10 * x, -cube[0], 10 * _gnd.Height - 10 * y + 10), tile[0], new Vector2(lm1.X, lm1.Y), c3, cube.Normals[0]);
							Vertex v4 = new Vertex(new Vector3(10 * x + 10, -cube[1], 10 * _gnd.Height - 10 * y + 10), tile[1], new Vector2(lm2.X, lm1.Y), c4, cube.Normals[1]);

							List<Vertex> l;

							if (!verts.TryGetValue(tile.TextureIndex, out l)) {
								l = new List<Vertex>();
								verts[tile.TextureIndex] = l;
							}

							l.Add(v4); l.Add(v2); l.Add(v1);
							l.Add(v4); l.Add(v1); l.Add(v3);
						}
						else if (viewport.RenderOptions.ShowBlackTiles) {
							Vertex v1 = new Vertex(new Vector3(10 * x, -cube[2], 10 * _gnd.Height - 10 * y), new Vector2(0), new Vector2(0), new TkVector4(0.0f), cube.Normals[2]);
							Vertex v2 = new Vertex(new Vector3(10 * x + 10, -cube[3], 10 * _gnd.Height - 10 * y), new Vector2(0), new Vector2(0), new TkVector4(0.0f), cube.Normals[3]);
							Vertex v3 = new Vertex(new Vector3(10 * x, -cube[0], 10 * _gnd.Height - 10 * y + 10), new Vector2(0), new Vector2(0), new TkVector4(0.0f), cube.Normals[0]);
							Vertex v4 = new Vertex(new Vector3(10 * x + 10, -cube[1], 10 * _gnd.Height - 10 * y + 10), new Vector2(0), new Vector2(0), new TkVector4(0.0f), cube.Normals[1]);

							List<Vertex> l;

							if (!verts.TryGetValue(-1, out l)) {
								l = new List<Vertex>();
								verts[-1] = l;
							}

							l.Add(v3); l.Add(v2); l.Add(v1);
							l.Add(v4); l.Add(v2); l.Add(v3);
						}

						if (cube.TileSide != -1 && x < _gnd.Width - 1) {
							Tile tile = _gnd.Tiles[cube.TileSide];

							Vector2 lm1 = new Vector2((tile.LightmapIndex % shadowmapRowCount) * (lmsx / ShadowmapSize) + 1.0f / ShadowmapSize, (tile.LightmapIndex / shadowmapRowCount) * (lmsy / ShadowmapSize) + 1.0f / ShadowmapSize);
							Vector2 lm2 = lm1 + new Vector2(lmsxu / ShadowmapSize, lmsyu / ShadowmapSize);


							TkVector4 c1 = new TkVector4(1f);
							if (x < _gnd.Width - 1 && _gnd[x + 1, y].TileUp != -1)
								c1 = new TkVector4(_gnd.Tiles[_gnd[x + 1, y].TileUp].Color) / 255.0f;
							TkVector4 c2 = new TkVector4(1f);
							if (x < _gnd.Width - 1 && y < _gnd.Height - 1 && _gnd[x + 1, y + 1].TileUp != -1)
								c2 = new TkVector4(_gnd.Tiles[_gnd[x + 1, y + 1].TileUp].Color) / 255.0f;

							Vector3 n = new Vector3(-1, 0, 0);
							Cube cubeN = _gnd[x + 1, y];

							if (cubeN != null && cube[1] < cubeN[0]) {
								n *= -1;
							}

							Vertex v1 = new Vertex(new Vector3(10 * x + 10, -cube[1], 10 * _gnd.Height - 10 * y + 10), tile[1], new Vector2(lm2.X, lm1.Y), c1, n);
							Vertex v2 = new Vertex(new Vector3(10 * x + 10, -cube[3], 10 * _gnd.Height - 10 * y), tile[0], new Vector2(lm1.X, lm1.Y), c2, n);
							Vertex v3 = new Vertex(new Vector3(10 * x + 10, -_gnd[x + 1, y][0], 10 * _gnd.Height - 10 * y + 10), tile[3], new Vector2(lm2.X, lm2.Y), c1, n);
							Vertex v4 = new Vertex(new Vector3(10 * x + 10, -_gnd[x + 1, y][2], 10 * _gnd.Height - 10 * y), tile[2], new Vector2(lm1.X, lm2.Y), c2, n);

							List<Vertex> l;

							if (!verts.TryGetValue(tile.TextureIndex, out l)) {
								l = new List<Vertex>();
								verts[tile.TextureIndex] = l;
							}

							l.Add(v1); l.Add(v3); l.Add(v4);
							l.Add(v4); l.Add(v2); l.Add(v1);
						}

						if (cube.TileFront != -1 && y < _gnd.Height - 1) {
							Tile tile = _gnd.Tiles[cube.TileFront];

							Vector2 lm1 = new Vector2((tile.LightmapIndex % shadowmapRowCount) * (lmsx / ShadowmapSize) + 1.0f / ShadowmapSize, (tile.LightmapIndex / shadowmapRowCount) * (lmsy / ShadowmapSize) + 1.0f / ShadowmapSize);
							Vector2 lm2 = lm1 + new Vector2(lmsxu / ShadowmapSize, lmsyu / ShadowmapSize);

							TkVector4 c1 = new TkVector4(1f);
							if (y < _gnd.Height - 1 && _gnd[x, y + 1].TileUp != -1)
								c1 = new TkVector4(_gnd.Tiles[_gnd[x, y + 1].TileUp].Color) / 255.0f;

							TkVector4 c2 = new TkVector4(1f);
							if (x < _gnd.Width - 1 && y < _gnd.Height - 1 && _gnd[x + 1, y + 1].TileUp != -1)
								c2 = new TkVector4(_gnd.Tiles[_gnd[x + 1, y + 1].TileUp].Color) / 255.0f;

							Vector3 n = new Vector3(0, 0, -1);
							Cube cubeN = _gnd[x, y + 1];

							if (cubeN != null && cube[2] < cubeN[0]) {
								n *= -1;
							}

							Vertex v1 = new Vertex(new Vector3(10 * x, -cube[2], 10 * _gnd.Height - 10 * y), tile[0], new Vector2(lm1.X, lm1.Y), c1, n);
							Vertex v2 = new Vertex(new Vector3(10 * x + 10, -cube[3], 10 * _gnd.Height - 10 * y), tile[1], new Vector2(lm2.X, lm1.Y), c2, n);
							Vertex v4 = new Vertex(new Vector3(10 * x + 10, -_gnd[x, y + 1][1], 10 * _gnd.Height - 10 * y), tile[3], new Vector2(lm2.X, lm2.Y), c2, n);
							Vertex v3 = new Vertex(new Vector3(10 * x, -_gnd[x, y + 1][0], 10 * _gnd.Height - 10 * y), tile[2], new Vector2(lm1.X, lm2.Y), c1, n);

							List<Vertex> l;

							if (!verts.TryGetValue(tile.TextureIndex, out l)) {
								l = new List<Vertex>();
								verts[tile.TextureIndex] = l;
							}

							l.Add(v1); l.Add(v2); l.Add(v3);
							l.Add(v3); l.Add(v2); l.Add(v4);
						}
					}
					catch (Exception err) {
						Z.F(err);
					}
				}
			}

			foreach (var vert in verts) {
				VertIndices.Add(new VboIndex { Texture = vert.Key, Begin = vertices.Count, Count = vert.Value.Count });
				vertices.AddRange(vert.Value);
			}

			_ri.RawVertices = Vbo.Vertex2Data(vertices);
		}

		private void _loadShadowmap() {
			byte[] data = new byte[ShadowmapSize * ShadowmapSize * 4];

			int xs = 0;
			int ys = 0;
			int off = _gnd.LightmapOffset();

			for (int i = 0; i < _gnd.Lightmaps.Count; i++) {
				var lightMap = _gnd.Lightmaps[i];

				for (int xx = 0; xx < _gnd.LightmapWidth; xx++) {
					for (int yy = 0; yy < _gnd.LightmapHeight; yy++) {
						int xxx = _gnd.LightmapWidth * xs + xx;
						int yyy = _gnd.LightmapHeight * ys + yy;

						// Ingame lightmap
						int off1 = (xxx + ShadowmapSize * yyy);
						int off2 = (xx + _gnd.LightmapWidth * yy);

						data[4 * off1 + 0] = lightMap[off + 3 * off2 + 2];
						data[4 * off1 + 1] = lightMap[off + 3 * off2 + 1];
						data[4 * off1 + 2] = lightMap[off + 3 * off2 + 0];
						data[4 * off1 + 3] = lightMap[xx + _gnd.LightmapWidth * yy];
					}
				}

				xs++;

				if (xs * _gnd.LightmapWidth >= ShadowmapSize) {
					xs = 0;
					ys++;

					if (ys * _gnd.LightmapHeight >= ShadowmapSize) {
						ys = 0;
					}
				}
			}

			_shadow = new GrfImage(data, ShadowmapSize, ShadowmapSize, GrfImageType.Bgra32);
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.Ground)
				return;

			if (viewport.RenderPass > 1)
				return;

			if (!IsLoaded) {
				Load(viewport);
			}

			if (viewport.RenderOptions.ShowWireframeView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

			if (viewport.RenderOptions.ShowPointView)
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);

			if (!_ri.VaoCreated()) {
				if (_request.CancelRequired())
					return;

				_ri.CreateVao();
				_ri.Vbo = new Vbo();
				_ri.Vbo.SetData(_ri.RawVertices, BufferUsageHint.StaticDraw, 14);
				_ri.RawVertices = null;
				
				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.EnableVertexAttribArray(2);
				GL.EnableVertexAttribArray(3);
				GL.EnableVertexAttribArray(4);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0 * sizeof(float));
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));
				GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 5 * sizeof(float));
				GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 14 * sizeof(float), 7 * sizeof(float));
				GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));

				Shader.Use();
				Matrix3 mat = Matrix3.Identity;
				mat = GLHelper.Rotate(mat, -GLHelper.ToRad(_rsw.Light.Latitude), new Vector3(1, 0, 0));
				mat = GLHelper.Rotate(mat, -GLHelper.ToRad(_rsw.Light.Longitude), new Vector3(0, 1, 0));

				Vector3 lightDirection = mat * new Vector3(0, 1, 0);

				Shader.SetVector3("lightAmbient", new Vector3(_rsw.Light.AmbientRed, _rsw.Light.AmbientGreen, _rsw.Light.AmbientBlue));
				Shader.SetVector3("lightDiffuse", new Vector3(_rsw.Light.DiffuseRed, _rsw.Light.DiffuseGreen, _rsw.Light.DiffuseBlue));
				Shader.SetFloat("lightIntensity", _rsw.Light.Intensity);
				Shader.SetVector3("lightDirection", lightDirection);

				var textLocation = GL.GetUniformLocation(Shader.Handle, "s_texture");
				var shadowLocation = GL.GetUniformLocation(Shader.Handle, "s_lighting");

				GL.Uniform1(textLocation, 0);
				GL.Uniform1(shadowLocation, 1);

				Shader.SetMatrix4("modelMatrix", Matrix4.Identity);
				Shader.SetFloat("showLightmap", viewport.RenderOptions.Lightmap ? 1.0f : 0.0f);
				Shader.SetFloat("showShadowmap", viewport.RenderOptions.Shadowmap ? 1.0f : 0.0f);
				Shader.SetFloat("enableCullFace", viewport.RenderOptions.EnableFaceCulling ? 1.0f : 0.0f);

				_gndShadow = new Texture("Shadow", _shadow, true, TextureRenderMode.ShadowMapTexture);
			}

			if (ReloadLight) {
				Shader.Use();
				Matrix3 mat = Matrix3.Identity;
				mat = GLHelper.Rotate(mat, -GLHelper.ToRad(_rsw.Light.Latitude), new Vector3(1, 0, 0));
				mat = GLHelper.Rotate(mat, -GLHelper.ToRad(_rsw.Light.Longitude), new Vector3(0, 1, 0));

				Vector3 lightDirection = mat * new Vector3(0, 1, 0);

				Shader.SetVector3("lightAmbient", new Vector3(_rsw.Light.AmbientRed, _rsw.Light.AmbientGreen, _rsw.Light.AmbientBlue));
				Shader.SetVector3("lightDiffuse", new Vector3(_rsw.Light.DiffuseRed, _rsw.Light.DiffuseGreen, _rsw.Light.DiffuseBlue));
				Shader.SetFloat("lightIntensity", _rsw.Light.Intensity);
				Shader.SetVector3("lightDirection", lightDirection);
				ReloadLight = false;
			}

			GL.ActiveTexture(TextureUnit.Texture1);
			_gndShadow.Bind();
			GL.ActiveTexture(TextureUnit.Texture0);

			Shader.Use();
			Shader.SetMatrix4("cameraMatrix", ref viewport.View);
			Shader.SetMatrix4("projectionMatrix", ref viewport.Projection);

			if (viewport.RenderOptions.ShowWireframeView || viewport.RenderOptions.ShowPointView) {
				Shader.SetBool("fixedColor", true);
			}
			else {
				Shader.SetBool("fixedColor", false);
			}

			_ri.BindVao();

			foreach (var vboIndex in VertIndices) {
				if (viewport.RenderPass == 0 && (vboIndex.Texture >= 0 && Textures[vboIndex.Texture].IsSemiTransparent))
					continue;
				if (viewport.RenderPass == 1 && (vboIndex.Texture < 0 || !Textures[vboIndex.Texture].IsSemiTransparent))
					continue;

				if (vboIndex.Texture != -1) {
					Textures[vboIndex.Texture].Bind();
				}
				else {
					Shader.SetInt("showLightmap", 0);
					_black.Bind();
				}

				GL.DrawArrays(PrimitiveType.Triangles, vboIndex.Begin, vboIndex.Count);

				if (vboIndex.Texture == -1) {
					Shader.SetInt("showLightmap", 1);
				}
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

				_ri.Unload();
			}
			catch {
			}

			TextureManager.UnloadTexture(_black.Resource, _request.Context);

			if (_gndShadow != null)
				_gndShadow.Unload();

			if (_shadow != null)
				_shadow.Close();

			_shadow = null;
		}
	}
}
