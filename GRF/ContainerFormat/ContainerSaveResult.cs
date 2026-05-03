using GRF.Core;

namespace GRF.ContainerFormat {
	public class ContainerSaveResult {
		public bool Success { get; set; }
		public string OldFileName { get; set; }
		public string NewFileName { get; set; }
		public bool MergedGrf { get; set; }
		public SavingMode SaveModeUsed { get; set; }
		public SavingMode SaveModeRequested { get; set; }
		public SyncMode SyncMode { get; set; }
	}
}
