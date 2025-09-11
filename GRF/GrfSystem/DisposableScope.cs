using System;

namespace GRF.GrfSystem {
	/// <summary>
	/// This class is meant for debugging purposes, to locate unclosed streams.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DisposableScope<T> : IDisposable where T : class, IDisposable {
		public T Value {
			get { return _disposable; }
		}

		private readonly T _disposable;

		public DisposableScope(T disposable) {
			_disposable = disposable;

			//if (disposable is FileStream) {
			//	ByteReaderStream.Streams[((FileStream) (object) disposable).Name]++;
			//}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_disposable != null) {
					//if (_disposable is FileStream) {
					//	ByteReaderStream.Streams[((FileStream) (object) _disposable).Name]--;
					//}

					_disposable.Dispose();
				}
			}
		}
	}
}
