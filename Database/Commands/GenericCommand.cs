namespace Database.Commands {
	internal class GenericCommand<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TKey _key;
		private readonly string _display;
		private readonly CCallbacks.GenericCallback<TKey> _callback;

		public delegate void TupleCallback(bool executed);

		public GenericCommand(TKey key, string display, CCallbacks.GenericCallback<TKey> callback) {
			_key = key;
			_display = display;
			_callback = callback;
			Key = key;
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			_callback(Key, true);
		}

		public void Undo(Table<TKey, TValue> table) {
			_callback(Key, false);
		}

		public string CommandDescription {
			get { return string.Format("[{0}] {1}", _key, _display); }
		}
	}
}
