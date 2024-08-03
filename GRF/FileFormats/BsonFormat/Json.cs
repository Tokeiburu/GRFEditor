using System;
using System.IO;
using Utilities;

namespace GRF.FileFormats.BsonFormat {
	public static class Json {
		public static BsonList Text2Bson(string text) {
			var reader = new JsonTextReader(text);

			return _readList(reader);
		}

		public static void Text2Binary(string text, string path) {
			var bson = Text2Bson(text);
			Bson.Write(bson, path);
		}

		public static byte[] Text2Binary(string text) {
			byte[] bytes = new byte[0];

			using (MemoryStream stream = new MemoryStream()) {
				var bson = Text2Bson(text);
				Bson.Write(bson, stream);

				bytes = stream.ToArray();
			}

			return bytes;
		}

		private static BsonList _readList(JsonTextReader reader) {
			BsonList list = new BsonList();

			reader.SkipSpace();

			if (reader.ReadChar() != '{')
				throw new Exception("Expected an opening semi-colon {");

			while (_readKeyValue(reader, list)) {
				if (reader.ReadChar() != ',')
					throw new Exception("Expected a comma.");
			}

			if (reader.ReadChar() != '}')
				throw new Exception("Expected a closing semi-colon }");

			reader.SkipSpace();
			return list;
		}

		private static bool _readKeyValue(JsonTextReader reader, BsonList list) {
			reader.SkipSpace();

			BsonKeyValue keyValue = new BsonKeyValue();

			switch (reader.PeekChar()) {
				case '\"':
					keyValue.Key = _readString(reader);
					reader.SkipSpace();

					if (reader.ReadChar() != ':')
						throw new Exception("Expected a semi-column after " + keyValue.Key);

					keyValue.Value = _readValue(reader);
					list.Items.Add(keyValue);
					break;
				case '}':
					return false;
			}

			reader.SkipSpace();

			if (reader.PeekChar() == ',')
				return true;

			return false;
		}

		private static BsonObject _readValue(JsonTextReader reader) {
			reader.SkipSpace();

			switch (reader.PeekChar()) {
				case '{':
					return _readList(reader);
				case '\"':
					return _readString(reader);
				case 't':
				case 'T':
				case 'f':
				case 'F':
					var word = _readSimpleWord(reader).ToLowerInvariant();

					if (word == "false") {
						return new BsonBoolean(false);
					}

					if (word == "true") {
						return new BsonBoolean(true);
					}

					throw new Exception("Expected a boolean.");
				default:
					try {
						var number = _readSimpleNumber(reader);

						if (number.EndsWith("Z") && number.Contains("T")) {
							return new BsonTimestamp(number);
						}

						if (number.Contains(".")) {
							return new BsonDouble(FormatConverters.DoubleConverter(number));
						}

						return new BsonInteger(Int32.Parse(number));
					}
					catch (Exception err) {
						throw new Exception("Failed to parse number.", err);
					}
			}
		}

		private static BsonString _readString(JsonTextReader reader) {
			if (reader.ReadChar() != '\"')
				throw new Exception("Strings must start with a \". Make sure quotations are properly escaped if necessary.");

			int offsetStart = reader.Index;
			bool keepReading = true;

			while (keepReading && reader.CanRead) {
				switch (reader.ReadChar()) {
					case '\\': // escape
						reader.ReadChar();
						break;
					case '\"':
						keepReading = false;
						break;
				}
			}

			if (keepReading)
				throw new Exception("Unable to find the end of string.");

			return new BsonString(reader.Text.Substring(offsetStart, reader.Index - offsetStart - 1));
		}

		private static string _readSimpleWord(JsonTextReader reader) {
			int offsetStart = reader.Index;

			while (reader.CanRead && char.IsLetter(reader.PeekChar())) {
				reader.Index++;
			}

			return reader.Text.Substring(offsetStart, reader.Index - offsetStart);
		}

		private static string _readSimpleNumber(JsonTextReader reader) {
			int offsetStart = reader.Index;

			while (reader.CanRead && (char.IsDigit(reader.PeekChar()) || reader.PeekChar() == '.' || reader.PeekChar() == '-' || reader.PeekChar() == 'T' || reader.PeekChar() == 'Z')) {
				reader.Index++;
			}

			return reader.Text.Substring(offsetStart, reader.Index - offsetStart);
		}
	}
}
