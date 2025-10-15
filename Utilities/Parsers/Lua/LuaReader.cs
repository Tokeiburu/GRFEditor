using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utilities.Parsers.Lua.Structure;

namespace Utilities.Parsers.Lua {
	public class LuaReader : IDisposable {
		private string _backBuffer;
		private int _backBufferPosition;
		private StreamReader _reader;
		private int _streamLine;

		#region IDisposable

		private bool _disposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~LuaReader() {
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}
			if (disposing) {
				if (_reader != null)
					_reader.Dispose();
			}
			_reader = null;
			_disposed = true;
		}

		#endregion

		public LuaReader(string file, Encoding encoding) {
			_reader = new StreamReader(File.OpenRead(file), encoding);
		}

		public LuaReader(string file) {
			_reader = new StreamReader(File.OpenRead(file), Encoding.GetEncoding(1252));
		}

		public LuaReader(Stream stream, Encoding encoding) {
			_reader = new StreamReader(stream, encoding);
		}

		public LuaReader(Stream stream) {
			_reader = new StreamReader(stream, Encoding.GetEncoding(1252));
		}

		public LuaList ReadAll() {
			_backBufferPosition = -1;

			if (_reader == null)
				throw new NullReferenceException("Stream not opened.");

			LuaList mainList = new LuaList();

			try {
				while (!_reader.EndOfStream) {
					ILuaVariable variable = _getVariable();

					if (variable == null)
						break;


					if (variable is LuaKeyValue keyValue) {
						mainList._keyValues[keyValue.Key] = keyValue.Value;
					}

					mainList.Variables.Add(variable);
				}
			}
			catch (Exception err) {
				throw new Exception("Lua Parsing Exception : \r\n" + err.Message);
			}

			return mainList;
		}

		private LuaList _getList() {
			LuaList list = new LuaList();
			List<ILuaVariable> variables = new List<ILuaVariable>();

			do {
				if (_peekChar() == '}')
					break;

				ILuaVariable variable = _getVariable();
				variables.Add(variable);


				if (variable is LuaKeyValue keyValue) {
					list._keyValues[keyValue.Key] = keyValue.Value;
				}
			} while ((_peekChar() == ',' || _peekChar() == ';') && (_nextChar() == ',' || true));

			list.Variables = variables;
			return list;
		}

		private ILuaVariable _getVariable() {
			char c = _peekChar();

			switch (c) {
				case '*':
					return null;
				case '=':
					_nextChar();
					return _getVariable();
				case ',':
					_nextChar();
					return _getVariable();
				case '{':
					_nextChar();
					LuaList list = _getList();
					if (_nextChar() != '}') {
						throw new Exception("Expected } at line #" + _streamLine);
					}
					return list;
				default:
					string element = _readElement();

					if (element.StartsWith("function(", StringComparison.Ordinal)) {
						LuaFunction function = new LuaFunction();
						function.Name = element;
						function.Value = _getFunction();
						return function;
					}

					if (element.StartsWith("function", StringComparison.Ordinal) && element.Contains("(") && element.Contains(")")) {
						LuaFunction function = new LuaFunction();
						function.Name = "function " + element.Substring("function".Length);
						function.Value = _getFunction();
						return function;
					}

					c = _peekChar();
					switch (c) {
						case '=':
							_nextChar();
							LuaKeyValue keyValue = new LuaKeyValue();
							keyValue.Key = element;
							keyValue.Value = _getVariable();
							return keyValue;
						case ',':
							return new LuaValue { Value = element };
						case '}':
							return new LuaValue { Value = element };
						case ';':
							return new LuaStringValue { Value = element };
						default:
							return new LuaValue { Value = element };
					}
			}
		}

		private string _getFunction() {
			string function = "";
			while (!_reader.EndOfStream) {
				string temp = _reader.ReadLine();
				_streamLine++;
				function += temp;
				if (temp.StartsWith("end", StringComparison.Ordinal))
					break;
				function += "\r\n";
			}
			_backBuffer = null;
			return function;
		}

		private string _readElement() {
			_refreshBuffer();

			//_backBuffer = _backBuffer.Replace("\\\"", "__&quote");
			if (_backBuffer[_backBufferPosition] == '"') {
				string temp = _backBuffer.Substring(_backBufferPosition, _backBuffer.IndexOf('"', _backBufferPosition + 1) - _backBufferPosition + 1);
				_backBufferPosition += temp.Length;
				return temp.Replace("__&quote", "\\\"");
			}
			else {
				int[] indexes = new int[] { _backBuffer.IndexOf(',', _backBufferPosition), _backBuffer.IndexOf('=', _backBufferPosition), _backBuffer.IndexOf('}', _backBufferPosition) }.Where(p => p != -1).ToArray();

				int index = indexes.Length == 0 ? _backBuffer.Length : indexes.Min();

				string temp = _backBuffer.Substring(_backBufferPosition, index - _backBufferPosition);
				_backBufferPosition += temp.Length;
				return temp.Replace("__&quote", "\\\"");
			}
		}

