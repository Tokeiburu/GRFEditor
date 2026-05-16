using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GRF.Core;
using Utilities.Extension;

namespace GRF.Threading {
	public enum ExtractionFailReason {
		CannotWriteFileToDisk,
		CorruptedOrEncrypted,
		GravityEncrypted,
		SourceFileMissing,
	}

	public class ExtractionResult {
		public FileEntry Entry;
		public ExtractionFailReason Reason;

		public ExtractionResult(FileEntry entry, ExtractionFailReason reason) {
			Entry = entry;
			Reason = reason;
		}
	}

	/// <summary>
	/// This class extracts and copy the files from the GRF from a given range
	/// It's used to optimize the data transfer.
	/// </summary>
	public class GrfThreadExtract : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 8388608;
		private readonly StreamReadBlockInfo _srb = new StreamReadBlockInfo(_bufferSize);
		public List<ExtractionResult> ErrorResults = new List<ExtractionResult>();

		public override void Start() {
			new Thread(_start) { Name = "GRF - Extract files thread " + StartIndex }.Start();
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
						data = _srb.ReadMisaligned(sortedEntries, out toIndex, fromIndex, indexMax, originalStream.Value, out _);

						for (int i = fromIndex; i < toIndex; i++) {
							if (_grfData.IsCancelling)
								return;

							if (IsPaused)
								Pause();

							entry = sortedEntries[i];
							entry.GrfEditorDecryptRequested(data);
							entry.DesDecrypt(data, (int) entry.TemporaryOffset);
							entry.Align(data, (int) entry.TemporaryOffset, out dataTmp);
							
							if (entry.RelativePath.GetExtension() == null) {
								// Fix: 2018-09-26
								// Sometimes Gravity sent the directory link as a file inside the GRF, ignore those when extraccting.
								if (_grfData.Table.ContainsDirectory(entry.RelativePath)) {
									NumberOfFilesProcessed++;
									continue;
								}
							}

							try {
								if (entry.Flags.HasFlag(EntryType.GravityEncryptedFile)) {
									Error = true;
									ErrorResults.Add(new ExtractionResult(entry, ExtractionFailReason.GravityEncrypted));
									continue;
								}
								else if ((entry.Flags & EntryType.LZSS) == EntryType.LZSS)
									dataTmp = Compression.LzssDecompress(dataTmp, entry.SizeDecompressed);
								else if ((entry.Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
									dataTmp = Compression.RawDecompress(dataTmp, entry.SizeDecompressed);
								else if (Compression.IsNormalCompression && (dataTmp.Length > 1 && dataTmp[0] == 0))
									dataTmp = Compression.DecompressLzma(dataTmp, entry.SizeDecompressed);
								else
									dataTmp = Compression.Decompress(dataTmp, entry.SizeDecompressed);

								if (dataTmp != null) {
									using (FileStream fs = new FileStream(entry.ExtractionFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
										fs.Write(dataTmp, 0, dataTmp.Length);
								}
							}
							catch (IOException err) {
								Error = true;
								ErrorResults.Add(new ExtractionResult(entry, ExtractionFailReason.CannotWriteFileToDisk));
								Exception = err;
							}
							catch (Exception err) {
								Error = true;
								ErrorResults.Add(new ExtractionResult(entry, ExtractionFailReason.CorruptedOrEncrypted));
								Exception = err;
							}
							finally {
								entry.TemporaryOffset = 0;
								NumberOfFilesProcessed++;
							}
						}
					}

					foreach (FileEntry entryCopy in sortedEntriesAdded) {
						try {
							if (entryCopy.SourceFilePath != entryCopy.ExtractionFilePath) {
								if (_grfData.IsCancelling)
									return;

								if (IsPaused)
									Pause();

								if (File.Exists(entryCopy.ExtractionFilePath))
									File.Delete(entryCopy.ExtractionFilePath);

								if (!File.Exists(entryCopy.SourceFilePath)) {
									ErrorResults.Add(new ExtractionResult(entryCopy, ExtractionFailReason.SourceFileMissing));
									continue;
								}

								File.Copy(entryCopy.SourceFilePath, entryCopy.ExtractionFilePath);
							}
						}
						catch (Exception err) {
							Error = true;
							ErrorResults.Add(new ExtractionResult(entryCopy, ExtractionFailReason.CannotWriteFileToDisk));
							Exception = err;
						}
						finally {
							NumberOfFilesProcessed++;
						}
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
