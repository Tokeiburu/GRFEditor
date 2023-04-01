using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GRF.Core;
using GRF.Threading;

namespace GRF.IO {
	/// <summary>
	/// This class extracts and copy the files from the GRF from a given range
	/// It's used to optimize the data transfer.
	/// </summary>
	public class ThreadSetEncryption : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 8388608;
		private readonly StreamReadBlockInfo _srb = new StreamReadBlockInfo(_bufferSize);

		public override void Start() {
			new Thread(_start) {Name = "GRF - Set encryption flag thread " + StartIndex}.Start();
		}

		private void _start() {
			try {
				uint offset;

				using (var originalStream = _grfData.GetSourceStream()) {
					byte[] data;
					int toIndex = 0;
					int fromIndex;
					FileEntry entry;

					if (IsPaused)
						Pause();

					List<FileEntry> sortedEntries = _entries.Skip(StartIndex).Take(EndIndex - StartIndex).ToList();

					int indexMax = sortedEntries.Count;

					while (toIndex < indexMax) {
						fromIndex = toIndex;

						data = _srb.ReadMisaligned(sortedEntries, out toIndex, fromIndex, indexMax, originalStream.Value);

						for (int i = fromIndex; i < toIndex; i++) {
							if (!((Container) _grfData).InternalHeader.EncryptionCheckFlag ||
								_grfData.IsCancelling)
								return;

							if (IsPaused)
								Pause();

							entry = sortedEntries[i];

							offset = entry.TemporaryOffset + 1;

							if (entry.SizeCompressed >= 2) {
								if ((entry.Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
									continue;

								if (data[offset - 1] != 0 && (data[offset - 1] != 0x78 || (data[offset] != 0x9c && data[offset] != 0x01 && data[offset] != 0xDA && data[offset] != 0x5E))) {
									entry.Flags |= EntryType.GrfEditorCrypted;
									entry.OnPropertyChanged("Encrypted");
								}
								else if (data[offset - 1] == 0) {
									entry.Flags |= EntryType.LzmaCompressed;
									entry.OnPropertyChanged("Lzma");
								}
							}

							NumberOfFilesProcessed++;
						}
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