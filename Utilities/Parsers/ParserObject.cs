using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities.Parsers {
	public enum ParserMode {
		Read,
		Write
	}

	public class ParserObject : IEnumerable<ParserObject> {
		public ParserTypes ParserType { get; private set; }
		public ParserObject Parent { get; set; }
		public List<string> Lines;
		public int Line { get; private set; }
		public int Length { get; set; }
		public int Indent { get; set; }
		public int ChildrenIndent { get; set; }

		public bool Added { get; set; }
		public bool Modified { get; set; }

		public string ObjectValue {
			get {
				if (this is ParserString confString)
					return confString.Value;

				if (this is ParserAggregate confAggregate) {
					StringBuilder builder = new StringBuilder();
					builder.Append("[");

					for (int i = 0; i < confAggregate.Objects.Count; i++) {
						builder.Append(confAggregate.Objects[i]);

						if (i != confAggregate.Objects.Count - 1)
							builder.Append(", ");
					}

					builder.Append("]");
					return builder.ToString();
				}

				if (this is ParserKeyValue confKeyValue)
					return confKeyValue.Value;

				return null;
			}
		}

		protected ParserObject(ParserTypes confType, int line) {
			ParserType = confType;
			Line = line;
			//Length = 1;
		}

		public ParserObject this[string key] {
			get {
				if (key.Contains(".")) {
					string[] keys = key.Split(new char[] { '.' }, 2);

					var obj = this[keys[0]];

					if (obj == null)
						return null;

					return obj[keys[1]];
				}

				if (this is ParserKeyValue keyValue && keyValue.Key == key)
					return keyValue.Value;

				if (this is ParserArrayBase arrayBase && (keyValue = arrayBase.Objects.OfType<ParserKeyValue>().FirstOrDefault(p => p.Key == key)) != null) {
					return keyValue.Value;
				}

				return null;
			}
			set {
				if (this is ParserKeyValue keyValue && keyValue.Key == key) {
					keyValue.Value = value;
				}
				else if (this is ParserArrayBase arrayBase && (keyValue = arrayBase.Objects.OfType<ParserKeyValue>().FirstOrDefault(p => p.Key == key)) != null) {
					keyValue.Value = value;
				}
				else {
					return;
				}

				this.Modified = true;
			}
		}

		public T To<T>() where T : class {
			return this as T;
		}

		public IEnumerator<ParserObject> GetEnumerator() {
			if (this is ParserArrayBase arrayBase)
				return arrayBase.Objects.GetEnumerator();

			return new List<ParserObject>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public static implicit operator string(ParserObject item) {
			return item.ObjectValue;
		}

		public bool IsModified() {
			if (Modified)
				return true;

			foreach (ParserObject child in this) {
				if (child.Modified)
					return true;
			}

			return false;
		}
	}

	public class ParserString : ParserObject {
		public string Value { get; set; }

		public ParserString(string value, int line)
			: base(ParserTypes.String, line) {
			Value = value;
		}

		public static implicit operator ParserString(string value) {
			return new ParserString(value, 0);
		}

		public override string ToString() {
			return "String: " + Value;
		}
	}

	public class ParserArrayBase : ParserObject {
		public List<ParserObject> Objects = new List<ParserObject>();

		protected ParserArrayBase(ParserTypes confType, int line) : base(confType, line) {
		}

		public void AddElement(ParserObject obj) {
			Objects.Add(obj);
		}
	}

	public class ParserArray : ParserArrayBase {
		public ParserArray(int line)
			: base(ParserTypes.Array, line) {
		}

		public override string ToString() {
			return "Array: " + Objects.Count + " elements.";
		}
	}

	public class ParserList : ParserArrayBase {
		public ParserList(int line)
			: base(ParserTypes.List, line) {
		}

		public override string ToString() {
			return "List: " + Objects.Count + " elements.";
		}
	}

	public class ParserAggregate : ParserArrayBase {
		public ParserAggregate(int line)
			: base(ParserTypes.Aggregate, line) {
		}

		public override string ToString() {
			return "Aggregate: " + Objects.Count + " elements.";
		}
	}

	public class ParserKeyValue : ParserObject {
		public string Key { get; private set; }
		public ParserObject Value { get; set; }

		public ParserKeyValue(string key, int line)
			: base(ParserTypes.KeyValue, line) {
			Key = key;
		}

		public override string ToString() {
			return "Key: " + Key + ", Value: { " + Value + " }";
		}
	}

	public enum ParserTypes {
		List,
		KeyValue,
		String,
		Array,
		Number,
		Aggregate,
		Null
	}
}