using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities.Parsers.Lua.Structure {
	public abstract class ILuaVariable {
		public abstract void Write(StringBuilder writer, int indentLevel);

		//public override string ToString() {
		//	if (this is LuaValue) {
		//		return ((LuaValue)this).Value;
		//	}
		//
		//	return base.ToString();
		//}
	}

	public class LuaKeyValue : ILuaVariable {
		public string Key { get; set; }
		public ILuaVariable Value { get; set; }

		#region ILuaVariable Members

		public override void Write(StringBuilder writer, int indentLevel) {
			LuaWriter.GetIndent(writer, indentLevel);
			writer.Append(Key);
			writer.Append(" = ");

			if (Value is LuaValue)
				writer.Append(((LuaValue) Value).Value);
			else {
				Value.Write(writer, indentLevel);
			}
		}

		#endregion

		public override string ToString() {
			return Key + " = " + Value;
		}
	}

	public class LuaList : ILuaVariable, IEnumerable {
		public List<ILuaVariable> Variables = new List<ILuaVariable>();
		internal Dictionary<string, ILuaVariable> _keyValues = new Dictionary<string, ILuaVariable>();

		#region ILuaVariable Members

		public override void Write(StringBuilder writer, int indentLevel) {
			writer.AppendLine("{");
			indentLevel++;
			for (int index = 0; index < Variables.Count; index++) {
				ILuaVariable variable = Variables[index];

				if (variable is LuaList) {
					LuaWriter.GetIndent(writer, indentLevel);
				}

				variable.Write(writer, indentLevel);

				if (variable is LuaStringValue && index < Variables.Count - 1)
					writer.AppendLine(";");
				else if (index < Variables.Count - 1)
					writer.AppendLine(",");
			}
			indentLevel--;
			writer.AppendLine();
			LuaWriter.GetIndent(writer, indentLevel);
			writer.Append('}');
		}

		#endregion

		public ILuaVariable this[string key] {
			get {
				if (_keyValues.ContainsKey(key))
					return _keyValues[key];
				
				return null;
			}
		}

		public IEnumerator GetEnumerator() {
			return Variables.GetEnumerator();
		}

		public static List<int> ToIntList(ILuaVariable value) {
			return ((LuaList)value).Variables.OfType<LuaValue>().Select(p => Int32.Parse(p.Value)).ToList();
		}
	}

	public class LuaValue : ILuaVariable {
		public string Value { get; set; }

		#region ILuaVariable Members

		public override void Write(StringBuilder writer, int indentLevel) {
			LuaWriter.GetIndent(writer, indentLevel);
			writer.Append(Value);
		}

		#endregion

		public override string ToString() {
			return Value;
		}
	}

	public class LuaStringValue : LuaValue {
	}

	public class LuaFunction : ILuaVariable {
		public string Name { get; set; }
		public string Value { get; set; }

		#region ILuaVariable Members

		public override void Write(StringBuilder writer, int indentLevel) {
			writer.AppendLine(Name);
			writer.AppendLine(Value);
		}

		#endregion
	}
}
