using GRF.Core;
using GRF.GrfSystem;
using System.Text;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;
using Utilities.Services;

namespace GRF.FileFormats.ThorFormat {
	public class ThorIntegrity {
		public TkDictionary<string, string> EntryHashes = new TkDictionary<string, string>();
		private Crc32Hash _crcHash = new Crc32Hash();
		
		public void AddHash(FileEntry entry, byte[] compressed) {
			if (!Settings.AddHashFileForThor ||
				entry.RelativePath.Contains(GrfStrings.GrfIntegrityFile) ||
				(entry.Flags & EntryType.RemoveFile) == EntryType.RemoveFile)
				return;

			if (compressed.Length > 1) {
				EntryHashes[entry.RelativePath.ReplaceFirst(RgzFormat.Rgz.Root, "")] = "0x" + _crcHash.ComputeHash(compressed);
			}
		}

		internal bool AddIntegrityFile(Container grf) {
			if (!Settings.AddHashFileForThor ||
				EntryHashes.Count == 0)
				return false;

			StringBuilder builder = new StringBuilder();

			foreach (var hash in EntryHashes) {
				builder.Append(hash.Key);
				builder.Append("=");
				builder.AppendLine(hash.Value);
			}

			// Unlocks the GRF
			grf.IsBusy = false;
			grf.Commands.AddFile(GrfStrings.GrfIntegrityFile, EncodingService.DisplayEncoding.GetBytes(builder.ToString()));
			grf.IsBusy = true;
			return true;
		}
	}
}
