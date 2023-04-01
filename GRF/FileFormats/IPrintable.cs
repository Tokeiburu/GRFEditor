namespace GRF.FileFormats {
	public interface IPrintable {
		/// <summary>
		/// Prints an object to a text format.
		/// </summary>
		/// <returns>A string containing the object parsed to a text format.</returns>
		string GetInformation();
	}
}