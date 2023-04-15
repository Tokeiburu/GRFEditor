using System;

namespace GRF.FileFormats.GatFormat {
	public enum GatType {
		NoGat = -1,
		Walkable = 0,
		NoWalkable,
		NoWalkableNoSnipable,
		Walkable2 = 3,
		Unknown,
		NoWalkableSnipable,
		Walkable3 = 6,
		Weird0 = Int32.MinValue + 0,
		Weird1 = Int32.MinValue + 1,
		Weird2 = Int32.MinValue + 2,
		Weird3 = Int32.MinValue + 3,
		Weird4 = Int32.MinValue + 4,
		Weird5 = Int32.MinValue + 5,
		Weird6 = Int32.MinValue + 6,
		Weird7 = Int32.MinValue + 7,
		Weird8 = Int32.MinValue + 8,
		Weird9 = Int32.MinValue + 9,
	}
}