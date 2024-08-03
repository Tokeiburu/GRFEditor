using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GRF.Core;
using GRF.System;

namespace GRF.Threading {
	/// <summary>
	/// This class creates a temporary file from the GRF added resources
	/// (ONLY for the added resources, the other files are processed normally (via direct copy))
	/// </summary>
	public class ThreadCompressFiles : GrfWriterThread<FileEntry> {
		private const int _bufferSize = 4194304;
		private byte[] _dataTmp;

		public override void Start() {
			new Thread(_start) {Name = "GRF - Compressing thread " + StartIndex}.Start();
		}

		private void _start() {
			try {
				List<byte[]> buffers = new List<byte[]>(500);
				byte[] ovData;
				FileEntry entry;
				int tempOffset = 0;
				int subIndex = 0;
				// Fix : 2016-11-28
				// This path must be unique enough that it doesn't create conflicts with other
				// applications or mass savings. A single instance of GRFE won't cause any issues though.
				FileName = Path.Combine(Settings.TempPath, "~tmp" + StartIndex + "_" + _grfData.UniqueString);

				// Sadly, C# is slow when it comes to opening streams, so... we actually merge all files of a directory into one file
				// first to boost the performance (by a LOT, we're talking about minutes to seconds).
				using (FileStream file = new FileStream(FileName, FileMode.Create)) {
					for (int i = StartIndex; i < EndIndex; i++) {
						if (IsPaused) {
							Pause();
						}

						if (_grfData.IsCancelling)
							return;

						entry = _entries[i];
						_dataTmp = entry.NewCompressedData;

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

						NumberOfFilesProcessed++;
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

		public void SyncStart() {
			_start();
		}
	}
}