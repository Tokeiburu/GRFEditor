using System.Collections.Generic;
using System.ComponentModel;

namespace TokeiLibrary.WPF.Styles.ListView {
	public class DefaultComparer<T> : IComparer<T> {
		private readonly DefaultListViewComparer<T> _internalSearch = new DefaultListViewComparer<T>();
		private string _searchGetAccessor;

		public DefaultComparer() {
		}

		public DefaultComparer(bool enableAlphaNum) {
			if (enableAlphaNum) {
				_internalSearch = new DefaultListViewComparer<T>(true);
			}
		}

		#region IComparer<T> Members

		public int Compare(T x, T y) {
			if (_searchGetAccessor != null)
				return _internalSearch.Compare(x, y);

			return 0;
		}

		#endregion

		public void SetOrder(string searchGetAccessor, ListSortDirection direction) {
			if (searchGetAccessor != null) {
				_searchGetAccessor = searchGetAccessor;
				_internalSearch.SetSort(searchGetAccessor, direction);
			}
		}
	}
}
