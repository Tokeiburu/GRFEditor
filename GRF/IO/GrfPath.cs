using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GRF.ContainerFormat;
using GRF.Core;
using Utilities;
using Utilities.Extension;

namespace GRF.IO {
	public static class GrfPath {
		#region OpenMode enum

		[Flags]
		public enum OpenMode {
			Container = 1 << 1,
			File = 1 << 2,
			FileAndContainer = Container | File,
			LoadContainers = Container | 1 << 3,
			All = Container | File | LoadContainers,
		}

		#endregion

		private const int _bufferLength = 8388608;

		public static readonly char DirectorySeparatorChar = '\\';
		public static readonly char AltDirectorySeparatorChar = '/';
		public static readonly char VolumeSeparatorChar = ':';

		/// <summary>
		/// Gets the name of the directory (without cutting away extra slashes).
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>The full name of the directory</returns>
		public static string GetDirectoryName(string path) {
			int index1 = path.LastIndexOf(DirectorySeparatorChar);
			int index2 = path.LastIndexOf(AltDirectorySeparatorChar);
			int index = Math.Min(index1, index2);

			if (index1 < 0 && index2 > -1)
				index = index2;

			if (index2 < 0 && index1 > -1)
				index = index1;

			if (index < 0) {
				return "";
			}

			return path.Substring(0, index);
		}

		/// <summary>
		/// Gets the name of the directory (without cutting away extra slashes).
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>The full name of the directory</returns>
		public static string GetDirectoryNameKeepSlash(string path) {
			int index1 = path.LastIndexOf(DirectorySeparatorChar);
			int index2 = path.LastIndexOf(AltDirectorySeparatorChar);
			int index = Math.Min(index1, index2);

			if (index1 < 0 && index2 > -1)
				index = index2;

			if (index2 < 0 && index1 > -1)
				index = index1;

			if (index < 0) {
				return "";
			}

			return path.Substring(0, index + 1);
		}

		public static string GetSingleName(string path, int folderIndex) {
			string[] directories = SplitDirectories(path);

			if (folderIndex < 0) {
				folderIndex = folderIndex * -1;

				for (int i = directories.Length - 1, current = 1; i >= 0; i--, current++) {
					if (current == folderIndex)
						return directories[i];
				}

				return null;
			}
			else {
				if (folderIndex < directories.Length) {
					return directories[folderIndex];
				}

				return null;
			}
		}

		internal static bool IsDirectorySeparator(char c) {
			return (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar);
		}

