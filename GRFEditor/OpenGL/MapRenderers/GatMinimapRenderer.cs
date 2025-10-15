using System.Collections.Generic;
using GRF.FileFormats.GatFormat;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Matrix4 = OpenTK.Matrix4;
using Vertex = GRFEditor.OpenGL.MapComponents.Vertex;

namespace GRFEditor.OpenGL.MapRenderers {
	public class GatMinimapRenderer : Renderer {
		private readonly RendererLoadRequest _request;
		private readonly Gat _gat;
		private readonly Gnd _gnd;
		private readonly RenderInfo _ri = new RenderInfo();

		public GatMinimapRenderer(RendererLoadRequest request, Shader shader, Gat gat, Gnd gnd) {
			Shader = shader;
			_request = request;
			_gat = gat;
			_gnd = gnd;
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			IsLoaded = true;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || !viewport.RenderOptions.Ground)
				return;

			if (viewport.RenderPass != RenderMode.LubTextures)
				return;

			if (!IsLoaded) {
				Load(viewport);
			}

			if (!_ri.VaoCreated()) {
				if (_request.CancelRequired())
					return;

				_ri.CreateVao();
				_ri.Vbo = new Vbo();
				_buildVertices();

				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.EnableVertexAttribArray(2);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
				GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));
			}

			_ri.BindVao();
			Shader.Use();
			Shader.SetMatrix4("mvp", ref viewport.ViewProjection);

			for (int i = 0; i < 3; i++) {
				Shader.SetVector4("color", new Vector4(1, 0, 0, 1.0f));
				GL.LineWidth(1.0f);
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

				if (i == 0) {
					GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
					Shader.SetVector4("color", ref viewport.RenderOptions.MinimapNonWalkColor);
				}
				else if (i == 1) {
					GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.One);
					Shader.SetVector4("color", ref viewport.RenderOptions.MinimapWalkColor);
				}
				else {
					GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
					Shader.SetVector4("color", new Vector4(0, 0, 0, 1));
				}

				GL.DrawArrays(PrimitiveType.Triangles, _ri.Indices[i].Begin, _ri.Indices[i].Count);
#if DEBUG
				viewport.Stats.DrawArrays_Calls++;
				viewport.Stats.DrawArrays_Calls_VertexLength += _ri.Indices[i].Count;
#endif
			}

			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		private void _buildVertices() {
			List<Vertex> dark = new List<Vertex>();
			List<Vertex> pale = new List<Vertex>();
			List<Vertex> black = new List<Vertex>();

			for (int x = 0; x < _gat.Width; x++) {
				for (int y = 0; y < _gat.Height; y++) {
					List<Vertex> list = null;

					Cell cell = _gat[x, y];
					Cube cube = _gnd[x / 2, y / 2];

					if (cube.TileUp == -1)
						continue;

					int gatType = (int)cell.Type;

					if (gatType < 0) {
						gatType &= (int)~0x80000000;
					}

					switch (gatType) {
						case 0:
							list = pale;
							break;
						default:
							list = dark;
							break;
					}

					Vertex v1 = new Vertex(new Vector3(5 * x, 3000, 5 * _gat.Height - 5 * y + 5), new Vector2(0), new Vector3(0, 1, 0));
					Vertex v2 = new Vertex(new Vector3(5 * x + 5, 3000, 5 * _gat.Height - 5 * y + 5), new Vector2(0), new Vector3(0, 1, 0));
					Vertex v3 = new Vertex(new Vector3(5 * x, 3000, 5 * _gat.Height - 5 * y + 10), new Vector2(0), new Vector3(0, 1, 0));
					Vertex v4 = new Vertex(new Vector3(5 * x + 5, 3000, 5 * _gat.Height - 5 * y + 10), new Vector2(0), new Vector3(0, 1, 0));

					list.Add(v4); list.Add(v2); list.Add(v1);
					list.Add(v4); list.Add(v1); list.Add(v3);
				}
			}

			_ri.BindVao();
			_ri.Indices.Clear();
			_ri.Indices.Add(new VboIndex { Begin = 0, Count = dark.Count });
			_ri.Indices.Add(new VboIndex { Begin = dark.Count, Count = pale.Count });
			_ri.Indices.Add(new VboIndex { Begin = dark.Count + pale.Count, Count = black.Count });
			_ri.Vertices = dark;
			_ri.Vertices.AddRange(pale);
			_ri.Vertices.AddRange(black);
			_ri.Vbo.SetData(_ri.Vertices, BufferUsageHint.StaticDraw);
			_ri.RawVertices = null;
			_ri.Vertices.Clear();
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
		}
	}
}
