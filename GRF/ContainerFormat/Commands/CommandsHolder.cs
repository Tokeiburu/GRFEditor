using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GRF.Core;
using GRF.IO;
using GRF.GrfSystem;
using Utilities;
using Utilities.Commands;
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
	public class CommandsHolder<TEntry> : AbstractCommand<IContainerCommand<TEntry>> where TEntry : ContainerEntry {
		protected ContainerAbstract<TEntry> _container;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandsHolder{TEntry}"/> class.
		/// </summary>
		/// <param name="container">The container.</param>
		public CommandsHolder(ContainerAbstract<TEntry> container) {
			_container = container;
		}

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute
		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFileInDirectory(@"data\sprite", @"C:\file.spr")<para></para>
		/// ex: AddFileInDirectory(@"data", @"C:\sprites")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileOrFolder">The file.</param>
		/// <param name="callback">The callback.</param>
		public void AddFileInDirectory(string grfPath, string fileOrFolder, CCallbacks.AddFilesCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(fileOrFolder, "fileOrFolder");

			grfPath = GrfPath.CleanGrfPath(grfPath);

			StoreAndExecute(new AddFiles<TEntry>(grfPath, fileOrFolder, callback));
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFileInDirectory(@"data\sprite", @"C:\file.spr")<para></para>
		/// ex: AddFileInDirectory(@"data", @"C:\sprites")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileOrFolder">The file.</param>
		public void AddFileInDirectory(string grfPath, string fileOrFolder) {
			AddFileInDirectory(grfPath, fileOrFolder, null);
		}

		/// <summary>
		/// Adds an existing file at the specified GRF file path.<para></para>
		/// ex: AddFile(@"data\sprite\test.spr", @"C:\file.spr")<para></para>
		/// The file is locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="fileOrFolder">The file.</param>
		/// <param name="callback">The callback.</param>
		public void AddFile(string grfFile, string fileOrFolder, CCallbacks.ReplaceFileCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFile, "grfFile");
			GrfExceptions.IfNullThrow(fileOrFolder, "fileOrFolder");
			if (!File.Exists(fileOrFolder)) GrfExceptions.ThrowFileNotFound(fileOrFolder);

			if (!EncodingService.IsCompatible(grfFile, EncodingService.DisplayEncoding))
				throw GrfExceptions.__InvalidTextEncoding.Create(grfFile);

			StoreAndExecute(new ReplaceFile<TEntry>(Path.GetDirectoryName(grfFile), Path.GetFileName(grfFile), fileOrFolder, callback));
		}

		/// <summary>
		/// Adds an existing file at the specified GRF file path.<para></para>
		/// ex: AddFile(@"data\sprite\test.spr", @"C:\file.spr")<para></para>
		/// The file is locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="fileOrFolder">The file.</param>
		public void AddFile(string grfFile, string fileOrFolder) {
			AddFile(grfFile, fileOrFolder, null);
		}

		/// <summary>
		/// Adds an existing file at the specified GRF file path.<para></para>
		/// ex: AddFile(@"data\sprite\test.spr", @"C:\file.spr")<para></para>
		/// The file is locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="entry">The data.</param>
		public void AddFile(string grfFile, TEntry entry) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(entry, "entry");

			StoreAndExecute(new ReplaceFile<TEntry>(Path.GetDirectoryName(grfFile), Path.GetFileName(grfFile), entry));
		}

		/// <summary>
		/// Adds existing files or folders at the specified GRF path.<para></para>
		/// ex: AddFileInDirectory(@"data\sprite", new string[] { @"C:\file.spr", @"C:\data\folder", @"C:\data\sprite2\file2.spr" })<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="filesOrFolders">The files.</param>
		/// <param name="callback">The callback.</param>
		public void AddFilesInDirectory(string grfPath, IEnumerable<string> filesOrFolders, CCallbacks.AddFilesCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(filesOrFolders, "filesOrFolders");

			List<string> filesList = filesOrFolders.ToList();

			if (filesList.Count == 0)
				return;

			grfPath = GrfPath.CleanGrfPath(grfPath);

			StoreAndExecute(new AddFiles<TEntry>(grfPath, filesList, callback));
		}

		/// <summary>
		/// Adds existing files or folders at the specified GRF path.<para></para>
		/// ex: AddFileInDirectory(@"data\sprite", new string[] { @"C:\file.spr", @"C:\data\folder", @"C:\data\sprite2\file2.spr" })<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="filesOrFolders">The files.</param>
		public void AddFilesInDirectory(string grfPath, IEnumerable<string> filesOrFolders) {
			AddFilesInDirectory(grfPath, filesOrFolders, null);
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileOrFolder">The file.</param>
		/// <param name="callback">The callback.</param>
		public void AddFilesInDirectory(string grfPath, string fileOrFolder, CCallbacks.AddFilesCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			AddFileInDirectory(grfPath, fileOrFolder, callback);
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="fileOrFolder">The file.</param>
		public void AddFilesInDirectory(string grfPath, string fileOrFolder) {
			AddFilesInDirectory(grfPath, fileOrFolder, null);
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="folder">The folder.</param>
		public void AddFilesFromFolder(string grfPath, string folder) {
			GrfExceptions.IfSavingThrow(_container);

			if (!Directory.Exists(folder)) return;

			BeginNoDelay();
			AddFileInDirectory(grfPath, folder, null);
			MergeFolders(GrfPath.Combine(grfPath, Path.GetFileName(folder)), grfPath, null, null);
			End();
		}

		/// <summary>
		/// Adds a file from a byte array at the specified GRF file path.<para></para>
		/// ex: AddFile(@"data\sprite\test.spr", File.ReadAllBytes(@"C:\file.spr"))<para></para>
		/// This method creates a temporary copy of the file.
		/// Use the stream option instead for performance, but it will use more memory.
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="fileData">The file data.</param>
		/// <param name="callback">The callback.</param>
		public void AddFile(string grfFile, byte[] fileData, CCallbacks.ReplaceFileCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(fileData, "fileData");

			string file = TemporaryFilesManager.GetTemporaryFilePath("added_file_{0:0000}");
			File.WriteAllBytes(file, fileData);
			TemporaryFilesManager.LockFile(file);
			StoreAndExecute(new ReplaceFile<TEntry>(Path.GetDirectoryName(grfFile), Path.GetFileName(grfFile), file, callback));
		}

		/// <summary>
		/// Adds a file from a byte array at the specified GRF file path.<para></para>
		/// ex: AddFile(@"data\sprite\test.spr", File.ReadAllBytes(@"C:\file.spr"))
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="fileData">The file data.</param>
		public void AddFile(string grfFile, byte[] fileData) {
			AddFile(grfFile, fileData, null);
		}

		/// <summary>
		/// Adds a file from a byte array at the specified GRF file path.<para></para>
		/// ex: AddFile(@"data\sprite\test.spr", File.ReadAllBytes(@"C:\file.spr"))
		/// </summary>
		/// <param name="grfFile">The GRF file.</param>
		/// <param name="dataStream">The data stream.</param>
		public void AddFile(string grfFile, Stream dataStream) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(dataStream, "dataStream");

			StoreAndExecute(new ReplaceFile<TEntry>(Path.GetDirectoryName(grfFile), Path.GetFileName(grfFile), _container.Resources.CreateMemoryHolder(dataStream)));
		}

		/// <summary>
		/// Removes the file or folder at the specified path.<para></para>
		/// Does nothing if the file or folder is not found.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="callback">The callback.</param>
		public void Remove(string path, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			if (string.IsNullOrEmpty(path))
				throw GrfExceptions.__NullPathException.Create(path);

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
		/// Removes the file or folder at the specified path.<para></para>
		/// Does nothing if the file or folder is not found.
		/// </summary>
		/// <param name="path">The path.</param>
		public void Remove(string path) {
			Remove(path, null);
		}

		/// <summary>
		/// Removes the files or folders at the specified paths.<para></para>
		/// Files or folders not found are ignored.
		/// </summary>
		/// <param name="paths">The paths.</param>
		/// <param name="callback">The callback.</param>
		public void Remove(IEnumerable<string> paths, CCallbacks.DeleteCallback callback) {
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
					CancelEdit();
				}
				finally {
					End();
				}
			}
		}

		/// <summary>
		/// Removes the files or folders at the specified paths.<para></para>
		/// Files or folders not found are ignored.
		/// </summary>
		/// <param name="paths">The paths.</param>
		public void Remove(IEnumerable<string> paths) {
			Remove(paths, null);
		}

		/// <summary>
		/// Removes a folder at the specified GRF path.<para></para>
		/// Warning: Adds a command on the AbstractCommand object even if the folder is not found.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFolder(string grfPath, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);

			if (string.IsNullOrEmpty(grfPath))
				throw GrfExceptions.__NullPathException.Create(grfPath);

			StoreAndExecute(new DeleteFolders<TEntry>(grfPath, callback));
		}

		/// <summary>
		/// Clears the entire content of the GRF.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="callback">The callback.</param>
		public void ClearContent(string grfPath, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			StoreAndExecute(new DeleteFolders<TEntry>("", callback));
		}

		/// <summary>
		/// Removes a folder at the specified GRF path.<para></para>
		/// Warning: Adds a command on the AbstractCommand object even if the folder is not found.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		public void RemoveFolder(string grfPath) {
			RemoveFolder(grfPath, null);
		}

		/// <summary>
		/// Removes folders at the specified GRF paths.<para></para>
		/// Folders not found are ignored.
		/// </summary>
		/// <param name="grfPaths">The GRF paths.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFolders(IEnumerable<string> grfPaths, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfPaths, "grfPaths");
			List<string> paths = grfPaths.ToList();
			if (paths.Count <= 0) return;

			StoreAndExecute(new DeleteFolders<TEntry>(paths, callback));
		}

		/// <summary>
		/// Removes folders at the specified GRF paths.<para></para>
		/// Folders not found are ignored.
		/// </summary>
		/// <param name="grfPaths">The GRF paths.</param>
		public void RemoveFolders(IEnumerable<string> grfPaths) {
			RemoveFolders(grfPaths, null);
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

			StoreAndExecute(new DeleteFiles<TEntry>(grfFile, callback));
		}

		/// <summary>
		/// Removes files at the specified GRF file paths.<para></para>
		/// Files not found are ignored.
		/// </summary>
		/// <param name="grfFiles">The GRF files.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFiles(IEnumerable<string> grfFiles, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(grfFiles, "grfFiles");
			List<string> files = grfFiles.ToList();
			if (files.Count <= 0) return;

			StoreAndExecute(new DeleteFiles<TEntry>(files, callback));
		}

		/// <summary>
		/// Removes files at the specified GRF file paths.<para></para>
		/// Files not found are ignored.
		/// </summary>
		/// <param name="grfFiles">The GRF files.</param>
		public void RemoveFiles(IEnumerable<string> grfFiles) {
			RemoveFiles(grfFiles, null);
		}

		/// <summary>
		/// Removes files using a wildcard search pattern.<para></para>
		/// ex: RemoveFiles("*.lu?")
		/// </summary>
		/// <param name="wildCardSearchPattern">The wild card search pattern.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFiles(string wildCardSearchPattern, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(wildCardSearchPattern, "wildCardSearchPattern");
			Regex regex = new Regex(Methods.WildcardToRegexLine(wildCardSearchPattern), RegexOptions.IgnoreCase);

			List<string> files = _container.Table.Files.Where(p => regex.IsMatch(p)).ToList();

			if (files.Count == 0)
				return;

			StoreAndExecute(new DeleteFiles<TEntry>(files, callback));
		}

		/// <summary>
		/// Removes files using a wildcard search pattern.<para></para>
		/// ex: RemoveFiles("*.lu?")
		/// </summary>
		/// <param name="wildCardSearchPattern">The wild card search pattern.</param>
		public void RemoveFiles(string wildCardSearchPattern) {
			RemoveFiles(wildCardSearchPattern, null);
		}

		/// <summary>
		/// Removes files using a regex search pattern.
		/// </summary>
		/// <param name="searchPattern">The search pattern.</param>
		/// <param name="callback">The callback.</param>
		public void RemoveFilesRegex(string searchPattern, CCallbacks.DeleteCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(searchPattern, "searchPattern");
			Regex regex = new Regex(searchPattern, RegexOptions.IgnoreCase);

			List<string> files = _container.Table.Files.Where(p => regex.IsMatch(p)).ToList();

			if (files.Count == 0)
				return;

			StoreAndExecute(new DeleteFiles<TEntry>(files, callback));
		}

		/// <summary>
		/// Removes files using a regex search pattern.
		/// </summary>
		/// <param name="searchPattern">The search pattern.</param>
		public void RemoveFilesRegex(string searchPattern) {
			RemoveFilesRegex(searchPattern, null);
		}

		/// <summary>
		/// Renames a file or a folder. This method cannot overwrite an existing file.<para></para>
		/// Use AddFile*() methods or MergeFolders to overwrite content.
		/// </summary>
		/// <param name="oldPath">The old path.</param>
		/// <param name="newPath">The new path.</param>
		/// <param name="callback">The callback.</param>
		public void Rename(string oldPath, string newPath, CCallbacks.RenameCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			newPath = EncodingService.FromAnyToDisplayEncoding(newPath);

			if (oldPath == newPath)
				return;

			if (String.IsNullOrEmpty(oldPath) || String.IsNullOrEmpty(newPath))
				throw GrfExceptions.__NullPathException.Create(String.IsNullOrEmpty(oldPath) ? oldPath : newPath);

			if (!EncodingService.IsCompatible(newPath, EncodingService.DisplayEncoding))
				throw GrfExceptions.__InvalidTextEncoding.Create(newPath);

			if (GrfPath.GetDirectoryName(oldPath) == newPath)
				throw GrfExceptions.__NullPathException.Create(oldPath);

			if (_container.Table.ContainsFile(oldPath)) {
				if (_container.Table.ContainsFile(newPath))
					throw GrfExceptions.__FileNameAlreadyExists.Create(newPath);

				if (_container.Table.Directories.Contains(newPath))
					throw GrfExceptions.__NullPathException.Create(oldPath);

				StoreAndExecute(new RenameFile<TEntry>(oldPath, newPath, callback));
			}
			else if (_container.Table.ContainsDirectory(oldPath)) {
				if ((newPath.StartsWith(oldPath.TrimEnd('\\', '/') + "\\")))
					throw GrfExceptions.__DestIsSubfolder.Create();

				if (_container.Table.HiddenDirectories.Contains(newPath))
					throw GrfExceptions.__HiddenFolderConflict.Create(newPath);

				// This is no longer allowed. Use the MergeFolders method instead
				if (_container.Table.Directories.Contains(newPath))
					GrfExceptions.ThrowFolderNameAlreadyExists(newPath);

				StoreAndExecute(new RenameFolder<TEntry>(oldPath, newPath, callback));
			}
			else {
				throw GrfExceptions.__PathNotFound.Create(oldPath);
			}
		}

		/// <summary>
		/// Renames a file or a folder. This method cannot overwrite an existing file.<para></para>
		/// Use AddFile*() methods or MergeFolders to overwrite content.
		/// </summary>
		/// <param name="oldPath">The old path.</param>
		/// <param name="newPath">The new path.</param>
		public void Rename(string oldPath, string newPath) {
			Rename(oldPath, newPath, null);
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

				MoveFiles(oldGrfPath, newGrfPath, _container.Table.GetFiles(oldGrfPath, null, SearchOption.AllDirectories, false).Select(p => p.ReplaceFirst(GrfPath.CleanGrfPath(oldGrfPath) + "\\", "")).ToList(), callbackAddFiles);

				if (string.IsNullOrEmpty(oldGrfPath))
					throw GrfExceptions.__NullPathException.Create(oldGrfPath);

				StoreAndExecute(new DeleteFolders<TEntry>(oldGrfPath, callbackDelete));
			}
			catch {
				CancelEdit();
				throw;
			}
			finally {
				_container.Commands.End();
			}
		}

		/// <summary>
		/// Moves a GRF path to a new one. If files are conflicted, they are overwritten.
		/// </summary>
		/// <param name="oldGrfPath">The old GRF path.</param>
		/// <param name="newGrfPath">The new GRF path.</param>
		public void MergeFolders(string oldGrfPath, string newGrfPath) {
			MergeFolders(oldGrfPath, newGrfPath, null, null);
		}

		/// <summary>
		/// Adds a GRF path.<para></para>
		/// Warning: Adding a GRF path does absolutely nothing to the GRF structure.<para></para>
		/// Warning: If you check with container.Table.ContainsDirectory("data\\myNewDirectory")<para></para>
		/// it will return false. A directory needs at least 1 file to exist.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="callback">The callback.</param>
		public void AddFolder(string grfPath, CCallbacks.AddFolderCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			if (String.IsNullOrEmpty(grfPath))
				throw GrfExceptions.__NullPathException.Create(grfPath);

			if (_container.Table.ContainsDirectory(grfPath))
				return;

			if (!EncodingService.IsCompatible(grfPath, EncodingService.DisplayEncoding))
				throw GrfExceptions.__InvalidTextEncoding.Create(grfPath);

			if (GrfPath.ContainsInvalidCharacters(grfPath))
				throw GrfExceptions.__InvalidCharactersInPath.Create(grfPath);

			StoreAndExecute(new AddFolder<TEntry>(grfPath, callback));
		}

		/// <summary>
		/// Adds a GRF path.<para></para>
		/// Warning: Adding a GRF path does absolutely nothing to the GRF structure.<para></para>
		/// Warning: If you check with container.Table.ContainsDirectory("data\\myNewDirectory")<para></para>
		/// it will return false. A directory needs at least 1 file to exist.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		public void AddFolder(string grfPath) {
			AddFolder(grfPath, null as CCallbacks.AddFolderCallback);
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="path">The path.</param>
		/// <param name="callback">The callback.</param>
		public void AddFolder(string grfPath, string path, CCallbacks.AddFilesCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			AddFilesInDirectory(grfPath, path, callback);
		}

		/// <summary>
		/// Adds an existing file or folder at the specified GRF path.<para></para>
		/// ex: AddFilesInDirectory(@"data\sprite", @"C:\data\folder")<para></para>
		/// The files are locked automatically if LockFiles is enabled in GRF.System.Settings.
		/// </summary>
		/// <param name="grfPath">The GRF path.</param>
		/// <param name="path">The path.</param>
		public void AddFolder(string grfPath, string path) {
			AddFolder(grfPath, path, null);
		}

		/// <summary>
		/// Moves a file or a folder at the specified new location.<para></para>
		/// Warning: This method does overwrite existing files, but not existing folders.
		/// </summary>
		/// <param name="oldPath">The old path.</param>
		/// <param name="newPath">The new path.</param>
		/// <param name="callback">The callback.</param>
		public void Move(string oldPath, string newPath, CCallbacks.RenameCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			newPath = EncodingService.FromAnyToDisplayEncoding(newPath);

			if (String.IsNullOrEmpty(oldPath) || String.IsNullOrEmpty(newPath))
				throw GrfExceptions.__NullPathException.Create(String.IsNullOrEmpty(oldPath) ? oldPath : newPath);

			if (!EncodingService.IsCompatible(newPath, EncodingService.DisplayEncoding))
				throw GrfExceptions.__InvalidTextEncoding.Create(newPath);

			if (_container.Table.ContainsFile(oldPath)) {
				if (_container.Table.ContainsDirectory(newPath))
					throw GrfExceptions.__DestMustBeFile.Create(oldPath, newPath);

				StoreAndExecute(new MoveFile<TEntry>(oldPath, newPath, callback));
			}
			else if (_container.Table.ContainsDirectory(oldPath)) {
				if (_container.Table.ContainsFile(newPath))
					throw GrfExceptions.__DestMustBeFolder.Create(oldPath, newPath);

				if ((newPath.StartsWith(oldPath.TrimEnd('\\', '/') + "\\")))
					throw GrfExceptions.__DestIsSubfolder.Create();

				if (_container.Table.ContainsDirectory(newPath))
					throw GrfExceptions.__FolderNameAlreadyExists.Create(newPath);

				StoreAndExecute(new RenameFolder<TEntry>(oldPath, newPath, callback));
			}
			else {
				throw GrfExceptions.__PathNotFound.Create(oldPath);
			}
		}

		/// <summary>
		/// Moves a file or a folder at the specified new location.<para></para>
		/// Warning: This method does overwrite existing files, but not existing folders.
		/// </summary>
		/// <param name="oldPath">The old path.</param>
		/// <param name="newPath">The new path.</param>
		public void Move(string oldPath, string newPath) {
			Move(oldPath, newPath, null);
		}

		/// <summary>
		/// Moves GRF files from a GRF path to another.<para></para>
		/// ex: MoveFiles(@"data\sprite", @"data", new string[] { "test.spr", "test.act" })
		/// </summary>
		/// <param name="oldGrfPath">The old GRF path.</param>
		/// <param name="newGrfPath">The new GRF path.</param>
		/// <param name="fileNames">The file names.</param>
		/// <param name="callback">The callback.</param>
		public void MoveFiles(string oldGrfPath, string newGrfPath, IEnumerable<string> fileNames, CCallbacks.AddFilesCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			GrfExceptions.IfNullThrow(oldGrfPath, "oldGrfPath");
			GrfExceptions.IfNullThrow(newGrfPath, "newGrfPath");

			if (oldGrfPath == newGrfPath) return;

			StoreAndExecute(new MoveFiles<TEntry>(oldGrfPath, newGrfPath, fileNames, callback));
		}

		/// <summary>
		/// Moves GRF files from a GRF path to another.<para></para>
		/// ex: MoveFiles(@"data\sprite", @"data", new string[] { "test.spr", "test.act" })
		/// </summary>
		/// <param name="oldGrfPath">The old GRF path.</param>
		/// <param name="newGrfPath">The new GRF path.</param>
		/// <param name="fileNames">The file names.</param>
		public void MoveFiles(string oldGrfPath, string newGrfPath, IEnumerable<string> fileNames) {
			MoveFiles(oldGrfPath, newGrfPath, fileNames, null);
		}

		/// <summary>
		/// Changes the version of the container.
		/// </summary>
		/// <param name="major">The major.</param>
		/// <param name="minor">The minor.</param>
		/// <param name="callback">The callback.</param>
		public void ChangeVersion(byte major, byte minor, CCallbacks.ChangeVersionCallback callback) {
			GrfExceptions.IfSavingThrow(_container);
			if (_container.Header.Is(major, minor)) return;

			StoreAndExecute(new ChangeVersion<TEntry>(major, minor, callback));
		}

		/// <summary>
		/// Changes the version of the container.
		/// </summary>
		/// <param name="major">The major.</param>
		/// <param name="minor">The minor.</param>
		public void ChangeVersion(byte major, byte minor) {
			ChangeVersion(major, minor, null);
		}

		/// <summary>
		/// Changes the magic header of the GRF.
		/// </summary>
		/// <param name="header">The file signature header.</param>>
		/// <param name="callback">The callback.</param>>
		public void ChangeHeader(string header, CCallbacks.ChangeHeaderCallback callback) {
			GrfExceptions.IfSavingThrow(_container);

			if (header == _container.Header.Magic) return;
			StoreAndExecute(new ChangeHeader<TEntry>(header, callback));
		}

		/// <summary>
		/// Changes the magic header of the GRF.
		/// </summary>
		/// <param name="header">The file signature header.</param>>
		public void ChangeHeader(string header) {
			ChangeHeader(header, null);
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

			StoreAndExecute(new EncryptFiles<TEntry>(files, callback));
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

			StoreAndExecute(new DecryptFiles<TEntry>(files, callback));
		}

		/// <summary>
		/// Sets the AbstractCommand object in the edit mode.<para></para>
		/// All following commands will be grouped.<para></para>
		/// Commands will be delayed (they will not be applied until End is called).
		/// </summary>
		public void Begin() {
			BeginEdit(new GroupCommand<TEntry>(_container, false));
		}

		/// <summary>
		/// Sets the AbstractCommand object in the edit mode.<para></para>
		/// All following commands will be grouped.<para></para>
		/// Commands will not be delayed (they will be applied as soon as they're added).
		/// </summary>
		public void BeginNoDelay() {
			BeginEdit(new GroupCommand<TEntry>(_container, true));
		}

		/// <summary>
		/// Removes the edit mode on the AbstractCommand object. All non-executed <para></para>
		/// commands will be executed.
		/// </summary>
		public void End() {
			EndEdit();
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
					throw GrfExceptions.__InvalidContainerFormat.Create(container.FileName, ".thor");

				StoreAndExecute(new AddFilesToRemove<TEntry>(grfFiles, callback));
			}
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

		/// <summary>
		/// Executes the specified command.
		/// </summary>
		/// <param name="command">The command.</param>
		protected override void _execute(IContainerCommand<TEntry> command) {
			command.Execute(_container);
			_container.Table.InvalidateInternalSets();
		}

		/// <summary>
		/// Undoes the specified command.
		/// </summary>
		/// <param name="command">The command.</param>
		protected override void _undo(IContainerCommand<TEntry> command) {
			command.Undo(_container);
			_container.Table.InvalidateInternalSets();
		}

		/// <summary>
		/// Redoes the specified command.
		/// </summary>
		/// <param name="command">The command.</param>
		protected override void _redo(IContainerCommand<TEntry> command) {
			command.Execute(_container);
			_container.Table.InvalidateInternalSets();
		}
	}
}