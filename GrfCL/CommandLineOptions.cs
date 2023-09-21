using System;
using System.Collections.Generic;
using System.Linq;
using GRF.Core;
using GrfToWpfBridge;
using Utilities;
using Utilities.CommandLine;

namespace GrfCL {
	/// <summary>
	/// See the CommandLineOptions constructor for the commands parameters
	/// These are used 
	/// </summary>
	public sealed class CommandLineOptions {
		private static readonly List<CommandLineOptions> _options = new List<CommandLineOptions>();

		// General
		public static readonly CommandLineOptions Help = new CommandLineOptions(
			"help,h", "Displays help for a command line option.",
			0, Int32.MaxValue, false
		);
		public static readonly CommandLineOptions Open = new CommandLineOptions(
			"open,o", "Opens a GRF.",
			1, 1, false,
			"Path of the GRF to open."
		);
		public static readonly CommandLineOptions New = new CommandLineOptions(
			"new,n", "Creates a new GRF with the default file name \"new.grf\", use -save to change it.",
			0, 0, false
		);
		public static readonly CommandLineOptions Close = new CommandLineOptions(
			"close", "Closes an opened GRF.",
			0, 0, true
		);
		public static readonly CommandLineOptions Exit = new CommandLineOptions(
			"exit", "Exits the application.",
			0, 0, false
		);

		// Global options
		public static readonly CommandLineOptions Compression = new CommandLineOptions(
			"compression,c", "Changes the compression level of the GRF for the repacking procedures.",
			1, 1, false,
			"Compression level, between 0 and 9."
		);
		public static readonly CommandLineOptions GrfInfo = new CommandLineOptions(
			"grfInfo,info,i", "Displays information about the opened GRF.",
			0, 0, true
		);
		public static readonly CommandLineOptions FileInfo = new CommandLineOptions(
			"fileInfo,fI", "Displays information about a file in the GRF.",
			1, 1, true,
			"GRF path of the file (ex: data\\test.txt)"
		);
		public static readonly CommandLineOptions ChangeVersion = new CommandLineOptions(
			"changeVersion,cV", "Changes the version of the GRF.",
			1, 1, true,
			"Version of the GRF (0x102, 0x103 or 0x200)"
		);
		public static readonly CommandLineOptions SaveAs = new CommandLineOptions(
			"saveAs,save,s", "Saves an opened GRF.",
			0, 1, true,
			"Save path of the GRF (using the same filename as the opened GRF will overwrite it)."
		);

		// Batch mode
		public static readonly CommandLineOptions SequenceMode = new CommandLineOptions(
			"sequenceMode,sM", "Enters the Sequence Mode. " +
			                   "This mode allows you to enter a sequence of commands manually, which is " +
			                   "useful if you want to modify a GRF by yourself instead of using a batch file.",
			0, 0, false
		);
		public static readonly CommandLineOptions ExitMode = new CommandLineOptions(
			"exitMode,eM", "Exits the Sequence Mode if it's running.",
			0, 0, false
		);

		// Extraction
		public static readonly CommandLineOptions ExtractFiles = new CommandLineOptions(
			"extractFiles,eFiles", "Extracts all files of a node in the GRF to a specified folder.",
			2, 2, true,
			"GRF path of a folder (ex: data\\texture).",
			"Path of the extraction folder (ex: C:\\Game\\data)"
		);
		public static readonly CommandLineOptions ExtractGrf = new CommandLineOptions(
			"extractGrf,eGrf", "Extracts a GRF file.",
			1, 3, true,
			new string[] {
				"Path of the extraction folder (ex: C:\\Game\\MyRO).",
				"Extracts the files where the container is located.",
				"Extracts the files where the container is located using the filename as the base directory."
			},
			"outputFromContainerPath,outputUsingFilename"
		);
		public static readonly CommandLineOptions ExtractFolder = new CommandLineOptions(
			"extractFolder,eFolder", "Extracts files inside the GRF to a specified folder.",
			2, Int32.MaxValue, true,
			"Path of the extraction folder (ex: C:\\Game\\data).",
			"GRF paths of the files (ex: data\\test.txt)."
		);
		public static readonly CommandLineOptions ExtractRgz = new CommandLineOptions(
			"extractRgz,eRgz", "Extracts a RGZ file.",
			2, 4, false,
			new string[] {
				"Path of the RGZ to extract.",
				"Path of the extraction folder (ex: C:\\Game\\MyRO).",
				"Extracts the files where the container is located (true or false, false by default).",
				"Extracts the files where the container is located using the filename as the base " +
				"directory (true or false, false by default)."
			},
			"outputFromContainerPath,outputUsingFilename"
		);

