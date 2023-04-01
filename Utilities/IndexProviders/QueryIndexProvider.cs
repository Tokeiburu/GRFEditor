using System;
using System.Collections.Generic;
using System.Linq;

namespace Utilities.IndexProviders {
	public class QueryIndexProvider : AbstractProvider {
		private readonly List<int> _indexes = new List<int>();

		public QueryIndexProvider(string query) {
			string[] subQuerries = query.Split(';').Where(p => !String.IsNullOrEmpty(p)).ToArray();

			foreach (string subQuery in subQuerries) {
				if (subQuery.Contains('-')) {
					int from = Int32.Parse(subQuery.Split('-')[0]);
					int to = Int32.Parse(subQuery.Split('-')[1]);

					for (int i = from; i <= to; i++) {
						_indexes.Add(i);
					}
				}
				else {
					_indexes.Add(Int32.Parse(subQuery));
				}
			}
		}

		public override List<int> GetIndexes() {
			return _indexes;
		}
	}
}