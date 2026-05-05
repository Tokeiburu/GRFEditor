using ErrorManager;
using GRF;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.ImfFormat;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.StrFormat;
using GRF.FileFormats.TgaFormat;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.Tools.GrfValidation {
	public class ValidateResult {
		public bool Success => Errors.Count > 0;
		public List<Utilities.Extension.Tuple<ValidationTypes, string, string>> Errors = new List<Utilities.Extension.Tuple<ValidationTypes, string, string>>();

		public void AddErrors(ValidationTypes type, string relativePath, string message) {
			Errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(type, relativePath, message));
		}
	}

	public class ValidateContentReader {
		private readonly object _lock = new object();
		public string TextureMiniMapPath;
		public string TextureCollectionPath;
		public string TextureIllustPath;
		public string TextureItemPath;
		public string TexturePath;

		public ValidateContentReader() {
			TextureMiniMapPath = EncodingService.FromAnsiToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\map");
			TextureCollectionPath = EncodingService.FromAnsiToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection");
			TextureIllustPath = EncodingService.FromAnsiToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\illust");
			TextureItemPath = EncodingService.FromAnsiToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item");

			TexturePath = EncodingService.FromAnsiToDisplayEncoding(@"data\texture\");
		}

		public void ValidateContent(ProgressObject prog, ValidateResult result, GrfHolder container) {
			try {
				List<FileEntry> entries = container.FileTable.Entries;
				int numberOfFilesProcessed = 0;
				Compression.EnsureChecksum = GrfEditorConfiguration.VcZlibChecksum;

				for (int index = 0; index < entries.Count; index++) {
					//Parallel.For(0, entries.Count, index => {
					if (prog.IsCancelling)
						return;

					FileEntry entry = entries[index];
					ValidateResult currentResult = Validate(entry);

					lock (_lock) {
						if (currentResult != null)
							result.Errors.AddRange(currentResult.Errors);

						numberOfFilesProcessed++;
					}

					prog.Progress = (float)numberOfFilesProcessed / entries.Count * 100f; ;
					//});
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Compression.EnsureChecksum = false;
				prog.Finish();
			}
		}

		public void ValidatePaths(IEnumerable<string> paths, ValidationTypes type, string file, string error, ValidateResult result) {
			foreach (string path in paths) {
				if (!GrfEditorConfiguration.Resources.MultiGrf.Exists(path)) {
					result.AddErrors(type, file, String.Format(error, path));
				}
			}
		}

		public ValidateResult Validate(FileEntry entry) {
			ValidateResult result = new ValidateResult();

			if (entry.Flags.HasFlag(EntryType.GravityEncryptedFile) ||
				entry.Flags.HasFlag(EntryType.GrfEditorCrypted))
				return result;

			byte[] data = _validateDecompress(entry, result);

			if (data == null || !GrfEditorConfiguration.VcLoadEntries)
				return result;

			try {
				switch (entry.RelativePath.GetExtension()) {
					case ".rsm":
					case ".rsm2":
						_validateRsm(entry, data, result);
						break;
					case ".str":
						_validateLoad<Str>(entry, data, result);
						break;
					case ".gnd":
						_validateGnd(entry, data, result);
						break;
					case ".gat":
						_validateLoad<Gat>(entry, data, result);
						_validateLoadImage(entry, data, result);
						break;
					case ".rsw":
						_validateRsw(entry, data, result);
						break;
					case ".act":
						_validateAct(entry, data, result);
						break;
					case ".tga":
						_validateLoad<Tga>(entry, data, result);
						_validateLoadImage(entry, data, result);
						break;
					case ".pal":
						_validateLoad<Pal>(entry, result, data, Pal.FormatMode.PreserveOriginal);
						_validateLoadImage(entry, data, result);
						break;
					case ".ebm":
						_validateLoad<Ebm>(entry, data, result);
						_validateLoadImage(entry, data, result);
						break;
					case ".bmp":
						if (GrfEditorConfiguration.VcInvalidImageFormat)
							_validateImageFormat(entry, data, result);

						_validateLoadImage(entry, data, result);
						break;
					case ".png":
						_validateLoadImage(entry, data, result);
						break;
					case ".jpg":
						_validateLoadImage(entry, data, result);
						break;
					case ".imf":
						_validateLoad<Imf>(entry, data, result);
						break;
					case ".fna":
						_validateLoad<Fna>(entry, data, result);
						break;
				}
			}
			catch (Exception err) {
				result.AddErrors(ValidationTypes.VcLoadEntries, entry.RelativePath, err.Message);
			}

			return result;
		}

		private void _validateImageFormat(FileEntry entry, byte[] data, ValidateResult result) {
			try {
				BmpDecoder decoder = new BmpDecoder(data);
				var friendlyFormat = decoder.GetOriginalImageFormat();

				if (entry.DirectoryPath == TextureMiniMapPath ||
					entry.DirectoryPath == TextureIllustPath ||
					entry.DirectoryPath == TextureCollectionPath ||
					entry.DirectoryPath == TextureItemPath ||
					entry.RelativePath.StartsWith(TexturePath)) {
					switch (friendlyFormat) {
						case GrfImageType.Indexed8:
						case GrfImageType.Bgr24:
							break;
						default:
							result.AddErrors(ValidationTypes.VcInvalidImageFormat, entry.RelativePath, "Expected BMP format to be either Indexed8 (256-colors) or Bgr24 (24-bit).");
							break;
					}
				}
			}
			catch {
				// If the BMP cannot be decoded, it'll be handled through _validateLoadImage
			}
		}

		private byte[] _validateDecompress(FileEntry entry, ValidateResult result) {
			byte[] entryData = null;

			try {
				entryData = entry.GetDecompressedData();

				if (GrfEditorConfiguration.VcInvalidEntryMetadata && entryData.Length != entry.NewSizeDecompressed && !entry.Added) {
					result.AddErrors(ValidationTypes.VcInvalidEntryMetadata, entry.RelativePath, "Invalid size decompressed.");
				}
			}
			catch (GrfException err) {
				entryData = null;

				if (GrfEditorConfiguration.VcZlibChecksum && err == GrfExceptions.__ChecksumFailed) {
					result.AddErrors(ValidationTypes.VcZlibChecksum, entry.RelativePath, "The zlib checksum test has failed, try to repack the GRF.");
				}
				else {
					result.AddErrors(ValidationTypes.VcDecompressEntries, entry.RelativePath, "Couldn't decompress the file (corrupted entry).");
				}
			}
			catch {
				entryData = null;

				result.AddErrors(ValidationTypes.VcDecompressEntries, entry.RelativePath, "Couldn't decompress the file (corrupted entry).");
			}

			return entryData;
		}

		private void _validateAct(FileEntry actEntry, byte[] actData, ValidateResult result) {
			Act act = null;
			Spr spr = null;

			FileEntry sprEntry = null;
			string sprPath = actEntry.RelativePath.ReplaceExtension(".spr");
			
			// Find spr first
			try {
				sprEntry = GrfEditorConfiguration.Resources.MultiGrf.GetEntry(sprPath);

				if (sprEntry == null) {
					// If a garment/wing, attempt to load the SPR from the parent directory
					if (actEntry.RelativePath.StartsWith(EncodingService.FromAnyToDisplayEncoding(@"data\sprite\·Îºê\"))) {
						var dirs = GrfPath.SplitDirectories(actEntry.RelativePath).ToList();
						dirs.RemoveAt(dirs.Count - 1);
						dirs.RemoveAt(dirs.Count - 1);
						dirs.Add(dirs.Last() + ".spr");

						sprEntry = GrfEditorConfiguration.Resources.MultiGrf.GetEntry(GrfPath.Combine(dirs.ToArray()));
					}
				}
			}
			catch {
				result.AddErrors(ValidationTypes.VcLoadEntries, actEntry.RelativePath, "Failed to load related SPR file's content (corrupted?).");
			}

			try {
				spr = sprEntry != null ? _validateLoad<Spr>(sprEntry, result) : null;

				if (spr == null)
					act = _validateLoad<Act>(actEntry, actData, result);
				else
					act = new Act(actEntry, sprEntry);
			}
			catch {
				result.AddErrors(ValidationTypes.VcLoadEntries, actEntry.RelativePath, "Failed to load file's content (corrupted?).");
			}

			if (act != null)
				_validateActSub(act, spr, actEntry, result);
		}

		private void _validateActSub(Act act, Spr spr, FileEntry entry, ValidateResult result) {
			if (GrfEditorConfiguration.VcSpriteIssues) {
				for (int i = 0; i < act.NumberOfActions; i++) {
					for (int j = 0; j < act[i].NumberOfFrames; j++) {
						Frame frame = act[i].Frames[j];

						if (GrfEditorConfiguration.VcSpriteSoundIndex) {
							if (frame.SoundId >= act.SoundFiles.Count) {
								result.AddErrors(ValidationTypes.VcSpriteSoundIndex, entry.RelativePath, String.Format("Actions[{0}].Frames[{1}] is referring to an invalid sound index ({2}).", i, j, frame.SoundId));
							}
						}

						if (GrfEditorConfiguration.VcSpriteIndex && spr != null) {
							for (int k = 0; k < frame.NumberOfLayers; k++) {
								if (frame.Layers[k].SpriteIndex >= act.Sprite.NumberOfImagesLoaded) {
									result.AddErrors(ValidationTypes.VcSpriteIndex, entry.RelativePath, String.Format("Actions[{0}].Frames[{1}].Layers[{2}] is referring to an invalid sprite index ({3}).", i, j, k, frame.Layers[k].SpriteIndex));
								}
							}
						}
					}
				}

				if (GrfEditorConfiguration.VcSpriteSoundMissing) {
					ValidatePaths(act.SoundFiles.Where(p => p != "atk").Select(p => @"data\wav\" + p), ValidationTypes.VcSpriteSoundMissing, entry.RelativePath, "Sound file missing ({0}).", result);
				}
			}
		}

		private void _validateRsw(FileEntry entry, byte[] data, ValidateResult result) {
			Rsw rsw = _validateLoad<Rsw>(entry, data, result);

			if (rsw != null && GrfEditorConfiguration.VcResourcesMapFiles) {
				ValidatePaths(rsw.ModelResources.Select(p => @"data\model\" + p), ValidationTypes.VcResourcesMapFiles, entry.RelativePath, "Texture file missing ({0}).", result);
			}
		}

		private void _validateGnd(FileEntry entry, byte[] data, ValidateResult result) {
			Gnd gnd = _validateLoad<Gnd>(entry, data, result);

			if (gnd != null && GrfEditorConfiguration.VcResourcesMapFiles) {
				ValidatePaths(gnd.TexturesPath.Select(p => @"data\texture\" + p), ValidationTypes.VcResourcesMapFiles, entry.RelativePath, "Texture file missing ({0}).", result);
			}
		}

		private void _validateRsm(FileEntry entry, byte[] data, ValidateResult result) {
			Rsm rsm = _validateLoad<Rsm>(entry, data, result);

			if (rsm != null && GrfEditorConfiguration.VcResourcesModelFiles) {
				ValidatePaths(rsm.Textures.Select(p => @"data\texture\" + p), ValidationTypes.VcResourcesModelFiles, entry.RelativePath, "Texture file missing ({0}).", result);
			}
		}

		private void _validateLoadImage(FileEntry entry, byte[] data, ValidateResult result) {
			try {
				ImageProvider.GetImage(data, entry.RelativePath.GetExtension());
			}
			catch {
				result.AddErrors(ValidationTypes.VcLoadEntries, entry.RelativePath, "Failed to load image's content (corrupted?).");
			}
		}

		private T _validateLoad<T>(FileEntry entry, ValidateResult result, params object[] obj) where T : class {
			try {
				if (obj?.Length > 0) {
					T t = (T)Activator.CreateInstance(typeof(T), obj);
					return t;
				}
				else {
					T t = (T)Activator.CreateInstance(typeof(T), (MultiType)entry);
					return t;
				}
			}
			catch {
				result.AddErrors(ValidationTypes.VcLoadEntries, entry.RelativePath, "Failed to load file's content (corrupted?).");
			}
		
			return null;
		}

		private T _validateLoad<T>(FileEntry entry, byte[] data, ValidateResult result) where T : class {
			try {
				T t = (T)Activator.CreateInstance(typeof(T), (MultiType)data);
				return t;
			}
			catch {
				result.AddErrors(ValidationTypes.VcLoadEntries, entry.RelativePath, "Failed to load file's content (corrupted?).");
			}

			return null;
		}
	}
}
