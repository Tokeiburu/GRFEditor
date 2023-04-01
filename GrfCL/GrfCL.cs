using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ErrorManager;
using GRF.Core;
using GRF.Image;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge.Application;
using Utilities.CommandLine;
using Utilities.Services;

namespace GrfCL {
	public static partial class GrfCL {
		private static readonly GrfHolder _grf = new GrfHolder();
		private static bool _breakOnExceptions;
		private static GenericCLOption _clOption;

		public static void Run() {
			ErrorHandler.SetErrorHandler(new BasicErrorHandler());
			ImageConverterManager.AddConverter(new DefaultImageConverter());
			Settings.CpuMonitoringEnabled = false;

			try {
				string commandLine;

				// Detect if input encoding came from the command line
				if (Console.InputEncoding.GetString(Console.InputEncoding.GetBytes(Environment.CommandLine)) == Environment.CommandLine) {
					// If it did, then do nothing
					CLHelper.Log = "Batch file detected an invalid encoding, changing command line arguments encoding to 1252";
					commandLine = EncodingService.DisplayEncoding.GetString(Console.InputEncoding.GetBytes(Environment.CommandLine));
				}
				else {
					// If the encoding is already correct, we keep the command line as it is
					if (EncodingService.Ansi.GetString(EncodingService.Ansi.GetBytes(Environment.CommandLine)) == Environment.CommandLine) {
						//CLHelper.Log = "CMD execution detected, guessing ANSI encoding";
						//commandLine = EncodingService.DisplayEncoding.GetString(EncodingService.ANSI.GetBytes(Environment.CommandLine));
						commandLine = Environment.CommandLine;
					}
					else {
						CLHelper.Log = "Unexpected encoding, assuming batch file and changing encoding to 1252";
						commandLine = EncodingService.DisplayEncoding.GetString(Console.InputEncoding.GetBytes(Environment.CommandLine));
					}
				}

				_executeCommands(commandLine);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				if (_breakOnExceptions)
					Break();
			}
		}

		private static bool _executeCommands(string commandLine, bool ignoreFirst = true) {
			// The arguments are parsed by CommandLineParser
			// You can add multiple arguments if needed; each argument will save the GRF and
			// it would be rather slow to save the GRF everytime a file is added...
			// The format of the commands are :
			// -commandName "quotesRequiredGrfPath" "argument1" -compression 6 -saveAs "output.grf"
			// Type "help" for a list of commands and view the batch files (.bat) for examples on
			// how to create your own 'automated programs'
			List<GenericCLOption> unknownOptions = CommandLineParser.GetOptions(commandLine.Replace(Environment.NewLine, " "), ignoreFirst);

			if (ignoreFirst && unknownOptions.Count == 0) {
				CLHelper.Exception = "The application has been opened with no arguments";
				unknownOptions.Add(new GenericCLOption { CommandName = CommandLineOptions.SequenceMode.CommandNames[0] });
			}

			if (CommandLineOptions.IsHelp(unknownOptions) && unknownOptions[0].Args.Count > 0) {
				unknownOptions = CommandLineOptions.ParseArgumentsForHelp(unknownOptions, ignoreFirst);
			}

			try {
				foreach (GenericCLOption unknownOption in unknownOptions) {
					CommandLineOptions clOption = CommandLineOptions.GetOption(unknownOption.CommandName);
					_clOption = unknownOption;

					if (clOption == null)
						throw new Exception("Command unrecognized " + unknownOption.CommandName);

					clOption.Assign(unknownOption, _grf);

					return _parseCommand(clOption);
				}
			}
			catch (Exception err) {
				CLHelper.Error = "An exception has been thrown";
				CLHelper.Error = "Given command line : " + commandLine;
				CLHelper.Error = "Command being executed : " + _clOption.CommandName + " " + _clOption.Args.Aggregate("", (current, f) => current + " <" + f + ">");
				CLHelper.Exception = err.Message;
				if (_breakOnExceptions)
					Break();
				if (BasicErrorHandler.ExitOnExceptions)
					return false;
			}
			return true;
		}

		private static void _merge(string grfPath1, string grfPath2) {
			GrfHolder grf1 = new GrfHolder();
			grf1.Open(grfPath1);
			GrfHolder grf2 = new GrfHolder();
			grf2.Open(grfPath2);

			grf1.Merge(grf2, SyncMode.Asynchronous);

			_showProgress(grf1);
			CLHelper.Log = "Merged " + grf1.FileName + " with " + grf2.FileName;
			grf1.Close();
			grf2.Close();
		}

		private static void _showProgress(GrfHolder grf) {
			while (grf.IsBusy) {
				CLHelper.Progress = grf.Progress;
				Thread.Sleep(200);
			}
			CLHelper.ProgressEnded();
		}

