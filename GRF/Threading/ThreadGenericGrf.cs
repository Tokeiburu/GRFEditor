using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GRF.Core;
using Utilities.Extension;

namespace GRF.Threading {
	/// <summary>
	/// This thread executes a common operation on GRF entries (only)
	/// </summary>
	public class ThreadGenericGrf : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 8388608;
		private readonly StreamReadBlockInfo _srb = new StreamReadBlockInfo(_bufferSize);

		public static bool IgnoreUnreadableFiles { get; set; }
		public static bool Cancelling { get; set; }
		private Action<FileEntry, byte[]> _function;

		public void Init(Action<FileEntry, byte[]> function) {
			_function = function;
		}

		public override void Start() {
			new Thread(_start) { Name = "GRF - Generic GRF thread " + StartIndex }.Start();
		}

		private void _start() {
			try {
				using (var originalStream = _grfData.GetSourceStream()) {
					byte[] data;

					int toIndex = 0;

					int fromIndex;
					byte[] dataTmp;
					FileEntry entry;

					if (IsPaused)
						Pause();

					List<FileEntry> allSortedEntries = _entries.Skip(StartIndex).Take(EndIndex - StartIndex).ToList();
					List<FileEntry> sortedEntries = allSortedEntries.Where(p => !p.Modification.HasFlags(Modification.Added)).OrderBy(p => p.FileExactOffset).ToList();
					List<FileEntry> sortedEntriesAdded = allSortedEntries.Where(p => p.Modification.HasFlags(Modification.Added)).ToList();
					int indexMax = sortedEntries.Count;

					while (toIndex < indexMax) {
						fromIndex = toIndex;
						data = _srb.ReadMisaligned(sortedEntries, out toIndex, fromIndex, indexMax, originalStream.Value);

						for (int i = fromIndex; i < toIndex; i++) {
							if (Cancelling)
								return;

							if (IsPaused)
								Pause();

							entry = sortedEntries[i];
							entry.DesDecrypt(data, (int) entry.TemporaryOffset);

							if (data[entry.TemporaryOffset] != 0x78 && !entry.IsEmpty() && data[entry.TemporaryOffset] != 0) {
								NumberOfFilesProcessed++;
								continue;
							}

							entry.Align(data, (int) entry.TemporaryOffset, out dataTmp);

							try {
								try {
									if (dataTmp.Length > 0 && Compression.IsNormalCompression && dataTmp[0] == 0)
										dataTmp = Compression.DecompressLzma(dataTmp, entry.SizeDecompressed);
									else if ((entry.Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
										dataTmp = Compression.RawDecompress(dataTmp, entry.SizeDecompressed);
									else
										dataTmp = Compression.Decompress(dataTmp, entry.SizeDecompressed);
								}
								catch {
									try {
										dataTmp = Compression.DecompressZlib(dataTmp, entry.SizeDecompressed);
									}
									catch {
										// Do nothing, leave it as is
									}
								}

								try {
									_function(entry, dataTmp);
								}
								catch {
								}
							}
							catch {
								if (!IgnoreUnreadableFiles)
									throw;
							}

							NumberOfFilesProcessed++;
						}
					}

					foreach (FileEntry entryCopy in sortedEntriesAdded) {
						dataTmp = entryCopy.GetDecompressedData();
						_function(entryCopy, dataTmp);
						NumberOfFilesProcessed++;
					}
				}
			}
			catch (Exception err) {
				Error = true;
				Exception = err;
			}
			finally {
				Terminated = true;
			}
		}
	}
}
