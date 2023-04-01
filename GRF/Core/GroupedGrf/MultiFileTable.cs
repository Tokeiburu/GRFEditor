using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.Core.GroupedGrf {
	public class MultiFileTable : FileTable {
		private readonly TableDictionary<FileEntry> _bufferedEntries = new TableDictionary<FileEntry>();
		private readonly MultiGrfReader _multiGrfHolder;
		private bool? _hasProcessed;

		public MultiFileTable(MultiGrfReader multiGrfHolder) : base(null) {
			_multiGrfHolder = multiGrfHolder;
		}

		public override sealed int Count {
			get { throw GrfExceptions.__UnsupportedAction.Create(); }
		}

		public override sealed FileEntry this[string key] {
			get {
				if (_hasProcessed == null) {
					foreach (string gPath in _multiGrfHolder.Paths) {
						var path = gPath;

						if (Directory.Exists(path)) {
							path = Path.GetDirectoryName(path);

							if (!File.Exists(GrfPath.Combine(path, key)) &&
							    File.Exists(GrfPath.Combine(path, EncodingService.FromAnyTo(key, EncodingService.Ansi)))) {
								key = EncodingService.FromAnyTo(key, EncodingService.Ansi);
							}

							if (File.Exists(GrfPath.Combine(path, key))) {
								FileEntry tempEntry = new FileEntry();
								tempEntry.Modification = Modification.Added;
								tempEntry.SourceFilePath = GrfPath.Combine(path, key);
								tempEntry.RelativePath = key;
								tempEntry.Header = _header;

								return tempEntry;
							}
						}
						else if (_multiGrfHolder.Containers.ContainsKey(path)) {
							GrfHolder grf = _multiGrfHolder.Containers[path];
							var res = grf.FileTable.TryGet(key) ?? grf.FileTable.TryGet(GrfStrings.RgzRoot + key);

							if (res != null)
								return res;
						}
					}

					return null;
				}

				if (_hasProcessed == false) {
					_generateBuffers();
					return this[key];
				}

				if (_hasProcessed == true) {
					if (_bufferedEntries.ContainsKey(key))
						return _bufferedEntries[key];
				}

				return null;
			}
		}

		public override sealed List<FileEntry> Entries {
			get { return base.Entries; }
		}

		public override sealed HashSet<string> Files {
			get { throw GrfExceptions.__UnsupportedAction.Create(); }
		}

		public override sealed HashSet<string> Directories {
			get { throw GrfExceptions.__UnsupportedAction.Create(); }
		}

		public override HashSet<string> HiddenDirectories {
			get { return new HashSet<string>(); }
		}

		public override sealed List<KeyValuePair<string, FileEntry>> FastAccessEntries {
			get { throw GrfExceptions.__UnsupportedAction.Create(); }
		}

		public override sealed List<Tuple<string, string, FileEntry>> FastTupleAccessEntries {
			get { throw GrfExceptions.__UnsupportedAction.Create(); }
		}

		internal override sealed FileEntry Add(string grfPath, string sourceFileName, bool overwrite) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void UndoDeleteFile(string file) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void DeleteFolder(string folder) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void UndoDeleteFolder(string folder) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed FileEntry Rename(string currentFileName, string newFileName, bool overwriteIfFileExists) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void RenameFolder(string oldFolderName, string newFolderName) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void MergeFolder(string source, string destination, out List<FileEntry> conflicts, out List<FileEntry> movedEntries) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void UndoMergeFolder(string source, string destination, List<FileEntry> conflicts, List<FileEntry> movedEntries) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void RemoveFile(string grfPath, string filePath) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void DeleteEntry(string fileName) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed FileEntry Replace(string grfPath, string filePath, string fileName) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		public override sealed void DeleteFile(string file) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed FileEntry AddFileToRemove(string grfPath) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		private void _generateBuffers() {
			_bufferedEntries.Clear();

			foreach (string gPath in _multiGrfHolder.Paths.Reverse()) {
				var path = gPath;

				if (Directory.Exists(path)) {
					var cleanPath = GrfPath.GetDirectoryName(gPath).TrimEnd('/', '\\');

					if (!Directory.Exists(cleanPath)) continue;

					foreach (string file in Directory.GetFiles(cleanPath, "*", SearchOption.AllDirectories)) {
						FileEntry tempEntry = new FileEntry();
						tempEntry.Modification = Modification.Added;
						tempEntry.SourceFilePath = file;
						tempEntry.RelativePath = file.Substring(cleanPath.Length + 1);
						tempEntry.Header = _header;
						_bufferedEntries[tempEntry.RelativePath] = tempEntry;
					}
				}
				else if (_multiGrfHolder.Containers.ContainsKey(path)) {
					GrfHolder grf = _multiGrfHolder.Containers[path];

					foreach (var entry in grf.FileTable.Entries) {
						_bufferedEntries[entry.RelativePath] = entry;
					}
				}
			}

			_hasProcessed = true;
		}

		public override sealed FileEntry TryGet(string key) {
			return this[key];
		}

		public override sealed IEnumerator<FileEntry> GetEnumerator() {
			return base.GetEnumerator();
		}

		public override sealed List<FileEntry> FindEntriesFromFileName(string fileName) {
			List<FileEntry> entries = new List<FileEntry>();
			fileName = "\\" + fileName.Trim('\\', '/');

			if (_hasProcessed == false) {
				_generateBuffers();
			}

			if (_hasProcessed == true) {
				HashSet<string> mappedFiles = _bufferedEntries.Files;
				return mappedFiles.Where(file => file.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)).Select(p => _bufferedEntries[p]).ToList();
			}

			foreach (string gPath in _multiGrfHolder.Paths) {
				var path = gPath;

				if (Directory.Exists(path)) {
					var cleanPath = GrfPath.GetDirectoryName(gPath).TrimEnd('/', '\\');

					if (!Directory.Exists(cleanPath)) continue;

					foreach (string file in Directory.GetFiles(cleanPath, "*", SearchOption.AllDirectories).Where(p => p.EndsWith(fileName))) {
						FileEntry tempEntry = new FileEntry();
						tempEntry.Modification = Modification.Added;
						tempEntry.SourceFilePath = file;
						tempEntry.RelativePath = file.Substring(cleanPath.Length + 1);
						tempEntry.Header = _header;

						entries.Add(tempEntry);
					}
				}
				else if (_multiGrfHolder.Containers.ContainsKey(path)) {
					GrfHolder grf = _multiGrfHolder.Containers[path];
					entries.AddRange(grf.FileTable.FindEntriesFromFileName(fileName));
				}
			}

			return entries;
		}

		public override sealed List<string> FilesInDirectory(string directory, SearchOption option, bool ignoreCase) {
			ignoreCase = true;
			HashSet<string> files = new HashSet<string>();

			if (_hasProcessed == false) {
				_generateBuffers();
			}

			if (_hasProcessed == true) {
				HashSet<string> mappedFiles = _bufferedEntries.Files;
				var currentPath = directory.TrimEnd('\\');

				// Root files
				IEnumerable<string> files2 = mappedFiles.Where(file => String.Compare(file, currentPath, StringComparison.OrdinalIgnoreCase) == 0).ToList();

				if (option == SearchOption.AllDirectories) {
					// We add all subfolders
					currentPath += "\\";
					files2 = files2.Concat(mappedFiles.Where(file => GrfPath.GetDirectoryName(file).StartsWith(currentPath, StringComparison.OrdinalIgnoreCase)));
				}

				return files2.Select(p => _bufferedEntries[p].RelativePath).Distinct().ToList();
			}

			foreach (string gPath in _multiGrfHolder.Paths.Reverse()) {
				var path = gPath;

				if (Directory.Exists(path)) {
					var cleanPath = GrfPath.GetDirectoryName(gPath).TrimEnd('/', '\\');
					var tDirectory = EncodingService.ConvertStringToAnsi(directory);

					if (!Directory.Exists(GrfPath.Combine(cleanPath, tDirectory))) continue;

					foreach (string file in Directory.GetFiles(GrfPath.Combine(cleanPath, tDirectory), "*", option)) {
						files.Add(file.Substring(cleanPath.Length + 1));
					}
				}
				else if (_multiGrfHolder.Containers.ContainsKey(path)) {
					GrfHolder grf = _multiGrfHolder.Containers[path];

					foreach (string file in grf.FileTable.GetFiles(directory, null, option, ignoreCase))
						files.Add(file);
				}
			}

			return files.ToList();
		}

		public override sealed List<FileEntry> EntriesInDirectory(string currentPath, SearchOption options, bool ignoreCase) {
			ignoreCase = true;
			return base.EntriesInDirectory(currentPath, options, ignoreCase);
		}

		internal override sealed void Delete() {
			base.Delete();
		}

		internal override sealed FileEntry EncryptFile(string fileName) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed FileEntry DecryptFile(string fileName) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void AddEntry(FileEntry entry) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed void AddEntry(string key, FileEntry entry) {
			throw GrfExceptions.__UnsupportedAction.Create();
		}

		internal override sealed bool Contains(string filename) {
			return this[filename] != null;
		}

		internal override bool ContainsKey(string key) {
			return this[key] != null;
		}

		public override sealed bool ContainsDirectory(string grfPath) {
			return Directories.Contains(grfPath, StringComparer.OrdinalIgnoreCase);
		}

		public override sealed bool ContainsFile(string grfFile) {
			return this[grfFile] != null;
		}

		public void Lock() {
			_hasProcessed = false;
		}

		public void Unlock() {
			_hasProcessed = null;
		}
	}
}