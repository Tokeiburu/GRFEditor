using System.Collections.Generic;
using System.IO;
using GRF.IO;

namespace GRF.ContainerFormat {
	public class ContainerResources {
		internal GrfMemoryStreamHolder CreateMemoryHolder(Stream stream) {
			_memoryStreamIndex++;
			var holder = new GrfMemoryStreamHolder(stream, this, _memoryStreamIndex);
			_grfMemoryHolders[_memoryStreamIndex] = holder;
			return holder;
		}

		private readonly Dictionary<int, GrfMemoryStreamHolder> _grfMemoryHolders = new Dictionary<int, GrfMemoryStreamHolder>();
		private int _memoryStreamIndex;

		public Stream GetIndexedMemoryStream(int streamIndex) {
			return _grfMemoryHolders[streamIndex].Stream;
		}

		public void Clear() {
			_memoryStreamIndex = 0;

			if (_grfMemoryHolders.Count > 0) {
				foreach (var stream in _grfMemoryHolders.Values) {
					stream.Stream.Close();
				}

				_grfMemoryHolders.Clear();
			}
		}
	}
}
