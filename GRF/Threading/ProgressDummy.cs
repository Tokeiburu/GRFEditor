namespace GRF.Threading {
	public class ProgressDummy : IProgress {
		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion
	}
}
