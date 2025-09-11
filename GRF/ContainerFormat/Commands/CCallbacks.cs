using System.Collections.Generic;

namespace GRF.ContainerFormat.Commands {
	/// <summary>
	/// Callback delegates used for the container commands.
	/// </summary>
	public static class CCallbacks {
		#region Delegates

		/// <summary>
		/// Callback of AddFiles
		/// </summary>
		/// <param name="filesFullPaths">The files full paths.</param>
		/// <param name="grfFullPaths">The GRF files full paths.</param>
		/// <param name="grfPaths">The new GRF folders created.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void AddFilesCallback(List<string> filesFullPaths, List<string> grfFullPaths, List<string> grfPaths, bool isExecuted);

		/// <summary>
		/// Callback of AddFolder
		/// </summary>
		/// <param name="newFolderName">New name of the folder.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void AddFolderCallback(string newFolderName, bool isExecuted);

		/// <summary>
		/// Callback of ChangeVersion
		/// </summary>
		/// <param name="major">The major.</param>
		/// <param name="minor">The minor.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void ChangeVersionCallback(byte major, byte minor, bool isExecuted);

		/// <summary>
		/// Callback of ChangeHeader
		/// </summary>
		/// <param name="header">The header.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void ChangeHeaderCallback(string header, bool isExecuted);

		/// <summary>
		/// Callback of Delete
		/// </summary>
		/// <param name="paths">The paths.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void DeleteCallback(List<string> paths, bool isExecuted);

		/// <summary>
		/// Callback of Encrypt
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void EncryptFilesCallback(string[] fileName, bool isExecuted);

		/// <summary>
		/// Callback of Rename
		/// </summary>
		/// <param name="oldFileName">Old name of the file.</param>
		/// <param name="newName">The new name.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void RenameCallback(string oldFileName, string newName, bool isExecuted);

		/// <summary>
		/// Callback of Replace
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="filePath">The file path.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void ReplaceFileCallback(string grfPath, string fileName, string filePath, bool isExecuted);

		#endregion
	}
}