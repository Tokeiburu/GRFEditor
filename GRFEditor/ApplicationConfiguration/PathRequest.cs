using TokeiLibrary.Paths;
using Utilities;

namespace GRFEditor.ApplicationConfiguration {
	public static class PathRequest {
		public static Setting ExtractSetting {
			get { return new Setting(null, typeof (GrfEditorConfiguration).GetProperty("ExtractingServiceLastPath")); }
		}

		public static string SaveFileEditor(params string[] extra) {
			return TkPathRequest.SaveFile(new Setting(null, typeof (GrfEditorConfiguration).GetProperty("AppLastPath")), extra);
		}

		public static string SaveFileExtract(params string[] extra) {
			return TkPathRequest.SaveFile(ExtractSetting, extra);
		}

		public static string OpenFileEditor(params string[] extra) {
			return TkPathRequest.OpenFile(new Setting(null, typeof (GrfEditorConfiguration).GetProperty("AppLastPath")), extra);
		}

		public static string OpenFileExtract(params string[] extra) {
			return TkPathRequest.OpenFile(ExtractSetting, extra);
		}

		public static string FolderEditor(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof (GrfEditorConfiguration).GetProperty("AppLastPath")), extra);
		}

		public static string FolderExtract(params string[] extra) {
			return TkPathRequest.Folder(ExtractSetting);
		}
	}
}