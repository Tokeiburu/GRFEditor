using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats;
using GRF.FileFormats.ThorFormat;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using Utilities;
using Utilities.Extension;

namespace GRF.Core {
	internal class Container : ContainerAbstract<FileEntry>, IDisposable {
		private TkDictionary<string, object> _attached = new TkDictionary<string, object>();

		internal Container() {
			_header = new GrfHeader(this);
			_table = new FileTable(InternalHeader);
			FileName = "new.grf";
			IsBusy = false;
			IsNewGrf = true;
			CancelReload = false;
			State = ContainerState.Normal;
		}

		internal Container(string fileName) {
			try {
				GrfExceptions.IfNullThrow(fileName, "fileName");

				// Fixed 2014-12-21 : Null-named containers are no longer allowed
				// They created a broken GRF on purpose and would allow further modifications.
				//if (fileName == null) {
				//	_table = new FileTable(InternalHeader);
				//	throw new Exception();
				//}

				_load(fileName);
				CancelReload = false;
			}
			catch (Exception err) {
				if (_header == null) {
					_header = new GrfHeader(this);
					_header.SetError("Null header, this object was forcibly instanced.");
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

		private FileTable _table { get; set; }
		private GrfHeader _header { get; set; }

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
		public bool IsModified {
			get { return Commands.IsModified; }
		}

		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		public string FileName { get; internal set; }

		/// <summary>
		/// Gets or sets a value indicating whether this container is a new GRF (doesn't have a source file yet).
		/// </summary>
		public bool IsNewGrf { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether reloading this file should be reloaded after saving.
		/// </summary>
		internal bool CancelReload { get; set; }

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

			if (_attached.TryGetValue(key, out value)) {
				return (T)value;
			}

			return default(T);
		}

		/// <summary>
		/// Converts the current container to a GRF container.
		/// </summary>
		/// <param name="grfName">Name of the GRF.</param>
		/// <returns>The converted container to a GRF container.</returns>
		internal override Container ToGrfContainer(string grfName = null) {
			throw new InvalidOperationException();
		}

		private void _load(string fileName) {
			try {
				if (fileName == null)
					return;

				IsNewGrf = false;
				IsBusy = false;
				FileName = fileName;
				Reader = new ByteReaderStream(FileName);

				InternalHeader = new GrfHeader(Reader, this);
				Reader.PositionUInt = InternalHeader.FileTableOffset + GrfHeader.StructSize;
				_table = new FileTable(InternalHeader, Reader);

				if (Table.Contains(GrfStrings.EncryptionFilename))
					InternalHeader.IsEncrypted = true;
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
			if (Reader != null)
				Reader.Close();
		}

		public void Save(string fileName, Container mergeGrf, SavingMode mode, SyncMode syncMode) {
			// Determines whether the repack should reload or not.
			bool shouldRepackCancelReload = false;

			try {
				GrfExceptions.IfTrueThrowContainerSaving(IsBusy);
				GrfExceptions.IfEncryptionCheckFlagThrow(this);

				if (mode == SavingMode.GrfSave || mode == SavingMode.QuickMerge) {
					if (mergeGrf != null && mergeGrf.IsModified)
						throw GrfExceptions.__AddedGrfModified.Create();
				}

				if (mode == SavingMode.RepackSource && (fileName ?? FileName.GetExtension()) != ".thor") {
					throw GrfExceptions.__OperationNotAllowed.Create();
				}

				if (mode == SavingMode.Repack) {
					if (IsModified || IsNewGrf)
						throw GrfExceptions.__ContainerNotSavedForRepack.Create();

					// Thor files are always repacked
					// ^ Bug fix : Not true! 2015-04-04
					switch (fileName ?? FileName.GetExtension()) {
						case ".thor":
							mode = SavingMode.Thor;
							Attached["Thor.Repack"] = true;
							break;
						case ".rgz":
							throw GrfExceptions.__InvalidContainerFormat.Create(fileName ?? FileName, ".grf, .gpf or .thor");
					}
				}

				if (mode == SavingMode.Compact) {
					if (IsModified || IsNewGrf)
						throw GrfExceptions.__ContainerNotSavedForCompact.Create();

					// If the filename is null, then we must find the original filename
					switch (fileName ?? FileName.GetExtension()) {
						case ".thor":
							mode = SavingMode.Thor;
							break;
						case ".rgz":
							mode = SavingMode.Rgz;
							break;
					}
				}

				IsBusy = true;
				CancelReload = false;
				shouldRepackCancelReload = true;

				if (syncMode == SyncMode.Synchronous)
					_save(fileName, mergeGrf, mode);
				else
					GrfThread.Start(() => _save(fileName, mergeGrf, mode));
			}
			catch (Exception err) {
				CancelReload = true;

				// The repack should reload if it starts going into the _save method.
				if ((shouldRepackCancelReload) && mode == SavingMode.Repack) {
					CancelReload = false;
				}

				if (syncMode == SyncMode.Synchronous)
					throw;

				ErrorHandler.HandleException(err);
			}
		}

		private void _internalSave(string fileName, Container mergeGrf, SavingMode mode) {
			bool shouldRepack = false;

			try {
				// Validation before saving
				if (fileName.GetExtension() == null && mode != SavingMode.QuickMerge) throw new InvalidOperationException("The file name must end with : .grf | .gpf | .rgz");
				if (_headerCheckFailed()) return;
				if (_headerEncryptedFailed(fileName)) return;

				try {
					switch(mode) {
						case SavingMode.GrfSave:
						case SavingMode.Compact:
						case SavingMode.Repack:
						case SavingMode.RepackSource:
						case SavingMode.QuickMerge:
							// Reset the temp offsets
							foreach (var entry in _table.Entries) {
								if (!entry.IsAdded) {
									// Fix : 2015-04-07
									// This is required...!
									entry.TemporaryOffset = 0;
								}
							}

							break;
					}

					if (mode == SavingMode.GrfSave || mode == SavingMode.QuickMerge) {
						_applyMergeOnTable(mergeGrf);
					}
					else {
						if (mergeGrf != null)
							throw GrfExceptions.__MergeNotSupported.Create();
					}

					if (mode == SavingMode.QuickMerge) {
						Close();
					}

					bool exists = File.Exists(fileName);

					switch(mode) {
						case SavingMode.GrfSave:
						case SavingMode.Compact:
						case SavingMode.Repack:
						case SavingMode.RepackSource:
							try {
								using (FileStream output = new FileStream(fileName, FileMode.Create)) {
									uint fileLength = GrfHeader.StructSize + InternalHeader.FileTableOffset;
									output.SetLength(fileLength);

									switch(mode) {
										case SavingMode.GrfSave:
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

									long fileTableOffset = output.Position - GrfHeader.StructSize;

									if (fileTableOffset > uint.MaxValue) {
										throw GrfExceptions.__GrfSizeLimitReached.Create();
									}

									int tableSize = _table.WriteMetadata(InternalHeader, output);
									InternalHeader.FileTableOffset = (uint)fileTableOffset;
									InternalHeader.RealFilesCount = Table.Entries.Count;

									output.Seek(0, SeekOrigin.Begin);
									InternalHeader.Write(output);
									output.SetLength(InternalHeader.FileTableOffset + GrfHeader.StructSize + tableSize);
								}

								_writeEncryptionIndex();
							}
							catch (OperationCanceledException) {
								if (!exists)
									GrfPath.Delete(fileName);

								throw;
							}
							break;
						case SavingMode.QuickMerge:
							try {
								using (FileStream output = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write)) {
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

									long fileTableOffset = offset - GrfHeader.StructSize;

									if (fileTableOffset > uint.MaxValue) {
										throw GrfExceptions.__GrfSizeLimitReached.Create();
									}

									InternalHeader.FileTableOffset = (uint)fileTableOffset;
									InternalHeader.RealFilesCount = Table.Entries.Count;

									output.Seek(offset, SeekOrigin.Begin);
									int tableSize = _table.WriteMetadata(InternalHeader, output);
									output.Seek(0, SeekOrigin.Begin);
									InternalHeader.Write(output);
									output.SetLength(InternalHeader.FileTableOffset + GrfHeader.StructSize + tableSize);
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
							Rgz.SaveRgz(this, fileName);
							break;
						case SavingMode.Thor:
							Thor.SaveFromGrf(this, fileName);
							break;
					}
				}
				catch (OperationCanceledException) {
					CancelReload = true;

					if (mode == SavingMode.Repack) {
						CancelReload = false;
					}
				}
				catch (GrfException err) {
					if (err == GrfExceptions.__GrfSizeLimitReached && mergeGrf != null) {
						CancelReload = true;

						switch(mode) {
							case SavingMode.GrfSave:
							case SavingMode.Compact:
							case SavingMode.Repack:
							case SavingMode.RepackSource:
								// Temporary file
								try {
									if (fileName != null && File.Exists(fileName))
										File.Delete(fileName);

									if (Reader != null)
										Reader.Close();

									_load(FileName);
								}
								catch {
									if (Reader != null)
										Reader.Open(FileName);

									CancelReload = true;
								}
								break;
						}

						ErrorHandler.HandleException(GrfStrings.CouldNotSaveContainerForceReload, err, ErrorLevel.Warning);
					}
					else {	
						ErrorHandler.HandleException(GrfStrings.CouldNotSaveContainer, err, ErrorLevel.Warning);
						CancelReload = true;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(GrfStrings.CouldNotSaveContainer, err, ErrorLevel.Warning);
					CancelReload = true;
				}
			}
			finally {
				if (mode == SavingMode.QuickMerge) {
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
			try {
				if (!InternalHeader.IsEncrypted)
					return;

				string file = File.GetLastWriteTimeUtc(FileName).ToFileTimeUtc() + "\\files.enc";

				using (GrfHolder grf = new GrfHolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GrfStrings.EncryptionDbFile), GrfLoadOptions.OpenOrNew)) {
					StringBuilder files = new StringBuilder();

					foreach (var entry in Table.Entries.Where(p => p.Encrypted)) {
						files.AppendLine(entry.RelativePath);
					}

					grf.Commands.AddFile(file, Encoding.Default.GetBytes(files.ToString()));
					grf.QuickSave();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
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

		private void _save(string fileName, Container mergeGrf, SavingMode mode) {
			try {
				AProgress.Init(this);

				if (fileName != null || mode == SavingMode.QuickMerge) {
					_internalSave(fileName, mergeGrf, mode);
				}
				else {
					string tmp = TemporaryFilesManager.GetTemporaryFilePath("temp_container_{0:0000}" + FileName.GetExtension());

					// Validate if the GRF can be written
					string streamFileName = Reader == null ? null : Reader.Stream == null ? null : Reader.Stream.Name;

					if (streamFileName != null) {
						try {
							if (Reader != null)
								Reader.Close();

							if (Methods.IsFileLocked(streamFileName)) {
								throw GrfExceptions.__FileLocked.Create(streamFileName);
							}
						}
						catch {
							CancelReload = true;
							throw;
						}
						finally {
							if (Reader != null)
								Reader.Open(streamFileName);
						}
					}

					_internalSave(tmp, mergeGrf, mode);

					// Prevents erasing the original file
					if (!CancelReload) {
						try {
							if (!File.Exists(tmp))
								return;

							if (Reader != null)
								Reader.Close();

							File.Delete(FileName);
							File.Move(tmp, FileName);

							_load(FileName);
						}
						catch {
							if (Reader != null)
								Reader.Open(FileName);

							CancelReload = true;

							throw;
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(GrfStrings.CouldNotSaveContainer, err);
			}
			finally {
				IsBusy = false;
				Attached["Thor.Repack"] = null;
				AProgress.Finalize(this);
			}
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

		private bool _headerEncryptedFailed(string fileName) {
			if (InternalHeader.IsEncrypted) {
				if ((fileName != null && fileName.GetExtension() == ".rgz") || Header.IsMajorVersion(1)) {
					ErrorHandler.HandleException(GrfStrings.FailedHeaderEncrypted);
					CancelReload = true;
					return true;
				}
			}

			return false;
		}

		private bool _headerCheckFailed() {
			if (InternalHeader.FoundErrors) {
				if (ErrorHandler.YesNoRequest(GrfStrings.GrfContainsErrors, GrfStrings.GrfDataIntegrity) == false) {
					CancelReload = true;
					return true;
				}
			}

			return false;
		}

		public void Close() {
			if (Reader != null) {
				Reader.Close();
			}
		}

		/// <summary>
		/// Gets the stream raw data. Use carefully.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <returns></returns>
		public byte[] GetStreamRawData(FileEntry entry) {
			byte[] data;

			lock (Reader.SharedLock) {
				Reader.PositionUInt = entry.FileExactOffset;
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
				thread.Init(action);
			}
			threadPool.Start(progress, isCancelling);
		}
	}
}