using System;
using System.Collections.Generic;
using System.IO;
using GRF.FileFormats.GatFormat;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;
using Utilities.Services;
using Matrix4 = OpenTK.Matrix4;
using Vertex = GRFEditor.OpenGL.MapComponents.Vertex;

namespace GRFEditor.OpenGL.MapRenderers {
	public class GatRenderer : Renderer {
		public static float GatOffset = 0.01f;
		public class GatChunk {
			public static int ChunkSize = 16;
			public bool Dirty { get; set; }
			private RenderInfo _ri = new RenderInfo();
			private int _x;
			private int _y;
			private Gat _gat;

			public GatChunk(int x, int y, Gat gat) {
				_x = x;
				_y = y;
				_gat = gat;
			}

			public void Render(OpenGLViewport viewport, GatRenderer renderer) {
				if (Dirty || !_ri.VaoCreated()) {
					Rebuild(renderer);
				}

				_ri.BindVao();
				GL.DrawArrays(PrimitiveType.Triangles, 0, _ri.Vbo.Length);
#if DEBUG
				viewport.Stats.DrawArrays_Calls++;
				viewport.Stats.DrawArrays_Calls_VertexLength += _ri.Vbo.Length;
#endif
			}

			public void Rebuild(GatRenderer renderer) {
				List<Vertex> list = new List<Vertex>();

				for (int x = _x; x < Math.Min(_x + ChunkSize, _gat.Width); x++) {
					for (int y = _y; y < Math.Min(_y + ChunkSize, _gat.Height); y++) {
						var cell = _gat[x, y];
						long gatType = (int)cell.Type;

						if (gatType < 0) {
							gatType &= ~0x80000000;
						}

						Vector2 t1 = new Vector2(0.25f * (gatType % 4), 0.25f * (int)(gatType / 4));
						Vector2 t2 = t1 + new Vector2(0.25f);

						var normal_t = cell.CalcNormal();
						Vector3 normal = new Vector3(normal_t.X, normal_t.Y, normal_t.Z);

						var v1 = new Vertex(new Vector3(5 * x	 , GatOffset + -cell.Heights[2], 5 * _gat.Height - 5 * y + 5), new Vector2(t1.X, t1.Y), normal);
						var v2 = new Vertex(new Vector3(5 * x + 5, GatOffset + -cell.Heights[3], 5 * _gat.Height - 5 * y + 5), new Vector2(t2.X, t1.Y), normal);
						var v3 = new Vertex(new Vector3(5 * x	 , GatOffset + -cell.Heights[0], 5 * _gat.Height - 5 * y + 10), new Vector2(t1.X, t2.Y), normal);
						var v4 = new Vertex(new Vector3(5 * x + 5, GatOffset + -cell.Heights[1], 5 * _gat.Height - 5 * y + 10), new Vector2(t2.X, t2.Y), normal);

						list.Add(v4); list.Add(v2); list.Add(v1);
						list.Add(v4); list.Add(v1); list.Add(v3);
					}
				}

				if (!_ri.VaoCreated()) {
					_ri.CreateVao();
					_ri.Vbo = new Vbo();
					_ri.Vbo.SetData(list, BufferUsageHint.StaticDraw);

					GL.EnableVertexAttribArray(0);
					GL.EnableVertexAttribArray(1);
					GL.EnableVertexAttribArray(2);
					GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
					GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
					GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));

					renderer.Shader.Use();
				}
			}
		}

		private readonly RendererLoadRequest _request;
		private readonly Gat _gat;
		private readonly Gnd _gnd;
		private List<GatChunk> _chunks = new List<GatChunk>();
		private int _chunkSizeWidth;
		private int _chunkSizeHeight;

		public GatRenderer(RendererLoadRequest request, Shader shader, Gat gat, Gnd gnd) {
			Shader = shader;
			_request = request;
			_gat = gat;
			_gnd = gnd;
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			_chunkSizeWidth = (int)Math.Ceiling(_gat.Width / (double)GatChunk.ChunkSize);
			_chunkSizeHeight = (int)Math.Ceiling(_gat.Height / (double)GatChunk.ChunkSize);

			for (int y = 0; y < _chunkSizeHeight; y++) {
				for (int x = 0; x < _chunkSizeWidth; x++) {
					_chunks.Add(new GatChunk(x * GatChunk.ChunkSize, y * GatChunk.ChunkSize, _gat));
				}
			}

			IsLoaded = true;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.Gat)
				return;

			if (viewport.RenderPass != RenderMode.TransparentTextures)
				return;

			if (!IsLoaded) {
				Load(viewport);
			}

			GL.DepthFunc(DepthFunction.Lequal);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			if (Textures.Count == 0) {
				var data = ApplicationManager.GetResource("gat.png");
				Textures.Add(TextureManager.LoadTexture("gat_cells", data, _request.Context));
			}

			Shader.Use();
			Shader.SetMatrix4("vp", ref viewport.ViewProjection);
			Shader.SetFloat("alpha", viewport.RenderOptions.GatAlpha);
			Shader.SetFloat("zbias", viewport.RenderOptions.GatZBias);
			Textures[0].Bind();

			for (int i = 0; i < _chunks.Count; i++) {
				_chunks[i].Render(viewport, this);
			}

			GL.DepthFunc(DepthFunction.Less);
		}

		public override void Unload() {
			IsUnloaded = true;

			try {
				foreach (var texture in Textures) {
					TextureManager.UnloadTexture(texture.Resource, _request.Context);
				}
			}
			catch {
			}
		}
	}
}
