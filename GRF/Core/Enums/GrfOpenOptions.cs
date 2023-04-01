using System;

// ReSharper disable CheckNamespace
namespace GRF.Core {
	[Flags]
	public enum GrfLoadOptions {
		Normal = 1 << 0,
		OpenOrNew = 1 << 1,
		New = 1 << 2,
		Repair = 1 << 3,
	}
}