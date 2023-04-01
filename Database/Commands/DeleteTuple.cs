using System.Collections.Generic;

namespace Database.Commands {
	public class DeleteTuple<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TKey _key;
		private readonly TupleCallback _callback;
		private TValue _tuple;

		public delegate void TupleCallback(TKey key, TValue value, bool executed);

		public DeleteTuple(TKey key, TupleCallback callback = null) {
			_key = key;
			_callback = callback;
			Key = key;
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			_tuple = table.TryGetTuple(_key);

			if (_tuple != null) {
				table.Remove(_tuple.GetKey<TKey>());

				if (_callback != null) {
					_callback(_tuple.GetKey<TKey>(), _tuple, true);
				}
			}
		}

		internal void Execute(Dictionary<TKey, TValue> table) {
			if (table.TryGetValue(_key, out _tuple)) {
				table.Remove(_tuple.GetKey<TKey>());

				if (_callback != null) {
					_callback(_tuple.GetKey<TKey>(), _tuple, true);
				}
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			if (_tuple != null) {
				table.Add(_tuple.GetKey<TKey>(), _tuple);

				if (_callback != null) {
					_callback(_tuple.GetKey<TKey>(), _tuple, false);
				}
			}
		}

		internal void Undo(Dictionary<TKey, TValue> table) {
			if (_tuple != null) {
				table.Add(_tuple.GetKey<TKey>(), _tuple);

				if (_callback != null) {
					_callback(_tuple.GetKey<TKey>(), _tuple, false);
				}
			}
		}

		public string CommandDescription {
			get { return string.Format("Remove [{0}]", _tuple == null ? "" : _tuple.GetKey<TKey>().ToString()); }
		}
	}
}
