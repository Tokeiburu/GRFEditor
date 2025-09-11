using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GRF.GrfSystem;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Types {
	public enum LubSourceType {
		Global,
		Constant
	}

	public interface ILubObject {
		void Print(StringBuilder builder, int level);
		int GetLength();
	}

	public class LubDictionary : ILubObject {
		private readonly Dictionary<string, LubKeyValue> _dico;
		private readonly List<ILubObject> _items;

		public LubDictionary(int array, int hash) {
			Array = array;
			Hash = hash;

			_items = new List<ILubObject>(array);
			_dico = new Dictionary<string, LubKeyValue>(array);
		}

		// We keep a reference on the items to speed up the dictionaries access

		public int Array { get; private set; }
		public int Hash { get; private set; }
		public bool IsAssigned { get; private set; }

		public int Count {
			get { return _items.Count + _dico.Count; }
		}

		protected int? Length { get; set; }

		#region ILubObject Members
		public void Print(StringBuilder builder, int level) {
			IsAssigned = true;

			if (builder.Length > 0 && builder[builder.Length - 1] == '\n') {
				builder.AppendIndent(level);
			}

			if (Count == 0) {
				builder.Append("{}");
				return;
			}

			List<ILubObject> items = _items.Concat(_dico.Values.Cast<ILubObject>()).ToList();

			bool sameLine =
				items.Sum(p => p.GetLength()) < Settings.LubDecompilerSettings.TextLengthLimit &&
				((Settings.LubDecompilerSettings.GroupIfAllValues && items.All(p => p is LubValueType)) ||
				 (Settings.LubDecompilerSettings.GroupIfAllKeyValues && items.All(p => p is LubKeyValue)));

			if (sameLine) {
				builder.Append("{ ");
			}
			else {
				builder.AppendLine("{");
			}

			ILubObject item;
			bool encapsulate = false;

			if (Settings.LubDecompilerSettings.EncapsulateByCheckingOtherKeys) {
				for (int i = 0; i < items.Count && i < 2000; i++) {
					var lubKeyValue = items[i] as LubKeyValue;

					if (lubKeyValue != null) {
						if (lubKeyValue.Key.Value[0] == '[') {
							i = items.Count;
							encapsulate = true;
							break;
						}

						if (!lubKeyValue.Key.IsValid()) {
							encapsulate = true;
							break;
						}
					}
				}
			}

			int nextLevel = sameLine ? 0 : level + 1;
			string separator = sameLine ? ", " : ",";

			for (int i = 0; i < items.Count; i++) {
				item = items[i];

				if (item is LubKeyValue) {
					((LubKeyValue)item).Print(builder, nextLevel, encapsulate);
				}
				else if (item is LubValueType) {
					builder.AppendIndent(nextLevel);
					item.Print(builder, nextLevel);
				}
				else {
					item.Print(builder, nextLevel);
				}

				if (i < items.Count - 1) {
					builder.Append(separator);
				}

				if (!sameLine) {
					builder.AppendLine();
				}
			}

			if (!sameLine) {
				builder.AppendIndent(nextLevel - 1);
				builder.Append("}");
			}
			else {
				builder.Append(" }");
			}
		}

		public int GetLength() {
			return _items.Sum(p => p.GetLength()) + _dico.Sum(p => p.Value.GetLength());
		}

		#endregion

		public void PrintKey(LubString key, StringBuilder builder, int level) {
			if (builder.Length > 0 && builder[builder.Length - 1] == '\n') {
				builder.AppendIndent(level - 1);
			}

			ILubObject item = _dico[key.ToString()];

			Settings.LubDecompilerSettings.OneTimeOverrideGroupIfAllKeyValues(false);

			item.Print(builder, level);

			Settings.LubDecompilerSettings.RemoveOverrides();
		}

		public bool ContainsKey(LubString key) {
			return _dico.ContainsKey(key.ToString());
		}

		public ILubObject GetValue(LubString key) {
			return _dico[key.ToString()].Value;
		}

		public LubKeyValue GetKeyValue(LubString key) {
			return _dico[key.ToString()];
		}

		public void SetKeyValue(LubString key, ILubObject value) {
			if (_dico.ContainsKey(key.ToString())) {
				_dico[key.ToString()].Value = value;
			}
			else {
				LubKeyValue item = new LubKeyValue(key, value);
				_dico.Add(key.ToString(), item);
			}
		}

		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			Print(builder, 0);
			return builder.ToString();
		}

		public void AddList(List<ILubObject> items) {
			_items.AddRange(items);
		}
	}

	public class LubKeyValue : ILubObject {
		public LubKeyValue(LubString key, ILubObject value) {
			Key = key;
			Value = value;
		}

		public LubString Key { get; set; }
		public ILubObject Value { get; set; }
		internal static string EncapsulateChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
		internal static bool[] EncapsulateArray = new bool[256];

		static LubKeyValue() {
			for (int i = 0; i < EncapsulateChars.Length; i++) {
				EncapsulateArray[EncapsulateChars[i]] = true;
			}
		}

		#region ILubObject Members
		public void Print(StringBuilder builder, int level, bool encapsulate) {
			if (builder.Length > 0 && builder[builder.Length - 1] == '\n') {
				builder.AppendIndent(level);
			}
			else if (builder.Length == 0) {
				builder.AppendIndent(level);
			}

			//builder.AppendIndent(level);
			string key = Key.ToString();

			if (encapsulate) {
				if (Key.Value == "__newindex" || Key.Value == "__index")
					builder.Append(key);
				else if (Key.Value.Length > 0)
					if (char.IsDigit(Key.Value[0]))
						builder.Append("[" + Key + "]");
					else if (key[0] == '[')
						builder.Append(key);
					else if (key[0] == '\"')
						builder.Append("[" + Key + "]");
					else
						builder.Append("[\"" + Key + "\"]");
				else
					builder.Append("[\"" + Key + "\"]");
			}
			else {
				builder.Append(key);
			}

			builder.Append(" = ");

			if (Value == null) {
				builder.Append("nil");
				return;
			}

			Value.Print(builder, level);
		}

		public void Print(StringBuilder builder, int level) {
			Print(builder, level, false);
		}

		public int GetLength() {
			return Key.GetLength() + (Value == null ? 0 : Value.GetLength()) + 3;
		}

		#endregion

		public override string ToString() {
			return Key.ToString();
		}
	}

	public abstract class LubValueType : ILubObject {
		private LubSourceType _source = LubSourceType.Constant;

		public LubSourceType Source {
			get { return _source; }
			set { _source = value; }
		}

		#region ILubObject Members
		public abstract void Print(StringBuilder builder, int level);

		public abstract int GetLength();
		#endregion
	}

	public class LubReferenceType : ILubObject {
		public LubReferenceType(LubString key, ILubObject value) {
			Key = key;
			Value = value;
		}

		public LubReferenceType(LubString key, ILubObject value, int start, int end) {
			Key = key;
			Value = value;
			StartLine = start;
			EndLine = end;
		}

		public LubString Key { get; set; }
		public ILubObject Value { get; set; }
		public ILubObject LoopValue { get; set; }
		public int StartLine { get; set; }
		public int EndLine { get; set; }

		private readonly Stack<LubString> _stackKeys = new Stack<LubString>();
		private readonly Stack<ILubObject> _stackValues = new Stack<ILubObject>();

		#region ILubObject Members
		public void Print(StringBuilder builder, int level) {
			if (Value == null) {
				if (builder.Length > 0 && builder[builder.Length - 1] == '\n')
					builder.AppendIndent(level);

				builder.Append(Key);
				return;
			}

			if (Key.ToString() != "") {
				builder.Append(Key + " = ");
			}

			Value.Print(builder, level + 1);
		}

		public int GetLength() {
			return Key.GetLength() + (Value == null ? 0 : Value.GetLength()) + 3;
		}

		#endregion

		public override string ToString() {
			if (Value is LubReferenceType)
				return Key + " = " + "ERROR";

			return Key + " = " + Value;
		}

		public bool IsValid(int pc) {
			return pc >= StartLine && pc <= EndLine;
		}

		public void Push() {
			_stackKeys.Push(Key);
			_stackValues.Push(Value);
		}

		public void Pop() {
			Key = _stackKeys.Pop();
			Value = _stackValues.Pop();
		}
	}

	public class LubMathOutput : LubOutput {
		private readonly string _value;

		public LubMathOutput(ILubObject left, string op, ILubObject right)
			: base(left + op + right) {
			if (left is LubMathOutput && right is LubMathOutput) {
				_value = "(" + left + ")" + op + "(" + right + ")";
			}
			else if (left is LubMathOutput) {
				_value = "(" + left + ")" + op + right;
			}
			else if (right is LubMathOutput) {
				_value = left + op + "(" + right + ")";
			}
			else {
				_value = left + op + right;
			}
		}

		public LubMathOutput(ILubObject left, string op)
			: base(op + left) {
			if (left is LubMathOutput) {
				_value = op + "(" + left + ")";
			}
			else {
				_value = op + left;
			}
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value);
		}

		public override int GetLength() {
			return _value.Length;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value;
		}
	}

	public class LubOutput : LubValueType {
		private readonly string _value;
		public bool ValidKey { get; set; }

		public LubOutput(string value, bool validKey = false) {
			_value = value;
			ValidKey = validKey;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value);
		}

		public override int GetLength() {
			return _value.Length;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value;
		}
	}

	public class LubString : LubValueType {
		private readonly string _value;
		public bool? ValidString { get; set; }

		internal string Value {
			get { return _value; }
		}

		public LubString(string value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value);
		}

		public override int GetLength() {
			return _value.Length;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public bool IsValid() {
			if (ValidString.HasValue)
				return ValidString.Value;

			if (Value.Length > 0) {
				if (char.IsDigit(Value[0])) {
					ValidString = false;
					return false;
				}

				for (int i = 0; i < Value.Length; i++) {
					if (!char.IsLetterOrDigit(Value[i]) && Value[i] != '_') {
						ValidString = false;
						return false;
					}
				}
			}

			ValidString = true;
			return true;
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value;
		}
	}

	public class LubNull : LubValueType {
		public override void Print(StringBuilder builder, int level) {
			builder.Append("nil");
		}

		public override int GetLength() {
			return 3;
		}

		public override int GetHashCode() {
			return 0;
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return "nil";
		}
	}

	public class LubBoolean : LubValueType {
		private readonly bool _value;

		public bool Value {
			get { return _value; }
		}

		public LubBoolean(bool value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value ? "true" : "false");
		}

		public override int GetLength() {
			return 5;
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value ? "true" : "false";
		}
	}

	public class LubNumber : LubValueType {
		private readonly double _value;

		public LubNumber(double value) {
			_value = value;
		}

		public override void Print(StringBuilder builder, int level) {
			builder.Append(_value.ToString(CultureInfo.InvariantCulture));
		}

		public override int GetLength() {
			if (_value >= 0) {
				if (_value < 10) return 1;
				if (_value < 100) return 2;
				if (_value < 1000) return 3;
				if (_value < 10000) return 4;
				if (_value < 100000) return 5;
				if (_value < 1000000) return 6;
				if (_value < 10000000) return 7;
				if (_value < 100000000) return 8;
				if (_value < 1000000000) return 9;
				return 10;
			}
			else {
				if (_value > -10) return 2;
				if (_value > -100) return 3;
				if (_value > -1000) return 4;
				if (_value > -10000) return 5;
				if (_value > -100000) return 6;
				if (_value > -1000000) return 7;
				if (_value > -10000000) return 8;
				if (_value > -100000000) return 9;
				if (_value > -1000000000) return 10;
				return 11;
			}
		}

		public override int GetHashCode() {
			return _value.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj.GetType() == GetType())
				return obj.GetHashCode() == GetHashCode();

			return false;
		}

		public override string ToString() {
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}