		private static void _showGrfTree(string grfPath, bool showFiles, SearchOption option) {
			if (grfPath.EndsWith("\\"))
				grfPath = grfPath.Remove(grfPath.Length - 1, 1);

			HashSet<string> files = _grf.FileTable.Files;
			List<string> folders = new List<string>();

			foreach (string file in files.Select(Path.GetDirectoryName).Distinct()) {
				folders.Add(file);
			}

			if (option == SearchOption.AllDirectories)
				folders = folders.Where(p => p.StartsWith(grfPath)).ToList();
			else
				folders = folders.Where(p => Path.GetDirectoryName(p) == grfPath).ToList();

			folders = folders.OrderBy(p => p).ToList();
			List<string> allFolders = new List<string>();

			foreach (string folder in folders) {
				string temp = folder;
				do {
					allFolders.Add(temp);
					temp = Path.GetDirectoryName(temp);
				} while (!String.IsNullOrEmpty(temp));
			}

			Encoding enco = Console.OutputEncoding;
			folders = allFolders.Distinct().OrderBy(p => p).ToList();
			try {
				foreach (string folder in folders) {
					int level = 4 * (folder.Split('\\').Length - 1);

					if (option == SearchOption.TopDirectoryOnly && folder.StartsWith(grfPath) &&
						grfPath.Length > 0 && folder.Replace(grfPath, "").Length > 0)
						continue;

					Console.WriteLine(CLHelper.Indent(Path.GetFileName(folder), level, false));
					if (folder.StartsWith(grfPath) && showFiles) {
						List<string> tFiles = files.Where(p => Path.GetDirectoryName(p) == folder).ToList();
						for (int index = 0; index < tFiles.Count; index++) {
							string file = tFiles[index];
							Console.OutputEncoding = Encoding.GetEncoding(1252);

							Console.Write(CLHelper.Fill(' ', level));

							if (index + 1 == tFiles.Count) {
								Console.Write((char)192);
								Console.Write((char)196);
							}
							else {
								Console.Write((char)195);
								Console.Write((char)196);
							}

							Console.OutputEncoding = enco;
							Console.WriteLine(CLHelper.Indent(Path.GetFileName(file), level, true));
						}
					}
				}
			}
			finally {
				Console.OutputEncoding = enco;
			}
		}

		private static void _fastExtraction(List<string> filesToCopy, string destinationPath) {
			List<FileEntry> nodes = filesToCopy.Select(file => _grf.FileTable[file]).ToList();

			string path = destinationPath.Contains(".") ? Path.GetDirectoryName(destinationPath) : destinationPath;

			if (path == null) {
				return;
			}

			foreach (FileEntry node in nodes) {
				node.ExtractionFilePath = Path.Combine(path, node.RelativePath);
			}

			foreach (string folder in nodes.Select(p => Path.GetDirectoryName(p.ExtractionFilePath)).Distinct()) {
				if (!Directory.Exists(folder))
					new DirectoryInfo(folder).Create();
			}

			int numOfThreads = filesToCopy.Count / 50;
			numOfThreads = numOfThreads <= 0 ? 1 : numOfThreads > Settings.MaximumNumberOfThreads ? Settings.MaximumNumberOfThreads : numOfThreads;

			List<GrfThreadExtract> threadsStreamCopy = new List<GrfThreadExtract>();

			for (int i = 0; i < numOfThreads; i++) {
				int startIndex = (int)(filesToCopy.Count / (float)numOfThreads * i);
				int endIndex = (int)(filesToCopy.Count / (float)numOfThreads * (i + 1));
				var t = new GrfThreadExtract();
				t.Init(_grf, nodes, startIndex, endIndex);
				threadsStreamCopy.Add(t);
			}

			const int DelayThreads = 2;
			for (int index = 0; index < threadsStreamCopy.Count; index++) {
				GrfThreadExtract gsc = threadsStreamCopy[index];
				if (Settings.CpuMonitoringEnabled && index > DelayThreads)
					gsc.IsPaused = true;

				gsc.Start();
			}

			float cpuPerf;
			int overMaxUsage = 0;
			GrfThreadExtract thread;
			Console.CursorVisible = false;
			
			while (threadsStreamCopy.Any(p => !p.Terminated)) {
				Thread.Sleep(300);
				CLHelper.Progress = threadsStreamCopy.Sum(b => b.NumberOfFilesProcessed) / (float)filesToCopy.Count * 100.0f;

				if (Settings.CpuMonitoringEnabled) {
					cpuPerf = CpuPerformance.GetCurrentCpuUsage();

					threadsStreamCopy.Where(p => !p.IsPaused).ToList().ForEach(p => p.IsPaused = false);

					if (cpuPerf < Settings.CpuUsageCritical) {
						thread = threadsStreamCopy.FirstOrDefault(p => p.IsPaused && !p.Terminated);
						if (thread != null) {
							thread.IsPaused = false;
						}
					}
					if (cpuPerf >= Settings.CpuUsageCritical) {
						overMaxUsage++;

						if (overMaxUsage >= 10) {
							overMaxUsage = 0;
							if (threadsStreamCopy.Count(p => !p.IsPaused && !p.Terminated) > 1) {
								thread = threadsStreamCopy.FirstOrDefault(p => !p.IsPaused && !p.Terminated);
								if (thread != null) {
									thread.IsPaused = true;
								}
							}
						}
					}
					else {
						overMaxUsage = 0;
					}

					if (!threadsStreamCopy.Any(p => !p.IsPaused && !p.Terminated)) {
						thread = threadsStreamCopy.FirstOrDefault(p => !p.Terminated);
						if (thread != null) {
							thread.IsPaused = false;
						}
					}
				}
			}

			CLHelper.ProgressEnded();
			Console.CursorVisible = true;
			
			threadsStreamCopy.Clear();
		}

		internal static void Break() {
			Console.Write("#BREAK -- Press any key to continue...");

			// Empty the input buffer
			while (Console.KeyAvailable) {
				Console.ReadKey(true);
			}

			Console.ReadKey(true);
			Console.WriteLine();
		}
	}
}
