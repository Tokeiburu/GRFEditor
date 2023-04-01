using System.Collections.Generic;
using Utilities.Commands;

namespace Database.Commands {
	public class ChangeTupleDicoProperty2<TKey, TValue> : ITableCommand<TKey, TValue>, IAutoReverse, ICombinableCommand
		where TValue : Tuple {
		private readonly TValue _tupleParent;
		private readonly TKey _dkey;
		private readonly bool _isModified;
		private readonly Dictionary<TKey, TValue> _dico;
		private readonly ChangeTupleProperty<TKey, TValue> _propChanged;
		private bool _reversable = true;

		public bool SubModified { get; set; }

		public TKey DicoKey {
			get { return _dkey; }
		}

		public bool Reversable {
			get { return _reversable; }
			set { _reversable = value; }
		}

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, TKey dkey, TValue dvalue, TKey newDkey, bool executed);

		public ChangeTupleDicoProperty2(TValue tupleParent, DbAttribute attributeTable, TValue tuple, DbAttribute attribute, object value) {
			_tupleParent = tupleParent;
			Key = tupleParent.GetKey<TKey>();
			Attribute = attribute;
			_dico = (Dictionary<TKey, TValue>)tupleParent.GetRawValue(attributeTable.Index);
			_isModified = tupleParent.Modified;
			_dkey = tuple.GetKey<TKey>();
			_propChanged = new ChangeTupleProperty<TKey, TValue>(tuple, attribute, value);
		}

		public DbAttribute Attribute { get; set; }

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			_propChanged.Execute(_dico);
			_tupleParent.Modified = true;
		}

		public void Undo(Table<TKey, TValue> table) {
			_propChanged.Undo(null);
			_tupleParent.Modified = _isModified;
		}

		public string CommandDescription {
			get {
				return string.Format("[{0}], changed '{1}' with '{2}'", Key, Attribute.DisplayName, _propChanged.NewValue ?? "");
			}
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as ChangeTupleDicoProperty2<TKey, TValue>;
			if (cmd != null) {
				if (Key.ToString() == cmd.Key.ToString() && Attribute == cmd.Attribute && DicoKey.ToString() == cmd.DicoKey.ToString()) {
					return _propChanged.CanCombine(cmd._propChanged);
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ChangeTupleDicoProperty2<TKey, TValue>;
			if (cmd != null) {
				_propChanged.Combine(cmd._propChanged, abstractCommand);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			var cmd = command as ChangeTupleDicoProperty2<TKey, TValue>;
			if (cmd != null) {
				if (Key.ToString() == cmd.Key.ToString() && Attribute == cmd.Attribute && DicoKey.ToString() == cmd.DicoKey.ToString()) {
					return _propChanged.CanDelete(cmd._propChanged);
				}
			}

			return false;
		}
	}
}
