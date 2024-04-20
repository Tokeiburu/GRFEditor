using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ErrorManager;
using Encryption;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.LubFormat;
using GRF.FileFormats.RgzFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.ThorFormat;
using GRF.Hash;
using GRF.IO;
using GRF.Image;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;
using Utilities.Hash;
using Utilities.Services;
using Rgz = GRF.FileFormats.Rgz;

namespace GrfCL {
	static partial class GrfCL {
		private static bool? _parseCommand(CommandLineOptions clOption) {
			if (clOption == CommandLineOptions.Help) {
				if (clOption.Args.Count == 0) {
					CLHelper.WriteLine = CommandLineOptions.GetHelp();
				}
				else {
					foreach (string foption in clOption.Args) {
						CLHelper.WriteLine = CommandLineOptions.GetHelp(CommandLineOptions.GetOption(foption));
					}
				}
			}
			else if (clOption == CommandLineOptions.New) {
				if (_grf.IsOpened)
					throw new Exception("-new cannot be used while a GRF is opened.");

				_grf.New();
				CLHelper.Log = "Created a new GRF with the name " + _grf.FileName;
			}
			else if (clOption == CommandLineOptions.Close) {
				CLHelper.Log = "Closed GRF " + _grf.FileName;
				_grf.Close();	// Must be after the log, because _grf.FileName 
				// requires access to the GRF which would be closed.
			}
			else if (clOption == CommandLineOptions.Open) {
				_grf.Open(clOption.Args[0].Contains(".") ? clOption.Args[0] : clOption.Args[0] + ".grf");
				clOption.CheckGrf(_grf);
				CLHelper.Log = "Opened GRF " + (clOption.Args[0].Contains(".") ? clOption.Args[0] : clOption.Args[0] + ".grf");
			}
			else if (clOption == CommandLineOptions.ImageConvert) {
				bool ignored = false;

				if (clOption.Option.OptionalArgs[clOption.FullOptionIds[0]] != null) {
					ignored = Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[0]]);
				}

				Regex regex = new Regex(Methods.WildcardToRegexLine(clOption.Args[1]), RegexOptions.IgnoreCase);

				List<TkPath> paths;

				if (_grf.IsOpened) {
					paths = _grf.FileTable.Files.Where(p => regex.IsMatch(p)).Select(p => new TkPath { FilePath = _grf.FileName, RelativePath = p }).ToList();
				}
				else {
					string inputFile = new FileInfo(clOption.Args[1].Replace('*', '_')).FullName;

					paths = Directory.GetFiles(Path.GetDirectoryName(inputFile), Path.GetFileName(clOption.Args[1])).Select(p => new TkPath { FilePath = p }).ToList();
				}

				foreach (TkPath path in paths) {
					string fullPath = path.GetFullPath();

					try {
						string destinationFolder = new FileInfo(clOption.Args[0]).FullName;
						string destinationFile = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fullPath));

						byte[] data;
						string extension;

						if (path.RelativePath == null) {
							data = File.ReadAllBytes(path.FilePath);
							extension = path.FilePath.GetExtension();
						}
						else {
							data = _grf.FileTable[path.RelativePath].GetDecompressedData();
							extension = path.RelativePath.GetExtension();
						}

