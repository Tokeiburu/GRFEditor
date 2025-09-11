using System;
using System.IO;
using GRF.FileFormats.LubFormat;

namespace GRF.GrfSystem {
	public static class Settings {
		public static int MaximumNumberOfThreads = 10;

		public static int CompressionLevel = 6;
		private static string _tempPath = "tmp";
		public static bool CpuMonitoringEnabled = true;
		public static bool LockFiles { get; set; }
		public static bool PreserveDirectoryStructure { get; set; }
		public static Action OnSavingFailed;
		public static float CpuUsageCritical = 90f;
		public static bool AddHashFileForThor = false;

		static Settings() {
			LubDecompilerSettings = new LubSettings {
				AppendFunctionId = true, 
				DecodeInstructions = true, 
				TextLengthLimit = 80, 
				UseCodeReconstructor = true,
				GroupIfAllKeyValues = false,
				GroupIfAllValues = true,
				EncapsulateByCheckingOtherKeys = true
			};
		}

		public static string TempPath {
			get {
				if (!Directory.Exists(_tempPath))
					Directory.CreateDirectory(_tempPath);

				return _tempPath;
			}
			set { _tempPath = value; }
		}

		public static LubSettings LubDecompilerSettings { get; set; }
	} 
}
