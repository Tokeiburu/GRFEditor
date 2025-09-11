using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.IO;
using GRF.Threading;

namespace GRF.GrfSystem {
	public static class TemporaryFilesManager {
		private static readonly object _lock = new object();
		private static readonly Dictionary<string, HashSet<string>> _patterns = new Dictionary<string, HashSet<string>>();
		private static readonly Dictionary<string, Stream> _streams = new Dictionary<string, Stream>();

		/// <summary>
		/// Clears the temporary files.
		/// </summary>
		public static void ClearTemporaryFiles() {
			GrfThread.Start(delegate {
				int errorCount = 0;
				foreach (string file in Directory.GetFiles(Settings.TempPath, "*")) {
					if (!GrfPath.Delete(file)) {
						errorCount++;
					}

					if (errorCount > 20)
						break;
				}
			}, "GRF - TemporaryFilesManager cleanup");
		}

		/// <summary>
		/// Gets the temporary file path.
		/// </summary>
		/// <param name="fileNamePattern">The file name pattern.</param>
		/// <returns></returns>
		public static string GetTemporaryFilePath(string fileNamePattern) {
			int currentIndex = -1;
			string path;
			bool isUnique = _patterns.ContainsKey(fileNamePattern);

			HashSet<string> usedPatterns = null;

			if (!isUnique) {
				UniquePattern(fileNamePattern);
				isUnique = true;
			}

			usedPatterns = _patterns[fileNamePattern];
			currentIndex = _patterns[fileNamePattern].Count - 1;

			HashSet<string> files = new HashSet<string>(Directory.GetFiles(Settings.TempPath, "*").ToList());

			lock (_lock) {
				do {
					currentIndex++;
					path = Path.Combine(Settings.TempPath, String.Format(fileNamePattern, currentIndex));
				} while (files.Contains(path) || usedPatterns.Contains(path) || File.Exists(path));

				usedPatterns.Add(path);
			}

			return path;
		}

		/// <summary>
		/// Gets the temporary folder path.
		/// </summary>
		/// <param name="pathPattern">The path pattern.</param>
		/// <returns></returns>
		public static string GetTemporaryFolderPath(string pathPattern) {
			int currentIndex = -1;
			string path;
			bool isUnique = _patterns.ContainsKey(pathPattern);

			HashSet<string> usedPatterns = null;

			if (isUnique) {
				usedPatterns = _patterns[pathPattern];
				currentIndex = _patterns[pathPattern].Count - 1;
			}

			List<string> files = Directory.GetDirectories(Settings.TempPath, "*").ToList();

			lock (_lock) {
				do {
					currentIndex++;
					path = Path.Combine(Settings.TempPath, String.Format(pathPattern, currentIndex));
				} while (files.Contains(path) || (isUnique && usedPatterns.Contains(path)) || Directory.Exists(path));

				if (isUnique) {
					usedPatterns.Add(path);
				}
			}

			return path;
		}

		/// <summary>
		/// Makes a pattern unique by keeping track of the last used pattern.
		/// This is useful if a large amount of temporary files with the pattern is used.
		/// </summary>
		/// <param name="fileNamePattern">The file name pattern.</param>
		private static void UniquePattern(string fileNamePattern) {
			lock (_lock) {
				if (!_patterns.ContainsKey(fileNamePattern))
					_patterns.Add(fileNamePattern, new HashSet<string>());
			}
		}

		public static FileStream GetTemporaryFileStream(string fileNamePattern) {
			return new FileStream(GetTemporaryFilePath(fileNamePattern), FileMode.CreateNew, FileAccess.Write);
		}

		public static void LockFile(string file) {
			_streams[file] = File.OpenRead(file);
		}
	}
}
