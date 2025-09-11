using System.IO;
using GRF.Core;
using GRF.IO;
using Utilities;

namespace GRF.ContainerFormat {
	/// <summary>
	/// Base class for all entry types which are found inside the container's table.<para></para>
	/// An entry contains the necessary information to decompress and read a file in the container.
	/// </summary>
	public abstract class ContainerEntry {
		private readonly object _lock = new object();
		internal ByteReaderStream Stream;

		#region Modification shortcut

		/// <summary>
		/// Gets a value indicating whether this entry has been removed.
		/// </summary>
		public bool IsRemoved {
			get { return (Modification & Modification.Removed) == Modification.Removed; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance has been added.
		/// </summary>
		public bool IsAdded {
			get { return (Modification & Modification.Added) == Modification.Added; }
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="ContainerEntry" /> class.
		/// </summary>
		internal ContainerEntry() {
			Flags = EntryType.File;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ContainerEntry" /> class.
		/// </summary>
		/// <param name="file">The file.</param>
		protected ContainerEntry(string file) : this() {
			SourceFilePath = file;
			SizeDecompressed = (int) new FileInfo(file).Length;
		}

		/// <summary>
		/// Offsets in the stream for the container entry (this property is not directly used by GRFs).
		/// </summary>
		public long Offset { get; internal set; }

		/// <summary>
		/// Gets the compressed size of the entry. This property may not be set.
		/// </summary>
		public int SizeCompressed { get; internal set; }

		/// <summary>
		/// Gets the decompressed size of the entry. This property may not be set.
		/// </summary>
		public int SizeDecompressed { get; internal set; }

		/// <summary>
		/// Gets or sets the removed flag count. This indicates the amount of times <para></para>
		/// the entry has been removed.
		/// </summary>
		internal int RemovedFlagCount { get; set; }

		/// <summary>
		/// Gets the flags associated with this entry; this property is only used by GRF-type archives.
		/// </summary>
		public EntryType Flags { get; internal set; }

		/// <summary>
		/// Gets the source file path of the entry, this is used for added files.
		/// </summary>
		public string SourceFilePath { get; internal set; }

		protected string _relativePath;
		protected bool _relativePathModified = true;

		/// <summary>
		/// Gets the path of the entry in the container.
		/// </summary>
		public virtual string RelativePath {
			get {
				return _relativePath;
			}
			internal set {
				_relativePath = value;
				_relativePathModified = true;
			}
		}

		private string _directoryPath;
		private string _fileName;

		/// <summary>
		/// Gets the directory name of the entry in the container.
		/// </summary>
		public string DirectoryPath {
			get {
				if (_relativePathModified) {
					GrfPath.GetGrfEntryDirectoryNameAndFileName(_relativePath, out _directoryPath, out _fileName);
					_relativePathModified = false;
				}

				return _directoryPath;
			}
		}

		/// <summary>
		/// Gets the file name of the entry in the container.
		/// </summary>
		public string FileName {
			get {
				if (_relativePathModified) {
					GrfPath.GetGrfEntryDirectoryNameAndFileName(_relativePath, out _directoryPath, out _fileName);
					_relativePathModified = false;
				}

				return _fileName;
			}
		}

		/// <summary>
		/// Gets the current state of the entry.
		/// </summary>
		public Modification Modification { get; internal set; }

		/// <summary>
		/// Raw data information. This property is used when adding byte data instead of specifying a file.
		/// </summary>
		public MultiType RawDataSource { get; set; }

		/// <summary>
		/// Offset used when reading the container stream in threads (also used when saving the container).
		/// </summary>
		internal long TemporaryOffset { get; set; }

		/// <summary>
		/// Temporary compressed alignment size used when reading the container <para></para>
		/// stream in threads (also used when saving the container).
		/// </summary>
		internal int TemporarySizeCompressedAlignment { get; set; }

		/// <summary>
		/// Gets the decompressed data.<para></para>
		/// This operation is thread-safe.
		/// </summary>
		/// <returns>The decompressed data.</returns>
		public byte[] GetDecompressedData() {
			if (SourceFilePath != null) {
				if (SourceFilePath == GrfStrings.DataStreamId)
					return RawDataSource.Data;

				return File.ReadAllBytes(SourceFilePath);
			}

			lock (_lock) {
				return _getDecompressedData();
			}
		}

		/// <summary>
		/// Gets the compressed data for this entry.<para></para>
		/// This operation is thread-safe.
		/// </summary>
		/// <returns>The compressed data.</returns>
		public byte[] GetCompressedData() {
			if ((Flags & EntryType.RemoveFile) == EntryType.RemoveFile)
				return new byte[] {};

			if (SourceFilePath != null) {
				if (SourceFilePath == GrfStrings.DataStreamId)
					return _compress(RawDataSource.Data);

				return _compress(File.ReadAllBytes(SourceFilePath));
			}

			lock (_lock) {
				return _getCompressedData();
			}
		}

		/// <summary>
		/// Extracts the content of this entry to the application path.
		/// </summary>
		public void Extract() {
			ExtractFromAbsolute(GrfPath.Combine(Methods.ApplicationPath, RelativePath));
		}

		/// <summary>
		/// Extracts the content of this entry to the specified base directory.
		/// </summary>
		/// <param name="path">Base directory.</param>
		public void ExtractFromRelative(string path) {
			ExtractFromAbsolute(GrfPath.Combine(path, RelativePath));
		}

		/// <summary>
		/// Extracts the content of this entry to the specified path.
		/// </summary>
		/// <param name="filepath">The filepath.</param>
		public virtual void ExtractFromAbsolute(string filepath) {
			if (SourceFilePath != null) {
				if (File.Exists(filepath)) {
					try {
						File.Delete(filepath);
						File.Copy(SourceFilePath, filepath);
					}
					catch {
					}
				}
				else {
					try {
						File.Copy(SourceFilePath, filepath);
					}
					catch {
					}
				}
			}
			else {
				GrfPath.CreateDirectoryFromFile(filepath);
				GrfPath.Write(filepath, GetDecompressedData());
			}
		}

		/// <summary>
		/// Converts this entry to a FileEntry
		/// </summary>
		/// <param name="header">The header.</param>
		/// <returns>The converted file entry</returns>
		public virtual FileEntry ToFileEntry(GrfHeader header) {
			FileEntry entry = new FileEntry();

			entry.RelativePath = RelativePath;
			entry.SizeCompressed = SizeCompressed;
			entry.SizeCompressedAlignment = Methods.Align(SizeCompressed);
			entry.SizeDecompressed = SizeDecompressed;
			entry.Flags = Flags;
			entry.TemporaryOffset = Offset;
			entry.FileExactOffset = Offset;
			entry.TemporarySizeCompressedAlignment = entry.SizeCompressedAlignment;
			entry.NewSizeCompressed = SizeCompressed;
			entry.NewSizeDecompressed = SizeDecompressed;
			entry.ExtractionFilePath = null;
			entry.SourceFilePath = null;
			entry.Header = header;

			return entry;
		}

		/// <summary>
		/// Creates a copy of this instance.
		/// </summary>
		/// <returns>The copy.</returns>
		internal abstract ContainerEntry Copy();

		/// <summary>
		/// Clears the streams entirely from the entry.
		/// </summary>
		internal void RemoveStream() {
			if (Stream != null)
				Stream.Delete();

			Stream = null;
		}

		internal virtual void Delete() {
			
		}

		protected abstract byte[] _getDecompressedData();
		protected abstract byte[] _getCompressedData();

		protected abstract byte[] _compress(byte[] data);
		protected abstract byte[] _decompress(byte[] data);
	}
}