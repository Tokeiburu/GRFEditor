using System.Collections.Generic;
using System.Linq;
using Utilities.Commands;

namespace Database.Commands {
	internal class GroupChangeTupleProperty<TKey, TValue> : ITableCommand<TKey, TValue>, ICombinableCommand
		where TValue : Tuple {
		private readonly TValue _tupleParent;
		private readonly List<TValue> _tuples;
		private readonly DbAttribute _attribute;
		private readonly CCallbacks.GroupChangeTupleCallback<TValue> _callback;
		private readonly List<bool> _isModified;
		private readonly bool _isParentModified;
		public List<object> NewValues { get; private set; }
		public List<object> OldValues { get; private set; }

		public DbAttribute Attribute {
			get { return _attribute; }
		}

		public GroupChangeTupleProperty(TValue tupleParent, List<TValue> tuples, DbAttribute attribute, object newValue, CCallbacks.GroupChangeTupleCallback<TValue> callback) {
			_tupleParent = tupleParent;
			_tuples = tuples;
			_attribute = attribute;

			NewValues = new List<object>(tuples.Count);
			OldValues = new List<object>(tuples.Count);
			_isModified = new List<bool>(tuples.Count);
			Keys = new List<TKey>(tuples.Count);

			for (int i = 0; i < tuples.Count; i++) {
				NewValues.Add(newValue);
				OldValues.Add(_tuples[i].GetValue(_attribute.Index));
				Keys.Add(_tuples[i].GetKey<TKey>());
				_isModified.Add(_tuples[i].Modified);
			}

			_isParentModified = _tupleParent == null ? false : _tupleParent.Modified;
			_callback = callback;
			Key = tuples[0].GetKey<TKey>();
		}

		public GroupChangeTupleProperty(List<TValue> tuples, DbAttribute attribute, object newValue, CCallbacks.GroupChangeTupleCallback<TValue> callback) : this(null, tuples, attribute, newValue, callback) {
		}

		public void Execute(Table<TKey, TValue> table) {
			for (int index = 0; index < _tuples.Count; index++) {
				var tuple = _tuples[index];
				tuple.SetValue(_attribute, NewValues[index]);
				NewValues[index] = tuple.GetValue(_attribute.Index);
			}

			if (_tupleParent != null) {
				_tupleParent.Modified = true;
			}

			if (_callback != null) {
				_callback(_tuples, _attribute, NewValues, OldValues, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			for (int index = 0; index < _tuples.Count; index++) {
				var tuple = _tuples[index];
				tuple.SetValue(_attribute, OldValues[index]);
				tuple.Modified = _isModified[index];
			}

			if (_tupleParent != null) {
				_tupleParent.Modified = _isParentModified;
			}

			if (_callback != null) {
				_callback(_tuples, _attribute, NewValues, OldValues, false);
			}
		}

		public string CommandDescription {
			get { return string.Format("[{0}...{1}], change '{2}' with '{3}'", _tuples.First().GetKey<TKey>(), _tuples.Last().GetKey<TKey>(), _attribute.DisplayName, (NewValues[0] ?? "").ToString().Replace("\r\n", "\\r\\n").Replace("\n", "\\n")); }
		}

		public TKey Key { get; private set; }
		public List<TKey> Keys { get; private set; }

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as GroupChangeTupleProperty<TKey, TValue>;
			if (cmd != null) {
				if (Keys.Count == cmd.Keys.Count && _attribute == cmd._attribute) {
					for (int i = 0; i < Keys.Count; i++) {
						// type
						if (Keys[i] is int && cmd.Keys[i] is int) {
							if ((int)(object)Keys[i] != (int)(object)cmd.Keys[i]) {
								return false;
							}
						}
						else if (Keys[i] is string && cmd.Keys[i] is string) {
							if ((string)(object)Keys[i] != (string)(object)cmd.Keys[i]) {
								return false;
							}
						}
						else {
							return false;
						}

						if ((NewValues[i] ?? "").ToString() != (cmd.OldValues[i] ?? "").ToString()) {
							return false;
						}
					}

					return true;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as GroupChangeTupleProperty<TKey, TValue>;
			if (cmd != null) {
				for (int i = 0; i < Keys.Count; i++) {
					NewValues[i] = cmd.NewValues[i];
				}

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}
	}
}
