using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.IO;
using GRF.Threading;
using Utilities;
using GRF.Core.GrfWriters;
using GRF.GrfSystem;

namespace GRF.Core {
	internal static class GrfWriter {
		internal const int BufferSize = 1 << 25; // Roughly 32MB in memory

		public static void WriteRepack(Container grf, Stream originalStream, Stream outputStream) {
			// Setup progress
			var tieredProgress = new TieredProgress(grf);
			tieredProgress.SetSpecialState(TieredProgress.SpecialPending);

			outputStream.Seek(GrfHeader.DataByteSize, SeekOrigin.Begin);

			// Retrieve entries
			List<FileEntry> sortedEntries = grf.Table.Entries.OrderBy(p => p.FileExactOffset).ToList();

			// Calculate progress
			tieredProgress.AddWeightedTier(sortedEntries.Count);

			// Apply operations
			if (sortedEntries.Count > 0) {
				var pool = new GrfThreadPool<FileEntry>();

				try {
					pool.Initialize<ThreadRepack>(grf, sortedEntries);
					pool.Start(v => tieredProgress.SetTierProgress(v / 100.0f), () => grf.IsCancelling);
					pool.Dump(grf.Header, outputStream);
				}
				catch {
					foreach (var thread in pool.Threads.OfType<ThreadRepack>()) {
						GrfPath.Delete(thread.FileName);
					}

					throw;
				}
			}

			tieredProgress.CompleteTier();
		}

		public static void WriteCompact(Container grf, Stream originalStream, Stream outputStream) {
			// Setup progress
			var tieredProgress = new TieredProgress(grf);
			tieredProgress.SetSpecialState(TieredProgress.SpecialIndexingContent);

			// This is done is three steps:
			// 1: Delete "duplicate" entries
			// 2: Copy GRF normally
			// 3: Restore deleted "duplicate" entries and fix their offsets

			// Step 1
			tieredProgress.AddWeightedTier(grf.Table.Entries.Count);
			WriterHelper.CompactHashAndDelete(tieredProgress, grf, originalStream, grf.Table.Entries.OrderBy(p => p.FileExactOffset).ToList(), out var deletedEntries, out var redirectedEntries);

			try {
				// Step 2
				WriteData(grf, originalStream, outputStream);

				// Step 3
				WriterHelper.CompactRedirectHashedOffsets(grf, deletedEntries, redirectedEntries);
			}
			catch {
				// Revert deleted entries!
				foreach (var entry in deletedEntries)
					grf.Table.AddEntry(entry);

				throw;
			}
		}

		/// <summary>
		/// This method only modifies the original data stream rather than creating a new file.
		/// </summary>
		/// <param name="grf">The GRF.</param>
		/// <param name="originalStream">The original stream, opened with write access.</param>
		/// <param name="grfAdd"> </param>
		public static long WriteDataQuick(Container grf, Stream originalStream, Container grfAdd = null) {
			// Setup progress
			var tieredProgress = new TieredProgress(grf);
			tieredProgress.SetSpecialState(TieredProgress.SpecialPending);

			FileTable table = grf.InternalTable;
			grf.Close();

			if (grfAdd != null) {
				if (grfAdd.InternalHeader.IsEncrypted) {
					WriterHelper.RestoreEncryptionHashFile(grfAdd);
				}

				WriterHelper.ApplyDeleteFiles(table, grfAdd);
			}

			// Retrieve entries
			_getEntries(grf, out _, out var continuousEntries, out var addedEntries, out var mergeEntries, out var encryptedEntries);

			QuickMergeHelper helper = new QuickMergeHelper(grf, continuousEntries);

			long offset = WriterHelper.EnsureStreamSize(originalStream, continuousEntries);
			long grfAddTotalSize = mergeEntries.Sum(p => (long)p.SizeCompressedAlignment);

			if (helper.ShouldRepackInstead(mergeEntries, addedEntries) || (grf.Header.Version < 3.0 && grfAddTotalSize + grf.InternalHeader.FileTableOffset > uint.MaxValue)) {
				grf.IsBusy = false;
				originalStream.Close();
				grf.Reader.SetStream(grf.GetSharedStream());
				grf.Save(null, grfAdd, SavingMode.FileCopy, SyncMode.Synchronous);
				throw GrfExceptions.__RepackInstead.Create();
			}

			// Prepare the stream length to make the copy faster; even if the file is larger it will get resized correctly afterwards.
			if (originalStream.Length < offset + grfAddTotalSize)
				originalStream.SetLength(offset + grfAddTotalSize);

			// Calculate progress
			tieredProgress.AddWeightedTier(addedEntries.Count);
			tieredProgress.AddWeightedTier(mergeEntries.Count);
			tieredProgress.AddWeightedTier(encryptedEntries.Count);

			// Apply operations
			_newFilesCopy(tieredProgress, addedEntries, grf, originalStream, ref offset, grfAddTotalSize);
			_continousCopy(tieredProgress, mergeEntries, grf, grfAdd, null, originalStream, ref offset, canCancel: false, helper);
			_encryptEntries(tieredProgress, encryptedEntries, grf, originalStream, ref offset);
			return offset;
		}

