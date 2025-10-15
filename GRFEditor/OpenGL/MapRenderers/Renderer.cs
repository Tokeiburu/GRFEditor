using System.Collections.Generic;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;

namespace GRFEditor.OpenGL.MapRenderers {
	public abstract class Renderer {
		public List<Texture> Textures = new List<Texture>();

		public Shader Shader { get; set; }
		public bool Permanent { get; set; }
		public bool IsLoaded { get; set; }
		public bool IsUnloaded { get; set; }
		protected int _subPass = 0;

		public abstract void Load(OpenGLViewport viewport);
		public abstract void Render(OpenGLViewport viewport);
		public virtual void Resize(OpenGLViewport viewport) {

		}

		public abstract void Unload();
	}
}
