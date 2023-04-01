using System;
using System.IO;
using System.Text;
using GRF.Graphics;
using GRF.Image;
using GRF.System;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.IO {
	public class ByteReaderStream : IBinaryReader {
		private readonly byte[] _data2 = new byte[2];
		private readonly byte[] _data4 = new byte[4];
		private readonly byte[] _data8 = new byte[8];
		internal object SharedLock = new object();
		private byte[] _data = new byte[4];

		//public static TkDictionary<string, int> Streams = new TkDictionary<string, int>();
		private DisposableScope<FileStream> _scope;

		public ByteReaderStream(FileStream stream) {
			Stream = stream;

			//if (Stream != null) {
			//	//CLHelper.WriteLine = "Stream opened : " + Stream.Name;
			//	Streams[Stream.Name]++;
			//
			//	if (Streams[Stream.Name] > 1) {
			//		Z.F();
			//	}
			//}
		}

		public ByteReaderStream() {
		}

		public ByteReaderStream(string file) : this(new FileStream(file, FileMode.Open, FileAccess.Read)) {
			FileName = file;
		}

		public string FileName { get; private set; }

		public FileStream Stream { get; private set; }

		#region IBinaryReader Members

		public int Length {
			get { return (int) Stream.Length; }
		}

		public long LengthLong { get { return Stream.Length; } }

		public bool CanRead {
			get { return Stream.Position < Stream.Length; }
		}

		public int Position {
			get { return (int) Stream.Position; }
			set { Stream.Seek(value, SeekOrigin.Begin); }
		}

		public uint PositionUInt {
			get { return (uint) Stream.Position; }
			set { Stream.Seek(value, SeekOrigin.Begin); }
		}

		public long PositionLong {
			get { return Stream.Position; }
			set { Stream.Seek(value, SeekOrigin.Begin); }
		}

		public void WriteToFile(string file) {
			int position = Position;
			Stream.Seek(0, SeekOrigin.Begin);
			byte[] data = new byte[Stream.Length];
			Stream.Read(data, 0, data.Length);

			File.WriteAllBytes(file, data);
			Position = position;
		}

		public string String(int length) {
			_data = new byte[length];
			Stream.Read(_data, 0, length);
			return EncodingService.DisplayEncoding.GetString(_data, 0, length);
		}

		public string String(int length, char cut) {
			_data = new byte[length];
			Stream.Read(_data, 0, length);
			return EncodingService.DisplayEncoding.GetString(_data, 0, length, cut);
		}

		public string StringANSI(int length) {
			_data = new byte[length];
			Stream.Read(_data, 0, length);
			return EncodingService.Ansi.GetString(_data, 0, length);
		}

		public string StringUnicode(int length) {
			_data = new byte[length];
			Stream.Read(_data, 0, length);
			return Encoding.Unicode.GetString(_data, 0, length);
		}

		public void Forward(int numberOfBytes) {
			Stream.Seek(numberOfBytes, SeekOrigin.Current);
		}

		public byte Byte() {
			return (byte) Stream.ReadByte();
		}

		public byte[] Bytes(int count) {
			byte[] data = new byte[count];
			Stream.Read(data, 0, count);
			return data;
		}

		public char Char() {
			return (char) Byte();
		}

		public char[] ArrayChar(int count) {
			char[] array = new char[count];

			for (int i = 0; i < count; i++) {
				array[i] = Char();
			}

			return array;
		}

		public bool Bool() {
			return Int32() != 0;
		}

		public bool ByteBool() {
			return Byte() != 0;
		}

		public bool[] ArrayBool(int count) {
			bool[] array = new bool[count];

			for (int i = 0; i < count; i++) {
				array[i] = Bool();
			}

			return array;
		}

		public short Int16() {
			Stream.Read(_data2, 0, 2);
			return BitConverter.ToInt16(_data2, 0);
		}

		public short[] ArrayInt16(int count) {
			short[] array = new short[count];

			for (int i = 0; i < count; i++) {
				array[i] = Int16();
			}

			return array;
		}

		public ushort UInt16() {
			Stream.Read(_data2, 0, 2);
			return BitConverter.ToUInt16(_data2, 0);
		}

		public ushort[] ArrayUInt16(int count) {
			ushort[] array = new ushort[count];

			for (int i = 0; i < count; i++) {
				array[i] = UInt16();
			}

			return array;
		}

		public int Int32() {
			Stream.Read(_data4, 0, 4);
			return BitConverter.ToInt32(_data4, 0);
		}

		public int[] ArrayInt32(int count) {
			int[] array = new int[count];

			for (int i = 0; i < count; i++) {
				array[i] = Int32();
			}

			return array;
		}

		public uint UInt32() {
			Stream.Read(_data4, 0, 4);
			return BitConverter.ToUInt32(_data4, 0);
		}

		public uint[] ArrayUInt32(int count) {
			uint[] array = new uint[count];

			for (int i = 0; i < count; i++) {
				array[i] = UInt32();
			}

			return array;
		}

		public long Int64() {
			Stream.Read(_data8, 0, 8);
			return BitConverter.ToInt64(_data8, 0);
		}

		public long[] ArrayInt64(int count) {
			long[] array = new long[count];

			for (int i = 0; i < count; i++) {
				array[i] = Int64();
			}

			return array;
		}

		public ulong UInt64() {
			Stream.Read(_data8, 0, 8);
			return BitConverter.ToUInt64(_data8, 0);
		}

		public ulong[] ArrayUInt64(int count) {
			ulong[] array = new ulong[count];

			for (int i = 0; i < count; i++) {
				array[i] = UInt64();
			}

			return array;
		}

		public float Float() {
			Stream.Read(_data4, 0, 4);
			return BitConverter.ToSingle(_data4, 0);
		}

		public float[] ArrayFloat(int number) {
			float[] array = new float[number];

			for (int i = 0; i < number; i++) {
				array[i] = Float();
			}

			return array;
		}

		public double Double() {
			Stream.Read(_data8, 0, 8);
			return BitConverter.ToDouble(_data8, 0);
		}

		public double[] ArrayDouble(int count) {
			double[] array = new double[count];

			for (int i = 0; i < count; i++) {
				array[i] = Double();
			}

			return array;
		}

		public GrfColor GrfColor() {
			_data = Bytes(4);
			return new GrfColor(_data[0], _data[1], _data[2], _data[3]);
		}

		public Point Point() {
			_data = Bytes(8);
			return new Point(_data, 0);
		}

		public Vertex Vertex() {
			_data = Bytes(12);
			return new Vertex(_data, 0);
		}

		#endregion

		public void SetStream(FileStream stream) {
			Stream = stream;
		}

		public void SetStream(DisposableScope<FileStream> scope) {
			_scope = scope;
			Stream = _scope.Value;
		}

		public void Close() {
			if (Stream != null) {
				//if (Stream != null && Stream.CanSeek) {
				//	//CLHelper.WriteLine = "Stream closed : " + Stream.Name;
				//	Streams[Stream.Name]--;
				//}
				//
				//if (_scope != null) {
				//	if (_scope.Value != null && _scope.Value.CanSeek) {
				//		Streams[_scope.Value.Name]--;
				//	}
				//}

				Stream.Close();
			}
		}

		public void Open(string file) {
			Close();
			Stream = new FileStream(file, FileMode.Open, FileAccess.Read);

			if (Stream != null) {
				//Streams[Stream.Name]++;
				//
				//if (Streams[Stream.Name] > 1) {
				//	Z.F();
				//}
			}
		}

		public void Delete() {
			Stream = null;
		}
	}
}