using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utilities.Extension;

namespace Utilities.Parsers.Libconfig {
	public class LibconfigParser {
		private int _bufferIndex;
		private byte[] _buffer;
		private string _word = "";
		public ParserObject Output { get; set; }
		private ParserObject _latest;
		private readonly string _file;
		private readonly List<string> _allLines;
		private readonly List<ParserKeyValue> _writeKeyValues;
		private List<ParserArray> _writeArrays;
		private readonly bool _list;
		private readonly string _idKey;

		public int Line { get; private set; }

		public int LinePosition {
			get {
				int bufferIndex = _bufferIndex;

				while (bufferIndex > 0 && _buffer[bufferIndex] != '\n') {
					bufferIndex--;
				}

				return _bufferIndex - bufferIndex;
			}
		}

		public LibconfigParser(string file, Encoding encoding = null, ParserMode mode = ParserMode.Write) {
			encoding = encoding ?? Encoding.Default;

			if (mode == ParserMode.Write) {
				_allLines = File.ReadAllLines(file, encoding).ToList();
			}

			_file = file;
			Line = 1;
			Output = null;

			_parse(File.ReadAllBytes(file));

			if (mode == ParserMode.Write) {
				var list = ((ParserKeyValue)Output).Value as ParserArrayBase;

				if (list != null) {
					_writeKeyValues = list.OfType<ParserKeyValue>().ToList();
					_writeArrays = list.OfType<ParserArray>().ToList();

					if (_writeArrays.Count > 0) {
						_idKey = ((ParserKeyValue)_writeArrays[0].Objects[0]).Key;
						_list = true;
					}

					if (_writeArrays.Count == 0 && _writeKeyValues.Count == 0) {
						if (list is ParserList) {
							switch(((ParserKeyValue)Output).Key) {
								case "item_db":
									_idKey = "Id";
									break;
								case "quest_db":
									_idKey = "Id";
									break;
								case "mob_db":
									_idKey = "Id";
									break;
								case "achievement_db":
									_idKey = "id";
									break;
								default:
									throw new Exception("Failed to identify the key for this database.");
							}

							_list = true;
						}
					}
				}
			}
		}

