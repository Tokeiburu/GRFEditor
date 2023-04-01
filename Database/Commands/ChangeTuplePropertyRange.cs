using System.Collections.Generic;

namespace Database.Commands {
	public class ChangeTuplePropertyRange<TKey, TValue> : ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private TValue _tuple;
		private readonly int _attributeOffset;
		private readonly int _indexOffset;
		private bool _isModified;
		public object[] NewValues { get; set; }
		public object[] OldValues { get; private set; }
		public TValue Tuple { get { return _tuple; } }
		private bool _reversable = true;
		private bool _isSet;
		private readonly List<DbAttribute> _attributes;

		public bool Reversable {
			get { return _reversable; }
			set { _reversable = value; }
		}

		public delegate void ChangeTupleCallback(TValue value, DbAttribute attribute, object oldValue, object newValue, bool executed);

		internal ChangeTuplePropertyRange(TKey key, TValue tuple, int attributeOffset, int indexOffset, List<DbAttribute> attributes, object[] newValues) {
			_attributes = attributes;
			_tuple = tuple;
			_attributeOffset = attributeOffset;
			_indexOffset = indexOffset;
			_attributes = attributes;
			NewValues = newValues;
			OldValues = new object[newValues.Length];

			for (int i = indexOffset; i < newValues.Length; i++) {
				OldValues[i] = tuple.GetRawValue(_attributes[i + attributeOffset].Index);
			}

			Key = key;
		}

		public void Execute(Table<TKey, TValue> table) {
			_tuple = table.GetTuple(Key);

			if (!_isSet) {
				_isModified = _tuple.Modified;
				_isSet = true;
			}

			for (int i = _indexOffset; i < NewValues.Length; i++) {
				_tuple.SetRawValue(_attributes[i + _attributeOffset].Index, NewValues[i]);
			}

			_tuple.Modified = true;
		}

		public void Undo(Table<TKey, TValue> table) {
			if (_tuple == null) return;

			for (int i = _indexOffset; i < NewValues.Length; i++) {
				_tuple.SetRawValue(_attributes[i + _attributeOffset].Index, OldValues[i]);
			}

			_tuple.Modified = _isModified;
		}

		public string CommandDescription {
			get { return string.Format("[{0}], updated entry", _tuple.GetKey<TKey>()); }
		}

		public TKey Key { get; private set; }
	}
}