		// File manipulations
		public static readonly CommandLineOptions AddFilesOrFolders = new CommandLineOptions(
			"add", "Adds files or folders to an opened GRF (all subfiles in folders are added as well).",
			2, Int32.MaxValue, true,
			"GRF path to add files or folders to (ex: data\\texture).",
			"List of files or folders to add."
		);
		public static readonly CommandLineOptions RenameFileOrFolder = new CommandLineOptions(
			"rename", "Renames a file or a folder inside the GRF.",
			2, 2, true,
			"GRF path of the file or folder.",
			"New GRF path of the file or folder."
		);
		public static readonly CommandLineOptions Delete = new CommandLineOptions(
			"delete,del", "Deletes files or folders to an opened GRF.",
			1, Int32.MaxValue, true,
			"List of GRF files or folders to delete (ex: data\\texture data\\test.txt)."
		);
		public static readonly CommandLineOptions Move = new CommandLineOptions(
			"move,moveFiles,mF", "Moves a list of files to a GRF path.",
			2, Int32.MaxValue, true,
			"GRF path of the folder.",
			"GRF paths of the files."
		);

		// Encryption
		public static readonly CommandLineOptions SetEncryptionKey = new CommandLineOptions(
			"setKey", "Sets the encryption key.",
			1, 1, true,
			"Encryption key file path."
		);

		public static readonly CommandLineOptions Encrypt = new CommandLineOptions(
			"encrypt", "Encrypts an entire GRF or a Thor file. An encryption key must be set with -setKey.",
			0, 0, true
		);

		public static readonly CommandLineOptions Decrypt = new CommandLineOptions(
			"decrypt", "Decrypts an entire GRF or a Thor file. An encryption key must be set with -setKey.",
			0, 0, true
		);

		public static readonly CommandLineOptions ExtractDllInfo = new CommandLineOptions(
			"extractDllInfo,eDllInfo", "Extracts information from the DLL.",
			1, 1, false,
			"Path of the DLL."
		);

