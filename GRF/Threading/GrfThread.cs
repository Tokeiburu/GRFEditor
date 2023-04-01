using System;
using System.Diagnostics;
using System.Threading;
using ErrorManager;

namespace GRF.Threading {
	/// <summary>
	/// A thread that reports progress and implements the cancel event.
	/// See the constructor for information on the required parameters.
	/// See GRFEditorAsyncOperation for a better example on how to use this class.
	/// </summary>
	public class GrfThread {
		#region Delegates

		public delegate void GrfThreadEventHandler(object state);

		#endregion

		private readonly Action _action;
		private readonly IProgress _caller;
		private readonly bool _exitWhenFunctionIsOver;
		private readonly bool _isSTA;
		private readonly int _msDelay;
		private readonly object _state;
		public bool ActivateTimeElapsed { get; set; }
		public TimeSpan TimeElapsed {
			get { return _watch.Elapsed; }
		}
		private Stopwatch _watch;

		/// <summary>
		/// Initializes a new instance of the <see cref="GrfThread" /> class.
		/// </summary>
		/// <param name="action">The action (the action should be on the caller's object).</param>
		/// <param name="caller">The caller is the object which can be 'monitered' (it has a progress value).</param>
		/// <param name="updateFrequence">The update frequence. Each update will trigger the ProgressUpdate event.</param>
		/// <param name="state">An object to be returned once the thread has finished.</param>
		/// <param name="exitWhenFunctionIsOver"> </param>
		/// <param name="isSTA"> </param>
		public GrfThread(Action action, IProgress caller, int updateFrequence, object state = null, bool exitWhenFunctionIsOver = false, bool isSTA = false) {
			_action = action;
			_caller = caller;
			_msDelay = updateFrequence;
			_state = state;
			_exitWhenFunctionIsOver = exitWhenFunctionIsOver;
			_isSTA = isSTA;
			IsRunning = false;
		}

		public int Delay {
			get { return _msDelay; }
		}

		public bool IsRunning { get; private set; }
		public float Progress { get { return _caller.Progress; } }

		public event GrfThreadEventHandler Finished;
		public event GrfThreadEventHandler Cancelled;
		public event GrfThreadEventHandler Cancelling;
		public event GrfThreadEventHandler ProgressUpdate;

		public void Start() {
			IsRunning = true;
			new Thread(new ThreadStart(delegate {
				try {
					_caller.Progress = -1;
					_caller.IsCancelled = false;
					_caller.IsCancelling = false;

					if (ProgressUpdate != null) {
						ProgressUpdate(_caller.Progress);
						ProgressUpdate(_caller.Progress);
					}

					if (ActivateTimeElapsed) {
						_watch = new Stopwatch();
						_watch.Start();
					}

					bool ended = false;

					Thread t = new Thread(new ThreadStart(delegate {
						try {
							_action();
						}
						catch (Exception err) {
							_caller.IsCancelling = true;
							_caller.IsCancelled = true;
							ErrorHandler.HandleException(err, ErrorLevel.Warning);
						}

						if (_exitWhenFunctionIsOver) {
							_caller.Progress = 100f;
							ended = true;
						}
					})) { Name = "GRF - Internal GrfThread's thread" };

					if (_isSTA)
						t.SetApartmentState(ApartmentState.STA);

					t.Start();

					while (_caller.Progress < 100.0f) {
						if (ended)
							break;

						Thread.Sleep(_msDelay);

						if (_caller.IsCancelling) {
							if (_caller.IsCancelled)
								break;

							if (Cancelling != null)
								Cancelling(null);
						}
						else {
							if (ProgressUpdate != null)
								ProgressUpdate(_caller.Progress);
						}
					}

					if (_caller.IsCancelled) {
						_caller.IsCancelling = false;

						if (Cancelled != null)
							Cancelled(_state);
					}

					if (Finished != null)
						Finished(_state);

				}
				finally {
					if (ActivateTimeElapsed) {
						_watch.Stop();
					}

					IsRunning = false;
				}
			})) { Name = "GRF - GrfThread thread" }.Start();
		}

		public void Cancel() {
			_caller.IsCancelling = true;
		}

		public static void Start(Action action, string name = "GRF - GrfThread thread starter") {
			new Thread(new ThreadStart(delegate {
				try {
					action();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			})) { Name = name }.Start();
		}

		public static void Start(Action action, string name, bool isAsync) {
			if (isAsync) {
				Start(action, name);
			}
			else {
				action();
			}
		}

		public static void StartSTA(Action action, string name = "GRF - GrfThread thread starter") {
			var t = new Thread(() => action()) { Name = name };
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}
	}
}
