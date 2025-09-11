using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GRF.Core;
using GRF.IO;
using GRF.GrfSystem;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.ContainerFormat {
	public class ContainerTable<TEntry> : IEnumerable<TEntry> where TEntry : ContainerEntry {
		protected TableDictionary<TEntry> _indexedEntries = new TableDictionary<TEntry>();
		private Dictionary<string, Stream> _lockedFiles = new Dictionary<string, Stream>();

		/// <summary>
		/// Gets the entry with the specified key.
		/// </summary>
		/// <returns>The entry.</returns>
		public virtual TEntry this[string key] {
			get { return _indexedEntries[key]; }
		}

		/// <summary>
		/// Gets the entries.
		/// </summary>
		public virtual List<TEntry> Entries {
			get { return _indexedEntries.Entries; }
		}

		/// <summary>
		/// Gets the files.
		/// </summary>
		public virtual HashSet<string> Files {
			get { return _indexedEntries.Files; }
		}

		/// <summary>
		/// Gets the directories.
		/// </summary>
		public virtual HashSet<string> Directories {
			get { return _indexedEntries.Directories; }
		}

		/// <summary>
		/// Gets the hidden directories.
		/// </summary>
		public virtual HashSet<string> HiddenDirectories {
			get { return _indexedEntries.HiddenDirectories; }
		}

		/// <summary>
		/// Gets the entries.
		/// </summary>
		public virtual List<KeyValuePair<string, TEntry>> FastAccessEntries {
			get { return _indexedEntries.FastAccessEntries; }
		}

		/// <summary>
		/// Gets the entries.
		/// </summary>
		public virtual List<(string Directory, string Filename, TEntry Entry)> FastTupleAccessEntries {
			get { return _indexedEntries.FastTupleAccessEntries; }
		}

		/// <summary>
		/// Gets the directory structure.
		/// </summary>
		public virtual TkDictionary<string, List<TEntry>> DirectoryStructure {
			get { return _indexedEntries.DirectoryStructure; }
		}

		/// <summary>
		/// Gets the number of entries.
		/// </summary>
		public virtual int Count {
			get { return _indexedEntries.Entries.Count; }
		}

		#region IEnumerable<TEntry> Members

		public virtual IEnumerator<TEntry> GetEnumerator() {
			return Entries.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Tries to get the entry.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Null if the entry is not found, otherwise the entry is returned.</returns>
		public virtual TEntry TryGet(string key) {
			TEntry res;

			if (_indexedEntries.TryGetValue(key, out res)) {
				return res;
			}

			return null;
		}

		/// <summary>
		/// Gets the files in the current directory, case insensitive.
		/// </summary>
		/// <param name="path">The current path.</param>
		/// <returns>A list of all the entries found.</returns>
		public List<string> GetFiles(string path) {
			return GetFiles(path, "", SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		/// Gets the files in the current directory, case insensitive.
		/// </summary>
		/// <param name="path">The current path.</param>
		/// <param name="searchPattern">The search pattern.</param>
		/// <returns>A list of all the entries found.</returns>
		public List<string> GetFiles(string path, string searchPattern) {
			return GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		/// Gets the files in the current directory, case insensitive.
		/// </summary>
		/// <param name="path">The current path.</param>
		/// <param name="searchPattern">The search pattern.</param>
		/// <param name="options">The options.</param>
		/// <returns>A list of all the entries found.</returns>
		public List<string> GetFiles(string path, string searchPattern, SearchOption options) {
			return GetFiles(path, searchPattern, options, true);
		}

		/// <summary>
		/// Gets the files in the current directory, case insensitive.
		/// </summary>
		/// <param name="path">The current path.</param>
		/// <param name="options">The options.</param>
		/// <returns>A list of all the entries found.</returns>
		public List<string> GetFiles(string path, SearchOption options) {
			return GetFiles(path, "", options, true);
		}

		/// <summary>
		/// Gets the files in the current directory.
		/// </summary>
		/// <param name="path">The current path.</param>
		/// <param name="searchPattern">The search pattern.</param>
		/// <param name="options">The options.</param>
		/// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
		/// <returns>A list of all the entries found.</returns>
		public virtual List<string> GetFiles(string path, string searchPattern, SearchOption options, bool ignoreCase) {
			if (searchPattern == null || searchPattern == "*")
				searchPattern = "";

			if (path == null)
				path = "";

			if (String.IsNullOrEmpty(path) && searchPattern == "")
				return Files.ToList();	// No need to search at all

			if (path != "")
				path = path.TrimEnd('\\') + "\\";
			
			if (ignoreCase)
				return FilesInDirectoryIgnoreCase(path, searchPattern, options);

			IEnumerable<string> files;

			if (options == SearchOption.AllDirectories) {
				files = Files.Where(file => file.StartsWith(path));
			}
			else {
				files = Files.Where(file => String.Compare(GrfPath.GetDirectoryNameKeepSlash(file), path, StringComparison.Ordinal) == 0);
			}

			if (searchPattern != "") {
				if (searchPattern.Contains("*") || searchPattern.Contains("?")) {
					Regex regex = new Regex(Methods.WildcardToRegex(searchPattern), RegexOptions.IgnoreCase);
					files = files.Where(p => regex.IsMatch(p));
				}
				else {
					files = files.Where(p => p.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1);
				}
			}

			return files.ToList();
		}

		/// <summary>
		/// Gets the files in the current directory.
		/// </summary>
		/// <param name="currentPath">The current path.</param>
		/// <param name="options">The options.</param>
		/// <returns>A list of all the entries found.</returns>
		[Obsolete("FilesInDirectory is deprecated, please use GetFiles instead.")]
		public List<string> FilesInDirectory(string currentPath, SearchOption options) {
			return GetFiles(currentPath, "", options, false);
		}

		/// <summary>
		/// Gets the files in the current directory.
		/// </summary>
		/// <param name="currentPath">The current path.</param>
		/// <param name="options">The options.</param>
		/// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
		/// <returns>A list of all the entries found.</returns>
		[Obsolete("FilesInDirectory is deprecated, please use GetFiles instead.")]
		public virtual List<string> FilesInDirectory(string currentPath, SearchOption options, bool ignoreCase) {
			if (ignoreCase)
				return FilesInDirectoryIgnoreCase(currentPath, "", options);

			if (String.IsNullOrEmpty(currentPath)) {
				return Files.ToList();
			}

			currentPath = currentPath.TrimEnd('\\') + "\\";

			List<string> files;

			if (options == SearchOption.AllDirectories) {
				files = Files.Where(file => file.StartsWith(currentPath)).ToList();
			}
			else {
				files = Files.Where(file => String.Compare(GrfPath.GetDirectoryNameKeepSlash(file), currentPath, StringComparison.Ordinal) == 0).ToList();
			}

			return files.ToList();
		}

		internal List<string> FilesInDirectoryIgnoreCase(string path, string searchPattern, SearchOption options) {
			if (searchPattern == null || searchPattern == "*")
				searchPattern = "";

			if (path == null)
				path = "";

			if (String.IsNullOrEmpty(path) && searchPattern == "")
				return Files.ToList();	// No need to search at all

			if (path != "")
				path = path.TrimEnd('\\') + "\\";

			HashSet<string> mappedFiles = _indexedEntries.Files;
			List<string> files;

			if (options == SearchOption.AllDirectories) {
				files = mappedFiles.Where(file => file.StartsWith(path, StringComparison.OrdinalIgnoreCase)).ToList();
			}
			else {
				files = mappedFiles.Where(file => String.Compare(GrfPath.GetDirectoryNameKeepSlash(file), path, StringComparison.OrdinalIgnoreCase) == 0).ToList();
			}

			files = (from file in files
			        where _indexedEntries.ContainsKey(file)
			        select _indexedEntries.GetMappedFile(file)).ToList();

			if (searchPattern != "") {
				if (searchPattern.Contains("*") || searchPattern.Contains("?")) {
					Regex regex = new Regex(Methods.WildcardToRegex(searchPattern), RegexOptions.IgnoreCase);
					files = files.Where(p => regex.IsMatch(p)).ToList();
				}
				else {
					files = files.Where(p => p.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1).ToList();
				}
			}

			return files;
		}

		/// <summary>
		/// Gets the entries in the current directory.
		/// </summary>
		/// <param name="currentPath">The current path.</param>
		/// <param name="options">The options.</param>
		/// <returns>A list of all the entries fround.</returns>
		public List<TEntry> EntriesInDirectory(string currentPath, SearchOption options) {
			return EntriesInDirectory(currentPath, options, false);
		}

		/// <summary>
		/// Gets the entries in the current directory.
		/// </summary>
		/// <param name="currentPath">The current path.</param>
		/// <param name="options">The options.</param>
		/// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
		/// <returns>A list of all the entries fround.</returns>
		public virtual List<TEntry> EntriesInDirectory(string currentPath, SearchOption options, bool ignoreCase) {
			if (ignoreCase)
				return EntriesInDirectoryIgnoreCase(currentPath, options);

			// Fix : 2015-07-20
			// Only return all entries if the option is set for all directories
			if (currentPath == null || (String.IsNullOrEmpty(currentPath) && options == SearchOption.AllDirectories)) {
				return Entries;
			}

			currentPath = currentPath.TrimEnd('\\');

			// Root files
			IEnumerable<TEntry> files = Entries.Where(file => file.DirectoryPath.Equals(currentPath)).ToList();

			if (options == SearchOption.AllDirectories) {
				// We add all subfolders
				currentPath += "\\";
				files = files.Concat(Entries.Where(file => file.DirectoryPath.StartsWith(currentPath))).Distinct();
			}

			return files.ToList();
		}

		internal List<TEntry> EntriesInDirectoryIgnoreCase(string currentPath, SearchOption options) {
			// Fix : 2015-07-20
			// Only return all entries if the option is set for all directories
			if (currentPath == null || (String.IsNullOrEmpty(currentPath) && options == SearchOption.AllDirectories)) {
				return Entries;
			}

			currentPath = currentPath.TrimEnd('\\').ToLowerInvariant();

			// Root files
			IEnumerable<TEntry> entries = Entries.Where(file => file.DirectoryPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase)).ToList();

			if (options == SearchOption.AllDirectories) {
				// We add all subfolders
				currentPath += "\\";
				entries = entries.Concat(Entries.Where(file => file.DirectoryPath.StartsWith(currentPath, StringComparison.OrdinalIgnoreCase))).Distinct();
			}

			return entries.ToList();
		}

		internal virtual void Delete() {
			if (_indexedEntries != null) {
				_indexedEntries.Values.ToList().ForEach(p => p.Delete());
				_indexedEntries.Clear();
				_indexedEntries = null;
			}

			if (_lockedFiles != null) {
				_lockedFiles.Values.ToList().ForEach(p => p.Close());
				_lockedFiles.Clear();
				_lockedFiles = null;
			}
		}

		internal virtual TEntry EncryptFile(string fileName) {
			TEntry oldEntry = _indexedEntries[fileName];
			TEntry entry = (TEntry) oldEntry.Copy();
			entry.Modification &= ~Modification.Decrypt;
			entry.Modification |= Modification.Encrypt;
			_indexedEntries[fileName] = entry;
			return oldEntry;
		}

		internal virtual TEntry DecryptFile(string fileName) {
			TEntry oldEntry = _indexedEntries[fileName];
			TEntry entry = (TEntry) oldEntry.Copy();
			entry.Modification &= ~Modification.Encrypt;
			entry.Modification |= Modification.Decrypt;
			_indexedEntries[fileName] = entry;
			return oldEntry;
		}

		/// <summary>
		/// Adds an entry directly, without checking anything first (should only be used by IGrfCommands)<para></para>
		/// Manipulating the file table directly may cause unwanted results.
		/// </summary>
		/// <param name="entry">The entry.</param>
		internal virtual void AddEntry(TEntry entry) {
			AddEntry(entry.RelativePath, entry);
		}

		/// <summary>
		/// Adds an entry directly, without checking anything first (should only be used by IGrfCommands)<para></para>
		/// Manipulating the file table directly may cause unwanted results.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="entry">The entry.</param>
		internal virtual void AddEntry(string key, TEntry entry) {
			_indexedEntries[key] = entry;

			if ((entry.Modification & Modification.Added) == Modification.Added && entry.SourceFilePath != null)
				_addLockedFile(entry.SourceFilePath);
		}

		/// <summary>
		/// Determines whether the file table contains the filename specified.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>
		///   <c>true</c> if the file table contains the entry
		/// </returns>
		internal virtual bool Contains(string filename) {
			return _indexedEntries.ContainsKey(filename);
		}

		internal virtual bool ContainsKey(string key) {
			return _indexedEntries.ContainsKey(key);
		}

		/// <summary>
		/// Determines whether the specified GRF path exists in the container.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <returns>
		///   <c>true</c> if the specified GRF path exists in the container; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool ContainsDirectory(string grfPath) {
			grfPath = grfPath.TrimEnd('\\', '/');
			return _indexedEntries.Directories.Contains(grfPath);
		}

		/// <summary>
		/// Determines whether the specified GRF file exists in the container.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <returns>
		///   <c>true</c> if the specified GRF file exists in the container; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool ContainsFile(string grfFile) {
			return _indexedEntries.ContainsKey(grfFile) && !_indexedEntries[grfFile].IsRemoved;
		}

		/// <summary>
		/// Determines whether the specified GRF file exists in the container.<para></para>
		/// Ignores the Removed flag.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <returns>
		///   <c>true</c> if the specified GRF file exists in the container; otherwise, <c>false</c>.
		/// </returns>
		public bool InternalContains(string grfFile) {
			return _indexedEntries.ContainsKey(grfFile);
		}

		internal virtual TEntry Add(string grfPath, string sourceFileName, bool overwrite) {
			TEntry entry = (TEntry) Activator.CreateInstance(typeof (TEntry));

			entry.RelativePath = EncodingService.FromAnyToDisplayEncoding(grfPath);
			entry.Flags = EntryType.File;
			entry.SourceFilePath = sourceFileName;
			entry.Modification |= Modification.Added;

			_addLockedFile(sourceFileName);

			if (_indexedEntries.ContainsKey(entry.RelativePath)) {
				if (overwrite) {
					TEntry conflictEntry = _indexedEntries[entry.RelativePath];
					_indexedEntries[entry.RelativePath] = entry;
					return conflictEntry;
				}
			}
			else {
				_indexedEntries[entry.RelativePath] = entry;
			}

			return null;
		}

		/// <summary>
		/// Undoes the virtual delete file.
		/// </summary>
		/// <param name="file">The file name (with path).</param>
		internal virtual void UndoDeleteFile(string file) {
			if (_indexedEntries.ContainsKey(file)) {
				TEntry entry = _indexedEntries[file];
				entry.RemovedFlagCount--;

				if (entry.RemovedFlagCount <= 0) {
					_indexedEntries[file].Modification &= ~Modification.Removed;
					entry.RemovedFlagCount = 0;
				}

				if ((entry.Modification & Modification.Added) == Modification.Added && entry.SourceFilePath != null)
					_addLockedFile(entry.SourceFilePath);
			}
		}

		/// <summary>
		/// Deletes the folder virtually.
		/// </summary>
		/// <param name="folder">The folder.</param>
		internal virtual void DeleteFolder(string folder) {
			foreach (TEntry entry in EntriesInDirectory(folder, SearchOption.AllDirectories, false)) {
				DeleteFile(entry.RelativePath);
			}
		}

		/// <summary>
		/// Undoes the virtual delete folder.
		/// </summary>
		/// <param name="folder">The folder.</param>
		internal virtual void UndoDeleteFolder(string folder) {
			// Fix : 2015-03-31
			// EntriesInDirectory should not be used for this method
			// since it doesn't return deleted entries (on purpose)
			folder = folder.TrimEnd('\\') + '\\';
			foreach (TEntry entry in _indexedEntries.Values.Where(p => p.RelativePath.StartsWith(folder))) {
				UndoDeleteFile(entry.RelativePath);
			}
		}

		/// <summary>
		/// Renames the specified current file name.
		/// </summary>
		/// <param name="currentFileName">Name of the current file.</param>
		/// <param name="newFileName">New name of the file.</param>
		/// <param name="overwriteIfFileExists">if set to <c>true</c> [overwrite if F ile exists].</param>
		/// <returns>
		/// The conflicted TEntry, only if overwriteIfFileExists is set to true (null otherwise)
		/// </returns>
		internal virtual TEntry Rename(string currentFileName, string newFileName, bool overwriteIfFileExists) {
			if (_indexedEntries.ContainsKey(currentFileName)) {
				TEntry currentEntry = _indexedEntries[currentFileName];
				currentEntry.RelativePath = newFileName.ToDisplayEncoding();

				if (_indexedEntries.ContainsKey(newFileName) == false) {
					_indexedEntries.Remove(currentFileName);
					_indexedEntries.Add(newFileName, currentEntry);
					return null;
				}

				if (overwriteIfFileExists) {
					TEntry conflictEntry = _indexedEntries[newFileName];
					_indexedEntries.Remove(currentFileName);
					_indexedEntries[newFileName] = currentEntry;
					return conflictEntry;
				}
			}

			return null;
		}

		/// <summary>
		/// Renames the folder.
		/// </summary>
		/// <param name="oldFolderName">Old name of the folder.</param>
		/// <param name="newFolderName">New name of the folder.</param>
		internal virtual void RenameFolder(string oldFolderName, string newFolderName) {
			List<TEntry> entries = EntriesInDirectory(oldFolderName, SearchOption.AllDirectories, false);
			foreach (TEntry entry in entries) {
				Rename(entry.RelativePath, entry.RelativePath.ReplaceFirst(oldFolderName, newFolderName), false);
			}
		}

		/// <summary>
		/// Merges the folders.
		/// </summary>
		/// <param name="source">Old name of the folder.</param>
		/// <param name="destination">New name of the folder.</param>
		/// <param name="conflicts">List of conflicted entries.</param>
		/// <param name="movedEntries">List of moved entries.</param>
		internal virtual void MergeFolder(string source, string destination, out List<TEntry> conflicts, out List<TEntry> movedEntries) {
			List<TEntry> entries = EntriesInDirectory(source, SearchOption.AllDirectories, false);
			movedEntries = entries;

			conflicts = new List<TEntry>();

			foreach (TEntry entry in entries) {
				TEntry conflict = Rename(entry.RelativePath, entry.RelativePath.ReplaceFirst(source, destination), true);

				if (conflict != null)
					conflicts.Add(conflict);
			}
		}

		/// <summary>
		/// Undo the merge folder.
		/// </summary>
		/// <param name="source">Old name of the folder.</param>
		/// <param name="destination">New name of the folder.</param>
		/// <param name="conflicts">List of conflicted entries.</param>
		/// <param name="movedEntries">List of moved entries.</param>
		internal virtual void UndoMergeFolder(string source, string destination, List<TEntry> conflicts, List<TEntry> movedEntries) {
			foreach (TEntry entry in movedEntries) {
				Rename(entry.RelativePath, entry.RelativePath.ReplaceFirst(destination, source), true);
			}

			foreach (TEntry entry in conflicts) {
				_indexedEntries.Add(entry.RelativePath, entry);
			}
		}

		/// <summary>
		/// Removes the file from the table (only works if the TEntry has the Added modifier)
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="filePath">The file path.</param>
		internal virtual void RemoveFile(string grfPath, string filePath) {
			TEntry entry = _indexedEntries[GrfPath.Combine(grfPath, Path.GetFileName(EncodingService.CorrectFileName(filePath)))];

			entry.RawDataSource = null;

			if (entry.Modification.HasFlags(Modification.Added))
				_indexedEntries.Remove(entry.RelativePath);

			if ((entry.Modification & Modification.Added) == Modification.Added && entry.SourceFilePath != null)
				_releaseLockedFile(entry.SourceFilePath);
		}

		/// <summary>
		/// Deletes an entry directly, without checking anything first (should only be used by IGRFCommands)
		/// (Why? Because the file is really deleted from the table, other methods only change their status
		/// by putting the Removed flag). Manipulating the file table directly may cause unwanted results.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		internal virtual void DeleteEntry(string fileName) {
			if (_indexedEntries.ContainsKey(fileName)) {
				TEntry entry = _indexedEntries[fileName];

				if ((entry.Modification & Modification.Added) == Modification.Added && entry.SourceFilePath != null)
					_releaseLockedFile(entry.SourceFilePath);

				_indexedEntries.Remove(fileName);
			}
		}

		protected void _addLockedFile(string path) {
			if (!Settings.LockFiles) return;

			try {
				if (File.Exists(path))
					_lockedFiles[path] = File.OpenRead(path);
			}
			catch {
			}
		}

		protected void _releaseLockedFile(string path) {
			if (_lockedFiles.ContainsKey(path)) {
				_lockedFiles[path].Close();
				_lockedFiles.Remove(path);
			}
		}

		internal void InvalidateInternalSets() {
			_indexedEntries.HasBeenModified = true;
		}

		internal virtual TEntry Replace(string grfPath, string filePath, string fileName) {
			TEntry entry = (TEntry) Activator.CreateInstance(typeof (TEntry));

			entry.RelativePath = GrfPath.Combine(grfPath, EncodingService.CorrectFileName(fileName) ?? Path.GetFileName(EncodingService.CorrectFileName(filePath)));
			entry.Flags = EntryType.File;
			entry.SourceFilePath = filePath;
			entry.Modification |= Modification.Added;

			_addLockedFile(filePath);

			if (_indexedEntries.ContainsKey(entry.RelativePath)) {
				TEntry conflictEntry = _indexedEntries[entry.RelativePath];
				_indexedEntries[entry.RelativePath] = entry;
				return conflictEntry;
			}

			_indexedEntries[entry.RelativePath] = entry;
			return null;
		}

		internal virtual TEntry Replace(string grfPath, TEntry dataEntry, string fileName) {
			TEntry entry = (TEntry)Activator.CreateInstance(typeof(TEntry));

			entry.RelativePath = GrfPath.Combine(grfPath, EncodingService.CorrectFileName(fileName));
			entry.Flags = EntryType.File;
			entry.SourceFilePath = GrfStrings.DataStreamId;
			entry.Modification |= Modification.Added;
			entry.RawDataSource = dataEntry;

			if (_indexedEntries.ContainsKey(entry.RelativePath)) {
				TEntry conflictEntry = _indexedEntries[entry.RelativePath];
				_indexedEntries[entry.RelativePath] = entry;
				return conflictEntry;
			}

			_indexedEntries[entry.RelativePath] = entry;
			return null;
		}

		internal virtual TEntry Replace(string grfPath, GrfMemoryStreamHolder data, string fileName) {
			TEntry entry = (TEntry)Activator.CreateInstance(typeof(TEntry));

			entry.RelativePath = GrfPath.Combine(grfPath, EncodingService.CorrectFileName(fileName));
			entry.Flags = EntryType.File;
			entry.SourceFilePath = GrfStrings.DataStreamId;
			entry.Modification |= Modification.Added;
			entry.RawDataSource = data;

			if (_indexedEntries.ContainsKey(entry.RelativePath)) {
				TEntry conflictEntry = _indexedEntries[entry.RelativePath];
				_indexedEntries[entry.RelativePath] = entry;
				return conflictEntry;
			}

			_indexedEntries[entry.RelativePath] = entry;
			return null;
		}

		internal virtual TEntry Replace(string grfPath, Stream data, string fileName) {
			TEntry entry = (TEntry) Activator.CreateInstance(typeof (TEntry));

			entry.RelativePath = GrfPath.Combine(grfPath, EncodingService.CorrectFileName(fileName));
			entry.Flags = EntryType.File;
			entry.SourceFilePath = GrfStrings.DataStreamId;
			entry.Modification |= Modification.Added;
			entry.RawDataSource = data;

			if (_indexedEntries.ContainsKey(entry.RelativePath)) {
				TEntry conflictEntry = _indexedEntries[entry.RelativePath];
				_indexedEntries[entry.RelativePath] = entry;
				return conflictEntry;
			}

			_indexedEntries[entry.RelativePath] = entry;
			return null;
		}

		/// <summary>
		/// Deletes the file virtually (keeps the metadata).
		/// </summary>
		/// <param name="file">The file name (with path).</param>
		public virtual void DeleteFile(string file) {
			if (_indexedEntries.ContainsKey(file)) {
				TEntry entry = _indexedEntries[file];
				entry.Modification |= Modification.Removed;
				entry.RemovedFlagCount++;

				if ((entry.Modification & Modification.Added) == Modification.Added && entry.SourceFilePath != null)
					_releaseLockedFile(entry.SourceFilePath);
			}
		}

		internal virtual TEntry AddFileToRemove(string grfPath) {
			TEntry entry = (TEntry) Activator.CreateInstance(typeof (TEntry));

			entry.RelativePath = EncodingService.FromAnyToDisplayEncoding(grfPath);
			entry.Flags = EntryType.File | EntryType.RemoveFile;

			if (_indexedEntries.ContainsKey(entry.RelativePath)) {
				TEntry conflictEntry = _indexedEntries[entry.RelativePath];
				_indexedEntries[entry.RelativePath] = entry;
				return conflictEntry;
			}

			_indexedEntries[entry.RelativePath] = entry;
			return null;
		}
	}
}