		// Others
		public static readonly CommandLineOptions Undo = new CommandLineOptions(
			"undo", "Undo the latest operation if possible.",
			0, 0, true
		);
		public static readonly CommandLineOptions Redo = new CommandLineOptions(
		  "redo", "Redo the latest opration if possible.",
		  0, 0, true
		);
		public static readonly CommandLineOptions Merge = new CommandLineOptions(
			"merge,m", "Merges two GRFs together, use the third argument to save the result elsewhere. " +
			           "There should be no opened GRF.",
			2, 3, false,
			"Path of the base GRF.",
			"Path of the GRF with files to add to the base GRF.",
			"Path of the output GRF, the default value is the base GRF."
		);
		public static readonly CommandLineOptions MakeGrf = new CommandLineOptions(
			"makeGrf,mGrf", "Makes a GRF from a folder (a data path is created).",
			2, 2, false,
			"Path of the GRF to save the file to (ex: C:\\myGrf.grf).",
			"Path of the data folder (ex: C:\\Games\\MyRO\\data)."
		);
		public static readonly CommandLineOptions Patch = new CommandLineOptions(
			"patch,p", "Makes a GRF by substracting a recent GRF on the opened GRF. " +
			           "It uses MD5 to compare each file. This command closes the opened GRF.",
			2, 2, true,
			"Path of the recent (newer) GRF.",
			"Path of the resulting GRF."
		);
		public static readonly CommandLineOptions Tree = new CommandLineOptions(
			"tree,t", "Displays the structure of the GRF in a tree view mode.",
			0, 3, true,
			"GRF path of a folder (displays the tree from a specific folder).",
			"Displays files (true or false).",
			"Search option (top or all)."
		);
		public static readonly CommandLineOptions ReadLine = new CommandLineOptions(
			"readLine", "Reads characters until the return key is pressed " +
			            "(use -break if you just want to pause the application).",
			0, 0, false
		);
		public static readonly CommandLineOptions Write = new CommandLineOptions(
			"write", "Displays a message on the console.",
			1, 1, false,
			"Message to be displayed."
		);
		public static readonly CommandLineOptions Cpu = new CommandLineOptions(
			"cpuPerf,cpu", "Enables or disables the CPU performance management.",
			1, 1, false,
			"Service enabled (true or false)."
		);
		public static readonly CommandLineOptions Beep = new CommandLineOptions(
			"beep", "Makes a system sound (can be used to tell when an operation has finished).",
			0, 2, false,
			"Frequency of the sound (a good value is around 900).",
			"Duration of the sound, in ms (a good value is around 300)."
		);
		public static readonly CommandLineOptions ShellOpen = new CommandLineOptions(
			"shellOpen", "Opens an explorer window and select the file (or folder).",
			0, 1, false,
			"Path of the GRF to open (leave empty to open the currently opened GRF)."
		);
		public static readonly CommandLineOptions BreakOnExceptions = new CommandLineOptions(
			"breakOnExceptions", "Requires a key press on exceptions.",
			1, 2, false,
			"Breaks on general exceptions (true or false).",
			"Breaks on GRF exceptions (true or false, false by default)."
		);
		public static readonly CommandLineOptions ExitOnExceptions = new CommandLineOptions(
			"exitOnExceptions", "Terminates the application on any exception.",
			1, 1, false,
			"Exits on any exception (true or false, false by default)."
		);
		public static readonly CommandLineOptions Log = new CommandLineOptions(
			"log", "Enables or disables the logger.",
			1, 1, false,
			"Logger state (true or false, true by default)."
		);
		public static readonly CommandLineOptions Break = new CommandLineOptions(
			"break", "Breaks and wait for a keystroke.",
			0, 0, false
		);
		public static readonly CommandLineOptions Encoding = new CommandLineOptions(
			"encoding,enc", "The encoding option only works for extracting or adding files, " +
							"it won't change the display of the console.",
			1, 1, false
		);
		public static readonly CommandLineOptions Timer = new CommandLineOptions(
			"timer,stopwatch", "Starts or stops a stopwatch and displays the result on the console.",
			1, 2, false,
			"Starts or stops the timer (start or stop).",
			"Timer ID, put any integer value that will be used to identify the timer."
		);
		public static readonly CommandLineOptions ThorPack = new CommandLineOptions(
			"thorPack", "Packs configuration files to Thor Patcher's executable.",
			4, Int32.MaxValue, false,
			"Path of the non-packed Thor Patcher.",
			"Path of the output Thor Patcher.",
			"Path where the config.ini file is located. Leave empty to use the current directory.",
			"Directories to pack. Example : \"images\" \"BGM\""
		);
		public static readonly CommandLineOptions ThorUnpack = new CommandLineOptions(
			"thorUnpack", "Retrieves the configuration files from a packed Thor Patcher.",
			1, 1, false,
			"Path of the Thor Patcher."
		);
		public static readonly CommandLineOptions ActToGif = new CommandLineOptions(
			"gif,actToGif", "Creates a GIF animation file from an ACT file (SPR is required as well).",
			3, 14, false,
			new string[] {
				"Path of the destination folder of the GIF file (ex: C:\\gifs\\).",
				"Path of the ACT file (if a GRF is opened, it will look for the ACT in it first).",
				"Action index (0 based, an animation (idle, walking, attacking, etc) has 8 actions).",
				"Background color used, ARGB based (default value is white, ex: #FFFFFFFF).",
				"Frame base index (default value is 0).",
				"Frame end index (default value is -1, meaning the end frame).",
				"Uniform the frames (default value is true).",
				"Guidelines color used, ARGB based (default value is transparent, ex: #00000000).",
				"BitmapScalingMode, will create smoother images depending on setting " +
				"(values are NearestNeighbor or HighQuality, default value is NearestNeighbor " +
				"but it might not be available on Windows XP)",
				"The function won't be executed if the SPR corresponding file is missing (value is true or false, false by default)",
				"Indicates the scale factor. If the value is -1, the sprites will be resized to 100%. If the value is -2, " +
				"only full resized sprites will be scaled down to 100%. " +
				"Other values will be multiplied by the animation's scale.",
				"Overrides the animation speed (value is integer, in milliseconds).",
				"Multiplies the delay by a given factor (value is float).",
				"Adds a margin of x pixels around the image."
			},
			"bColor,fBaseId,fEndId,uniform,gColor,scaling,ignore,fixScale,delay,delayMult,margin"
		);
		public static readonly CommandLineOptions RebuildQuadtree = new CommandLineOptions(
			"rebuildQuadtree,rQuad", "Rebuilds the quadtree for a given map. A GRF must be provided " +
			                         "to retrieve the models (rsm).",
			2, 2, true,
			"Path of the RSW file (the GND file must also be present).",
			"Path of the folder where the new map files will be copied."
		);
		public static readonly CommandLineOptions PrintQuadtree = new CommandLineOptions(
			"printQuadtree,pQuad", "Prints the quadtree for a given map.",
			2, 2, false,
			"Path of the RSW file.",
			"Path of the printed result (.txt format)."
		);
		public static readonly CommandLineOptions RemoveQuadtree = new CommandLineOptions(
			"deleteQuadtree,dQuad", "Removes the quadtree for a given map.",
			2, 2, false,
			"Path of the RSW file.",
			"Path of the folder where the new map file will be copied."
		);
		public static readonly CommandLineOptions GzipCompression = new CommandLineOptions(
			"gzip,cgzip", "Compress a file with GZIP.",
			2, 2, false,
			"Path of the file to compress.",
			"Path of the output file."
		);
		public static readonly CommandLineOptions GzipDecompression = new CommandLineOptions(
			"dgzip,ugzip", "Decompress a file with GZIP.",
			2, 2, false,
			"Path of the file to decompress.",
			"Path of the output file."
		);
		public static readonly CommandLineOptions ZlibCompression = new CommandLineOptions(
			"zlibCompress,zCompress", "Compress a file using the zlib library.",
			2, 2, false,
			"Path of the file to compress.",
			"Path of the output file."
		);
		public static readonly CommandLineOptions ZlibDecompression = new CommandLineOptions(
			"zlibDecompress,zDecompress", "Decompress a file using the zlib library.",
			2, 2, false,
			"Path of the file to decompress.",
			"Path of the output file."
		);
		public static readonly CommandLineOptions AddFakeClientInfo = new CommandLineOptions(
			"addFakeClientinfo", "Redirects the client info.",
			0,0, false,
			"Path of the clientinfo file."
		);
		//public static readonly CommandLineOptions MapGenerator = new CommandLineOptions(
		//	"map,mapgen", "Creates a minimap for a map.",
		//	2, 2, false,
		//	"Path of the map file.",
		//	"Path of the output file."
		//);
		public static readonly CommandLineOptions Version = new CommandLineOptions(
			"version,ver,v", "Displays the current version of this software.",
			0, 0, false
		);
		public static readonly CommandLineOptions AnalyseRgz = new CommandLineOptions(
			"rgzAnalysis,aRgz", "Gives the analysis output of a RGZ file.",
			1, 1, false,
			new string[] {
				"Path of the file to analyse."
			}
		);
		public static readonly CommandLineOptions LubDecompile = new CommandLineOptions(
			"lubDecompile,lub", "Decompiles a .lub files.",
			2, 2, false,
			"Path of the file to decompile.",
			"Path of the output file (default value changes the extension for .lua)."
		);
		public static readonly CommandLineOptions CompareFolder = new CommandLineOptions(
			"compareFolder,compare", "Compares two folders.",
			2, 2, false,
			"Path of the first folder.",
			"Path of the second folder."
		);
		public static readonly CommandLineOptions HashFolder = new CommandLineOptions(
			"hash", "Hashes the content of a folder.",
			3, 3, false,
			"Path of the folder to hash.",
			"Search pattern.",
			"Output file."
		);
		public static readonly CommandLineOptions HashCompare = new CommandLineOptions(
			"hashComp,hashCompare", "Compares two hash files.",
			2, 3, false,
			"Path of the 'server' hash file.",
			"Path of the client hash file.",
			"Path of the repair file."
		);
		public static readonly CommandLineOptions ImageConvert = new CommandLineOptions(
			"imageConvert", "Converts an image to another format.",
			3, 6, false,
			new string[] {
				"Path of the output folder.",
				"Path of the image file (if a GRF is opened, it will look for the file in it first).",
				"Output format (values are " + Methods.Aggregate(PixelFormatInfo.Formats.Select(p => p.AssemblyName).ToList(), "|") + ")",
				"Stops the conversion if an error occurs (value is true or false, false by default)",
				"Sets the transparent pixel index (usually set to 0)",
				"Sets the transparent color (usually is #FFFF00FF for pink)"
			},
			"ignore,transparentIndex,transparentColor"
		);

