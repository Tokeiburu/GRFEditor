using System.Collections.Generic;
using System.Linq;

namespace Utilities.IndexProviders {
	public class GroupIndexProvider : AbstractProvider {
		private readonly IEnumerable<IEnumerable<int>> _indexes;
		private List<IIndexProvider> _bufferedIndexes;

		public GroupIndexProvider(IEnumerable<IEnumerable<int>> indexes) {
			_indexes = indexes;
		}

		public IEnumerable<IEnumerable<int>> Groups {
			get {
				return _indexes;
			}
		}

		public override IEnumerable<IIndexProvider> Providers {
			get {
				return Groups.Select(_detect);
			}
		}

		public override List<int> GetIndexes() {
			return null;
		}

		public override object Next() {
			if (_position < 0) {
				_bufferedIndexes = Providers.ToList();
			}

			if (++_position >= _bufferedIndexes.Count)
				return null;

			return _bufferedIndexes[_position];
		}

		private IIndexProvider _detect(IEnumerable<int> @group) {
			if (@group == null)
				return new NullIndexProvider();

			if (GroupAs == null) {
				List<int> indexes = @group.ToList();

				if (indexes.Count % 2 != 0) {
					return new SpecifiedIndexProvider(indexes);
				}

				for (int i = 0; i < indexes.Count; i++) {
					if (i % 2 == 0 && indexes[i] < 0) {
						return new SpecifiedRangeIndexProvider(indexes);
					}

					if (i % 2 == 1 && indexes[i] <= 0) {
						return new SpecifiedIndexProvider(indexes);
					}
				}

				return new SpecifiedIndexProvider(indexes);
			}

			if (GroupAs == typeof(NullIndexProvider)) return new NullIndexProvider();
			if (GroupAs == typeof(SpecifiedRangeIndexProvider)) return new SpecifiedRangeIndexProvider(@group);
			if (GroupAs == typeof(SpecifiedIndexProvider)) return new SpecifiedIndexProvider(@group);

			return new NullIndexProvider();
		}
	}
}