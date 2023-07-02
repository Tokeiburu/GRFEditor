using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Markup;
using System.Windows.Media;
using ErrorManager;
using GRF.IO;
using GRF.Image;
using GRF.System;
using GrfToWpfBridge;
using OpenTK;
using Utilities;
using Configuration = TokeiLibrary.Configuration;

namespace GRFEditor.ApplicationConfiguration {
	/// <summary>
	/// Contains all the configuration information
	/// The ConfigAsker shouldn't be used manually to store variable,
	/// make a new property instead. The properties should also always
	/// have a default value.
	/// </summary>
	public static class GrfEditorConfiguration {
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "config.txt"))); }
			set { _configAsker = value; }
		}

		public static Brush UIPanelPreviewBackground {
			get { return (Brush) XamlReader.Parse(ConfigAsker["[Style - Panel preview background]", XamlWriter.Save(new SolidColorBrush(Colors.Transparent)).Replace(Environment.NewLine, "")]); }
			set { ConfigAsker["[Style - Panel preview background]"] = XamlWriter.Save(value).Replace(Environment.NewLine, ""); }
		}

		public static Brush UIPanelPreviewBackgroundStr {
			get { return (Brush)XamlReader.Parse(ConfigAsker["[Style - Panel preview background str]", XamlWriter.Save(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0))).Replace(Environment.NewLine, "")]); }
			set { ConfigAsker["[Style - Panel preview background str]"] = XamlWriter.Save(value).Replace(Environment.NewLine, ""); }
		}

		public static bool StrEditorShowGrid {
			get { return Boolean.Parse(ConfigAsker["[GrfEditor - StrEditorShowGrid]", true.ToString()]); }
			set { ConfigAsker["[GrfEditor - StrEditorShowGrid]"] = value.ToString(); }
		}

		public class QuickColorSetting {
			private readonly ConfigAskerSetting _setting;
			private Vector4? _color;
			
			public QuickColorSetting(ConfigAskerSetting setting) {
				_setting = setting;
			}

			public void SetNull() {
				_color = null;
			}
			
			public void Set(Vector4 color) {
				_color = color;
				string old = _setting.Get();
				_setting.Set(new GrfColor((byte)(255 * color[3]), (byte)(255 * color[0]), (byte)(255 * color[1]), (byte)(255 * color[2])).ToHexString());
				string new_ = _setting.Get();
				_setting.OnPreviewPropertyChanged(old, new_);
			}
			
			public Vector4 Get() {
				if (_color == null) {
					try {
						var color = ((SolidColorBrush)UIPanelPreviewBackgroundStr).Color;
						_color = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
					}
					catch {
						_color = new Vector4(0, 0, 0, 150f / 255f);
					}
				}
			
				return _color.Value;
			}
			
			public Vector4 Color {
				get { return Get(); }
			}
		}

		public static QuickColorSetting StrEditorBackgroundColorQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => UIPanelPreviewBackgroundStr));

		#region TreeBehavior

		public static bool TreeBehaviorSaveExpansion {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Save expansion]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Save expansion]"] = value.ToString(); }
		}

		public static string TreeBehaviorSaveExpansionFolders {
			get { return ConfigAsker["[TreeBehavior - Save expansion folders]", ""]; }
			set { ConfigAsker["[TreeBehavior - Save expansion folders]"] = value; }
		}
		
		public static bool TreeBehaviorExpandSpecificFolders {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Expand specific GRF paths]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Expand specific GRF paths]"] = value.ToString(); }
		}

		public static string TreeBehaviorSpecificFolders {
			get { return ConfigAsker["[TreeBehavior - Specific folders]", "data,root,data\\Example"]; }
			set { ConfigAsker["[TreeBehavior - Specific folders]"] = value; }
		}

		public static bool TreeBehaviorSelectLatest {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Select latest node]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Select latest node]"] = value.ToString(); }
		}

		public static string TreeBehaviorSelectLatestFolders {
			get { return ConfigAsker["[TreeBehavior - Select latest folders]", ""]; }
			set { ConfigAsker["[TreeBehavior - Select latest folders]"] = value; }
		}

		public static string MapExtractorResources {
			get { return ConfigAsker["[MapExtractor - Resources]", ""]; }
			set { ConfigAsker["[MapExtractor - Resources]"] = value; }
		}

		public static string DefaultExtractingPath {
			get {
				var path = ConfigAsker["[GRFEditor - Default extration path]", ProgramDataPath];

				try {
					if (!Directory.Exists(path)) {
						Directory.CreateDirectory(path);
					}
				}
				catch (Exception err) {
					DefaultExtractingPath = ProgramDataPath;
					path = ProgramDataPath;
					ErrorHandler.HandleException("The extraction path is invalid, it has been reset to its default value", err);
				}

				return path;
			}
			set { ConfigAsker["[GRFEditor - Default extration path]"] = value; }
		}

		public static bool AutomaticallyPlaySoundFiles {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Automatically read sound files]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Automatically read sound files]"] = value.ToString(); }
		}

		#endregion

		#region Program's configuration and information

		public static string PublicVersion {
			get { return "1.8.5.0"; }
		}

		public static string Author {
			get { return "Tokeiburu"; }
		}

		public static string ProgramName {
			get { return "GRF Editor"; }
		}

		public static string RealVersion {
			get { return Assembly.GetEntryAssembly().GetName().Version.ToString(); }
		}

		public static int PatchId {
			get { return Int32.Parse(ConfigAsker["[GRFEditor - Patch ID]", "0"]); }
			set { ConfigAsker["[GRFEditor - Patch ID]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static string WebHost {
			get { return @"https://googledrive.com/host/0B8dzg7ZYdSrSYjdyNHpac2hrV1U/"; }
		}

		#endregion

		#region GRFEditor

		public static string TempPath {
			get {
				string path = GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "~tmp");

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
		}

		public static string ProgramDataPath {
			get { return GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName); }
		}

		public static bool AlwaysReopenLatestGrf {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Always reopen latest Grf]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Always reopen latest Grf]"] = value.ToString(); }
		}

		public static bool LockFiles {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Lock added files]", false.ToString()]); }
			set {
				ConfigAsker["[GRFEditor - Lock added files]"] = value.ToString();
				Settings.LockFiles = value;
			}
		}

		public static bool AddHashFileForThor {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Add hash file for Thor]", false.ToString()]); }
			set {
				ConfigAsker["[GRFEditor - Add hash file for Thor]"] = value.ToString();
				Settings.AddHashFileForThor = value;
			}
		}

		public static bool AttemptingCustomDllLoad {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Loading custom DLL state]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Loading custom DLL state]"] = value.ToString(); }
		}

		public static bool PreviewRawGrfProperties {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Preview service - Grf properties - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Preview service - Grf properties - Show raw view]"] = value.ToString(); }
		}

		public static bool PreviewRawFileStructure {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Preview service - File structure - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Preview service - File structure - Show raw view]"] = value.ToString(); }
		}

		public static bool PreviewSpritesWrap {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Preview sprites wrapping]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Preview sprites wrapping]"] = value.ToString(); }
		}

		public static bool PreviewSpritesAuto {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Preview sprites auto adjust]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Preview sprites auto adjust]"] = value.ToString(); }
		}

		public static int PreviewSpritesPerLine {
			get {
				int value = Int32.Parse(ConfigAsker["[GRFEditor - Preview sprites per line]", "8"]);
				if (value <= 0)
					return 1;
				if (value > 50)
					return 50;
				return value;
			}
			set { ConfigAsker["[GRFEditor - Preview sprites per line]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static string EncodingIndex {
			get { return ConfigAsker["[GRFEditor - Encoding index]", "0"]; }
			set { ConfigAsker["[GRFEditor - Encoding index]"] = value; }
		}

		public static float PreviewImageZoom {
			get { return FormatConverters.DefConverter(ConfigAsker["[GRFEditor - Preview zoom]", "1"], FormatConverters.SingleConverter); }
			set { ConfigAsker["[GRFEditor - Preview zoom]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static bool AlwaysOpenAfterExtraction {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - ExtractingService - Always open after extraction]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - ExtractingService - Always open after extraction]"] = value.ToString(); }
		}

		public static bool AutomaticallyPlaceFiles {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - AutomaticallyPlaceFiles]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - AutomaticallyPlaceFiles]"] = value.ToString(); }
		}

		public static int EncodingCodepage {
			get { return Int32.Parse(ConfigAsker["[GRFEditor - Encoding codepage]", "1252"]); }
			set { ConfigAsker["[GRFEditor - Encoding codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int ThemeIndex {
			get { return Int32.Parse(ConfigAsker["[GRFEditor - ThemeIndex]", "0"]); }
			set { ConfigAsker["[GRFEditor - ThemeIndex]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int GatPreviewMode {
			get { return Int32.Parse(ConfigAsker["[GRFEditor - Gat preview - Mode]", "0"]); }
			set { ConfigAsker["[GRFEditor - Gat preview - Mode]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static bool GatPreviewRescale {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Gat preview - Rescale]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Gat preview - Rescale]"] = value.ToString(); }
		}

		public static bool GatPreviewHideBorders {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Gat preview - Hide borders]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Gat preview - Hide borders]"] = value.ToString(); }
		}

		public static bool GatPreviewTransparent {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Gat preview - Transparent]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Gat preview - Transparent]"] = value.ToString(); }
		}

		public static string ExtractingServiceLastPath {
			get { return ConfigAsker["[GRFEditor - ExtractingService - Latest directory]", Configuration.ApplicationPath]; }
			set { ConfigAsker["[GRFEditor - ExtractingService - Latest directory]"] = value; }
		}

		public static string AppLastPath {
			get { return ConfigAsker["[GRFEditor - Application latest file name]", Configuration.ApplicationPath]; }
			set { ConfigAsker["[GRFEditor - Application latest file name]"] = value; }
		}

		public static bool OverrideExtractionPath {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - ExtractingService - Override extraction path]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - ExtractingService - Override extraction path]"] = value.ToString(); }
		}

		public static bool CpuPerformanceManagement {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Cpu performance management]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Cpu performance management]"] = value.ToString(); }
		}

		public static bool ShowGrfEditorHeader {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Show header in text files]", true.ToString()]); }
			set { ConfigAsker["[GRFEditor - Show header in text files]"] = value.ToString(); }
		}

		public static int MaximumNumberOfThreads {
			get {
				int tmp = Int32.Parse(ConfigAsker["[GRFEditor - Maximum number of threads]", "10"]);

				if (tmp < 1 || tmp > 50) {
					ConfigAsker["[GRFEditor - Maximum number of threads]"] = "10";
					tmp = 10;
				}

				return tmp;
			}
			set {
				if (value < 1 || value > 50) {
					ConfigAsker["[GRFEditor - Maximum number of threads]"] = "10";
				}
				else {
					ConfigAsker["[GRFEditor - Maximum number of threads]"] = value.ToString(CultureInfo.InvariantCulture);
				}
			}
		}

		public static FileAssociation FileShellAssociated {
			get {
				try {
					return (FileAssociation) Enum.Parse(typeof (FileAssociation), ConfigAsker["[GRFEditor - File type associated]", "0"]);
				}
				catch {
					return 0;
				}
			}
			set { ConfigAsker["[GRFEditor - File type associated]"] = value.ToString(); }
		}

		#endregion

		#region SpriteMaker

		public static string SpriteMakerPath {
			get {
				if (!Directory.Exists(Path.Combine(Methods.ApplicationPath, "SpriteMaker")))
					Directory.CreateDirectory(Path.Combine(Methods.ApplicationPath, "SpriteMaker"));

				return Path.Combine(Methods.ApplicationPath, "SpriteMaker");
			}
		}

		#endregion

		#region Encryptor

		public static string EncryptorPath {
			get {
				string path = Path.Combine(ProgramDataPath, "Encryption");

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
		}

		public static string EncryptorClientPath {
			get { return ConfigAsker["[Encryptor - Client path]", ""]; }
			set { ConfigAsker["[Encryptor - Client path]"] = value; }
		}

		public static string EncryptorWrapper {
			get { return ConfigAsker["[Encryptor - Wrapper name]", "cps.dll"]; }
			set { ConfigAsker["[Encryptor - Wrapper name]"] = value; }
		}

		public static byte[] EncryptorPassword { get; set; }

		#endregion

		#region FlatMapsMaker

		public static string FlatMapsMakerInputTexturesPath {
			get { return ConfigAsker["[FlatMapsMaker - Input textures path]", Path.Combine(ProgramDataPath, "FlatMapsMaker\\InputTextures")]; }
			set { ConfigAsker["[FlatMapsMaker - Input textures path]"] = value; }
		}

		public static string FlatMapsMakerInputMapsPath {
			get { return ConfigAsker["[FlatMapsMaker - Input maps path]", GrfPath.Combine(ProgramDataPath, "FlatMapsMaker\\InputMaps")]; }
			set { ConfigAsker["[FlatMapsMaker - Input maps path]"] = value; }
		}

		public static string FlatMapsMakerOutputMapsPath {
			get { return ConfigAsker["[FlatMapsMaker - Output maps path]", Path.Combine(ProgramDataPath, "FlatMapsMaker\\OutputMaps\\maps.grf")]; }
			set { ConfigAsker["[FlatMapsMaker - Output maps path]"] = value; }
		}

		public static string FlatMapsMakerOutputTexturesPath {
			get { return ConfigAsker["[FlatMapsMaker - Output textures path]", Path.Combine(ProgramDataPath, "FlatMapsMaker\\OutputTextures")]; }
			set { ConfigAsker["[FlatMapsMaker - Output textures path]"] = value; }
		}

		public static string FlatMapsMakerId {
			get { return ConfigAsker["[FlatMapsMaker - Maps id]", ""]; }
			set { ConfigAsker["[FlatMapsMaker - Maps id]"] = value; }
		}

		public static bool ShowGutterLines {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Show gutter lines]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Show gutter lines]"] = value.ToString(); }
		}

		public static bool RemoveAllLighting {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Remove light map]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Remove light map]"] = value.ToString(); }
		}

		public static bool RemoveAllObjects {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Remove objects]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Remove objects]"] = value.ToString(); }
		}

		public static bool FlattenGround {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Flatten ground]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Flatten ground]"] = value.ToString(); }
		}

		public static bool UseCustomTextures {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Use custom textures]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Use custom textures]"] = value.ToString(); }
		}

		public static bool StickGatCellsToGround {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Stick gat cells to the ground]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Stick gat cells to the ground]"] = value.ToString(); }
		}

		public static bool RemoveWater {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Remove water]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Remove water]"] = value.ToString(); }
		}

		public static bool TextureWalls {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Texture wall custom]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Texture wall custom]"] = value.ToString(); }
		}

		public static bool TextureBlack {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Texture wall black]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Texture wall black]"] = value.ToString(); }
		}

		public static bool TextureOriginal {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Texture wall original]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Texture wall original]"] = value.ToString(); }
		}

		public static bool ResetGlobalLighting {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Reset global lighting]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Reset global lighting]"] = value.ToString(); }
		}

		public static Color FlatMapsMakerC0 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 0]", GrfColor.ToHex(61, 61, 61)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 0]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerC1 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 1]", GrfColor.ToHex(33, 33, 33)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 1]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerC2 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 2]", GrfColor.ToHex(33, 33, 33)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 2]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerC3 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 3]", GrfColor.ToHex(61, 61, 61)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 3]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerC4 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 4]", GrfColor.ToHex(63, 238, 248)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 4]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerC5 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 5]", GrfColor.ToHex(136, 136, 136)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 5]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerC6 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color 6]", GrfColor.ToHex(61, 61, 61)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color 6]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerCwForeground {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color water foreground]", GrfColor.ToHex(148, 148, 148)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color water foreground]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerCwBackground {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color water background]", GrfColor.ToHex(61, 61, 61)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color water background]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerCWall {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color wall]", GrfColor.ToHex(33, 33, 33)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color wall]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerGutter1 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color gutter 1]", GrfColor.ToHex(255, 127, 39)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color gutter 1]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerGutter2 {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color gutter 2]", GrfColor.ToHex(223, 89, 0)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color gutter 2]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerCBorder {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color border]", GrfColor.ToHex(0, 0, 0)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color border]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static Color FlatMapsMakerCx {
			get { return new GrfColor((ConfigAsker["[FlatMapsMaker - Cell color reserved]", GrfColor.ToHex(255, 255, 255)])).ToColor(); }
			set { ConfigAsker["[FlatMapsMaker - Cell color reserved]"] = GrfColor.ToHex(value.R, value.G, value.B); }
		}

		public static string FlatMapsCellWidth {
			get { return ConfigAsker["[FlatMapsMaker - Cell border width]", "2"]; }
			set { ConfigAsker["[FlatMapsMaker - Cell border width]"] = value; }
		}

		public static int FlatMapsCellWidth2 {
			get {
				int value;

				if (Int32.TryParse(FlatMapsCellWidth, out value)) {
					return value;
				}
				else {
					return 2;
				}
			}
		}

		#endregion

		#region Grf validation

		public static bool FeNoExtension {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - No extension]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - No extension]"] = value.ToString(); }
		}

		public static bool FeMissingSprAct {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Missing spr or act files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Missing spr or act files]"] = value.ToString(); }
		}

		public static bool FeEmptyFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Empty files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Empty files]"] = value.ToString(); }
		}

		public static bool FeDb {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Existing db files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Existing db files]"] = value.ToString(); }
		}

		public static bool FeSvn {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Existing svn files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Existing svn files]"] = value.ToString(); }
		}

		public static bool FeDuplicateFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Duplicate files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Duplicate files]"] = value.ToString(); }
		}

		public static bool FeDuplicatePaths {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Duplicate paths]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Duplicate paths]"] = value.ToString(); }
		}

		public static bool FeSpaceSaved {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Find space saved by repacking]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Find space saved by repacking]"] = value.ToString(); }
		}

		public static bool FeInvalidFileTable {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Invalid file table]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Invalid file table]"] = value.ToString(); }
		}

		public static bool FeRootFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Root files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Root files]"] = value.ToString(); }
		}

		public static bool VcDecompressEntries {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Decompress entries]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Decompress entries]"] = value.ToString(); }
		}

		public static bool VcLoadEntries {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Load entries]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Load entries]"] = value.ToString(); }
		}

		public static bool VcInvalidEntryMetadata {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect invalid entry metadata]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect invalid entry metadata]"] = value.ToString(); }
		}

		public static bool VcSpriteIssues {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect sprite issues]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect sprite issues]"] = value.ToString(); }
		}

		public static bool VcSpriteIssuesRle {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect early ending RLE encoding]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect early ending RLE encoding]"] = value.ToString(); }
		}

		public static bool VcSpriteSoundIndex {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Invalid sound index]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Invalid sound index]"] = value.ToString(); }
		}

		public static bool VcSpriteSoundMissing {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect sound file not missing]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect sound file not missing]"] = value.ToString(); }
		}

		public static bool VcSpriteIndex {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Invalid sprite index]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Invalid sprite index]"] = value.ToString(); }
		}

		public static bool VcResourcesModelFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect missing resources in model files]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect missing resources in model files]"] = value.ToString(); }
		}

		public static bool VcResourcesMapFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect missing resources in map files]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect missing resources in map files]"] = value.ToString(); }
		}

		public static bool VcInvalidQuadTree {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect invalid QuadTree]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect invalid QuadTree]"] = value.ToString(); }
		}

		public static bool VcZlibChecksum {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect invalid checksum]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect invalid checksum]"] = value.ToString(); }
		}

		public static bool VeFilesNotFound {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate extraction - Ignore files not found]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate extraction - Ignore files not found]"] = value.ToString(); }
		}

		public static bool VeFilesDifferentSize {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate extraction - Ignore files with different size]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate extraction - Ignore files with different size]"] = value.ToString(); }
		}

		public static bool ValidationRawView {
			get { return Boolean.Parse(ConfigAsker["[Validation - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[Validation - Show raw view]"] = value.ToString(); }
		}

		public static string VeFolder {
			get { return ConfigAsker["[Validation - Hard drive folder]", "C:\\RO\\data"]; }
			set { ConfigAsker["[Validation - Hard drive folder]"] = value; }
		}

		#endregion

		#region Lub decompiler

		public static bool UseCustomDecompiler {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Use GRF Editor Decompiler]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Use GRF Editor Decompiler]"] = value.ToString(); }
		}

		public static bool AppendFunctionId {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Append function Id]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Append function Id]"] = value.ToString(); }
		}

		public static bool UseCodeReconstructor {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Use code reconstructor]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Use code reconstructor]"] = value.ToString(); }
		}

		public static bool DecodeInstructions {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Decode instructions]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Decode instructions]"] = value.ToString(); }
		}

		public static bool GroupIfAllValues {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Group if all values]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Group if all values]"] = value.ToString(); }
		}

		public static bool GroupIfAllKeyValues {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Group if all key values]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Group if all key values]"] = value.ToString(); }
		}

		public static int TextLengthLimit {
			get { return Int32.Parse(ConfigAsker["[Lub decompiler - Text length limit]", "80"]); }
			set { ConfigAsker["[Lub decompiler - Text length limit]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		#endregion

		#region Grf compression

		public static bool CoIdenticalFiles {
			get { return Boolean.Parse(ConfigAsker["[Compression - General compression - Remove identical files]", true.ToString()]); }
			set { ConfigAsker["[Compression - General compression - Remove identical files]"] = value.ToString(); }
		}

		public static bool ShowRswObjects {
			get { return Boolean.Parse(ConfigAsker["[Compression - ShowRswObjects]", false.ToString()]); }
			set { ConfigAsker["[Compression - ShowRswObjects]"] = value.ToString(); }
		}

		public static bool EnableWordWrap {
			get { return Boolean.Parse(ConfigAsker["[Compression - EnableWordWrap]", false.ToString()]); }
			set { ConfigAsker["[Compression - EnableWordWrap]"] = value.ToString(); }
		}

		#endregion
	}

	[Flags]
	public enum FileAssociation {
		Grf = 1 << 1,
		Gpf = 1 << 2,
		Rgz = 1 << 3,
		GrfKey = 1 << 4,
		Spr = 1 << 5,
		All = 1 << 6,
		Cde = 1 << 7,
		Thor = 1 << 8,
		Sde = 1 << 9,
	}
}