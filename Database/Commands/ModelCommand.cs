using System;
using System.Linq.Expressions;
using System.Reflection;
using Utilities.Commands;

namespace Database.Commands {
	public class ModelCommand<TKey, TValue, TFieldValue> : ITableCommand<TKey, TValue>, IAutoReverse, ICombinableCommand where TValue : Tuple {
		private TValue _tuple;
		public TFieldValue OldValue;
		public TFieldValue NewValue;
		public string ModelKey;
		public string ModelValue;
		private bool IsReversible;
		private bool _isModified;
		private bool _isSet;

		private Func<TFieldValue> _get;
		private Action<TFieldValue> _set;

		public ModelCommand(TValue tuple, object model, string fieldName, TFieldValue newValue, bool isReversible = true) {
			_tuple = tuple;
			_isModified = _tuple.Modified;

			var fi = model.GetType().GetField(fieldName);
			ModelKey = fi.Name;
			ModelValue = _getStringValue(newValue);

			_get = () => (TFieldValue)fi.GetValue(model);
			_set = v => fi.SetValue(model, v);

			OldValue = _get();
			NewValue = newValue;

			Key = tuple.GetKey<TKey>();
			IsReversible = isReversible;
		}

		public ModelCommand(TValue tuple, Func<TFieldValue> getter, Action<TFieldValue> setter, TFieldValue newValue, string fieldName, bool isReversible = true) {
			_tuple = tuple;
			_isModified = _tuple.Modified;

			ModelKey = fieldName;
			ModelValue = _getStringValue(newValue);

			_get = getter;
			_set = setter;

			OldValue = _get();
			NewValue = newValue;

			Key = tuple.GetKey<TKey>();
			IsReversible = isReversible;
		}

		public ModelCommand(TValue tuple, Expression<Func<TFieldValue>> expression, TFieldValue newValue, bool isReversible = true) {
			_tuple = tuple;
			_isModified = _tuple.Modified;

			var body = (MemberExpression)expression.Body;
			var pi = (PropertyInfo)body.Member;
			ModelKey = pi.Name;
			ModelValue = _getStringValue(newValue);

			_get = expression.Compile();
			_set = v => pi.SetValue(null, v, null);

			OldValue = _get();
			NewValue = newValue;

			Key = tuple.GetKey<TKey>();
			IsReversible = isReversible;
		}

		public string CommandDescription => $"[{Key}], change '{ModelKey}' with '{ModelValue}'";

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			if (!_isSet) {
				_isModified = _tuple.Modified;
				_isSet = true;
			}

			_set(NewValue);
			_tuple.Modified = true;
		}

		public void Undo(Table<TKey, TValue> table) {
			_set(OldValue);
			_tuple.Modified = _isModified;
		}

		public bool CanCombine(ICombinableCommand command) {
			if (!IsReversible) return false;

			if (command is ModelCommand<TKey, TValue, TFieldValue> cmd) {
				if (Key.ToString() == cmd.Key.ToString() && ModelKey == cmd.ModelKey) {
					return IsEqual(NewValue, cmd.OldValue);
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ModelCommand<TKey, TValue, TFieldValue>;
			if (cmd != null) {
				NewValue = cmd.NewValue;

				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			if (!IsReversible) return false;
			return IsEqual(NewValue, OldValue);
		}

		public bool IsEqual(TFieldValue value1, TFieldValue value2) {
			return _getStringValue(value1) == _getStringValue(value2);
		}

		private string _getStringValue(TFieldValue value) {
			return value == null ? "" : value.ToString();
		}
	}
}
