using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;

namespace GRFEditor.OpenGL.MapRenderers {
	/// <summary>
	/// Background grid for the map renderer.
	/// </summary>
	/// <seealso cref="GRFEditor.OpenGL.MapRenderers.Renderer"/>
	public class BackgroundRenderer : Renderer {
		private Texture _backTex;
		private Matrix4 _model = Matrix4.Identity;
		private readonly RenderInfo _ri = new RenderInfo();
		private float[] _vertices = {
			 0.5f,  0.5f, 0.0f, 1.0f, 1.0f,
			 0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
			-0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
			-0.5f,  0.5f, 0.0f, 0.0f, 1.0f 
		};

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

			Shader = new Shader("shader_color.vert", "shader_color.frag");

			_ri.CreateVao();

			Resize(viewport);

			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float));
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
		}

		public override void Resize(OpenGLViewport viewport) {
			if (!IsLoaded || IsUnloaded) {
				return;
			}

			_model[0, 0] = 2f;
			_model[1, 1] = 2f;
			_model[3, 0] = 0;
			_model[3, 1] = 0;

			const float tileSizeX = 16f;
			const float tileSizeY = 16f;

			Vector2 bottomLeft = new Vector2(0, 0);
			Vector2 bottomRight = new Vector2(viewport._primary.Width / tileSizeX, 0);
			Vector2 topRight = new Vector2(viewport._primary.Width / tileSizeX, viewport._primary.Height / tileSizeY);
			Vector2 topLeft = new Vector2(0, viewport._primary.Height / tileSizeY);
			Vector2 translate = new Vector2(0, 1 - (viewport._primary.Height % tileSizeY) / tileSizeY);
			translate += new Vector2(0.5f, 0);

			bottomLeft += translate;
			bottomRight += translate;
			topRight += translate;
			topLeft += translate;
			
			_vertices = new float[] {
				 0.5f,  0.5f, 0.0f, topRight.X, topRight.Y,
				 0.5f, -0.5f, 0.0f, bottomRight.X, bottomRight.Y,
				-0.5f, -0.5f, 0.0f, bottomLeft.X, bottomLeft.Y,
				-0.5f,  0.5f, 0.0f, topLeft.X, topLeft.Y 
			};

			_ri.BindVao();

			if (_ri.Vbo == null) {
				_ri.Vbo = new Vbo();
			}

			_ri.Vbo.SetData(_vertices, BufferUsageHint.StaticDraw, 5);
			_previousWidth = viewport._primary.Width;
			_previousHeight = viewport._primary.Height;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			if (viewport.RenderPass != 0)
				return;

			if (!IsLoaded) {
				Load(viewport);
			}

			if (viewport.RenderOptions.MinimapMode) {
				GL.ClearColor(255, 0, 255, 255);
				return;
			}

			if (_previousWidth != viewport._primary.Width || _previousHeight != viewport._primary.Height)
				Resize(viewport);

			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();

			GL.Disable(EnableCap.DepthTest);

			Shader.Use();
			GL.Disable(EnableCap.Blend);

			if (viewport.RenderOptions.ShowWireframeView || viewport.RenderOptions.ShowPointView) {
				Shader.SetVector4("color", new Vector4(1, 1, 1, 1));
			}
			else if (viewport.RenderOptions.RenderSkymapFeature && viewport.RenderOptions.RenderSkymapDetected && viewport.RenderOptions.RenderingMap) {
				Shader.SetVector4("color", viewport.RenderOptions.SkymapBackgroundColor);
			}
			else {
				Shader.SetVector4("color", GrfEditorConfiguration.MapBackgroundColorQuick.Color);
			}

			GL.Enable(EnableCap.Texture2D);
			_backTex.Bind();

			Shader.SetMatrix4("model", ref _model);
			Shader.SetMatrix4("view", Matrix4.Identity);
			Shader.SetMatrix4("projection", Matrix4.Identity);

			_ri.BindVao();
			GL.DrawArrays(PrimitiveType.Quads, 0, 4);

			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);

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
