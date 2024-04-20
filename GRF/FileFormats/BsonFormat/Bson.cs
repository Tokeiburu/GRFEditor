using System;
using System.IO;
using System.Text;
using GRF.IO;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.FileFormats.BsonFormat {
	public static class Bson {
		public static BsonList Parse(MultiType data) {
			var reader = data.GetBinaryReader();
			var bsonList = new BsonList();
			_parseList(reader, bsonList, reader.Int32());
			return bsonList;
		}

		public static void Write(BsonObject bsonObject, Stream stream) {
			using (BinaryWriter writer = new BinaryWriter(stream)) {
				bsonObject.Write(writer);
			}
		}

		public static void Write(BsonObject bsonObject, string path) {
			using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))) {
				bsonObject.Write(writer);
			}
		}

		public static string Bson2Json(BsonList obj) {
			var b = new StringBuilder();
			obj.Print(b, 0);
			return b.ToString();
		}

		public static string Bson2Json(MultiType data) {
			var b = new StringBuilder();
			var obj = Parse(data);
			obj.Print(b, 0);
			return b.ToString();
		}

		private static BsonList _parseList(IBinaryReader reader, BsonList bsonBaseList, int length) {
			int start = reader.Position;
			int end = start + length - 4;	// -4 for the length which is 4 bytes, it is included in the total length

			while (reader.CanRead && reader.Position < end - 1) {
				byte type = reader.Byte();

				BsonKeyValue keyValue = new BsonKeyValue();
				keyValue.Key = _readString(reader);

				switch ((BsonType)type) {
					case BsonType.Long:
						keyValue.Value = new BsonInteger(reader.Int32());
						reader.Int32();
						bsonBaseList.Items.Add(keyValue);
						break;
					case BsonType.Integer:
						keyValue.Value = new BsonInteger(reader.Int32());
						bsonBaseList.Items.Add(keyValue);
						break;
					case BsonType.Number:
						keyValue.Value = new BsonDouble(reader.Double());
						bsonBaseList.Items.Add(keyValue);
						break;
					case BsonType.Object:
					case BsonType.Array:
						keyValue.Value = _parseList(reader, new BsonList(), reader.Int32()); ;
						bsonBaseList.Items.Add(keyValue);
						break;
					case BsonType.Boolean:
						keyValue.Value = new BsonBoolean(reader.Byte() != 0);
						bsonBaseList.Items.Add(keyValue);
						break;
					case BsonType.String:
						keyValue.Value = _readString(reader, reader.Int32());
						bsonBaseList.Items.Add(keyValue);
						break;
					default:
						throw new Exception("Unsupported bson type: " + type);
				}
			}

			if (!reader.CanRead || reader.Byte() != 0) {
				throw new Exception("Unexpected end of object byte value.");
			}

			return bsonBaseList;
		}

		private static BsonString _readString(IBinaryReader reader) {
			int start = reader.Position;
			char c;

			while (reader.CanRead && (c = reader.Char()) != '\0') {
				// skip
			}

			int length = reader.Position - start - 1;
			reader.Position = start;
			string output = EncodingService.Utf8.GetString(reader.Bytes(length), 0, length);
			reader.Forward(1);
			return new BsonString(output.Escape(EscapeMode.Normal));
		}

		private static BsonString _readString(IBinaryReader reader, int length) {
			string output = EncodingService.Utf8.GetString(reader.Bytes(length - 1), 0, length - 1);
			reader.Forward(1);
			return new BsonString(output.Escape(EscapeMode.Normal));
		}
	}
}