						GrfImage imageSource = ImageProvider.GetImage(data, extension);
						imageSource.Save(destinationFile.ReplaceExtension(""), PixelFormatInfo.GetFormatFromAssembly(clOption.Args[2]));
						CLHelper.Log = "File converted successfully : " + Path.GetFileName(fullPath);
					}
					catch (Exception) {
						CLHelper.Warning = "File conversion failed : " + fullPath;

						if (ignored) {
							continue;
						}

						throw;
					}
				}
			}
			else if (clOption == CommandLineOptions.Compression) {
				int compression = Int32.Parse(clOption.Args[0]);

				if (compression > 9 || compression < 0)
					throw new Exception("Compression level must be between 0 and 9");

				Settings.CompressionLevel = compression;
				CLHelper.Log = "Changed compression level to " + clOption.Args[0];
			}
			else if (clOption == CommandLineOptions.GrfInfo) {
				Console.WriteLine(FileFormatParser.DisplayObjectProperties(_grf));
				CLHelper.Log = "Object properties have been displayed";
			}
			else if (clOption == CommandLineOptions.FileInfo) {
				Console.Write(FileFormatParser.DisplayObjectPropertiesFromEntry(_grf, _clOption.Args[0]));
				CLHelper.Log = "Object properties have been displayed";
			}
			else if (clOption == CommandLineOptions.ChangeVersion) {
				switch (clOption.Args[0]) {
					case "0x102":
						_grf.Commands.ChangeVersion(1, 2);
						break;
					case "0x103":
						_grf.Commands.ChangeVersion(1, 3);
						break;
					case "0x200":
						_grf.Commands.ChangeVersion(2, 0);
						break;
					default:
						throw new Exception("Unrecognized version, allowed values are 0x102, 0x103 and 0x200");
				}
				CLHelper.Log = "Version has been changed to " + clOption.Args[0];
			}
			else if (clOption == CommandLineOptions.SaveAs) {
				if (clOption.Args.Count == 0) {
					string randomFile = Path.Combine(Settings.TempPath, Path.GetRandomFileName() + Path.GetExtension(_grf.FileName));
					string oldGrfFilename = _grf.FileName;

					_grf.Save(randomFile, SyncMode.Asynchronous);
					_showProgress(_grf);

					if (_grf.CancelReload) {
						_grf.CancelReload = false;
						throw new Exception("The GRF has cancelled the reload procedure (original file will not be affected).");
					}

					try {
						_grf.Close();
						File.Delete(oldGrfFilename);
						File.Move(randomFile, oldGrfFilename);
						_grf.Open(oldGrfFilename);
						CLHelper.Log = "Saved the GRF to " + _grf.FileName;
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
				else if (clOption.Args.Count == 1) {
					if (!Directory.Exists(Path.GetDirectoryName(clOption.Args[0])) && !String.IsNullOrEmpty(Path.GetDirectoryName(clOption.Args[0])))
						Directory.CreateDirectory(Path.GetDirectoryName(clOption.Args[0]));

					_grf.Save(clOption.Args[0], SyncMode.Asynchronous);
					_showProgress(_grf);
					CLHelper.Log = "Saved the GRF to " + clOption.Args[0];
				}
			}
			else if (clOption == CommandLineOptions.Options) {
				if (clOption.Option.OptionalArgs[clOption.FullOptionIds[0]] != null) {
					_grf.Attached["Thor.UseGrfMerging"] = Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[0]]);
				}

				if (clOption.Option.OptionalArgs[clOption.FullOptionIds[1]] != null) {
					_grf.Attached["Thor.TargetGrf"] = clOption.Option.OptionalArgs[clOption.FullOptionIds[1]];
				}
			}
			else if (clOption == CommandLineOptions.SequenceMode) {
				CLHelper.Log = CLHelper.Indent("Entering the Sequence Mode, type -help for a list of commands or type -exit to leave this mode.", 7, true);
				Encoding enc2 = Console.InputEncoding;
				Console.InputEncoding = Encoding.GetEncoding(1252);

				do {
					Console.Write("Commands> ");
				}
				while (_executeCommands(Console.ReadLine(), false));

				Console.InputEncoding = enc2;
				CLHelper.Log = "Exited the Sequence Mode.";
			}

			else if (clOption == CommandLineOptions.ExtractFiles) {
				List<string> filesToCopy = _grf.FileTable.Files.Where(p => p.IndexOf(clOption.Args[0], StringComparison.InvariantCulture) == 0).ToList();

				// See ExampleProject to see how you can extract the files normally.
				// The GRF library simply offers quicker ways of extracting files and
				// we take advantage of that here.
				_fastExtraction(filesToCopy, clOption.Args[1]);
				CLHelper.Log = "Extracted " + filesToCopy.Count + " file(s)";
			}
			else if (clOption == CommandLineOptions.ExtractGrf) {
				List<string> filesToCopy = _grf.FileTable.Files.ToList();

				if (clOption.Option.OptionalArgs[clOption.FullOptionIds[0]] == null && clOption.Option.OptionalArgs[clOption.FullOptionIds[1]] == null) {
					_fastExtraction(filesToCopy, clOption.Args[0]);
				}
				else {
					if (clOption.Option.OptionalArgs[clOption.FullOptionIds[0]] != null && Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[0]])) {
						_fastExtraction(filesToCopy, Path.GetDirectoryName(_grf.FileName));
					}
					else if (clOption.Option.OptionalArgs[clOption.FullOptionIds[1]] != null && Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[1]])) {
						_fastExtraction(filesToCopy, Path.Combine(Path.GetDirectoryName(_grf.FileName), Path.GetFileNameWithoutExtension(_grf.FileName)));
					}
				}

				CLHelper.Log = "Extracted " + filesToCopy.Count + " file(s)";
			}
			else if (clOption == CommandLineOptions.ExtractFolder) {
				List<string> grfFiles = new List<string>();

				for (int j = 1; j < clOption.Args.Count; j++) {
					grfFiles.Add(clOption.Args[j]);
				}

				_fastExtraction(grfFiles, clOption.Args[0]);
				CLHelper.Log = "Extracted " + grfFiles.Count + " file(s)";
			}
			else if (clOption == CommandLineOptions.ExtractRgz) {
				if (clOption.Option.OptionalArgs[clOption.FullOptionIds[0]] == null && clOption.Option.OptionalArgs[clOption.FullOptionIds[1]] == null) {
					Rgz.ExtractRgz(new ProgressDummy(), clOption.Args[0], clOption.Args[1], false);
					CLHelper.Log = "Extracted RGZ to " + clOption.Args[1];
				}
				else {
					if (clOption.Option.OptionalArgs[clOption.FullOptionIds[0]] != null && Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[0]])) {
						Rgz.ExtractRgz(new ProgressDummy(), clOption.Args[0], Path.GetDirectoryName(clOption.Args[0]), false);
						CLHelper.Log = "Extracted RGZ to " + Path.GetDirectoryName(clOption.Args[0]);
					}
					else if (clOption.Option.OptionalArgs[clOption.FullOptionIds[1]] != null && Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[1]])) {
						Rgz.ExtractRgz(new ProgressDummy(), clOption.Args[0], Path.Combine(Path.GetDirectoryName(clOption.Args[0]), Path.GetFileNameWithoutExtension(clOption.Args[0])), false);
						CLHelper.Log = "Extracted RGZ to " + Path.Combine(Path.GetDirectoryName(clOption.Args[0]), Path.GetFileNameWithoutExtension(clOption.Args[0]));
					}
				}
			}
			else if (clOption == CommandLineOptions.AddFilesOrFolders) {
				_grf.Commands.AddFilesInDirectory(clOption.Args[0], clOption.Args.Skip(1).ToArray());
				CLHelper.Log = "Added files or folders...";
			}
			else if (clOption == CommandLineOptions.RenameFileOrFolder) {
				_grf.Commands.Rename(clOption.Args[0], clOption.Args[1]);
				CLHelper.Log = "Renamed file or folder " + clOption.Args[0] + " to " + clOption.Args[1];
			}
			else if (clOption == CommandLineOptions.Delete) {
				// DeleteFolder also deletes files, so it works well in this scenario
				_clOption.Args.ForEach(p => _grf.Commands.RemoveFolder(p));
				CLHelper.Log = "Deleted files or folders...";
			}
			else if (clOption == CommandLineOptions.Move) {
				_clOption.Args.Skip(1).ToList().ForEach(p => _grf.Commands.Move(p, GrfPath.Combine(clOption.Args[0], Path.GetFileName(p))));
				CLHelper.Log = "Moved files...";
			}
			else if (clOption == CommandLineOptions.ExtractDllInfo) {
				byte[] dll = File.ReadAllBytes(clOption.Args[0]);

				byte[] executableName = new byte[260];
				Buffer.BlockCopy(dll, dll.Length - 260, executableName, 0, 260);

				for (int i = 0; i < executableName.Length; i++) {
					executableName[i] ^= 0xff;
				}

				byte[] fileLength = new byte[4];
				Buffer.BlockCopy(dll, dll.Length - 524, fileLength, 0, 4);

				CLHelper.WriteLine = "Executable name : " + EncodingService.DisplayEncoding.GetString(executableName).TrimEnd('\0');
				CLHelper.WriteLine = "Executable length : " + BitConverter.ToInt32(fileLength, 0) + " bytes";
				CLHelper.WriteLine = "Key file : key.dat";

				byte[] keyId = new byte[4];
				Buffer.BlockCopy(dll, dll.Length - 520, keyId, 0, 4);

				byte[] extendedKey = new byte[256];
				Buffer.BlockCopy(dll, dll.Length - 516, extendedKey, 0, 256);

				int random = BitConverter.ToInt32(keyId, 0);

				for (int i = 0; i < 256; i++) {
					extendedKey[i] = (byte)(extendedKey[i] ^ (random % 256));
					random *= 47;
				}

				File.WriteAllBytes("key.dat", extendedKey);
				File.WriteAllBytes("key2.dat", Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(Ee322.fec67f91f4ef59f498874efbdd21c1c1("test147")));
			}
			else if (clOption == CommandLineOptions.Undo) {
				CLHelper.Log = "The undo has " + (_grf.Commands.Undo() ? "worked" : "failed");
			}
			else if (clOption == CommandLineOptions.Redo) {
				CLHelper.Log = "The redo has " + (_grf.Commands.Redo() ? "worked" : "failed");
			}
			else if (clOption == CommandLineOptions.Merge) {
				if (_grf.IsOpened)
					throw new Exception("-merge requires the GRF to be closed.");

				if (clOption.Args.Count == 2) {
					_merge(clOption.Args[0], clOption.Args[1]);
				}
				else {
					File.Delete(clOption.Args[2]);
					File.Copy(clOption.Args[0], clOption.Args[2]);
					_merge(clOption.Args[2], clOption.Args[1]);
				}
			}
			else if (clOption == CommandLineOptions.MakeGrf) {
				if (_grf.IsOpened)
					throw new Exception("You have opened the GRF with -open; -makeGrf must be used as the first command.");

				_grf.New();
				string[] shellFiles = Directory.GetFiles(clOption.Args[1], "*.*", SearchOption.TopDirectoryOnly);
				string[] shellFolders = Directory.GetDirectories(clOption.Args[1]);
				_grf.Commands.AddFilesInDirectory("data", shellFiles.Concat(shellFolders).ToArray());
				_grf.Save(clOption.Args[0], SyncMode.Asynchronous);
				_showProgress(_grf);
				_grf.Close();
				CLHelper.Log = "GRF " + clOption.Args[0] + " has been made";
			}
			else if (clOption == CommandLineOptions.Patch) {
				GrfHolder newerGrf = new GrfHolder();
				newerGrf.Open(clOption.Args[0]);
				_grf.Patch(newerGrf, clOption.Args[1]);
				newerGrf.Close();
				CLHelper.Log = "GRF " + clOption.Args[1] + " has been made";
			}
			else if (clOption == CommandLineOptions.Encoding) {
				if (_grf.IsOpened)
					throw new Exception("-encoding cannot be used while a GRF is opened.");

				EncodingService.DisplayEncoding = Encoding.GetEncoding(Int32.Parse(clOption.Args[0]));
				CLHelper.Log = CLHelper.Indent("Extraction and files added will now use this encoding : " + EncodingService.DisplayEncoding.WebName, 7, true);
			}
			else if (clOption == CommandLineOptions.Tree) {
				if (clOption.Args.Count == 0)
					_showGrfTree("", false, SearchOption.AllDirectories);
				if (clOption.Args.Count == 1)
					_showGrfTree(clOption.Args[0], false, SearchOption.AllDirectories);
				if (clOption.Args.Count == 2)
					_showGrfTree(clOption.Args[0], Boolean.Parse(clOption.Args[1]), SearchOption.AllDirectories);
				if (clOption.Args.Count == 3)
					_showGrfTree(clOption.Args[0], Boolean.Parse(clOption.Args[1]), clOption.Args[2].ToLower() == "all" ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			}
			else if (clOption == CommandLineOptions.ReadLine) {
				Console.ReadLine();
			}
			else if (clOption == CommandLineOptions.Write) {
				Console.WriteLine(clOption.Args[0]);
			}
			else if (clOption == CommandLineOptions.Cpu) {
				Settings.CpuMonitoringEnabled = Boolean.Parse(clOption.Args[0]);
				if (Settings.CpuMonitoringEnabled) {
					CLHelper.Log = "Starting CPU performance management service...";
					CpuPerformance.GetCurrentCpuUsage();
				}
				else
					CLHelper.Log = "CPU performance management service has been disabled...";
			}
			else if (clOption == CommandLineOptions.Beep) {
				if (clOption.Args.Count == 0)
					Console.Beep();
				if (clOption.Args.Count == 1)
					Console.Beep(Int32.Parse(clOption.Args[0]), 100);
				if (clOption.Args.Count == 2)
					Console.Beep(Int32.Parse(clOption.Args[0]), Int32.Parse(clOption.Args[1]));
			}
			else if (clOption == CommandLineOptions.ShellOpen) {
				if (clOption.Args.Count == 0)
					if (_grf.IsClosed)
						throw new Exception("The GRF must be opened before using this command.");
					else
						OpeningService.FileOrFolder(_grf.FileName);
				if (clOption.Args.Count == 1)
					OpeningService.FileOrFolder(clOption.Args[0]);
			}
			else if (clOption == CommandLineOptions.BreakOnExceptions) {
				_breakOnExceptions = Boolean.Parse(clOption.Args[0]);
				if (clOption.Args.Count == 2)
					BasicErrorHandler.BreakOnExceptions = Boolean.Parse(clOption.Args[1]);

				CLHelper.Log = "Breaking on general exceptions = " + _breakOnExceptions;

				if (clOption.Args.Count == 2)
					CLHelper.Log = "Redirecting GRF exceptions = " + Boolean.Parse(clOption.Args[1]);
			}
			else if (clOption == CommandLineOptions.ExitOnExceptions) {
				BasicErrorHandler.ExitOnExceptions = Boolean.Parse(clOption.Args[0]);
				CLHelper.Log = "Exiting on exceptions : " + Boolean.Parse(clOption.Args[0]);
			}
			else if (clOption == CommandLineOptions.Log) {
				CLHelper.SetLogState(Boolean.Parse(clOption.Args[0]));
				CLHelper.Log = "Logger is " + (Boolean.Parse(clOption.Args[0]) ? "enabled" : "disabled");
			}
			else if (clOption == CommandLineOptions.Timer) {
				int id = clOption.Args.Count == 2 ? Int32.Parse(clOption.Args[1]) : 0;
				if (clOption.Args[0].ToLower() == "stop")
					Z.Stop(id);
				else if (clOption.Args[0].ToLower() == "start")
					Z.Start(id);
				else
					throw new Exception("<1> Unexpected value, use \"stop\" or \"start\".");
			}
			else if (clOption == CommandLineOptions.Break) {
				Break();
			}
			else if (clOption == CommandLineOptions.ExitMode) {
				return false;
			}
			else if (clOption == CommandLineOptions.Exit) {
				return false;
			}
			else if (clOption == CommandLineOptions.ThorPack) {
				string nonPackedPath = clOption.Args[0];
				string packedPath = clOption.Args[1];
				string pathConfig = clOption.Args[2];

				byte[] nonPackedThor = File.ReadAllBytes(nonPackedPath);

				GrfHolder grf = new GrfHolder("tmp.grf", GrfLoadOptions.New);

				grf.Commands.AddFile("config", GrfPath.Combine(pathConfig, "config.ini"));
				grf.Commands.AddFile("LanguageMap.ini", GrfPath.Combine(pathConfig, "LanguageMap.ini"));

				for (int i = 3; i < clOption.Args.Count; i++) {
					var subFolder = new DirectoryInfo(GrfPath.Combine(pathConfig, clOption.Args[i])).FullName.Replace("/", "\\");
					var toRemove = GrfPath.GetDirectoryName(subFolder).Replace("/", "\\");

					foreach (string file in Directory.GetFiles(subFolder, "*", SearchOption.AllDirectories)) {
						var tFile = file.Replace("/", "\\");
						var tSub = tFile.Substring(toRemove.Length + 1);
						grf.Commands.AddFile(tSub, tFile);
					}
				}

				grf.Attached["Thor.PackFormat"] = 1;
				grf.Attached["Thor.PackOffset"] = nonPackedThor.Length;

				string thorPath = TemporaryFilesManager.GetTemporaryFilePath("pack_thor_{0:0000}.thor");

				Thor.SaveFromGrf(grf, thorPath);

				byte[] data = File.ReadAllBytes(thorPath);
				Buffer.BlockCopy(nonPackedThor, 0, data, 0, nonPackedThor.Length);

				File.WriteAllBytes(packedPath, data);
			}
			else if (clOption == CommandLineOptions.HashFolder) {
				FolderHash hashFolder = new FolderHash();
				HashObject obj = hashFolder.HashFolder(String.IsNullOrEmpty(clOption.Args[0]) ? Directory.GetCurrentDirectory() : clOption.Args[0], String.IsNullOrEmpty(clOption.Args[1]) ? "*" : "", clOption.Args[2], HashStrategy.Quick);
				CLHelper.Log = obj.NumberOfFilesHashed + " files hashed";
				CLHelper.Log = "Data output file : " + clOption.Args[2];
			}
			else if (clOption == CommandLineOptions.HashCompare) {
				HashObject master = new HashObject(clOption.Args[0]);
				HashObject client = new HashObject(clOption.Args[1]);

				var issues = HashObjectComparer.GetIssues(master, client, false);

				CLHelper.Log = "Start of issues : ";
				foreach (var issue in issues) {
					CLHelper.WL = issue.ErrorToolTip;
				}
				CLHelper.Log = issues.Count + " issues";
			}
			else if (clOption == CommandLineOptions.ActToGif) {
				Regex regex = new Regex(Methods.WildcardToRegexLine(clOption.Args[1]), RegexOptions.IgnoreCase);

				List<TkPath> paths;

				if (_grf.IsOpened) {
					paths = _grf.FileTable.Files.Where(p => regex.IsMatch(p)).Select(p => new TkPath { FilePath = _grf.FileName, RelativePath = p }).ToList();
				}
				else {
					string inputFile = new FileInfo(clOption.Args[1].Replace('*', '_')).FullName;

					paths = Directory.GetFiles(Path.GetDirectoryName(inputFile), Path.GetFileName(clOption.Args[1])).Select(p => new TkPath { FilePath = p }).ToList();
				}

				foreach (TkPath path in paths) {
					string fullPath = path.GetFullPath();
					List<string> extra = new List<string>();
					string destinationFolder = clOption.Args[0];
					string destinationFile = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fullPath) + ".gif");

					if (clOption.Option.OptionalArgs[clOption.FullOptionIds[6]] != null && Boolean.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[6]])) {
						if (path.RelativePath == null) {
							if (!File.Exists(path.FilePath.ReplaceExtension(".spr"))) {
								CLHelper.Warning = "File skipped (no SPR matching file) : " + fullPath;
								continue;
							}
						}
						else {
							if (!_grf.FileTable.ContainsFile(path.RelativePath.ReplaceExtension(".spr"))) {
								CLHelper.Warning = "File skipped (no SPR matching file) : " + fullPath;
								continue;
							}
						}
					}

					Act act;

					if (path.RelativePath == null) {
						act = new Act(File.ReadAllBytes(path.FilePath), new Spr(File.ReadAllBytes(path.FilePath.ReplaceExtension(".spr"))));
					}
					else {
						act = new Act(
							_grf.FileTable[path.RelativePath].GetDecompressedData(),
							new Spr(_grf.FileTable[path.RelativePath.ReplaceExtension(".spr")].GetDecompressedData()));
					}

					int actionIndex = Int32.Parse(clOption.Args[2]);
					if (clOption.Option.OptionalArgs[clOption.FullOptionIds[7]] != null) {
						float scale = Single.Parse(clOption.Option.OptionalArgs[clOption.FullOptionIds[7]], CultureInfo.InvariantCulture);

						float maxScaleX = act[actionIndex].Frames.Max(p => p.Layers.Max(q => q.ScaleX));
						float maxScaleY = act[actionIndex].Frames.Max(p => p.Layers.Max(q => q.ScaleY));

						float maxScale = Math.Max(maxScaleX, maxScaleY);
						float multiplyingFactor = scale == -1 ? 1 / maxScale : scale;

						if (scale == -2) {
							if (act[actionIndex].Frames.All(p => p.Layers.All(q => q.ScaleX == maxScaleX)) &&
							    act[actionIndex].Frames.All(p => p.Layers.All(q => q.ScaleY == maxScaleY))) {
								multiplyingFactor = 1 / maxScale;

								if (multiplyingFactor != 1) {
									CLHelper.Log = "Applying multiplying factor of " + multiplyingFactor +
									               " on sprite " + Path.GetFileNameWithoutExtension(fullPath);

									act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.ScaleX *= multiplyingFactor));
									act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.ScaleY *= multiplyingFactor));
									act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.OffsetX = (int)(q.OffsetX * multiplyingFactor)));
									act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.OffsetY = (int)(q.OffsetY * multiplyingFactor)));
								}
							}
						}
						else if (multiplyingFactor != 1) {
							CLHelper.Log = "Applying multiplying factor of " + multiplyingFactor +
							               " on sprite " + Path.GetFileNameWithoutExtension(fullPath);

							act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.ScaleX *= multiplyingFactor));
							act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.ScaleY *= multiplyingFactor));
							act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.OffsetX = (int)(q.OffsetX * multiplyingFactor)));
							act[actionIndex].Frames.ForEach(p => p.Layers.ForEach(q => q.OffsetY = (int)(q.OffsetY * multiplyingFactor)));
						}
					}

					extra.Add("background");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[0]]);

					extra.Add("indexFrom");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[1]]);

					extra.Add("indexTo");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[2]]);

					extra.Add("uniform");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[3]]);

					extra.Add("guideLinesColor");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[4]]);

					extra.Add("scaling");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[5]]);

					extra.Add("delay");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[8]]);

					extra.Add("delayFactor");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[9]]);

					extra.Add("margin");
					extra.Add(clOption.Option.OptionalArgs[clOption.FullOptionIds[10]]);

					ProgressDummy dum = new ProgressDummy();
					dum.Progress = -1;

					new GrfThread(() => ActImaging.Imaging.SaveAsGif(destinationFile, act, actionIndex, dum, extra.ToArray()),
						dum, 200, null, true, true).Start();

					while (dum.Progress < 100) {
						CLHelper.Progress = dum.Progress;
						Thread.Sleep(200);
					}
					CLHelper.ProgressEnded();
				}
			}
			else if (clOption == CommandLineOptions.AddFakeClientInfo) {
				_generateHashData("def.txt");
			}
			else if (clOption == CommandLineOptions.RebuildQuadtree) {
				string mapName = Path.GetFileNameWithoutExtension(new FileInfo(clOption.Args[0]).FullName);
				string folder = Path.GetDirectoryName(new FileInfo(clOption.Args[0]).FullName);
				Rsw rsw = new Rsw(File.ReadAllBytes(Path.Combine(folder, mapName + ".rsw")));
				Gnd gnd = new Gnd(File.ReadAllBytes(Path.Combine(folder, mapName + ".gnd")));

				if (!rsw.Header.IsCompatibleWith(2, 1))
					rsw.Header.SetVersion(2, 1);

				rsw.RebuildQuadTree(_grf, gnd.Header.Width, gnd.Header.Height, gnd, clOption.Args.Count == 3 ? Single.Parse(clOption.Args[2], CultureInfo.InvariantCulture) : 200f);
				GrfPath.CreateDirectoryFromFile(Path.Combine(clOption.Args[1], mapName + ".rsw"));
				rsw.Save(Path.Combine(clOption.Args[1], mapName + ".rsw"));
				CLHelper.Log = "Rebuilt the quadtree for the map " + mapName;
			}
			else if (clOption == CommandLineOptions.PrintQuadtree) {
				string filename = new FileInfo(clOption.Args[0]).FullName;
				Rsw rsw = new Rsw(File.ReadAllBytes(filename));
				rsw.PrintQuadTree(clOption.Args[1]);
				CLHelper.Log = "Printed the quadtree for the map " + filename;
			}
			else if (clOption == CommandLineOptions.AnalyseRgz) {
				string filename = new FileInfo(clOption.Args[0]).FullName;

				ByteReaderStream stream = new ByteReaderStream(filename);
				string temporaryFile = TemporaryFilesManager.GetTemporaryFilePath("temp_rgz_{0:0000}.dat");

				Compression.GZipDecompress(new ProgressDummy(), stream, temporaryFile);
				ByteReaderStream dataReader = new ByteReaderStream(temporaryFile);

				while (dataReader.CanRead) {
					char entryType = dataReader.Char();

					Console.Write("Entry type : " + entryType);

					switch(entryType) {
						case 'f':
							RgzEntry entry = new RgzEntry(dataReader);
							Console.Write(" - [" + entry.RelativePath + "], Size = " + entry.SizeDecompressed + " bytes");
							break;
						case 'd':
							int size = dataReader.Byte();
							string path = dataReader.StringANSI(size);
							int indexOf = path.IndexOf('\0');

							if (indexOf > 0) {
								path = path.Substring(0, indexOf);
							}

							Console.Write(" - [ignored:" + path + "], Size = " + size + " bytes");
							break;
						case 'e':
							int size2 = dataReader.Byte();
							string path2 = dataReader.StringANSI(size2);
							Console.Write(" - END [" + path2 + "]");
							break;
					}

					Console.WriteLine();
				}

				CLHelper.Log = "Printed the RGZ structure " + filename;
			}
			else if (clOption == CommandLineOptions.RemoveQuadtree) {
				string filename = new FileInfo(clOption.Args[0]).FullName;
				string folder = new DirectoryInfo(clOption.Args[1]).FullName;

				Rsw rsw = new Rsw(File.ReadAllBytes(filename));
				rsw.Header.SetVersion(1, 9);
				rsw.Save(Path.Combine(folder, Path.GetFileName(filename)));
				CLHelper.Log = "Map has been downgraded to 0x109 : " + Path.GetFileName(filename);
			}
			else if (clOption == CommandLineOptions.GzipCompression) {
				string fileToCompress = new FileInfo(clOption.Args[0]).FullName;
				string fileOutput = new FileInfo(clOption.Args[1]).FullName;

				byte[] fileToCompressData = File.ReadAllBytes(fileToCompress);
				byte[] fileOutputData = GZip.Compress(fileToCompressData);

				File.WriteAllBytes(fileOutput, fileOutputData);
				CLHelper.Log = "Compressed file " + Path.GetFileName(fileToCompress) + " to " + fileOutput;
			}
			else if (clOption == CommandLineOptions.GzipDecompression) {
				string fileToCompress = new FileInfo(clOption.Args[0]).FullName;
				string fileOutput = new FileInfo(clOption.Args[1]).FullName;

				byte[] fileToDecompressData = File.ReadAllBytes(fileToCompress);
				byte[] fileOutputData = GZip.Decompress(fileToDecompressData);

				File.WriteAllBytes(fileOutput, fileOutputData);
				CLHelper.Log = "Decompressed file " + Path.GetFileName(fileToCompress) + " to " + fileOutput;
			}
			else if (clOption == CommandLineOptions.ZlibCompression) {
				string fileToCompress = new FileInfo(clOption.Args[0]).FullName;
				string fileOutput = new FileInfo(clOption.Args[1]).FullName;

				byte[] fileToCompressData = File.ReadAllBytes(fileToCompress);
				byte[] fileOutputData = Compression.Compress(fileToCompressData);

				File.WriteAllBytes(fileOutput, fileOutputData);
				CLHelper.Log = "Compressed file " + Path.GetFileName(fileToCompress) + " to " + fileOutput;
			}
			else if (clOption == CommandLineOptions.ZlibDecompression) {
				string fileToCompress = new FileInfo(clOption.Args[0]).FullName;
				string fileOutput = new FileInfo(clOption.Args[1]).FullName;

				byte[] fileToDecompressData = File.ReadAllBytes(fileToCompress);
				byte[] fileOutputData = Compression.Decompress(fileToDecompressData, 0);

				File.WriteAllBytes(fileOutput, fileOutputData);
				CLHelper.Log = "Decompressed file " + Path.GetFileName(fileToCompress) + " to " + fileOutput;
			}
			else if (clOption == CommandLineOptions.Version) {
				Assembly assembly = Assembly.GetExecutingAssembly();
				Console.WriteLine("Current assembly version : " + assembly.GetName().Version);
				Console.WriteLine("Current assembly file version : " + FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion);
			}
			else if (clOption == CommandLineOptions.LubDecompile) {
				string headDirectory = Path.GetFullPath(Path.GetDirectoryName(clOption.Args[0])) + "\\";
				string outputFolder = clOption.Args[1];
				bool directoryOutput = Path.GetFileName(outputFolder).GetExtension() == null;
				byte[] data = new byte[] { };

				foreach (TkPath path in GrfPath.GetTkPaths(_grf, clOption.Args[0])) {
					try {
						data = GrfPath.GetData(path, _grf, GrfPath.OpenMode.FileAndContainer);
						Lub decompiler = new Lub(data);

						if (directoryOutput) {
							string file = GrfPath.Combine(outputFolder, Path.GetDirectoryName(path.GetMostRelative().ReplaceFirst(headDirectory, "")), path.FileName.ReplaceExtension(".lua"));
							GrfPath.CreateDirectoryFromFile(file);
							File.WriteAllText(file, decompiler.Decompile());
						}
						else {
							File.WriteAllText(clOption.Args[1], decompiler.Decompile());
						}
					}
					catch (Exception err) {
						if (directoryOutput) {
							// We write it anyway!
							string file = GrfPath.Combine(outputFolder, Path.GetDirectoryName(path.GetMostRelative().ReplaceFirst(headDirectory, "")), path.FileName.ReplaceExtension(".lua"));
							GrfPath.CreateDirectoryFromFile(file);
							File.WriteAllBytes(file, data);
							CLHelper.Warning = "File '" + path.GetFullPath() + "' is being copied directly (failed to decompile).\r\n" + err.Message;
						}
						else {
							CLHelper.Error = "File '" + path.GetFullPath() + "'\r\n" + err.Message;
						}
					}
				}

				CLHelper.Log = "All data has been decompiled.";
			}
			else if (clOption == CommandLineOptions.CompareFolder) {
				Func<string, string, string> removeHead = (input, head) => input.ReplaceFirst(head, "");
				Func<string, string, string> keepParentHead = (input, head) => GrfPath.Combine(GrfPath.GetSingleName(head, -1), removeHead(input, head));

				string headDirectory1 = clOption.Args[0].TrimEnd('\\') + "\\";
				string headDirectory2 = clOption.Args[1].TrimEnd('\\') + "\\";

				Func<string, string> toFile2 = file1 => headDirectory2 + removeHead(file1, headDirectory1);

				HashSet<string> files1 = new HashSet<string>(Directory.GetFiles(clOption.Args[0], "*", SearchOption.AllDirectories));
				HashSet<string> files2 = new HashSet<string>(Directory.GetFiles(clOption.Args[1], "*", SearchOption.AllDirectories));

				CLHelper.Log = String.Format("Comparing {0} files with {1} files.", files1.Count, files2.Count);
				int indent = 5;

				foreach (string file1 in files1) {
					string file2 = toFile2(file1);

					if (files2.Contains(file2)) {
						files2.Remove(file2);

						if (!Md5Hash.Compare(file1, file2)) {
							CLHelper.WriteLine = CLHelper.Fill(' ', indent) + CLHelper.Indent(String.Format("#Files '{0}' and '{1}' have a different hashes.", keepParentHead(file1, headDirectory1), keepParentHead(file2, headDirectory2)), indent, true, true);
							File.Delete(file2.ReplaceExtension("_.lua"));
							File.Copy(file1, file2.ReplaceExtension("_.lua"));
						}
					}
					else {
						CLHelper.WriteLine = CLHelper.Fill(' ', indent) + CLHelper.Indent("#File not found '" + keepParentHead(file2, headDirectory2) + "'", indent, true, true);
					}
				}

				foreach (string file2 in files2) {
					CLHelper.WriteLine = CLHelper.Fill(' ', indent) + CLHelper.Indent("#Extra file '" + keepParentHead(file2, headDirectory2) + "'", indent, true, true);
				}

				CLHelper.Log = "All files have been compared.";
			}
			else if (clOption == CommandLineOptions.SetEncryptionKey) {
				_grf.Header.SetKey(Ee322.fc598f9d7ea7a3dfb74fd71f285c0d77(File.ReadAllBytes(clOption.Args[0])), _grf);
				CLHelper.Log = "Encryption key set.";
			}
			else if (clOption == CommandLineOptions.Encrypt) {
				if (_grf.Header.EncryptionKey == null)
					throw new Exception("Encryption key file has not been set. Use the -setKey command first.");

				_grf.Header.SetEncryption(_grf.Header.EncryptionKey, _grf);
				CLHelper.Log = "All file entries have been encrypted.";
			}
			else if (clOption == CommandLineOptions.Decrypt) {
				if (_grf.Header.EncryptionKey == null)
					throw new Exception("Encryption key file has not been set. Use the -setKey command first.");

				_grf.Header.SetDecryption(_grf.Header.EncryptionKey, _grf);
				CLHelper.Log = "All file entries have been decrypted.";
			}
			else {
				return null;
			}

			return true;
		}
	}
}
