using System;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats;
using GRF.FileFormats.ThorFormat;
using GRF.IO;
using GRF.GrfSystem;
using GRF.Threading;
using Utilities;
using Utilities.Extension;
using GRF.Core.GrfWriters;

namespace GRF.Core {
	internal class Container : ContainerAbstract<FileEntry>, IDisposable {
		private TkDictionary<string, object> _attached = new TkDictionary<string, object>();

		internal Container() {
			_header = new GrfHeader(this);
			_table = new FileTable(InternalHeader);
			FileName = "new.grf";
			IsBusy = false;
			IsNewGrf = true;
			State = ContainerState.Normal;
		}

		internal Container(string fileName, GrfLoadData loadData = null) {
			try {
				GrfExceptions.IfNullThrow(fileName, "fileName");
				_load(fileName, loadData);
			}
			catch (Exception err) {
				if (_header == null) {
					_header = new GrfHeader(this);
					_header.SetError("Null header, this object was forcibly instantiated.");
				}

				_header.SetError(err.Message);
			}
			finally {
				AProgress.Finalize(this);
			}
		}

		internal ByteReaderStream Reader {
			get { return _reader ?? (_reader = new ByteReaderStream()); }
			set { _reader = value; }
		}

		private FileTable _table;
		private GrfHeader _header;

		/// <summary>
		/// Gets or sets the table.
		/// </summary>
		public override ContainerTable<FileEntry> Table {
			get { return _table; }
			protected set { _table = (FileTable) value; }
		}

		/// <summary>
		/// Gets or sets the table.
		/// </summary>
		public FileTable InternalTable {
			get { return _table; }
			set { _table = value; }
		}

		/// <summary>
		/// Gets the container header.
		/// </summary>
		public override FileHeader Header {
			get { return _header; }
			internal set { _header = (GrfHeader) value; }
		}

		/// <summary>
		/// Gets the container header.
		/// </summary>
		public GrfHeader InternalHeader {
			get { return _header; }
			internal set { _header = value; }
		}

		/// <summary>
		/// Gets a value indicating whether this container is modified (from the Commands object).
		/// </summary>
		public bool IsModified => Commands.IsModified;

		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		public string FileName { get; internal set; }

		/// <summary>
		/// Gets or sets a value indicating whether this container is a new GRF (doesn't have a source file yet).
		/// </summary>
		public bool IsNewGrf { get; set; }

		/// <summary>
		/// Gets or sets attached properties.
		/// </summary>
		internal TkDictionary<string, object> Attached {
			get { return _attached; }
			set { _attached = value; }
		}

		internal override string UniqueString {
			get {
				if (IsNewGrf)
					return "" + (FileName ?? "null").GetHashCode();

				return base.UniqueString;
			}
		}

		/// <summary>
		/// Gets an attached value and converts it to the requested format.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">The key.</param>
		/// <returns>The attached properties</returns>
		internal T GetAttachedProperty<T>(string key) {
			object value;

			if (!_attached.TryGetValue(key, out value)) {
				value = default(T);
				_attached[key] = value;
			}

			return (T)value;
		}

		/// <summary>
		/// Converts the current container to a GRF container.
		/// </summary>
		/// <param name="grfName">Name of the GRF.</param>
		/// <returns>The converted container to a GRF container.</returns>
		internal override Container ToGrfContainer(string grfName = null) {
			throw new InvalidOperationException();
		}

		private void _load(string fileName, GrfLoadData loadData = null) {
			try {
				if (fileName == null)
					return;

				IsNewGrf = false;
				IsBusy = false;
				FileName = fileName;
				Reader = new ByteReaderStream(FileName);
				InternalHeader = new GrfHeader(Reader, this);
				Reader.PositionLong = InternalHeader.FileTableOffset + GrfHeader.DataByteSize;

				if (loadData != null && loadData.DecryptFileTable && loadData.EncryptionKey != null) {
					InternalHeader.DecryptFileTable = true;
					InternalHeader.EncryptionKey = loadData.EncryptionKey;
				}

				_table = new FileTable(InternalHeader, Reader);

				var encryptedCheckEntry = Table.TryGet(GrfStrings.EncryptionFilename);

				if (encryptedCheckEntry != null) {
					InternalHeader.IsEncrypted = true;
					Debug.Ignore(() => InternalHeader.EncryptionHashValue = BitConverter.ToUInt32(encryptedCheckEntry.GetDecompressedData(), 0));
				}
			}
			catch (Exception err) {
				if (Header != null) {
					_header.SetError(GrfStrings.FailedReadContainer, err.Message);
				}
				else
					ErrorHandler.HandleException(GrfExceptions.__CouldNotLoadGrf, err, ErrorLevel.Warning);
			}
			finally {
				Progress = 100f;
			}
		}

