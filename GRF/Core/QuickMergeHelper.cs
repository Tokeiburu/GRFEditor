using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.ContainerFormat.Commands;
using Utilities.Extension;

namespace GRF.Core {
	internal class QuickMergeHelper {
		public static int MaximumFragmentedSpace = 20971520;
		private readonly Container _grf;
		private List<Utilities.Extension.Tuple<long, long>> _freeSpace = new List<Utilities.Extension.Tuple<long, long>>();
		private int _virtualSpaceAdded;

		public QuickMergeHelper(Container container) {
			_grf = container;

			_calculateFreespace();
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

		private void _calculateFreespace() {
			_freeSpace = new List<Utilities.Extension.Tuple<long, long>>();

			long bufferLength;
			List<FileEntry> sortedEntries = _grf.Table.Entries.Where(p => !p.Added).OrderBy(p => p.FileExactOffset).ToList();
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

		public void Write(Stream originalStream, ref long endStreamOffset, byte[] data, FileEntry entry) {
			long? possibleOffset = _getNextFreeIndex(entry);

			if (possibleOffset != null) {
				entry.TemporaryOffset = possibleOffset.Value;
				entry.FileExactOffset = possibleOffset.Value;
				originalStream.Seek(possibleOffset.Value, SeekOrigin.Begin);
				originalStream.Write(data, 0, data.Length);
			}
			else {
				long offset = endStreamOffset;
				entry.TemporaryOffset = offset;
				entry.FileExactOffset = endStreamOffset;
				originalStream.Seek(endStreamOffset, SeekOrigin.Begin);
				originalStream.Write(data, 0, data.Length);
				endStreamOffset += data.Length;
			}
		}

		public bool ShouldRepackInstead(List<FileEntry> entries, List<FileEntry> entriesAdded) {
			if (_grf.InternalHeader.IsEncrypting || _grf.InternalHeader.IsDecrypting) return true;
			if (_grf.InternalHeader.IsEncrypted && !_grf.InternalHeader.EncryptFileTable) return true;
			if (_grf.InternalHeader.EncryptionKey != null && !_grf.InternalHeader.EncryptFileTable) return true;
			if (_grf.Table.Entries.Any(p => (p.Modification & Modification.Encrypt) == Modification.Encrypt || (p.Modification & Modification.Decrypt) == Modification.Decrypt)) return true;
			if (_grf.Commands.HasCommand<ChangeVersion<FileEntry>>()) return true;
			if (_grf.Table.Entries.Count < 30) return true; // Always rewrite small GRFs
			if (entriesAdded.Count > 500) return true;

			var freeSpace = new List<Utilities.Extension.Tuple<long, long>>();

			for (int i = 0; i < _freeSpace.Count; i++) {
				freeSpace.Add(new Utilities.Extension.Tuple<long, long>(_freeSpace[i].Item1, _freeSpace[i].Item2));
			}

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