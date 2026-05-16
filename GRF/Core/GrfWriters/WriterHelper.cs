using GRF.GrfSystem;
using GRF.IO;
using GRF.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;
using Utilities.Hash;
using Utilities.Tools;

namespace GRF.Core.GrfWriters {
	internal class WriterHelper {
		public static void EncryptionCheck(Container grf) {
			if (grf.InternalHeader.IsEncrypted) {
				byte[] data;

				if (grf.InternalHeader.EncryptionKey != null)
					data = BitConverter.GetBytes(Crc32.Compute(grf.InternalHeader.EncryptionKey));
				else {
					data = BitConverter.GetBytes(grf.InternalHeader.EncryptionHashValue);
				}

				string tempFile = Path.Combine(Settings.TempPath, GrfStrings.EncryptionFilename);
				File.WriteAllBytes(tempFile, data);
				grf.Table.Add(GrfPath.Combine(IsRgz(grf) ? GrfStrings.RgzRoot : "", GrfStrings.EncryptionFilename), tempFile, true);
			}
			else {
				grf.Table.DeleteFile(GrfStrings.EncryptionFilename);
			}
		}

		public static bool IsRgz(Container grf) {
			return grf.Table.Entries.Any(p => p.RelativePath.StartsWith(GrfStrings.RgzRoot));
		}

		public class ByteArrayComparer : IEqualityComparer<byte[]> {
			public unsafe bool Equals(byte[] x, byte[] y) {
				if (ReferenceEquals(x, y)) return true;
				if (x == null || y == null) return false;

				if (x.Length != 16 || y.Length != 16)
					return false;

				fixed (byte* pX = x, pY = y) {
					long* lX = (long*)pX;
					long* lY = (long*)pY;

					return lX[0] == lY[0] && lX[1] == lY[1];
				}
			}

			public unsafe int GetHashCode(byte[] obj) {
				if (obj == null) return 0;
				if (obj.Length != 16) return 0;

				fixed (byte* pObj = obj) {
					long* lObj = (long*)pObj;

					unchecked {
						long hash = 17;
						hash = hash * 31 + lObj[0];
						hash = hash * 31 + lObj[1];

						return (int)(hash ^ (hash >> 32));
					}
				}
			}
		}

		public static void CompactHashAndDelete(TieredProgress tieredProgress, Container grf, Stream originalStream, List<FileEntry> entries, out List<FileEntry> deletedEntries, out Dictionary<FileEntry, FileEntry> redirectEntries) {
			Md5Hash hash = new Md5Hash();
			deletedEntries = new List<FileEntry>();
			var hashedEntries = new Dictionary<byte[], FileEntry>(new ByteArrayComparer());
			redirectEntries = new Dictionary<FileEntry, FileEntry>();

			try {
				originalStream.Seek(GrfHeader.DataByteSize, SeekOrigin.Begin);

				byte[] data;
				int toIndex = 0;
				FileEntry entry;
				FileEntry conflictEntry;
				int fromIndex;
				int indexMax = entries.Count;
				StreamReadBlockInfo srb = new StreamReadBlockInfo(GrfWriter.BufferSize);

				while (toIndex < indexMax) {
					fromIndex = toIndex;
					data = srb.ReadMisaligned(entries, out toIndex, fromIndex, indexMax, originalStream, out _);

					for (int i = fromIndex; i < toIndex; i++) {
						AProgress.IsCancelling(grf);
						entry = entries[i];

						var hashBytes = hash.ComputeByteHash(data, (int)entry.TemporaryOffset, entry.SizeCompressed);

						if (hashedEntries.TryGetValue(hashBytes, out conflictEntry)) {
							grf.Table.DeleteEntry(entry.RelativePath);
							deletedEntries.Add(entry);
							redirectEntries[entry] = conflictEntry;
						}
						else {
							hashedEntries[hashBytes] = entry;
						}

						tieredProgress?.SetTierProgress(i + 1);
					}
				}
			}
			catch {
				// Revert deleted entries!
				foreach (var entry in deletedEntries)
					grf.Table.AddEntry(entry);

				throw;
			}

			tieredProgress?.CompleteTier();
			grf.Table.InvalidateInternalSets();
		}

		public static void CompactRedirectHashedOffsets(Container grf, List<FileEntry> deletedEntries, Dictionary<FileEntry, FileEntry> redirectedEntries) {
			Md5Hash hash = new Md5Hash();

			for (int i = 0; i < deletedEntries.Count; i++) {
				FileEntry entry = deletedEntries[i];
				var dEntry = redirectedEntries[entry];

				entry.FileExactOffset = dEntry.FileExactOffset;
				entry.Offset = dEntry.Offset;
				entry.TemporaryOffset = dEntry.TemporaryOffset;

				grf.Table.AddEntry(entry);
			}
		}

		public static long EnsureStreamSize(Stream originalStream, List<FileEntry> sortedEntries) {
			long endStreamOffset = 0;

			if (sortedEntries.Count > 0) {
				var lastEntry = sortedEntries.Last();
				endStreamOffset = lastEntry.FileExactOffset + (uint)lastEntry.TemporarySizeCompressedAlignment;
			}

			endStreamOffset = Math.Max(endStreamOffset, GrfHeader.DataByteSize);

			if (originalStream.Length < endStreamOffset) {
				originalStream.SetLength(endStreamOffset);
			}

			return endStreamOffset;
		}

		public static void RestoreEncryptionHashFile(Container grf) {
			grf.Table.UndoDeleteFile(GrfStrings.EncryptionFilename);
			grf.Table.InvalidateInternalSets();
		}


		/// <summary>
		/// If the added GRF (in this case, it would be a Thor file) contains entries with the RemoveFile flag, 
		/// these entries must be removed from the primary file table.
		/// </summary>
		/// <param name="table">The primary GRF file table.</param>
		/// <param name="grfAdd">The GRF to add.</param>
		public static void ApplyDeleteFiles(FileTable table, Container grfAdd) {
			List<string> entriesToDelete = grfAdd.Table.Entries.Where(p => (p.Flags & EntryType.RemoveFile) == EntryType.RemoveFile).Select(p => p.RelativePath).ToList();

			foreach (string file in entriesToDelete) {
				grfAdd.Table.DeleteEntry(file);

				Regex regex = new Regex(Methods.WildcardToRegex(file), RegexOptions.IgnoreCase);

				foreach (string match in table.Files.Where(p => regex.IsMatch(p))) {
					table.DeleteFile(match);
				}
			}

			// I get the idea of removing existing entries, but these would be marked with GrfMerge? Why delete them on top...?
			//foreach (var entry in grfAdd.Table.Entries) {
			//	table.DeleteFile(entry.RelativePath);
			//}

			table.InvalidateInternalSets();
		}
	}
}