		public byte[] GetRawData(FileEntry node) {
			return node.GetCompressedData();
		}

		public byte[] GetDecompressedData(FileEntry node) {
			return node.GetDecompressedData();
		}

		protected override void _init() {
			throw new InvalidOperationException();
		}

		protected override void _onPreviewDispose() {
			Reader?.Close();
		}

		public ContainerSaveResult SaveResult;

		/// <summary>
		/// Saves the container to the hard drive.
		/// </summary>
		/// <param name="fileName">Name of the file (null if overwriting the current container).</param>
		/// <param name="mergeGrf">The GRF to merge (null if nothing to merge).</param>
		/// <param name="mode">The saving mode.</param>
		/// <param name="syncMode">The synchronize mode (default to Synchronous.</param>
		public ContainerSaveResult Save(string fileName, Container mergeGrf, SavingMode mode, SyncMode syncMode) {
			ContainerSaveResult result = new ContainerSaveResult(this, fileName, mergeGrf, mode, syncMode);

			try {
				GrfExceptions.IfTrueThrowContainerSaving(IsBusy);
				GrfExceptions.IfEncryptionCheckFlagThrow(this);
				string ext = (fileName ?? FileName).GetExtension();

				switch (mode) {
					case SavingMode.FileCopy:
					case SavingMode.FileEdit:
						if (mergeGrf != null && mergeGrf.IsModified)
							throw GrfExceptions.__AddedGrfModified.Create();
						break;
					case SavingMode.RepackSource:
						if (ext != ".thor")
							throw GrfExceptions.__OperationNotAllowed.Create();
						break;
					case SavingMode.Repack:
						if (IsModified || IsNewGrf)
							throw GrfExceptions.__ContainerNotSavedForRepack.Create();

						switch (ext) {
							case ".thor":
								mode = SavingMode.Thor;
								InternalHeader.ThorSettings.Repack = true;
								break;
							case ".rgz":
								throw GrfExceptions.__InvalidContainerFormat.Create(fileName ?? FileName, ".grf, .gpf or .thor");
						}
						break;
					case SavingMode.Compact:
						if (IsModified || IsNewGrf)
							throw GrfExceptions.__ContainerNotSavedForCompact.Create();

						switch (ext) {
							case ".thor":
								mode = SavingMode.Thor;
								break;
							case ".rgz":
								mode = SavingMode.Rgz;
								break;
						}
						break;
				}

				IsBusy = true;

				if (syncMode == SyncMode.Synchronous)
					_save(fileName, mergeGrf, mode, result);
				else
					GrfThread.Start(delegate {
						_save(fileName, mergeGrf, mode, result);
						result.Completed = true;
						SaveResult = result;
					});
			}
			catch (Exception err) {
				result.Fail(err);
			}

			result.Completed = true;
			SaveResult = result;
			return result;
		}

