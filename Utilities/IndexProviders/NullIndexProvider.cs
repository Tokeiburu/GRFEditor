using System.Collections.Generic;

namespace Utilities.IndexProviders {
	public class NullIndexProvider : AbstractProvider {
		public override List<int> GetIndexes() {
			return new List<int>();
		}
	}
}