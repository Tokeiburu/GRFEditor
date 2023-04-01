using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Database.Commands;
using Utilities;

namespace Database {
	public class Table<T1, T2> : BaseTable, IEnumerable<T2> where T2 : Tuple {
		public virtual CommandsHolder<T1, T2> Commands { get; private set; }

		protected readonly AttributeList _list;
		private readonly Dictionary<T1, T2> _tuples = new Dictionary<T1, T2>();
		internal TkDictionary<int, int> AutoIncrements = new TkDictionary<int, int>();
		public bool EnableRawEvents { get; set; }

		public Dictionary<T1, T2> Tuples {
			get { return _tuples; }
		}

		public int Cardinality {
			get { return _tuples.Count; }
		}
		public int Count {
			get { return Cardinality; }
		}

		public event TableEventHandler TupleRemoved;
		public event TableEventHandler TupleAdded;
		public event TableEventHandler TupleRawAdded;
		public event TableEventHandler TupleModified;
		public event UpdateTableEventHandler TableUpdated;

		public delegate void TableEventHandler(object sender, T1 key, T2 value);
		public delegate void UpdateTableEventHandler(object sender);

		public void OnTableUpdated() {
			UpdateTableEventHandler handler = TableUpdated;
			if (handler != null) handler(this);
		}

		public virtual void OnTupleRawAdded(T1 key, T2 value) {
			TableEventHandler handler = TupleRawAdded;
			if (handler != null) handler(this, key, value);
		}

		public virtual void OnTupleModified(T1 key, T2 value) {
			if (!EnableEvents) return;
			TableEventHandler handler = TupleModified;
			if (handler != null) handler(this, key, value);
		}

		public virtual void OnTupleAdded(T1 key, T2 value) {
			if (!EnableEvents) return;
			TableEventHandler handler = TupleAdded;
			if (handler != null) handler(this, key, value);
		}

		public virtual void OnTupleRemoved(T1 key, T2 value) {
			if (!EnableEvents) return;
			TableEventHandler handler = TupleRemoved;
			if (handler != null) handler(this, key, value);
		}

		public Table(AttributeList list, bool unsafeContext = false) {
			if (!unsafeContext) {
				if (typeof (T1) != list.PrimaryAttribute.DataType)
					throw new Exception("The primary attribute type doesn't match the database primary key");
			}

			Commands = new CommandsHolder<T1, T2>(this);
			_list = list;
		}

		public AttributeList AttributeList {
			get { return _list; }
		}

		public object this[T1 key, object input] {
			get {
				DatabaseExceptions.ThrowIfTraceNotEnabled();
				int attribute = _list.Find(input);
				return Get(key, _list.Attributes[attribute]);
			}
			set {
				DatabaseExceptions.ThrowIfTraceNotEnabled();
				int attribute = _list.Find(input);

				if (!ContainsKey(key)) {
					T2 element = Compiled.New2<T2>.Instance();
					element.Init(key, _list);
					element.Added = true;

					Commands.AddTuple(key, element);
				}

				if (attribute == 0) {
					if (!(value is T1))
						DatabaseExceptions.ThrowKeyConstraint<T1>(value);

					T1 newKey = (T1)value;

					if (newKey.ToString() == key.ToString())
						return;

					Commands.ChangeKey(key, newKey);
					
					return;
				}

				Commands.Set(_tuples[key], attribute, value);
			}
		}

		public T2 this[T1 key] {
			get {
				DatabaseExceptions.ThrowIfTraceNotEnabled();
				return GetTuple(key);
			}
			set {
				DatabaseExceptions.ThrowIfTraceNotEnabled();

				if (value == null) {
					this.Commands.Delete(key);
					return;
				}

				T2 element = Compiled.New2<T2>.Instance();
				element.Init(key, _list);
				element.Added = true;
				element.Copy(value);
				element.SetRawValue(0, key);

				Commands.AddTuple(key, element);
			}
		}

		public virtual List<T2> FastItems {
			get {
				return _tuples.Select(p => p.Value).ToList();
			}
		}

		public virtual object Get(T1 key, DbAttribute attribute) {
			return _tuples[key].GetValue(attribute.Index);
		}

		public virtual void Set(T1 key, DbAttribute attribute, object value) {
			_tuples[key].SetValue(attribute, value);
		}

		public virtual T Get<T>(T1 key, DbAttribute attribute) where T : class {
			return _tuples[key].GetValue(attribute.Index) as T;
		}

		public virtual object GetRaw(T1 key, DbAttribute attribute) {
			return _tuples[key].GetRawValue(attribute.Index);
		}

		public virtual void SetRaw(T1 key, DbAttribute attribute, object value, bool forceSet = false) {
			// Always add inexistingn tuples automatically
			if (!_tuples.ContainsKey(key)) {
				T2 element = Compiled.New2<T2>.Instance();
				element.Init(key, _list);
				_tuples.Add(key, element);

				if (EnableRawEvents) {
					T2 tuple = _tuples[key];
					tuple.Added = true;
					Commands.StoreAndExecute(new AddTuple<T1, T2>(key, tuple, null) {IgnoreConflict = true});
				}
			}

			if (EnableRawEvents) {
				Commands.StoreAndExecute(new ChangeTupleProperties<T1, T2>(_tuples[key], attribute, value));
			}
			else {
				_tuples[key].SetRawValue(attribute, value);
			}
		}

		public virtual void SetRawRange(T1 key, int attributeOffset, int indexOffset, List<DbAttribute> attributes, string[] values) {
			object[] valuesObject = new object[values.Length];

			for (int i = indexOffset; i < values.Length; i++)
				valuesObject[i] = values[i];

			SetRawRange(key, attributeOffset, indexOffset, attributes, valuesObject);
		}

