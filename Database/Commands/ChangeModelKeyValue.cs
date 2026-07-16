using System.Collections.Generic;
using Utilities.Commands;

namespace Database.Commands {
	public class ChangeModelKeyValue<TKey, TValue> : ITableCommand<TKey, TValue>, IAutoReverse, ICombinableCommand
		where TValue : Tuple {
		private TValue _tuple;
		private readonly DbAttribute _attribute;
		private readonly ChangeTupleCallback _callback;
		private bool _isModified;
		public object NewValue { get; set; }
		public object UpdatedNewValue { get; set; }
		public object OldValue { get; private set; }
		public string ModelKey { get; private set; }
		public string ModelValue { get; private set; }

		public TValue Tuple { get { return _tuple; } }
		private bool _reversable = true;

		public bool Reversable {
			get { return _reversable; }
			set { _reversable = value; }
		}

		public DbAttribute Attribute {
			get { return _attribute; }
		}

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, object oldValue, object newValue, bool executed);

		public ChangeModelKeyValue(TValue tuple, DbAttribute attribute, object model, string key, string value, ChangeTupleCallback callback = null) {
			_tuple = tuple;
			_attribute = attribute;
			NewValue = model;
			OldValue = attribute.DataConverter.ConvertTo(null, _tuple.GetValue(_attribute.Index));
			ModelKey = key;
			ModelValue = value;
			_callback = callback;
			_isModified = _tuple.Modified;
			Key = tuple.GetKey<TKey>();
		}

		public void Execute(Table<TKey, TValue> table) {
			_tuple.SetValue(_attribute, NewValue);
			UpdatedNewValue = _tuple.GetValue(_attribute);

			_callback?.Invoke(_tuple, _attribute, UpdatedNewValue, OldValue, true);
		}

		public void Undo(Table<TKey, TValue> table) {
			if (_tuple == null) return;

			_tuple.SetValue(_attribute, OldValue);
			_tuple.Modified = _isModified;

			_callback?.Invoke(_tuple, _attribute, NewValue, OldValue, false);
		}

		public string CommandDescription {
			get { return string.Format("[{0}], change '{1}' with '{2}'", _tuple.GetKey<TKey>(), ModelKey, ModelValue.ToString().Replace("\r\n", "\\r\\n").Replace("\n", "\\n")); }
		}

		public TKey Key { get; private set; }

		public bool CanCombine(ICombinableCommand command) {
			if (!Reversable) return false;

			var cmd = command as ChangeModelKeyValue<TKey, TValue>;
			if (cmd != null) {
				if (Key.ToString() == cmd.Key.ToString() && _attribute == cmd._attribute && ModelKey == cmd.ModelKey) {
					return true;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ChangeModelKeyValue<TKey, TValue>;
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
