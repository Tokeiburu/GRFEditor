namespace GRF.ContainerFormat.Commands {
	internal class RenameFolder<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.RenameCallback _callback;
		private readonly string _newFolderName;
		private readonly string _oldFolderName;

		/// <summary>
		/// This method should NOT be used if the new folder already exists
		/// (if the new folder contains files with the same names, they
		/// won't be copied and undo'ing this operation will not be possible)
		/// So always checks if the new path doesn't exist already.
		/// </summary>
		/// <param name="oldName">The old name.</param>
		/// <param name="newName">The new name.</param>
		/// <param name="callback">Callback </param>
		public RenameFolder(string oldName, string newName, CCallbacks.RenameCallback callback) {
			_callback = callback;
			_oldFolderName = oldName;
			_newFolderName = newName;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			container.Table.RenameFolder(_oldFolderName, _newFolderName);

			if (_callback != null)
				_callback(_oldFolderName, _newFolderName, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			container.Table.RenameFolder(_newFolderName, _oldFolderName);

			if (_callback != null)
				_callback(_oldFolderName, _newFolderName, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.RenameFolder, _oldFolderName, _newFolderName); }
		}

		#endregion
	}
}