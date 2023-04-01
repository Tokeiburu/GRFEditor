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
using GRF.System;
using GRF.Threading;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

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
		public virtual bool IsClosed {
			get { return _grfClosed; }
		}

		/// <summary>
		/// Gets a value indicating whether this container is opened.
		/// </summary>
		public virtual bool IsOpened {
			get { return !_grfClosed; }
		}

		/// <summary>
		/// Gets the header.
		/// </summary>
		public virtual GrfHeader Header {
			get { return _grf.InternalHeader; }
		}

		/// <summary>
		/// Gets the file table.
		/// </summary>
		public virtual FileTable FileTable {
			get { return _grf.InternalTable; }
		}

		/// <summary>
		/// Gets a value indicating whether this container has been modified.
		/// </summary>
		public bool IsModified {
			get { return _grf.IsModified; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the reload should be cancelled.
		/// </summary>
		public virtual bool CancelReload {
			get { return _grf.CancelReload; }
			set { _grf.CancelReload = value; }
		}

		/// <summary>
		/// Gets the name of the opened file.
		/// </summary>
		public virtual string FileName {
			get { return _grf.FileName; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a new file.
		/// </summary>
		public virtual bool IsNewGrf {
			get { return _grf.IsNewGrf; }
			set { _grf.IsNewGrf = value; }
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

		internal Container Container {
			get { return _grf; }
		}

		/// <summary>
		/// Main component to execute commands on the GRF.
		/// </summary>
		public CommandsHolder<FileEntry> Commands {
			get { return _grf.Commands; }
		}

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
		/// Detects the encrypted files and sets the flag.
		/// </summary>
		public void SetEncryptionFlag(bool forceSet = false) {
			Header.EncryptionCheckFlag = true;

			if (_grf.InternalHeader.IsEncrypted || forceSet) {
				try {
					string file = File.GetLastWriteTimeUtc(_grf.FileName).ToFileTimeUtc() + "\\files.enc";

					using (GrfHolder grf = new GrfHolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GrfStrings.EncryptionDbFile), GrfLoadOptions.OpenOrNew)) {
						if (grf.FileTable.ContainsFile(file)) {
							var encryptedFiles = Encoding.Default.GetString(grf.FileTable[file].GetDecompressedData());

							foreach (var line in encryptedFiles.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
								if (!string.IsNullOrEmpty(line)) {
									var entry = _grf.Table.TryGet(line);
									if (entry != null) {
										entry.Flags |= EntryType.GrfEditorCrypted;
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
					pool.Initialize<ThreadSetEncryption>(_grf, FileTable.Entries, Settings.MaximumNumberOfThreads);
					pool.Start(v => _grf.Progress = v, () => _grf.IsCancelling);

					using (GrfHolder grf = new GrfHolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GrfStrings.EncryptionDbFile), GrfLoadOptions.OpenOrNew)) {
						StringBuilder files = new StringBuilder();
					
						foreach (var entry in FileTable.Entries.Where(p => p.Encrypted)) {
							files.AppendLine(entry.RelativePath);
						}
					
						grf.Commands.AddFile(file, Encoding.Default.GetBytes(files.ToString()));
						grf.QuickSave();
					}
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

		/// <summary>
		/// Detects the encrypted files and sets the flag.
		/// </summary>
		public void SetLzmaFlag() {
			GrfThread.Start(delegate {
				try {
					AProgress.Init(_grf);
					GrfThreadPool<FileEntry> pool = new GrfThreadPool<FileEntry>();
					pool.Initialize<ThreadSetLzma>(_grf, FileTable.Entries, 3);
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
		public void Repack() {
			Repack(null, SyncMode.Synchronous);
		}

		/// <summary>
		/// Repacks the GRF synchronously.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		public void Repack(string fileName) {
			Repack(fileName, SyncMode.Synchronous);
		}

		/// <summary>
		/// Repacks the GRF.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public void Repack(string fileName, SyncMode syncMode) {
			_validateOperation(Condition.Opened);
			_internalGrf.Save(fileName, null, SavingMode.Repack, syncMode);
		}

		/// <summary>
		/// Saves the GRF synchronously.
		/// </summary>
		public void Save() {
			Save(SyncMode.Synchronous);
		}

		/// <summary>
		/// Saves the GRF.
		/// </summary>
		public void Save(SyncMode syncMode) {
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
					mode = SavingMode.GrfSave;
					break;
			}

			_internalGrf.Save(null, null, mode, syncMode);
		}

		/// <summary>
		/// Saves the GRF synchronously.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		public void Save(string fileName) {
			Save(fileName, SyncMode.Synchronous);
		}

		/// <summary>
		/// Saves the GRF.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public void Save(string fileName, SyncMode syncMode) {
			_validateOperation(Condition.Opened);

			string extension = fileName.GetExtension();

			SavingMode mode;

			switch (extension) {
				case ".rgz":
					mode = SavingMode.Rgz;
					break;
				case ".thor":
					mode = SavingMode.Thor;
					break;
				default:
					mode = SavingMode.GrfSave;
					break;
			}

			_internalGrf.Save(fileName, null, mode, syncMode);
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
			Open(fileName, 0);
		}

		public void Open(string fileName, GrfLoadOptions options) {
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
						_grf = GrfContainerProvider.Get(fileName);
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
					_grf = GrfContainerProvider.Get(fileName);
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
		/// Merges the GRF synchronously.
		/// </summary>
		/// <param name="grfAdd">The GRF add.</param>
		public void Merge(GrfHolder grfAdd) {
			Merge(grfAdd, SyncMode.Synchronous);
		}

		/// <summary>
		/// Merges the GRF.
		/// </summary>
		/// <param name="grfAdd">The GRF add.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public void Merge(GrfHolder grfAdd, SyncMode syncMode) {
			_grf.Save(null, grfAdd.Container, SavingMode.GrfSave, syncMode);
		}

		/// <summary>
		/// Saves the GRF synchronously (does not repack).<para></para>
		/// This method uses the QuickMerge feature if it's available.
		/// </summary>
		public bool QuickSave() {
			switch (FileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					_grf.Save(null, null, SavingMode.QuickMerge, SyncMode.Synchronous);
					return true;
				default:
					Save(SyncMode.Synchronous);
					return false;
			}
		}

		/// <summary>
		/// Saves the GRF synchronously (does not repack).
		/// This method uses the QuickMerge feature if it's available.
		/// </summary>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public bool QuickSave(SyncMode syncMode) {
			switch (FileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					_grf.Save(null, null, SavingMode.QuickMerge, syncMode);
					return true;
				default:
					Save(syncMode);
					return false;
			}
		}

		/// <summary>
		/// Merges the GRF synchronously (does not repack).
		/// </summary>
		/// <param name="grfAdd">The GRF add.</param>
		public bool QuickMerge(GrfHolder grfAdd) {
			switch (FileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					_grf.Save(null, grfAdd == null ? null : grfAdd.Container, SavingMode.QuickMerge, SyncMode.Synchronous);
					return true;
				default:
					Save(SyncMode.Synchronous);
					return false;
			}
		}

		//public void AnySave( string path)

		/// <summary>
		/// Redirects identical entries in the GRF table to save space.
		/// </summary>
		public void Compact() {
			_validateOperation(Condition.Opened);
			_grf.Save(null, null, SavingMode.Compact, SyncMode.Synchronous);
		}

		/// <summary>
		/// Gets the size of all files in the GRF.
		/// </summary>
		/// <returns></returns>
		public List<Tuple<string, string>> GetSizes() {
			return _grf.GetFileCompressedSizes();
		}

		/// <summary>
		/// Merges the GRF (does not repack).
		/// </summary>
		/// <param name="grfAdd">The GRF add.</param>
		/// <param name="syncMode">Synchronization mode (executed on a different thread if asynchronous).</param>
		public bool QuickMerge(GrfHolder grfAdd, SyncMode syncMode) {
			switch (FileName.GetExtension()) {
				case ".grf":
				case ".gpf":
					_grf.Save(null, grfAdd == null ? null : grfAdd.Container, SavingMode.QuickMerge, syncMode);
					return true;
				default:
					Save(syncMode);
					return false;
			}
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
					entries = _grf.Table.EntriesInDirectory(grfPath, searchOption);

					if (entries.Count == 0)
						return;

					foreach (var entry in entries) {
						entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(nodeName), _noRoot(entry.RelativePath.ReplaceFirst(grfPathSlash, "")));
					}

					if (searchOption == SearchOption.TopDirectoryOnly) {
						openingMode = OpeningMode.MultipleFiles;
						files = entries.Select(p => p.ExtractionFilePath).ToList();
					}
				}
				else {
					if (entries.All(p => GrfPath.GetDirectoryName(p.RelativePath) == grfPath)) {
						pathToOpen = destinationPath;

						foreach (var entry in entries) {
							entry.ExtractionFilePath = GrfPath.Combine(destinationPath, _noRoot(entry.RelativePath.ReplaceFirst(grfPathSlash, "")));
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
				output.SetLength(GrfHeader.StructSize);
				output.Seek(GrfHeader.StructSize, SeekOrigin.Begin);

				uint baseOffset;

				foreach (var list in subEntries) {
					var stream = streams[list.Key];

					const int BufferCopyLength = 2097152;
					byte[] buffer = new byte[BufferCopyLength];
					baseOffset = (uint) output.Position;

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

				header.FileTableOffset = (uint)output.Position - GrfHeader.StructSize;
				header.RealFilesCount = container.Table.Entries.Count;

				int tableSize = container.InternalTable.WriteMetadata(header, output);

				output.Seek(0, SeekOrigin.Begin);
				header.Write(output);
				output.SetLength(header.FileTableOffset + GrfHeader.StructSize + tableSize);
			}
		}
	}

	public enum Condition {
		Closed,
		Opened
	}
}