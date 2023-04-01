using System.Collections.Generic;

namespace GRF.ContainerFormat.Commands {
	public static class CCallbacks {
		#region Delegates

		/// <summary>
		/// Callback of AddFiles
		/// </summary>
		/// <param name="filesFullPaths">The files full paths </param>
		/// <param name="grfFullPaths">The GRF files full paths.</param>
		/// <param name="grfPaths">The new GRF folders created.</param>
		/// <param name="isExecuted">if set to <c>true</c> [is executed].</param>
		public delegate void AddFilesCallback(List<string> filesFullPaths, List<string> grfFullPaths, List<string> grfPaths, bool isExecuted);

		public delegate void AddFolderCallback(string newFolderName, bool isExecuted);

		public delegate void ChangeVersionCallback(byte major, byte minor, bool isExecuted);

		public delegate void DeleteCallback(List<string> paths, bool isExecuted);

		public delegate void EncryptFilesCallback(string[] fileName, bool isExecuted);

		public delegate void RenameCallback(string oldFileName, string newName, bool isExecuted);

		public delegate void ReplaceFileCallback(string grfPath, string fileName, string filePath, bool isExecuted);

		#endregion
	}
}