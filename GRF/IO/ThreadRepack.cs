using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GRF.Core;
using GRF.GrfSystem;
using GRF.Threading;
using Utilities;

namespace GRF.IO {
	/// <summary>
	/// This class extracts and copy the files from the GRF from a given range
	/// It's used to optimize the data transfer.
	/// </summary>
	public class ThreadRepack : GrfWriterThread<FileEntry> {
		private readonly StreamReadBlockInfo _srb = new StreamReadBlockInfo(GrfWriter.BufferSize);
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

				using (var outputFileStream = new FileStream(FileName, FileMode.Create)) {
					bool requiresDispose = true;
					Stream originalStream = null;
					bool canCancel = true;

					if (ThreadPool != null && ThreadPool.SourceStream != null) {
						originalStream = ThreadPool.SourceStream;
						requiresDispose = false;
						
						// Cannot cancel on the if the source GRF stream is used.
						canCancel = false;
					}
					else {
						originalStream = _grfData.GetSourceStream().Value;
					}

					try {
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

							data = _srb.ReadMisaligned(sortedEntries, out toIndex, fromIndex, indexMax, originalStream, out _);

							for (int i = fromIndex; i < toIndex; i++) {
								if (canCancel && _grfData.IsCancelling)
									return;

								if (IsPaused)
									Pause();

								entry = sortedEntries[i];

								if ((entry.Header.IsEncrypted && (entry.Flags & EntryType.GrfEditorCrypted) == EntryType.GrfEditorCrypted)) {
									if (entry.Header.EncryptionKey != null) {
										entry.GrfEditorDecryptRequested(data);
										entry.Modification |= Modification.Encrypt;
									}
								}

								entry.DesDecryptPrealigned(data, (int)entry.TemporaryOffset);
								entry.Cycle = -1;
								dataTmp = new byte[entry.SizeCompressedAlignment];
								Buffer.BlockCopy(data, (int)entry.TemporaryOffset, dataTmp, 0, entry.SizeCompressed);

								bool compress = true;

								try {
									if (entry.GravityEncrypted)
										dataTmp = AttemptProtectedGrfDecode(entry, dataTmp, ref compress);
									else
										dataTmp = entry.GetDecompressedDataFromBuffer(dataTmp);

									entry.NewSizeDecompressed = compress ? dataTmp.Length : entry.SizeDecompressed;

									if (compress) {
										dataTmp = Compression.Compress(dataTmp);
										entry.NewSizeCompressed = dataTmp.Length;
										entry.TemporarySizeCompressedAlignment = Methods.Align(entry.NewSizeCompressed);

										_dataTmp = new byte[entry.TemporarySizeCompressedAlignment];
										Buffer.BlockCopy(dataTmp, 0, _dataTmp, 0, dataTmp.Length);

										entry.DesEncrypt(_dataTmp);
										entry.GrfEditorEncrypt(_dataTmp);
									}
									else {
										entry.NewSizeCompressed = entry.SizeCompressed;
										entry.TemporarySizeCompressedAlignment = Methods.Align(entry.NewSizeCompressed);

										_dataTmp = new byte[entry.TemporarySizeCompressedAlignment];
										Buffer.BlockCopy(dataTmp, 0, _dataTmp, 0, entry.SizeCompressed);
									}

									outputFileStream.Write(_dataTmp, 0, entry.TemporarySizeCompressedAlignment);
								}
								catch {
									entry.NewSizeCompressed = entry.SizeCompressed;
									entry.TemporarySizeCompressedAlignment = entry.SizeCompressedAlignment;
									entry.NewSizeDecompressed = entry.SizeDecompressed;

									dataTmp = new byte[entry.SizeCompressedAlignment];
									Buffer.BlockCopy(data, (int)entry.TemporaryOffset, dataTmp, 0, entry.SizeCompressed);

									try {
										if (entry.GravityEncrypted)
											dataTmp = AttemptProtectedGrfDecode(entry, dataTmp, ref compress);
										else
											dataTmp = entry.GetDecompressedDataFromBuffer(dataTmp);
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
											dataTmp = Compression.CompressZlibDotNet(dataTmp);
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
									outputFileStream.Write(_dataTmp, 0, entry.TemporarySizeCompressedAlignment);
								}
								finally {
									if (!entry.GravityEncrypted) {
										entry.Flags &= ~EntryType.RawDataFile;
										entry.Flags |= EntryType.File;
									}
								}

								NumberOfFilesProcessed++;
							}
						}
					}
					finally {
						if (requiresDispose)
							originalStream?.Dispose();
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

		private byte[] AttemptProtectedGrfDecode(FileEntry entry, byte[] dataTmp, ref bool compress) {
			try {
				var data = entry.DecodeProtectedData(dataTmp);
				entry.Flags &= ~EntryType.GravityEncryptedFile;
				return data;
			}
			catch {
				compress = false;
			}

			return dataTmp;
		}
	}
}
