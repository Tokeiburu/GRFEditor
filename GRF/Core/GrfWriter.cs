using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GRF.ContainerFormat;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;
using Utilities.Tools;

namespace GRF.Core {
	internal static class GrfWriter {
		internal const int BufferSize = 8388608;

		private static Container _grf;

		public static float Progress {
			get { return _grf.Progress < 0 ? 0 : _grf.Progress; }
			set { _grf.Progress = value >= 99.99f ? 99.99f : value; }
		}

		public static void WriteDataRepack(Container grf, Stream originalStream, Stream grfStream) {
			_grf = grf;
			Progress = -1;

			grfStream.Seek(GrfHeader.StructSize, SeekOrigin.Begin);

			List<FileEntry> sortedEntries = grf.Table.Entries.OrderBy(p => p.FileExactOffset).ToList();

			if (sortedEntries.Count > 0) {
				GrfThreadPool<FileEntry> pool = new GrfThreadPool<FileEntry>();
				pool.Initialize<ThreadRepack>(grf, sortedEntries);
				pool.Start(v => Progress = v, () => grf.IsCancelling);
				pool.Dump(grfStream);
			}
		}

		public static long WriteCompact(Container grf, Stream originalStream, Stream grfStream) {
			_grf = grf;
			Progress = -5;
			Dictionary<string, FileEntry> hashedEntries = new Dictionary<string, FileEntry>();
			Md5Hash hash = new Md5Hash();
			List<FileEntry> deletedEntries = new List<FileEntry>();

			int i = 0;
			var entries = grf.Table.Entries;

			_encryption(grf);

			foreach (var entry in entries) {
				string key = entry.GetDataHashFromCompressed(hash);

				if (hashedEntries.ContainsKey(key)) {
					grf.Table.DeleteEntry(entry.RelativePath);
					deletedEntries.Add(entry);
				}
				else {
					hashedEntries[key] = entry;
				}
				i++;
				Progress = (float) i / entries.Count * 100f;
			}

			grf.Table.InvalidateInternalSets();

			int numberOfFilesToCopy = grf.Table.Entries.Count;
			long offset = _continousCopy(grf, numberOfFilesToCopy, originalStream, grfStream, GrfHeader.StructSize);
			_newFilesCopy(grf, numberOfFilesToCopy, grfStream, offset);

			foreach (var entry in deletedEntries) {
				string key = entry.GetDataHashFromCompressed(hash);
				var dEntry = hashedEntries[key];

				entry.FileExactOffset = dEntry.FileExactOffset;
				entry.Offset = dEntry.Offset;
				entry.TemporaryOffset = dEntry.TemporaryOffset;

				grf.Table.AddEntry(entry);
			}

			return offset;
		}

