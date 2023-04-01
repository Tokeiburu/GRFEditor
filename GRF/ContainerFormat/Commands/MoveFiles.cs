using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GRF.ContainerFormat.Commands {
	internal class MoveFiles<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.AddFilesCallback _callback;
		private readonly List<MoveFile<TEntry>> _moveFiles = new List<MoveFile<TEntry>>();
		private readonly List<string> _newFiles;
		private readonly string _newPath;
		private readonly string _oldPath;

		/// <summary>
		/// This command looks similar to MoveFile, but it's different.
		/// The list of files are the files that will be moved from
		/// the old path to the new path. The file names shouldn't
		/// be absolute (ex : "map.rsw" but NOT "data\map.rsw")
		/// </summary>
		/// <param name="oldFolderPath">The old folder path.</param>
		/// <param name="newFolderPath">The new folder path.</param>
		/// <param name="newFiles">The new files.</param>
		/// <param name="callback"> </param>
		public MoveFiles(string oldFolderPath, string newFolderPath, IEnumerable<string> newFiles, CCallbacks.AddFilesCallback callback) {
			_oldPath = oldFolderPath;
			_newPath = newFolderPath;
			_callback = callback;
			_newFiles = newFiles.ToList();

			foreach (string file in _newFiles) {
				_moveFiles.Add(new MoveFile<TEntry>(Path.Combine(_oldPath, file), Path.Combine(_newPath, file), null));
			}
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			List<string> newFolders = new List<string>();

			foreach (MoveFile<TEntry> move in _moveFiles) {
				move.Execute(container);
				newFolders.Add(Path.GetDirectoryName(move.NewPath));
			}

			if (_callback != null)
				_callback(null, null, newFolders, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			foreach (MoveFile<TEntry> move in _moveFiles.Reverse<MoveFile<TEntry>>()) {
				move.Undo(container);
			}

			if (_callback != null)
				_callback(null, null, null, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.MoveFiles, _oldPath, _newPath); }
		}

		#endregion
	}
}