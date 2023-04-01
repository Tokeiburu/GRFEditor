using System.Collections.Generic;

namespace GRF.ContainerFormat.Commands {
	internal class DeleteFiles<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.DeleteCallback _callback;
		private readonly List<string> _fileNames = new List<string>();

		public DeleteFiles(string entry, CCallbacks.DeleteCallback callback = null) : this(new string[] {entry}, callback) {
		}

		public DeleteFiles(IEnumerable<string> entries, CCallbacks.DeleteCallback callback) {
			_callback = callback;
			_fileNames = new List<string>(entries);
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			foreach (string file in _fileNames) {
				container.Table.DeleteFile(file);
			}

			if (_callback != null)
				_callback(_fileNames, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			foreach (string file in _fileNames) {
				container.Table.UndoDeleteFile(file);
			}

			if (_callback != null)
				_callback(_fileNames, false);
		}

		public string CommandDescription {
			get {
				if (_fileNames.Count == 1) {
					return string.Format(GrfStrings.DeleteFile, _fileNames[0]);
				}

				return string.Format(GrfStrings.DeleteFiles);
			}
		}

		#endregion
	}
}