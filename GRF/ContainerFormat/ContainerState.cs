using System;

namespace GRF.ContainerFormat {
	[Flags]
	public enum ContainerState {
		Normal = 1 << 0,
		Error = 1 << 1,
		LoadCancelled = 1 << 2,
		Cancelled = 1 << 3,
	}
}