		/// <summary>
		/// This method only rewrites the file table without modifying any of the content.
		/// </summary>
		/// <param name="grf">The GRF.</param>
		/// <param name="originalStream">The original stream, opened with write access.</param>
		/// <param name="grfAdd"> </param>
		public static long WriteDataQuick(Container grf, Stream originalStream, Container grfAdd = null) {
			FileTable table = grf.InternalTable;
			grf.Close();
			_grf = grf;
			Progress = -1;

			if (grfAdd != null) {
				List<string> entriesToDelete = grfAdd.Table.Entries.Where(p => (p.Flags & EntryType.RemoveFile) == EntryType.RemoveFile).Select(p => p.RelativePath).ToList();

				if (grfAdd.InternalHeader.IsEncrypted) {
					grfAdd.Table.UndoDeleteFile(GrfStrings.EncryptionFilename);
					grfAdd.Table.InvalidateInternalSets();
				}

				foreach (string file in entriesToDelete) {
					grfAdd.Table.DeleteEntry(file);

					Regex regex = new Regex(Methods.WildcardToRegex(file), RegexOptions.IgnoreCase);

					foreach (string match in table.Files.Where(p => regex.IsMatch(p))) {
						table.DeleteFile(match);
					}
				}

				foreach (var entry in grfAdd.Table.Entries) {
					table.DeleteFile(entry.RelativePath);
				}

				table.InvalidateInternalSets();
			}

			List<FileEntry> sortedEntries = table.Entries.OrderBy(p => p.FileExactOffset).ToList();
			QuickMergeHelper helper = new QuickMergeHelper(grf);
			long endStreamOffset = sortedEntries.Count > 0 ? sortedEntries.Last().FileExactOffset + (uint) sortedEntries.Last().TemporarySizeCompressedAlignment : GrfHeader.StructSize;
			byte[] data;

			if (endStreamOffset < GrfHeader.StructSize) {
				// This will happen for new GRFs
				endStreamOffset = GrfHeader.StructSize;

				if (originalStream.Length < endStreamOffset) {
					originalStream.SetLength(endStreamOffset);
				}
			}

			List<FileEntry> entriesGrfAdd = new List<FileEntry>();
			long grfAddTotalSize = 0;

			if (grfAdd != null) {
				entriesGrfAdd = grfAdd.Table.Entries.OrderBy(p => p.FileExactOffset).Select(p => new FileEntry(p)).ToList(); // Copy
				grfAddTotalSize = entriesGrfAdd.Sum(p => (long)p.SizeCompressedAlignment);
			}

			List<FileEntry> entriesAdded = grf.Table.Entries.Where(p => (p.Modification & Modification.Added) == Modification.Added).ToList();

			// Fix : 2017-03-15
			// We have to check if the current entries from 0x200 are DES encrypted,
			// otherwise the file table writer will create issues on these entries.
			bool shouldRepack = false;

			if (grf.Header.IsCompatibleWith(2, 0)) {
				foreach (var entry in grf.Table.Entries) {
					if (!entry.Added && !entry.Removed) {
						if (entry.Cycle > -1) {
							shouldRepack = true;
							break;
						}
					}
				}
			}

			if (shouldRepack || helper.ShouldRepackInstead(entriesGrfAdd, entriesAdded) || grfAddTotalSize + grf.InternalHeader.FileTableOffset > uint.MaxValue) {
				grf.IsBusy = false;
				originalStream.Close();
				grf.Reader.SetStream(grf.GetSharedStream());

				grf.Save(null, grfAdd, SavingMode.GrfSave, SyncMode.Synchronous);
				throw GrfExceptions.__RepackInstead.Create();
			}

			int totalEntriesCount = entriesGrfAdd.Count + entriesAdded.Count;

			if (entriesAdded.Count > 0) {
				originalStream.Seek(endStreamOffset, SeekOrigin.Begin);
				endStreamOffset = _newFilesCopy(grf, totalEntriesCount, originalStream, endStreamOffset, grfAddTotalSize);
			}

			float currentProgress = Progress;

			if (grfAdd != null) {
				int mode =
					grf.Header.IsMajorVersion(1) && grfAdd.Header.Is(2, 0) ? 0 :
					grf.Header.Is(2, 0) && grfAdd.Header.IsMajorVersion(1) ? 1 :
					grf.Header.IsMajorVersion(1) && grfAdd.Header.IsMajorVersion(1) ? 2 : 3;

				for (int index = 0; index < entriesGrfAdd.Count; index++) {
					FileEntry entry = entriesGrfAdd[index];

					if (mode == 0) {
						data = grfAdd.GetStreamRawData(entry);
						entry.DesEncrypt(data, false);
					}
					else if (mode == 1) {
						data = grfAdd.GetRawData(entry);
						entry.Cycle = -1;
					}
					else if (mode == 2) {
						// Fix : 2015-04-06
						// The 0x100 merged with a 0x100 wasn't being handled.
						data = grfAdd.GetStreamRawData(entry);
						entry.DesEncrypt(data, false);
					}
					else {
						data = grfAdd.GetStreamRawData(entry);

						// Fix : 2015-01-17
						// Some GRFs with the 0x200 version has DES encrypted
						// data, which is by itself invalid. The following line
						// fixes that issue.
						entry.DesDecrypt(data, 0);
					}

					entry.GrfEditorEncrypt(data);
					entry.GrfEditorDecrypt(data, 0);

					helper.Write(originalStream, ref endStreamOffset, data, entry);
					table.AddEntry(entry);
					entry.SetStream(null); // Setting the stream to null will forcibly reassign the value later

					Progress = (index + 1f) / totalEntriesCount * 100f + currentProgress;
					//Progress = (index + 1f) / totalEntriesCount * 100f;
				}
			}

			return endStreamOffset;
		}