		private void _internalSave(string fileName, Container mergeGrf, SavingMode mode, ContainerSaveResult result) {
			bool shouldRepack = false;

			try {
				// Validation before saving
				if (fileName.GetExtension() == null && mode != SavingMode.FileEdit)
					throw new InvalidOperationException("The file name must end with : .grf | .gpf | .rgz");
				if (_headerCheckFailed()) {
					result.Cancelled();
					return;
				}

				_headerEncryptedFailed(fileName);

				try {
					bool exists = File.Exists(fileName);
					_table.ResetTemporaryOffsets();

					// Merge GRF validation
					switch (mode) {
						case SavingMode.FileCopy:
							_applyMergeOnTable(mergeGrf);
							break;
						case SavingMode.FileEdit:
							_applyMergeOnTable(mergeGrf);
							Close();
							break;
						default:
							if (mergeGrf != null)
								throw GrfExceptions.__MergeNotSupported.Create();
							break;
					}

					switch(mode) {
						case SavingMode.FileCopy:
						case SavingMode.Compact:
						case SavingMode.Repack:
						case SavingMode.RepackSource:
							try {
								using (FileStream output = new FileStream(fileName, FileMode.Create)) {
									long fileLength = GrfHeader.DataByteSize + InternalHeader.FileTableOffset;

									if (Header.Version < 3.0 && fileLength > uint.MaxValue)
										throw GrfExceptions.__GrfSizeLimitReached.Create();

									output.SetLength(fileLength);

									switch(mode) {
										case SavingMode.FileCopy:
											_table.WriteData(this, Reader.Stream, output, mergeGrf);
											break;
										case SavingMode.Repack:
										case SavingMode.RepackSource:
											_table.WriteDataRepack(this, Reader.Stream, output);
											break;
										case SavingMode.Compact:
											_table.WriteDataCompact(this, Reader.Stream, output);
											break;
									}

									long fileTableOffset = output.Position - GrfHeader.DataByteSize;
									
									if (Header.Version < 3.0 && fileTableOffset > uint.MaxValue)
										throw GrfExceptions.__GrfSizeLimitReached.Create();

									int tableSize = _table.WriteMetadata(InternalHeader, output);
									InternalHeader.FileTableOffset = fileTableOffset;
									InternalHeader.RealFilesCount = Table.Entries.Count;

									output.Seek(0, SeekOrigin.Begin);
									InternalHeader.Write(output);
									output.SetLength(InternalHeader.FileTableOffset + GrfHeader.DataByteSize + tableSize);
								}

								_writeEncryptionIndex();
							}
							catch (OperationCanceledException) {
								if (!exists)
									GrfPath.Delete(fileName);

								throw;
							}
							break;
						case SavingMode.FileEdit:
							try {
								using (FileStream output = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
									long offset;

									try {
										offset = _table.WriteDataQuick(this, output, mergeGrf);
									}
									catch (GrfException err) {
										if (err == GrfExceptions.__RepackInstead) {
											Close();
											shouldRepack = true;
											return;
										}
										else {
											throw;
										}
									}

									long fileTableOffset = offset - GrfHeader.DataByteSize;

									if (Header.Version < 3.0 && fileTableOffset > uint.MaxValue)
										throw GrfExceptions.__GrfSizeLimitReached.Create();

									InternalHeader.FileTableOffset = fileTableOffset;
									InternalHeader.RealFilesCount = Table.Entries.Count;

									output.Seek(offset, SeekOrigin.Begin);
									int tableSize = _table.WriteMetadata(InternalHeader, output);
									output.Seek(0, SeekOrigin.Begin);
									InternalHeader.Write(output);
									output.SetLength(InternalHeader.FileTableOffset + GrfHeader.DataByteSize + tableSize);
								}

								_writeEncryptionIndex();
							}
							catch (OperationCanceledException) {
								if (!exists)
									GrfPath.Delete(fileName);

								throw;
							}
							break;
						case SavingMode.Rgz:
							Rgz.SaveRgz(this, fileName, result);
							break;
						case SavingMode.Thor:
							Thor.SaveFromGrf(this, fileName, result);
							break;
					}
				}
				catch (OperationCanceledException) {
					result.Cancelled();
					return;
				}
				catch (GrfException err) when (err == GrfExceptions.__GrfSizeLimitReached && mergeGrf != null) {
					switch(mode) {
						case SavingMode.FileCopy:
						case SavingMode.Compact:
						case SavingMode.Repack:
						case SavingMode.RepackSource:
							// Temporary file
							try {
								if (fileName != null && File.Exists(fileName))
									File.Delete(fileName);

								Reader?.Close();
								_load(FileName);
							}
							catch {
								Reader?.Open(FileName);
							}
							break;
					}

					throw;
				}
			}
			finally {
				if (mode == SavingMode.FileEdit) {
					try {
						ResetStream();
					}
					catch {
						if (!shouldRepack) throw;
					}
				}
			}
		}

		private void _writeEncryptionIndex() {
			if (!InternalHeader.IsEncrypted)
				return;

			GrfHolder.WriteEncryptionIndex(this, File.GetLastWriteTimeUtc(FileName).ToFileTimeUtc() + "\\files.enc");
		}

