namespace Database.Commands {
	public class ChangeTuplePropertyAndUpdate<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TValue _tuple;
		private readonly DbAttribute _attribute;
		private readonly object _newValue;
		private readonly ChangeTupleCallback _callback;
		private object _oldValue;
		private bool _isModified;

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, object oldValue, object newValue, bool executed);

		public ChangeTuplePropertyAndUpdate(TValue tuple, DbAttribute attribute, object newValue, ChangeTupleCallback callback = null) {
			_tuple = tuple;
			_attribute = attribute;
			_newValue = newValue;
			_callback = callback;
			Key = tuple.GetKey<TKey>();
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			_oldValue = _tuple.GetValue(_attribute.Index);
			_isModified = _tuple.Modified;

			_tuple.SetValue(_attribute, _newValue);
			table.OnTupleModified(_tuple.GetKey<TKey>(), _tuple);

			if (_callback != null) {
				_callback(_tuple, _attribute, _newValue, _oldValue, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			_tuple.SetValue(_attribute, _oldValue);
			_tuple.Modified = _isModified;
			table.Add(_tuple.GetKey<TKey>(), _tuple);

			table.OnTupleModified(_tuple.GetKey<TKey>(), _tuple);

			if (_callback != null) {
				_callback(_tuple, _attribute, _newValue, _oldValue, false);
			}
		}

		public string CommandDescription {
			get { return string.Format("[{0}], change '{1}' with '{2}'", _tuple.GetKey<TKey>(), _attribute.DisplayName, (_newValue ?? "").ToString().Replace("\r\n", "\\r\\n").Replace("\n", "\\n")); }
		}
	}
}
