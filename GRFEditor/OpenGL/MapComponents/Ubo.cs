using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapComponents {
	public class Ubo<T> where T : struct {
		private readonly int _ubo;

		public int Length { get; set; }
		public int Id {
			get { return _ubo; }
		}

		public Ubo() {
			_ubo = GL.GenBuffer();
			OpenGLMemoryManager.AddUbo(_ubo);
		}

		public void Unload() {
			GL.DeleteBuffer(_ubo);
			OpenGLMemoryManager.DelUbo(_ubo);
		}

		public void SetData(List<T> data) {
			Length = data.Count;
			Bind();
			GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, Marshal.SizeOf<T>(), data.ToArray());
		}

		public void SetData(T[] data) {
			Length = data.Length;
			Bind();
			GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, Marshal.SizeOf<T>(), data);
		}

		public void SetData(ref T data) {
			Length = 1;
			Bind();
			GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, Marshal.SizeOf<T>(), ref data);
		}

		public void Bind() {
			GL.BindBuffer(BufferTarget.UniformBuffer, _ubo);
			GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _ubo);
		}

		public void Unbind() {
			GL.BindBuffer(BufferTarget.UniformBuffer, 0);
		}
	}
}
