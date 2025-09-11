using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat.Commands;
using GRF.Core;
using GRF.FileFormats;
using GRF.IO;
using GRF.GrfSystem;
using GRF.Threading;
using Utilities;
using Utilities.Extension;

namespace GRF.ContainerFormat {
	/// <summary>
	/// Base class of all container type objects (GRF, GPF, RGZ, THOR). This class handles
	/// the common operations used on the archives.
	/// </summary>
	/// <typeparam name="TEntry">The type of the entry.</typeparam>
	public abstract class ContainerAbstract<TEntry> : IDisposable, IProgress where TEntry : ContainerEntry {
		private bool _disposed;
		private bool _limitProgress;
		private float _progress;
		protected ByteReaderStream _reader;
		protected ContainerValidation _validation = new ContainerValidation();
		private ContainerResources _resources = new ContainerResources();
		private CommandsHolder<TEntry> _commands;

		protected ContainerAbstract() {
			Commands = new CommandsHolder<TEntry>(this);
		}

		protected ContainerAbstract(ByteReaderStream reader) {
			Table = new ContainerTable<TEntry>();
			Commands = new CommandsHolder<TEntry>(this);
			_reader = reader;

			try {
				_init();
				State = ContainerState.Normal;
			}
			catch (OperationCanceledException) {
				_validation.Add("Operation has been cancelled while loading the container.");
				State = ContainerState.LoadCancelled;
			}
			catch (Exception err) {
				_validation.Add(err);
				State = ContainerState.Error;
			}
		}

		/// <summary>
		/// Gets or sets the state of the container.
		/// </summary>
		internal ContainerState State { get; set; }

		/// <summary>
		/// Gets the validation.
		/// </summary>
		internal ContainerValidation Validation {
			get { return _validation; }
		}

		/// <summary>
		/// Gets the stream resources used with this container.
		/// </summary>
		internal ContainerResources Resources {
			get { return _resources; }
		}

		/// <summary>
		/// Gets or sets the table.
		/// </summary>
		public virtual ContainerTable<TEntry> Table { get; protected set; }

		/// <summary>
		/// Gets the container commands.
		/// </summary>
		public CommandsHolder<TEntry> Commands {
			get {
				if (_disposed)
					throw new ObjectDisposedException("Container");

				return _commands;
			}
			protected set { _commands = value; }
		}

