using GRFEditor.ApplicationConfiguration;

namespace GRFEditor.OpenGL.MapComponents {
	public static class ResourceManager {
		public static byte[] GetData(string path) {
			return GrfEditorConfiguration.Resources.MultiGrf.GetData(path);
		}
	}
}
