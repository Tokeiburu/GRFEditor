using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using GRF.Core;
using GRF.Threading;
using Utilities;

namespace GRF.IO {
	public class QuickOrderedGrfStreamReader {
		private const int _quickOrderedbufferSize = 8388608;
		private readonly Stream _stream;
		private readonly byte[] _buffer1 = new byte[_quickOrderedbufferSize];
		private readonly byte[] _buffer2 = new byte[_quickOrderedbufferSize];
		private long _filePosOffset1 = -1;
		private long _filePosOffset2 = -1;

		public QuickOrderedGrfStreamReader(Stream stream) {
			_stream = stream;

			// Read first blocks of data
			_filePosOffset1 = _stream.Position;
			_stream.Read(_buffer1, 0, _quickOrderedbufferSize);

			_filePosOffset2 = _stream.Position;
			_stream.Read(_buffer2, 0, _quickOrderedbufferSize);
		}

		public byte ReadByte(long offset) {
			if (offset < 0 || offset >= _stream.Length)
				throw new IndexOutOfRangeException("Offset used is out of range: " + offset + " / " + _stream.Length);

			do {
				// Within first buffer
				if (offset >= _filePosOffset1 && offset < _filePosOffset1 + _quickOrderedbufferSize) {
					return _buffer1[offset - _filePosOffset1];
				}

				// Within second buffer
				if (offset >= _filePosOffset2 && offset < _filePosOffset2 + _quickOrderedbufferSize) {
					return _buffer2[offset - _filePosOffset2];
				}

				// Read next block!
				if (_filePosOffset1 < _filePosOffset2) {
					_filePosOffset1 = _stream.Position;
					_stream.Read(_buffer1, 0, _quickOrderedbufferSize);
				}
				else {
					_filePosOffset2 = _stream.Position;
					_stream.Read(_buffer2, 0, _quickOrderedbufferSize);
				}
			}
			while (_stream.CanRead);

			return 0;
		}
	}

	//public class QuickOrderedGrfStreamReader {
	//	private const int _quickOrderedbufferSize = 8388608;
	//	private readonly MemoryMappedViewAccessor _accessor;
	//	private readonly long _length;
	//	private readonly byte[] _buffer1 = new byte[_quickOrderedbufferSize];
	//	private readonly byte[] _buffer2 = new byte[_quickOrderedbufferSize];
	//	private long _filePosOffset1 = 0;
	//	private long _filePosOffset2 = 0;
	//	private long _streamPosition = 0;
	//
	//	public QuickOrderedGrfStreamReader(MemoryMappedViewAccessor accessor, long length) {
	//		_accessor = accessor;
	//		_length = length;
	//
	//		_readBlock1();
	//		_readBlock2();
	//	}
	//
	//	private void _readBlock1() {
	//		_filePosOffset1 = _streamPosition;
	//		int bytesToRead = (int)Math.Min(_quickOrderedbufferSize, _accessor.Capacity - _filePosOffset1);
	//		_accessor.ReadArray(_filePosOffset1, _buffer1, 0, bytesToRead);
	//		_streamPosition = _filePosOffset1 + bytesToRead;
	//	}
	//
	//	private void _readBlock2() {
	//		_filePosOffset2 = _streamPosition;
	//		int bytesToRead = (int)Math.Min(_quickOrderedbufferSize, _accessor.Capacity - _filePosOffset2);
	//		_accessor.ReadArray(_filePosOffset2, _buffer2, 0, bytesToRead);
	//		_streamPosition = _filePosOffset2 + bytesToRead;
	//	}
	//
	//	public byte ReadByte(long offset) {
	//		if (offset < 0 || offset >= _length)
	//			throw new IndexOutOfRangeException("Offset used is out of range: " + offset + " / " + _length);
	//
	//		do {
	//			// Within first buffer
	//			if (offset >= _filePosOffset1 && offset < _filePosOffset1 + _quickOrderedbufferSize) {
	//				return _buffer1[offset - _filePosOffset1];
	//			}
	//
	//			// Within second buffer
	//			if (offset >= _filePosOffset2 && offset < _filePosOffset2 + _quickOrderedbufferSize) {
	//				return _buffer2[offset - _filePosOffset2];
	//			}
	//
	//			// Read next block!
	//			if (_filePosOffset1 < _filePosOffset2) {
	//				_readBlock1();
	//			}
	//			else {
	//				_readBlock2();
	//			}
	//		}
	//		while (_streamPosition < _length);
	//
	//		return 0;
	//	}
	//}

	/// <summary>
	/// This class looks for the entry encryption state and sets the EncryptionFlag to the FileEntry.
	/// The goal is purely visual.
	/// </summary>
	public class ThreadSetEncryption : GrfWriterThread<FileEntry> {
		public override void Start() {
			new Thread(_start) {Name = "GRF - Set encryption flag thread " + StartIndex}.Start();
		}

		private void _start() {
			try {
				using (var grfStream = _grfData.GetSourceStream()) {
					var grfData = new QuickOrderedGrfStreamReader(grfStream.Value);
					int toIndex = 0;
					byte b0;
					byte b1;
					FileEntry entry;

					if (IsPaused)
						Pause();

					List<FileEntry> sortedEntries = _entries.Skip(StartIndex).Take(EndIndex - StartIndex).OrderBy(p => p.FileExactOffset).ToList();

					int indexMax = sortedEntries.Count;

					while (toIndex < indexMax) {
						if (!((Container)_grfData).InternalHeader.EncryptionCheckFlag || _grfData.IsCancelling)
							return;

						if (IsPaused)
							Pause();

						int toRead = 3000;

						if (toIndex + toRead > indexMax) {
							toRead = indexMax - toIndex;
						}

						for (int i = toIndex; i < toIndex + toRead; i++) {
							entry = sortedEntries[i];
							long offset = entry.FileExactOffset;

							if (entry.SizeCompressed >= 2) {
								if ((entry.Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
									continue;

								b0 = grfData.ReadByte(offset);
								b1 = grfData.ReadByte(offset + 1);

								if (b0 != 0 && (b0 != 0x78 || (b1 != 0x9c && b1 != 0x01 && b1 != 0xDA && b1 != 0x5E))) {
									entry.Flags |= EntryType.GrfEditorCrypted;
									entry.OnPropertyChanged("Encrypted");
								}
								else if (b0 == 0) {
									entry.Flags |= EntryType.CustomCompressed;
									entry.OnPropertyChanged("CustomCompressed");
								}
							}

							NumberOfFilesProcessed++;
						}

						toIndex += toRead;
					}
				}
			}
			catch (Exception err) {
				Exception = err;
				Error = true;
			}
			finally {
				Terminated = true;
			}
		}
	}
}