using System.Collections.Generic;
using Utilities;
using Utilities.Commands;

namespace Database.Commands {
	public class ChangeTupleProperty<TKey, TValue> : ITableCommand<TKey, TValue>, IAutoReverse, ICombinableCommand
		where TValue : Tuple {
		private TValue _tuple;
		private readonly DbAttribute _attribute;
		private readonly ChangeTupleCallback _callback;
		private bool _isModified;
		public object NewValue { get; set; }
		public object UpdatedNewValue { get; set; }
		public object OldValue { get; private set; }
		public TValue Tuple { get { return _tuple; } }
		private bool _reversable = true;
		private bool _useKey;
		private bool _isSet;

		public bool Reversable {
			get { return _reversable; }
			set { _reversable = value; }
		}

		public DbAttribute Attribute {
			get { return _attribute; }
		}

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, object oldValue, object newValue, bool executed);

		public ChangeTupleProperty(TValue tuple, DbAttribute attribute, object newValue, ChangeTupleCallback callback = null) {
			_tuple = tuple;
			_attribute = attribute;
			NewValue = newValue;
			OldValue = attribute.DataConverter.ConvertTo(null, _tuple.GetValue(_attribute.Index));
			_callback = callback;
			_isModified = _tuple.Modified;
			Key = tuple.GetKey<TKey>();
		}

		public ChangeTupleProperty(TValue tuple, int attributeId, object newValue, ChangeTupleCallback callback = null) : this(tuple, tuple.Attributes.Attributes[attributeId], newValue, callback) {
		}

		internal ChangeTupleProperty(TKey key, TValue tuple, DbAttribute attribute, object newValue, ChangeTupleCallback callback = null) {
			_tuple = null;
			_useKey = true;
			_attribute = attribute;
			NewValue = newValue;
			OldValue = attribute.DataConverter.ConvertTo(null, tuple.GetValue(_attribute.Index));
			_callback = callback;
			
			Key = key;
		}

		public void Execute(Table<TKey, TValue> table) {
			if (_useKey) {
				_tuple = table.GetTuple(Key);

				if (!_isSet) {
					_isModified = _tuple.Modified;
					_isSet = true;
				}
			}

			_tuple.SetValue(_attribute, NewValue);
			UpdatedNewValue = _tuple.GetValue(_attribute);

			if (_callback != null) {
				_callback(_tuple, _attribute, UpdatedNewValue, OldValue, true);
			}
		}

		internal void Execute(Dictionary<TKey, TValue> table) {
			if (_useKey) {
				_tuple = table[Key];

				if (!_isSet) {
					_isModified = _tuple.Modified;
					_isSet = true;
				}
			}

			_tuple.SetValue(_attribute, NewValue);
			UpdatedNewValue = _tuple.GetValue(_attribute);

			if (_callback != null) {
				_callback(_tuple, _attribute, UpdatedNewValue, OldValue, true);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			if (_tuple == null) return;

			_tuple.SetValue(_attribute, OldValue);
			_tuple.Modified = _isModified;

			if (_callback != null) {
				_callback(_tuple, _attribute, NewValue, OldValue, false);
			}
		}

		public string CommandDescription {
			get { return string.Format("[{0}], change '{1}' with '{2}'", _tuple.GetKey<TKey>(), _attribute.DisplayName, (NewValue ?? "").ToString().Replace("\r\n", "\\r\\n").Replace("\n", "\\n")); }
		}

		public TKey Key { get; private set; }

		public bool CanCombine(ICombinableCommand command) {
			if (!Reversable) return false;

			var cmd = command as ChangeTupleProperty<TKey, TValue>;
			if (cmd != null) {
				if (Key.ToString() == cmd.Key.ToString() && _attribute == cmd._attribute) {
					if ((NewValue ?? "").ToString() == (cmd.OldValue ?? "").ToString() || 
						(UpdatedNewValue ?? "").ToString() == (cmd.OldValue ?? "").ToString())
						return true;

					return false;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ChangeTupleProperty<TKey, TValue>;
			if (cmd != null) {
				NewValue = cmd.NewValue;
				UpdatedNewValue = cmd.UpdatedNewValue;

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			if (!Reversable) return false;
			return 
				(NewValue ?? "").ToString() == (OldValue ?? "").ToString() ||
				(UpdatedNewValue ?? "").ToString() == (OldValue ?? "").ToString();
		}
	}
}
