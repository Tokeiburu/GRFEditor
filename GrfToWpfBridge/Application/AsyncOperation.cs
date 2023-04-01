using System;
using System.Threading;
using System.Windows;
using ErrorManager;
using GRF.Threading;
using TokeiLibrary.WPF.Styles;

namespace GrfToWpfBridge.Application {
	/// <summary>
	/// This class makes sure that only one operation can be run at the same time.
	/// Also, it makes use of a progress bar to report progress, but you could easily change that
	/// for your own control.
	/// </summary>
	public class AsyncOperation {
		private readonly TkProgressBar _bar;
		private bool _isRunningOverride;
		private GrfThread _thread;

		public AsyncOperation(TkProgressBar bar) {
			_bar = bar;
			_bar.Cancel += new RoutedEventHandler(_bar_Cancel);
		}

		public TkProgressBar ProgressBar {
			get { return _bar; }
		}

		public bool DoNotShowExtraDialogs { get; set; }

		public bool IsRunning {
			get { return _isRunningOverride || (_thread != null && _thread.IsRunning); }
			private set { _isRunningOverride = value; }
		}

		public event GrfThread.GrfThreadEventHandler Finished;
		public event GrfThread.GrfThreadEventHandler Cancelling;

		private void _onCancelling(object state) {
			GrfThread.GrfThreadEventHandler handler = Cancelling;
			if (handler != null) handler(state);
		}

		private void _onFinished(object state) {
			GrfThread.GrfThreadEventHandler handler = Finished;
			if (handler != null) handler(state);
		}

		public IsRunningBlock Begin() {
			if (IsRunning) {
				throw new Exception("An opration is currently running, wait for it to finish or cancel it.");
			}

			return new IsRunningBlock(this);
		}

		private void _bar_Cancel(object sender, RoutedEventArgs e) {
			if (_thread != null) {
				_thread.Cancel();
			}
		}

		public void SetAndRunOperation(GrfThread thread) {
			if (_thread != null && _thread.IsRunning) {
				if (!DoNotShowExtraDialogs)
					ErrorHandler.HandleException("An opration is currently running, wait for it to finish or cancel it.", ErrorLevel.NotSpecified);
			}
			else {
				_thread = thread;
				_thread.Cancelling += new GrfThread.GrfThreadEventHandler(_thread_Cancelling);
				_thread.Cancelled += new GrfThread.GrfThreadEventHandler(_thread_Cancelled);
				_thread.Finished += new GrfThread.GrfThreadEventHandler(_thread_Finished);
				_thread.ProgressUpdate += new GrfThread.GrfThreadEventHandler(_thread_ProgressUpdate);
				_thread.Start();
			}
		}

		public void QueueAndRunOperation(GrfThread thread) {
			if (_thread != null && _thread.IsRunning) {
				_thread.Finished += delegate {
					GrfThread.Start(delegate {
						while (_thread.IsRunning) {
							Thread.Sleep(200);
						}

						SetAndRunOperation(thread);
					}, "GrfToWprBridge - AsyncOperation queuing thread.");
				};
			}
			else {
				SetAndRunOperation(thread);
			}
		}

		public void SetAndRunOperation(GrfThread thread, GrfThread.GrfThreadEventHandler finished) {
			if (_thread != null && _thread.IsRunning) {
				if (!DoNotShowExtraDialogs)
					ErrorHandler.HandleException("An operation is currently running, wait for it to finish or cancel it.", ErrorLevel.NotSpecified);
			}
			else {
				_thread = thread;
				_thread.Cancelling += new GrfThread.GrfThreadEventHandler(_thread_Cancelling);
				_thread.Cancelled += new GrfThread.GrfThreadEventHandler(_thread_Cancelled);
				_thread.Finished += new GrfThread.GrfThreadEventHandler(_thread_Finished);
				_thread.ProgressUpdate += new GrfThread.GrfThreadEventHandler(_thread_ProgressUpdate);
				_thread.Finished += finished;
				_thread.Start();
			}
		}

		public void WaitUntilFinished() {
			while (IsRunning) {
				Thread.Sleep(_thread.Delay);
			}
		}

		private void _thread_ProgressUpdate(object state) {
			try {
				_bar.Progress = (float) state;
			}
			catch {
			}
		}

		private void _thread_Finished(object state) {
			try {
				_bar.Progress = 100.0f;
				_onFinished(state);
			}
			catch {
			}
		}

		private void _thread_Cancelled(object state) {
			try {
				_bar.SetSpecialState(TkProgressBar.ProgressStatus.Finished);
			}
			catch {
			}
		}

		private void _thread_Cancelling(object state) {
			try {
				_bar.SetSpecialState(TkProgressBar.ProgressStatus.Cancelling);
				_onCancelling(state);
			}
			catch {
			}
		}

		public void Cancel() {
			if (_thread != null)
				_thread.Cancel();
		}

		#region Nested type: IsRunningBlock

		public class IsRunningBlock : IDisposable {
			private readonly AsyncOperation _ao;

			public IsRunningBlock(AsyncOperation ao) {
				_ao = ao;
				_ao.IsRunning = true;
			}

			#region IDisposable Members

			public void Dispose() {
				_ao.IsRunning = false;
			}

			#endregion

			public void Close() {
				_ao.IsRunning = false;
			}
		}

		#endregion
	}
}