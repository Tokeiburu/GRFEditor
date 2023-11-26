using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapGLGroup {
	public class LineRenderer : MapGLObject {
		public Vector4 Color = new Vector4(0, 0, 0, 0.7f);
		private readonly Vector3[] _lines;
		private readonly RenderInfo _ri = new RenderInfo();

		public LineRenderer(Shader shader, params Vector3[] lines) {
			Shader = shader;
			_lines = lines;
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			var vertices = new float[_lines.Length * 3];

			for (int i = 0; i < _lines.Length; i++) {
				for (int j = 0; j < 3; j++) {
					vertices[3 * i + j] = _lines[i][j];
				}
			}

			_ri.CreateVao();
			_ri.Vbo = new Vbo();
			_ri.Vbo.SetData(vertices, BufferUsageHint.StaticDraw, 3);

			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
			IsLoaded = true;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			if (!IsLoaded) {
				Load(viewport);
			}

			Shader.Use();
			Shader.SetVector4("colorMult", Color);

			Shader.SetMatrix4("model", Matrix4.Identity);
			Shader.SetMatrix4("view", viewport.View);
			Shader.SetMatrix4("projection", viewport.Projection);

			GL.BindVertexArray(_ri.Vao);
			GL.DrawArrays(PrimitiveType.Lines, 0, _lines.Length);
		}

		public override void Unload() {
			IsUnloaded = true;
			_ri.Unload();
		}
	}
}
