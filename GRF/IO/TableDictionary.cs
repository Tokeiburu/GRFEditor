using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Core;
using Utilities;
using Utilities.Extension;

namespace GRF.IO {
	/// Two ways of doing it : 
	/// 1) 
	/// LowerUpperCase to LowerOnlyCase		subMapper
	/// LowerOnlyCase to LowerUpperCase		mapper
	/// LowerUpperCase to value				base
	/// 
	/// 2)
	/// LowerUpperCase to LowerOnlyCase		mapper
	/// LowerOnlyCase to value				base
	/// - Offers buffered items!
	/// The option 2 is faster.
	public class TableDictionary<TEntry> : Dictionary<string, TEntry> where TEntry : ContainerEntry {
		private List<KeyValuePair<string, TEntry>> _fastAccessEntries;
		private List<TEntry> _fastEntries;
		private List<Tuple<string, string, TEntry>> _fastTupleAccessEntries;
		private TkDictionary<string, List<EntrySearchNode<TEntry>>> _directoryStructure = new TkDictionary<string, List<EntrySearchNode<TEntry>>>();
		private HashSet<string> _files;
		private HashSet<string> _directories;
		private HashSet<string> _hiddenDirectories;
		private bool _hasBeenModified;
		private HashSet<string> _filesWithHidden;
		//private Dictionary<string, string> _mapper = new Dictionary<string, string>();

		public TableDictionary() : base(StringComparer.OrdinalIgnoreCase) {
		}

		internal bool HasBeenModified {
			set {
				if (value && _hasBeenModified == false) {
					_files = null;
					_fastEntries = null;
					_fastAccessEntries = null;
					_fastTupleAccessEntries = null;
					_directories = null;
					_directoryStructure = null;
					_filesWithHidden = null;
				}

				_hasBeenModified = value;
			}
		}

		public new TEntry this[string key] {
			get { return base[key]; }
			set {
				base[key] = value;
				HasBeenModified = true;
			}
		}

		internal void SetQuick(string key, TEntry value) {
			base.Add(key, value);
		}

		internal TEntry GetQuick(string key) {
			return base[key];
		}

		public HashSet<string> Files {
			get {
				if (_files == null) {
					_files = new HashSet<string>();

					foreach (string file in Values.Where(p => (p.Modification & Modification.Removed) != Modification.Removed).Select(p => p.RelativePath)) {
						_files.Add(file);
					}

					_hasBeenModified = false;
				}
				return _files;
			}
		}

		internal HashSet<string> FilesWithHidden {
			get {
				if (_filesWithHidden == null) {
					_filesWithHidden = new HashSet<string>();

					foreach (string file in Values.Where(p => (p.Modification & Modification.Removed) != Modification.Removed).Select(p => p.RelativePath)) {
						_filesWithHidden.Add(file);
					}

					_hasBeenModified = false;
				}
				return _filesWithHidden;
			}
		}

		public HashSet<string> Directories {
			get {
				if (_directories == null) {
					_directories = new HashSet<string>();

					foreach (string directory in Files.Select(GrfPath.GetDirectoryName).Distinct()) {
						string temp = directory;

						// Note : 2015-06-30
						// The path can be empty if files are found at the root of the GRF
						do {
							_directories.Add(temp);
							temp = GrfPath.GetDirectoryName(temp);
						} while (!String.IsNullOrEmpty(temp));
					}

					_hasBeenModified = false;
				}
				return _directories;
			}
		}

		public HashSet<string> HiddenDirectories {
			get {
				if (_hiddenDirectories == null) {
					_hiddenDirectories = new HashSet<string>();

					foreach (string directory in Values.Where(p => (p.Modification & Modification.Removed) == Modification.Removed).Select(p => GrfPath.GetDirectoryName(p.RelativePath)).Distinct()) {
						_hiddenDirectories.Add(directory);
					}

					_hasBeenModified = false;
				}
				return _hiddenDirectories;
			}
		}

