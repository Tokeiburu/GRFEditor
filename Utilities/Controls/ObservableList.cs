using System.Collections;
using System.Collections.Generic;

namespace Utilities.Controls {
	public abstract class ObservableList {
		public delegate void ObservableListEventHandler(object sender, ObservabableListEventArgs args);
	}

	public class ObservableList<T> : IList<T> {
		private readonly List<T> _values = new List<T>();

		public event ObservableList.ObservableListEventHandler CollectionChanged;

		private void _onCollectionChanged(ObservabableListEventArgs args) {
			ObservableList.ObservableListEventHandler handler = CollectionChanged;
			if (handler != null) handler(this, args);
		}

		public int IndexOf(T item) {
			return _values.IndexOf(item);
		}

		public void Insert(int index, T item) {
			_values.Insert(index, item);
			_onCollectionChanged(new ObservabableListEventArgs(new List<T> { item }, ObservableListEventType.Added));
		}

		public void RemoveAt(int index) {
			T item = _values[index];
			_values.RemoveAt(index);
			_onCollectionChanged(new ObservabableListEventArgs(new List<T> { item }, ObservableListEventType.Removed));
		}

		public T this[int index] {
			get { return _values[index]; }
			set {
				_values[index] = value;
				_onCollectionChanged(new ObservabableListEventArgs(new List<T> { _values[index] }, ObservableListEventType.Modified));
			}
		}

		public void Add(T item) {
			if (!_values.Contains(item)) {
				_values.Add(item);
				_onCollectionChanged(new ObservabableListEventArgs(new List<T> { item }, ObservableListEventType.Added));
			}
		}

		public void Clear() {
			List<T> items = new List<T>(_values);
			_values.Clear();

			if (items.Count > 0)
				_onCollectionChanged(new ObservabableListEventArgs(items, ObservableListEventType.Removed));
		}

		public bool Contains(T item) {
			return _values.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex) {
			_values.CopyTo(array, arrayIndex);
		}

		bool ICollection<T>.Remove(T item) {
			if (_values.Remove(item)) {
				_onCollectionChanged(new ObservabableListEventArgs(new List<T> { item }, ObservableListEventType.Removed));
				return true;
			}

			return false;
		}

		public int Count { get { return _values.Count; } }
		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public void AddRange(IEnumerable<T> items) {
			List<T> newItems = new List<T>();

			foreach (T item in items) {
				if (!_values.Contains(item))
					newItems.Add(item);
			}

			if (newItems.Count > 0) {
				_values.AddRange(newItems);
				_onCollectionChanged(new ObservabableListEventArgs(newItems, ObservableListEventType.Added));
			}
		}

		public void Remove(T item) {

		}

		public IEnumerator<T> GetEnumerator() {
			return _values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	public class ObservabableListEventArgs {
		public IList Items { get; set; }
		public ObservableListEventType Action { get; private set; }

		public ObservabableListEventArgs(IList items, ObservableListEventType action) {
			Items = items;
			Action = action;
		}
	}

	public enum ObservableListEventType {
		None,
		Added,
		Modified,
		Removed
	}
}
