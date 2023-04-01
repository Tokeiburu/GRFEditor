using System.Threading;

namespace GRF.Threading {
	/// <summary>
	/// A thread that can be paused to be resumed later on
	/// </summary>
	public abstract class PausableThread {
		private readonly AutoResetEvent _are = new AutoResetEvent(false);
		private bool _isPaused;

		public bool IsPaused {
			get { return _isPaused; }
			set {
				if (value == false)
					Resume();

				_isPaused = value;
			}
		}

		protected void Pause() {
			_are.WaitOne();
		}

		protected void Resume() {
			_are.Set();
		}
	}
}
