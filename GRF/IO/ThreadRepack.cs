using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GRF.Core;
using GRF.System;
using GRF.Threading;
using Utilities;

namespace GRF.IO {
	/// <summary>
	/// This class extracts and copy the files from the GRF from a given range
	/// It's used to optimize the data transfer.
	/// </summary>
	public class ThreadRepack : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 16777216;
		private readonly StreamReadBlockInfo _srb = new StreamReadBlockInfo(_bufferSize);
		private byte[] _dataTmp;

		public override void Start() {
			new Thread(_start) {Name = "GRF - Repacking thread " + StartIndex}.Start();
		}

		private void _start() {
			try {
				// Fix : 2016-11-28
				// This path must be unique enough that it doesn't create conflicts with other
				// applications or mass savings. A single instance of GRFE won't cause any issues though.
				FileName = Path.Combine(Settings.TempPath, "~rtmp" + StartIndex + "_" + _grfData.UniqueString);

				using (FileStream outPutFile = new FileStream(FileName, FileMode.Create))
				using (var originalStream = _grfData.GetSourceStream()) {
					byte[] data;
					int toIndex = 0;
					int fromIndex;
					byte[] dataTmp;
					FileEntry entry;

					if (IsPaused)
						Pause();

					List<FileEntry> sortedEntries = _entries.Skip(StartIndex).Take(EndIndex - StartIndex).ToList();

					int indexMax = sortedEntries.Count;

					while (toIndex < indexMax) {
						fromIndex = toIndex;

						data = _srb.ReadAligned(sortedEntries, out toIndex, fromIndex, indexMax, originalStream.Value);

						for (int i = fromIndex; i < toIndex; i++) {
							if (_grfData.IsCancelling)
								return;

							if (IsPaused)
								Pause();

							entry = sortedEntries[i];

							if ((entry.Header.IsEncrypted && (entry.Flags & EntryType.GrfEditorCrypted) == EntryType.GrfEditorCrypted)) {
								if (entry.Header.EncryptionKey != null) {
									entry.GrfEditorDecryptRequested(data);
									entry.Flags |= EntryType.Encrypt;
									entry.Modification |= Modification.Encrypt;
								}
							}

							entry.DesDecryptPrealigned(data, (int) entry.TemporaryOffset);
							entry.Cycle = -1;
							dataTmp = new byte[entry.SizeCompressedAlignment];
							Buffer.BlockCopy(data, (int) entry.TemporaryOffset, dataTmp, 0, entry.SizeCompressed);

							try {
								if ((entry.Flags & EntryType.LZSS) == EntryType.LZSS)
									dataTmp = Compression.LzssDecompress(dataTmp, entry.SizeDecompressed);
								else if ((entry.Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
									dataTmp = Compression.RawDecompress(dataTmp, entry.SizeDecompressed);
								else if (Compression.IsNormalCompression && dataTmp[0] == 0)
									dataTmp = Compression.DecompressLzma(dataTmp, entry.SizeDecompressed);
								else
									dataTmp = Compression.Decompress(dataTmp, entry.SizeDecompressed);

								entry.NewSizeDecompressed = dataTmp.Length;
								dataTmp = Compression.Compress(dataTmp);
								entry.NewSizeCompressed = dataTmp.Length;
								entry.TemporarySizeCompressedAlignment = Methods.Align(entry.NewSizeCompressed);
								_dataTmp = new byte[entry.TemporarySizeCompressedAlignment];
								Buffer.BlockCopy(dataTmp, 0, _dataTmp, 0, dataTmp.Length);

								entry.DesEncrypt(_dataTmp);
								entry.GrfEditorEncrypt(_dataTmp);
								outPutFile.Write(_dataTmp, 0, entry.TemporarySizeCompressedAlignment);
							}
							catch {
								entry.NewSizeCompressed = entry.SizeCompressed;
								entry.TemporarySizeCompressedAlignment = entry.SizeCompressedAlignment;
								entry.NewSizeDecompressed = entry.SizeDecompressed;

								dataTmp = new byte[entry.SizeCompressedAlignment];
								Buffer.BlockCopy(data, (int) entry.TemporaryOffset, dataTmp, 0, entry.SizeCompressed);
								bool compress = true;

								try {
									if ((entry.Flags & EntryType.LZSS) == EntryType.LZSS)
										dataTmp = Compression.LzssDecompress(dataTmp, entry.SizeDecompressed);
									else if ((entry.Flags & EntryType.RawDataFile) == EntryType.RawDataFile)
										dataTmp = Compression.RawDecompress(dataTmp, entry.SizeDecompressed);
									else if (dataTmp.Length > 0 && Compression.IsNormalCompression && dataTmp[0] == 0)
										dataTmp = Compression.DecompressLzma(dataTmp, entry.SizeDecompressed);
									else
										dataTmp = Compression.Decompress(dataTmp, entry.SizeDecompressed);
								}
								catch {
									try {
										dataTmp = Compression.DecompressZlib(dataTmp, entry.SizeDecompressed);
									}
									catch {
										compress = false;
									}
								}

								if (compress) {
									entry.NewSizeDecompressed = dataTmp.Length;

									try {
										dataTmp = Compression.Compress(dataTmp);
									}
									catch {
										dataTmp = Compression.CompressDotNet(dataTmp);
									}

									entry.NewSizeCompressed = dataTmp.Length;
									entry.TemporarySizeCompressedAlignment = Methods.Align(entry.NewSizeCompressed);
								}

								_dataTmp = new byte[entry.TemporarySizeCompressedAlignment];
								Buffer.BlockCopy(dataTmp, 0, _dataTmp, 0, dataTmp.Length);

								if (compress) {
									entry.DesEncrypt(_dataTmp);
								}

								entry.GrfEditorEncrypt(_dataTmp);
								outPutFile.Write(_dataTmp, 0, entry.TemporarySizeCompressedAlignment);
							}
							finally {
								entry.Flags &= ~EntryType.RawDataFile;
								entry.Flags |= EntryType.File;
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