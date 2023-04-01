using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GRF.FileFormats.BsonFormat {
	public enum BsonType : sbyte {
		Number = 1,
		String = 2,
		Object = 3,
		Array = 4,
		Binary = 5,
		Undefined = 6,
		Oid = 7,
		Boolean = 8,
		Date = 9,
		Null = 10,
		Regex = 11,
		Reference = 12,
		Code = 13,
		Symbol = 14,
		CodeWScope = 15,
		Integer = 16,
		TimeStamp = 17,
		Long = 18,
		MinKey = -1,
		MaxKey = 127
	}
}
