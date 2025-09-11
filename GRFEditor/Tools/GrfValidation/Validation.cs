using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.Core.GroupedGrf;
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
using GRF.IO;
using GRF.Image;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;

//using Parallel = GRF.Threading.Parallel;
using Utilities.Services;

namespace GRFEditor.Tools.GrfValidation {
	public class Validation : IProgress {
		private readonly object _lock = new object();

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		public void FindErrors(List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors, GrfHolder container) {
			try {
				_initProgress();
				errors.Clear();

				List<KeyValuePair<string, FileEntry>> entries = container.FileTable.FastAccessEntries;

				if (GrfEditorConfiguration.FeSpaceSaved) {
					long size = 0;

					List<FileEntry> entriesList = entries.Select(p => p.Value).OrderBy(p => p.FileExactOffset).ToList();

					if (entries.Count > 0) {
						size = entriesList[0].FileExactOffset - GrfHeader.DataByteSize;
					}

					for (int i = 0; i < entries.Count - 1; i++) {
						size += entriesList[i + 1].FileExactOffset - entriesList[i].FileExactOffset - entriesList[i].SizeCompressedAlignment;
					}

					if (size > 0) {
						string spaceSavedString = Methods.FileSizeToString(size);
						errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeSpaceSaved, "", "You can save " + spaceSavedString + " by saving this GRF."));
					}
				}

				if (GrfEditorConfiguration.FeInvalidFileTable) {
					List<FileEntry> entriesList = entries.Where(p => !p.Value.Added).Select(p => p.Value).ToList();

					errors.AddRange(entriesList.Where(p => p.SizeCompressedAlignment < p.SizeCompressed)
										.Select(p => new Utilities.Extension.Tuple<ValidationTypes, string, string>(
							                             ValidationTypes.FeInvalidFileTable, p.RelativePath, "Invalid file alignment (lower than compressed size).")));

					if (container.Header.IsMajorVersion(1)) {
						errors.AddRange(entriesList.Where(p => p.SizeCompressedAlignment % 8 != 0)
											.Select(p => new Utilities.Extension.Tuple<ValidationTypes, string, string>(
								                             ValidationTypes.FeInvalidFileTable, p.RelativePath, "Invalid file alignment (misaligned bytes : " + (p.SizeCompressedAlignment % 8) + ").")));
					}
				}

				if (GrfEditorConfiguration.FeNoExtension) {
					errors.AddRange(entries.Where(p => !Path.GetFileName(p.Key).
															Contains('.')).Select(p => new Utilities.Extension.Tuple<ValidationTypes, string, string>(
							                                                               ValidationTypes.FeNoExtension, p.Key, "File name has no extension.")));
				}

				if (GrfEditorConfiguration.FeMissingSprAct) {
					string garmentFolder = EncodingService.FromAnyToDisplayEncoding(@"data\sprite\·Îºê\");
					List<FileEntry> actFiles = entries.Where(p => !p.Key.Contains(garmentFolder) && p.Key.IsExtension(".act")).Select(p => p.Value).ToList();
					List<FileEntry> sprFiles = entries.Where(p => !p.Key.Contains(garmentFolder) && p.Key.IsExtension(".spr")).Select(p => p.Value).ToList();
					List<string> sprFileNamesCut = sprFiles.Select(p => p.RelativePath.Replace(Path.GetExtension(p.RelativePath), "")).ToList();
					List<string> actFileNamesCut = actFiles.Select(p => p.RelativePath.Replace(Path.GetExtension(p.RelativePath), "")).ToList();

					errors.AddRange(
						from act in actFiles
						where !sprFileNamesCut.Contains(act.RelativePath.Replace(Path.GetExtension(act.RelativePath), ""))
						select new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeMissingSprAct, act.RelativePath, "Complementing Spr file missing."));

					errors.AddRange(
						from spr in sprFiles
						where !actFileNamesCut.Contains(spr.RelativePath.Replace(Path.GetExtension(spr.RelativePath), ""))
						select new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeMissingSprAct, spr.RelativePath, "Spr file is not used."));
				}

