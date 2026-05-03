using GRF.Core;
using GRF.FileFormats.SprFormat;
using System;
using System.IO;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.Tools.SpriteEditor {
	public class SprLoaderService {
		public class LoadResult {
			public bool AddToRecentFiles;
			public bool RemoveToRecentFiles;
			public string FilePath;
			public bool Success;
			public string ErrorMessage;
			public Spr LoadedSpr;

			public static LoadResult Fail(string message, string path) {
				LoadResult result = new LoadResult();
				result.Success = false;
				result.ErrorMessage = message;
				result.FilePath = path;
				result.RemoveToRecentFiles = true;
				return result;
			}
		}

		public LoadResult Load(TkPath file) {
			if (file.FilePath.IsExtension(".spr") || String.IsNullOrEmpty(file.RelativePath)) {
				return _loadFromFileSystem(file.FilePath);
			}
			else {
				return _loadFromGrf(file);
			}
		}

		private LoadResult _loadFromFileSystem(string file) {
			if (!file.IsExtension(".spr"))
				return LoadResult.Fail("Invalid file extension; only .spr files are allowed.", file);

			if (!File.Exists(file))
				return LoadResult.Fail("File not found while trying to open the Spr.\r\n\r\n" + file, file);

			LoadResult result = new LoadResult();
			result.FilePath = file;
			result.AddToRecentFiles = true;
			result.LoadedSpr = new Spr(file);
			result.LoadedSpr.LoadedPath = file;
			result.Success = true;
			return result;
		}

		private LoadResult _loadFromGrf(TkPath file) {
			if (!File.Exists(file.FilePath))
				return LoadResult.Fail("GRF path not found.", file);

			LoadResult result = new LoadResult();
			result.FilePath = file.GetFullPath();
			result.AddToRecentFiles = true;

			byte[] dataSpr = null;

			using (GrfHolder grf = new GrfHolder(file.FilePath)) {
				if (grf.FileTable.ContainsFile(file.RelativePath))
					dataSpr = grf.FileTable[file.RelativePath].GetDecompressedData();
			}

			if (dataSpr == null)
				return LoadResult.Fail("File not found: " + file, file);

			result.LoadedSpr = new Spr(dataSpr);
			result.LoadedSpr.LoadedPath = file.GetFullPath();
			result.Success = true;
			return result;
		}
	}
}