		/// <summary>
		/// Creates a new option
		/// Prevents a default instance of the <see cref="CommandLineOptions" /> class from being created.
		/// </summary>
		/// <param name="commandName">Name of the command, use comas if you want to connect multiple keywords with the command.</param>
		/// <param name="description">The description of the command.</param>
		/// <param name="minArgument">The mininum number of arguments (0 or more).</param>
		/// <param name="maxArgument">The maximum number of arguments (for a list of arguments, use Int32.MaxValue).</param>
		/// <param name="requiresOpenedGrf">if set to <c>true</c> [requires opened GRF].</param>
		/// <param name="parametersDescription">The parameters description, the descriptions must be of the same number of available parameters.</param>
		private CommandLineOptions(string commandName, string description, int minArgument, int maxArgument, bool requiresOpenedGrf, params string[] parametersDescription) {
			Description = description;
			CommandNames = commandName.Split(',').Select(p => "-" + p).ToList();
			MinArgument = minArgument;
			MaxArgument = maxArgument;
			RequiresOpenedGrf = requiresOpenedGrf;
			ParametersDescription = new List<string>();
			ParametersDescription.AddRange(parametersDescription);
			Args = new List<string>();
			_options.Add(this);
		}

		/// <summary>
		/// Creates a new advanced option
		/// Prevents a default instance of the <see cref="CommandLineOptions" /> class from being created.
		/// </summary>
		/// <param name="commandName">Name of the command, use comas if you want to connect multiple keywords with the command.</param>
		/// <param name="description">The description of the command.</param>
		/// <param name="minArgument">The mininum number of arguments (0 or more).</param>
		/// <param name="maxArgument">The maximum number of arguments (for a list of arguments, use Int32.MaxValue).</param>
		/// <param name="requiresOpenedGrf">if set to <c>true</c> [requires opened GRF].</param>
		/// <param name="parametersDescription">The parameters' description, the descriptions must be of the same number of available parameters.</param>
		/// <param name="optionalIds">These IDs are used to generate fully optionnal parameters, they will be typed as /param= </param>
		private CommandLineOptions(string commandName, string description, int minArgument, int maxArgument, bool requiresOpenedGrf, IEnumerable<string> parametersDescription, string optionalIds) {
			Description = description;
			CommandNames = commandName.Split(',').Select(p => "-" + p).ToList();
			FullOptionIds = optionalIds.Split(',').Select(p => "/" + p + "=").ToList();
			FullOptionIndex = maxArgument - FullOptionIds.Count;
			MinArgument = minArgument;
			MaxArgument = maxArgument;
			RequiresOpenedGrf = requiresOpenedGrf;
			ParametersDescription = new List<string>();
			ParametersDescription.AddRange(parametersDescription);
			Args = new List<string>();
			_options.Add(this);
		}

