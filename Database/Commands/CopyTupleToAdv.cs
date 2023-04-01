namespace Database.Commands {
	internal class CopyTupleToAdv<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly Table<TKey, TValue> _tableSource;
		private readonly TKey _oldKey;
		private readonly Table<TKey, TValue> _tableDest;
		private readonly TKey _newKey;
		private readonly CCallbacks.CopyToTupleCallback<TKey, TValue> _callback;
		private TValue _conflict;

		public CopyTupleToAdv(Table<TKey, TValue> tableSource, TKey oldkey, Table<TKey, TValue> tableDest, TKey newKey, CCallbacks.CopyToTupleCallback<TKey, TValue> callback) {
			_tableSource = tableSource;
			_oldKey = oldkey;
			_tableDest = tableDest;
			_newKey = newKey;
			_callback = callback;
			Key = oldkey;
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			if (_tableDest.ContainsKey(_newKey)) {
				_conflict = _tableDest.GetTuple(_newKey);
			}

			table.Copy(_tableSource, _oldKey, _newKey);

			if (_callback != null) {
				_callback(_tableSource, _oldKey, _tableDest, _newKey, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			_tableDest.Remove(_newKey);

			if (_conflict != null) {
				_tableDest.Add(_newKey, _conflict);
			}

			if (_callback != null) {
				_callback(_tableSource, _oldKey, _tableDest, _newKey, false);
			}
		}

		public string CommandDescription {
			get { return string.Format("Copy [{0}] to [{1}]", _oldKey, _newKey); }
		}
	}
}
