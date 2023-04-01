using System.Collections.Generic;

namespace Utilities.IndexProviders {
	public class DefaultIndexProvider : AbstractProvider {
		private readonly int _from;
		private readonly int _length;

		public DefaultIndexProvider(int from, int length) {
			_from = @from;
			_length = length;
		}

		public override List<int> GetIndexes() {
			List<int> indexes = new List<int>();

			for (int i = 0; i < _length; i++) {
				indexes.Add(_from + i);
			}

			return indexes;
		}
	}
}