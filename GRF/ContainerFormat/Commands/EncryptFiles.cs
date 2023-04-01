using System.Collections.Generic;
using System.Linq;
using GRF.Core;

namespace GRF.ContainerFormat.Commands {
	internal class EncryptFiles<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.EncryptFilesCallback _callback;
		private readonly List<TEntry> _entries = new List<TEntry>();
		private readonly string[] _files;
		private bool _isEncrypted;

		public EncryptFiles(IEnumerable<string> files, CCallbacks.EncryptFilesCallback callback) {
			_files = files.ToArray();
			_callback = callback;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			_isEncrypted = ((GrfHeader) container.Header).IsEncrypted;
			((GrfHeader) container.Header).IsEncrypted = true;

			_entries.Clear();

			foreach (string file in _files) {
				_entries.Add(container.Table.EncryptFile(file));
			}

			if (_callback != null)
				_callback(_files, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			((GrfHeader) container.Header).IsEncrypted = _isEncrypted;

			_entries.ForEach(p => container.Table.DeleteEntry(p.RelativePath));
			_entries.ForEach(p => container.Table.AddEntry(p));

			if (_callback != null)
				_callback(_files, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.EncryptFiles); }
		}

		#endregion
	}
}