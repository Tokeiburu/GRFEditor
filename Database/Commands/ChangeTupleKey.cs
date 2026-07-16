using System.Collections.Generic;

namespace Database.Commands {
	public class ChangeTupleKey<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly TKey _oldKey;
		private readonly TKey _newKey;
		private readonly CCallbacks.KeyTupleCallback<TKey> _callback;
		private TValue _conflict;
		private ChangeTupleProperty<TKey, TValue> _changePropety;
		private bool _isModel;

		internal ChangeTupleKey(TKey oldKey, TKey newKey, CCallbacks.KeyTupleCallback<TKey> callback) {
			_oldKey = oldKey;
			_newKey = newKey;
			_callback = callback;
			Key = oldKey;
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			if (_changePropety == null) {
				var tuple = table.GetTuple(_oldKey);
				_changePropety = new ChangeTupleProperty<TKey, TValue>(tuple, table.AttributeList.PrimaryAttribute, _newKey);

				if (table.AttributeList.Count > 0 && typeof(IModel).IsAssignableFrom(table.AttributeList[1].DataType)) {
					_isModel = true;
				}
			}

			if (table.ContainsKey(_newKey)) {
				_conflict = table.GetTuple(_newKey);
			}

			if (_isModel) {
				var tuple = table.GetTuple(_oldKey);
				((IModel)tuple.GetValue(1)).SetKey(_newKey);
			}

			table.ChangeKey(_oldKey, _newKey);
			_changePropety.Execute(table);

			if (_callback != null) {
				_callback(_oldKey, _newKey, true);
			}
		}

		internal void Execute(Dictionary<TKey, TValue> table) {
			if (_changePropety == null) {
				_changePropety = new ChangeTupleProperty<TKey, TValue>(table[_oldKey], 0, _newKey);
			}

			if (table.ContainsKey(_newKey)) {
				_conflict = table[_newKey];
			}

			if (_isModel) {
				var tuple = table[_oldKey];
				((IModel)tuple.GetValue(1)).SetKey(_newKey);
			}

			_changeKey(table, _oldKey, _newKey);
			_changePropety.Execute(table);

			if (_callback != null) {
				_callback(_oldKey, _newKey, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			_changePropety.Undo(table);
			table.ChangeKey(_newKey, _oldKey);

			if (_isModel) {
				var tuple = table.GetTuple(_oldKey);
				((IModel)tuple.GetValue(1)).SetKey(_oldKey);
			}

			if (_conflict != null) {
				table.Add(_newKey, _conflict);
			}

			if (_callback != null) {
				_callback(_oldKey, _newKey, false);
			}
		}

		internal void Undo(Dictionary<TKey, TValue> table) {
			_changePropety.Undo(null);
			_changeKey(table, _newKey, _oldKey);

			if (_isModel) {
				var tuple = table[_oldKey];
				((IModel)tuple.GetValue(1)).SetKey(_oldKey);
			}

			if (_conflict != null) {
				table.Add(_newKey, _conflict);
			}

			if (_callback != null) {
				_callback(_oldKey, _newKey, false);
			}
		}

		private void _changeKey(Dictionary<TKey, TValue> table, TKey oldKey, TKey newKey) {
			if (table.ContainsKey(oldKey)) {
				TValue temp = table[oldKey];
				table.Remove(oldKey);
				table[newKey] = temp;
			}
		}

		public string CommandDescription {
			get { return string.Format("Change [{0}] to [{1}]", _oldKey, _newKey); }
		}
	}
}