		public static void WriteData(Container grf, Stream originalStream, Stream outputStream, Container grfAdd = null) {
			// Setup progress
			var tieredProgress = new TieredProgress(grf);
			tieredProgress.SetSpecialState(TieredProgress.SpecialPending);

			// Retrieve entries
			_getEntries(grf, out _, out var continuousEntries, out var addedEntries, out var mergeEntries, out _);

			// Calculate progress
			tieredProgress.AddWeightedTier(continuousEntries.Count);
			tieredProgress.AddWeightedTier(addedEntries.Count);
			tieredProgress.AddWeightedTier(mergeEntries.Count);

			long offset = GrfHeader.DataByteSize;

			// Apply operations
			_continousCopy(tieredProgress, continuousEntries, grf, grf, originalStream, outputStream, ref offset, canCancel: true);
			_newFilesCopy(tieredProgress, addedEntries, grf, outputStream, ref offset);
			_continousCopy(tieredProgress, mergeEntries, grf, grfAdd, null, outputStream, ref offset, canCancel: true);
		}

		private static void _encryptEntries(TieredProgress tieredProgress, List<FileEntry> entries, Container grf, Stream originalStream, ref long offset) {
			byte[] data;

			for (int i = 0; i < entries.Count; i++) {
				FileEntry entry = entries[i];

				try {
					originalStream.Position = entry.FileExactOffset;
					data = new byte[entry.SizeCompressedAlignment];
					originalStream.Read(data, 0, entry.SizeCompressedAlignment);

					entry.GrfEditorEncrypt(data);
					entry.GrfEditorDecrypt(data, 0);

					originalStream.Position = entry.FileExactOffset;
					originalStream.Write(data, 0, entry.SizeCompressedAlignment);
				}
				catch {
					// ??
					// Not sure what would happen to trigger this, but the stream cannot be canceled mid write for quick merge
				}

				tieredProgress.SetTierProgress(i + 1);
			}

			tieredProgress.CompleteTier();
		}

		private static void _getEntries(Container grf, out List<FileEntry> sortedEntries, out List<FileEntry> continuousEntries, out List<FileEntry> addedEntries, out List<FileEntry> mergeEntries, out List<FileEntry> encryptedEntries) {
			// Encryption file must be set or removed before looking up for the entries
			WriterHelper.EncryptionCheck(grf);

			sortedEntries = grf.Table.Entries.OrderBy(p => p.FileExactOffset).ToList();
			continuousEntries = new List<FileEntry>();
			addedEntries = new List<FileEntry>();
			mergeEntries = new List<FileEntry>();
			encryptedEntries = new List<FileEntry>();
			bool enableEncryption = grf.InternalHeader.EncryptionKey != null;

			foreach (var entry in sortedEntries) {
				if (entry.Modification.HasFlag(Modification.GrfMerge)) {
					mergeEntries.Add(entry);
				}
				else if (entry.Modification.HasFlag(Modification.Added)) {
					addedEntries.Add(entry);
				}
				else {
					if (enableEncryption && (entry.Modification.HasFlag(Modification.Encrypt) || entry.Modification.HasFlag(Modification.Decrypt)))
						encryptedEntries.Add(entry);

					continuousEntries.Add(entry);
				}
			}
		}

