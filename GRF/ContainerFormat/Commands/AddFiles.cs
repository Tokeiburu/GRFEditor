using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.IO;
using Utilities.Extension;
using Utilities.Services;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable ImplicitlyCapturedClosure

namespace GRF.ContainerFormat.Commands {
	internal class AddFiles<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.AddFilesCallback _callback;
		private readonly List<TEntry> _conflicts = new List<TEntry>();
		private readonly string[] _files;

		private readonly string _grfPath;
		private List<Tuple<string, string>> _foldersFilesPath = new List<Tuple<string, string>>(); // Left = correctedPathName, right = originalFileName
		private bool _processed;

		public AddFiles(string grfPath, IEnumerable<string> filesPath, CCallbacks.AddFilesCallback callback) {
			_grfPath = GrfPath.CleanGrfPath(grfPath);
			_files = filesPath.Select(p => p.RemoveDoubleSlashes()).ToArray();
			_callback = callback;
		}

		public AddFiles(string grfPath, string filePath, CCallbacks.AddFilesCallback callback)
			: this(grfPath, new string[] {filePath}, callback) {
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			if (!_processed) {
				foreach (string file in _files.Where(File.Exists)) {
					_foldersFilesPath.Add(new Tuple<string, string>(EncodingService.CorrectPathExplode(GrfPath.Combine(_grfPath, Path.GetFileName(file))), file));
				}

				foreach (string directory in _files.Where(Directory.Exists)) {
					string toReplace = Path.GetDirectoryName(directory) + "\\";
					_foldersFilesPath.AddRange(Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Select(p => new Tuple<string, string>(EncodingService.CorrectPathExplode(Path.Combine(_grfPath, p.ReplaceFirst(toReplace, ""))), p)));
				}

				_foldersFilesPath = _foldersFilesPath.Distinct(TupleComparer.Default).ToList();
				_processed = true;
			}

			List<string> newFolders = new List<string>();

			foreach (Tuple<string, string> entry in _foldersFilesPath) {
				TEntry conflictEntry = container.Table.Add(entry.Item1, entry.Item2, true);

				if (conflictEntry != null)
					_conflicts.Add(conflictEntry);

				newFolders.Add(Path.GetDirectoryName(entry.Item1));
			}

			if (_callback != null)
				_callback(_foldersFilesPath.Select(p => p.Item2).ToList(), _foldersFilesPath.Select(p => p.Item1).ToList(), newFolders, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			foreach (Tuple<string, string> entry in _foldersFilesPath) {
				container.Table.DeleteEntry(entry.Item1);
			}

			foreach (TEntry entry in _conflicts) {
				container.Table.AddEntry(entry);
			}

			_conflicts.Clear();

			if (_callback != null)
				_callback(_foldersFilesPath.Select(p => p.Item2).ToList(), _foldersFilesPath.Select(p => p.Item1).ToList(), null, false);
		}

		public string CommandDescription {
			get {
				if (_foldersFilesPath.Count == 1 && _foldersFilesPath.Count == 0) {
					return string.Format(GrfStrings.AddFile, _foldersFilesPath[0].Item2, _grfPath);
				}

				return string.Format(GrfStrings.AddFiles, _grfPath);
			}
		}

		#endregion

		#region Nested type: TupleComparer

		public sealed class TupleComparer : IEqualityComparer<Tuple<string, string>> {
// ReSharper disable StaticFieldInGenericType
			private static readonly TupleComparer _instance = new TupleComparer();
// ReSharper restore StaticFieldInGenericType

			public static TupleComparer Default {
				get { return _instance; }
			}

			#region IEqualityComparer<Tuple<string,string>> Members

			public bool Equals(Tuple<string, string> x, Tuple<string, string> y) {
				return x.Item1 == y.Item1;
			}

			public int GetHashCode(Tuple<string, string> obj) {
				return obj.Item1.GetHashCode();
			}

			#endregion
		}

		#endregion
	}
}