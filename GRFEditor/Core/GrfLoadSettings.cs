namespace GRFEditor.Core {
	public class GrfLoadSettings {
		public bool DecryptFileTable { get; set; }
		public string FileName { get; set; }
		public byte[] EncryptionKey { get; set; }
	}
}