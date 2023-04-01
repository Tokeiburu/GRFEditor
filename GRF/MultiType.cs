using System.IO;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities;
using Utilities.Extension;

namespace GRF {
	/// <summary>
	/// A class which simplifies the function calls. This class can be converted <para></para>
	/// from multiple type entries, such as streams, a file path, a byte array, a file entry, etc.
	/// </summary>
	public class MultiType {
		private readonly ContainerEntry _entry;
		private byte[] __data;
		private byte[] _data {
			get {
				if (_grfMemoryStreamIndex > 0) {
					var stream = _containerResources.GetIndexedMemoryStream(_grfMemoryStreamIndex);

					var memStream = stream as MemoryStream;

					if (memStream != null) {
						if (memStream.GetBuffer().Length == stream.Length)
							return memStream.GetBuffer();

						memStream.Capacity = (int)stream.Length;
						return memStream.GetBuffer();
					}

					return stream.ReadAllBytes();
				}

				if (__data == null && _entry != null) {
					__data = _entry.GetDecompressedData();
				}

				return __data;
			}
			set {
				__data = value;
			}
		}

		private bool _isUnique = true;
		private readonly int _grfMemoryStreamIndex;
		private readonly ContainerResources _containerResources;

		private MultiType(byte[] data) {
			_data = data;
		}

		private MultiType(GrfMemoryStreamHolder grfMemoryStream) {
			_grfMemoryStreamIndex = grfMemoryStream.GrfStreamIndex;
			_containerResources = grfMemoryStream.ContainerResources;
		}

		private MultiType(ContainerEntry entry) {
			_entry = entry;
		}

		/// <summary>
		/// Gets the path of the loaded file.
		/// </summary>
		public string Path { get; private set; }

		public static implicit operator MultiType(string file) {
			return new MultiType(File.ReadAllBytes(file)) { Path = file };
		}

		public static implicit operator MultiType(Stream stream) {
			return new MultiType(stream.ReadAllBytes());
		}

		public static implicit operator MultiType(byte[] data) {
			return new MultiType(data) { IsUnique = false};
		}

		public static implicit operator MultiType(GrfMemoryStreamHolder data) {
			return new MultiType(data);
		}

		public static implicit operator MultiType(ContainerEntry entry) {
			return new MultiType(entry);
		}

		/// <summary>
		/// Gets the data.
		/// </summary>
		public byte[] Data {
			get { return _data; }
		}

		/// <summary>
		/// Gets the unique data.
		/// </summary>
		public byte[] UniqueData {
			get {
				if (!IsUnique) {
					return Methods.Copy(_data);
				}

				return _data;
			}
		}

		internal bool IsUnique {
			get { return _isUnique; }
			set { _isUnique = value; }
		}

		internal bool IsGrfMemoryStream {
			get { return _grfMemoryStreamIndex > 0; }
		}

		internal int GrfMemoryStreamIndex {
			get { return _grfMemoryStreamIndex; }
		}

		public int Length {
			get {
				if (_entry != null)
					return _entry.SizeDecompressed;

				if (_grfMemoryStreamIndex > 0)
					return (int)_containerResources.GetIndexedMemoryStream(_grfMemoryStreamIndex).Length;

				return Data.Length;
			}
		}

		/// <summary>
		/// Gets the binary reader.
		/// </summary>
		/// <returns>Returns the binary reader</returns>
		public IBinaryReader GetBinaryReader() {
			return new ByteReader(_data);
		}
	}
}