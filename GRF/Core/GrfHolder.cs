using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.ContainerFormat.Commands;
using GRF.FileFormats.RgzFormat;
using GRF.IO;
using GRF.GrfSystem;
using GRF.Threading;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Encryption;

namespace GRF.Core {
	/// <summary>
	/// GrfHolder is a class that holds the Container and
	/// is used to ensure the streams are handled properly.
	/// It also adds functionalities to the Container object.
	/// 
	/// Any operation must be validated before being applied
	/// to the actual object. The exceptions should not be
	/// handled by the ErrorManager; if an error is encountered
	/// it means there is a problem with the code.
	/// 
	/// Exceptions usually occur because the GRF object wasn't
	/// disposed (or closed) before loading a new GRF.
	/// </summary>
	public class GrfHolder : IProgress, IDisposable {
		#region Delegates

		public delegate void GrfHolderEventHandler(object sender);

		#endregion

		protected bool _grfClosed = true;
		private Container _internalGrf;

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfHolder" /> class.
		/// </summary>
		public GrfHolder() {
			_grfClosed = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfHolder" /> class.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		public GrfHolder(string fileName) : this(fileName, GrfLoadOptions.Normal) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfHolder" /> class.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="options">The options.</param>
		public GrfHolder(string fileName, GrfLoadOptions options) : this() {
			_grfClosed = true;
			Open(fileName, options);
		}

		/// <summary>
		/// Property used to attach objects to the container.
		/// </summary>
		public TkDictionary<string, object> Attached {
			get { return _grf.Attached; }
		}

		/// <summary>
		/// Gets an attached value and converts it to the requested format.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">The key.</param>
		/// <returns>The attached properties</returns>
		public T GetAttachedProperty<T>(string key) {
			object value;

			if (_grf.Attached.TryGetValue(key, out value)) {
				return (T)value;
			}

			return default(T);
		}

		/// <summary>
		/// Gets a value indicating whether this container is busy.
		/// </summary>
		public virtual bool IsBusy {
			get { return (!_grfClosed && _internalGrf != null) && _grf.IsBusy; }
			internal set { _grf.IsBusy = value; }
		}

		/// <summary>
		/// Gets a value indicating whether this container is closed.
		/// </summary>
		public virtual bool IsClosed => _grfClosed;

		/// <summary>
		/// Gets a value indicating whether this container is opened.
		/// </summary>
		public virtual bool IsOpened => !_grfClosed;

		/// <summary>
		/// Gets the header.
		/// </summary>
		public virtual GrfHeader Header => _grf.InternalHeader;

		/// <summary>
		/// Gets the file table.
		/// </summary>
		public virtual FileTable FileTable => _grf.InternalTable;

		/// <summary>
		/// Gets a value indicating whether this container has been modified.
		/// </summary>
		public bool IsModified => _grf.IsModified;

		/// <summary>
		/// Gets or sets the last SaveResult outcome of a save operation.
		/// </summary>
		public ContainerSaveResult LastSaveResult {
			get => _grf.SaveResult;
		}

		/// <summary>
		/// Gets the name of the opened file.
		/// </summary>
		public virtual string FileName => _grf.FileName;

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a new file.
		/// </summary>
		public virtual bool IsNewGrf {
			get => _grf.IsNewGrf;
			set => _grf.IsNewGrf = value;
		}

		private Container _grf {
			get {
				if (_grfClosed)
					throw GrfExceptions.__ReadContainerNotOpened.Create();

				if (_internalGrf == null)
					throw GrfExceptions.__ReadContainerNotProperlyLoaded.Create();

				return _internalGrf;
			}
			set {
				_internalGrf = value;
			}
		}

		internal Container Container => _grf;

		/// <summary>
		/// Main component to execute commands on the GRF.
		/// </summary>
		public CommandsHolder<FileEntry> Commands => _grf.Commands;

		#region IDisposable Members

		public void Dispose() {
			Close();
		}

		#endregion

		#region IProgress Members

		/// <summary>
		/// Gets or sets the progress.
		/// </summary>
		public virtual float Progress {
			get {
				if (_internalGrf == null)
					return -1;
				return _grf.Progress;
			}
			set {
				if (_internalGrf != null)
					_grf.Progress = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cancelling.
		/// </summary>
		public virtual bool IsCancelling {
			get {
				if (_internalGrf == null)
					return false;
				return _grf.IsCancelling;
			}
			set {
				if (_internalGrf != null)
					_grf.IsCancelling = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cancelled.
		/// </summary>
		public virtual bool IsCancelled {
			get {
				if (_internalGrf == null)
					return false;
				return _grf.IsCancelled;
			}
			set {
				if (_internalGrf != null)
					_grf.IsCancelled = value;
			}
		}

		#endregion

		/// <summary>
		/// Occurs when the container opens.
		/// </summary>
		public event GrfHolderEventHandler ContainerOpened;

		internal virtual void OnContainerOpened() {
			GrfHolderEventHandler handler = ContainerOpened;
			if (handler != null) handler(this);
		}

		/// <summary>
		/// Occurs when the container closes.
		/// </summary>
		public event GrfHolderEventHandler ContainerClosed;

		internal virtual void OnContainerClosed() {
			GrfHolderEventHandler handler = ContainerClosed;
			if (handler != null) handler(this);
		}

		/// <summary>
		/// Sets the encryption key using the direct password.
		/// </summary>
		/// <param name="keypass">The keypass.</param>
		public void SetEncryptionPassword(string keypass) {
			_validateOperation(Condition.Opened);

			SetEncryptionKey(Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(Ee322.fec67f91f4ef59f498874efbdd21c1c1(keypass)));
		}

		/// <summary>
		/// Sets the encryption key using the .grfkey file.
		/// </summary>
		/// <param name="key">The key.</param>
		public void SetEncryptionKeyFile(byte[] key) {
			_validateOperation(Condition.Opened);

			SetEncryptionKey(Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(key));
		}

		/// <summary>
		/// Sets the 256 byte encryption key.
		/// </summary>
		/// <param name="key">The key.</param>
		public void SetEncryptionKey(byte[] key) {
			_validateOperation(Condition.Opened);

			Header.SetKey(key, this);
			Header.EncryptionManualSet = true;
		}

		/// <summary>
		/// Detects the encrypted files and sets the flag.
		/// </summary>
		public void SetEncryptionFlag(bool forceSet = false) {
			Header.EncryptionCheckFlag = true;

			if (forceSet)
				_grf.InternalHeader.IsEncrypted = true;

			if (_grf.InternalHeader.IsEncrypted || forceSet) {
				try {
					string file = File.GetLastWriteTimeUtc(_grf.FileName).ToFileTimeUtc() + "\\files.enc";

					using (GrfHolder grf = new GrfHolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GrfStrings.EncryptionDbFile), GrfLoadOptions.OpenOrNew)) {
						if (grf.FileTable.ContainsFile(file)) {
							var encryptedFiles = Encoding.Default.GetString(grf.FileTable[file].GetDecompressedData());
						
							foreach (var line in encryptedFiles.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
								if (!string.IsNullOrEmpty(line)) {
									var entry = _grf.Table.TryGet(line);
									if (entry != null && ((entry.Flags & EntryType.GravityEncryptedFile) != EntryType.GravityEncryptedFile)) {
										entry.Flags |= EntryType.GrfEditorCrypted;
										entry.OnPropertyChanged("Encrypted");
									}
								}
							}
						
							if (IsOpened)
								Header.EncryptionCheckFlag = false;
						
							return;
						}
					}

					AProgress.Init(_grf);
					GrfThreadPool<FileEntry> pool = new GrfThreadPool<FileEntry>();
					pool.Initialize<ThreadSetEncryption>(_grf, FileTable.Entries.OrderBy(p => p.FileExactOffset).ToList(), 1);
					pool.Start(v => _grf.Progress = v, () => _grf.IsCancelling);

					WriteEncryptionIndex(_grf, file);
				}
				catch (Exception) {
					//ErrorHandler.HandleException(err);
				}
				finally {
					if (IsOpened)
						AProgress.Finalize(_grf);
				}
			}

			if (IsOpened)
				Header.EncryptionCheckFlag = false;
		}

		internal static void WriteEncryptionIndex(Container container, string fileUid) {
			try {
				using (GrfHolder grf = new GrfHolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GrfStrings.EncryptionDbFile), GrfLoadOptions.OpenOrNew)) {
					StringBuilder files = new StringBuilder();

					foreach (var entry in container.Table.Entries.Where(p => p.Encrypted)) {
						files.AppendLine(entry.RelativePath);
					}

					grf.Commands.AddFile(fileUid, Encoding.Default.GetBytes(files.ToString()));
					grf.Save();
				}
			}
			catch {
				// Ignore any potential errors
			}
		}

		/// <summary>
		/// Detects the encrypted files and sets the flag.
		/// </summary>
		public void SetCustomCompressionFlag() {
			GrfThread.Start(delegate {
				try {
					AProgress.Init(_grf);
					GrfThreadPool<FileEntry> pool = new GrfThreadPool<FileEntry>();
					pool.Initialize<ThreadSetCustomCompression>(_grf, FileTable.Entries, 3);
					pool.Start(v => _grf.Progress = v, () => _grf.IsCancelling);
				}
				catch (Exception) {
					//ErrorHandler.HandleException(err);
				}
				finally {
					AProgress.Finalize(_grf);
				}
			});
		}

		/// <summary>
		/// Cancels any operation currently done by the GRF.
		/// </summary>
		public virtual void Cancel() {
			_validateOperation(Condition.Opened);
			_internalGrf.IsCancelling = true;
		}

		/// <summary>
		/// Closes the current GRF. Must be called before reopening a new GRF.
		/// </summary>
		public virtual void Close() {
			if (_internalGrf == null)
				return;

			if (_internalGrf.InternalHeader != null)
				_internalGrf.InternalHeader.EncryptionCheckFlag = false;
			_internalGrf.Dispose();

			if (FileTable != null)
				FileTable.Delete();

			_grfClosed = true;
			_internalGrf = null;

			OnContainerClosed();
		}

		/// <summary>
		/// Repacks the GRF synchronously.
		/// </summary>
		public ContainerSaveResult Repack() {
			return RepackAs(null, SyncMode.Synchronous);
		}

		/// <summary>
		/// Repacks the GRF.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult RepackAs(string fileName, SyncMode syncMode = SyncMode.Synchronous) {
			_validateOperation(Condition.Opened);
			return _internalGrf.Save(fileName, null, SavingMode.Repack, syncMode);
		}

