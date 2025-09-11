using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GRF.Core;
using GRF.IO;
using GRF.GrfSystem;
using Utilities;

namespace GRF.Threading {
	/// <summary>
	/// This class creates a temporary file from the GRF added resources
	/// (ONLY for the added resources, the other files are processed normally (via direct copy))
	/// </summary>
	public class ThreadCompressSmallFiles : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 4194304;
		public int Count;

		private byte[] _dataTmp;
		private int _numberOfFilesProcessed;

		public override void Start() {
			new Thread(_start) { Name = "GRF - Extract small files thread " + StartIndex }.Start();
		}

		private void _start() {
			try {
				List<byte[]> buffers = new List<byte[]>(_bufferSize / 2048);	// Minimum capacity
				byte[] ovData;
				FileEntry entry;
				int tempOffset = 0;
				int subIndex = 0;

				// Fix : 2016-11-28
				// This path must be unique enough that it doesn't create conflicts with other
				// applications or mass savings. A single instance of GRFE won't cause any issues though.
				FileName = Path.Combine(Settings.TempPath, "~stmp" + StartIndex + "_" + _grfData.UniqueString);

				int bytesRead;
				byte[] buffer;

				// Sadly, C# is slow when it comes to opening streams, so... we actually merge all small files into
				// one big (max size is 40 000 * 2048 bytes) to boost the performance (by a LOT, we're talking from minutes to seconds).
				using (FileStream outPutFile = new FileStream(Path.Combine(Settings.TempPath, "out" + StartIndex + ".bin"), FileMode.Create))
				using (FileStream file = new FileStream(FileName, FileMode.Create)) {
					for (int i = StartIndex; i < EndIndex; i++) {
						if (_grfData.IsCancelling)
							return;

						entry = _entries[i];
						buffer = new byte[entry.NewSizeDecompressed];
						if (entry.SourceFilePath == GrfStrings.DataStreamId) {
							outPutFile.Write(entry.RawDataSource.Data, 0, entry.RawDataSource.Data.Length);
						}
						else {
							using (FileStream inputTempFile = new FileStream(entry.SourceFilePath, FileMode.Open, FileAccess.Read)) {
								while ((bytesRead = inputTempFile.Read(buffer, 0, entry.NewSizeDecompressed)) > 0)
									outPutFile.Write(buffer, 0, bytesRead);
							}
						}
						_numberOfFilesProcessed++;
						NumberOfFilesProcessed = _numberOfFilesProcessed / 2;
					}

					outPutFile.Seek(0, SeekOrigin.Begin);

					for (int i = StartIndex; i < EndIndex; i++) {
						if (_grfData.IsCancelling)
							return;

						entry = _entries[i];
						_dataTmp = new byte[entry.NewSizeDecompressed];
						outPutFile.Read(_dataTmp, 0, _dataTmp.Length);
						_dataTmp = Compression.Compress(new MemoryStream(_dataTmp));
						entry.NewSizeCompressed = _dataTmp.Length;
						entry.TemporarySizeCompressedAlignment = Methods.Align(entry.NewSizeCompressed);

						// Fix : 2015-07-01
						// The size alignment of DES encrypted content must be kept at their
						// size aligned.
						if (entry.HasToDesEncrypt()) {
							if (_dataTmp.Length != entry.TemporarySizeCompressedAlignment) {
								byte[] dataTmp = new byte[entry.TemporarySizeCompressedAlignment];
								Buffer.BlockCopy(_dataTmp, 0, dataTmp, 0, _dataTmp.Length);
								_dataTmp = dataTmp;
							}

							entry.DesEncrypt(_dataTmp);
						}

						entry.GrfEditorEncrypt(_dataTmp);

						buffers.Add(_dataTmp);

						entry.TemporaryOffset = (uint) tempOffset;
						tempOffset += entry.TemporarySizeCompressedAlignment;

						if (tempOffset > _bufferSize) {
							ovData = new byte[tempOffset];

							for (int j = 0; j < buffers.Count; j++) {
								Buffer.BlockCopy(buffers[j], 0, ovData, (int) _entries[StartIndex + j + subIndex].TemporaryOffset, buffers[j].Length);
							}

							file.Write(ovData, 0, ovData.Length);
							subIndex += buffers.Count;
							buffers.Clear();
							tempOffset = 0;
						}

						_numberOfFilesProcessed++;
						NumberOfFilesProcessed = _numberOfFilesProcessed / 2;
					}

					if (tempOffset != 0) {
						ovData = new byte[tempOffset];

						for (int j = 0; j < buffers.Count; j++) {
							if (_grfData.IsCancelling)
								return;

							Buffer.BlockCopy(buffers[j], 0, ovData, (int) _entries[StartIndex + j + subIndex].TemporaryOffset, buffers[j].Length);
						}

						file.Write(ovData, 0, ovData.Length);
						buffers.Clear();
					}

					Count = (int)file.Length;
				}
			}
			catch (Exception err) {
				Exception = err;
				Error = true;
			}
			finally {
				GrfPath.Delete(Path.Combine(Settings.TempPath, "out" + StartIndex + ".bin"));
				Terminated = true;
			}
		}
	}
}
