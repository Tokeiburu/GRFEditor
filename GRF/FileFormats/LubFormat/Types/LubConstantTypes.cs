using System;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.FileFormats.LubFormat.Types {
	public class ValueTypeProvider {
		public static LubValueType GetConstant(byte type, byte[] data, ref int offset, LubHeader header) {
			switch (type) {
				case 0:
					return new LubNull();
				case 1:
					bool value = BitConverter.ToBoolean(data, offset);
					offset++;
					return new LubBoolean(value);
				case 3:
					double number = BitConverter.ToDouble(data, offset);
					offset += header.SizeOfNumbers;
					return new LubNumber(number);
				case 4:
					int size = BitConverter.ToInt32(data, offset);
					offset += 4;
					string temp = Lub.Escape(EncodingService.DisplayEncoding.GetString(data, offset, size, '\0'));
					offset += size;
					return new LubString(temp);
			}

			throw new Exception("Unknown constant type.");
		}
	}
}