		/// <summary>
		/// Saves the GRF.
		/// </summary>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult Save(SyncMode syncMode = SyncMode.Synchronous) {
			_validateOperation(Condition.Opened);

			string extension = _grf.FileName.GetExtension();

			SavingMode mode;

			switch (extension) {
				case ".rgz":
					mode = SavingMode.Rgz;
					break;
				case ".thor":
					mode = SavingMode.Thor;
					break;
				default:
					mode = SavingMode.FileEdit;
					break;
			}

			return _internalGrf.Save(null, null, mode, syncMode);
		}

		/// <summary>
		/// Saves the GRF.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult SaveAs(string fileName, SyncMode syncMode = SyncMode.Synchronous) {
			_validateOperation(Condition.Opened);

			string extension = (fileName ?? _internalGrf.FileName).GetExtension();
			SavingMode mode;

			switch (extension) {
				case ".rgz":
					mode = SavingMode.Rgz;
					break;
				case ".thor":
					mode = SavingMode.Thor;
					break;
				default:
					mode = SavingMode.FileCopy;
					break;
			}

			return _internalGrf.Save(fileName, null, mode, syncMode);
		}

		/// <summary>
		/// Defragment the GRF by copying all its content doing a SaveAs.
		/// </summary>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult Defragment(SyncMode syncMode = SyncMode.Synchronous) => SaveAs(null, syncMode);

		/// <summary>
		/// Defragment the GRF by copying all its content doing a SaveAs.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult DefragmentAs(string fileName, SyncMode syncMode = SyncMode.Synchronous) => SaveAs(fileName, syncMode);

		/// <summary>
		/// Redirects identical entries in the GRF table to save space.
		/// </summary>
		public ContainerSaveResult Compact() => CompactAs(null);

		/// <summary>
		/// Redirects identical entries in the GRF table to save space.
		/// </summary>
		public ContainerSaveResult CompactAs(string fileName) {
			_validateOperation(Condition.Opened);
			return _grf.Save(fileName, null, SavingMode.Compact, SyncMode.Synchronous);
		}

		private void _validateOperation(Condition condition) {
			if (_internalGrf == null && !_grfClosed) {
				throw GrfExceptions.__NonInstiatedContainer.Create();
			}

			if ((_grfClosed && condition == Condition.Opened) || (!_grfClosed && condition == Condition.Closed)) {
				if (_grfClosed)
					throw GrfExceptions.__GrfAccessViolationClosed.Create();
				else
					throw GrfExceptions.__GrfAccessViolationOpened.Create();
			}
		}

		/// <summary>
		/// Creates a new GRF (the filename isn't used).
		/// </summary>
		/// <param name="defaultName">The default name.</param>
		public void New(string defaultName = "new.grf") {
			_validateOperation(Condition.Closed);
			_grfClosed = false;
			_grf = new Container();
			_internalGrf.FileName = defaultName;
			OnContainerOpened();
		}

		/// <summary>
		/// Opens the specified GRF synchronously.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		public void Open(string fileName) {
			Open(fileName ?? FileName, 0);
		}

		public void Open(string fileName, GrfLoadOptions options) {
			Open(fileName, options, null);
		}

		public void Open(string fileName, GrfLoadOptions options, GrfLoadData loadData) {
			Close();
			_validateOperation(Condition.Closed);

			try {
				if (options.HasFlags(GrfLoadOptions.Repair)) {
					if ((options & ~GrfLoadOptions.Repair) != 0) {
						throw GrfExceptions.__InvalidRepairArguments.Create();
					}
				}
				else if (options.HasFlags(GrfLoadOptions.OpenOrNew)) {
					_grfClosed = false;
					if (File.Exists(fileName)) {
						_grf = GrfContainerProvider.Get(fileName, loadData);
					}
					else {
						_grf = new Container();
						_internalGrf.FileName = fileName;
					}
				}
				else if (options.HasFlags(GrfLoadOptions.New)) {
					_grfClosed = false;
					_grf = new Container();
					_internalGrf.FileName = fileName;
				}
				else {
					_grfClosed = false;
					_grf = GrfContainerProvider.Get(fileName, loadData);
				}

				OnContainerOpened();
			}
			catch {
				_grfClosed = true;
				_internalGrf = null;
				throw;
			}
		}

		/// <summary>
		/// Merges the current GRF with another GRF.
		/// </summary>
		/// <param name="grfAdd">The GRF add.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult Merge(GrfHolder grfAdd, SyncMode syncMode = SyncMode.Synchronous) {
			switch (FileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					return _grf.Save(null, grfAdd?.Container, SavingMode.FileEdit, syncMode);
				default:
					throw GrfExceptions.__MergeNotSupported.Create();
			}
		}

		/// <summary>
		/// Merges the current GRF with another GRF to a specific destination.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="grfAdd">The GRF add.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public ContainerSaveResult MergeAs(string fileName, GrfHolder grfAdd, SyncMode syncMode = SyncMode.Synchronous) {
			switch (FileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					return _grf.Save(fileName, grfAdd?.Container, SavingMode.FileCopy, syncMode);
				default:
					throw GrfExceptions.__MergeNotSupported.Create();
			}
		}

		/// <summary>
		/// Gets the size of all files in the GRF.
		/// </summary>
		/// <returns></returns>
		public List<Utilities.Extension.Tuple<string, string>> GetSizes() {
			return _grf.GetFileCompressedSizes();
		}

		/// <summary>
		/// Generates a patch file by removing the newer entries from newerGrf.
		/// </summary>
		/// <param name="newerGrf">The newer GRF.</param>
		/// <param name="filename">The output filename.</param>
		public void Patch(GrfHolder newerGrf, string filename) {
			_validateOperation(Condition.Opened);

			try {
				AProgress.Init(newerGrf);
				GrfPatcher.Patch(_internalGrf, newerGrf.Container, filename);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				IsBusy = false;
				AProgress.Finalize(newerGrf);
				Progress = 100.0f;
			}
		}

		/// <summary>
		/// Executes an operation on the GRF entries on multiple threads.
		/// </summary>
		/// <param name="progress">The progress method.</param>
		/// <param name="isCancelling">The cancelling method.</param>
		/// <param name="action">The action.</param>
		public void ThreadOperation(Action<float> progress, Func<bool> isCancelling, Action<FileEntry, byte[]> action) {
			_validateOperation(Condition.Opened);
			_grf.ThreadOperation(progress, isCancelling, action);
		}

		/// <summary>
		/// Extracts files from a container; this method is highly optimized. For single file extraction, use entry.Extract*(...).
		/// </summary>
		/// <param name="destinationPath">The destination path where the files will be extracted (set to null to extract at the GRF location).</param>
		/// <param name="grfPath">The GRF path (selected node).</param>
		/// <param name="searchOption">The search option (root files only or all files).</param>
		/// <param name="entries">The entries (set to null to extract entire GRF nodes).</param>
		/// <param name="dataPath">Application data path.</param>
		/// <param name="options">The options.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public void Extract(string destinationPath, string grfPath, SearchOption searchOption, IEnumerable<FileEntry> entries, string dataPath, ExtractOptions options, SyncMode syncMode) {
			_validateOperation(Condition.Opened);
			GrfExceptions.IfNullThrow(grfPath, "grfPath");
			List<FileEntry> entriesList = entries == null ? null : new List<FileEntry>(entries);

			if (syncMode == SyncMode.Synchronous)
				_extract(destinationPath, grfPath, searchOption, entriesList, options, dataPath);
			else
				GrfThread.Start(() => _extract(destinationPath, grfPath, searchOption, entriesList, options, dataPath), "GRF - GrfHolder extract thread");
		}

		/// <summary>
		/// Extracts files from a container; this method is highly optimized. For single file extraction, use entry.Extract*(...).
		/// </summary>
		/// <param name="destinationPath">The destination path where the files will be extracted (set to null to extract at the GRF location).</param>
		/// <param name="entries">The entries (set to null to extract entire GRF nodes).</param>
		/// <param name="dataPath">Application data path.</param>
		/// <param name="options">The options.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public void Extract(string destinationPath, IEnumerable<FileEntry> entries, string dataPath, ExtractOptions options, SyncMode syncMode) {
			_validateOperation(Condition.Opened);
			GrfExceptions.IfNullThrow(entries, "grfPath");

			if (syncMode == SyncMode.Synchronous)
				_extract(destinationPath, new List<FileEntry>(entries), options, dataPath);
			else
				GrfThread.Start(() => _extract(destinationPath, new List<FileEntry>(entries), options, dataPath), "GRF - GrfHolder extract thread");
		}

		private string _noRoot(string path, ExtractOptions options = ExtractOptions.Normal) {
			if (path == null)
				return "";

			if (path == "root")
				return "";

			path = path.ReplaceFirst(Rgz.Root, "");

			if ((options & ExtractOptions.ExtractAllInSameFolder) == ExtractOptions.ExtractAllInSameFolder) {
				path = Path.GetFileName(path);
			}

			return path;
		}

		private void _extract(string destinationPath, string grfPath, SearchOption searchOption, List<FileEntry> entries, ExtractOptions options, string dataPath) {
			OpeningMode openingMode = OpeningMode.None;
			List<string> files = new List<string>();
			var pathToOpen = "";
			var originalDestinationPath = destinationPath;

			try {
				if (entries != null && entries.Count == 0) {
					return;
				}

				GrfExceptions.IfEncryptionCheckFlagThrow(this);
				GrfExceptions.IfSavingThrow(_grf);
				GrfExceptions.IfNullThrow(grfPath, "grfPath");
				Progress = -1;
				IsCancelling = false;
				IsCancelled = false;

				var nodeName = GrfPath.GetSingleName(grfPath, -1) ?? "";
				var grfPathSlash = (grfPath.TrimEnd('\\', '/') + "\\");
				openingMode = OpeningMode.Folder;

				// Detect the destination path - null when the extraction is done at the root of the GRF
				if (destinationPath == null) {
					var basePath = GrfPath.GetDirectoryName(new FileInfo(_grf.FileName).FullName);

					// Entries are null when extracting folders
					if (entries == null) {
						destinationPath = GrfPath.Combine(basePath, _noRoot(GrfPath.GetDirectoryName(grfPath)));
						pathToOpen = GrfPath.Combine(destinationPath, _noRoot(nodeName));
					}
					else {
						if (entries.All(p => GrfPath.GetDirectoryName(p.RelativePath) == grfPath)) {
							destinationPath = GrfPath.Combine(basePath, _noRoot(grfPath));
							pathToOpen = destinationPath;
						}
						else {
							// This happens when the files extracted have no common extraction properties
							// No explorer windows should be opened.
							destinationPath = basePath;
							pathToOpen = "";
							openingMode = OpeningMode.None;
						}
					}
				}
				else {
					// root
					if (searchOption == SearchOption.TopDirectoryOnly) {
						nodeName = "";
						pathToOpen = destinationPath;
					}
					// all sub files
					else {
						pathToOpen = GrfPath.Combine(destinationPath, _noRoot(nodeName));
					}
				}

				// Load the entries
				if (entries == null) {
					entries = _grf.Table.EntriesInDirectory(grfPath, searchOption, options.HasFlags(ExtractOptions.IgnoreCase));

					if (entries.Count == 0)
						return;

					foreach (var entry in entries) {
						entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(nodeName), _noRoot(entry.RelativePath.ReplaceFirst(grfPathSlash, "", StringComparison.OrdinalIgnoreCase)));
					}

					if (searchOption == SearchOption.TopDirectoryOnly) {
						openingMode = OpeningMode.MultipleFiles;
						files = entries.Select(p => p.ExtractionFilePath).ToList();
					}
				}
				else {
					if (entries.All(p => GrfPath.GetDirectoryName(p.RelativePath).Equals(grfPath, options.HasFlags(ExtractOptions.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))) {
						pathToOpen = destinationPath;

						foreach (var entry in entries) {
							entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(entry.RelativePath.ReplaceFirst(grfPathSlash, "", StringComparison.OrdinalIgnoreCase)));
						}

						openingMode = OpeningMode.MultipleFiles;
						files = entries.Select(p => p.ExtractionFilePath).ToList();
					}
					else {
						openingMode = OpeningMode.None;

						foreach (var entry in entries) {
							entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(entry.RelativePath));
						}
					}
				}

				if ((options & ExtractOptions.UseAppDataPathToExtract) == ExtractOptions.UseAppDataPathToExtract &&
					originalDestinationPath == null) {
					var basePath = GrfPath.GetDirectoryName(new FileInfo(_grf.FileName).FullName) + "\\";
					var newPath = dataPath.TrimEnd('/', '\\') + "\\";

					foreach (var entry in entries) {
						entry.ExtractionFilePath = entry.ExtractionFilePath.ReplaceFirst(basePath, newPath);
					}

					if (pathToOpen != null) {
						pathToOpen = pathToOpen.TrimEnd('/', '\\') + "\\";
						pathToOpen = pathToOpen.ReplaceFirst(basePath, newPath);
					}

					for (int index = 0; index < files.Count; index++) {
						files[index] = files[index].ReplaceFirst(basePath, newPath);
					}
				}

				foreach (string folder in entries.Select(p => GrfPath.GetDirectoryName(p.ExtractionFilePath)).Distinct()) {
					if (!Directory.Exists(folder))
						new DirectoryInfo(folder).Create();
				}

				int numOfThreads = Methods.Clamp(entries.Count / 50, 1, Settings.MaximumNumberOfThreads);

				if ((options & ExtractOptions.SingleThreaded) == ExtractOptions.SingleThreaded) {
					numOfThreads = 1;
				}

				bool overrideCpuPerf = (options & ExtractOptions.OverrideCpuPerf) == ExtractOptions.OverrideCpuPerf;

				entries = entries.OrderBy(p => p.FileExactOffset).ToList();
				var pool = new GrfThreadPool<FileEntry>();
				pool.Initialize<GrfThreadExtract>(_grf, entries, numOfThreads);
				pool.Start(v => Progress = v, () => IsCancelling, !overrideCpuPerf, true, errorHandling: false);

				List<ExtractionResult> results = new List<ExtractionResult>();
				Exception exception = null;

				foreach (var t in pool.Threads.OfType<GrfThreadExtract>()) {
					results.AddRange(t.ErrorResults);
					
					if (exception == null && t.Exception != null)
						exception = t.Exception;
				}

				if (results.Count > 0) {
					StringBuilder b = new StringBuilder();

					b.AppendLine("Some files could not be extracted:");

					foreach (var result in results) {
						b.AppendLine("#" + result.Entry.RelativePath + " - " + result.Reason);
					}

					ErrorHandler.HandleException(b.ToString());
				}
				else if (exception != null) {
					ErrorHandler.HandleException("Generic failure: a task in the thread pool has failed to finish properly. The current operation will be cancelled.", exception);
				}
			}
			catch (OperationCanceledException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
				IsCancelling = false;
				IsCancelled = true;

				if ((options & ExtractOptions.OpenAfterExtraction) == ExtractOptions.OpenAfterExtraction && (entries != null && entries.Count > 0)) {
					SelectFiles(openingMode, pathToOpen, files);
				}
			}
		}

		private void _extract(string destinationPath, List<FileEntry> entries, ExtractOptions options, string dataPath) {
			OpeningMode openingMode = OpeningMode.None;
			List<string> files = new List<string>();
			var pathToOpen = "";
			var originalDestinationPath = destinationPath;

			try {
				if (entries.Count == 0) {
					return;
				}

				GrfExceptions.IfEncryptionCheckFlagThrow(this);
				GrfExceptions.IfSavingThrow(_grf);
				Progress = -1;
				IsCancelling = false;
				IsCancelled = false;

				openingMode = OpeningMode.Folder;

				// Detect the destination path - null when the extraction is done at the root of the GRF
				if (destinationPath == null) {
					var basePath = GrfPath.GetDirectoryName(new FileInfo(_grf.FileName).FullName);
					
					// This happens when the files extracted have no common extraction properties
					// No explorer windows should be opened.
					destinationPath = basePath;
					pathToOpen = "";
					openingMode = OpeningMode.None;
				}

				// Load the entries
				var grfPath = GrfPath.GetDirectoryName(entries[0].RelativePath);
				var grfPathSlash = grfPath + "/";

				if (entries.All(p => GrfPath.GetDirectoryName(p.RelativePath) == grfPath)) {
					pathToOpen = destinationPath;

					foreach (var entry in entries) {
						entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(entry.RelativePath.ReplaceFirst(grfPathSlash, ""), options));
					}

					openingMode = OpeningMode.MultipleFiles;
					files = entries.Select(p => p.ExtractionFilePath).ToList();
				}
				else {
					openingMode = OpeningMode.None;

					foreach (var entry in entries) {
						entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(entry.RelativePath, options));
					}
				}

				if ((options & ExtractOptions.UseAppDataPathToExtract) == ExtractOptions.UseAppDataPathToExtract &&
					originalDestinationPath == null) {
					var basePath = GrfPath.GetDirectoryName(new FileInfo(_grf.FileName).FullName) + "\\";
					var newPath = dataPath.TrimEnd('/', '\\') + "\\";

					foreach (var entry in entries) {
						entry.ExtractionFilePath = entry.ExtractionFilePath.ReplaceFirst(basePath, newPath);
					}

					if (pathToOpen != null) {
						pathToOpen = pathToOpen.TrimEnd('/', '\\') + "\\";
						pathToOpen = pathToOpen.ReplaceFirst(basePath, newPath);
					}

					for (int index = 0; index < files.Count; index++) {
						files[index] = files[index].ReplaceFirst(basePath, newPath);
					}
				}

				if ((options & ExtractOptions.ExtractAllInSameFolder) == ExtractOptions.ExtractAllInSameFolder) {
					openingMode = OpeningMode.MultipleFiles;

					if (destinationPath != null) {
						pathToOpen = destinationPath;
						files = entries.Select(p => p.ExtractionFilePath).Distinct().ToList();
					}
				}

				foreach (string folder in entries.Select(p => GrfPath.GetDirectoryName(p.ExtractionFilePath)).Distinct()) {
					if (!Directory.Exists(folder))
						new DirectoryInfo(folder).Create();
				}

				int numOfThreads = entries.Count / 50;
				numOfThreads = numOfThreads <= 0 ? 1 : numOfThreads > Settings.MaximumNumberOfThreads ? Settings.MaximumNumberOfThreads : numOfThreads;

				if ((options & ExtractOptions.SingleThreaded) == ExtractOptions.SingleThreaded) {
					numOfThreads = 1;
				}

				bool overrideCpuPerf = (options & ExtractOptions.OverrideCpuPerf) == ExtractOptions.OverrideCpuPerf;

				entries = entries.OrderBy(p => p.FileExactOffset).ToList();
				var pool = new GrfThreadPool<FileEntry>();
				pool.Initialize<GrfThreadExtract>(_grf, entries, numOfThreads);
				pool.Start(v => Progress = v, () => IsCancelling, !overrideCpuPerf, true);
			}
			catch (OperationCanceledException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
				IsCancelling = false;
				IsCancelled = true;

				if ((options & ExtractOptions.OpenAfterExtraction) == ExtractOptions.OpenAfterExtraction && (entries != null && entries.Count > 0)) {
					SelectFiles(openingMode, pathToOpen, files);
				}
			}
		}

		internal enum OpeningMode {
			None,
			Folder,
			MultipleFiles
		}

		internal void SelectFiles(OpeningMode mode, string destinationPath, List<string> files) {
			if (destinationPath == null)
				return;

			try {
				destinationPath = destinationPath.TrimEnd('/', '\\') + "\\";

				switch (mode) {
					case OpeningMode.Folder:
						if (Directory.Exists(destinationPath))
							OpeningService.FileOrFolder(destinationPath);
						break;
					case OpeningMode.MultipleFiles:
						if (Directory.Exists(destinationPath)) {
							OpeningService.FilesOrFolders(files);
						}
						break;
				}
			}
			catch {
			}
		}

		internal GrfDirectory OpenDirectory(string path) {
			_validateOperation(Condition.Opened);
			return new GrfDirectory(_grf, path);
		}

		/// <summary>
		/// Gets the size of the container file.
		/// </summary>
		/// <returns>The size of the container file.</returns>
		public long GetFileSize() {
			try {
				if (_grf.Reader != null && _grf.Reader.FileName != null) {
					return _grf.Reader.LengthLong;
				}

				return new FileInfo(_grf.FileName).Length;
			}
			catch {
				return -1;
			}
		}

		/// <summary>
		/// Reloads the container.
		/// </summary>
		public void Reload() {
			_validateOperation(Condition.Opened);
			var name = FileName;
			Close();
			Open(name);
		}

		public static void CreateFromBufferedFiles(string fileName, List<FileEntry> entries) {
			Container container = new Container();
			GrfHeader header = new GrfHeader(container);
			container.InternalHeader = header;
			container.Header = header;

			Dictionary<string, FileStream> streams = new Dictionary<string, FileStream>();
			Dictionary<string, List<FileEntry>> subEntries = new Dictionary<string, List<FileEntry>>();

			foreach (var entry in entries) {
				entry.Header = header;

				if (!streams.ContainsKey(entry.SourceFilePath)) {
					streams[entry.SourceFilePath] = new FileStream(entry.SourceFilePath, FileMode.Open);
				}

				if (!subEntries.ContainsKey(entry.SourceFilePath)) {
					subEntries[entry.SourceFilePath] = new List<FileEntry>();
				}

				subEntries[entry.SourceFilePath].Add(entry);
				container.InternalTable.AddEntry(entry);
			}

			using (FileStream output = new FileStream(fileName, FileMode.Create)) {
				output.SetLength(GrfHeader.DataByteSize);
				output.Seek(GrfHeader.DataByteSize, SeekOrigin.Begin);

				long baseOffset;

				foreach (var list in subEntries) {
					var stream = streams[list.Key];

					const int BufferCopyLength = 2097152;
					byte[] buffer = new byte[BufferCopyLength];
					baseOffset = output.Position;

					using (FileStream file = stream) {
						int len;
						while ((len = file.Read(buffer, 0, BufferCopyLength)) > 0) {
							output.Write(buffer, 0, len);
						}
					}
					File.Delete(stream.Name);

					foreach (var entry in list.Value) {
						entry.TemporaryOffset += baseOffset;
					}
				}

				header.FileTableOffset = (uint)output.Position - GrfHeader.DataByteSize;
				header.RealFilesCount = container.Table.Entries.Count;

				int tableSize = container.InternalTable.WriteMetadata(header, output);

				output.Seek(0, SeekOrigin.Begin);
				header.Write(output);
				output.SetLength(header.FileTableOffset + GrfHeader.DataByteSize + tableSize);
			}
		}

		public override string ToString() {
			try {
				if (!IsOpened) {
					return "Closed GRF";
				}

				if (_internalGrf == null)
					return "No loaded GRF";

				return _internalGrf.FileName + " | " + _internalGrf.InternalHeader.FormatView;
			}
			catch {
				return "Invalid GRF";
			}
		}

		public bool ProcessSaveResult(bool handle = true) {
			var result = LastSaveResult;

			if (result.Error != null) {
				if (handle)
					ErrorHandler.HandleException(result.Error);
				else
					throw result.Error;
			}

			if (result.RequiresReload)
				Open(result.NewFileName);

			if (result.IsCancelled) {
				if (handle)
					return false;
				else
					throw new OperationCanceledException();
			}

			return result.Success;
		}
	}

	public enum Condition {
		Closed,
		Opened
	}
}