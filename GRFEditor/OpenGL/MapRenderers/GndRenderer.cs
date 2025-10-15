using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GRF.FileFormats.RswFormat;
using GRF.Graphics;
using GRF.Image;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Buffer = System.Buffer;
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

		[StructLayout(LayoutKind.Sequential)]
		public struct VertexP3T2S2C4N3 {
			public Vector3 Pos;
			public Vector2 Tex;
			public Vector2 ShadowTex;
			public Vector4 Color;
			public Vector3 Normal;

			public VertexP3T2S2C4N3(in Vector3 pos, in Vector2 tex, in Vector2 shadowTex, in Vector4 color, in Vector3 normal) {
				Pos = pos;
				Tex = tex;
				ShadowTex = shadowTex;
				Color = color;
				Normal = normal;
			}

			public VertexP3T2S2C4N3(in Vector3 pos, in Vector2 tex, in float sX, in float sY, in Vector4 color, in Vector3 normal) {
				Pos = pos;
				Tex = tex;
				ShadowTex.X = sX;
				ShadowTex.Y = sY;
				Color = color;
				Normal = normal;
			}
		};

		private static readonly Vector4 ColorWhite = new Vector4(1f);

		private void _loadGround(OpenGLViewport viewport) {
			VertexP3T2S2C4N3[][] verts;
			int[] vertsSizes;
			VertIndices.Clear();

			float lmsx = _gnd.LightmapWidth;
			float lmsy = _gnd.LightmapHeight;
			float lmsxu = lmsx - 2.0f;
			float lmsyu = lmsy - 2.0f;

			int shadowmapRowCount = ShadowmapSize / _gnd.LightmapWidth;

			// Pre-assign the arrays
			const int SizePerTile = 6;

			int size = _gnd.Tiles.Max(p => p.TextureIndex) + 2;
			int totalVertex = 0;

			verts = new VertexP3T2S2C4N3[size][];
			vertsSizes = new int[size];
			var cubes = _gnd.Cubes;
			var tiles = _gnd.Tiles;
			int width = _gnd.Width;
			int height = _gnd.Height;

			int x = 0;
			int y = 0;

			for (int i = 0; i < cubes.Length; i++) {
				Cube cube = cubes[i];

				if (cube.TileUp != -1) {
					vertsSizes[tiles[cube.TileUp].TextureIndex + 1]++;
				}
				else if (viewport.RenderOptions.ShowBlackTiles) {
					vertsSizes[0]++;
				}

				if (cube.TileSide != -1 && x < width - 1) {
					vertsSizes[tiles[cube.TileSide].TextureIndex + 1]++;
				}

				if (cube.TileFront != -1 && y < height - 1) {
					vertsSizes[tiles[cube.TileFront].TextureIndex + 1]++;
				}

				x++;

				if (x == width) {
					x = 0;
					y++;
				}
			}

			for (int i = 0; i < verts.Length; i++) {
				verts[i] = new VertexP3T2S2C4N3[vertsSizes[i] * SizePerTile];
				totalVertex += vertsSizes[i] * SizePerTile;
				vertsSizes[i] = 0;
			}

			Vector2[] lightmapMins = new Vector2[_gnd.Lightmaps.Count];
			Vector2[] lightmapMaxs = new Vector2[_gnd.Lightmaps.Count];
			float shadowUnit = 1.0f / ShadowmapSize;
			float shadowUnitX = lmsx / ShadowmapSize;
			float shadowUnitY = lmsy / ShadowmapSize;
			float shadowUnitUX = lmsxu / ShadowmapSize;
			float shadowUnitUY = lmsyu / ShadowmapSize;
			for (int t = 0; t < _gnd.Lightmaps.Count; t++) {
				float lx = (t % shadowmapRowCount) * shadowUnitX + shadowUnit;
				float ly = (t / shadowmapRowCount) * shadowUnitY + shadowUnit;
				lightmapMins[t] = new Vector2(lx, ly);
				lightmapMaxs[t] = lightmapMins[t] + new Vector2(shadowUnitUX, shadowUnitUY);
			}

			Vector4 c1, c2, c3, c4;
			x = 0;
			y = 0;

			for (int i = 0; i < cubes.Length; i++) {
				Cube cube = cubes[i];

				if (cube.TileUp != -1) {
					Tile tile = tiles[cube.TileUp];

					var lm1 = lightmapMins[tile.LightmapIndex];
					var lm2 = lightmapMaxs[tile.LightmapIndex];

					int idxUp    = (y < height - 1) ? i + width     : -1;
					int idxRight = (x < width - 1)  ? i + 1         : -1;
					int idxDiag  = (x < width - 1 && y < height - 1) ? i + width + 1 : -1;

					c1 = (idxUp    >= 0 && cubes[idxUp].TileUp    != -1) ? tiles[cubes[idxUp].TileUp].Color    : ColorWhite;
					c2 = (idxDiag  >= 0 && cubes[idxDiag].TileUp  != -1) ? tiles[cubes[idxDiag].TileUp].Color  : ColorWhite;
					c4 = (idxRight >= 0 && cubes[idxRight].TileUp != -1) ? tiles[cubes[idxRight].TileUp].Color : ColorWhite;
					c3 = tile.Color;

					int offset = vertsSizes[tile.TextureIndex + 1];
					ref var v1 = ref verts[tile.TextureIndex + 1][offset + 2];
					ref var v2 = ref verts[tile.TextureIndex + 1][offset + 1];
					ref var v3 = ref verts[tile.TextureIndex + 1][offset + 5];
					ref var v4 = ref verts[tile.TextureIndex + 1][offset + 0];
					v1 = new VertexP3T2S2C4N3(new Vector3(10 * x, -cube[2], 10 * height - 10 * y), tile[2], new Vector2(lm1.X, lm2.Y), c1, cube.Normals[2]);
					v2 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[3], 10 * height - 10 * y), tile[3], new Vector2(lm2.X, lm2.Y), c2, cube.Normals[3]);
					v3 = new VertexP3T2S2C4N3(new Vector3(10 * x, -cube[0], 10 * height - 10 * y + 10), tile[0], new Vector2(lm1.X, lm1.Y), c3, cube.Normals[0]);
					v4 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[1], 10 * height - 10 * y + 10), tile[1], new Vector2(lm2.X, lm1.Y), c4, cube.Normals[1]);

					verts[tile.TextureIndex + 1][offset + 3] = verts[tile.TextureIndex + 1][offset + 0];
					verts[tile.TextureIndex + 1][offset + 4] = verts[tile.TextureIndex + 1][offset + 2];
					vertsSizes[tile.TextureIndex + 1] += SizePerTile;
				}
				else if (viewport.RenderOptions.ShowBlackTiles) {
					int offset = vertsSizes[0];
					ref var v1 = ref verts[0][offset + 2];
					ref var v2 = ref verts[0][offset + 1];
					ref var v3 = ref verts[0][offset + 0];
					ref var v4 = ref verts[0][offset + 3];
					v1 = new VertexP3T2S2C4N3(new Vector3(10 * x, -cube[2], 10 * height - 10 * y), new Vector2(0), new Vector2(0), new Vector4(0.0f), cube.Normals[2]);
					v2 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[3], 10 * height - 10 * y), new Vector2(0), new Vector2(0), new Vector4(0.0f), cube.Normals[3]);
					v3 = new VertexP3T2S2C4N3(new Vector3(10 * x, -cube[0], 10 * height - 10 * y + 10), new Vector2(0), new Vector2(0), new Vector4(0.0f), cube.Normals[0]);
					v4 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[1], 10 * height - 10 * y + 10), new Vector2(0), new Vector2(0), new Vector4(0.0f), cube.Normals[1]);

					verts[0][offset + 4] = verts[0][offset + 1];
					verts[0][offset + 5] = verts[0][offset + 0];
					vertsSizes[0] += SizePerTile;
				}

				if (cube.TileSide != -1 && x < width - 1) {
					Tile tile = tiles[cube.TileSide];

					var lm1 = lightmapMins[tile.LightmapIndex];
					var lm2 = lightmapMaxs[tile.LightmapIndex];
					
					int idxRight = (x < width - 1)  ? i + 1         : -1;
					int idxDiag  = (x < width - 1 && y < height - 1) ? i + width + 1 : -1;
					
					c1 = (idxRight >= 0 && cubes[idxRight].TileUp != -1) ? tiles[cubes[idxRight].TileUp].Color : ColorWhite;
					c2 = (idxDiag  >= 0 && cubes[idxDiag].TileUp  != -1) ? tiles[cubes[idxDiag].TileUp].Color  : ColorWhite;

					Vector3 n = new Vector3(-1, 0, 0);
					Cube cubeN = cubes[x + 1 + y * width];

					if (cubeN != null && cube[1] < cubeN[0]) {
						n *= -1;
					}

					int offset = vertsSizes[tile.TextureIndex + 1];
					ref var v1 = ref verts[tile.TextureIndex + 1][offset + 0];
					ref var v2 = ref verts[tile.TextureIndex + 1][offset + 4];
					ref var v3 = ref verts[tile.TextureIndex + 1][offset + 1];
					ref var v4 = ref verts[tile.TextureIndex + 1][offset + 2];
					v1 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[1], 10 * height - 10 * y + 10), tile[1], new Vector2(lm2.X, lm1.Y), c1, n);
					v2 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[3], 10 * height - 10 * y), tile[0], new Vector2(lm1.X, lm1.Y), c2, n);
					v3 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cubes[x + 1 + y * width][0], 10 * height - 10 * y + 10), tile[3], new Vector2(lm2.X, lm2.Y), c1, n);
					v4 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cubes[x + 1 + y * width][2], 10 * height - 10 * y), tile[2], new Vector2(lm1.X, lm2.Y), c2, n);

					verts[tile.TextureIndex + 1][offset + 3] = verts[tile.TextureIndex + 1][offset + 2];
					verts[tile.TextureIndex + 1][offset + 5] = verts[tile.TextureIndex + 1][offset + 0];
					vertsSizes[tile.TextureIndex + 1] += SizePerTile;
				}

				if (cube.TileFront != -1 && y < height - 1) {
					Tile tile = tiles[cube.TileFront];

					var lm1 = lightmapMins[tile.LightmapIndex];
					var lm2 = lightmapMaxs[tile.LightmapIndex];

					int idxUp    = (y < height - 1) ? i + width     : -1;
					int idxDiag  = (x < width - 1 && y < height - 1) ? i + width + 1 : -1;
					
					c1 = (idxUp    >= 0 && cubes[idxUp].TileUp    != -1) ? tiles[cubes[idxUp].TileUp].Color    : ColorWhite;
					c2 = (idxDiag  >= 0 && cubes[idxDiag].TileUp  != -1) ? tiles[cubes[idxDiag].TileUp].Color  : ColorWhite;

					Vector3 n = new Vector3(0, 0, -1);
					Cube cubeN = cubes[x + (y + 1) * width];

					if (cubeN != null && cube[2] < cubeN[0]) {
						n *= -1;
					}

					int offset = vertsSizes[tile.TextureIndex + 1];
					ref var v1 = ref verts[tile.TextureIndex + 1][offset + 0];
					ref var v2 = ref verts[tile.TextureIndex + 1][offset + 1];
					ref var v3 = ref verts[tile.TextureIndex + 1][offset + 2];
					ref var v4 = ref verts[tile.TextureIndex + 1][offset + 5];
					v1 = new VertexP3T2S2C4N3(new Vector3(10 * x, -cube[2], 10 * height - 10 * y), tile[0], new Vector2(lm1.X, lm1.Y), c1, n);
					v2 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cube[3], 10 * height - 10 * y), tile[1], new Vector2(lm2.X, lm1.Y), c2, n);
					v4 = new VertexP3T2S2C4N3(new Vector3(10 * x + 10, -cubes[x + (y + 1) * width][1], 10 * height - 10 * y), tile[3], new Vector2(lm2.X, lm2.Y), c2, n);
					v3 = new VertexP3T2S2C4N3(new Vector3(10 * x, -cubes[x + (y + 1) * width][0], 10 * height - 10 * y), tile[2], new Vector2(lm1.X, lm2.Y), c1, n);

					verts[tile.TextureIndex + 1][offset + 3] = verts[tile.TextureIndex + 1][offset + 2];
					verts[tile.TextureIndex + 1][offset + 4] = verts[tile.TextureIndex + 1][offset + 1];
					vertsSizes[tile.TextureIndex + 1] += SizePerTile;
				}

				x++;

				if (x == width) {
					x = 0;
					y++;
				}
			}

			int vertOffset = 0;
			int structSize = Marshal.SizeOf<VertexP3T2S2C4N3>();
			int structFloatSize = structSize / sizeof(float);
			_ri.RawVertices = new float[totalVertex * structFloatSize];

			for (int i = 0; i < verts.Length; i++) {
				VertIndices.Add(new VboIndex { Texture = i - 1, Begin = vertOffset, Count = vertsSizes[i] });

				if (vertsSizes[i] == 0)
					continue;

				IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(verts[i], 0);
				Marshal.Copy(ptr, _ri.RawVertices, vertOffset * structFloatSize, vertsSizes[i] * structFloatSize);

				vertOffset += vertsSizes[i];
			}
		}

		private void _loadShadowmap() {
			_shadow = GRF.FileFormats.GndFormat.Gnd.GenerateShadowMap(ShadowmapSize, _gnd.LightmapOffset(), _gnd.LightmapWidth, _gnd.LightmapHeight, _gnd.LightmapSizeCell, _gnd.Lightmaps);
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.Ground)
				return;

			if (viewport.RenderPass != RenderMode.OpaqueTextures && viewport.RenderPass != RenderMode.TransparentTextures)
				return;

			if (!IsLoaded) {
				Load(viewport);
			}

			Shader.Use();

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

				Matrix3 mat = Matrix3.Identity;
				mat = GLHelper.Rotate(ref mat, -GLHelper.ToRad(_rsw.Light.Latitude), new Vector3(1, 0, 0));
				mat = GLHelper.Rotate(ref mat, -GLHelper.ToRad(_rsw.Light.Longitude), new Vector3(0, 1, 0));

				Vector3 lightDirection = mat * new Vector3(0, 1, 0);

				Shader.SetVector3("lightAmbient", new Vector3(_rsw.Light.AmbientRed, _rsw.Light.AmbientGreen, _rsw.Light.AmbientBlue));
				Shader.SetVector3("lightDiffuse", new Vector3(_rsw.Light.DiffuseRed, _rsw.Light.DiffuseGreen, _rsw.Light.DiffuseBlue));
				Shader.SetVector3("lightDirection", ref lightDirection);

				var textLocation = GL.GetUniformLocation(Shader.Handle, "s_texture");
				var shadowLocation = GL.GetUniformLocation(Shader.Handle, "s_lighting");

				GL.Uniform1(textLocation, 0);
				GL.Uniform1(shadowLocation, 1);

				Shader.SetFloat("showLightmap", viewport.RenderOptions.Lightmap ? 1.0f : 0.0f);
				Shader.SetFloat("showShadowmap", viewport.RenderOptions.Shadowmap ? 1.0f : 0.0f);
				Shader.SetFloat("enableCullFace", viewport.RenderOptions.EnableFaceCulling ? 1.0f : 0.0f);

				_gndShadow = new Texture("Shadow", _shadow, true, TextureRenderMode.ShadowMapTexture);
			}

			if (ReloadLight) {
				Matrix3 mat = Matrix3.Identity;
				mat = GLHelper.Rotate(ref mat, -GLHelper.ToRad(_rsw.Light.Latitude), new Vector3(1, 0, 0));
				mat = GLHelper.Rotate(ref mat, -GLHelper.ToRad(_rsw.Light.Longitude), new Vector3(0, 1, 0));

				Vector3 lightDirection = mat * new Vector3(0, 1, 0);

				Shader.SetVector3("lightAmbient", new Vector3(_rsw.Light.AmbientRed, _rsw.Light.AmbientGreen, _rsw.Light.AmbientBlue));
				Shader.SetVector3("lightDiffuse", new Vector3(_rsw.Light.DiffuseRed, _rsw.Light.DiffuseGreen, _rsw.Light.DiffuseBlue));
				Shader.SetVector3("lightDirection", ref lightDirection);
				ReloadLight = false;
			}

			GL.ActiveTexture(TextureUnit.Texture1);
			_gndShadow.Bind();
			GL.ActiveTexture(TextureUnit.Texture0);

			Shader.SetMatrix4("mvp", ref viewport.ViewProjection);

			if (viewport.RenderOptions.ShowWireframeView || viewport.RenderOptions.ShowPointView) {
				Shader.SetBool("wireframe", true);
			}
			else {
				Shader.SetBool("wireframe", false);
			}

			_ri.BindVao();

			foreach (var vboIndex in VertIndices) {
				if (viewport.RenderPass == RenderMode.OpaqueTextures && (vboIndex.Texture >= 0 && Textures[vboIndex.Texture].IsSemiTransparent))
					continue;
				if (viewport.RenderPass == RenderMode.TransparentTextures && (vboIndex.Texture < 0 || !Textures[vboIndex.Texture].IsSemiTransparent))
					continue;

				if (vboIndex.Texture != -1) {
					Textures[vboIndex.Texture].Bind();
				}
				else {
					Shader.SetFloat("showLightmap", 0.0f);
					_black.Bind();
				}

				GL.DrawArrays(PrimitiveType.Triangles, vboIndex.Begin, vboIndex.Count);
#if DEBUG
				if (_subPass == 0) {
					viewport.Stats.DrawArrays_Calls++;
					viewport.Stats.DrawArrays_Calls_VertexLength += vboIndex.Count;
				}
#endif

				if (vboIndex.Texture == -1) {
					Shader.SetFloat("showLightmap", viewport.RenderOptions.Lightmap ? 1.0f : 0.0f);
				}
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
