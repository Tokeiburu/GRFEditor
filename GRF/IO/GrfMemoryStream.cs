using System.IO;
using GRF.ContainerFormat;

namespace GRF.IO {
	public class GrfMemoryStreamHolder {
		private readonly Stream _stream;
		private readonly ContainerResources _containerResources;
		public int GrfStreamIndex { get; private set; }

		public ContainerResources ContainerResources {
			get { return _containerResources; }
		}

		public Stream Stream {
			get { return _stream; }
		}

		public GrfMemoryStreamHolder(Stream stream, ContainerResources containerResources, int index) {
			_stream = stream;
			_containerResources = containerResources;
			GrfStreamIndex = index;
		}
	}
}
