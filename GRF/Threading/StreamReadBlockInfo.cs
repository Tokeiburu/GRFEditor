using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Core;
using Utilities;

namespace GRF.Threading {
	internal class StreamReadBlockInfo {
		internal struct BlockInfo {
			public long StreamPosition;
			public int StreamReadLength;
			public int BufferPosition;
			public int BufferLength;
			public int From;
			public int To;
		}

		//public List<Tuple<int, int>> Blocks = new List<Tuple<int, int>>();
		private readonly int _bufferSize;

		public StreamReadBlockInfo(int bufferSize) {
			_bufferSize = bufferSize;
		}

		public byte[] ReadMisaligned(List<FileEntry> entries, out int toIndex, int @from, int to, Stream stream) {
			int streamBufferSize = 0;
			int bufferLength = 0;
			List<BlockInfo> blocks = new List<BlockInfo>();
			FileEntry entryFrom;

			while (streamBufferSize < _bufferSize && from < to) {
				BlockInfo block = new BlockInfo();
				block.From = from;

				entryFrom = entries[from];
				block.From = from;

				entryFrom.TemporaryOffset = streamBufferSize + bufferLength;
				bufferLength += entryFrom.SizeCompressedAlignment;
				from++;

				while (bufferLength < _bufferSize && from < to) {
					if (entries[from - 1].FileExactOffset + entries[from - 1].SizeCompressedAlignment == entries[from].FileExactOffset) {
						entryFrom = entries[from];
						entryFrom.TemporaryOffset = streamBufferSize + bufferLength;
						bufferLength += entryFrom.SizeCompressedAlignment;
						from++;
					}
					else {
						break;
					}
				}

				block.StreamPosition = entries[block.From].FileExactOffset;
				block.StreamReadLength = bufferLength;
				block.BufferPosition = streamBufferSize;
				block.To = from;
				blocks.Add(block);
				streamBufferSize += bufferLength;
				bufferLength = 0;
			}

			BlockInfo bi;
			byte[] data = new byte[streamBufferSize];

			for (int i = 0; i < blocks.Count; i++) {
				bi = blocks[i];

				stream.Seek(bi.StreamPosition, SeekOrigin.Begin);
				stream.Read(data, bi.BufferPosition, bi.StreamReadLength);
			}

			toIndex = from;
			return data;
		}

		public byte[] ReadAligned(List<FileEntry> entries, out int toIndex, int @from, int to, Stream stream) {
			int streamBufferSize = 0;
			int bufferLength = 0;
			int dataBufferLength = 0;
			int ovDataBufferLength = 0;
			List<BlockInfo> blocks = new List<BlockInfo>();
			FileEntry entryFrom;

			foreach (FileEntry entry in entries.Skip(@from).Take(to - @from)) {
				entry.TemporarySizeCompressedAlignment = Methods.Align(entry.SizeCompressedAlignment);
			}

			while (streamBufferSize < _bufferSize && from < to) {
				BlockInfo block = new BlockInfo();
				block.From = from;

				entryFrom = entries[from];
				block.From = from;
				block.BufferPosition = ovDataBufferLength;

				entryFrom.TemporaryOffset = bufferLength;
				bufferLength += entryFrom.SizeCompressedAlignment;
				dataBufferLength += entryFrom.TemporarySizeCompressedAlignment;
				from++;

				while (bufferLength < _bufferSize && from < to) {
					if (entries[from - 1].FileExactOffset + entries[from - 1].SizeCompressedAlignment == entries[from].FileExactOffset) {
						entryFrom = entries[from];
						entryFrom.TemporaryOffset = bufferLength;
						bufferLength += entryFrom.SizeCompressedAlignment;
						dataBufferLength += entryFrom.TemporarySizeCompressedAlignment;
						from++;
					}
					else {
						break;
					}
				}

				block.StreamPosition = entries[block.From].FileExactOffset;
				block.StreamReadLength = bufferLength;
				block.BufferLength = dataBufferLength;
				block.To = from;
				blocks.Add(block);
				streamBufferSize += bufferLength;
				bufferLength = 0;
				ovDataBufferLength += dataBufferLength;
				dataBufferLength = 0;
			}

			byte[] data = new byte[ovDataBufferLength];
			BlockInfo bi;
			FileEntry currentEntry;
			int offset;

			for (int i = 0; i < blocks.Count; i++) {
				bi = blocks[i];

				if (bi.BufferLength == bi.StreamReadLength) {
					stream.Seek(bi.StreamPosition, SeekOrigin.Begin);
					stream.Read(data, bi.BufferPosition, bi.StreamReadLength);
				}
				else {
					byte[] streamData = new byte[bi.StreamReadLength];
					stream.Seek(bi.StreamPosition, SeekOrigin.Begin);
					stream.Read(streamData, 0, bi.StreamReadLength);

					for (int j = bi.From, top = bi.To; j < top; ) {
						entryFrom = entries[j];

						int toCopy = 0;
						offset = 0;

						while (j < top) {
							currentEntry = entries[j];
							toCopy += currentEntry.SizeCompressedAlignment;
							offset += currentEntry.TemporarySizeCompressedAlignment;
							j++;

							if (currentEntry.TemporarySizeCompressedAlignment != currentEntry.SizeCompressedAlignment) {
								break;
							}
						}

						try {
							Buffer.BlockCopy(streamData, (int) entryFrom.TemporaryOffset, data, bi.BufferPosition, toCopy);
							bi.BufferPosition += offset;
						}
						catch (Exception) {
						}
					}
				}
			}

			toIndex = from;

			if (blocks.Count == 1 && blocks[0].StreamReadLength == blocks[0].BufferLength)
				return data;

			int init = blocks[0].From;
			offset = 0;

			for (int i = init; i < from; i++) {
				entries[i].TemporaryOffset = offset;
				offset += entries[i].TemporarySizeCompressedAlignment;
			}

			return data;
		}

