using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.FileFormats.RgzFormat;
using Utilities.Extension;

namespace GRF.ContainerFormat.Commands {
	internal class AddFilesToRemove<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.AddFilesCallback _callback;
		private readonly List<TEntry> _conflicts = new List<TEntry>();

		private readonly List<string> _files;

		/// <summary>
		/// </summary>
		/// <param name="filesPath">The files or folders path (files will be added directly into the folder and
		/// the folders will be as well).</param>
		/// <param name="callback">The callback.</param>
		public AddFilesToRemove(IEnumerable<string> filesPath, CCallbacks.AddFilesCallback callback) {
			_files = filesPath.Select(p => Rgz.Root + p.ReplaceFirst("root\\", "")).ToList();
			_callback = callback;
		}

		public AddFilesToRemove(string filePath, CCallbacks.AddFilesCallback callback = null)
			: this(new string[] {filePath}, callback) {
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			List<string> newFolders = new List<string>();

			foreach (string entry in _files) {
				TEntry conflictEntry = container.Table.AddFileToRemove(entry);

				if (conflictEntry != null)
					_conflicts.Add(conflictEntry);

				newFolders.Add(Path.GetDirectoryName(entry));
			}

			if (_callback != null)
				_callback(null, _files.ToList(), newFolders, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			foreach (string entry in _files) {
				container.Table.DeleteEntry(entry);
			}

			foreach (TEntry entry in _conflicts) {
				container.Table.AddEntry(entry);
			}

			_conflicts.Clear();

			if (_callback != null)
				_callback(null, _files.ToList(), null, false);
		}

		public string CommandDescription {
			get {
				if (_files.Count == 1) {
					return string.Format(GrfStrings.AddFilesToRemove, _files[0]);
				}

				return string.Format(GrfStrings.AddFileToRemove);
			}
		}

		#endregion
	}
}