		public List<TEntry> Entries {
			get {
				if (_fastEntries == null) {
					_fastEntries = Values.Where(p => (p.Modification & Modification.Removed) != Modification.Removed).ToList();
					_hasBeenModified = false;
				}
				return _fastEntries;
			}
		}

		// Uppercase
		public List<KeyValuePair<string, TEntry>> FastAccessEntries {
			get {
				if (_fastAccessEntries == null) {
					_fastAccessEntries = Values.Where(p => (p.Modification & Modification.Removed) != Modification.Removed).ToDictionary(p => p.RelativePath).ToList();
					_hasBeenModified = false;
				}
				return _fastAccessEntries;
			}
		}

		// Slowest request to the file table
		public List<Tuple<string, string, TEntry>> FastTupleAccessEntries {
			get {
				if (_fastTupleAccessEntries == null) {
					_fastTupleAccessEntries = Values.Where(p => (p.Modification & Modification.Removed) != Modification.Removed).
						Select(p => new Tuple<string, string, TEntry>(GrfPath.GetDirectoryName(p.RelativePath), Path.GetFileName(p.RelativePath), p)).ToList();
					_hasBeenModified = false;
				}
				return _fastTupleAccessEntries;
			}
		}

		// Slowest request to the file table
		public TkDictionary<string, List<EntrySearchNode<TEntry>>> DirectoryStructure {
			get {
				if (_directoryStructure == null) {
					_directoryStructure = new TkDictionary<string, List<EntrySearchNode<TEntry>>>();

					List<EntrySearchNode<TEntry>> list = null;
					string currentPath = null;

					foreach (var entry in FastTupleAccessEntries.OrderBy(p => p.Item1)) {
						if (currentPath == null) {
							currentPath = entry.Item1;
							list = new List<EntrySearchNode<TEntry>>();
							_directoryStructure[currentPath] = list;
						}

						if (currentPath != entry.Item1) {
							currentPath = entry.Item1;
							list = new List<EntrySearchNode<TEntry>>();
							_directoryStructure[currentPath] = list;
						}

						list.Add(new EntrySearchNode<TEntry>(entry.Item3, entry.Item2));
					}

					_hasBeenModified = false;
				}
				return _directoryStructure;
			}
		}

		public new void Add(string key, TEntry value) {
			base[key] = value;
			HasBeenModified = true;
		}

		public new void Remove(string key) {
			if (ContainsKey(key)) {
				base.Remove(key);
				HasBeenModified = true;
			}
		}

		public new bool ContainsKey(string key) {
			return base.ContainsKey(key);
		}

		//private string _getBaseKey(string key) {
		//	if (!_mapper.ContainsKey(key))
		//		_mapper[key] = key.ToLowerInvariant();
		//
		//	return _mapper[key];
		//}

		//internal List<string> GetMappingFiles() {
		//	return _mapper.Values.ToList();
		//}

		public new void Clear() {
			if (_files != null) {
				_files.Clear();
			}

			if (_fastAccessEntries != null) {
				_fastAccessEntries.Clear();
			}

			if (_fastEntries != null) {
				_fastEntries.Clear();
			}

			if (_fastTupleAccessEntries != null) {
				_fastTupleAccessEntries.Clear();
			}

			base.Clear();
		}

		public void Delete() {
			if (_files != null) {
				_files.Clear();
				_files = null;
			}

			if (_fastAccessEntries != null) {
				_fastAccessEntries.Clear();
				_fastAccessEntries = null;
			}

			if (_fastEntries != null) {
				_fastEntries.Clear();
				_fastEntries = null;
			}

			if (_fastTupleAccessEntries != null) {
				_fastTupleAccessEntries.Clear();
				_fastTupleAccessEntries = null;
			}

			if (_directories != null) {
				_directories.Clear();
				_directories = null;
			}

			if (_hiddenDirectories != null) {
				_hiddenDirectories.Clear();
				_hiddenDirectories = null;
			}
		}

		internal string GetMappedFile(string key) {
			return base[key].RelativePath;
		}
	}
}