using System;
using System.Threading;

namespace GRF.Threading {
	public class ConditionalThread {
		public bool ThreadRunning { get; set; }
		public bool Wait {
			get { return _wait; }
			set { _wait = value; }
		}

		private readonly Func<bool> _condition;
		private readonly int _delayUpdate;
		private Action _action;
		private bool _wait = true;

		public Action Action {
			set { _action = value; }
		}

		public ConditionalThread(Func<bool> condition, int delayUpdate = 0) {
			_condition = condition;
			_delayUpdate = delayUpdate;
		}

		public ConditionalThread(Func<bool> condition, Action action, int delayUpdate = 0) {
			_condition = condition;
			_action = action;
			_delayUpdate = delayUpdate;
		}

		public void Start() {
			ThreadRunning = true;

			new Thread(new ThreadStart(delegate {
				try {
					while (_condition()) {
						if (_delayUpdate > 0 && Wait)
							Thread.Sleep(_delayUpdate);

						if (_action != null)
							_action();
						else
							_start();
					}

				}
				finally {
					ThreadRunning = false;
				}
			})) { Name = "GRF - Conditional thread"}.Start();
		}

		protected virtual void _start(object argument = null) { }
	}
}
