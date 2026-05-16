using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.ContainerFormat.Commands;

namespace GRF.Core {
	internal class QuickMergeHelper {
		public static int MaximumFragmentedSpace = 20971520;
		private readonly Container _grf;
		private List<Utilities.Extension.Tuple<long, long>> _freeSpace = new List<Utilities.Extension.Tuple<long, long>>();
		private long _virtualSpaceAdded;
		public int WriteCalls = 0;

		public QuickMergeHelper(Container container, List<FileEntry> continuousEntries) {
			_grf = container;

			_calculateFreespace(continuousEntries);
		}

		private long? _getNextFreeIndex(FileEntry entry) {
			var tup = _freeSpace.FirstOrDefault(p => p.Item2 > entry.TemporarySizeCompressedAlignment);

			if (tup != null) {
				long offset = tup.Item1;
				tup.Item2 -= entry.TemporarySizeCompressedAlignment;
				tup.Item1 += entry.TemporarySizeCompressedAlignment;
				return offset;
			}

			return null;
		}

		private void _getNextFreeIndex(IList<Utilities.Extension.Tuple<long, long>> freeSpace, ContainerEntry entry) {
			for (int i = 0, count = freeSpace.Count; i < count; i++) {
				if (freeSpace[i].Item2 > entry.TemporarySizeCompressedAlignment) {
					var tup = freeSpace[i];
					tup.Item2 -= (uint) entry.TemporarySizeCompressedAlignment;
					tup.Item1 += (uint) entry.TemporarySizeCompressedAlignment;
					return;
				}
			}

			_virtualSpaceAdded += entry.TemporarySizeCompressedAlignment;
		}

		private void _calculateFreespace(List<FileEntry> continuousEntries) {
			_freeSpace = new List<Utilities.Extension.Tuple<long, long>>();

			long bufferLength;
			List<FileEntry> sortedEntries = continuousEntries;
			int indexMax = sortedEntries.Count;
			long endOffset;

			try {
				if (indexMax > 0) {
					if (sortedEntries[0].FileExactOffset > GrfHeader.DataByteSize) {
						_freeSpace.Add(new Utilities.Extension.Tuple<long, long>(GrfHeader.DataByteSize, sortedEntries[0].FileExactOffset - GrfHeader.DataByteSize));
					}
				}
				for (int i = 0; i < indexMax - 1; i++) {
					endOffset = sortedEntries[i].FileExactOffset + (long) sortedEntries[i].SizeCompressedAlignment;

					if (endOffset != sortedEntries[i + 1].FileExactOffset && sortedEntries[i + 1].FileExactOffset > endOffset) {
						bufferLength = sortedEntries[i + 1].FileExactOffset - endOffset;
						_freeSpace.Add(new Utilities.Extension.Tuple<long, long>(endOffset, bufferLength));
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			_freeSpace = _freeSpace.OrderBy(p => p.Item2).ToList();
		}

		public void Write(Stream originalStream, ref long endStreamOffset, byte[] data, int dataOffset, int dataLength, FileEntry entry) {
			long? possibleOffset = _getNextFreeIndex(entry);

			if (possibleOffset != null) {
				entry.TemporaryOffset = possibleOffset.Value;
				entry.FileExactOffset = possibleOffset.Value;
				originalStream.Seek(possibleOffset.Value, SeekOrigin.Begin);
				originalStream.Write(data, dataOffset, dataLength);
				WriteCalls++;
			}
			else {
				long offset = endStreamOffset;
				entry.TemporaryOffset = offset;
				entry.FileExactOffset = endStreamOffset;
				originalStream.Seek(endStreamOffset, SeekOrigin.Begin);
				originalStream.Write(data, dataOffset, dataLength);
				WriteCalls++;
				endStreamOffset += dataLength;
			}
		}

		public void Write(Stream originalStream, ref long endStreamOffset, byte[] data, FileEntry entry) {
			Write(originalStream, ref endStreamOffset, data, 0, data.Length, entry);
		}

		public bool ShouldRepackInstead(List<FileEntry> entries, List<FileEntry> entriesAdded) {
			long totalSizeEncryptionModified = 0;
			long encryptedFileCount = 0;

			foreach (var entry in _grf.Table.Entries) {
				if ((entry.Modification & Modification.Added) == Modification.Added)
					continue;

				if ((entry.Modification & Modification.Encrypt) == Modification.Encrypt) {
					if ((entry.Flags & EntryType.GrfEditorCrypted) != EntryType.GrfEditorCrypted) {
						totalSizeEncryptionModified += entry.NewSizeCompressed;
						encryptedFileCount++;
					}
				}
				else if ((entry.Modification & Modification.Decrypt) == Modification.Decrypt) {
					if ((entry.Flags & EntryType.GrfEditorCrypted) == EntryType.GrfEditorCrypted) {
						totalSizeEncryptionModified += entry.NewSizeCompressed;
						encryptedFileCount++;
					}
				}
			}

			// If more than 32MB of data is encrypted, use the file copy method instead
			// Or if more than 50 files are encrypted, use the file copy method instead
			// Note: Encrypted files via FileEdit mode requires seek/write for each entry, which is slow.
			if (totalSizeEncryptionModified > 1 << 27 || encryptedFileCount > 50) return true;

			// Changing version requires scanning and writing the entire GRF
			if (_grf.Commands.HasCommand<ChangeVersion<FileEntry>>()) return true;

			// Adding more than 500 files would be faster though threads
			// ^ Why? Adding files is done through a thread either way, doing a file copy would change nothing
			// Though I suppose it would support cancel...?
			if (entriesAdded.Count > 500) return true;

			var freeSpace = new List<Utilities.Extension.Tuple<long, long>>();

			for (int i = 0; i < _freeSpace.Count; i++) {
				freeSpace.Add(new Utilities.Extension.Tuple<long, long>(_freeSpace[i].Item1, _freeSpace[i].Item2));
			}

			_virtualSpaceAdded = 0;

			foreach (FileEntry entry in entries) {
				_calculateVirtualFreespace(freeSpace, entry);
			}

			long availableSpace = freeSpace.Sum(p => p.Item2);

			if (availableSpace > MaximumFragmentedSpace)
				return true;

			var totalSize = _grf.InternalHeader.FileTableOffset + _virtualSpaceAdded;

			// > 10% wasted space
			if (availableSpace / (double) totalSize > 0.1d)
				return true;

			return false;
		}

		private void _calculateVirtualFreespace(IList<Utilities.Extension.Tuple<long, long>> freeSpace, ContainerEntry entry) {
			_getNextFreeIndex(freeSpace, entry);
		}
	}
}