		public virtual void SetRawRange(T1 key, int attributeOffset, int indexOffset, List<DbAttribute> attributes, object[] values) {
			if (values.Length == 1) {
				SetRaw(key, attributes[attributeOffset], values[0]);
				return;
			}

			if (!_tuples.ContainsKey(key)) {
				T2 element = Compiled.New2<T2>.Instance();
				element.Init(key, _list);
				_tuples.Add(key, element);

				if (EnableRawEvents) {
					T2 tuple = _tuples[key];
					tuple.Added = true;

					for (int i = indexOffset; i < values.Length; i++) {
						tuple.SetRawValue(attributes[i + attributeOffset], values[i]);
					}

					Commands.StoreAndExecute(new AddTuple<T1, T2>(key, tuple, null) {IgnoreConflict = true});
					return;
				}
			}

			if (EnableRawEvents) {
				Commands.StoreAndExecute(new ChangeTuplePropertyRange<T1, T2>(key, _tuples[key], attributeOffset, indexOffset, attributes, values));
			}
			else {
				var tuple = _tuples[key];

				for (int i = indexOffset; i < values.Length; i++) {
					tuple.SetRawValue(attributes[i + attributeOffset], values[i]);
				}
			}
		}

		public virtual void Clear() {
			_tuples.Clear();
		}

		public virtual bool ContainsKey(T1 key) {
			return _tuples.ContainsKey(key);
		}
		public virtual void ChangeKey(T1 oldKey, T1 newKey) {
			if (_tuples.ContainsKey(oldKey)) {
				T2 temp = _tuples[oldKey];
				_tuples.Remove(oldKey);
				_tuples[newKey] = temp;
			}
		}
		public virtual void Remove(T1 key) {
			if (_tuples.ContainsKey(key)) {
				T2 tuple = _tuples[key];
				_tuples.Remove(key);
				OnTupleRemoved(key, tuple);
			}
		}
		public virtual void Add(T1 key, T2 item) {
			_tuples[key] = item;
			OnTupleAdded(key, item);
		}

		public virtual IEnumerable<T2> GetSortedItems() {
			return _tuples.OrderBy(p => p.Key).Select(p => p.Value);
		}

		public virtual T2 Copy(T1 elementFromId, T1 elementToId) {
			T2 elementFrom = _tuples[elementFromId];

			T2 elementTo = Utilities.Compiled.New2<T2>.Instance();
			elementTo.Init(elementToId, _list);

			//T2 elementTo = (T2) Activator.CreateInstance(typeof (T2), elementToId, _list);

			foreach (DbAttribute attribute in _list.Attributes) {
				if (!typeof(IBinding).IsAssignableFrom(attribute.DataType))
					elementTo.SetRawValue(attribute, elementFrom.GetRawCopyValue(attribute.Index));
			}

			elementTo.SetRawValue(_list.PrimaryAttribute, elementToId);
			Remove(elementToId);
			Add(elementToId, elementTo);
			return elementTo;
		}

		public virtual T2 Copy(T1 elementFromId) {
			T2 elementFrom = _tuples[elementFromId];

			T2 elementTo = Utilities.Compiled.New2<T2>.Instance();
			elementTo.Init(elementFromId, _list);

			//T2 elementTo = (T2)Activator.CreateInstance(typeof(T2), elementFromId, _list);

			foreach (DbAttribute attribute in _list.Attributes) {
				if (!typeof(IBinding).IsAssignableFrom(attribute.DataType))
					elementTo.SetRawValue(attribute, elementFrom.GetRawCopyValue(attribute.Index));
			}

			elementTo.SetRawValue(_list.PrimaryAttribute, elementFromId);
			return elementTo;
		}

		public virtual T2 Copy(Table<T1, T2> source, T1 elementFromId, T1 elementToId) {
			T2 elementFrom = source._tuples[elementFromId];

			T2 elementTo = Utilities.Compiled.New2<T2>.Instance();
			elementTo.Init(elementToId, _list);

			foreach (DbAttribute attribute in _list.Attributes) {
				if (!typeof(IBinding).IsAssignableFrom(attribute.DataType))
					elementTo.SetRawValue(attribute, elementFrom.GetRawCopyValue(attribute.Index));
			}

			elementTo.SetRawValue(_list.PrimaryAttribute, elementToId);
			Remove(elementToId);
			Add(elementToId, elementTo);
			return elementTo;
		}

		public virtual T2 GetTuple(T1 key) {
			return _tuples[key];
		}

		public virtual T2 TryGetTuple(T1 key) {
			if (_tuples.ContainsKey(key))
				return _tuples[key];

			return null;
		}

		public override void ClearTupleStates() {
			foreach (T2 tuple in _tuples.Values.Where(p => !p.Normal)) {
				tuple.Modified = false;
				tuple.Added = false;
			}
		}

		internal override bool Contains(Tuple tuple) {
			T2 t = tuple as T2;
			if (t == null) return false;
			return _tuples.ContainsValue(t);
		}

		internal override void CommandSet(Tuple tuple, DbAttribute attribute, object value) {
			this[tuple.GetKey<T1>(), attribute.Index] = value;
		}

		public IEnumerator<T2> GetEnumerator() {
			return _tuples.Select(p => p.Value).OrderBy(p => p).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public void Add(T1 key) {
			DatabaseExceptions.ThrowIfTraceNotEnabled();
			this[key, 0] = key;
		}

		public void Delete(T1 key) {
			DatabaseExceptions.ThrowIfTraceNotEnabled();
			Commands.Delete(key);
		}
	}
}