		private void _parse(byte[] buffer) {
			try {
				_buffer = buffer;
				_bufferIndex = 0;

				while (_bufferIndex < buffer.Length) {
					switch((char)buffer[_bufferIndex]) {
						case '/':
							if (buffer[_bufferIndex + 1] == '/') {
								_skipLine();
								continue;
							}

							if (buffer[_bufferIndex + 1] == '*') {
								_skipCommentBlock();
								continue;
							}
							break;
						case ':':
							ParserKeyValue keyValue = new ParserKeyValue(_word, Line);

							if (_latest == null && Output != null) {
								// The file contains multiple arrays
								ParserArray tempList = new ParserArray(Line);
								tempList.AddElement(Output);
								Output.Parent = tempList;
								Output = tempList;
								_latest = tempList;
							}

							if (_latest == null) {
								Output = keyValue;
								_latest = Output;
							}
							else {
								keyValue.Parent = _latest;

								switch(_latest.ParserType) {
									case ParserTypes.Array:
										((ParserArray)_latest).AddElement(keyValue);
										_latest = keyValue;
										break;
									default:
										throw new Exception("Expected an Array.");
								}
							}

							break;
						case '<':
							_readMultilineQuote();

							if (_latest != null) {
								switch(_latest.ParserType) {
									case ParserTypes.KeyValue:
										((ParserKeyValue)_latest).Value = new ParserString(_word, Line);
										_latest.Length = Line - _latest.Line + 1;
										_latest = _latest.Parent;
										break;
									default:
										throw new Exception("Expected a KeyValue.");
								}
							}
							break;
						case '(':
							ParserList list = new ParserList(Line);

							if (_latest == null)
								throw new Exception("Trying to open a List type without a parent.");

							switch(_latest.ParserType) {
								case ParserTypes.List:
									((ParserList)_latest).AddElement(list);
									list.Parent = _latest;
									_latest = list;
									break;
								case ParserTypes.KeyValue:
									((ParserKeyValue)_latest).Value = list;
									list.Parent = _latest;
									_latest = list;
									break;
								default:
									throw new Exception("Expected a KeyValue.");
							}

							break;
						case '{':
							ParserArray array = new ParserArray(Line);

							if (_latest == null) {
								// Used for copy pasting inputs, create a temporary list
								Output = new ParserKeyValue("copy_paste", Line) {
									Value = new ParserList(Line)
								};

								_latest = Output["copy_paste"];
							}

							switch(_latest.ParserType) {
								case ParserTypes.List:
									((ParserList)_latest).AddElement(array);
									array.Parent = _latest;
									_latest = array;
									break;
								case ParserTypes.KeyValue:
									((ParserKeyValue)_latest).Value = array;
									array.Parent = _latest;
									_latest = array;
									break;
								default:
									throw new Exception("Expected a List.");
							}

							break;
						case '[':
							ParserAggregate aggregate = new ParserAggregate(Line);

							if (_latest == null)
								throw new Exception("Trying to open an Aggregate type without a parent.");

							switch(_latest.ParserType) {
								case ParserTypes.KeyValue:
									((ParserKeyValue)_latest).Value = aggregate;
									_latest.Length = Line - _latest.Line + 1;
									aggregate.Parent = _latest;
									_latest = aggregate;
									break;
								default:
									throw new Exception("Expected a KeyValue.");
							}

							break;
						case ']':
						case ')':
						case '}':
							if (_latest == null)
								throw new Exception("Trying to close a statement without knowing its beginning.");

							switch(_latest.ParserType) {
								case ParserTypes.Aggregate:
								case ParserTypes.Array:
								case ParserTypes.List:
									_latest.Length = Line - _latest.Line + 1;
									_latest = _latest.Parent;

									if (_latest is ParserKeyValue) {
										_latest = _latest.Parent;
									}
									break;
								case ParserTypes.KeyValue:
									_latest = _latest.Parent;
									_latest.Length = Line - _latest.Line + 1;
									break;
								default:
									throw new Exception("Expected a KeyValue or an Array.");
							}

							break;
						case '\"':
							_readQuote();

							if (_latest == null)
								throw new Exception("Trying to read a quote without a parent.");

							switch(_latest.ParserType) {
								case ParserTypes.KeyValue:
									((ParserKeyValue)_latest).Value = new ParserString(_word, Line);
									_latest.Length = Line - _latest.Line + 1;
									_latest = _latest.Parent;
									break;
								case ParserTypes.List:
									((ParserList)_latest).AddElement(new ParserString(_word, Line));
									break;
								default:
									throw new Exception("Expected a KeyValue.");
							}

							continue;
						case ',':
						case '\t':
						case ' ':
						case '\r':
							break;
						case '\n':
							Line++;
							break;
						default:
							_readWord();

							if (_word == "") {
								throw new Exception("Null-length word. This is most likely caused by an unexpected character in a string.");
							}

							if (_buffer[_bufferIndex] == ':')
								continue;

							if (_latest != null) {
								switch(_latest.ParserType) {
									case ParserTypes.KeyValue:
										((ParserKeyValue)_latest).Value = new ParserString(_word, Line);
										_latest.Length = Line - _latest.Line + 1;
										_latest = _latest.Parent;
										break;
									case ParserTypes.List:
									case ParserTypes.Aggregate:
										((ParserArrayBase)_latest).AddElement(new ParserString(_word, Line));
										break;
									default:
										// It will be handled by the ':' parsing.
										break;
								}
							}

							continue;
					}

					_bufferIndex++;
				}
			}
			catch (Exception err) {
				throw new Exception("Failed to parse " + _file + " at line " + Line + ", position " + LinePosition, err);
			}
		}

		private void _readWord() {
			StringBuilder word = new StringBuilder();

			while (_isLetter(_buffer[_bufferIndex])) {
				word.Append((char)_buffer[_bufferIndex]);
				_bufferIndex++;
			}

			_word = word.ToString();
		}

		private void _readMultilineQuote() {
			StringBuilder word = new StringBuilder();
			bool trim = true;
			_bufferIndex += 2;

			while (_buffer[_bufferIndex] != '\"' || _buffer[_bufferIndex + 1] != '>') {
				if (_buffer[_bufferIndex] == '\r') {
					_bufferIndex++;
					continue;
				}

				if (_buffer[_bufferIndex] == '\n') {
					_bufferIndex++;
					trim = true;
					Line++;
					word.Append(' ');
					continue;
				}

				if (trim) {
					if (_buffer[_bufferIndex] == ' ' || _buffer[_bufferIndex] == '\t') {
						_bufferIndex++;
						continue;
					}

					if (_buffer[_bufferIndex] == '/' && _buffer[_bufferIndex + 1] == '/') {
						_skipLine();
						continue;
					}

					if (_buffer[_bufferIndex] == '/' && _buffer[_bufferIndex + 1] == '*') {
						_skipCommentBlock();
						continue;
					}
				}

				trim = false;
				word.Append((char)_buffer[_bufferIndex]);
				_bufferIndex++;
			}

			_bufferIndex += 2;

			_word = word.ToString().Trim(' ', '\t');
		}

