using System;
using GRF.ContainerFormat;

namespace GRF.Threading {
	/// <summary>
	/// An interface that implements the progression of an object.
	/// This allows objects inheriting from this to be cancelled and have their progress updated.
	/// </summary>
	public interface IProgress {
		/// <summary>
		/// Gets or sets the progress.
		/// </summary>
		float Progress { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cancelling.
		/// </summary>
		bool IsCancelling { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cancelled.
		/// </summary>
		bool IsCancelled { get; set; }
	}

	public static class AProgress {
		public static void Init(IProgress prog) {
			prog.Progress = -1;
			prog.IsCancelled = false;
			prog.IsCancelling = false;
		}

		public static void Finalize(IProgress prog) {
			prog.Progress = 100f;
			prog.IsCancelled = true;
			prog.IsCancelling = false;
		}

		public static void IsCancelling(IProgress prog) {
			if (prog.IsCancelling)
				throw new OperationCanceledException();
		}

		public static float LimitProgress(float value) {
			if (value >= 100f)
				return 99.99f;

			return value;
		}
	}
}
