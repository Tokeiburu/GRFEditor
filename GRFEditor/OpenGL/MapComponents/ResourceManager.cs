using GRF.Core.GroupedGrf;

namespace GRFEditor.OpenGL.MapComponents {
	public static class ResourceManager {
		private static MultiGrfReader _reader;

		public static void SetMultiGrf(MultiGrfReader reader) {
			_reader = reader;
		}

		public static byte[] GetData(string path) {
			return _reader.GetData(path);
		}
	}
}
