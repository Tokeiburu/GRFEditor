using System;
using GRF.IO;

namespace GRF.FileFormats.LubFormat.Types {
	public class ValueTypeProvider {
		public static LubValueType GetConstant(byte type, IBinaryReader reader, LubHeader header) {
			switch(type) {
				case 0:
					return new LubNull();
				case 1:
					return new LubBoolean(reader.ByteBool());
				case 3:
					return new LubNumber(reader.Double());
				case 4:
					string temp = Lub.Escape(reader.String(reader.Int32(), '\0'));
					return new LubString(temp);
			}

			throw new Exception("Unknown constant type.");
		}
	}
}