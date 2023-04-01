using GRF.IO;
using Utilities.Services;

namespace GRF.ContainerFormat.Commands {
	internal class ReplaceFile<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly TEntry _dataEntry;
		private readonly CCallbacks.ReplaceFileCallback _callback;
		private readonly string _fileName;
		private readonly string _filePath;
		private readonly string _grfPath;
		private TEntry _conflictEntry;
		private readonly GrfMemoryStreamHolder _streamData;

		/// <summary>
		/// ReplaceFile is used to replace an entry with a file; it's similar to AddFile (which replaces the
		/// existing entry) however the filename of the old entry won't be modified.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="filePath">The file path.</param>
		/// <param name="callback">The callback.</param>
		public ReplaceFile(string grfPath, string fileName, string filePath, CCallbacks.ReplaceFileCallback callback) {
			_filePath = filePath;
			_callback = callback;
			_grfPath = EncodingService.CorrectPathExplode(grfPath);
			_fileName = EncodingService.CorrectPathExplode(fileName);
		}

		public ReplaceFile(string grfPath, string fileName, GrfMemoryStreamHolder streamData) {
			_grfPath = EncodingService.CorrectPathExplode(grfPath);
			_fileName = EncodingService.CorrectPathExplode(fileName);
			_streamData = streamData;
			//_rawData = data;
		}

		public ReplaceFile(string grfPath, string fileName, TEntry dataEntry) {
			_grfPath = EncodingService.CorrectPathExplode(grfPath);
			_fileName = EncodingService.CorrectPathExplode(fileName);
			_dataEntry = dataEntry;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			if (_dataEntry != null)
				_conflictEntry = container.Table.Replace(_grfPath, _dataEntry, _fileName);
			else if (_streamData != null)
				_conflictEntry = container.Table.Replace(_grfPath, _streamData, _fileName);
			else
				_conflictEntry = container.Table.Replace(_grfPath, _filePath, _fileName);

			if (_callback != null)
				_callback(_grfPath, _fileName, _filePath, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			container.Table.RemoveFile(_grfPath, _fileName);

			if (_conflictEntry != null) {
				container.Table.AddEntry(_conflictEntry);
				_conflictEntry = null;
			}

			if (_callback != null)
				_callback(_grfPath, _fileName, _filePath, false);
		}

		public string CommandDescription {
			get { return string.Format(GrfStrings.ReplaceFile, _fileName, _filePath ?? ""); }
		}

		#endregion
	}
}