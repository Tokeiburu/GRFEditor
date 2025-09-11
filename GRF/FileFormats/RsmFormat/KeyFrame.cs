using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GRF.FileFormats.RsmFormat {
	[Serializable]
	public abstract class KeyFrame {
		public int Frame;

		public abstract KeyFrame Copy();
	}
}
