using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ErrorManager;
using GRF.Core;
using GRF.Core.GrfCompression;
using GRF.System;
using GRF.Threading;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.FileFormats {
	public static class Rgz {
		public static bool ExtractRgz(IProgress progress, string fileName, string path, bool shellOpen) {
			string decompressedFileName = Path.Combine(Settings.TempPath, Path.GetRandomFileName());
			progress.Progress = -1;

			try {
				Compression.GZipDecompress(progress, fileName, decompressedFileName);
				byte[] arrDecompressed = File.ReadAllBytes(decompressedFileName);

				int offset = 0;
				while (offset <= arrDecompressed.Length) {
					progress.Progress = 50f + offset / (float) arrDecompressed.Length * 50.0f;

					char entryType = (char) arrDecompressed[offset];
					int fileLength = arrDecompressed[offset + 1];
					string name;
					offset += 2;

					if (entryType == 'f') {
						name = EncodingService.Ansi.GetString(arrDecompressed, offset, fileLength);
						name = EncodingService.FromAnsiToDisplayEncoding(name);
						name = name.Substring(0, name.IndexOf('\0'));

						string pathToCreate = Path.GetDirectoryName(Path.Combine(path, name));

						if (pathToCreate != null && !Directory.Exists(pathToCreate))
							Directory.CreateDirectory(pathToCreate);

						int length = BitConverter.ToInt32(arrDecompressed, offset + fileLength);
						byte[] actualData = new byte[length];
						Buffer.BlockCopy(arrDecompressed, offset + fileLength + 4, actualData, 0, length);
						offset += length + 4;

						File.WriteAllBytes(Path.Combine(path, name), actualData);
					}
					else if (entryType == 'd') {
						name = EncodingService.Ansi.GetString(arrDecompressed, offset, fileLength);
						name = EncodingService.FromAnsiToDisplayEncoding(name);
						name = name.Substring(0, name.IndexOf('\0'));

						string pathToCreate = Path.Combine(path, name);

						if (!Directory.Exists(pathToCreate))
							Directory.CreateDirectory(pathToCreate);
					}
					else if (entryType == 'e')
						break;

					offset += fileLength;
				}

				if (shellOpen)
					Process.Start(path);

				return true;
			}
			catch (OperationCanceledException) {
				return false;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
			finally {
				if (progress.IsCancelling) {
					progress.IsCancelled = true;
				}

				progress.Progress = 100f;
				File.Delete(decompressedFileName);
			}
		}

		private static string _checkCompression() {
			if (Compression.IsNormalCompression) {
				return null;
			}

			if (ErrorHandler.YesNoRequest("You are trying to compress a patch file using a custom compression method. The files identified are not going to be merged into a GRF and their extraction will most likely fail.\r\n\r\nDo you wish to change the compression back to zlib? (Press 'No' to ignore this warning).", "Suspicious compression")) {
				var oldCompression = ((CustomCompression) Compression.CompressionAlgorithm).FilePath;
				Compression.CompressionAlgorithm = new CpsCompression();
				return oldCompression;
			}

			return null;
		}

// ReSharper disable AccessToDisposedClosure
		internal static void SaveRgz(Container grfData, string fileName) {
			string compressionMethod = _checkCompression();

			try {
				using (GZipStream compressing = new GZipStream(new FileStream(fileName, FileMode.Create, FileAccess.Write), CompressionMode.Compress)) {
					grfData.ThreadOperation(p => { grfData.Progress = p; }, () => grfData.IsCancelling, (entry, data) => {
						if (!entry.Modification.HasFlags(Modification.Added) && entry.NewSizeDecompressed == 0) return;
						string name = EncodingService.GetAnsiString(entry.RelativePath);

						// Writes name and length
						if (name.StartsWith(GrfStrings.RgzRoot)) {
							string printedName = name.Replace(GrfStrings.RgzRoot, "");

							string directoryName = Path.GetDirectoryName(printedName);
							if (String.IsNullOrEmpty(directoryName)) {
								// This is a root file
								_writeString('f', printedName, compressing);

								compressing.Write(BitConverter.GetBytes(data.Length), 0, 4);
								compressing.Write(data, 0, data.Length);
							}
							else {
								// This is a file inside a folder
								string directory = Path.GetDirectoryName(printedName);

								if (!string.IsNullOrEmpty(directory))
									_writeString('d', Path.GetDirectoryName(printedName), compressing);

								_writeString('f', printedName, compressing);

								compressing.Write(BitConverter.GetBytes(data.Length), 0, 4);
								compressing.Write(data, 0, data.Length);
							}
						}
						else {
							// This is a file inside a folder
							string directory = Path.GetDirectoryName(name);

							if (!string.IsNullOrEmpty(directory))
								_writeString('d', Path.GetDirectoryName(name), compressing);

							_writeString('f', name, compressing);

							compressing.Write(BitConverter.GetBytes(data.Length), 0, 4);
							compressing.Write(data, 0, data.Length);
						}
					}, 1);

					_writeString('e', "end", compressing);
				}
			}
			finally {
				if (compressionMethod != null) {
					Compression.CompressionAlgorithm = new CustomCompression(compressionMethod, new Setting(v => { }, () => false));
				}
			}
		}

		private static void _writeString(char type, string name, Stream output) {
			output.WriteByte((byte) type);
			output.WriteByte((byte) (name.Length + 1));
			byte[] buffer = name.Bytes(name.Length + 1, EncodingService.Ansi);
			output.Write(buffer, 0, buffer.Length);
		}

		public static List<Tuple<string, string>> ConvertToSegments(IProgress container, string fileName) {
			List<Tuple<string, string>> filesGrf = new List<Tuple<string, string>>();
			string decompressedFileName = Path.Combine(Settings.TempPath, Path.GetRandomFileName());

			try {
				Compression.GZipDecompress(container, fileName, decompressedFileName);
				byte[] arrDecompressed = File.ReadAllBytes(decompressedFileName);

				int offset = 0;
				while (offset <= arrDecompressed.Length) {
					char entryType = (char) arrDecompressed[offset];
					int fileLength = arrDecompressed[offset + 1];
					string name;
					offset += 2;

					if (entryType == 'f') {
						name = EncodingService.Ansi.GetString(arrDecompressed, offset, fileLength);
						name = EncodingService.FromAnsiToDisplayEncoding(name);
						name = name.Substring(0, name.IndexOf('\0'));

						int length = BitConverter.ToInt32(arrDecompressed, offset + fileLength);
						byte[] actualData = new byte[length];
						Buffer.BlockCopy(arrDecompressed, offset + fileLength + 4, actualData, 0, length);
						offset += length + 4;

						string path = TemporaryFilesManager.GetTemporaryFilePath("rgz_segment_{0:000000}.part");

						File.WriteAllBytes(path, actualData);
						filesGrf.Add(new Tuple<string, string>(name, path));
					}
					else if (entryType == 'd') {
					}
					else if (entryType == 'e')
						break;

					offset += fileLength;
				}

				return filesGrf;
			}
			catch (OperationCanceledException) {
				return null;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return null;
			}
			finally {
				File.Delete(decompressedFileName);
			}
		}

		public static string GetRealFilename(string fileName) {
			if (fileName.StartsWith(GrfStrings.RgzRoot))
				return fileName.Remove(0, GrfStrings.RgzRoot.Length);
			if (fileName == GrfStrings.RgzRoot.TrimEnd('\\'))
				return fileName.Remove(0, GrfStrings.RgzRoot.TrimEnd('\\').Length);
			return fileName;
		}
	}
}