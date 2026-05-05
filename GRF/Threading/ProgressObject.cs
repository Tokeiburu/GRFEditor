using System;

namespace GRF.Threading {
	public class ProgressObject : IProgress {
		#region IProgress Members

		public float Progress { get; set; } = -1f;
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		public void Cancel() {
			IsCancelling = true;
			throw new OperationCanceledException();
		}

		public void Init() {
			Progress = -1f;
			IsCancelled = false;
			IsCancelling = false;
		}

		public void Finish() {
			Progress = 100f;
			IsCancelled = true;
			IsCancelling = false;
		}
	}
}
