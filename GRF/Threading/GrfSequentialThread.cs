using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ErrorManager;

namespace GRF.Threading {
	public class GrfSequentialThread<T> : PausableThread where T : class {
		private readonly List<T> _items = new List<T>();
		private readonly object _lock = new object();
		public bool Cancel { get; private set; }
		public bool IsRunning { get; set; }

		public void Push(T item) {
			lock (_lock) {
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
						List<T> items = null;
						bool noItems = false;

						lock (_lock) {
							if (_items.Count == 0) {
								noItems = true;
							}
							else {
								items = new List<T>(_items);
							}

							_items.Clear();
						}

						if (noItems) {
							Pause();
						}
						else {
							foreach (var item in items)
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
}
