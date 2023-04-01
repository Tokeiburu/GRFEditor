using System.Collections.Generic;

namespace Utilities.IndexProviders {
	public interface IIndexProvider {
		List<int> GetIndexes();
	}
}