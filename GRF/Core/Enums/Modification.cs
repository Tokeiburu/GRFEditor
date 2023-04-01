using System;

// ReSharper disable CheckNamespace
namespace GRF.Core {
	/// <summary>
	/// Flag used to indicate the current state of the container entry.
	/// </summary>
	[Flags]
	public enum Modification {
		Removed = 1 << 1,
		Added = 1 << 2,
		GrfMerge = 1 << 3,
		FileNameRenamed = 1 << 4,
		Encrypt = 1 << 5,
		Decrypt = 1 << 6,
		Special = 1 << 7,
		DoNotRewrite = 1 << 8,
	}
}