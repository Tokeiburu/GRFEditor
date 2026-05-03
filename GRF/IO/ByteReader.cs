using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using GRF.Graphics;
using GRF.Image;
using Utilities.Services;

namespace GRF.IO {
	public class ByteReader : IBinaryReader {
		public readonly byte[] _data;
		private int _byteRead;
		private int _offset;
		private string _source;

		public ByteReader(string file) : this(File.ReadAllBytes(file)) {
			_source = file;
		}

		public ByteReader(string file, int offset) : this(File.ReadAllBytes(file), offset) {
			_source = file;
		}

		public ByteReader(byte[] data, int offset = 0) {
			_data = data;
			_offset = offset;
		}

		#region IBinaryReader Members

		public bool CanRead {
			get { return Position < _data.Length; }
		}

		public int Length {
			get { return _data.Length; }
		}

		public int CanReadLength {
			get { return _data.Length - Position; }
		}

		public long LengthLong { get { return _data.Length; } }

		public int Position {
			get { return _offset + _byteRead; }
			set {
				_byteRead = 0;
				_offset = value;
			}
		}

		public long PositionLong { get { return Position; } set { Position = (int)value; } }
		public uint PositionUInt { get { return (uint) Position; } set { Position = (int)value; } }

		public void WriteToFile(string file) {
			File.WriteAllBytes(file, _data);
		}

		public unsafe string String(int length) {
			_forward(length);
			if (_offset + length > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return EncodingService.DisplayEncoding.GetString(p, length);
			}
		}

		public unsafe string String(int length, char cut) {
			_forward(length);
			if (_offset + length > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				int len = 0;

				while (len < length && p[len] != 0)
					len++;
				
				return EncodingService.DisplayEncoding.GetString(p, len);
			}
		}

		public unsafe string StringANSI(int length) {
			_forward(length);
			if (_offset + length > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return EncodingService.Ansi.GetString(p, length);
			}
		}

		public string StringUnicode(int length) {
			_forward(length);
			return Encoding.Unicode.GetString(_data, _offset, length);
		}

		public void Forward(int numberOfBytes) {
			_forward(numberOfBytes);
		}

		public byte Byte() {
			_forward(1);
			return _data[_offset];
		}

		public byte Peek() {
			return _data[_offset];
		}

		public byte[] Bytes(int count) {
			_forward(count);
			byte[] data = new byte[count];
			Buffer.BlockCopy(_data, _offset, data, 0, count);
			return data;
		}

		public void Bytes(byte[] destination, int offset, int count) {
			_forward(count);
			Buffer.BlockCopy(_data, _offset, destination, offset, count);
		}

		public char Char() {
			_forward(1);
			return (char) _data[_offset];
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

		public unsafe short Int16() {
			_forward(2);
			if (_offset + 2 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(short*)p;
			}
		}

		public unsafe short[] ArrayInt16(int count) {
			short[] array = new short[count];
			int length = count * 2;
			_forward(length);
			if (_offset + length > _data.Length)
				throw new IndexOutOfRangeException();

			fixed (short* pDst = array)
			fixed (byte* p = &_data[_offset]) {
				Buffer.MemoryCopy(p, pDst, length, length);
			}

			return array;
		}

		public unsafe ushort UInt16() {
			_forward(2);
			if (_offset + 2 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(ushort*)p;
			}
		}

		public unsafe ushort[] ArrayUInt16(int count) {
			ushort[] array = new ushort[count];
			int length = count * 2;
			_forward(length);
			if (_offset + length > _data.Length)
				throw new IndexOutOfRangeException();
			
			fixed (ushort* pDst = array)
			fixed (byte* p = &_data[_offset]) {
				Buffer.MemoryCopy(p, pDst, length, length);
			}

			return array;
		}

		public unsafe int Int32() {
			_forward(4);
			if (_offset + 4 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(int*)p;
			}
		}

		public int Int32BigEndian() {
			_forward(4);
			return (_data[0 + _offset] << 24) + (_data[1 + _offset] << 16) + (_data[2 + _offset] << 8) + _data[3 + _offset];
		}

		public int[] ArrayInt32(int count) {
			int[] array = new int[count];

			for (int i = 0; i < count; i++) {
				array[i] = Int32();
			}

			return array;
		}

		public unsafe uint UInt32() {
			_forward(4);
			if (_offset + 4 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(uint*)p;
			}
		}

		public uint[] ArrayUInt32(int count) {
			uint[] array = new uint[count];

			for (int i = 0; i < count; i++) {
				array[i] = UInt32();
			}

			return array;
		}

		public unsafe long Int64() {
			_forward(8);
			if (_offset + 8 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(long*)p;
			}
		}

		public long[] ArrayInt64(int count) {
			long[] array = new long[count];

			for (int i = 0; i < count; i++) {
				array[i] = Int64();
			}

			return array;
		}

		public unsafe ulong UInt64() {
			_forward(8);
			if (_offset + 8 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(ulong*)p;
			}
		}

		public ulong[] ArrayUInt64(int count) {
			ulong[] array = new ulong[count];

			for (int i = 0; i < count; i++) {
				array[i] = UInt64();
			}

			return array;
		}

		public unsafe float Float() {
			_forward(4);
			if (_offset + 4 > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return *(float*)p;
			}
		}

		public unsafe float[] ArrayFloat(int count) {
			float[] array = new float[count];
			int length = count * 4;
			_forward(length);
			if (_offset + length > _data.Length)
				throw new IndexOutOfRangeException();

			fixed (float* pDst = array)
			fixed (byte* p = &_data[_offset]) {
				Buffer.MemoryCopy(p, pDst, length, length);
			}

			return array;
		}

		public double Double() {
			_forward(8);
			return BitConverter.ToDouble(_data, _offset);
		}

		public double[] ArrayDouble(int count) {
			double[] array = new double[count];

			for (int i = 0; i < count; i++) {
				array[i] = Double();
			}

			return array;
		}

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void _forward(int numOfBytes) {
			_offset += _byteRead;
			_byteRead = numOfBytes;
		}

		public TkVector3 Vector3() {
			_forward(12);
			return new TkVector3(_data, _offset);
		}

		public TkVector2 Vector2() {
			_forward(8);
			return new TkVector2(_data, _offset);
		}

		public GrfColor GrfColor() {
			_forward(4);
			return new GrfColor(_data[_offset], _data[_offset + 1], _data[_offset + 2], _data[_offset + 3]);
		}

		public override string ToString() {
			return string.Format("Position = {0}; Length = {1}; CanRead = {2}", PositionLong, LengthLong, CanRead.ToString());
		}

		public string GetSource() {
			return _source;
		}

		public unsafe byte* GetPointer(int count) {
			_forward(count);
			if (_offset + count > _data.Length)
				throw new IndexOutOfRangeException();
			fixed (byte* p = &_data[_offset]) {
				return p;
			}
		}
	}
}