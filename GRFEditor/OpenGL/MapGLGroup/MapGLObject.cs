using System.Collections.Generic;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;

namespace GRFEditor.OpenGL.MapGLGroup {
	public abstract class MapGLObject {
		public List<Texture> Textures = new List<Texture>();

		public Shader Shader { get; set; }
		public bool Permanent { get; set; }
		public bool IsLoaded { get; set; }
		public bool IsUnloaded { get; set; }

		public abstract void Load(OpenGLViewport viewport);
		public abstract void Render(OpenGLViewport viewport);
		public virtual void Resize(OpenGLViewport viewport) {

		}

		public abstract void Unload();
	}
}
