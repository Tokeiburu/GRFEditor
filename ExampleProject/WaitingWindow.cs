using System;
using System.Windows.Forms;
using GRF;
using GRF.Threading;

namespace ExampleProject {
	/// <summary>
	/// If you're familiar with threads and assigning a progress's bar value without 'lagging' the UI, then skip this
	/// 
	/// Each .NET control (Button, ProgressBar, TextBox, etc...) has its own thread and threads can only be 
	/// accessed from their creator (in this case : WaitingWindow). That means that progressBar1 can only be changed
	/// from the WaitingWindow instance and any other thread (such as GrfThread) trying to change the bar's value will
	/// trigger an exception called cross-thread InvalidOperationException. To change the value in a thread-safe
	/// way, you must invoke the object's thread (progressBar1.Invoke()) and then the operation will be executed
	/// synchronously once the main UI thread (WaitingWindow) wakes up.
	/// 
	/// See GRFEditorAsyncOperation.cs for a better implementation of a similar class (simply change TKProgressBar
	/// for ProgressBar and a few minor tweaks to use it in a WindowsForms project).
	/// </summary>
	public partial class WaitingWindow : Form {
		private readonly GrfThread _thread;
		public bool Result = true;

		public WaitingWindow(Action action, IProgress caller) {
			InitializeComponent();
			
			progressBar1.Style = ProgressBarStyle.Marquee;
			_thread = new GrfThread(action, caller, 200, null);
			button1.Click += new EventHandler((o, e) => _thread.Cancel());
		}

		protected override void OnShown(EventArgs e) {
			_startGrfThread();
			base.OnShown(e);
		}

		private void _startGrfThread() {
			_thread.Cancelling += _thread_Cancelling;
			_thread.Cancelled += _thread_Cancelled;
			_thread.Finished += _thread_Finished;
			_thread.ProgressUpdate += _thread_ProgressUpdate;
			_thread.Start();
		}

		private void _thread_ProgressUpdate(object state) {
			_setProgress(Convert.ToInt32(state));
		}

		private void _setProgress(int progress) {
			// You must check if the thread is running to avoid threading issues
			// (Otherwise you might get the thread stuck after invoking the progressBar1
			//  and the application will keep running indefinitely in the background after
			//  you close the application).
			//
			// GRF Editor has background threads that kills the process in case the
			// application is closed and stuff still run preventing a real closure. How you
			// deal with it is really up to you.
			if (_thread.IsRunning) {
				progressBar1.Invoke(new Action(delegate {
					if (progress == -1) {
						progressBar1.Style = ProgressBarStyle.Marquee;
					}
					else {
						progressBar1.Style = ProgressBarStyle.Continuous;
						progressBar1.Value = progress;
					}
				}));
			}
		}

		private void _thread_Finished(object state) {
			_setProgress(100);
			_close();
		}

		private void _close() {
			try {
				this.Invoke(new Action(delegate {
					this.DialogResult = this.Result ? DialogResult.OK : DialogResult.Cancel;
					this.Close();
				}));
			}
			catch { }
		}

		private void _thread_Cancelled(object state) {
			_close();
		}

		private void _thread_Cancelling(object state) {
			_setProgress(-1);
			_setResult(false);
		}

		private void _setResult(bool result) {
			this.Invoke(new Action(() => this.Result = result));
		}
	}
}