				if (GrfEditorConfiguration.FeEmptyFiles) {
					string emptySizeString = Methods.FileSizeToString(0);
					errors.AddRange(entries.Where(p => p.Value.DisplaySize == emptySizeString).Select(p =>
																									  new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeEmptyFiles, p.Key, "Empty file (size = 0), remove it.")));
				}

				if (GrfEditorConfiguration.FeDb) {
					errors.AddRange(entries.Where(p => Path.GetExtension(p.Key) != null && Path.GetExtension(p.Key).ToLower() == ".db").Select(p =>
					                                                                                                                           new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeDb, p.Key, "Hidden database file for thumbnails, remove it.")));
				}

				if (GrfEditorConfiguration.FeSvn) {
					errors.AddRange(entries.Where(p => Path.GetExtension(p.Key) != null && Path.GetExtension(p.Key).ToLower() == ".svn").Select(p =>
					                                                                                                                            new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeSvn, p.Key, "Subversion file (usually hidden), remove it.")));
				}

				if (GrfEditorConfiguration.FeDuplicateFiles) {
					Dictionary<string, int> entriesDuplicates = new Dictionary<string, int>();
					string lowerCaseEntry;

					foreach (KeyValuePair<string, FileEntry> entry in entries) {
						lowerCaseEntry = container.FileTable[entry.Key].RelativePath;

						if (entriesDuplicates.ContainsKey(lowerCaseEntry)) {
							entriesDuplicates[lowerCaseEntry]++;
						}
						else {
							entriesDuplicates[lowerCaseEntry] = 0;
						}
					}

					errors.AddRange(entriesDuplicates.Where(p => p.Value > 0).Select(p =>
					                                                                 new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeDuplicateFiles, p.Key, "This file has been found " + (p.Value + 1) + " times.")));
				}

				if (GrfEditorConfiguration.FeRootFiles) {
					foreach (KeyValuePair<string, FileEntry> entry in entries) {
						if (String.IsNullOrEmpty(Path.GetDirectoryName(entry.Key))) {
							errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
								           ValidationTypes.FeRootFiles, entry.Key, "This file is at the root of the container, this is not recommended."));
						}
					}
				}

				if (GrfEditorConfiguration.FeDuplicatePaths) {
					List<string> folders = entries.Select(p => Path.GetDirectoryName(p.Key)).Distinct().ToList();

					Dictionary<string, int> entriesDuplicates = new Dictionary<string, int>();
					string lowerCaseEntry;

					foreach (string folder in folders) {
						lowerCaseEntry = folder.ToLower();

						if (entriesDuplicates.ContainsKey(lowerCaseEntry)) {
							entriesDuplicates[lowerCaseEntry]++;
						}
						else {
							entriesDuplicates[lowerCaseEntry] = 0;
						}
					}

					errors.AddRange(entriesDuplicates.Where(p => p.Value > 0).Select(p =>
					                                                                 new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.FeDuplicatePaths, p.Key, "This path has been found " + (p.Value + 1) + " times.")));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_finalizeProgress();
			}
		}

		public void ValidateContent(List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors, GrfHolder container, MultiGrfReader metaGrf) {
			try {
				_initProgress();
				errors.Clear();

				MultiGrfReader metaGrfLocalCopy = metaGrf;
				List<FileEntry> entries = container.FileTable.Entries;
				int numberOfFilesProcessed = 0;
				Compression.EnsureChecksum = GrfEditorConfiguration.VcZlibChecksum;

				Parallel.For(0, entries.Count, index => {
					//for (int index = 0; index < entries.Count; index++) {
					FileEntry entry = entries[index];

					try {
						if (!IsCancelling) {
							if (GrfEditorConfiguration.VcDecompressEntries) {
								byte[] entryData;

								try {
									entryData = entry.GetDecompressedData();

									if (GrfEditorConfiguration.VcInvalidEntryMetadata) {
										if (entryData.Length != entry.NewSizeDecompressed && !entry.Added) {
											errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
														   ValidationTypes.VcInvalidEntryMetadata, entry.RelativePath, "Invalid size decompressed."));
										}
									}
								}
								catch (GrfException err) {
									entryData = null;

									if (err == GrfExceptions.__ChecksumFailed) {
										errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
												   ValidationTypes.VcZlibChecksum, entry.RelativePath, "The zlib checksum test has failed, try to repack the GRF."));
									}
									else {
										errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
													   ValidationTypes.VcDecompressEntries, entry.RelativePath, "Couldn't decompress the file (corrupted entry)."));
									}
								}
								catch {
									entryData = null;
									errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
												   ValidationTypes.VcDecompressEntries, entry.RelativePath, "Couldn't decompress the file (corrupted entry)."));
								}

								if (entryData != null) {
									if (GrfEditorConfiguration.VcLoadEntries) {
										string extension = Path.GetExtension(entry.RelativePath);

										if (extension != null) {
											extension = extension.ToLower();

											switch (extension) {
												case ".rsm":
												case ".rsm2":
													Rsm rsm = _init<Rsm>((MultiType)entryData);
													_reportNullReference(rsm);

													if (GrfEditorConfiguration.VcResourcesModelFiles) {
														_validatePaths(metaGrfLocalCopy, rsm.Textures.Select(p => @"data\texture\" + p),
														               ValidationTypes.VcResourcesModelFiles, entry.RelativePath, "Texture file missing ({0}).", errors);
													}
													break;
												case ".str":
													Str str = _init<Str>((MultiType)entryData);
													_reportNullReference(str);
													break;
												case ".gnd":
													Gnd gnd = _init<Gnd>((MultiType)entryData);
													_reportNullReference(gnd);

													if (GrfEditorConfiguration.VcResourcesMapFiles) {
														_validatePaths(metaGrfLocalCopy, gnd.TexturesPath.Select(p => @"data\texture\" + p),
														               ValidationTypes.VcResourcesMapFiles, entry.RelativePath, "Texture file missing ({0}).", errors);
													}
													break;
												case ".gat":
													Gat gat = _init<Gat>((MultiType)entryData);
													_reportNullReference(gat);
													_toImage(entryData, entry.RelativePath);
													break;
												case ".rsw":
													Rsw rsw = _init<Rsw>((MultiType)entryData);
													_reportNullReference(rsw);

													//if (GrfEditorConfiguration.VcInvalidQuadTree) {
													//    rsw.RebuildQuadTree()
													//}

													if (GrfEditorConfiguration.VcResourcesMapFiles) {
														_validatePaths(metaGrfLocalCopy, rsw.ModelResources.Select(p => @"data\model\" + p),
														               ValidationTypes.VcResourcesMapFiles, entry.RelativePath, "Model file missing ({0}).", errors);
													}
													break;
												case ".act":
													Act act = null;
													Spr sprTemp = null;

													try {
														sprTemp = _init<Spr>((MultiType) metaGrfLocalCopy.GetData(entry.RelativePath.ToLower().Replace(".act", ".spr")));
														act = _init<Act>((MultiType)entryData, sprTemp);
													}
													catch {
														if (sprTemp != null) {
															_reportNullReference(act);
														}
													}

													if (act != null)
														_validateAct(act, errors, entry, metaGrfLocalCopy);
													break;
												case ".spr":
													Spr spr = _init<Spr>((MultiType) entryData);
													_reportNullReference(spr);
													_toImage(entryData, Path.GetExtension(entry.RelativePath));

													if (GrfEditorConfiguration.VcSpriteIssues) {
														if (GrfEditorConfiguration.VcSpriteIssuesRle) {
															if (spr.GetEarlyEndingEncoding().Cast<object>().Any()) {
																errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.VcSpriteIssuesRle, entry.RelativePath, "Early ending RLE encoding (usually not an issue)."));
															}
														}
													}
													break;
												case ".tga":
													Tga tga = _init<Tga>((MultiType)entryData);
													_reportNullReference(tga);
													_toImage(entryData, entry.RelativePath);
													break;
												case ".pal":
													Pal pal = _init<Pal>(entryData, true);
													_reportNullReference(pal);
													_toImage(entryData, entry.RelativePath);
													break;
												case ".ebm":
													Ebm ebm = _init<Ebm>(entryData);
													_reportNullReference(ebm);
													_toImage(entryData, entry.RelativePath);
													break;
												case ".bmp":
												case ".png":
												case ".jpg":
													// Common formats
													_toImage(entryData, entry.RelativePath);
													break;
												case ".imf":
													Imf imf = _init<Imf>((MultiType)entryData);
													_reportNullReference(imf);
													break;
												case ".fna":
													Fna fna = _init<Fna>((MultiType)entryData);
													_reportNullReference(fna);
													break;
											}
										}
									}
								}
							}
						}
					}
					catch (NullReferenceException) {
						_failedLoading(errors, entry);
					}
					catch (Exception err) {
						//_failedLoading(errors, entry);
						errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.VcUnknown, entry.RelativePath, err.Message));
					}

					lock (_lock) {
						numberOfFilesProcessed++;
					}
					Progress = (float) numberOfFilesProcessed / entries.Count * 100f;
					//}
				});

				if (IsCancelling) {
					IsCancelled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Compression.EnsureChecksum = false;
				_finalizeProgress();
			}
		}

		private void _toImage(byte[] entryData, string path) {
			try {
				ImageProvider.GetImage(entryData, Path.GetExtension(path)).Cast<BitmapSource>();
			}
			catch {
				throw new NullReferenceException();
			}
		}

		private void _validateAct(Act act, List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors, FileEntry entry, MultiGrfReader metaGrfLocalCopy) {
			if (GrfEditorConfiguration.VcSpriteIssues) {
				for (int i = 0; i < act.NumberOfActions; i++) {
					for (int j = 0; j < act[i].NumberOfFrames; j++) {
						Frame frame = act[i].Frames[j];

						if (GrfEditorConfiguration.VcSpriteSoundIndex) {
							if (frame.SoundId >= act.SoundFiles.Count) {
								errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.VcSpriteSoundIndex,
								                                                      entry.RelativePath, String.Format("Actions[{0}].Frames[{1}] is referring to an invalid sound index ({2}).",
								                                                                                        i, j, frame.SoundId)));
							}
						}

						if (GrfEditorConfiguration.VcSpriteIndex) {
							for (int k = 0; k < frame.NumberOfLayers; k++) {
								if (frame.Layers[k].SpriteIndex >= act.Sprite.NumberOfImagesLoaded) {
									errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.VcSpriteIndex,
									                                                      entry.RelativePath, String.Format("Actions[{0}].Frames[{1}].Layers[{2}] is referring to an invalid sprite index ({3}).",
									                                                                                        i, j, k, frame.Layers[k].SpriteIndex)));
								}
							}
						}
					}
				}

				if (GrfEditorConfiguration.VcSpriteSoundMissing) {
					_validatePaths(metaGrfLocalCopy, act.SoundFiles.Where(p => p != "atk").Select(p => @"data\wav\" + p),
					               ValidationTypes.VcSpriteSoundMissing, entry.RelativePath, "Sound file missing ({0}).", errors);
				}
			}
		}

		private T _init<T>(params object[] args) where T : class {
			try {
				T t = (T) Activator.CreateInstance(typeof (T), args);
				return t;
			}
			catch {
				return null;
			}
		}

		private void _reportNullReference(object obj) {
			if (obj == null)
				throw new NullReferenceException();
		}

		private void _validatePaths(MultiGrfReader metaGrf, IEnumerable<string> paths, ValidationTypes type, string file, string error, List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors) {
			foreach (string path in paths) {
				if (!metaGrf.Exists(path)) {
					errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(type, file, String.Format(error, path)));
				}
			}
		}

		private void _failedLoading(List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors, FileEntry entry) {
			errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(ValidationTypes.VcLoadEntries, entry.RelativePath, "Failed to load file's content (corrupted?)."));
		}

		private void _finalizeProgress() {
			if (IsCancelling) {
				IsCancelled = true;
			}

			Progress = 100f;
		}

		private void _initProgress() {
			Progress = -1;
			IsCancelling = false;
			IsCancelled = false;
		}

		public void ValidateExtraction(List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors, GrfHolder grfHolder,
		                               string path, bool direction, IHash hash, bool ignoreFiles) {
			try {
				_initProgress();
				errors.Clear();

				// Grf to Folder
				if (direction) {
					List<FileEntry> entries = grfHolder.FileTable.Entries;
					int numberOfFilesProcessed = 0;
					path = Path.GetDirectoryName(path).Trim('\\');

					Parallel.For(0, entries.Count, index => {
						//for (int index = 0; index < entries.Count; index++) {
						try {
							if (!IsCancelling) {
								FileEntry entry = entries[index];
								string fileName = Path.Combine(path, entry.RelativePath);

								if (File.Exists(fileName)) {
									byte[] file1 = File.ReadAllBytes(fileName);
									byte[] file2 = entry.GetDecompressedData();

									if (GrfEditorConfiguration.VeFilesDifferentSize &&
									    file1.Length != file2.Length) {
										lock (_lock) {
											errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
												           ValidationTypes.VeFilesDifferentSize, entry.RelativePath, "Different size (GRF: " +
												                                                                     file2.Length + "; HD: " + file1.Length + ")."));
										}
									}
									else {
										byte[] hashString1 = hash.ComputeByteHash(file1);
										byte[] hashString2 = hash.ComputeByteHash(file2);

										if (!Methods.ByteArrayCompare(hashString1, hashString2)) {
											lock (_lock) {
												errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
													           ValidationTypes.VeDifferentHashValue, entry.RelativePath, "Different hash value (GRF: " +
													                                                                     Methods.ByteArrayToString(hashString2) + "; HD: " + Methods.ByteArrayToString(hashString1) + ")."));
											}
										}
									}
								}
								else {
									if (!ignoreFiles) {
										errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
											           ValidationTypes.VeFileNotFound, entry.RelativePath, "File not found on the hard drive : " + fileName));
									}
								}

								lock (_lock) {
									numberOfFilesProcessed++;
								}

								Progress = (float) numberOfFilesProcessed / entries.Count * 100f;
							}
						}
						catch (Exception err) {
							IsCancelling = true;
							ErrorHandler.HandleException(err);
						}
					});
					//}
				}
				else {
					List<string> files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
					int numberOfFilesProcessed = 0;
					path = GrfPath.GetDirectoryName(path).Trim('\\') + "\\";
					grfHolder.FileTable.ContainsFile(""); // Forces the indexation

					Parallel.For(0, files.Count, index => {
						//for (int index = 0; index < files.Count; index++) {
						try {
							if (!IsCancelling) {
								string fileName = files[index];
								string relativePath = fileName.ReplaceFirst(path, "");

								if (grfHolder.FileTable.ContainsFile(relativePath)) {
									FileEntry entry = grfHolder.FileTable[relativePath];

									byte[] file1 = File.ReadAllBytes(fileName);
									byte[] file2 = entry.GetDecompressedData();

									if (GrfEditorConfiguration.VeFilesDifferentSize &&
									    file1.Length != file2.Length) {
										lock (_lock) {
											errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
												           ValidationTypes.VeFilesDifferentSize, entry.RelativePath, "Different size (GRF: " +
												                                                                     file2.Length + "; HD: " + file1.Length + ")."));
										}
									}
									else {
										byte[] hashString1 = hash.ComputeByteHash(file1);
										byte[] hashString2 = hash.ComputeByteHash(file2);

										if (!Methods.ByteArrayCompare(hashString1, hashString2)) {
											lock (_lock) {
												errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
													           ValidationTypes.VeDifferentHashValue, fileName, "Different hash value (GRF: " +
													                                                           Methods.ByteArrayToString(hashString2) + "; HD: " + Methods.ByteArrayToString(hashString1) + ")."));
											}
										}
									}
								}
								else {
									if (!ignoreFiles) {
										lock (_lock) {
											errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
												           ValidationTypes.VeFileNotFound, fileName, "File not found in the GRF : " + relativePath));
										}
									}
								}

								lock (_lock) {
									numberOfFilesProcessed++;
								}

								Progress = (float) numberOfFilesProcessed / files.Count * 100f;
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
						//}
					});
				}

				if (IsCancelling) {
					IsCancelled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_finalizeProgress();
			}
		}

		public void ComputeHash(List<Utilities.Extension.Tuple<ValidationTypes, string, string>> errors, GrfHolder grfHolder, IHash hash, bool direction, string path) {
			try {
				_initProgress();
				errors.Clear();

				// Grf to Folder
				if (direction) {
					List<FileEntry> entries = grfHolder.FileTable.Entries;
					int numberOfFilesProcessed = 0;

					Parallel.For(0, entries.Count, index => {
						try {
							if (!IsCancelling) {
								FileEntry entry = entries[index];
								string hashString2 = hash.ComputeHash(entry.GetDecompressedData());

								lock (_lock) {
									errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
										           ValidationTypes.VeComputeHash, entry.RelativePath, hashString2));
									numberOfFilesProcessed++;
								}
							}

							Progress = (float) numberOfFilesProcessed / entries.Count * 100f;
						}
						catch (Exception err) {
							IsCancelling = true;
							ErrorHandler.HandleException(err);
						}
					});
				}
				else {
					List<string> files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
					int numberOfFilesProcessed = 0;

					Parallel.For(0, files.Count, index => {
						try {
							if (!IsCancelling) {
								string fileName = files[index];
								string hashString1 = hash.ComputeHash(File.ReadAllBytes(fileName));

								lock (_lock) {
									errors.Add(new Utilities.Extension.Tuple<ValidationTypes, string, string>(
										           ValidationTypes.VeComputeHash, fileName, hashString1));
									numberOfFilesProcessed++;
								}
							}

							Progress = (float) numberOfFilesProcessed / files.Count * 100f;
						}
						catch (Exception err) {
							IsCancelling = true;
							ErrorHandler.HandleException(err);
						}
					});
				}

				if (IsCancelling) {
					IsCancelled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_finalizeProgress();
			}
		}

		public void ComputeHashQuick(List<Utilities.Extension.Tuple<TkPath, byte[]>> errors, GrfHolder grfHolder, IHash hash) {
			try {
				_initProgress();
				errors.Clear();

				List<FileEntry> entries = grfHolder.FileTable.Entries;
				int numberOfFilesProcessed = 0;
				string filePath = grfHolder.FileName;

				Parallel.For(0, entries.Count, index => {
					try {
						if (!IsCancelling) {
							FileEntry entry = entries[index];
							byte[] byteHash = hash.ComputeByteHash(entry.GetCompressedData());

							lock (_lock) {
								errors.Add(new Utilities.Extension.Tuple<TkPath, byte[]>(
									           new TkPath { FilePath = filePath, RelativePath = entry.RelativePath }, byteHash));
							}
						}

						numberOfFilesProcessed++;
						Progress = (float) numberOfFilesProcessed / entries.Count * 100f;
					}
					catch (Exception err) {
						IsCancelling = true;
						ErrorHandler.HandleException(err);
					}
				});

				if (IsCancelling) {
					IsCancelled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_finalizeProgress();
			}
		}
	}

	public enum ValidationTypes {
		FeNoExtension,
		FeMissingSprAct,
		FeEmptyFiles,
		FeDb,
		FeSvn,
		FeDuplicateFiles,
		FeDuplicatePaths,
		FeSpaceSaved,
		FeInvalidFileTable,
		FeRootFiles,

		VcDecompressEntries,
		VcZlibChecksum,
		VcLoadEntries,
		VcInvalidEntryMetadata,
		VcSpriteIssues,
		VcSpriteIssuesRle,
		VcResourcesModelFiles,
		VcResourcesMapFiles,
		VcInvalidQuadTree,
		VcSpriteSoundIndex,
		VcSpriteIndex,
		VcSpriteSoundMissing,
		VcUnknown,

		VeFileNotFound,
		VeDifferentHashValue,
		VeComputeHash,
		VeFilesDifferentSize,
	}
}