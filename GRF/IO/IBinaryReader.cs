using GRF.Graphics;
using GRF.Image;

namespace GRF.IO {
	public interface IBinaryReader {
		bool CanRead { get; }
		int Position { get; set; }
		uint PositionUInt { get; set; }
		long PositionLong { get; set; }
		int Length { get; }
		long LengthLong { get; }

		string String(int length);
		string String(int length, char cut);
		string StringANSI(int length);
		string StringUnicode(int length);

		void Forward(int numberOfBytes);
		void WriteToFile(string file);

		byte Byte();
		byte[] Bytes(int count);

		char Char();
		char[] ArrayChar(int count);

		bool Bool();
		bool ByteBool();
		bool[] ArrayBool(int count);

		short Int16();
		short[] ArrayInt16(int count);

		ushort UInt16();
		ushort[] ArrayUInt16(int count);

		int Int32();
		int[] ArrayInt32(int count);

		uint UInt32();
		uint[] ArrayUInt32(int count);

		long Int64();
		long[] ArrayInt64(int count);

		ulong UInt64();
		ulong[] ArrayUInt64(int count);

		float Float();
		float[] ArrayFloat(int count);

		double Double();
		double[] ArrayDouble(int count);

		GrfColor GrfColor();
		TkVector3 Vector3();
		TkVector2 Vector2();

		string GetSource();
	}
}