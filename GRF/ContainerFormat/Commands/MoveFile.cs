namespace GRF.ContainerFormat.Commands {
	internal class MoveFile<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.RenameCallback _callback;
		private readonly string _newPath;
		private readonly string _oldPath;
		private TEntry _entry;

		/// <summary>
		/// Moves files with their absolute path
		/// </summary>
		/// <param name="oldFullFileName">Old name of the file.</param>
		/// <param name="newFullFileName">New name of the file.</param>
		/// <param name="callback"> </param>
		public MoveFile(string oldFullFileName, string newFullFileName, CCallbacks.RenameCallback callback) {
			_oldPath = oldFullFileName;
			_newPath = newFullFileName;
			_callback = callback;
		}

		public string OldPath {
			get { return _oldPath; }
		}

		public string NewPath {
			get { return _newPath; }
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			_entry = container.Table.Rename(_oldPath, _newPath, true);

			if (_callback != null)
				_callback(_oldPath, _newPath, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			container.Table.Rename(_newPath, _oldPath, true);

			if (_entry != null) {
				container.Table.AddEntry(_entry);
			}

			if (_callback != null)
				_callback(_oldPath, _newPath, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.MoveFile, _oldPath, _newPath); }
		}

		#endregion
	}
}