		internal static int GetRootLength(String path) {
			int i = 0;
			int length = path.Length;

			if (length >= 1 && (IsDirectorySeparator(path[0]))) {
				// handles UNC names and directories off current drive's root.
				i = 1;
				if (length >= 2 && (IsDirectorySeparator(path[1]))) {
					i = 2;
					int n = 2;
					while (i < length && ((path[i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar) || --n > 0)) i++;
				}
			}
			else if (length >= 2 && path[1] == VolumeSeparatorChar) {
				// handles A:\foo.
				i = 2;
				if (length >= 3 && (IsDirectorySeparator(path[2]))) i++;
			}
			return i;
		}

		/// <summary>
		/// Splits the path with their proper directory names (double slashes are ignored)
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>List of directory names.</returns>
		public static string[] SplitDirectories(string path) {
			string[] nodes = path.Split('\\');
			List<string> nodesToReturn = new List<string>();

			for (int index = 0; index < nodes.Length; index++) {
				string node = nodes[index];

				if (index == 0 && node == "") {
				}
				else if (index == nodes.Length - 1 && node == "") {
				}
				else if (node == "") {
					int subIndex = index - 1;
					while (nodes[subIndex] != "" && subIndex > 0) {
						subIndex--;
					}
					nodesToReturn[subIndex] = nodesToReturn[subIndex] + "\\";
				}
				else {
					nodesToReturn.Add(node);
				}
			}

			return nodesToReturn.ToArray();
		}

		/// <summary>
		/// Cleans the GRF path.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <returns>A valid GRF path.</returns>
		/// <exception cref="Exception">Invalid characters in grf path</exception>
		public static string CleanGrfPath(string grfPath) {
			if (grfPath == null)
				return "";

			string path = grfPath.Replace("/", "\\").ReplaceAll("\\\\", "\\").Trim('\\');

			if (path.Any(p => _getInvalidPathChars.Contains(p)) || path.Split('\\').Any(p => p.Any(q => _getInvalidFileNameChars.Contains(q))))
				throw GrfExceptions.__InvalidCharactersInPath.Create(path);

			return path;
		}

		private static char[] __getInvalidPathChars;
		private static char[] _getInvalidPathChars {
			get { return __getInvalidPathChars ?? (__getInvalidPathChars = Path.GetInvalidPathChars()); }
		}

		private static char[] __getInvalidFileNameChars;
		private static char[] _getInvalidFileNameChars {
			get { return __getInvalidFileNameChars ?? (__getInvalidFileNameChars = Path.GetInvalidFileNameChars()); }
		}

		public static bool Delete(string fileOrFolder) {
			try {
				if (fileOrFolder == null)
					return false;

				if (Directory.Exists(fileOrFolder)) {
					DirectoryInfo downloadedMessageInfo = new DirectoryInfo(fileOrFolder);

					foreach (FileInfo file in downloadedMessageInfo.GetFiles()) {
						try {
							file.Delete();
						}
						catch {
						}
					}

					foreach (DirectoryInfo dir in downloadedMessageInfo.GetDirectories()) {
						dir.Delete(true);
					}
				}
				else {
					File.Delete(fileOrFolder);
				}

				return true;
			}
			catch {
				return false;
			}
		}

		public static bool Delete(TkPath file) {
			try {
				string fullFilePath = Path.GetFullPath(file.FilePath);

				if (file.RelativePath == null)
					return Delete(fullFilePath);

				if (!File.Exists(fullFilePath))
					return true;

				using (GrfHolder grf = new GrfHolder(fullFilePath, GrfLoadOptions.Normal)) {
					grf.Commands.RemoveFiles(file.RelativePath);
					grf.QuickMerge(null);
					return true;
				}
			}
			catch {
				return false;
			}
		}

		public static IEnumerable<TkPath> GetTkPaths(GrfHolder grf, string search) {
			if (grf.IsOpened) {
				Regex regex = new Regex(Methods.WildcardToRegexLine(search), RegexOptions.IgnoreCase);

				foreach (string file in grf.FileTable.Files.Where(p => regex.IsMatch(p))) {
					yield return new TkPath(grf.FileName, file);
				}
			}
			else {
				string inputFile = new FileInfo(search.Replace('*', '_')).FullName;

				foreach (TkPath file in Directory.GetFiles(GetDirectoryName(inputFile), Path.GetFileName(search), SearchOption.AllDirectories).Select(p => new TkPath {FilePath = p})) {
					yield return file;
				}
			}
		}

		public static byte[] GetData(TkPath tkPath) {
			return GetData(tkPath, null, OpenMode.All);
		}

		public static byte[] GetData(TkPath tkPath, OpenMode mode) {
			return GetData(tkPath, null, mode);
		}

		public static byte[] GetData(TkPath tkPath, GrfHolder grf, OpenMode mode) {
			// This is a container
			if ((mode & OpenMode.Container) == OpenMode.Container) {
				if (tkPath.FilePath != null && tkPath.RelativePath != null) {
					if ((mode & OpenMode.LoadContainers) == OpenMode.LoadContainers) {
						if (grf != null && grf.IsOpened && grf.FileName == tkPath.FilePath) {
							if (grf.FileTable.ContainsKey(tkPath.RelativePath)) {
								return grf.FileTable[tkPath.RelativePath].GetDecompressedData();
							}
						}
						else {
							using (GrfHolder openedGrf = new GrfHolder(tkPath.FilePath)) {
								if (openedGrf.FileTable.ContainsKey(tkPath.RelativePath)) {
									return openedGrf.FileTable[tkPath.RelativePath].GetDecompressedData();
								}

								return null;
							}
						}
					}

					if (grf == null || !grf.IsOpened)
						return null;

					if (grf.FileTable.ContainsKey(tkPath.RelativePath)) {
						return grf.FileTable[tkPath.RelativePath].GetDecompressedData();
					}
				}
			}

			if ((mode & OpenMode.File) == OpenMode.File) {
				if (tkPath.RelativePath == null) {
					if (File.Exists(tkPath.FilePath))
						return File.ReadAllBytes(tkPath.FilePath);
				}
			}

			return null;
		}

		public static TkPath GetFullPath(TkPath path, params string[] directories) {
			if (path.FilePath == null)
				return path;

			directories = new string[] {""}.Concat(directories).ToArray();

			foreach (string directory in directories) {
				if (File.Exists(Combine(directory, path.FilePath))) {
					return new TkPath(new FileInfo(Combine(directory, path.FilePath)).FullName, path.RelativePath);
				}
			}

			return path;
		}

		public static string Combine(params string[] paths) {
			string path = paths[0];

			for (int i = 1; i < paths.Length; i++) {
				if (path == null)
					path = "";

				if (path.EndsWith(":") && paths[i] != "")
					path = path + "\\";

				path = Path.Combine(path, paths[i]);
			}

			return path;
		}

		public static string CombineUrl(params string[] paths) {
			string path = paths[0];

			if (!path.Contains("://"))
				return Combine(paths);

			path = path.TrimEnd('/', '\\');

			for (int i = 1; i < paths.Length; i++) {
				path = path.TrimEnd('/', '\\') + "/" + paths[i].Trim('/', '\\');
			}

			return path;
		}

		public static void CreateDirectoryFromFile(string fileName) {
			string directory = Path.GetDirectoryName(fileName);

			if (directory == null)
				throw new Exception("directory is null.");

			if (!Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}
		}

		public static void Write(string filepath, byte[] data) {
			FileStream stream2 = null;

			try {
				Delete(filepath);

				int offset = 0;
				int length;
				stream2 = new FileStream(filepath, FileMode.Create, FileAccess.Write);

				while (offset < data.Length) {
					length = _bufferLength;

					if (length + offset > data.Length) {
						length = data.Length - offset;
					}

					stream2.Write(data, offset, length);
					offset += _bufferLength;
				}
			}
			finally {
				if (stream2 != null) {
					stream2.Close();
					stream2.Dispose();
				}
			}
		}

		public static void CreateFile(string path) {
			CreateDirectoryFromFile(path);

			if (!File.Exists(path)) {
				File.Create(path).Close();
			}
		}

		public static bool ContainsInvalidCharacters(string path) {
			return !CheckInvalidPathChars(path);
		}

		internal static bool CheckInvalidPathChars(string path) {
			for (int i = 0; i < path.Length; i++) {
				int c = path[i];

				if (c == '\"' || c == '<' || c == '>' || c == '|' || c < 32)
					return false;
			}

			return true;
		}

		public static long GetFileSize(string filename) {
			IntPtr handle = NativeMethods.CreateFile(
				filename,
				FileAccess.Read,
				FileShare.Read,
				IntPtr.Zero,
				FileMode.Open,
				FileAttributes.ReadOnly,
				IntPtr.Zero);
			long fileSize;
			NativeMethods.GetFileSizeEx(handle, out fileSize);
			NativeMethods.CloseHandle(handle);
			return fileSize;
		}

		public static void Copy(string from, string to) {
			Delete(to);
			File.Copy(from, to);
		}
	}
}