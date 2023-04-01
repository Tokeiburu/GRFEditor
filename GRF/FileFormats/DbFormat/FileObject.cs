using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace GRF.FileFormats.DbFormat {
	public class FileObject : Stream {
		/// <summary>
		/// Gets the length of the file
		/// </summary>
		public override long Length {
			get {
				STATSTG sTatstg;

				if (FileStream == null) {
					throw new ObjectDisposedException("fileStream", "storage stream no longer available");
				}
				FileStream.Stat(out sTatstg, 1);
				return sTatstg.cbSize;
			}
		}

		/// <summary>
		/// Gets a flag if reading is supported
		/// </summary>
		public override bool CanRead {
			get {
				if (FileStream != null) {
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets a flag if writing is supported
		/// </summary>
		public override bool CanWrite {
			get { return true; }
		}

		/// <summary>
		/// Gets a flag if seeking is supported
		/// </summary>
		public override bool CanSeek {
			get { return true; }
		}

		/// <summary>
		/// Gets the current position in the stream
		/// </summary>
		public override long Position {
			get { return Seek(0, SeekOrigin.Current); }

			set { Seek(value, SeekOrigin.Begin); }
		}

		/// <summary>
		/// Gets the file name
		/// </summary>
		public string FileName { get; set; }

		public string FilePath { get; set; }

		public string FileUrl {
			get { return String.Concat(BaseStorageWrapper.BaseUrl, FilePath.Replace("\\", "/"), "/", FileName); }
		}

		public int FileType { get; set; }
		public Interop.IStorage FileStorage { get; set; }
		public IStream FileStream { get; set; }

		/// <summary>
		/// Reads bytes from the stream
		/// </summary>
		/// <param name="buffer">buffer which will receive the bytes</param>
		/// <param name="offset">offset</param>
		/// <param name="count">number of bytes to be read</param>
		/// <returns>Returns the actual number of bytes read from the stream.</returns>
		public override int Read(byte[] buffer, int offset, int count) {
			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			int i = 0;
			object local = i;
			var gCHandle = new GCHandle();
			try {
				gCHandle = GCHandle.Alloc(local, GCHandleType.Pinned);
				IntPtr j = gCHandle.AddrOfPinnedObject();
				if (offset != 0) {
					var bs = new byte[count - 1];
					FileStream.Read(bs, count, j);
					i = (int) local;
					Array.Copy(bs, 0, buffer, offset, i);
				}
				else {
					FileStream.Read(buffer, count, j);
					i = (int) local;
				}
			}
			finally {
				if (gCHandle.IsAllocated) {
					gCHandle.Free();
				}
			}
			return i;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (FileStream == null) {
				throw new ObjectDisposedException("theStream");
			}
			if (offset != 0) {
				int i = buffer.Length - offset;
				var bs = new byte[i];
				Array.Copy(buffer, offset, bs, 0, i);
				FileStream.Write(bs, i, IntPtr.Zero);
				return;
			}
			FileStream.Write(buffer, count, IntPtr.Zero);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			long l = 0;
			object local = l;
			var gCHandle = new GCHandle();
			try {
				gCHandle = GCHandle.Alloc(local, GCHandleType.Pinned);
				IntPtr i = gCHandle.AddrOfPinnedObject();
				FileStream.Seek(offset, (int) origin, i);
				l = (long) local;
			}
			finally {
				if (gCHandle.IsAllocated) {
					gCHandle.Free();
				}
			}
			return l;
		}

		/// <summary>
		/// Flushes the stream
		/// </summary>
		public override void Flush() {
			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			FileStream.Commit(0);
		}

		/// <summary>
		/// Closes the storage stream
		/// </summary>
		public override void Close() {
			if (FileStream != null) {
				FileStream.Commit(0);
				Marshal.ReleaseComObject(FileStream);
				FileStream = null;
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Sets the length of the stream
		/// </summary>
		/// <param name="value">new length of the stream</param>
		public override void SetLength(long value) {
			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			FileStream.SetSize(value);
		}

		/// <summary>
		/// Saves the stream to a file
		/// </summary>
		/// <param name="fileName">filename</param>
		public void Save(string fileName) {
			int i;

			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			var bs = new byte[Length];
			Seek(0, SeekOrigin.Begin);
			Stream stream = File.OpenWrite(fileName);
			while ((i = Read(bs, 0, 1024)) > 0) {
				stream.Write(bs, 0, i);
			}
			stream.Close();
		}

		/// <summary>
		/// Reads from the stream (text-based)
		/// </summary>
		/// <returns>The string contents of the stream</returns>
		public string ReadFromFile() {
			int i;

			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			Stream stream = new MemoryStream();
			var bs = new byte[Length];
			Seek(0, SeekOrigin.Begin);
			while ((i = Read(bs, 0, 1024)) > 0) {
				stream.Write(bs, 0, i);
			}
			stream.Seek(0, SeekOrigin.Begin);
			return new StreamReader(stream, Encoding.Default).ReadToEnd().ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Reads from the stream (text based)
		/// </summary>
		/// <param name="encoder">encoder to use during read operation</param>
		/// <returns>The string contents of the stream</returns>
		public string ReadFromFile(Encoding encoder) {
			int i;

			if (FileStream == null) {
				throw new ObjectDisposedException("fileStream", "storage stream no longer available");
			}
			Stream stream = new MemoryStream();
			var bs = new byte[Length];
			Seek(0, SeekOrigin.Begin);
			while ((i = Read(bs, 0, 1024)) > 0) {
				stream.Write(bs, 0, i);
			}
			stream.Seek(0, SeekOrigin.Begin);
			return new StreamReader(stream, encoder).ReadToEnd();
		}
	}
}