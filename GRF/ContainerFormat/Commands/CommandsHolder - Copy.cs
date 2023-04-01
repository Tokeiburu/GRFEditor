using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GRF.Core;
using GRF.IO;
using GRF.System;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.ContainerFormat.Commands {
	/// <summary>
	/// grfPath : The directory path within the GRF. Example : data\texture
	/// file : The full path of a file existing on the hard drive. Example : C:\ex.txt
	/// path : The full path of a folder existing on the hard drive. Example : C:\sprite
	/// grfFile : The full path of a file within the GRF. Example : data\texture\back.bmp
	/// grfFileName : The file name within the GRF (without a path). Example : back.bmp
	/// </summary>
	/// <typeparam name="TEntry">The type of the entry.</typeparam>
	public class CommandsHolder<TEntry> where TEntry : ContainerEntry {
		private readonly ContainerAbstract<TEntry> _container;

		public CommandsHolder(ContainerAbstract<TEntry> container) {
			_container = container;
		}

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute
		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFileInDirectory(@"data\sprite", @"C:\file.spr")<para></para>
		/// ex: AddFileInDirectory(@"data", @"C:\sprite")<para></para>
		/// The files are locked automatically if LockFiles is enabled in the Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileOrFolder">The file.</param>
		/// <param name="callback">The callback.</param>
		public void AddFileInDirectory(string grfPath, string fileOrFolder, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(fileOrFolder, "fileOrFolder");

			grfPath = GrfPath.CleanGrfPath(grfPath);

			try {
				_container.Lock();
				_container.StoreAndExecute(new AddFiles<TEntry>(grfPath, fileOrFolder, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Adds an existing file at the specified GRF file path.<para></para>
		/// ex: AddFileAbsolute(@"data\sprite\test.spr", @"C:\file.spr")<para></para>
		/// The file is locked automatically if LockFiles is enabled in the Settings.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="fileOrFolder">The file.</param>
		/// <param name="callback">The callback.</param>
		public void AddFileAbsolute(string grfFile, string fileOrFolder, CCallbacks.ReplaceFileCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFile, "grfFile");
			GrfExceptions.IfNullThrow(fileOrFolder, "fileOrFolder");
			if (!File.Exists(fileOrFolder)) GrfExceptions.ThrowFileNotFound(fileOrFolder);

			try {
				_container.Lock();
				_container.StoreAndExecute(new ReplaceFile<TEntry>(Path.GetDirectoryName(grfFile), Path.GetFileName(grfFile), fileOrFolder, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Adds existing files or folders at the specified GRF path.<para></para>
		/// ex: AddFileInDirectory(@"data\sprite", new string[] { @"C:\file.spr", @"C:\data\folder", @"C:\data\sprite2\file2.spr" })<para></para>
		/// The files are locked automatically if LockFiles is enabled in the Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="filesOrFolders">The files.</param>
		/// <param name="callback">The callback.</param>
		public void AddFilesInDirectory(string grfPath, IEnumerable<string> filesOrFolders, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(filesOrFolders, "filesOrFolders");

			List<string> filesList = filesOrFolders.ToList();

			if (filesList.Count == 0)
				return;

			grfPath = GrfPath.CleanGrfPath(grfPath);

			try {
				_container.Lock();
				_container.StoreAndExecute(new AddFiles<TEntry>(grfPath, filesList, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in the Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileOrFolder">The file.</param>
		/// <param name="callback">The callback.</param>
		public void AddFilesInDirectory(string grfPath, string fileOrFolder, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			AddFileInDirectory(grfPath, fileOrFolder, callback);
		}

		/// <summary>
		/// Adds a file from a byte array at the specified GRF file path.<para></para>
		/// ex: AddFileAbsolute(@"data\sprite\test.spr", File.ReadAllBytes(@"C:\file.spr"))
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="fileData">The file data.</param>
		/// <param name="callback">The callback.</param>
		public void AddFileAbsolute(string grfFile, byte[] fileData, CCallbacks.ReplaceFileCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(fileData, "fileData");

			string file = TemporaryFilesManager.GetTemporaryFilePath("added_file_{0:0000}");
			File.WriteAllBytes(file, fileData);
			TemporaryFilesManager.LockFile(file);
			try {
				_container.Lock();
				_container.StoreAndExecute(new ReplaceFile<TEntry>(Path.GetDirectoryName(grfFile), Path.GetFileName(grfFile), file, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Removes the file or folder at the specified path.<para></para>
		/// Does nothing if the file or folder is not found.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="callback">The callback.</param>
		public void Remove(string path, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			if (path == null || path == "")
				GrfExceptions.ThrowNullPathException(path);

			if (path.EndsWith("/") || path.EndsWith("\\"))
				path = GrfPath.CleanGrfPath(path);

			if (_container.Table.ContainsFile(path)) {
				RemoveFile(path, callback);
			}
			else if (_container.Table.ContainsDirectory(path)) {
				RemoveFolder(path, callback);
			}
		}

		/// <summary>
		/// Removes the files or folders at the specified paths.<para></para>
		/// Files or folders not found are ignored.
		/// </summary>
		/// <param name="paths">The paths.</param>
		/// <param name="callback">The callback.</param>
		public void Remove(IEnumerable<string> paths, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(paths, "paths");

			List<string> cleanedPaths = new List<string>();

			foreach (var path in paths) {
				if (path.EndsWith("/") || path.EndsWith("\\"))
					cleanedPaths.Add(GrfPath.CleanGrfPath(path));
				else
					cleanedPaths.Add(path);
			}

			if (cleanedPaths.Count == 0) return;

			if (cleanedPaths.All(p => _container.Table.ContainsFile(p))) {
				RemoveFiles(cleanedPaths, callback);
			}
			else if (cleanedPaths.All(p => _container.Table.ContainsDirectory(p))) {
				RemoveFolders(cleanedPaths, callback);
			}
			else {
				try {
					BeginNoDelay();

					foreach (string path in cleanedPaths) {
						if (_container.Table.ContainsFile(path)) {
							RemoveFile(path, callback);
						}
						else if (_container.Table.ContainsDirectory(path)) {
							RemoveFolder(path, callback);
						}
					}
				}
				catch {
					_container.CancelEdit();
				}
				finally {
					End();
				}
			}
		}

		/// <summary>
		/// Removes a folder at the specified GRF path.<para></para>
		/// Warning: Adds a command on the AbstractCommand object even if the folder is not found.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFolder(string grfPath, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			if (grfPath == null || grfPath == "")
				GrfExceptions.ThrowNullPathException(grfPath);

			//if (!_container.Table.ContainsDirectory(grfPath)) {
			//	return;
			//}

			try {
				_container.Lock();
				_container.StoreAndExecute(new DeleteFolders<TEntry>(grfPath, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Removes folders at the specified GRF paths.<para></para>
		/// Folders not found are ignored.
		/// </summary>
		/// <param name="grfPaths">The GRF paths.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFolders(IEnumerable<string> grfPaths, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfPaths, "grfPaths");
			List<string> paths = grfPaths.ToList();
			if (paths.Count <= 0) return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new DeleteFolders<TEntry>(paths, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Removes a file at the specified GRF file path.<para></para>
		/// Does nothing if the file is not found.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFile(string grfFile, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFile, "grfFile");
			if (!_container.Table.ContainsFile(grfFile)) return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new DeleteFiles<TEntry>(grfFile, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Removes files at the specified GRF file paths.<para></para>
		/// Files not found are ignored.
		/// </summary>
		/// <param name="grfFiles">The GRF files.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFiles(IEnumerable<string> grfFiles, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFiles, "grfFiles");
			List<string> files = grfFiles.ToList();
			if (files.Count <= 0) return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new DeleteFiles<TEntry>(files, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Removes files using a wildcard search pattern.<para></para>
		/// ex: RemoveFiles("*.lu?")
		/// </summary>
		/// <param name="wildCardSearchPattern">The wild card search pattern.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFiles(string wildCardSearchPattern, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(wildCardSearchPattern, "wildCardSearchPattern");
			Regex regex = new Regex(Methods.WildcardToRegexLine(wildCardSearchPattern), RegexOptions.IgnoreCase);

			List<string> files = _container.Table.Files.Where(p => regex.IsMatch(p)).ToList();

			if (files.Count == 0)
				return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new DeleteFiles<TEntry>(files, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Removes files using a regex search pattern.
		/// </summary>
		/// <param name="searchPattern">The search pattern.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFilesRegex(string searchPattern, CCallbacks.DeleteCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(searchPattern, "searchPattern");
			Regex regex = new Regex(searchPattern, RegexOptions.IgnoreCase);

			List<string> files = _container.Table.Files.Where(p => regex.IsMatch(p)).ToList();

			if (files.Count == 0)
				return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new DeleteFiles<TEntry>(files, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		public void StoreAndExecute(IContainerCommand<TEntry> command) {
			_container.StoreAndExecute(command);
		}

		/// <summary>
		/// Renames a file or a folder. This method cannot overwrite an existing file.<para></para>
		/// Use AddFile*() methods or MergeFolders to overwrite content.
		/// </summary>
		/// <param name="oldPath">The old path.</param>
		/// <param name="newPath">The new path.</param>
		/// <param name="callback">The callback.</param>
		public void Rename(string oldPath, string newPath, CCallbacks.RenameCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			newPath = EncodingService.FromAnyToDisplayEncoding(newPath);

			if (oldPath == newPath)
				return;

			if (String.IsNullOrEmpty(oldPath) || String.IsNullOrEmpty(newPath))
				GrfExceptions.ThrowNullPathException(String.IsNullOrEmpty(oldPath) ? oldPath : newPath);

			if (!EncodingService.IsCompatible(newPath, EncodingService.Ansi))
				GrfExceptions.ThrowInvalidTextEncoding(newPath);

			if (GrfPath.GetDirectoryName(oldPath) == newPath)
				GrfExceptions.ThrowNullPathException(oldPath);

			if (_container.Table.ContainsFile(oldPath)) {
				if (_container.Table.ContainsFile(newPath))
					GrfExceptions.ThrowFileNameAlreadyExists(newPath);

				if (_container.Table.Directories.Contains(newPath))
					GrfExceptions.ThrowNullPathException(oldPath);

				try {
					_container.Lock();
					_container.StoreAndExecute(new RenameFile<TEntry>(oldPath, newPath, callback));
				}
				finally {
					_container.Unlock();
				}
			}
			else if (_container.Table.ContainsDirectory(oldPath)) {
				if (newPath.StartsWith(oldPath))
					GrfExceptions.ThrowDestIsSubfolder();

				if (_container.Table.HiddenDirectories.Contains(newPath))
					GrfExceptions.ThrowHiddenFolderConflict(newPath);

				// This is no longer allowed. Use the MergeFolders method instead
				if (_container.Table.Directories.Contains(newPath))
					GrfExceptions.ThrowFolderNameAlreadyExists(newPath);

				try {
					_container.Lock();
					_container.StoreAndExecute(new RenameFolder<TEntry>(oldPath, newPath, callback));
				}
				finally {
					_container.Unlock();
				}
			}
			else {
				GrfExceptions.ThrowPathNotFound(oldPath);
			}
		}

		/// <summary>
		/// Moves a GRF path to a new one. If files are conflicted, they are overwritten.
		/// </summary>
		/// <param name="oldGrfPath">The old GRF path.</param>
		/// <param name="newGrfPath">The new GRF path.</param>
		/// <param name="callbackDelete">The callback delete.</param>
		/// <param name="callbackAddFiles">The callback add files.</param>
		public void MergeFolders(string oldGrfPath, string newGrfPath, CCallbacks.DeleteCallback callbackDelete, CCallbacks.AddFilesCallback callbackAddFiles) {
			GrfExceptions.IfSavingThrow(_container);

			try {
				_container.Commands.BeginNoDelay();

				MoveFiles(oldGrfPath, newGrfPath, _container.Table.FilesInDirectory(oldGrfPath, SearchOption.AllDirectories, false).Select(p => p.ReplaceFirst(GrfPath.CleanGrfPath(oldGrfPath) + "\\", "")).ToList(), callbackAddFiles);

				if (oldGrfPath == null || oldGrfPath == "")
					GrfExceptions.ThrowNullPathException(oldGrfPath);

				try {
					_container.Lock();
					_container.StoreAndExecute(new DeleteFolders<TEntry>(oldGrfPath, callbackDelete));
				}
				finally {
					_container.Unlock();
				}
			}
			catch {
				_container.CancelEdit();
				throw;
			}
			finally {
				_container.Commands.End();
			}
		}

		/// <summary>
		/// Adds a GRF path.<para></para>
		/// Warning: Adding a GRF path does absolutely nothing to the GRF structure.<para></para>
		/// Warning: If you check with container.Table.ContainsDirectory("data\\myNewDirectory")<para></para>
		/// it will return false. A directory needs at least 1 file to exist.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="callback">The callback.</param>
		public void AddFolder(string grfPath, CCallbacks.AddFolderCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			if (String.IsNullOrEmpty(grfPath))
				GrfExceptions.ThrowNullPathException(grfPath);

			if (_container.Table.ContainsDirectory(grfPath))
				return;

			if (!EncodingService.IsCompatible(grfPath, EncodingService.Ansi))
				GrfExceptions.ThrowInvalidTextEncoding(grfPath);

			if (GrfPath.ContainsInvalidCharacters(grfPath))
				GrfExceptions.ThrowInvalidCharactersInPath(grfPath);

			try {
				_container.Lock();
				_container.StoreAndExecute(new AddFolder<TEntry>(grfPath, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in the Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="path">The path.</param>
		/// <param name="callback">The callback.</param>
		public void AddFolder(string grfPath, string path, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			AddFilesInDirectory(grfPath, path, callback);
		}

		/// <summary>
		/// Moves a file or a folder at the specified new location.<para></para>
		/// Warning: This method does overwrite existing files, but not existing folders.
		/// </summary>
		/// <param name="oldPath">The old path.</param>
		/// <param name="newPath">The new path.</param>
		/// <param name="callback">The callback.</param>
		public void Move(string oldPath, string newPath, CCallbacks.RenameCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			newPath = EncodingService.FromAnyToDisplayEncoding(newPath);

			if (String.IsNullOrEmpty(oldPath) || String.IsNullOrEmpty(newPath))
				GrfExceptions.ThrowNullPathException(String.IsNullOrEmpty(oldPath) ? oldPath : newPath);

			if (!EncodingService.IsCompatible(newPath, EncodingService.Ansi))
				GrfExceptions.ThrowInvalidTextEncoding(newPath);

			if (_container.Table.ContainsFile(oldPath)) {
				_container.StoreAndExecute(new MoveFile<TEntry>(oldPath, newPath, callback));
			}
			else if (_container.Table.ContainsDirectory(oldPath)) {
				if (_container.Table.ContainsDirectory(newPath))
					GrfExceptions.ThrowFolderNameAlreadyExists(newPath);

				try {
					_container.Lock();
					_container.StoreAndExecute(new RenameFolder<TEntry>(oldPath, newPath, callback));
				}
				finally {
					_container.Unlock();
				}
			}
			else {
				GrfExceptions.ThrowPathNotFound(oldPath);
			}
		}

		/// <summary>
		/// Moves GRF files from a GRF path to another.<para></para>
		/// ex: MoveFiles(@"data\sprite", @"data", new string[] { "test.spr", "test.act" })
		/// </summary>
		/// <param name="oldGrfPath">The old GRF path.</param>
		/// <param name="newGrfPath">The new GRF path.</param>
		/// <param name="fileNames">The file names.</param>
		/// <param name="callback">The callback.</param>
		public void MoveFiles(string oldGrfPath, string newGrfPath, IEnumerable<string> fileNames, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(oldGrfPath, "oldGrfPath");
			GrfExceptions.IfNullThrow(newGrfPath, "newGrfPath");

			if (oldGrfPath == newGrfPath) return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new MoveFiles<TEntry>(oldGrfPath, newGrfPath, fileNames, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Changes the version of the container.
		/// </summary>
		/// <param name="major">The major.</param>
		/// <param name="minor">The minor.</param>
		/// <param name="callback">The callback.</param>
		public void ChangeVersion(byte major, byte minor, CCallbacks.ChangeVersionCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			if (_container.Header.Is(major, minor)) return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new ChangeVersion<TEntry>(major, minor, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Encrypts GRF files.
		/// </summary>
		/// <param name="grfFiles">The GRF files.</param>
		/// <param name="callback">The callback.</param>
		public void EncryptFiles(IEnumerable<string> grfFiles, CCallbacks.EncryptFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFiles, "grfFiles");

			List<string> files = grfFiles.ToList();

			if (files.Count == 0)
				return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new EncryptFiles<TEntry>(files, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Decrypts GRF files.
		/// </summary>
		/// <param name="grfFiles">The GRF files.</param>
		/// <param name="callback">The callback.</param>
		public void DecryptFiles(IEnumerable<string> grfFiles, CCallbacks.EncryptFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFiles, "grfFiles");

			List<string> files = grfFiles.ToList();

			if (files.Count == 0)
				return;

			try {
				_container.Lock();
				_container.StoreAndExecute(new DecryptFiles<TEntry>(files, callback));
			}
			finally {
				_container.Unlock();
			}
		}

		/// <summary>
		/// Sets the AbstractCommand object in the edit mode.<para></para>
		/// All following commands will be grouped.<para></para>
		/// Commands will be delayed (they will not be applied until End is called).
		/// </summary>
		public void Begin() {
			_container.BeginEdit(new GroupCommand<TEntry>(_container, false));
		}

		/// <summary>
		/// Sets the AbstractCommand object in the edit mode.<para></para>
		/// All following commands will be grouped.<para></para>
		/// Commands will not be delayed (they will be applied as soon as they're added).
		/// </summary>
		public void BeginNoDelay() {
			_container.BeginEdit(new GroupCommand<TEntry>(_container, true));
		}

		/// <summary>
		/// Removes the edit mode on the AbstractCommand object. All non-executed <para></para>
		/// commands will be executed.
		/// </summary>
		public void End() {
			_container.EndEdit();
		}

		/// <summary>
		/// Add files to remove for the Thor format only.
		/// </summary>
		/// <param name="grfFiles">The GRF files.</param>
		/// <param name="callback">The callback.</param>
		public void ThorAddFilesToRemove(IEnumerable<string> grfFiles, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFiles, "grfFiles");

			var container = _container as Container;
			if (container != null) {
				if (!container.FileName.IsExtension(".thor"))
					GrfExceptions.ThrowInvalidContainerFormat(container.FileName, ".thor");

				try {
						_container.Lock();
						_container.StoreAndExecute(new AddFilesToRemove<TEntry>(grfFiles, callback));
				}
				finally {
					_container.Unlock();
				}
			}

			GrfExceptions.ThrowInvalidContainerFormat("Unknown", ".thor");
		}

		/// <summary>
		/// Add a file to remove for the Thor format only.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="callback">The callback.</param>
		public void ThorAddFilesToRemove(string grfFile, CCallbacks.AddFilesCallback callback = null) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFile, "grfFile");

			ThorAddFilesToRemove(new string[] {grfFile}, callback);
		}
	}
}