using ErrorManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities {
	public sealed class Debouncer : IDisposable {
		private readonly object _lock = new object();
		private CancellationTokenSource _cts;
		private bool _disposed;

		public void Execute(Action action, int delayMs = 0) {
			lock (_lock) {
				_cts?.Cancel();
				_cts?.Dispose();
				_cts = new CancellationTokenSource();
				var token = _cts.Token;

				Task.Run(async () => {
					try {
						if (delayMs > 0)
							await Task.Delay(delayMs, token);

						if (!token.IsCancellationRequested)
							action();
					}
					catch (OperationCanceledException) { }
					catch (Exception ex) {
						ErrorHandler.HandleException(ex);
					}
				});
			}
		}

		public void Dispose() {
			if (_disposed) {
				return;
			}

			_cts?.Cancel();
			_cts?.Dispose();
			_disposed = true;
		}
	}

	public sealed class CoalescingExecutor {
		private int _running;
		private int _pending;

		public void Execute(Action action) {
			if (Interlocked.Exchange(ref _pending, 1) == 1) {
				return;
			}

			Task.Run(() => {
				if (Interlocked.Exchange(ref _running, 1) == 1)
					return;

				try {
					do {
						Interlocked.Exchange(ref _pending, 0);
						action();
					}
					while (Interlocked.Exchange(ref _pending, 0) == 1);
				}
				finally {
					Interlocked.Exchange(ref _running, 0);
				}
			});
		}
	}

	public class UpdateDispatcher {
		private readonly object _lock = new object();
		private readonly int _delayMs;

		private bool _isProcessing;
		private Action _pendingAction;

		public UpdateDispatcher(int delayMs = 50) {
			_delayMs = delayMs;
		}

		public void Execute(Action action) {
			lock (_lock) {
				_pendingAction = action;

				if (_isProcessing)
					return;

				_isProcessing = true;
			}

			_ = ProcessAsync();
		}

		private async Task ProcessAsync() {
			while (true) {
				Action action;

				lock (_lock) {
					action = _pendingAction;
					_pendingAction = null;
				}

				try {
					action?.Invoke();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}

				if (_delayMs > 0)
					await Task.Delay(_delayMs).ConfigureAwait(false);

				lock (_lock) {
					if (_pendingAction == null) {
						_isProcessing = false;
						return;
					}
				}
			}
		}
	}
}
