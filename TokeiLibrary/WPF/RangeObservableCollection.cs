using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace TokeiLibrary.WPF {
	public class RangeObservableCollection<T> : ObservableCollection<T> {
		private bool _suppressNotification;

		public RangeObservableCollection() : base() {
			
		}

		public RangeObservableCollection(IEnumerable<T> coll) : base(coll) {
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
			if (!_suppressNotification)
				base.OnCollectionChanged(e);
		}

		public void Disable() {
			_suppressNotification = true;
		}

		public void Enable() {
			_suppressNotification = false;
		}

		public void Update() {
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public void UpdateAndEnable() {
			_suppressNotification = false;
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public void AddRange(IEnumerable<T> list) {
			if (list == null)
				throw new ArgumentNullException("list");

			_suppressNotification = true;

			foreach (T item in list) {
				Add(item);
			}
			_suppressNotification = false;
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public void RemoveRange(IEnumerable<T> list) {
			_suppressNotification = true;

			Dictionary<T, int> toMap = new Dictionary<T, int>();

			for (int index = 0; index < Items.Count; index++) {
				var hash = Items[index];
				toMap[hash] = index;
			}

			List<int> toRemove = new List<int>();

			foreach (T item in list) {
				int id;

				if (toMap.TryGetValue(item, out id)) {
					toRemove.Add(id);
				}
			}

			foreach (var id in toRemove.OrderByDescending(p => p)) {
				if (id < Items.Count)
					RemoveItem(id);
			}

			_suppressNotification = false;
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
}