		private char _peekChar() {
			_refreshBuffer();
			return _backBufferPosition == -1 ? '*' : _backBuffer[_backBufferPosition];
		}

		private void _refreshBuffer() {
			if (_backBuffer == null || _backBufferPosition >= _backBuffer.Length) {
				if (!_backBufferRead() && _reader.EndOfStream) {
					_backBufferPosition = -1;
					return;
				}

				_backBufferPosition = 0;
			}
		}

		private char _nextChar() {
			_refreshBuffer();
			return _backBuffer[_backBufferPosition++];
		}

		private bool _backBufferRead() {
			const int linesToReadAtOnce = 1;
			int lineIndex = 0;

			_backBuffer = "";
			string temp;

			if (_reader.EndOfStream)
				return false;

			while (!_reader.EndOfStream && lineIndex < linesToReadAtOnce) {
				temp = _reader.ReadLine();
				_streamLine++;
				lineIndex++;


				if (temp != null) {
					bool sEntered = false;
					bool sBogus;
					StringBuilder nString = new StringBuilder();

					for (int i = 0; i < temp.Length; i++) {
						if (temp[i] == '\"') {
							sEntered = !sEntered;
						}
						else if (temp[i] == '\\') {
							if (i + 1 < temp.Length) {
								if (temp[i + 1] == '\"' && sEntered) {
									nString.Append("__&quote");
								}
								else {
									// Remove bogus quotations and bogus tabs
									sBogus = false;

									for (int j = i; j < temp.Length; j += 2) {
										if (j + 1 < temp.Length) {
											if (temp[j] == '\\' && temp[j + 1] == '\\') {
											}
											else if (temp[j] == '\\' && temp[j + 1] == '\"') {
												sBogus = true;
												nString.Append("__&quote");
												i = j;
												break;
											}
											else
												break;
										}
										else
											break;
									}

									if (!sBogus) {
										nString.Append(temp[i]);
										nString.Append(temp[i + 1]);
									}
								}

								i++;
								continue;
							}
						}

						nString.Append(temp[i]);
					}

					temp = nString.ToString();
				}

				// Block comments force us to read more!
				int indexOfCommentBlock = temp.IndexOf("--[[", 0, StringComparison.Ordinal);
				int indexOfCommentLine = temp.IndexOf("--", 0, StringComparison.Ordinal);
				int indexOfQuote = temp.IndexOf('\"');

				if (indexOfQuote != -1 && indexOfCommentLine > indexOfQuote) {
					// Possible comment inside a string
					int indexOfSecondQuote = temp.IndexOf("\"", indexOfQuote + 1, StringComparison.Ordinal);
					if (indexOfSecondQuote > indexOfCommentLine) {
						// The comment was inside the string
						_backBuffer += temp;
					}
					else {
						// The comment was outsie the string, removing it
						temp = temp.Substring(0, indexOfCommentLine);
						_backBuffer += temp;
					}
				}
				else if (indexOfCommentBlock == -1) {
					// No comment block
					if (indexOfCommentLine != -1) {
						temp = temp.Substring(0, indexOfCommentLine);
					}
					_backBuffer += temp;
				}
				else {
					// We have a possible comment line that goes before the comment block
					if (indexOfCommentLine < indexOfCommentBlock) {
						// We have a comment line after all
						temp = temp.Substring(0, indexOfCommentLine);
						_backBuffer += temp;
					}
					else {
						// We have a comment block, find the end!
						temp = temp.Substring(0, indexOfCommentLine);
						_backBuffer += temp;

						while (!_reader.EndOfStream) {
							temp = _reader.ReadLine();
							_streamLine++;

							indexOfCommentBlock = temp.IndexOf("]]", 0, StringComparison.Ordinal);

							if (indexOfCommentBlock != -1) {
								// Found the end
								temp = temp.Substring(indexOfCommentBlock + 2, temp.Length - indexOfCommentBlock - 2);
								_backBuffer += temp;
								break;
							}
						}
					}
				}
			}

			temp = "";

			if (_backBuffer == null)
				return false;

			int stringStart;
			int stringEnd = -1;

			while (true) {
				stringStart = _backBuffer.IndexOf('"', stringEnd + 1);

				if (stringStart == -1) {
					temp += _backBuffer.Substring(stringEnd + 1, _backBuffer.Length - stringEnd - 1).Replace(" ", "").Replace("\t", "");
					break;
				}

				temp += _backBuffer.Substring(stringEnd + 1, stringStart - stringEnd - 1).Replace(" ", "").Replace("\t", "");

				stringEnd = _backBuffer.IndexOf('"', stringStart + 1);

				if (stringEnd == -1)
					throw new Exception("End of string not found at line #" + _streamLine);

				temp += _backBuffer.Substring(stringStart, stringEnd - stringStart + 1);
			}

			_backBuffer = temp;

			if (_backBuffer == "")
				return _backBufferRead();

			return true;
		}
	}
}