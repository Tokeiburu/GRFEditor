using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using TokeiLibrary;

namespace GRFEditor.OpenGL.MapRenderers {
	/// <summary>
	/// Background grid for the map renderer.
	/// </summary>
	/// <seealso cref="GRFEditor.OpenGL.MapRenderers.Renderer"/>
	public class BackgroundRenderer : Renderer {
		private Texture _backTex;
		private readonly RenderInfo _ri = new RenderInfo();

		private int _previousWidth = 0;
		private int _previousHeight = 0;

		public override void Load(OpenGLViewport viewport) {
			IsLoaded = true;
			_backTex = new Texture("_APP_background", new GrfImage(ApplicationManager.GetResource("background.png")));
			// Remove it from the memory manager, we'll handle this one ourselves
			OpenGLMemoryManager.DelTextureId(_backTex.Id);

			_backTex.Bind();
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Shader = new Shader("background.vert", "background.frag");

			_ri.CreateVao();

			Resize(viewport);
		}

		public override void Resize(OpenGLViewport viewport) {
			if (!IsLoaded || IsUnloaded) {
				return;
			}

			List<Vertex> vertices = new List<Vertex>();

			vertices.Add(new Vertex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 0.0f)));
			vertices.Add(new Vertex(new Vector3(1.0f, -1.0f, 0.0f), new Vector2(1.0f, 0.0f)));
			vertices.Add(new Vertex(new Vector3(1.0f, 1.0f, 0.0f), new Vector2(1.0f, 1.0f)));
			vertices.Add(new Vertex(new Vector3(1.0f, 1.0f, 0.0f), new Vector2(1.0f, 1.0f)));
			vertices.Add(new Vertex(new Vector3(-1.0f, 1.0f, 0.0f), new Vector2(0.0f, 1.0f)));
			vertices.Add(new Vertex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 0.0f)));

			_ri.BindVao();
			_ri.Vertices = vertices;
			if (_ri.Vbo == null) {
				_ri.Vbo = new Vbo();
			}
			_ri.Vbo.SetData(_ri.Vertices, BufferUsageHint.StaticDraw);
			_ri.RawVertices = null;
			_ri.Vertices.Clear();

			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

			_previousWidth = viewport._primary.Width;
			_previousHeight = viewport._primary.Height;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			if (viewport.RenderPass != RenderMode.OpaqueTextures)
				return;

			if (!IsLoaded) {
				Load(viewport);

				Shader.Use();
				Shader.SetFloat("uTexSize", 16f);
				Shader.SetVector2("uViewportSize", new Vector2(viewport._primary.Width, viewport._primary.Height));
			}

			if (viewport.RenderOptions.MinimapMode) {
				GL.ClearColor(255, 0, 255, 255);
				return;
			}

			Shader.Use();

			if (_previousWidth != viewport._primary.Width || _previousHeight != viewport._primary.Height) {
				Resize(viewport);
				Shader.SetFloat("uTexSize", 16f);
				Shader.SetVector2("uViewportSize", new Vector2(viewport._primary.Width, viewport._primary.Height));
			}

			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.Blend);

			if (viewport.RenderOptions.ShowWireframeView || viewport.RenderOptions.ShowPointView) {
				Shader.SetVector4("color", new Vector4(1, 1, 1, 1));
			}
			else if (viewport.RenderOptions.RenderSkymapFeature && viewport._request != null && viewport._request.SkyMapRenderer != null && viewport._request.SkyMapRenderer.IsValidSkyMap && viewport.RenderOptions.RenderingMap) {
				Shader.SetVector4("color", ref viewport._request.SkyMapRenderer.SkyMap.Bg_Color);
			}
			else {
				Shader.SetVector4("color", GrfEditorConfiguration.MapBackgroundColorQuick.Color);
			}

			GL.Enable(EnableCap.Texture2D);
			_backTex.Bind();

			_ri.BindVao();
			GL.DrawArrays(PrimitiveType.Triangles, 0, _ri.Vbo.Length);
#if DEBUG
			viewport.Stats.DrawArrays_Calls++;
			viewport.Stats.DrawArrays_Calls_VertexLength += _ri.Vbo.Length;
#endif

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
		}

		public override void Unload() {
			IsUnloaded = true;

			_ri.Unload();
			_backTex.Unload();
		}
	}
}
