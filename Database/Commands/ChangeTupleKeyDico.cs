using System.Collections.Generic;

namespace Database.Commands {
	public sealed class ChangeTupleKeyDico<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TValue _tupleParent;
		private readonly bool _isModified;
		private TValue _initValue;
		private TValue _oldValue;
		private readonly ChangeTupleKey<TKey, TValue> _propChanged;
		private readonly Dictionary<TKey, TValue> _table;
		private readonly TKey _oldKey;
		private readonly TKey _newKey;

		public bool SubModified { get; set; }

		public TValue NewValue { get; set; }

		public TValue OldValue {
			get { return _oldValue; }
			set {
				if (_initValue == null) {
					_initValue = value;
				}

				_oldValue = value;
			}
		}

		public TValue InitialValue {
			get { return _initValue; }
			set { _initValue = value; }
		}

		public TKey ParentKey { get; private set; }

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, TKey dkey, TValue dvalue, TKey newDkey, bool executed);

		public ChangeTupleKeyDico(TValue tupleParent, DbAttribute attributeTable, TKey oldKey, TKey newKey, ChangeTupleCallback callback = null) {
			_tupleParent = tupleParent;
			_table = (Dictionary<TKey, TValue>)tupleParent.GetRawValue(attributeTable.Index);
			_isModified = tupleParent.Modified;
			_oldKey = oldKey;
			_newKey = newKey;
			Key = oldKey;
			_propChanged = new ChangeTupleKey<TKey, TValue>(oldKey, newKey, null);
			ParentKey = _tupleParent.GetKey<TKey>();
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			_propChanged.Execute(_table);
			_tupleParent.Modified = true;
		}

		public void Undo(Table<TKey, TValue> table) {
			_propChanged.Undo(_table);
			_tupleParent.Modified = _isModified;
		}

		public string CommandDescription {
			get {
				return
					string.Format("[{0}], change [{1}] to [{2}]", _tupleParent.GetKey<TKey>(), _oldKey, _newKey);
			}
		}
	}
}
