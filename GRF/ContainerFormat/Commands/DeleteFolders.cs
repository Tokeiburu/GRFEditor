using System.Collections.Generic;
using System.Linq;

namespace GRF.ContainerFormat.Commands {
	internal class DeleteFolders<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.DeleteCallback _callback;
		private readonly List<string> _paths = new List<string>();

		public DeleteFolders(string grfPath, CCallbacks.DeleteCallback callback = null) : this(new string[] {grfPath}, callback) {
		}

		public DeleteFolders(IEnumerable<string> grfPaths, CCallbacks.DeleteCallback callback) {
			_callback = callback;
			_paths = grfPaths.ToList();
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			foreach (string file in _paths) {
				container.Table.DeleteFolder(file);
			}

			if (_callback != null)
				_callback(_paths, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			foreach (string file in _paths) {
				container.Table.UndoDeleteFolder(file);
			}

			if (_callback != null)
				_callback(_paths, false);
		}

		public string CommandDescription {
			get {
				if (_paths.Count == 1) {
					return string.Format(GrfStrings.DeleteFolder, _paths[0]);
				}

				return string.Format(GrfStrings.DeleteFolders);
			}
		}

		#endregion
	}
}