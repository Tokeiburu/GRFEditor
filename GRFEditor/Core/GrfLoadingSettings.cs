namespace GRFEditor.Core {
	public class GrfLoadingSettings {
		private bool _visualReloadRequired = true;

		public GrfLoadingSettings() {
		}

		public GrfLoadingSettings(GrfLoadingSettings grfLoadingSettings) {
			SkipReloadOnce = grfLoadingSettings.SkipReloadOnce;
			FileName = grfLoadingSettings.FileName;
			VisualReloadRequired = grfLoadingSettings.VisualReloadRequired;
			ReloadKey = grfLoadingSettings.ReloadKey;
		}

		public bool ReloadKey { get; set; }
		public string FileName { get; set; }
		public bool SkipReloadOnce { get; set; }

		public bool VisualReloadRequired {
			get { return _visualReloadRequired; }
			set { _visualReloadRequired = value; }
		}
	}
}