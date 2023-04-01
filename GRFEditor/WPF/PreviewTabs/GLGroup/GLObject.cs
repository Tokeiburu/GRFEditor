using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.WPF.PreviewTabs.GLGroup {
	public abstract class GLObject {
		protected int _vertexArrayObject;
		protected int _vertexBufferObject;
		protected int _elementBufferObject;
		protected Matrix4 Model = Matrix4.Identity;

		protected float[] _vertices = {
			 0.5f,  0.5f, 0.0f, 1.0f, 1.0f, // top right
			 0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
			-0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
			-0.5f,  0.5f, 0.0f, 0.0f, 1.0f  // top left
		};

		protected uint[] _indices = { 0, 1, 2, 3 };

		public Shader Shader { get; set; }

		public abstract void Load(OpenGLViewport viewport);
		public abstract void Draw(OpenGLViewport viewport);

		public void SetupShader() {
			_vertexArrayObject = GL.GenVertexArray();
			GL.BindVertexArray(_vertexArrayObject);

			_vertexBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

			_elementBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
			GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

			// Because there's now 5 floats between the start of the first vertex and the start of the second,
			// we modify the stride from 3 * sizeof(float) to 5 * sizeof(float).
			// This will now pass the new vertex array to the buffer.
			var vertexLocation = Shader.GetAttribLocation("aPosition");
			GL.EnableVertexAttribArray(vertexLocation);
			GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

			// Next, we also setup texture coordinates. It works in much the same way.
			// We add an offset of 3, since the texture coordinates comes after the position data.
			// We also change the amount of data to 2 because there's only 2 floats for texture coordinates.
			var texCoordLocation = Shader.GetAttribLocation("aTexCoord");
			GL.EnableVertexAttribArray(texCoordLocation);
			GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
		}

		public virtual void ChangeVertex(int point, float v1, float v2) {
			_vertices[5 * point + 0] = v1;
			_vertices[5 * point + 1] = v2;
		}

		public virtual void ChangeVertex(int point, double v1, double v2) {
			_vertices[5 * point + 0] = (float)v1;
			_vertices[5 * point + 1] = (float)v2;
		}

		public virtual void UpdateVertex() {
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
		}

		public void ChangeVertexCoord(int point, float v1, float v2) {
			_vertices[5 * point + 3] = v1;
			_vertices[5 * point + 4] = v2;
		}

		public void ChangeVertexCoord(int point, double v1, double v2) {
			_vertices[5 * point + 3] = (float)v1;
			_vertices[5 * point + 4] = (float)v2;
		}

		public void UpdateVertexCoords() {
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
		}
	}
}
