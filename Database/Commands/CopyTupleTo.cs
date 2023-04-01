namespace Database.Commands {
	internal class CopyTupleTo<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TKey _oldKey;
		private readonly TKey _newKey;
		private readonly CCallbacks.KeyTupleCallback<TKey> _callback;
		private TValue _conflict;

		public CopyTupleTo(TKey oldkey, TKey newKey, CCallbacks.KeyTupleCallback<TKey> callback) {
			_oldKey = oldkey;
			_newKey = newKey;
			_callback = callback;
			Key = oldkey;
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			if (table.ContainsKey(_newKey)) {
				_conflict = table.GetTuple(_newKey);
			}

			table.Copy(_oldKey, _newKey);

			if (_callback != null) {
				_callback(_oldKey, _newKey, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			table.Remove(_newKey);

			if (_conflict != null) {
				table.Add(_newKey, _conflict);
			}

			if (_callback != null) {
				_callback(_oldKey, _newKey, false);
			}
		}

		public string CommandDescription {
			get { return string.Format("Copy [{0}] to [{1}]", _oldKey, _newKey); }
		}
	}
}
