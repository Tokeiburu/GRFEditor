using System;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor {
	[Flags]
	public enum FragmentType {
		None = 1 << 0,
		IfElse = 1 << 2,
		ElseIf = 1 << 3,
		If = 1 << 4,
		WhileLoop = 1 << 5,
		ForLoop = 1 << 6,
		NormalExecution = 1 << 7,
		NotReferenced = 1 << 8,
	}
}