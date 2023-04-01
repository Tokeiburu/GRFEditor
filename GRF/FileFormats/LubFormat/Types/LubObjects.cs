using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.System;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Types {
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
				builder.AppendIndent(level - 1);
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

			for (int i = 0; i < items.Count && i < 2000 && !encapsulate; i++) {
				var lubKeyValue = items[i] as LubKeyValue;

				if (lubKeyValue != null) {
					for (int j = 0; j < lubKeyValue.Key.Value.Length; j++) {
						if (lubKeyValue.Key.Value[0] == '[') {
							i = items.Count;
							break;
						}

						if (!LubKeyValue.EncapsulateArray[lubKeyValue.Key.Value[j]]) {
							encapsulate = true;
							break;
						}
					}
				}
			}

			for (int i = 0; i < items.Count; i++) {
				item = items[i];

				if (item is LubKeyValue) {
					((LubKeyValue)item).Print(builder, sameLine && ((LubKeyValue)item).Value is LubValueType ? 0 : level, encapsulate);
				}
				else if (item is LubValueType) {
					if (!sameLine) {
						builder.AppendIndent(level);
					}

					item.Print(builder, level);
				}
				else {
					item.Print(builder, level + 1);
				}

				if (i < items.Count - 1) {
					builder.Append(!sameLine ? "," : ", ");
				}

				if (!sameLine) {
					builder.AppendLine();
				}
			}

			if (!sameLine) {
				builder.AppendIndent(level - 1);
				builder.Append("}");
			}
			else {
				builder.Append(" }");
			}
		}

		public int GetLength() {
			return Length ?? _items.Sum(p => p.GetLength()) + _dico.Sum(p => p.Value.GetLength());
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
		protected int? Length { get; set; }
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

			Value.Print(builder, level + 1);
		}

		public void Print(StringBuilder builder, int level) {
			Print(builder, level, false);
		}

		public int GetLength() {
			return Length ?? Key.GetLength() + (Value == null ? 0 : Value.GetLength()) + 3;
		}

		#endregion

		public override string ToString() {
			return Key.ToString();
		}
	}
}