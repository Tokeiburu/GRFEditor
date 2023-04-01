// ReSharper disable CheckNamespace
namespace GRF.Core {
	public enum SavingMode {
		/// <summary>
		/// Save the container as a GRF archive. This flag creates a new temporary file and then overwrites the original one (if no issues occur).
		/// </summary>
		GrfSave,
		/// <summary>
		/// Save the container as a GRF archive. This flag edits the source GRF (default behavior of GRF Editor).
		/// </summary>
		QuickMerge,
		/// <summary>
		/// Decompress and then recompress all the data in the container.
		/// </summary>
		Repack,
		/// <summary>
		/// Flag reserved for Thor packing (do not use).
		/// </summary>
		RepackSource,
		/// <summary>
		/// Save the container as a RGZ archive.
		/// </summary>
		Rgz,
		/// <summary>
		/// Save the container as a Thor archive.
		/// </summary>
		Thor,
		/// <summary>
		/// Redirect all the identical files to the same index, saving space.
		/// </summary>
		Compact
	}
}