		public int MinArgument { get; private set; }
		public int MaxArgument { get; private set; }
		public int FullOptionIndex { get; private set; }
		public bool RequiresOpenedGrf { get; private set; }
		public string Description { get; private set; }
		public List<string> Args { get; private set; }
		public List<string> CommandNames { get; private set; }
		public List<string> ParametersDescription { get; private set; }
		public List<string> FullOptionIds { get; private set; }
		public GenericCLOption Option { get; private set; }

		public static CommandLineOptions GetOption(string commandName) {
			foreach (CommandLineOptions opt in _options) {
				if (opt.CommandNames.Contains(commandName) ||
					(!commandName.StartsWith("-") && opt.CommandNames.Contains("-" + commandName))) {
					return opt;
				}
			}
			return null;
		}

		public void Assign(GenericCLOption option, GrfHolder grf) {
			_minMaxNumOfArgs(MinArgument, MaxArgument, option.Args);

			if (RequiresOpenedGrf)
				CheckGrf(grf);

			Args = option.Args;
			Option = option;
		}

		public void CheckGrf(GrfHolder grf) {
			if (grf.IsClosed)
				throw new Exception("The GRF hasn't been opened yet. Use \"-open C:\\example.grf\" as the first option.");

			if (grf.Header == null || grf.Header.FoundErrors) {
				grf.Close();
				throw new Exception("The GRF contains error and it has been closed.");
			}
		}