		private static void _encryption(Container grf) {
			if (grf.InternalHeader.IsEncrypting || grf.InternalHeader.IsEncrypted) {
				byte[] data;

				if (grf.InternalHeader.EncryptionKey != null)
					data = BitConverter.GetBytes(Crc32.Compute(grf.InternalHeader.EncryptionKey));
				else {
					grf.Table[GrfStrings.EncryptionFilename].BypassSaveCheck = true;
					data = grf.GetDecompressedData(grf.Table[GrfStrings.EncryptionFilename]);
				}

				using (BinaryWriter stream = new BinaryWriter(File.Create(Path.Combine(Settings.TempPath, GrfStrings.EncryptionFilename)))) {
					stream.Write(data);
				}

				grf.Table.Add(GrfPath.Combine(grf.Table.Entries.Any(p => p.RelativePath.StartsWith(GrfStrings.RgzRoot)) ? GrfStrings.RgzRoot : "", GrfStrings.EncryptionFilename), Path.Combine(Settings.TempPath, GrfStrings.EncryptionFilename), true);
			}

			if (grf.InternalHeader.IsDecrypting || (!grf.InternalHeader.IsEncrypting && !grf.InternalHeader.IsEncrypted)) {
				if (grf.Table.Contains(GrfStrings.EncryptionFilename))
					grf.Table.DeleteFile(GrfStrings.EncryptionFilename);
			}
		}

		public static void WriteData(Container grf, Stream originalStream, Stream grfStream, Container grfAdd = null) {
			_grf = grf;
			Progress = -1;
			int numberOfFilesToCopy = grf.Table.Entries.Count;
			long currentOffset;

			_encryption(grf);
			currentOffset = _continousCopy(grf, numberOfFilesToCopy, originalStream, grfStream, GrfHeader.StructSize);
			currentOffset = _newFilesCopy(grf, numberOfFilesToCopy, grfStream, currentOffset);
			_mergeGrf(grf, numberOfFilesToCopy, grfAdd, grfStream, currentOffset);
		}

		private static long _mergeGrf(Container grf, int numberOfFilesToCopy, Container grfAdd, Stream grfStream, long currentOffset) {
			// ** The grfAdd is only used to retrieve the actual file data, the output location
			// should be in the current grf file table (with the GrfSource) attribute.
			List<FileEntry> sortedEntries = grf.Table.Entries.Where(p => p.Modification.HasFlags(Modification.GrfMerge)).OrderBy(p => p.FileExactOffset).ToList();

			if (grfAdd != null) {
				if (grfAdd.InternalHeader.IsEncrypted && grf.Header.IsMajorVersion(1))
					throw GrfExceptions.__MergeVersionEncryptionException.Create();

				float currentProgress = Progress;
				int toIndex = 0;
				int fromIndex;
				int indexMax = sortedEntries.Count;
				byte[] data;
				FileEntry entry;
				StreamReadBlockInfo srb = new StreamReadBlockInfo(BufferSize);

				using (var streamAdd = grfAdd.GetSourceStream()) {
					while (toIndex < indexMax) {
						fromIndex = toIndex;
						data = srb.ReadAligned(sortedEntries, out toIndex, fromIndex, indexMax, streamAdd.Value);

						for (int i = fromIndex; i < toIndex; i++) {
							AProgress.IsCancelling(grf);
							entry = sortedEntries[i];

							if (entry.Cycle >= 0 && grf.Header.IsCompatibleWith(2, 0))
								entry.DesDecryptPrealigned(data, (int) entry.TemporaryOffset);
							else if (grf.Header.IsMajorVersion(1) && grfAdd.Header.IsCompatibleWith(2, 0))
								entry.DesEncryptPrealigned(data, (int) entry.TemporaryOffset, false);

							entry.TemporaryOffset = (uint)currentOffset;
							currentOffset += (uint) entry.TemporarySizeCompressedAlignment;
						}

						grfStream.Write(data, 0, data.Length);
						Progress = toIndex / (float) numberOfFilesToCopy * 100.0f + currentProgress;
						AProgress.IsCancelling(grf);
					}
				}
			}

			return currentOffset;
		}

