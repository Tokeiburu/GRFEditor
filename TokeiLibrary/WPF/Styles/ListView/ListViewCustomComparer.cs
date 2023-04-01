using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace TokeiLibrary.WPF.Styles.ListView {
	public abstract class ListViewCustomComparer : IComparer {
		protected int _columnIndex;
		protected ListSortDirection _direction;
		protected string _sortColumn;

		#region IComparer Members

		public abstract int Compare(object x, object y);

		#endregion

		public virtual void SetSort(string sortColumn, ListSortDirection dir) {
			_sortColumn = sortColumn;
			_direction = dir;
		}

		public virtual void SetSort(int columnIndex, ListSortDirection dir) {
			_columnIndex = columnIndex;
			_direction = dir;
		}
	}

	public abstract class ListViewCustomComparer<T> : IComparer<T> {
		protected int _columnIndex;
		protected ListSortDirection _direction;
		protected string _sortColumn;

		public virtual void SetSort(string sortColumn, ListSortDirection dir) {
			_sortColumn = sortColumn;
			_direction = dir;
		}

		public virtual void SetSort(int columnIndex, ListSortDirection dir) {
			_columnIndex = columnIndex;
			_direction = dir;
		}

		public abstract int Compare(T x, T y);
	}
}
