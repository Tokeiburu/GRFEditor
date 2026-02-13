using GRFEditor.ApplicationConfiguration;

namespace GRFEditor.OpenGL.MapComponents {
	public static class ResourceManager {
		public static byte[] GetData(string path) {
			return GrfEditorConfiguration.Resources.MultiGrf.GetData(path);
		}

		public static byte[] GetDataBuffered(string path) {
			return GrfEditorConfiguration.Resources.MultiGrf.GetDataBuffered(path);
		}
	}
}