		/// <summary>
		/// Restores the entry streams.
		/// </summary>
		internal void ResetStream() {
			if (Table == null) return;
			Close();
			Reader.SetStream(GetSharedStream());

			if (Table.Entries.Count > 0) {
				var entries = Table.Entries;

				foreach (FileEntry entry in entries) {
					entry.RefreshStream(Reader);
				}
			}
		}

		private void _save(string fileName, Container mergeGrf, SavingMode mode, ContainerSaveResult result) {
			AProgress.Init(this);
			bool fileCopy = fileName == null && mode != SavingMode.FileEdit;
			string targetFilePath = fileName;
			string sourceFilePath = FileName;

			try {
				if (fileCopy) {
					// Make a file copy first before overwriting the current file
					string tmp = GrfPath.Combine(Path.GetDirectoryName(sourceFilePath), "~" + Path.GetFileName(sourceFilePath));

					// Validate if the source GRF can be deleted, since it needs to be moved afterwards
					string streamFileName = Reader?.Stream?.Name;

					if (streamFileName != null)
						_testIfSourceFileCanBeDeleted(streamFileName);

					targetFilePath = tmp;
				}

				_internalSave(targetFilePath, mergeGrf, mode, result);

				if (!result.Success)
					return;

				if (fileCopy) {
					if (!File.Exists(targetFilePath))
						return;

					Progress = TieredProgress.SpecialCopyingFile;
					Reader?.Close();
					File.Delete(sourceFilePath);
					File.Move(targetFilePath, sourceFilePath);
					targetFilePath = sourceFilePath;
				}

				switch (mode) {
					case SavingMode.Rgz:
					case SavingMode.Thor:
						result.RequiresReload = true;
						break;
					default:
						_quickLoad(targetFilePath, mode, ignoreFileType: true);
						break;
				}
			}
			catch (GrfException err) {
				if (fileCopy)
					_deleteFileCopy(sourceFilePath, targetFilePath);
				result.Fail(err);
			}
			catch (Exception err) {
				if (fileCopy)
					_deleteFileCopy(sourceFilePath, targetFilePath);
				result.Fail(new Exception(GrfStrings.CouldNotSaveContainer, err));
			}
			finally {
				IsBusy = false;
				InternalHeader.ThorSettings.Repack = false;
				AProgress.Finalize(this);
			}
		}

		private void _deleteFileCopy(string sourceFilePath, string targetFilePath) {
			try {
				if (File.Exists(sourceFilePath) && File.Exists(targetFilePath))
					GrfPath.Delete(targetFilePath);
			}
			catch { }
		}

		private void _testIfSourceFileCanBeDeleted(string streamFileName) {
			try {
				Reader?.Close();

				if (Methods.IsFileLocked(streamFileName))
					throw GrfExceptions.__FileLocked.Create(streamFileName);
			}
			finally {
				Reader?.Open(streamFileName);
			}
		}

		private void _quickLoad(string fileName, SavingMode mode, bool ignoreFileType = false) {
			// Thor and Rgz use temporary archives, not reloading them is problematic
			if (!ignoreFileType && fileName.IsExtension(".thor", ".rgz")) {
				return;
			}

			if (fileName != null) {
				FileName = fileName;
				Reader?.Close();
				Reader = new ByteReaderStream(fileName);
			}

			var entries = Table.Entries;

			for (int i = 0; i < entries.Count; i++) {
				var entry = entries[i];
				bool propertyChanged = false;

				if (entry.Modification.HasFlag(Modification.Removed)) {
					Table.DeleteEntry(entry.RelativePath);
					continue;
				}

				if (entry.Modification.HasFlag(Modification.Encrypt)) {
					entry.Flags |= EntryType.GrfEditorCrypted;
				}
				else if (entry.Modification.HasFlag(Modification.Decrypt)) {
					entry.Flags &= ~EntryType.GrfEditorCrypted;
				}
				else if (entry.Modification.HasFlag(Modification.Added)) {
					propertyChanged = true;
				}

				entry.Modification &= ~(Modification.GrfMerge | Modification.Encrypt | Modification.Decrypt | Modification.Added);
				entry.ExtractionFilePath = null;
				entry.SourceFilePath = null;
				entry.RawDataSource = null;
				entry.RemovedFlagCount = 0;

				entry.SizeCompressed = entry.NewSizeCompressed;
				entry.SizeCompressedAlignment = entry.TemporarySizeCompressedAlignment;
				entry.SizeDecompressed = entry.NewSizeDecompressed;
				entry.FileExactOffset = entry.TemporaryOffset;

				entry.Stream = Reader;
				entry.Header = InternalHeader;

				if (propertyChanged)
					entry.OnPropertyChanged("IsAdded");
			}

			Table.InvalidateInternalSets();
			Commands.ClearCommands();
		}