		public byte[] ReadAligned<TEntry>(List<TEntry> entries, out int toIndex, int @from, int to, Stream stream) where TEntry : ContainerEntry {
			int streamBufferSize = 0;
			int bufferLength = 0;
			int dataBufferLength = 0;
			int ovDataBufferLength = 0;
			List<BlockInfo> blocks = new List<BlockInfo>();
			TEntry entryFrom;

			foreach (TEntry entry in entries.Skip(@from).Take(to - @from)) {
				entry.TemporarySizeCompressedAlignment = Methods.Align(entry.SizeCompressed);
			}

			while (streamBufferSize < _bufferSize && from < to) {
				BlockInfo block = new BlockInfo();
				block.From = from;

				entryFrom = entries[from];
				block.From = from;
				block.BufferPosition = ovDataBufferLength;

				entryFrom.TemporaryOffset = bufferLength;
				bufferLength += entryFrom.SizeCompressed;
				dataBufferLength += entryFrom.TemporarySizeCompressedAlignment;
				from++;

				while (bufferLength < _bufferSize && from < to) {
					if (entries[from - 1].Offset + entries[from - 1].SizeCompressed == entries[from].Offset) {
						entryFrom = entries[from];
						entryFrom.TemporaryOffset = bufferLength;
						bufferLength += entryFrom.SizeCompressed;
						dataBufferLength += entryFrom.TemporarySizeCompressedAlignment;
						from++;
					}
					else {
						break;
					}
				}

				block.StreamPosition = entries[block.From].Offset;
				block.StreamReadLength = bufferLength;
				block.BufferLength = dataBufferLength;
				block.To = from;
				blocks.Add(block);
				streamBufferSize += bufferLength;
				bufferLength = 0;
				ovDataBufferLength += dataBufferLength;
				dataBufferLength = 0;
			}

			byte[] data = new byte[ovDataBufferLength];
			BlockInfo bi;
			TEntry currentEntry;
			int offset;

			for (int i = 0; i < blocks.Count; i++) {
				bi = blocks[i];

				if (bi.BufferLength == bi.StreamReadLength) {
					stream.Seek(bi.StreamPosition, SeekOrigin.Begin);
					stream.Read(data, bi.BufferPosition, bi.StreamReadLength);
				}
				else {
					byte[] streamData = new byte[bi.StreamReadLength];
					stream.Seek(bi.StreamPosition, SeekOrigin.Begin);
					stream.Read(streamData, 0, bi.StreamReadLength);

					for (int j = bi.From, top = bi.To; j < top;) {
						entryFrom = entries[j];

						int toCopy = 0;
						offset = 0;

						while (j < top) {
							currentEntry = entries[j];
							toCopy += currentEntry.SizeCompressed;
							offset += currentEntry.TemporarySizeCompressedAlignment;
							j++;

							if (currentEntry.TemporarySizeCompressedAlignment != currentEntry.SizeCompressed) {
								break;
							}
						}

						try {
							Buffer.BlockCopy(streamData, (int) entryFrom.TemporaryOffset, data, bi.BufferPosition, toCopy);
							bi.BufferPosition += offset;
						}
						catch (Exception) {
						}
					}
				}
			}

			toIndex = from;

			if (blocks.Count == 1 && blocks[0].StreamReadLength == blocks[0].BufferLength)
				return data;

			int init = blocks[0].From;
			offset = 0;

			for (int i = init; i < from; i++) {
			    entries[i].TemporaryOffset = offset;
			    offset += entries[i].TemporarySizeCompressedAlignment;
			}

			return data;
		}
	}
}
