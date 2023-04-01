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
		private List<Tuple<uint, uint>> _freeSpace = new List<Tuple<uint, uint>>();
		private int _virtualSpaceAdded;

		public QuickMergeHelper(Container container) {
			_grf = container;

			_calculateFreespace();
		}

		private uint? _getNextFreeIndex(FileEntry entry) {
			Tuple<uint, uint> tup = _freeSpace.FirstOrDefault(p => p.Item2 > entry.TemporarySizeCompressedAlignment);

			if (tup != null) {
				uint offset = tup.Item1;
				tup.Item2 -= (uint) entry.TemporarySizeCompressedAlignment;
				tup.Item1 += (uint) entry.TemporarySizeCompressedAlignment;
				return offset;
			}

			return null;
		}

		private void _getNextFreeIndex(IList<Tuple<uint, uint>> freeSpace, ContainerEntry entry) {
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
			_freeSpace = new List<Tuple<uint, uint>>();

			uint bufferLength;
			List<FileEntry> sortedEntries = _grf.Table.Entries.Where(p => !p.Added).OrderBy(p => p.FileExactOffset).ToList();
			int indexMax = sortedEntries.Count;
			uint endOffset;

			try {
				if (indexMax > 0) {
					if (sortedEntries[0].FileExactOffset > GrfHeader.StructSize) {
						_freeSpace.Add(new Tuple<uint, uint>(GrfHeader.StructSize, sortedEntries[0].FileExactOffset - GrfHeader.StructSize));
					}
				}
				for (int i = 0; i < indexMax - 1; i++) {
					endOffset = sortedEntries[i].FileExactOffset + (uint) sortedEntries[i].SizeCompressedAlignment;

					if (endOffset != sortedEntries[i + 1].FileExactOffset && sortedEntries[i + 1].FileExactOffset > endOffset) {
						bufferLength = sortedEntries[i + 1].FileExactOffset - endOffset;
						_freeSpace.Add(new Tuple<uint, uint>(endOffset, bufferLength));
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			_freeSpace = _freeSpace.OrderBy(p => p.Item2).ToList();
		}

		public void Write(Stream originalStream, ref long endStreamOffset, byte[] data, FileEntry entry) {
			uint? possibleOffset = _getNextFreeIndex(entry);

			if (possibleOffset != null) {
				entry.TemporaryOffset = possibleOffset.Value;
				entry.FileExactOffset = possibleOffset.Value;
				originalStream.Seek(possibleOffset.Value, SeekOrigin.Begin);
				originalStream.Write(data, 0, data.Length);
			}
			else {
				long offset = endStreamOffset;
				entry.TemporaryOffset = (uint)offset;
				entry.FileExactOffset = (uint)endStreamOffset;
				originalStream.Seek(endStreamOffset, SeekOrigin.Begin);
				originalStream.Write(data, 0, data.Length);
				endStreamOffset += (uint) data.Length;
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

			List<Tuple<uint, uint>> freeSpace = new List<Tuple<uint, uint>>();

			for (int i = 0; i < _freeSpace.Count; i++) {
				freeSpace.Add(new Tuple<uint, uint>(_freeSpace[i].Item1, _freeSpace[i].Item2));
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

		private void _calculateVirtualFreespace(IList<Tuple<uint, uint>> freeSpace, ContainerEntry entry) {
			_getNextFreeIndex(freeSpace, entry);
		}
	}
}