		/// <summary>
		/// Gets the shared stream.
		/// </summary>
		/// <returns></returns>
		public override DisposableScope<FileStream> GetSharedStream() {
			return new DisposableScope<FileStream>(File.Exists(FileName) ? new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read) : null);
		}

		/// <summary>
		/// Gets the source stream.
		/// </summary>
		/// <returns></returns>
		public override DisposableScope<FileStream> GetSourceStream() {
			try {
				return new DisposableScope<FileStream>(File.Exists(Reader.Stream.Name) ? new FileStream(Reader.Stream.Name, FileMode.Open, FileAccess.Read, FileShare.Read) : null);
			}
			catch {
				return GetSharedStream();
			}
		}

		private void _applyMergeOnTable(Container grfAdd) {
			if (grfAdd == null)
				return;

			foreach (FileEntry entry in grfAdd.Table.Entries) {
				if (Table.ContainsKey(entry.RelativePath)) {
					// Ensures all streams are closed... again?
					Table.DeleteFile(entry.RelativePath);
				}
			}

			foreach (FileEntry entry in grfAdd.Table.Entries) {
				Table.AddEntry(new FileEntry(entry) {Modification = Modification.GrfMerge});
			}

			foreach (FileEntry entry in grfAdd.Table.Entries.Where(p => p.Flags.HasFlags(EntryType.RemoveFile))) {
				Table.DeleteFile(entry.RelativePath);
			}

			Table.InvalidateInternalSets();
		}

		private void _headerEncryptedFailed(string fileName) {
			if ((InternalHeader.IsEncrypted || InternalHeader.EncryptFileTable) && (fileName.IsExtension(".rgz") || Header.IsMajorVersion(1)))
				throw GrfExceptions.__UnsupportedEncryptionVersion.Create();
		}

		private bool _headerCheckFailed() {
			if (InternalHeader.FoundErrors) {
				if (ErrorHandler.YesNoRequest(GrfStrings.GrfContainsErrors, GrfStrings.GrfDataIntegrity) == false) {
					return true;
				}
			}

			return false;
		}

		public void Close() {
			Reader?.Close();
		}

		/// <summary>
		/// Gets the stream raw data. Use carefully.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <returns></returns>
		public byte[] GetStreamRawData(FileEntry entry) {
			byte[] data;

			lock (Reader.SharedLock) {
				Reader.PositionLong = entry.FileExactOffset;
				data = Reader.Bytes(entry.SizeCompressedAlignment);
			}

			return data;
		}

		/// <summary>
		/// Executes an operation on the GRF entries on multiple threads.
		/// </summary>
		/// <param name="progress">The progress method.</param>
		/// <param name="isCancelling">The cancelling method.</param>
		/// <param name="action">The action.</param>
		public void ThreadOperation(Action<float> progress, Func<bool> isCancelling, Action<FileEntry, byte[]> action) {
			ThreadOperation(progress, isCancelling, action, -1);
		}

		/// <summary>
		/// Executes an operation on the GRF entries on multiple threads.
		/// </summary>
		/// <param name="progress">The progress method.</param>
		/// <param name="isCancelling">The cancelling method.</param>
		/// <param name="action">The action.</param>
		/// <param name="numOfThreads">The number of threads.</param>
		public void ThreadOperation(Action<float> progress, Func<bool> isCancelling, Action<FileEntry, byte[]> action, int numOfThreads) {
			GrfThreadPool<FileEntry> threadPool = new GrfThreadPool<FileEntry>();
			threadPool.Initialize<ThreadGenericGrf>(this, Table.Entries, numOfThreads);
			foreach (var thread in threadPool.Threads.OfType<ThreadGenericGrf>()) {
				thread.Init(action, isCancelling);
			}
			threadPool.Start(progress, isCancelling);
		}
	}
}