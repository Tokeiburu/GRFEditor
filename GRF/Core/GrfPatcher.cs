using System.Collections.Generic;
using Utilities.Hash;

namespace GRF.Core {
	internal static class GrfPatcher {
		public static void Patch(Container grf1, Container grf2, string outputFilename) {
			// grf2 must be newer
			// We eliminate any entries that have the same hash
			// We only keep the files from grf2 that are new
			// If a file has the same hash, we remove it and continue

			List<FileEntry> entries1 = grf1.Table.Entries;
			string filename;

			Md5Hash hash = new Md5Hash();

			for (int i = 0; i < entries1.Count; i++) {
				filename = entries1[i].RelativePath;

				if (grf2.Table.Contains(filename)) {
					if (hash.CompareData(grf1.GetRawData(entries1[i]), grf2.GetRawData(grf2.Table[filename]))) {
						grf2.Table.DeleteEntry(filename);
					}
					else {
						if (hash.CompareData(grf1.GetDecompressedData(entries1[i]), grf2.GetDecompressedData(grf2.Table[filename]))) {
							grf2.Table.DeleteEntry(filename);
						}
					}
				}

				if (grf2.IsCancelling) {
					grf2.IsCancelled = true;
					return;
				}

				grf2.Progress = i / (float) entries1.Count * 100f;
			}

			grf2.Save(outputFilename, null, SavingMode.GrfSave, SyncMode.Synchronous);
			grf2.Close();
			grf1.Close();
		}
	}
}