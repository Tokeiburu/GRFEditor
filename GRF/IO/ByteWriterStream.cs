using System;
using System.IO;
using Utilities.Services;

namespace GRF.IO {
	public class ByteWriterStream : IDisposable {
		internal object SharedLock = new object();

		public ByteWriterStream(FileStream stream) {
			Stream = stream;
		}

		public ByteWriterStream(string file) : this(new FileStream(file, FileMode.Open, FileAccess.Write)) {
		}

		public int Length {
			get { return (int) Stream.Length; }
		}

		public FileStream Stream { get; private set; }

		public bool CanRead {
			get { return false; }
		}

		public int Position {
			get { return (int) Stream.Position; }
			set { Stream.Seek(value, SeekOrigin.Begin); }
		}

		public uint PositionUInt {
			get { return (uint)Stream.Position; }
			set { Stream.Seek(value, SeekOrigin.Begin); }
		}

		public long PositionLong {
			get { return Stream.Position; }
			set { Stream.Seek(value, SeekOrigin.Begin); }
		}

		#region IDisposable Members

		public void Dispose() {
			if (Stream != null) {
				Stream.Close();
				Stream = null;
			}
		}

		#endregion

		public void SetStream(FileStream stream) {
			Stream = stream;
		}

		public void Write(int value) {
			Stream.Write(BitConverter.GetBytes(value), 0, 4);
		}

		public void Write(byte[] value) {
			Stream.Write(value, 0, value.Length);
		}

		public void Write(short value) {
			Stream.Write(BitConverter.GetBytes(value), 0, 2);
		}

		public void Write(float value) {
			Stream.Write(BitConverter.GetBytes(value), 0, 4);
		}

		public void Write(byte value) {
			WriteByte(value);
		}

		//public void Write(string value) {
		//    Stream.Write(BitConverter.GetBytes(value), 0, 4);
		//}

		public void WriteAnsi(string value) {
			byte[] data = EncodingService.Ansi.GetBytes(EncodingService.GetAnsiString(value));
			Stream.Write(data, 0, data.Length);
		}

		public void Write(double value) {
			Stream.Write(BitConverter.GetBytes(value), 0, 4);
		}

		public void WriteByte(Enum value) {
			Stream.WriteByte((byte) Convert.ToInt32(value));
		}

		public void WriteByte(byte value) {
			Stream.WriteByte(value);
		}

		public void WriteByte(char value) {
			Stream.WriteByte((byte) value);
		}
	}
}