		/// <summary>
		/// Gets the container header.
		/// </summary>
		public virtual FileHeader Header { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether the container is busy.
		/// </summary>
		public bool IsBusy { get; internal set; }

		/// <summary>
		/// Gets the TEntry with the specified name.
		/// </summary>
		/// <returns>The entry in the table object.</returns>
		public TEntry this[string name] {
			get { return Table[name]; }
		}

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			Dispose(_disposed);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region IProgress Members

		/// <summary>
		/// Gets or sets the progress.
		/// </summary>
		public float Progress {
			get { return _progress; }
			set { _progress = _limitProgress ? AProgress.LimitProgress(value) : value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cancelling.
		/// </summary>
		public bool IsCancelling { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cancelled.
		/// </summary>
		public bool IsCancelled { get; set; }

		/// <summary>
		/// Gets a unique string for this container. This is used for temporary files.
		/// </summary>
		internal virtual string UniqueString {
			get {
				try {
					if (_reader == null || _reader.Stream == null)
						return "";

					return "" + (uint) _reader.Stream.Name.GetHashCode();
				}
				catch {
					return "";
				}
			}
		}

		#endregion

		internal void LimitProgress(bool state) {
			_limitProgress = state;
		}

		~ContainerAbstract() {
			Dispose(_disposed);
		}

		protected abstract void _init();

		protected void Dispose(bool disposing) {
			if (!_disposed) {
				_onPreviewDispose();

				if (_reader != null) {
					_reader.Close();
					_reader = null;
				}

				if (Table != null) {
					Table.Delete();
					Table = null;
				}

				if (Commands != null) {
					Commands.Dispose();
					Commands = null;
				}

				if (_resources != null) {
					_resources.Clear();
					_resources = null;
				}

				_onDispose();

				_disposed = true;
			}
		}

		protected virtual void _onPreviewDispose() {
		}

		protected virtual void _onDispose() {
		}

		/// <summary>
		/// Converts a container and export it to a GRF format.
		/// </summary>
		/// <param name="ext">The extension.</param>
		/// <param name="overwriteFlags">if set to <c>true</c> [overwrite flags].</param>
		/// <param name="grfName">Name of the GRF.</param>
		/// <returns>The path of the new GRF.</returns>
		protected string _toGrfContainer(string ext, bool overwriteFlags, string grfName = null) {
			try {
				if (State.HasFlags(ContainerState.Error))
					throw GrfExceptions.__CannotConvertWithErrors.Create();

				AProgress.Init(this);

				if (grfName == null) {
					grfName = TemporaryFilesManager.GetTemporaryFilePath("grf_convert_{0:0000}" + ext);
				}

				GrfPath.CreateDirectoryFromFile(grfName);

				using (FileStream output = new FileStream(grfName, FileMode.Create)) {
					output.SetLength(GrfHeader.DataByteSize);
					output.Seek(GrfHeader.DataByteSize, SeekOrigin.Begin);

					List<FileEntry> entries = new List<FileEntry>();
					long offset = GrfHeader.DataByteSize;
					GrfHeader header = new GrfHeader(null);

					for (int i = 0; i < Table.Entries.Count; i++) {
						AProgress.IsCancelling(this);

						TEntry entry = Table.Entries[i];
						FileEntry fileEntry = entry.ToFileEntry(header);

						byte[] data = entry.GetCompressedData();
						int sizeCompressed = data.Length;
						int sizeCompressedAlignment = sizeCompressed % 8 > 0 ? (8 - sizeCompressed % 8) + sizeCompressed : sizeCompressed;

						byte[] dataAligned = new byte[sizeCompressedAlignment];
						Buffer.BlockCopy(data, 0, dataAligned, 0, data.Length);

						output.Write(dataAligned, 0, sizeCompressedAlignment);

						fileEntry.FileExactOffset = offset;
						fileEntry.TemporaryOffset = offset;
						fileEntry.SizeCompressed = sizeCompressed;
						fileEntry.NewSizeCompressed = sizeCompressed;
						fileEntry.TemporarySizeCompressedAlignment = sizeCompressedAlignment;
						fileEntry.SizeCompressedAlignment = sizeCompressedAlignment;

						offset += sizeCompressedAlignment;
						entries.Add(fileEntry);
					}

					header.FileTableOffset = output.Position - GrfHeader.DataByteSize;
					header.RealFilesCount = entries.Count;

					int tableSize = _writeMetadata(output, header, entries, overwriteFlags);
					output.Seek(0, SeekOrigin.Begin);
					header.Write(output);
					output.SetLength(header.FileTableOffset + GrfHeader.DataByteSize + tableSize);
				}

				return grfName;
			}
			finally {
				AProgress.Finalize(this);
			}
		}

		/// <summary>
		/// Gets the shared stream, where the file was loaded.
		/// </summary>
		/// <returns>The shared stream.</returns>
		public virtual DisposableScope<FileStream> GetSharedStream() {
			return new DisposableScope<FileStream>(File.Exists(_reader.Stream.Name) ? new FileStream(_reader.Stream.Name, FileMode.Open, FileAccess.Read, FileShare.Read) : null);
		}

		/// <summary>
		/// Gets the source stream, used by the entries.
		/// </summary>
		/// <returns>The source stream.</returns>
		public virtual DisposableScope<FileStream> GetSourceStream() {
			return GetSharedStream();
		}

		/// <summary>
		/// Gets the file name of the source stream.
		/// </summary>
		/// <returns>The name of the source stream.</returns>
		public virtual string GetStreamName() {
			if (File.Exists(_reader.Stream.Name))
				return _reader.Stream.Name;

			return null;
		}

		/// <summary>
		/// Converts the current container to a GRF container.
		/// </summary>
		/// <param name="grfName">Name of the GRF.</param>
		/// <returns>The converted container to a GRF container.</returns>
		internal abstract Container ToGrfContainer(string grfName = null);

		/// <summary>
		/// Gets the file compressed sizes, by their extension.
		/// </summary>
		/// <returns>A table with all the extensions and their overall sizes in the container.</returns>
		internal List<Utilities.Extension.Tuple<string, string>> GetFileCompressedSizes() {
			Dictionary<string, int> sizes = new Dictionary<string, int>();

			foreach (var entry in Table.Entries) {
				string ext = entry.RelativePath.GetExtension();

				if (ext == null)
					ext = "";

				if (!sizes.ContainsKey(ext))
					sizes[ext] = 0;

				sizes[ext] += entry.SizeCompressed;
			}

			return sizes.OrderByDescending(p => p.Value).Select(keyPair => new Utilities.Extension.Tuple<string, string>(keyPair.Key, Methods.FileSizeToString(keyPair.Value))).ToList();
		}

		private int _writeMetadata(Stream output, GrfHeader header, IEnumerable<FileEntry> entries, bool overwriteFlags = true) {
			int tableSizeCompressed;

			using (MemoryStream stream = new MemoryStream()) {
				foreach (FileEntry entry in entries) {
					entry.WriteMetadata(header, stream, overwriteFlags);
				}

				stream.Seek(0, SeekOrigin.Begin);

				int tableSize = (int) stream.Length;
				byte[] dataCompressed = Compression.CompressDotNet(stream);
				tableSizeCompressed = dataCompressed.Length;

				output.Write(BitConverter.GetBytes(tableSizeCompressed), 0, 4);
				output.Write(BitConverter.GetBytes(tableSize), 0, 4);
				output.Write(dataCompressed, 0, dataCompressed.Length);
			}

			return tableSizeCompressed + 8;
		}
	}
}