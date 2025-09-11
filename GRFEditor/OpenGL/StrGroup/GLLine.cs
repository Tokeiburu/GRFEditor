using System.Windows.Controls;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.WPF.PreviewTabs;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;

namespace GRFEditor.OpenGL.StrGroup {
	public class GLLine : GLObject {
		private readonly Orientation _orientation;
		private int _textId;

		static GLLine() {
			LineShader = new Shader("shader.vert", "shader.frag");
		}

		public static Shader LineShader { get; set; }

		public GLLine(Orientation orientation) {
			_orientation = orientation;
		}

		public override void Load(OpenGLViewport viewport) {
			_textId = GLHelper.LoadTexture(new GrfImage(ApplicationManager.GetResource("line.png")), "INTERNAL_line.png");

			Shader = LineShader;
			SetupShader();
		}

		public override void Draw(OpenGLViewport viewport) {
			if (!GrfEditorConfiguration.StrEditorShowGrid)
				return;

			Shader.Use();
			Shader.SetVector4("colorMult", new Vector4(0, 0, 0, 1f));

			GL.BindTexture(TextureTarget.Texture2D, _textId);
			GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);

			float relativeCenterX = 0.5f;
			float relativeCenterY = 0.5f;

			if (_orientation == Orientation.Vertical) {
				Model[0, 0] = (float)(1 / viewport.ZoomEngine.Scale);
				Model[1, 1] = (float)(viewport._primary.Height * (1 / viewport.ZoomEngine.Scale));
				Model[3, 0] = 0;
				Model[3, 1] = -(float)((-relativeCenterY * viewport._primary.Height + viewport._primary.Height / 2d) * (1 / viewport.ZoomEngine.Scale));
			}
			else {
				Model[0, 0] = (float)(viewport._primary.Width * (1 / viewport.ZoomEngine.Scale));
				Model[1, 1] = (float)(1 / viewport.ZoomEngine.Scale);
				Model[3, 0] = -(float)((relativeCenterX * viewport._primary.Width - viewport._primary.Width / 2d) * (1 / viewport.ZoomEngine.Scale));
				Model[3, 1] = 0;
			}

			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, _textId);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			
			Shader.Use();

			Shader.SetMatrix4("model", ref Model);
			Shader.SetMatrix4("view", ref viewport.View);
			Shader.SetMatrix4("projection", ref viewport.Projection);

			GL.BindVertexArray(_vertexArrayObject);
			GL.DrawElements(PrimitiveType.Quads, _indices.Length, DrawElementsType.UnsignedInt, 0);
		}
	}
}
