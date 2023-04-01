using System.Collections.Generic;
using Utilities;
using Utilities.Commands;

namespace Database.Commands {
	public class AddTuple<TKey, TValue> : ITableCommand<TKey, TValue>, ICombinableCommand
		where TValue : Tuple {
		private readonly TValue _tuple;
		private readonly bool _autoIncrement;
		private bool _isSet;
		private readonly CCallbacks.GenericTupleCallback<TKey> _callback;
		private TValue _conflict;
		private int _hash;
		public bool IgnoreConflict { get; set; }
		public bool IsCombinable { get; set; }

		internal AddTuple(TKey key, TValue tuple, bool autoIncrement, CCallbacks.GenericTupleCallback<TKey> callback) {
			_tuple = tuple;
			_autoIncrement = autoIncrement;
			_callback = callback;
			Key = key;
			IsCombinable = true;
		}

		internal AddTuple(TKey key, TValue tuple, CCallbacks.GenericTupleCallback<TKey> callback) : this(key, tuple, false, callback) {
		}

		public TKey Key { get; private set; }

		public TValue Tuple {
			get { return _tuple; }
		}

		public void Execute(Table<TKey, TValue> table) {
			if (_autoIncrement && typeof(TKey) == typeof(int)) {
				if (!_isSet) {
					_hash = _tuple.GetHash();
				}

				table.AutoIncrements[_hash]++;

				if (!_isSet) {
					Key = (TKey)(object)((int)(object)Key + table.AutoIncrements[_hash]);
					_tuple.SetRawValue(0, Key);
					_isSet = true;
				}
			}

			if (table.ContainsKey(Key) && !IgnoreConflict) {
				_conflict = table.GetTuple(Key);
				table.Remove(Key);
			}

			table.Add(Key, _tuple);

			if (_callback != null) {
				//_callback(_oldKey, _newKey, true);
			}
		}

		internal void Execute(Dictionary<TKey, TValue> table) {
			if (table.ContainsKey(Key) && !IgnoreConflict) {
				_conflict = table[Key];
				table.Remove(Key);
			}

			table.Add(Key, _tuple);
		}

		public void Undo(Table<TKey, TValue> table) {
			if (_autoIncrement && typeof(TKey) == typeof(int)) {
				table.AutoIncrements[_hash]--;
			}

			table.Remove(Key);

			if (_conflict != null) {
				table.Add(Key, _conflict);
			}

			if (_callback != null) {
				//_callback(_oldKey, _newKey, false);
			}
		}

		public void Undo(Dictionary<TKey, TValue> table) {
			table.Remove(Key);

			if (_conflict != null) {
				table.Add(Key, _conflict);
			}
		}

		public string CommandDescription {
			get { return string.Format("Add [{0}]", Key); }
		}

		public bool CanCombine(ICombinableCommand command) {
			if (!IsCombinable) return false;

			var changeTupleProperty = command as ChangeTupleProperty<TKey, TValue>;
			if (changeTupleProperty != null) {
				if (changeTupleProperty.Tuple == this._tuple) {
					return true;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var changeTupleProperty = command as ChangeTupleProperty<TKey, TValue>;

			if (changeTupleProperty != null) {
				if (changeTupleProperty.Tuple == this._tuple) {
					_tuple.SetRawValue(changeTupleProperty.Attribute, changeTupleProperty.NewValue);
				}
			}
		}
	}
}