		private static void _newFilesCopy(TieredProgress tieredProgress, List<FileEntry> entries, Container grf, Stream outputStream, ref long currentOffset, long grfAddTotalSize = 0) {
			outputStream.Seek(currentOffset, SeekOrigin.Begin);

			if (entries.Count > 0) {
				int numberOfFilesToAdd = entries.Count;

				foreach (FileEntry fileEntry in entries) {
					fileEntry.NewSizeDecompressed = fileEntry.GetSizeDecompressed();
				}

				Random rnd = new Random();
				List<FileEntry> smallEntries = entries.Where(p => p.NewSizeDecompressed <= 2048).ToList();
				entries = entries.Where(p => p.NewSizeDecompressed > 2048).OrderBy(p => rnd.Next()).ToList(); // Better spread

				GrfThreadPool<FileEntry> pool = new GrfThreadPool<FileEntry>();
				pool.Initialize<ThreadCompressFiles>(grf, entries);
				pool.Initialize<ThreadCompressSmallFiles>(grf, smallEntries, smallEntries.Count == 0 ? 0 : (smallEntries.Count / 50000) + 1);
				pool.Start(v => tieredProgress.SetTierProgress(v / 100.0f), () => grf.IsCancelling);
				currentOffset = pool.Dump(grf.Header, outputStream, currentOffset, grfAddTotalSize);
			}

			tieredProgress.CompleteTier();
		}

		public class BufferedStreamWriter {
			private byte[] _buffer;
			private int _bufferSize;
			private Stream _output;
			private int _bufferReadLength;
			public int WriteCalls;

			public BufferedStreamWriter(int bufferSize, Stream output) {
				//bufferSize = bufferSize << 1;
				_buffer = new byte[bufferSize];
				_bufferSize = bufferSize;
				_output = output;
			}

			public void Add(byte[] data, int length) {
				if (length > _bufferSize) {
					Flush();
					_output.Write(data, 0, length);
				}

				if (length + _bufferReadLength > _bufferSize) {
					Flush();
				}

				Buffer.BlockCopy(data, 0, _buffer, _bufferReadLength, length);
				_bufferReadLength += length;
			}

			public void Flush() {
				if (_bufferReadLength > 0) {
					_output.Write(_buffer, 0, _bufferReadLength);
					WriteCalls++;
				}

				_bufferReadLength = 0;
			}
		}

		private static void _continousCopy(TieredProgress tieredProgress, List<FileEntry> entries, Container grfDst, Container grfSrc, Stream originalStream, Stream outputStream, ref long currentOffset, bool canCancel, QuickMergeHelper helper = null) {
			if (grfSrc == null) {
				tieredProgress.CompleteTier();
				return;
			}

			if (grfSrc.InternalHeader.IsEncrypted && grfDst.Header.Version < 2.0)
				throw GrfExceptions.__MergeVersionEncryptionException.Create();

			outputStream.Seek(currentOffset, SeekOrigin.Begin);

			byte[] data;
			int toIndex = 0;
			FileEntry entry;
			int fromIndex;
			int indexMax = entries.Count;
			StreamReadBlockInfo srb = new StreamReadBlockInfo(BufferSize);
			bool desEncrypt = grfDst.Header.Version < 2.0;

			if (grfDst.Header.Version < 2.0) {
				foreach (var sEntry in entries) {
					sEntry.TemporarySizeCompressedAlignment = Methods.Align(sEntry.TemporarySizeCompressedAlignment);
				}
			}

			DisposableScope<FileStream> sourceStream = null;

			try {
				// If no stream set, retrieve it from the grfSrc directly
				if (originalStream == null) {
					sourceStream = grfSrc.GetSourceStream();
					originalStream = sourceStream.Value;
				}

				BufferedStreamWriter bsw = null;
				
				if (helper == null)
					bsw = new BufferedStreamWriter(BufferSize, outputStream);

				while (toIndex < indexMax) {
					fromIndex = toIndex;
					data = srb.ReadAligned(entries, out toIndex, fromIndex, indexMax, originalStream, out int dataLength);

					for (int i = fromIndex; i < toIndex; i++) {
						if (canCancel)
							AProgress.IsCancelling(grfDst);

						entry = entries[i];

						if (desEncrypt)
							entry.DesEncryptPrealigned(data, (int)entry.TemporaryOffset, false);
						else
							entry.DesDecryptPrealigned(data, (int)entry.TemporaryOffset);

						// Won't apply the encryption/decryption if the flags aren't set.
						entry.GrfEditorEncrypt(data, (int)entry.TemporaryOffset);
						entry.GrfEditorDecrypt(data, (int)entry.TemporaryOffset);

						if (helper != null) {
							helper.Write(outputStream, ref currentOffset, data, (int)entry.TemporaryOffset, entry.TemporarySizeCompressedAlignment, entry);
						}
						else {
							entry.TemporaryOffset = currentOffset;
							currentOffset += entry.TemporarySizeCompressedAlignment;
						}

						tieredProgress.SetTierProgress(i + 1);
					}

					bsw?.Add(data, dataLength);
				}

				bsw?.Flush();
				tieredProgress.CompleteTier();
			}
			finally {
				sourceStream?.Dispose();
			}
		}
	}
}