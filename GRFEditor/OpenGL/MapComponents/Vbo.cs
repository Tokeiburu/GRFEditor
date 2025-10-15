using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Buffer = System.Buffer;

namespace GRFEditor.OpenGL.MapComponents {
	public class Vbo {
		private readonly int _vbo;

		public int Length { get; set; }
		public int Id {
			get { return _vbo; }
		}

		public Vbo() {
			_vbo = GL.GenBuffer();
			OpenGLMemoryManager.AddVbo(_vbo);
		}

		public void Unload() {
			GL.DeleteBuffer(_vbo);
			OpenGLMemoryManager.DelVbo(_vbo);
		}

		public void SetData(float[] data, BufferUsageHint usage, int vertexSize) {
			Length = data.Length / vertexSize;
			Bind();
			GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, usage);
		}

		public void SetData(List<Vertex> data, BufferUsageHint usage) {
			if (data.Count == 0)
				return;
			
			int vertexLength = data[0].data.Length;
			Length = data.Count;
			Bind();
			float[] array = new float[vertexLength * data.Count];

			for (int i = 0; i < data.Count; i++) {
				Buffer.BlockCopy(data[i].data, 0, array, vertexLength * i * sizeof(float), vertexLength * sizeof(float));
			}

			GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * array.Length, array, usage);
		}

		public void Bind() {
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
		}

		public void Unbind() {
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public static float[] Vertex2Data(List<Vertex> data) {
			if (data.Count == 0)
				return new float[0];

			int vertexLength = data[0].data.Length;
			float[] array = new float[vertexLength * data.Count];

			for (int i = 0; i < data.Count; i++) {
				Buffer.BlockCopy(data[i].data, 0, array, vertexLength * i * sizeof(float), vertexLength * sizeof(float));
			}

			return array;
		}
	}
}
