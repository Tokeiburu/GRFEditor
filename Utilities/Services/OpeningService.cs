using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Utilities.Services {
	public static class OpeningService {
		#region Static methods

		static Guid IID_IShellFolder = typeof(NativeMethods.IShellFolder).GUID;

		static IntPtr GetShellFolderChildrenRelativePIDL(NativeMethods.IShellFolder parentFolder, string displayName) {
			uint pchEaten;
			uint pdwAttributes = 0;
			IntPtr ppidl;
			parentFolder.ParseDisplayName(IntPtr.Zero, null, displayName, out pchEaten, out ppidl, ref pdwAttributes);

			return ppidl;
		}

		public static IntPtr PathToAbsolutePIDL(string path) {
			var desktopFolder = SHGetDesktopFolder();
			return GetShellFolderChildrenRelativePIDL(desktopFolder, path);
		}

		static NativeMethods.IShellFolder PIDLToShellFolder(NativeMethods.IShellFolder parent, IntPtr pidl) {
			NativeMethods.IShellFolder folder;
			var result = parent.BindToObject(pidl, null, ref IID_IShellFolder, out folder);
			Marshal.ThrowExceptionForHR(result);
			return folder;
		}

		static NativeMethods.IShellFolder PIDLToShellFolder(IntPtr pidl) {
			return PIDLToShellFolder(SHGetDesktopFolder(), pidl);
		}

		static void SHOpenFolderAndSelectItems(IntPtr pidlFolder, IntPtr[] apidl, bool edit) {
			NativeMethods.SHOpenFolderAndSelectItems(pidlFolder, apidl, edit ? 1 : 0);
		}

		public static void OpenFolder(string path) {
			if (path == null) throw new ArgumentNullException("path");
			Process.Start(path);
			
			//string[] files = Directory.GetFiles(path);
			//
			//if (files.Length > 0) {
			//	var pidl = PathToAbsolutePIDL(files[0]);
			//	try {
			//		SHOpenFolderAndSelectItems(pidl, null, false);
			//	}
			//	finally {
			//		NativeMethods.ILFree(pidl);
			//	}
			//}
			//else {
			//	Process.Start(path);
			//}
		}

		public static void FileOrFolder(string path, bool edit = false) {
			if (path == null) throw new ArgumentNullException("path");

			if (File.Exists(path))
				path = new FileInfo(path).FullName;

			if (Directory.Exists(path)) {
				// Called the wrong method, oh well...
				OpenFolder(new DirectoryInfo(path).FullName);
				return;
			}

			var pidl = PathToAbsolutePIDL(path);
			try {
				SHOpenFolderAndSelectItems(pidl, null, edit);
			}
			finally {
				NativeMethods.ILFree(pidl);
			}
		}

		public static NativeMethods.IShellFolder SHGetDesktopFolder() {
			NativeMethods.IShellFolder result;
			Marshal.ThrowExceptionForHR(NativeMethods.SHGetDesktopFolder_(out result));
			return result;
		}

		#endregion

		private static IEnumerable<FileSystemInfo> PathToFileSystemInfo(IEnumerable<string> paths) {
			foreach (var path in paths) {
				var fixedPath = path;
				if (fixedPath.EndsWith(Path.DirectorySeparatorChar.ToString())
				    || fixedPath.EndsWith(Path.AltDirectorySeparatorChar.ToString())) {
					fixedPath = fixedPath.Remove(fixedPath.Length - 1);
				}

				if (Directory.Exists(fixedPath)) {
					yield return new DirectoryInfo(fixedPath);
				}
				else if (File.Exists(fixedPath)) {
					yield return new FileInfo(fixedPath);
				}
				else {
					throw new FileNotFoundException
						(String.Format("The specified file or folder doesn't exists : {0}", fixedPath),
						 fixedPath);
				}
			}
		}

		public static void FilesOrFolders(string parentDirectory, ICollection<string> filenames) {
			if (filenames == null) throw new ArgumentNullException("filenames");
			if (filenames.Count == 0) return;

			var parentPidl = PathToAbsolutePIDL(parentDirectory);
			try {
				var parent = PIDLToShellFolder(parentPidl);
				var filesPidl = filenames
					.Select(filename => GetShellFolderChildrenRelativePIDL(parent, filename))
					.ToArray();

				try {
					SHOpenFolderAndSelectItems(parentPidl, filesPidl, false);
				}
				finally {
					foreach (var pidl in filesPidl) {
						NativeMethods.ILFree(pidl);
					}
				}
			}
			finally {
				NativeMethods.ILFree(parentPidl);
			}
		}

		public static void FilesOrFolders(params string[] paths) {
			FilesOrFolders((IEnumerable<string>)paths);
		}

		public static void FilesOrFolders(string path) {
			FilesOrFolders(new string[] { path });
		}

		public static void FilesOrFolders(IEnumerable<string> paths) {
			if (paths == null) throw new ArgumentNullException("paths");

			FilesOrFolders(PathToFileSystemInfo(paths));
		}

		public static void FilesOrFolders(IEnumerable<FileSystemInfo> paths) {
			if (paths == null) throw new ArgumentNullException("paths");
			var pathsArray = paths.ToArray();
			if (pathsArray.Count() == 0) return;

			var explorerWindows = pathsArray.GroupBy(p => Path.GetDirectoryName(p.FullName));

			foreach (var explorerWindowPaths in explorerWindows) {
				var parentDirectory = Path.GetDirectoryName(explorerWindowPaths.First().FullName);
				FilesOrFolders(parentDirectory, explorerWindowPaths.Select(fsi => fsi.Name).ToList());
			}
		}
	}
}
