using System.Collections.Generic;

namespace Database.Commands {
	internal class AddTupleDico<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TValue _tupleParent;
		private readonly CCallbacks.AddTupleDicoCallback<TKey, TValue> _callback;
		private readonly bool _isModified;
		private readonly Dictionary<TKey, TValue> _dico;
		private readonly AddTuple<TKey, TValue> _propChanged;
		private readonly TKey _key;

		public TKey ParentKey { get; private set; }

		public AddTupleDico(TValue tupleParent, DbAttribute attributeTable, TKey key, TValue tuple, CCallbacks.AddTupleDicoCallback<TKey, TValue> callback = null) {
			_tupleParent = tupleParent;
			_callback = callback;
			_key = key;
			_dico = (Dictionary<TKey, TValue>)tupleParent.GetRawValue(attributeTable.Index);
			_isModified = tupleParent.Modified;
			_propChanged = new AddTuple<TKey, TValue>(key, tuple, null);
			ParentKey = _tupleParent.GetKey<TKey>();
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			_propChanged.Execute(_dico);
			_tupleParent.Modified = true;

			if (_callback != null) {
				_callback(_tupleParent, _key, _propChanged.Tuple, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			_propChanged.Undo(_dico);
			_tupleParent.Modified = _isModified;

			if (_callback != null) {
				_callback(_tupleParent, _key, _propChanged.Tuple, false);
			}
		}

		public string CommandDescription {
			get {
				return _propChanged.CommandDescription;
			}
		}
	}
}