		private static long _newFilesCopy(Container grf, int numberOfFilesToCopy, Stream grfStream, long currentOffset, long grfAddTotalSize = 0) {
			List<FileEntry> sortedEntries = grf.Table.Entries.Where(p => p.Modification.HasFlags(Modification.Added)).ToList();

			if (sortedEntries.Count > 0) {
				int numberOfFilesToAdd = sortedEntries.Count;
				float currentProgress = Progress;

				foreach (FileEntry fileEntry in sortedEntries) {
					fileEntry.NewSizeDecompressed = fileEntry.GetSizeDecompressed();
				}

				Random rnd = new Random();
				List<FileEntry> smallEntries = sortedEntries.Where(p => p.NewSizeDecompressed <= 2048).ToList();
				sortedEntries = sortedEntries.Where(p => p.NewSizeDecompressed > 2048).OrderBy(p => rnd.Next()).ToList(); // Better spread

				GrfThreadPool<FileEntry> pool = new GrfThreadPool<FileEntry>();
				pool.Initialize<ThreadCompressFiles>(grf, sortedEntries);
				pool.Initialize<ThreadCompressSmallFiles>(grf, smallEntries, smallEntries.Count == 0 ? 0 : (smallEntries.Count / 50000) + 1);
				pool.Start(v => Progress = (v * numberOfFilesToAdd) / (float) numberOfFilesToCopy + currentProgress, () => grf.IsCancelling);
				currentOffset = pool.Dump(grfStream, currentOffset, grfAddTotalSize);
			}

			return currentOffset;
		}

		private static long _continousCopy(Container grf, int numberOfFilesToCopy, Stream originalStream, Stream grfStream, long currentOffset) {
			List<FileEntry> sortedEntries = grf.Table.Entries.Where(p => !p.Modification.HasFlags(Modification.Added) && !p.Modification.HasFlags(Modification.GrfMerge)).OrderBy(p => p.FileExactOffset).ToList();
			grfStream.Seek(GrfHeader.StructSize, SeekOrigin.Begin);

			byte[] data;
			int toIndex = 0;
			FileEntry entry;
			int fromIndex;
			int indexMax = sortedEntries.Count;
			StreamReadBlockInfo srb = new StreamReadBlockInfo(BufferSize);

			if (grf.Header.IsMajorVersion(1)) {
				foreach (var sEntry in sortedEntries) {
					sEntry.TemporarySizeCompressedAlignment = Methods.Align(sEntry.TemporarySizeCompressedAlignment);
				}
			}

			while (toIndex < indexMax) {
				fromIndex = toIndex;
				data = srb.ReadAligned(sortedEntries, out toIndex, fromIndex, indexMax, originalStream);

				for (int i = fromIndex; i < toIndex; i++) {
					AProgress.IsCancelling(grf);
					entry = sortedEntries[i];

					if (grf.Header.IsMajorVersion(1))
						entry.DesEncryptPrealigned(data, (int) entry.TemporaryOffset);
					else if (grf.Header.IsCompatibleWith(2, 0))
						entry.DesDecryptPrealigned(data, (int) entry.TemporaryOffset);

					// Won't apply the encryption/decryption if the flags aren't set.
					entry.GrfEditorEncrypt(data, (int) entry.TemporaryOffset);
					entry.GrfEditorDecrypt(data, (int) entry.TemporaryOffset);

					entry.TemporaryOffset = (uint) currentOffset;
					currentOffset += entry.TemporarySizeCompressedAlignment;
				}

				grfStream.Write(data, 0, data.Length);

				Progress = toIndex / (float) numberOfFilesToCopy * 100.0f;
			}

			return currentOffset;
		}
	}
}