		public static string GetHelp() {
			string buffer = "";
			foreach (CommandLineOptions command in _options) {
				buffer += GetHelp(command);
			}
			return buffer;
		}

		public static string GetHelp(CommandLineOptions command) {
			if (command == null)
				return "Command not found.";

			string buffer = command.CommandNames.Aggregate((p, n) => p + ", " + n) + 
				Environment.NewLine + CLHelper.Indent(command.Description, 8) + Environment.NewLine;
			for (int index = 0; index < command.ParametersDescription.Count; index++) {
				string help = command.ParametersDescription[index];
				if (index < command.MinArgument && index + 1 == command.MinArgument && command.MaxArgument == Int32.MaxValue) {
					buffer += " <...>  " + CLHelper.Indent(help, 8, true) + Environment.NewLine;
				}
				else if (index < command.MinArgument) {
					buffer += "   <" + (index + 1) + ">  " + CLHelper.Indent(help, 8, true) + Environment.NewLine;
				}
				else {
					buffer += " <opt>  ";

					if (command.FullOptionIds != null && index >= command.FullOptionIndex)
						buffer += command.FullOptionIds[index - command.FullOptionIndex].Remove(command.FullOptionIds[index - command.FullOptionIndex].Length - 1, 1) +
							" " + CLHelper.Fill(' ', command.FullOptionIds.Max(p => p.Length) - command.FullOptionIds[index - command.FullOptionIndex].Length) + CLHelper.Indent(help, 8 + command.FullOptionIds.Max(p => p.Length), true) + Environment.NewLine;
					else
						buffer += CLHelper.Indent(help, 8, true) + Environment.NewLine;
				}
			}

			return buffer + Environment.NewLine;
		}

		public static bool IsHelp(List<GenericCLOption> unknownOptions) {
			return unknownOptions.Count > 0 && GetOption(unknownOptions[0].CommandName) == Help;
		}

		public static List<GenericCLOption> ParseArgumentsForHelp(List<GenericCLOption> unknownOptions, bool ignoreFirst) {
			string command = "";
			foreach (GenericCLOption option in unknownOptions) {
				command += option.CommandName.Replace("-", "") + " ";
				if (option.Args.Count != 0)
					command += option.Args.Aggregate((current, arg) => current + " " + arg.Replace("-", ""));
			}
			return CommandLineParser.GetOptions(command, ignoreFirst);
		}

		private void _minMaxNumOfArgs(int min, int max, List<string> arguments) {
			if (arguments.Count < min)
				throw new Exception("Not enough arguments to complete the command " + CommandNames[0] + Environment.NewLine + GetHelp(this));

			if (arguments.Count > max)
				CLHelper.Warning = "Too many arguments found for the command " + CommandNames[0];
		}
	}
}
