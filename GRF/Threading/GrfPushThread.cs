using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ErrorManager;

namespace GRF.Threading {
	public class GrfPushSingleThread<T> : PausableThread where T : class {
		private readonly List<T> _items = new List<T>();
		private readonly object _lock = new object();
		public bool Cancel { get; private set; }
		public bool IsRunning { get; set; }

		public void Push(T item) {
			lock (_lock) {
				Cancel = true;
				_items.Add(item);
			}

			Resume();
		}

		public void Terminate() {
			IsRunning = false;
			Resume();
		}

		public void Start(string threadName, Action<T, Func<bool>> process) {
			IsRunning = true;

			new Thread(new ThreadStart(delegate {
				try {
					while (IsRunning) {
						T item = null;
						bool noItems = false;

						lock (_lock) {
							if (_items.Count == 0) {
								noItems = true;
							}
							else {
								item = _items.Last();
								Cancel = false;
							}

							_items.Clear();
						}

						if (noItems) {
							Pause();
						}
						else {
							Cancel = false;
							process(item, () => Cancel);
						}
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
				finally {
					IsRunning = false;
				}
			})) { Name = threadName }.Start();
		}
	}

	public class GrfPushMultiThread<T> where T : class {
		private string _threadName;
		private Action<T, Func<bool>> _process;
		private readonly object _lock = new object();
		private T _lastItem = null;

		public void Push(T item) {
			lock (_lock) {
				_lastItem = item;
			}

			new Thread(new ThreadStart(delegate {
				try {
					_process(item, () => _lastItem != item);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			})) { Name = _threadName }.Start();
		}

		public void Terminate() {
			
		}

		public void Start(string threadName, Action<T, Func<bool>> process) {
			_threadName = threadName;
			_process = process;
		}
	}
}
