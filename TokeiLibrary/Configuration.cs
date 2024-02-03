using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using Utilities;

namespace TokeiLibrary {
	/// <summary>
	/// The ConfigAsker item SHOULD be replaced by the application's one
	/// </summary>
	public static class Configuration {
		#region Settings
		private static ConfigAsker _configAsker;
		private static BitmapScalingMode _availableScaleMode;
		private static bool _availableScaleModeFound;
		private static string _programDataPath;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(Path.Combine(ApplicationPath, "config.txt"))); }
			set { _configAsker = value; }
		}

		public static ErrorLevel WarningLevel {
			get { return (ErrorLevel)Int32.Parse(ConfigAsker["[Application - Warning level]", "0"]); }
			set { ConfigAsker["[Application - Warning level]"] = ((int)value).ToString(CultureInfo.InvariantCulture); }
		}

		public static string ApplicationPath {
			get { return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); }
		}

		public static SolidColorBrush UIGridBackground {
			get { return new SolidColorBrush(Color.FromArgb(255, 114, 114, 114)); }
		}

		public static Thickness UISeparatorMarginTop {
			get { return Environment.OSVersion.Version.Major >= 6 ? new Thickness(30, 0, 1, 1) : new Thickness(25, 0, 1, 1); }
		}

		public static Thickness UISeparatorMarginBottom {
			get { return Environment.OSVersion.Version.Major >= 6 ? new Thickness(30, 1, 1, 0) : new Thickness(25, 1, 1, 0); }
		}

		public static BitmapScalingMode UIImageRendering {
			get { return BestAvailableScaleMode; }
		}

		public static bool TreeBehaviorUseAlt {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Use Alt]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Use Alt]"] = value.ToString(); }
		}

		public static bool LogAnyExceptions {
			get { return Boolean.Parse(ConfigAsker["[Application - Log any exceptions]", false.ToString()]); }
			set { ConfigAsker["[Application - Log any exceptions]"] = value.ToString(); }
		}

		public static bool EnableDebuggerTrace {
			get { return Boolean.Parse(ConfigAsker["[Application - Enable debugger trace]", false.ToString()]); }
			set { ConfigAsker["[Application - Enable debugger trace]"] = value.ToString(); }
		}

		public static bool EnableWindowsOwnership {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Enable windows ownership]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Enable windows ownership]"] = value.ToString(); }
		}

		public static string ApplicationDataPath {
			get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
		}

		public static bool TranslateTreeView {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Tree behavior - Translate tree view]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Tree behavior - Translate tree view]"] = value.ToString(); }
		}

		public static bool WriteExceptionsInCurrentFolder { get; set; }

		public static string ProgramDataPath {
			get {
				if (_programDataPath == null)
					_programDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GRF Editor");

				if (!Directory.Exists(_programDataPath))
					Directory.CreateDirectory(_programDataPath);

				return _programDataPath;
			}
			set { _programDataPath = value; }
		}

		public static BitmapScalingMode BestAvailableScaleMode {
			get {
				if (!_availableScaleModeFound) {
					try {
						try {
							Image image = new Image();
							image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
							_availableScaleMode = BitmapScalingMode.NearestNeighbor;
						}
						catch {
							try {
								Image image = new Image();
								image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
								_availableScaleMode = BitmapScalingMode.HighQuality;
							}
							catch {
								_availableScaleMode = BitmapScalingMode.Unspecified;
							}
						}
					}
					finally {
						_availableScaleModeFound = true;
					}
				}

				return _availableScaleMode;
			}
		}

		#endregion

		public static void SetImageRendering(ResourceDictionary dico) {
			dico.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/ProgramStyles.xaml", UriKind.RelativeOrAbsolute) });
		}
	}
}
