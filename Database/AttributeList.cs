using System;
using System.Collections;
using System.Collections.Generic;
using Utilities.Extension;

namespace Database {
	public class AttributeList : IEnumerable<DbAttribute> {
		public static string GetQueryName(string attribute) {
			return attribute.ToLower().RemoveBreakLines().Replace(" ", "_").Replace(".", "_").Replace("__", "_");
		}

		private readonly List<DbAttribute> _attributes = new List<DbAttribute>();
		private bool _closed;
		public DbAttribute PrimaryAttribute { get; private set; }
		public List<DbAttribute> Attributes {
			get {
				_closed = true;
				return _attributes;
			}
		}
		public int Degree {
			get { return _attributes.Count; }
		}
		public int Count {
			get { return Degree; }
		}

		public DbAttribute this[int index] {
			get { return Attributes[index]; }
		}

		public void Add(DbAttribute attribute) {
			attribute.Index = _attributes.Count;
			attribute.Parent = this;

			if (_closed)
				throw new Exception("Trying to add an attribute after the list has been closed. Only access the attributes once you have finished adding the attribute to the list.");

			if (attribute.PrimaryKey) {
				if (PrimaryAttribute != null)
					throw new Exception("Primary attribute has already been set.");

				PrimaryAttribute = attribute;
				_attributes.Add(attribute);
			}
			else {
				if (attribute.Index == 0)
					throw new Exception("The first attribute added must be the primary attribute.");

				_attributes.Add(attribute);
			}
		}

		public int Find(object input) {
			if (input is string) {
				string inputS = (string)input;

				inputS = inputS.Replace(" ", "_");

				for (int i = 0; i < Count; i++) {
					if (String.Compare(GetQueryName(_attributes[i].DisplayName), inputS, StringComparison.OrdinalIgnoreCase) == 0) {
						return i;
					}

					if (String.Compare(GetQueryName(_attributes[i].DisplayName), inputS, StringComparison.OrdinalIgnoreCase) == 0) {
						return i;
					}

					if (String.Compare(GetQueryName(_attributes[i].AttributeName), inputS, StringComparison.OrdinalIgnoreCase) == 0) {
						return i;
					}
				}

				int ival;

				if (Int32.TryParse(inputS, out ival)) {
					return ival;
				}
			}
			else if (input is int) {
				return (int)input;
			}
			else {
				string inputS = input.ToString();
				int ival;

				if (Int32.TryParse(inputS, out ival)) {
					return ival;
				}
			}

			DatabaseExceptions.ThrowAttributeNotFound(input, this);
			return -1;
		}

		public IEnumerator<DbAttribute> GetEnumerator() {
			return _attributes.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
