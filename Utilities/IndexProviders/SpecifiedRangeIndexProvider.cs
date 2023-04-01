using System.Collections.Generic;
using System.Linq;

namespace Utilities.IndexProviders {
	public class SpecifiedRangeIndexProvider : AbstractProvider {
		private readonly List<int> _indexes = new List<int>();

		public SpecifiedRangeIndexProvider(IEnumerable<int> indexes) {
			List<int> indexes2 = indexes.ToList();

			for (int i = 0; i < indexes2.Count; i += 2) {
				for (int j = 0; j < indexes2[i + 1]; j++) {
					_indexes.Add(indexes2[i] + j);
				}
			}
		}

		public override List<int> GetIndexes() {
			return _indexes;
		}
	}
}