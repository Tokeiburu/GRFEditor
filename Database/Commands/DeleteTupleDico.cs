using System.Collections.Generic;

namespace Database.Commands {
	public sealed class DeleteTupleDico<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TValue _tupleParent;
		private readonly bool _isModified;
		private readonly DeleteTuple<TKey, TValue> _propChanged;
		private readonly Dictionary<TKey, TValue> _table;

		public TKey ParentKey { get; private set; }

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, TKey dkey, TValue dvalue, TKey newDkey, bool executed);

		public DeleteTupleDico(TValue tupleParent, DbAttribute attributeTable, TKey key) {
			_tupleParent = tupleParent;
			_isModified = tupleParent.Modified;
			_table = (Dictionary<TKey, TValue>)tupleParent.GetRawValue(attributeTable.Index);
			_propChanged = new DeleteTuple<TKey, TValue>(key);
			Key = key;
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
					string.Format("[{0}], remove [{1}]", ParentKey, Key);
			}
		}
	}
}
