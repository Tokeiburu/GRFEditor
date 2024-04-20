using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GRF.Core;
using Utilities.CommandLine;
using Utilities.Extension;
using Utilities.Hash;

namespace GRF.Threading {
	/// <summary>
	/// This class extracts and copy the files from the GRF from a given range
	/// It's used to optimize the data transfer.
	/// </summary>
	public class ThreadScanGrf : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 8388608;
		private readonly StreamReadBlockInfo _srb = new StreamReadBlockInfo(_bufferSize);

		public static bool Cancelling { get; set; }
		public Dictionary<string, byte[]> Hashes = new Dictionary<string, byte[]>();
		private IHash _hash;
		private string _relativePath;

		public void Init(IHash hash, string relativePath) {
			_hash = hash;
			_relativePath = relativePath == null ? "" : relativePath + "?";
		}

		public override void Start() {
			new Thread(_start) { Name = "GRF - Hash thread " + StartIndex }.Start();
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

							if (data[entry.TemporaryOffset] != 0x78 && !entry.IsEmpty()) {
								int sizeDecomp = entry.SizeDecompressed;
								byte[] sDecomp = BitConverter.GetBytes(sizeDecomp);

								Hashes[_relativePath + entry.RelativePath] = new byte[] {
									255, 255, 255, 255, 255, 255, 255, 255,
									0, 0, 0, 0,
									sDecomp[0], sDecomp[1], sDecomp[2], sDecomp[3]
								};
								NumberOfFilesProcessed++;
								continue;
							}

							entry.Align(data, (int) entry.TemporaryOffset, out dataTmp);

							try {
								Hashes[_relativePath + entry.RelativePath] = _hash.ComputeByteHash(Compression.Decompress(dataTmp, entry.SizeDecompressed));
							}
							catch {
								Hashes[_relativePath + entry.RelativePath] = _hash.Error;
								throw new Exception("Bad file.");
								//Console.SetCursorPosition(0, Console.CursorTop);
								//CLHelper.Warning = "Failed to read file : " + entry.RelativePath;
							}

							NumberOfFilesProcessed++;
						}
					}

					foreach (FileEntry entryCopy in sortedEntriesAdded) {
						try {
							Hashes[_relativePath + entryCopy.RelativePath] = _hash.ComputeByteHash(entryCopy.GetDecompressedData());
						}
						catch {
							Hashes[_relativePath + entryCopy.RelativePath] = _hash.Error;

							Console.SetCursorPosition(0, Console.CursorTop);
							CLHelper.Warning = "Failed to read file : " + entryCopy.RelativePath;
						}

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
