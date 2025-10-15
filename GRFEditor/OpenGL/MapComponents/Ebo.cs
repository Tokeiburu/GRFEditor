using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace GRFEditor.OpenGL.MapComponents {
	public class Ebo {
		private readonly int _ebo;

		public int Length { get; private set; }

		public Ebo() {
			_ebo = GL.GenBuffer();
			OpenGLMemoryManager.AddEbo(_ebo);
		}

		public void Unload() {
			GL.DeleteBuffer(_ebo);
			OpenGLMemoryManager.DelEbo(_ebo);
		}

		public void SetData(List<uint> indices, BufferUsageHint usage) {
			SetData(indices.ToArray(), usage);
		}

		public void SetData(uint[] indices, BufferUsageHint usage) {
			if (indices.Length == 0)
				return;

			Bind();
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, usage);
		}

		public void Bind() {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
		}

		public void Unbind() {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
	}
}