		private void _readQuote() {
			StringBuilder word = new StringBuilder();
			_bufferIndex++;

			while (_buffer[_bufferIndex] != '\"' || _buffer[_bufferIndex - 1] == '\\') {
				word.Append((char)_buffer[_bufferIndex]);
				_bufferIndex++;
			}

			_bufferIndex++;

			_word = word.ToString();
		}

		private bool _isLetter(byte b) {
			if ((b >= 'a' && b <= 'z') ||
			    (b >= 'A' && b <= 'Z') ||
			    (b >= '0' && b <= '9') ||
			    b == '_' || b == '-' || b == '\'' || b == '.')
				return true;
			return false;
		}

		private void _skipCommentBlock() {
			while (_buffer[_bufferIndex] != '*' || _buffer[_bufferIndex + 1] != '/') {
				if (_buffer[_bufferIndex] == '\n')
					Line++;

				_bufferIndex++;
			}

			_bufferIndex += 2;
		}

		private void _skipLine() {
			while (_buffer[_bufferIndex] != '\n') {
				_bufferIndex++;
			}
		}

		public void Write2(string key, string line) {
			if (!_list) {
				ParserKeyValue conf = _writeKeyValues.FirstOrDefault(p => p.Key == key);

				if (conf == null) {
					// Add a new one!
					_writeKeyValues.Add(new ParserKeyValue(key, Int32.MaxValue) {
						Value = new ParserString(line, Int32.MaxValue),
						Added = true,
						Length = 1
					});

					return;
				}

				// Erase what's there first
				ParserArray array = (ParserArray)conf.Value;

				if (array != null) {
					for (int i = 0; i < array.Length; i++) {
						_allLines[i + array.Line - 1] = null;
					}
				}

				conf.Modified = true;
				conf.Value = new ParserString(line, conf.Value.Line);
			}
			else {
				ParserArray conf = _writeArrays.FirstOrDefault(p => p[_idKey] == key);

				if (conf == null) {
					_writeArrays.Add(new ParserArray(Int32.MaxValue) {
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
					});

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

		public void Write(string key, string line) {
			if (!_list) {
				ParserKeyValue conf = _writeKeyValues.FirstOrDefault(p => p.Key == key);

				if (conf == null) {
					// Add a new one!
					_writeKeyValues.Add(new ParserKeyValue(key, Int32.MaxValue) {
						Value = new ParserString(line, Int32.MaxValue),
						Added = true,
						Length = 1
					});

					return;
				}

				conf.Modified = true;
				conf.Value = new ParserString(line, conf.Value.Line);
			}
			else {
				ParserArray conf = _writeArrays.FirstOrDefault(p => p[_idKey] == key);

				if (conf == null) {
					_writeArrays.Add(new ParserArray(Int32.MaxValue) {
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
					});

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

		public void Delete<TKey>(TKey key) {
			var sKey = key.ToString();

			if (!_list) {
				ParserKeyValue conf = _writeKeyValues.FirstOrDefault(p => p.Key == sKey);

				if (conf != null) {
					for (int i = 0; i < conf.Length; i++) {
						_allLines[i + conf.Line - 1] = null;
					}
				}
			}
			else {
				ParserArray conf = _writeArrays.FirstOrDefault(p => p[_idKey] == sKey);

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
				foreach (var confElement in _writeArrays.Where(p => p.Modified)) {
					for (int i = 0; i < confElement.Length; i++) {
						_allLines[i + confElement.Line - 1] = null;
					}

					_allLines[confElement.Line - 1] = confElement.Objects[1].ObjectValue;
				}

				_writeArrays = _writeArrays.OrderBy(p => Int32.Parse(p[_idKey])).ToList();

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

			File.WriteAllText(path, builder.ToString());
		}

		private int _getInsertIndex(ParserArray confElement) {
			if (_writeArrays.Count == 0) {
				return ((ParserKeyValue)Output).Value.Line + ((ParserKeyValue)Output).Value.Length - 1;
			}

			var last = _writeArrays.LastOrDefault(p => !p.Added);

			if (last == null) {
				return ((ParserKeyValue)Output).Value.Line + ((ParserKeyValue)Output).Value.Length - 1;
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