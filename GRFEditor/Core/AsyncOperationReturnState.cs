using System;

namespace GRFEditor.Core {
	[Flags]
	public enum AsyncOperationReturnState {
		None = 1 << 0,
		DoesNotRequireVisualReload = 1 << 2,
		//Reload = 1 << 4,
	}
}