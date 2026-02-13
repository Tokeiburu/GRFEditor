using System;

namespace GRF.FileFormats.RsmFormat {
	[Serializable]
	public abstract class KeyFrame {
		public int Frame;

		public abstract KeyFrame Copy();
	}
}
