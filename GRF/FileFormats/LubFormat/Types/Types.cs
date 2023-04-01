using System.Text;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Types {
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
		public int StartLine { get; set; }
		public int EndLine { get; set; }

		#region ILubObject Members

		public void Print(StringBuilder builder, int level) {
			if (Value == null) {
				builder.AppendIndent(level + 1);
				builder.Append(Key);
				return;
			}

			if (Key.ToString() != "") {
				builder.Append(Key + " = ");
			}

			Value.Print(builder, level);
		}

		public int GetLength() {
			return Key.GetLength() + (Value == null ? 0 : Value.GetLength()) + 3;
		}

		#endregion

		public override string ToString() {
			return Key + " = " + Value;
		}
	}
}