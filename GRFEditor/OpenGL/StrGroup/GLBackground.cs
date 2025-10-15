using System.Windows;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.WPF.PreviewTabs;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;

namespace GRFEditor.OpenGL.StrGroup {
	public class GLBackground : GLObject {
		private int _textId;

		public override void Load(OpenGLViewport viewport) {
			_textId = GLHelper.LoadTexture(new GrfImage(ApplicationManager.GetResource("background.png")), "INTERNAL_background.png");

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Shader = new Shader("str.background.vert", "str.background.frag");
			SetupShader();
		}

		public override void Draw(OpenGLViewport viewport) {
			double scalerX = 16d;
			double scalerY = 16d;
			Point relativeCenter = new Point(0.5f, 0.5f);
			double uv_offsetX = 0;
			double uv_offsetY = 0;

			Shader.Use();
			GL.Disable(EnableCap.Blend);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Shader.SetVector4("color", GrfEditorConfiguration.StrEditorBackgroundColorQuick.Color);

			GL.BindTexture(TextureTarget.Texture2D, _textId);

			Model[0, 0] = viewport._primary.Width;
			Model[1, 1] = viewport._primary.Height;
			Model[3, 0] = -(float)((relativeCenter.X * viewport._primary.Width - viewport._primary.Width / 2d));
			Model[3, 1] = -(float)((-relativeCenter.Y * viewport._primary.Height + viewport._primary.Height / 2d));

			double tileSizeX = scalerX;
			double tileSizeY = scalerY;

			var x = -(viewport._primary.Width * relativeCenter.X) / tileSizeX;
			var y = -(viewport._primary.Height * (1 - relativeCenter.Y)) / tileSizeY;

			Point bottomLeft = new Point(0 + uv_offsetX, 0 + uv_offsetY);
			Point bottomRight = new Point(viewport._primary.Width / tileSizeX + uv_offsetX, 0 + uv_offsetY);
			Point topRight = new Point(viewport._primary.Width / tileSizeX + uv_offsetX, viewport._primary.Height / tileSizeY + uv_offsetY);
			Point topLeft = new Point(0 + uv_offsetX, viewport._primary.Height / tileSizeY + uv_offsetY);

			Vector translate = new Vector(x, y);

			bottomLeft += translate;
			bottomRight += translate;
			topRight += translate;
			topLeft += translate;

			ChangeVertexCoord(0, topRight.X, topRight.Y);
			ChangeVertexCoord(1, bottomRight.X, bottomRight.Y);
			ChangeVertexCoord(2, bottomLeft.X, bottomLeft.Y);
			ChangeVertexCoord(3, topLeft.X, topLeft.Y);

			UpdateVertexCoords();

			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, _textId);

			Shader.SetMatrix4("model", ref Model);
			Shader.SetMatrix4("view", ref viewport.View);
			Shader.SetMatrix4("projection", ref viewport.Projection);

			GL.BindVertexArray(_vertexArrayObject);
			GL.DrawElements(PrimitiveType.Quads, _indices.Length, DrawElementsType.UnsignedInt, 0);
		}
	}
}
