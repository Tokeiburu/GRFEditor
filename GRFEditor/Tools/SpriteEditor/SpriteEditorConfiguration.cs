using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using GRFEditor.ApplicationConfiguration;
using Utilities;

namespace GRFEditor.Tools.SpriteEditor {
	public class SpriteEditorConfiguration {
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(Path.Combine(Methods.ApplicationPath, "config.txt"))); }
			set { _configAsker = value; }
		}

		public static string PublicVersion {
			get { return "2.0.0"; }
		}

		public static string Author {
			get { return "Tokeiburu"; }
		}

		public static string ProgramName {
			get { return "Sprite Editor"; }
		}

		public static string RealVersion {
			get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
		}

		public static string AppLastPath {
			get { return ConfigAsker["[Sprite Editor - Application latest file name]", Path.Combine(GrfEditorConfiguration.ProgramDataPath, "sprite.spr")]; }
			set { ConfigAsker["[Sprite Editor - Application latest file name]"] = value; }
		}

		public static bool UseDithering {
			get { return Boolean.Parse(ConfigAsker["[Sprite Editor - Use dithering]", false.ToString()]); }
			set { ConfigAsker["[Sprite Editor - Use dithering]"] = value.ToString(); }
		}

		public static bool UseTgaImages {
			get { return Boolean.Parse(ConfigAsker["[Sprite Editor - Use TGA images]", false.ToString()]); }
			set { ConfigAsker["[Sprite Editor - Use TGA images]"] = value.ToString(); }
		}

		public static int TransparencyMode {
			get { return Int32.Parse(ConfigAsker["[Sprite Editor - Transparency mode]", 1.ToString(CultureInfo.InvariantCulture)]); }
			set { ConfigAsker["[Sprite Editor - Transparency mode]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int FormatConflictOption {
			get { return Int32.Parse(ConfigAsker["[Sprite Editor - Last format conflict option]", 2.ToString(CultureInfo.InvariantCulture)]); }
			set { ConfigAsker["[Sprite Editor - Last format conflict option]"] = value.ToString(CultureInfo.InvariantCulture); }
		}
	}
}