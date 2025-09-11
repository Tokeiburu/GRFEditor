using System;

// ReSharper disable CheckNamespace
namespace GRF.Core {
	[Flags]
	public enum ExtractOptions {
		Normal = 1 << 0,
		OverrideCpuPerf = 1 << 1,
		OpenAfterExtraction = 1 << 2,
		UseAppDataPathToExtract = 1 << 3,
		SingleThreaded = 1 << 4,
		ExtractAllInSameFolder = 1 << 5,
		IgnoreCase = 1 << 6,
	}
}