using GRF.Core;
using System;

namespace GRF.ContainerFormat {
	public class ContainerSaveResult {
		internal ContainerSaveResult(Container container, string fileName, Container mergeGrf, SavingMode mode, SyncMode syncMode) {
			OldFileName = container.FileName;
			NewFileName = fileName ?? container.FileName;
			MergedGrf = mergeGrf != null;
			SaveModeRequested = mode;
			SyncMode = syncMode;
			Success = true;
		}

		public bool Success { get; set; }
		public bool Completed { get; set; }
		public string OldFileName { get; set; }
		public string NewFileName { get; set; }
		public bool MergedGrf { get; set; }
		public SavingMode SaveModeUsed { get; set; }
		public SavingMode SaveModeRequested { get; set; }
		public SyncMode SyncMode { get; set; }
		public Exception Error { get; set; }
		public bool IsCancelled { get; set; }
		public bool RequiresReload { get; set; }

		public void Fail(Exception err) {
			Success = false;
			Error = err;
		}

		public void Cancelled() {
			Success = false;
			IsCancelled = true;
		}
	}
}
