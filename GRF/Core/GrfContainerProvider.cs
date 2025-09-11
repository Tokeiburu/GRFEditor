using GRF.FileFormats.RgzFormat;
using GRF.FileFormats.ThorFormat;
using Utilities.Extension;

namespace GRF.Core {
	public class GrfLoadData {
		public byte[] EncryptionKey {get; set; }
		public bool DecryptFileTable { get; set; }
	}

	/// <summary>
	/// Retrieves a GRF container from various type of files.
	/// </summary>
	internal static class GrfContainerProvider {
		/// <summary>
		/// Gets the specified file name.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="loadData">Optional loading parameters.</param>
		/// <returns>The GRF container.</returns>
		public static Container Get(string fileName, GrfLoadData loadData = null) {
			switch (fileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					return new Container(fileName, loadData);
				case ".rgz":
					using (var file = new Rgz(fileName)) {
						return file.ToGrfContainer();
					}
				case ".thor":
					using (var file = new Thor(fileName)) {
						return file.ToGrfContainer();
					}
				default:
					return new Container(fileName);
			}
		}
	}
}