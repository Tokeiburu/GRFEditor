using System.Collections.Generic;
using Utilities;
using Utilities.Commands;
using Utilities.Extension;

namespace Database.Commands {
	public class ChangeTupleProperties<TKey, TValue> : ITableCommand<TKey, TValue>, ICombinableCommand
		where TValue : Tuple {
		private readonly TValue _tuple;
		public Dictionary<DbAttribute, Tuple<object, object>> Values = new Dictionary<DbAttribute, Tuple<object, object>>();
		private readonly ChangeTupleCallback _callback;
		private readonly bool _isModified;
		public TValue Tuple { get { return _tuple; } }

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, object oldValue, object newValue, bool executed);

		public ChangeTupleProperties(TValue tuple, DbAttribute attribute, object newValue, ChangeTupleCallback callback = null) {
			_tuple = tuple;
			Values[attribute] = new Tuple<object, object>(_tuple.GetValue(attribute.Index), newValue);
			_callback = callback;
			_isModified = _tuple.Modified;
			Key = tuple.GetKey<TKey>();
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			foreach (var tuple in Values) {
				_tuple.SetValue(tuple.Key, tuple.Value.Item2);
			}

			_tuple.Modified = true;

			if (_callback != null) {
				//_callback(_tuple, _attribute, NewValue, OldValue, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			foreach (var tuple in Values) {
				_tuple.SetValue(tuple.Key, tuple.Value.Item1);
			}

			_tuple.Modified = _isModified;

			if (_callback != null) {
				//_callback(_tuple, _attribute, NewValue, OldValue, false);
			}
		}

		public string CommandDescription {
			get { return string.Format("[{0}], change attributes... ({1})", _tuple.GetKey<TKey>(), Values.Count).Replace("\r\n", "\\r\\n").Replace("\n", "\\n"); }
		}

		public bool CanCombine(ICombinableCommand command) {
			var changeTupleProperties = command as ChangeTupleProperties<TKey, TValue>;
			if (changeTupleProperties != null) {
				if (changeTupleProperties.Tuple == this.Tuple) {
					return true;
				}
			}
			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var changeTupleProperties = command as ChangeTupleProperties<TKey, TValue>;
			if (changeTupleProperties != null) {
				if (changeTupleProperties.Tuple == this.Tuple) {
					foreach (var keyPair in changeTupleProperties.Values) {
						Values[keyPair.Key] = keyPair.Value;
					}
				}
			}
		}
	}
}
