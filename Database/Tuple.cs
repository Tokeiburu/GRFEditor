using System;
using System.Collections.Generic;
using System.Linq;

namespace Database {
	public class Tuple : IComparable {
		protected object[] _elements;
		private bool _modified;

		public bool Added { get; set; }
		public bool Deleted { get; set; }
		public bool Normal { get { return !(Added || Modified || Deleted); } }

		//public bool IsRedo { get; internal set; }
		//public bool IsUndo { get; internal set; }

		public AttributeList Attributes { get; private set; }

		public delegate void TupleEventHandler(object sender, bool value);

		public event TupleEventHandler TupleModified;

		public virtual void OnTupleModified(bool value) {
			TupleEventHandler handler = TupleModified;
			if (handler != null) handler(this, value);
		}

		public static Type BindingType = typeof (IBinding);

		public Tuple() {
		}

		public void Init(object key, AttributeList list) {
			Attributes = list;
			_elements = new object[list.Attributes.Count];

			_elements[0] = key;

			for (int index = 1; index < list.Attributes.Count; index++) {
				// This is not a real attribute
				if (BindingType.IsAssignableFrom(list.Attributes[index].DataType)) {
					IBinding binding = (IBinding)Activator.CreateInstance(list.Attributes[index].DataType, new object[] { });
					binding.Tuple = this;
					binding.AttachedAttribute = list.Attributes[index];
					_elements[index] = binding;
				}
				else {
					_elements[index] = list.Attributes[index].Default;
				}
			}
		}

		public Tuple(object key, AttributeList list) {
			Init(key, list);
		}

		public virtual bool Default {
			get {
				return true;
			}
		}

		public bool Modified {
			get { return _modified; }
			set {
				_modified = value;
				OnTupleModified(value);
			}
		}

		public virtual T GetKey<T>() {
			return (T) _elements[0];
		}

		public virtual object this[int index] {
			get {
				DbAttribute attribute = Attributes.Attributes[index];

				if (attribute.DataType == typeof(int)) {
					return attribute.DataConverter.ConvertFrom<int>(this, GetValue(index));
				}

				if (attribute.DataType == typeof(string)) {
					return attribute.DataConverter.ConvertFrom<string>(this, GetValue(index));
				}

				return attribute.Accessor.Get(this, GetValue(index));
			}
			set {
				DatabaseExceptions.ThrowIfTraceNotEnabled();
				DbAttribute attribute = Attributes.Attributes[index];

				foreach (var table in TableHelper.Tables) {
					if (table.Contains(this)) {
						table.CommandSet(this, attribute, value);
						break;
					}
				}
			}
		}

		public virtual object this[string index] {
			get {
				DatabaseExceptions.ThrowIfTraceNotEnabled();
				var attIndex = Attributes.Find(index);
				return this[attIndex];
			}
			set {
				DatabaseExceptions.ThrowIfTraceNotEnabled();
				var attIndex = Attributes.Find(index);
				this[attIndex] = value;
			}
		}

		public virtual T GetValue<T>(int index) {
			return Attributes.Attributes[index].DataConverter.ConvertFrom<T>(this, GetValue(index));
		}
		public virtual T GetValue<T>(DbAttribute attribute) {
			return attribute.DataConverter.ConvertFrom<T>(this, GetValue(attribute.Index));
		}
		public virtual object GetValue(int index) {
			return _elements[index];
		}
		public virtual object GetValue(DbAttribute attribute) {
			return GetValue(attribute.Index);
		}
		public virtual void SetValue(DbAttribute attribute, object value) {
			_elements[attribute.Index] = attribute.DataConverter.ConvertTo(this, value);
		}
		public object GetRawCopyValue(int index) {
			return Attributes.Attributes[index].DataCopy.CopyFrom(_elements[index]);
		}
		public object GetRawValue(int index) {
			return _elements[index];
		}
		public T GetRawValue<T>(int index) {
			return (T) _elements[index];
		}
		public T GetRawValue<T>(DbAttribute attribute) {
			return (T)_elements[attribute.Index];
		}
		public void SetRawValue(DbAttribute attribute, object value) {
			_elements[attribute.Index] = value;
		}
		public void SetElements(object[] elements) {
			_elements = elements;
		}
		public void SetRawValue(int index, object value) {
			_elements[index] = value;
		}

		public object DataImage {
			get {
				if (GetImageData != null) {
					return GetImageData();
				}

				return null;
			}
		}

		public List<object> GetRawElements() {
			return _elements.ToList();
		}

		public bool CompareWith(Tuple tuple) {
			if (this._elements.Length != tuple._elements.Length)
				return false;

			if (this.Attributes.Attributes.Count != tuple.Attributes.Attributes.Count)
				return false;

			for (int i = 0; i < _elements.Length && i < tuple.Attributes.Attributes.Count; i++) {
				DbAttribute attribute = tuple.Attributes.Attributes[i];

				if (String.CompareOrdinal(GetValue<string>(attribute), tuple.GetValue<string>(attribute)) != 0)
					return false;
			}

			return true;
		}

		public Func<object> GetImageData { get; set; }

		internal int GetHash() {
			return String.Join(",", _elements.Select(p => (p ?? "").ToString()).ToArray()).GetHashCode();
		}

		public int CompareTo(object obj) {
			if (obj == null) return 1;

			Tuple tuple = obj as Tuple;
			if (tuple == null)
				throw new ArgumentException("Object is not a Tuple");

			var o = tuple.GetValue(0);

			if (o is int)
				return ((int) this.GetValue(0)).CompareTo(o);
			return ((string)this.GetValue(0)).CompareTo(o);
		}

		public void Copy(Tuple tuple) {
			_elements = new object[tuple._elements.Length];

			for (int i = 0; i < tuple._elements.Length; i++) {
				_elements[i] = tuple._elements[i];
			}
		}
	}
}
