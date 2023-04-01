namespace GRF.ContainerFormat.Commands {
	internal class AddFolder<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.AddFolderCallback _callback;
		private readonly string _newName;

		/// <summary>
		/// Adding a folder has no effect on the GRF. If you want to create
		/// a new folder, use AddFile and refresh the TreeView if needed.
		/// </summary>
		/// <param name="newFolder"> full GRF path of the new path to create </param>
		/// <param name="callback">Callback </param>
		public AddFolder(string newFolder, CCallbacks.AddFolderCallback callback) {
			_newName = newFolder;
			_callback = callback;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			//var path = GrfPath.Combine(_newName, ".dir");
			//
			//container.Table.Add(path, "", false);
			//_entry = container.Table[path];
			//_entry.Modification |= Modification.Hidden;

			if (_callback != null)
				_callback(_newName, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			//if (_entry != null) {
			//	container.Table.DeleteEntry(_entry.RelativePath);
			//}

			if (_callback != null)
				_callback(_newName, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.AddFolder, _newName); }
		}

		#endregion
	}
}