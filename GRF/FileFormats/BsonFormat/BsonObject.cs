using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Utilities.Extension;

namespace GRF.FileFormats.BsonFormat {
	public abstract class BsonObject {
		public abstract void Print(StringBuilder b, int indent);
		public abstract void Write(BinaryWriter writer);
		public abstract BsonType GetBsonType();
	}

	public class BsonString : BsonObject {
		public string Value { get; set; }

		public BsonString(string value) {
			Value = value;
		}

		public override string ToString() {
			return "\"" + Value + "\"";
		}

		public override void Print(StringBuilder b, int indent) {
			b.Append("\"");
			b.Append(Value);
			b.Append("\"");
		}

		public override void Write(BinaryWriter writer) {
			var bytes = Encoding.UTF8.GetBytes(Value.Unescape(EscapeMode.Normal));
			writer.Write(bytes.Length + 1);
			writer.Write(bytes);
			writer.Write((byte)0);
		}

		public override BsonType GetBsonType() {
			return BsonType.String;
		}
	}

	public class BsonInteger : BsonObject {
		public int Value { get; set; }

		public BsonInteger(int value) {
			Value = value;
		}

		public override string ToString() {
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override void Print(StringBuilder b, int indent) {
			b.Append(this);
		}

		public override void Write(BinaryWriter writer) {
			writer.Write(Value);
		}

		public override BsonType GetBsonType() {
			return BsonType.Integer;
		}
	}

	public class BsonDouble : BsonObject {
		public double Value { get; set; }

		public BsonDouble(double value) {
			Value = value;
		}

		public override string ToString() {
			if ((Value % 1) == 0) {
				return Value.ToString(CultureInfo.InvariantCulture) + ".0";
			}

			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override void Print(StringBuilder b, int indent) {
			b.Append(this);
		}

		public override void Write(BinaryWriter writer) {
			writer.Write(Value);
		}

		public override BsonType GetBsonType() {
			return BsonType.Number;
		}
	}

	public class BsonBoolean : BsonObject {
		public bool Value { get; set; }

		public BsonBoolean(bool value) {
			Value = value;
		}

		public override string ToString() {
			return Value.ToString().ToLowerInvariant();
		}

		public override void Print(StringBuilder b, int indent) {
			b.Append(this);
		}

		public override void Write(BinaryWriter writer) {
			writer.Write((byte)(Value == true ? 1 : 0));
		}

		public override BsonType GetBsonType() {
			return BsonType.Boolean;
		}
	}

	public class BsonTimestamp : BsonObject {
		internal static readonly Int64 InitialJavaScriptDateTicks = 621355968000000000;

		public Int64 Value { get; set; }

		public BsonTimestamp(string value) {
			Value = DateTime.Parse(value).ToFileTimeUtc();
		}

		public BsonTimestamp(Int64 value) {
			Value = value;
		}

		public override string ToString() {
			DateTime dateTime = new DateTime((Value * 10000) + InitialJavaScriptDateTicks, DateTimeKind.Utc);
			return dateTime.ToString("yyyy-MM-ddThh:mm:ss.fffZ");
		}

		public override void Print(StringBuilder b, int indent) {
			b.Append(this);
		}

		public override void Write(BinaryWriter writer) {
			writer.Write(Value);
		}

		public override BsonType GetBsonType() {
			return BsonType.TimeStamp;
		}
	}

	public class BsonKeyValue : BsonObject {
		private BsonString _key;

		public BsonString Key {
			get { return _key; }
			set { _key = value; }
		}

		private BsonObject _value;

		public BsonObject Value {
			get { return _value; }
			set { _value = value; }
		}

		public override string ToString() {
			return Key + ": Bson." + Enum.GetName(typeof(BsonType), Value.GetBsonType());
		}

		public override void Print(StringBuilder b, int indent) {
			b.AppendIndent(indent);
			b.Append(Key);
			b.Append(": ");
			Value.Print(b, indent);
		}

		public override void Write(BinaryWriter writer) {
			writer.Write((byte)Value.GetBsonType());
			writer.Write(Encoding.UTF8.GetBytes(Key.Value));
			writer.Write((byte)0);

			Value.Write(writer);
		}

		public override BsonType GetBsonType() {
			return BsonType.Null;
		}
	}

	public class BsonList : BsonObject {
		private readonly List<BsonObject> _items = new List<BsonObject>();

		public List<BsonObject> Items {
			get { return _items; }
		}

		public override void Print(StringBuilder b, int indent) {
			b.AppendLine("{");

			for (int i = 0; i < Items.Count; i++) {
				Items[i].Print(b, indent + 1);

				if (i == Items.Count - 1) {
					b.AppendLine();
				}
				else {
					b.AppendLine(",");
				}
			}

			b.AppendIndent(indent);
			b.Append("}");
		}

		public override void Write(BinaryWriter writer) {
			var offsetStart = writer.BaseStream.Position;
			writer.Write(0);

			foreach (var item in Items) {
				item.Write(writer);
			}

			var offsetEnd = writer.BaseStream.Position;
			var totalLength = offsetEnd + 1 - offsetStart;
			writer.BaseStream.Seek(offsetStart, SeekOrigin.Begin);
			writer.Write((int)totalLength);
			writer.BaseStream.Seek(offsetEnd, SeekOrigin.Begin);
			writer.Write((byte)0);
		}

		public override BsonType GetBsonType() {
			return BsonType.Object;
		}
	}
}
