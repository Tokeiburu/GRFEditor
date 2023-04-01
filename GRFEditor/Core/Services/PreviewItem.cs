namespace GRFEditor.Core.Services {
	public struct PreviewItem {
		public string FileName { get; set; }
		public string Extension { get; set; }

		public bool IsNull() {
			return FileName == null && Extension == null;
		}
	}
}