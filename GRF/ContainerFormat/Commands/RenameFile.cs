namespace GRF.ContainerFormat.Commands {
	internal class RenameFile<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.RenameCallback _callback;
		private readonly string _newFileName;
		private readonly string _oldFileName;
		private TEntry _conflictedEntry;

		public RenameFile(string oldFileName, string newName, CCallbacks.RenameCallback callback) {
			_oldFileName = oldFileName;
			_newFileName = newName;
			_callback = callback;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			_conflictedEntry = container.Table.Rename(_oldFileName, _newFileName, true);

			if (_callback != null)
				_callback(_oldFileName, _newFileName, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			container.Table.Rename(_newFileName, _oldFileName, false);

			if (_conflictedEntry != null)
				container.Table.AddEntry(_conflictedEntry);

			if (_callback != null)
				_callback(_oldFileName, _newFileName, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.RenameFile, _oldFileName, _newFileName); }
		}

		#endregion
	}
}