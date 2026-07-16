using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Database.Commands {
	public enum ListCommandMode {
		Add,
		Remove,
		Insert,
		ChangeList,
	}

	public class ModelListCommand<TKey, TValue, TFieldValue> : ITableCommand<TKey, TValue> where TValue : Tuple {
		private TValue _tuple;
		public List<TFieldValue> Copy;
		public List<TFieldValue> OldValue;
		public List<TFieldValue> NewValue;
		public string ModelKey;
		public string ModelValue;
		private bool _isModified;
		private ListCommandMode _mode;
		private int _index;
		private bool _isSet;

		private Func<List<TFieldValue>> _get;

		public ModelListCommand(TValue tuple, Expression<Func<List<TFieldValue>>> expression, List<TFieldValue> newValue, ListCommandMode mode, int index = -1) {
			_tuple = tuple;
			_isModified = _tuple.Modified;
			_mode = mode;
			_index = index;

			var body = (MemberExpression)expression.Body;
			var pi = body.Member;
			ModelKey = pi.Name;
			ModelValue = newValue.Count.ToString();

			_get = expression.Compile();

			OldValue = _get();
			NewValue = newValue;

			Key = tuple.GetKey<TKey>();
		}

		public string CommandDescription {
			get {
				switch (_mode) {
					default:
					case ListCommandMode.Add:
						return $"[{Key}], added '{ModelKey}' ({ModelValue})";
					case ListCommandMode.Remove:
						return $"[{Key}], removed '{ModelKey}' ({ModelValue})";
					case ListCommandMode.Insert:
						return $"[{Key}], insert '{ModelKey}' ({ModelValue})";
					case ListCommandMode.ChangeList:
						return $"[{Key}], modified '{ModelKey}' ({ModelValue})";
				}
			}
		}

		public TKey Key { get; private set; }

		public void Execute(Table<TKey, TValue> table) {
			if (!_isSet) {
				_isModified = _tuple.Modified;
				_isSet = true;
			}

			_tuple.Modified = true;

			switch (_mode) {
				case ListCommandMode.Add:
					OldValue.AddRange(NewValue);
					break;
				case ListCommandMode.Remove:
					foreach (var v in NewValue)
						OldValue.Remove(v);
					break;
				case ListCommandMode.Insert:
					OldValue.InsertRange(_index, NewValue);
					break;
				case ListCommandMode.ChangeList:
					if (Copy == null)
						Copy = OldValue.ToList();

					OldValue.Clear();
					OldValue.AddRange(NewValue);
					break;
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			_tuple.Modified = _isModified;

			switch (_mode) {
				case ListCommandMode.Add:
					foreach (var v in NewValue)
						OldValue.Remove(v);
					break;
				case ListCommandMode.Remove:
					OldValue.AddRange(NewValue);
					break;
				case ListCommandMode.Insert:
					OldValue.RemoveRange(_index, NewValue.Count);
					break;
				case ListCommandMode.ChangeList:
					OldValue.Clear();
					OldValue.AddRange(Copy);
					break;
			}
		}
	}
}
