using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utilities.Parsers.Yaml {
	public class YamlParser {
		public ParserObject Output { get; set; }
		private readonly List<string> _allLines;
		private readonly List<ParserKeyValue> _writeKeyValues;
		private List<ParserArray> _writeArrays;
		private readonly bool _list;
		private readonly string _idKey;
		private Dictionary<string, ParserKeyValue> _indexedWriteKeyValues = null;
		private Dictionary<string, ParserArray> _indexedWriteArrays = null;
		private Encoding _encoding;

		public YamlParser(string file) : this(file, Encoding.Default, ParserMode.Write, "") {
		}

		public YamlParser(string file, Encoding encoding) : this(file, encoding, ParserMode.Write, "") {
		}

		public YamlParser(string file, Encoding encoding, ParserMode mode, string idKey) {
			_encoding = encoding ?? Encoding.Default;

			if (mode == ParserMode.Write) {
				_allLines = File.ReadAllLines(file, _encoding).ToList();
			}

			_parser_main(new YamlFileData(file, _encoding));

			if (mode == ParserMode.Write) {
				var list = Output as ParserArrayBase;
				
				if (list != null) {
					if (Output["Body"] == null) {
						_allLines.Add("");
						_allLines.Add("Body:");

						var body = new ParserKeyValue("Body", _allLines.Count - 1);
						list.AddElement(body);
						body.Parent = list;
						body.Value = new ParserList(_allLines.Count - 1);
					}

					var entries = list.OfType<ParserKeyValue>().ToList();

					if (entries.Count == 1) {
						_writeKeyValues = (entries[0]).Value.OfType<ParserKeyValue>().ToList();
						_writeArrays = (entries[0]).Value.OfType<ParserArray>().ToList();
					}
					else if (entries.Count == 2) {	
						_writeKeyValues = (entries[1]).Value.OfType<ParserKeyValue>().ToList();
						_writeArrays = (entries[1]).Value.OfType<ParserArray>().ToList();
					}

					if (_writeArrays.Count > 0) {
						_idKey = ((ParserKeyValue)_writeArrays[0].Objects[0]).Key;
					}
					else {
						_idKey = idKey;
					}
					
					_list = true;
				}
			}
		}

		public class ByteBuilder {
			private readonly Encoding _encoding;
			private byte[] _data;
			private int _length = 0;

			public ByteBuilder(Encoding endocing) {
				_encoding = endocing;
				_data = new byte[16];
			}

			public void Append(byte b) {
				if (_length >= _data.Length) {
					byte[] dataExpand = new byte[_length * 2];

					Buffer.BlockCopy(_data, 0, dataExpand, 0, _data.Length);

					_data = dataExpand;
				}

				_data[_length] = b;
				_length++;
			}

			public override string ToString() {
				return (_encoding).GetString(_data, 0, _length);
			}
		}

		private class YamlFileData {
			public byte[] Data;
			public int Position;
			public int LineNumber;
			public int CurrentLineIndent;
			public int ValueLength;
			private int _lastLineStartOffset = 0;
			public string FileName { get; private set; }
			private Encoding _encoding;

			public int Length {
				get { return Data.Length; }
			}

			public bool CanRead {
				get { return Position < Data.Length; }
			}

			public YamlFileData(string file, Encoding endocing) {
				Data = File.ReadAllBytes(file);
				FileName = file;
				LineNumber = 1;
				_encoding = endocing;
			}

			public string ReadKey() {
				var b = new ByteBuilder(_encoding);
				bool comment = false;

				while (CanRead) {
					byte c = Data[Position];

					if (c == '\r' || c == '\n' || c == ':')
						break;

					if (c == '#') {
						comment = true;
					}

					b.Append(c);
					Position++;
				}

				// Clean up potential comments
				if (comment) {
					string ret = b.ToString();
					
					for (int i = 1; i < ret.Length; i++) {
						if (ret[i] == '#') {
							if (ret[i - 1] == '\t' || ret[i - 1] == ' ') {
								ret = ret.Substring(0, i - 1);
								break;
							}
						}
					}

					return ret.TrimEnd(' ', '\t');
				}

				return b.ToString();
			}

			public string ReadWord() {
				var b = new ByteBuilder(_encoding);

				while (CanRead) {
					byte c = Data[Position];

					if (c == '\r' || c == '\n' || c == ':' || c == ',' || c == ']')
						break;

					b.Append(c);
					Position++;
				}

				return b.ToString();
			}

			public void Trim() {
				while (CanRead) {
					char c = (char)Data[Position];

					if (c == ' ' || c == '\t') {
						Position++;
						continue;
					}

					if (c == '#') {	// Skip comment line when trimming end of key or word
						while (CanRead) {
							if (Data[Position++] == '\n') {
								Position--;
								return;
							}
						}
					}

					break;
				}
			}

			public bool IsLetter(char b) {
				if ((b >= 'a' && b <= 'z') ||
					(b >= 'A' && b <= 'Z') ||
					(b >= '0' && b <= '9') ||
					b == '_' || b == '-' || b == '\'' || b == '.' || b == '+' || b == '?' || b == '<' || b == '>' || b == '/')
					return true;
				return false;
			}

			public void NextLine() {
				LineNumber++;
				CurrentLineIndent = 0;
				_lastLineStartOffset = Position;
			}

			public bool EoL() {
				if (CanRead) {
					if ((Data[Position] == '\n') || (Data[Position] == '\r' && Data[Position + 1] == '\n'))
						return true;
				}

				return false;
			}

			public string ReadValue() {
				var b = new ByteBuilder(_encoding);
				ValueLength = 0;

				if (Data[Position] == '\"') {
					Position++;

					while (CanRead && Data[Position] != '\r' && Data[Position] != '\n') {
						if (Data[Position] == '\"' && Data[Position - 1] != '\\') {
							Position++;
							break;
						}

						b.Append(Data[Position]);
						Position++;
					}

					ValueLength = 1;
					return b.ToString();
				}
				else if (Data[Position] == '|' || Data[Position] == '>') {
					int indent = CurrentLineIndent;

					SkipLine();

					MoveToIndentEnd();

					if (CurrentLineIndent < indent)	// Technically valid
						return "";

					// Remove all comment blocks, makes it easier to handle afterwards
					indent = CurrentLineIndent;
					int readLines = 0;
					bool trim = true;
					bool blockComment = false;
					int read = 0;

					while (CanRead) {
						char c = (char)Data[Position];
						
						if (c == '\n' || (c == '\r' && Position + 1 < Length && Data[Position + 1] == '\n')) {
							if (c == '\n') {
								Position++;
							}
							else {
								Position += 2;
							}

							NextLine();
							MoveToIndentEnd(indent);

							if (read > 0)
								b.Append((byte)' ');

							readLines++;
							trim = true;
							read = 0;
							continue;
						}

						if (CurrentLineIndent < indent) { // Needs to be checked after to make an exception for empty lines
							break;
						}

						if (Position + 1 < Length) {
							if (blockComment) {
								if (c == '*' && Data[Position + 1] == '/') {
									blockComment = false;
									Position++;
								}

								Position++;
								continue;
							}

							if (c == '/' && Data[Position + 1] == '/') {
								SkipLine();
								MoveToIndentEnd(indent);
								readLines++;
								trim = true;
								read = 0;
								continue;
							}
							else if (c == '/' && Data[Position + 1] == '*') {
								blockComment = true;
								Position++;
								continue;
							}
						}

						if (trim) {
							if (c == ' ' || c == '\t') {
								Position++;
								continue;
							}
						}

						trim = false;
						b.Append((byte)c);
						read++;
						Position++;
					}

					ValueLength = readLines + 1;
					return b.ToString().TrimEnd(' ');
				}
				else {
					while (CanRead && Data[Position] != '\r' && Data[Position] != '\n' && Data[Position] != '#') {
						b.Append(Data[Position]);
						Position++;
					}

					ValueLength = 1;
					return b.ToString().TrimEnd(' ', '\t');
				}
			}

			public char PeekChar() {
				return (char)Data[Position];
			}

			public void MoveToIndentEnd(int max = Int32.MaxValue) {
				while (CanRead && Data[Position] == ' ' && CurrentLineIndent < max) {
					CurrentLineIndent++;
					Position++;
				}
			}

			public void SetupParent(ParserObject parent, int indent) {
				if (parent.Indent == -1) {
					if (CurrentLineIndent < indent)
						throw GetException("Expected list or array declaration.");

					parent.Indent = CurrentLineIndent;
				}

				if (parent.ChildrenIndent == -1) {
					parent.ChildrenIndent = CurrentLineIndent;
				}
			}

			public void SkipLine() {
				while (CanRead) {
					if (Data[Position++] == '\n') {
						NextLine();
						break;
					}
				}
			}

			public Exception GetException(string reason) {
				// Attempt to read last line
				string lastLine = "";
				int oldPosition = Position;

				try {
					Position = _lastLineStartOffset;

					while (CanRead) {
						if (Data[Position++] == '\n') {
							break;
						}
					}

					int length = Position - oldPosition;

					if (length > 0) {
						lastLine = (_encoding).GetString(Data, oldPosition, length);
					}
				}
				finally {
					Position = oldPosition;
				}

				return new Exception("Failed to parse " + FileName + " at line " + LineNumber + ", position " + (Position - _lastLineStartOffset) + "\r\n" + (lastLine == "" ? "" : "* " + lastLine + "\r\n" + "Error: " + reason));
			}
		}

		public enum YamlListType {
			NotDefined,
			Array,
			KeyValue,
		}

		private void _parser_main(YamlFileData file) {
			Output = new ParserArray(file.LineNumber);
			Output.Indent = -1;
			Output.ChildrenIndent = -1;

			_readNode(file, Output, 0, YamlListType.NotDefined);

			if (Output.ParserType == ParserTypes.Array) {
				var parserArray = Output.To<ParserArray>();

				// Copy pate handling
				if (parserArray.Objects.Count > 0 && parserArray.Objects[0].ParserType == ParserTypes.Array) {
					var tmp_list = new ParserList(0);
					var tmp_keyValue = new ParserKeyValue("copy_paste", 0);

					tmp_list.Objects.AddRange(parserArray.Objects);
					tmp_keyValue.Value = tmp_list;
					parserArray.Objects.Clear();
					parserArray.Objects.Add(tmp_keyValue);
				}
			}

			// Calculate lengths
			if (Output != null) {
				_calculateLength(Output);
				Output.Length = _getLength(Output);
			}
		}

		private void _readNode(YamlFileData file, ParserObject parent, int indent, YamlListType listType) {
			string word_s = null;
			
			while (file.CanRead) {
				char c = file.PeekChar();

				switch (c) {
					case '#':
						file.SkipLine();
						continue;
					case '\r':	// Ignore character
						file.Position++;
						continue;
					case '\n':
						file.Position++;
						file.NextLine();
						continue;
					case '-':
						file.SetupParent(parent, indent);

						if (listType == YamlListType.NotDefined) {
							listType = YamlListType.Array;
						}

						// Validate parent indent
						switch(parent.ParserType) {
							case ParserTypes.List:
							case ParserTypes.Array:
								if (file.CurrentLineIndent < parent.ChildrenIndent) {
									if (word_s != null && parent.ParserType == ParserTypes.Array) {
										((ParserArray)parent).AddElement(new ParserString(word_s, file.LineNumber));
										word_s = null;
									}

									return;
								}

								if (file.CurrentLineIndent > parent.ChildrenIndent)
									throw file.GetException("Unexpected indent (parent indent: " + parent.Indent + ", parent child indent: " + parent.ChildrenIndent + ", current indent: " + file.CurrentLineIndent + ").");

								break;
						}

						if (!(file.Position + 2 < file.Length && file.Data[file.Position + 1] == ' ' && file.IsLetter((char)file.Data[file.Position + 2]))) {
							throw file.GetException("Expected a space after the hyphen for the list declaration.");
						}

						// Array declaration
						ParserArray array = new ParserArray(file.LineNumber);
						array.Indent = file.CurrentLineIndent;
						file.CurrentLineIndent += 2;
						array.ChildrenIndent = file.CurrentLineIndent;
						file.Position += 2;

						_readNode(file, array, file.CurrentLineIndent, YamlListType.NotDefined);

						switch (parent.ParserType) {
							case ParserTypes.List:
								((ParserList)parent).AddElement(array);
								break;
							case ParserTypes.Array:	// Used for copy pasting only
								((ParserArray)parent).AddElement(array);
								break;
							default:
								throw file.GetException("Unexpected parent node type. It can either be a list or an array, found a '" + parent.ParserType + "'.");
						}

						continue;
					case ' ':
						file.CurrentLineIndent++;
						file.Position++;
						continue;
					case ':':
						if (string.IsNullOrEmpty(word_s)) {
							throw file.GetException("Missing declaration key before ':'.");
						}

						file.Position++;
						file.Trim();

						ParserKeyValue keyValue = new ParserKeyValue(word_s, file.LineNumber);
						keyValue.Indent = parent.Indent;
						word_s = null;

						// List declaration
						if (file.EoL()) {
							ParserList list = new ParserList(file.LineNumber);
							list.Indent = -1;
							list.ChildrenIndent = -1;
							keyValue.Value = list;

							_readNode(file, list, file.CurrentLineIndent, YamlListType.NotDefined);
						}
						else if (file.Data[file.Position] == '[') { // Aggregate parsing, does not support multi-line
							file.Position++;
							ParserAggregate aggregate = new ParserAggregate(file.LineNumber);
							aggregate.Parent = parent;

							while (file.CanRead) {
								c = file.PeekChar();

								if (c == '\r' || c == '\n')
									throw file.GetException("Unexpected syntax; multi-line aggregate arrays are not supported.");

								if (c == ']')
									break;

								word_s = c == '\"' ? file.ReadValue() : file.ReadWord();
								aggregate.AddElement(new ParserString(word_s.Trim(' '), file.LineNumber));
								c = file.PeekChar();

								while (c != '\n' && (c == ',' || c == ' ' || c == '\r')) {
									file.Position++;
									c = file.PeekChar();
								}
							}

							word_s = null;
							keyValue.Value = aggregate;
						}
						else {	// KeyValue, get the line number first!
							var parserString = new ParserString(null, file.LineNumber);
							parserString.Value = file.ReadValue();
							parserString.Length = file.ValueLength;
							keyValue.Value = parserString;
						}

						switch (parent.ParserType) {
							case ParserTypes.List:
								((ParserList)parent).AddElement(keyValue);
								break;
							case ParserTypes.Array:
								((ParserArray)parent).AddElement(keyValue);
								break;
						}

						continue;
					default:
						file.SetupParent(parent, indent);

						if (listType == YamlListType.NotDefined) {
							listType = YamlListType.KeyValue;
						}

						// Validate parent indent
						switch(parent.ParserType) {
							case ParserTypes.List:
								if (file.CurrentLineIndent < parent.ChildrenIndent)
									return;

								if (file.CurrentLineIndent == parent.ChildrenIndent && listType != YamlListType.KeyValue)
									return;

								if (file.CurrentLineIndent > parent.ChildrenIndent)
									throw file.GetException("Unexpected indent while reading key (parent indent: " + parent.Indent + ", parent child indent: " + parent.ChildrenIndent + ", current indent: " + file.CurrentLineIndent + ").");

								break;
							case ParserTypes.Array:
								if (file.CurrentLineIndent < parent.ChildrenIndent) {
									if (word_s != null && parent.ParserType == ParserTypes.Array) {
										((ParserArray)parent).AddElement(new ParserString(word_s, file.LineNumber));
										word_s = null;
									}

									return;
								}

								if (file.CurrentLineIndent > parent.ChildrenIndent)
									throw file.GetException("Unexpected indent while reading key (parent indent: " + parent.Indent + ", parent child indent: " + parent.ChildrenIndent + ", current indent: " + file.CurrentLineIndent + ").");

								break;
						}

						word_s = file.ReadKey();

						if (word_s.Length == 0) {
							throw file.GetException("Null-length word. This is most likely caused by an unexpected character in a string.");
						}

						file.Trim();
						continue;
				}
			}
		}

		private int _getLength(ParserObject obj) {
			switch (obj.ParserType) {
				case ParserTypes.String:
					return Math.Max(1, obj.Length);
				case ParserTypes.Aggregate:
				case ParserTypes.Null:
				case ParserTypes.Number:
					return 1;
				case ParserTypes.List:
					var list = (ParserList)obj;

					if (list.Objects.Count == 0)
						return 0;

					return list.Last().Line - obj.Line + list.Last().Length;
				case ParserTypes.Array:
					var array = (ParserArray)obj;

					if (array.Objects.Count == 0)
						return 0;

					return array.Last().Line - obj.Line + array.Last().Length;
				case ParserTypes.KeyValue:
					var keyValue = (ParserKeyValue)obj;

					if (keyValue.Value == null)
						return 0;

					return _getLength(keyValue.Value) + keyValue.Line - obj.Line;
			}

			return 0;
		}

		private void _calculateLength(ParserObject obj) {
			switch (obj.ParserType) {
				case ParserTypes.String:
					obj.Length = Math.Max(obj.Length, 1);
					break;
				case ParserTypes.Aggregate:
				case ParserTypes.Null:
				case ParserTypes.Number:
					obj.Length = 1;
					break;
				case ParserTypes.Array:
				case ParserTypes.List:
					foreach (var ele in obj) {
						_calculateLength(ele);
					}

					obj.Length = _getLength(obj);
					break;
				case ParserTypes.KeyValue:
					var value = ((ParserKeyValue)obj).Value;
					_calculateLength(value);
					obj.Length = _getLength(obj);
					break;
			}
		}

		public void Write(string key, string line) {
			if (!_list) {
				ParserKeyValue conf = _writeKeyValuesFind(key);

				if (conf == null) {
					// Add a new one!
					var entry = new ParserKeyValue(key, Int32.MaxValue) {
						Value = new ParserString(line, Int32.MaxValue),
						Added = true,
						Length = 1
					};

					_writeKeyValues.Add(entry);
					_indexedWriteKeyValues[key] = entry;
					return;
				}

				conf.Modified = true;
				conf.Value = new ParserString(line, conf.Value.Line);
			}
			else {
				ParserArray conf = _writeArraysFind(key);
				//_writeArrays.FirstOrDefault(p => p[_idKey] == key);

				if (conf == null) {
					var entry = new ParserArray(Int32.MaxValue) {
						Objects = new List<ParserObject> {
							new ParserKeyValue(_idKey, Int32.MaxValue) {
								Value = new ParserString(key, Int32.MaxValue),
							},
							new ParserKeyValue("Content__", Int32.MaxValue) {
								Value = new ParserString(line, Int32.MaxValue),
							}
						},
						Added = true,
						Length = 1
					};

					_writeArrays.Add(entry);
					_indexedWriteArrays[key] = entry;
					return;
				}

				conf.Modified = true;
				conf.Objects = new List<ParserObject> {
					new ParserKeyValue(_idKey, Int32.MaxValue) {
						Value = new ParserString(key, Int32.MaxValue),
					},
					new ParserKeyValue("Content__", Int32.MaxValue) {
						Value = new ParserString(line, Int32.MaxValue),
					}
				};
			}
		}

		private ParserKeyValue _writeKeyValuesFind(string key) {
			if (_indexedWriteKeyValues == null) {
				_indexedWriteKeyValues = new Dictionary<string, ParserKeyValue>();

				foreach (var entry in _writeKeyValues) {
					if (_indexedWriteKeyValues.ContainsKey(entry.Key)) {
						continue;
					}

					_indexedWriteKeyValues[entry.Key] = entry;
				}
			}

			if (key == null)
				return null;

			ParserKeyValue ret;

			if (_indexedWriteKeyValues.TryGetValue(key, out ret))
				return ret;

			return null;
		}

		private ParserArray _writeArraysFind(string key) {
			if (_indexedWriteArrays == null) {
				_indexedWriteArrays = new Dictionary<string, ParserArray>();

				foreach (var entry in _writeArrays) {
					if (_indexedWriteArrays.ContainsKey(entry[_idKey])) {
						continue;
					}

					_indexedWriteArrays[entry[_idKey]] = entry;
				}
			}

			if (key == null)
				return null;

			ParserArray ret;

			if (_indexedWriteArrays.TryGetValue(key, out ret))
				return ret;

			return null;
		}

		public void Delete(string sKey) {
			if (!_list) {
				ParserKeyValue conf = _writeKeyValuesFind(sKey);
				
				if (conf != null) {
					for (int i = 0; i < conf.Length; i++) {
						_allLines[i + conf.Line - 1] = null;
					}
				}
			}
			else {
				ParserArray conf = _writeArraysFind(sKey);

				if (conf != null) {
					for (int i = 0; i < conf.Length; i++) {
						_allLines[i + conf.Line - 1] = null;
					}
				}
			}
		}

		public void WriteFile(string path) {
			// Where to add lines
			int addIndex = _allLines.Count;
			StringBuilder builder = new StringBuilder();

			if (!_list) {
				var last = _writeKeyValues.Where(p => !p.Added).OrderByDescending(p => p.Line).FirstOrDefault();

				if (last != null) {
					addIndex = last.Line + (last.Length <= 0 ? 1 : last.Length) - 1;
				}

				foreach (var confElement in _writeKeyValues.OrderByDescending(p => p.Line)) {
					if (confElement.Added) {
						_allLines.Insert(addIndex, string.Concat("\t", confElement.Key, ": ", confElement.Value));
						continue;
					}

					if (confElement.Modified) {
						for (int i = 0; i < confElement.Length; i++) {
							_allLines[i + confElement.Line - 1] = null;
						}

						_allLines[confElement.Line - 1] = string.Concat("\t", confElement.Key, ": ", confElement.Value);
					}
				}
			}
			else {
				// Set modified lines first
				foreach (var confElement in _writeArrays.Where(p => p.IsModified())) {
					for (int i = 0; i < confElement.Length; i++) {
						_allLines[i + confElement.Line - 1] = null;
					}

					_allLines[confElement.Line - 1] = confElement.Objects[1].ObjectValue;
				}

				AlphanumComparator alphaComparer = new AlphanumComparator(StringComparison.Ordinal);
				_writeArrays = _writeArrays.OrderBy(p => p[_idKey], alphaComparer).ToList();

				// Set added lines in order of their ID
				foreach (var confElement in _writeArrays.Where(p => p.Added).OrderByDescending(p => Int32.Parse(p[_idKey]))) {
					int lineIndex = _getInsertIndex(confElement);
					_allLines.Insert(lineIndex - 1, confElement.Objects[1].ObjectValue);
				}
			}

			foreach (var line in _allLines) {
				if (line != null)
					builder.AppendLine(line);
			}

			File.WriteAllText(path, builder.ToString(), _encoding);
		}

		private int _getInsertIndex(ParserArray confElement) {
			if (_writeArrays.Count == 0) {
				return _allLines.Count + 1;
			}

			var last = _writeArrays.LastOrDefault(p => !p.Added);

			if (last == null) {
				return _allLines.Count + 1;
			}

			int lineIndex = last.Line + last.Length;

			int arrayIndex = _writeArrays.IndexOf(confElement);

			arrayIndex++;

			while (arrayIndex < _writeArrays.Count && _writeArrays[arrayIndex].Added) {
				arrayIndex++;
			}

			if (arrayIndex < _writeArrays.Count - 1) {
				return _writeArrays[arrayIndex].Line;
			}

			return lineIndex;
		}
	}
}