using System.Collections.Generic;
using System.Linq;
using GRF.Core;

namespace GRF.ContainerFormat.Commands {
	internal class GravityDecryptFiles<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.EncryptFilesCallback _callback;
		private readonly List<TEntry> _entries = new List<TEntry>();
		private readonly string[] _files;

		public GravityDecryptFiles(IEnumerable<string> files, CCallbacks.EncryptFilesCallback callback) {
			_files = files.ToArray();
			_callback = callback;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			_entries.Clear();

			foreach (string file in _files) {
				_entries.Add(container.Table.GravityDecryptFile(file));
			}

			_callback?.Invoke(_files, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			_entries.ForEach(p => container.Table.DeleteEntry(p.RelativePath));
			_entries.ForEach(p => container.Table.AddEntry(p));

			_callback?.Invoke(_files, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.GravityDecryptFiles); }
		}

		#endregion
	}
}