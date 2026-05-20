using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.Core;
using Utilities;

namespace GRF.Threading {
	internal class StreamReadBlockInfo {
		private int _bufferSize;

		public int SeekCalls;
		public int ReadCalls;

		public StreamReadBlockInfo(int bufferSize) {
			_bufferSize = bufferSize;
			_readBuffer = new byte[_bufferSize];
		}

		private byte[] _readBuffer;

		public byte[] ReadMisaligned(List<FileEntry> entries, out int toIndex, int indexFrom, int indexTo, Stream stream, out int dataLength) {
			return Read(entries, out toIndex, indexFrom, indexTo, stream, out dataLength, false, 0);
		}

		public byte[] ReadAligned(List<FileEntry> entries, out int toIndex, int indexFrom, int indexTo, Stream stream, out int dataLength) {
			return Read(entries, out toIndex, indexFrom, indexTo, stream, out dataLength, true, 0);
		}

		public byte[] Read(List<FileEntry> entries, out int toIndex, int indexFrom, int indexTo, Stream stream, out int dataLength, bool isDataAligned, int indexBufferStart) {
			if (indexFrom == indexTo) {
				dataLength = 0;
				toIndex = indexFrom;
				return null;
			}

			FileEntry entry;
			long bufferReadLength = indexBufferStart;
			FileEntry firstEntry = entries[indexFrom];
			long startOffset = firstEntry.FileExactOffset;
			int processedEntryCount = 0;

			firstEntry.TemporarySizeCompressedAlignment = Methods.Align(firstEntry.SizeCompressedAlignment);

			if (indexBufferStart != 0 && firstEntry.TemporarySizeCompressedAlignment + bufferReadLength > _bufferSize) {
				dataLength = 0;
				toIndex = indexFrom;
				return null;
			}

			while (indexBufferStart == 0 && _bufferSize < firstEntry.TemporarySizeCompressedAlignment)
				_bufferSize *= 2;

			if (_bufferSize != _readBuffer.Length)
				_readBuffer = new byte[_bufferSize];

			stream.Seek(startOffset, SeekOrigin.Begin);
			SeekCalls++;

			for (int i = indexFrom; i < indexTo; i++) {
				entry = entries[i];

				//if (entry.SizeCompressedAlignment != Methods.Align(entry.SizeCompressedAlignment)) {
				//	Z.F();
				//}

				entry.TemporarySizeCompressedAlignment = Methods.Align(entry.SizeCompressedAlignment);

				// No more space available in the buffer
				if (entry.TemporarySizeCompressedAlignment + bufferReadLength > _bufferSize)
					break;
				// Entry data isn't in the data stream block being read, just return
				if (entry.FileExactOffset - startOffset + entry.TemporarySizeCompressedAlignment > _bufferSize)
					break;
				// The requested data must be aligned, often used when the data is written back directly as is
				if (isDataAligned && entry.FileExactOffset - startOffset + indexBufferStart != bufferReadLength)
					break;

				entry.TemporaryOffset = entry.FileExactOffset - startOffset + indexBufferStart;
				bufferReadLength = entry.TemporaryOffset + entry.TemporarySizeCompressedAlignment;
				processedEntryCount++;
			}

			int totalRead = 0;
			bufferReadLength -= indexBufferStart;

			do {
				totalRead += stream.Read(_readBuffer, totalRead + indexBufferStart, (int)bufferReadLength - totalRead);
				ReadCalls++;
			} while (totalRead < bufferReadLength);

			toIndex = indexFrom + processedEntryCount;
			dataLength = (int)bufferReadLength;
			return _readBuffer;
		}
	}
}
