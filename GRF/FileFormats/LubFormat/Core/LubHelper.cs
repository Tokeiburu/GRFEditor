using System;
using System.Linq;
using System.Text;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Core {
	public static class LubHelper {
		public static string Escape(string str) {
			StringBuilder builder = new StringBuilder();
			char c;

			for (int i = 0; i < str.Length; i++) {
				c = str[i];

				if (c <= 124) {
					switch (c) {
						case '\t':
							builder.Append(@"\t");
							break;
						case '\n':
							builder.Append(@"\n");
							break;
						case '\f':
							builder.Append(@"\f");
							break;
						case '\r':
							builder.Append(@"\r");
							break;
						case '\\':
							builder.Append(@"\\");
							break;
						case '\"':
							builder.Append("\\\"");
							break;
						default:
							builder.Append(c);
							break;
					}
				}
				else {
					builder.Append(c);
				}
			}
			return builder.ToString();
		}

		public static string EscapeIgnoreLineFeed(string str) {
			StringBuilder builder = new StringBuilder();
			char c;

			for (int i = 0; i < str.Length; i++) {
				c = str[i];

				if (c <= 124) {
					switch (c) {
						case '\t':
							builder.Append(@"\t");
							break;
						case '\f':
							builder.Append(@"\f");
							break;
						case '\\':
							builder.Append(@"\\");
							break;
						case '\"':
							builder.Append("\\\"");
							break;
						default:
							builder.Append(c);
							break;
					}
				}
				else {
					builder.Append(c);
				}
			}
			return builder.ToString();
		}

		public static bool ContainsEscapeChar(string str) {
			StringBuilder builder = new StringBuilder();
			char c;

			for (int i = 0; i < str.Length; i++) {
				c = str[i];

				if (c <= 124) {
					switch (c) {
						case '\t':
						case '\f':
						case '\\':
						case '\"':
							return true;
						default:
							builder.Append(c);
							break;
					}
				}
			}
			return false;
		}

		public static string Format(string source, int indent = 0) {
			StringBuilder builder = new StringBuilder();

			string[] lines = source.ReplaceAll("\r\n", "\n").Split(new char[] { '\n' });

			foreach (string lineRead in lines) {
				string line = lineRead;
				line = line.TrimStart('\t', ' ');

				indent = _detectPreIndent(line, indent);

				builder.AppendIndent(indent);
				builder.AppendLine(line);

				indent = _detectPostIndent(line, indent);
			}

			return builder.ToString();
		}

		private static int _detectPreIndent(string line, int indent) {
			int bracketClose = line.Count(p => p == '}');
			int bracketOpen = line.Count(p => p == '{');

			if (line == "end")
				indent--;
			else if (line.StartsWith("elseif ", StringComparison.Ordinal))
				indent--;
			else if (line == "else")
				indent--;
			else if (bracketClose > bracketOpen)
				indent -= bracketClose - bracketOpen;

			return indent < 0 ? 0 : indent;
		}

		private static int _detectPostIndent(string line, int indent) {
			int bracketClose = line.Count(p => p == '}');
			int bracketOpen = line.Count(p => p == '{');

			if (line.EndsWith(" then", StringComparison.Ordinal))
				indent++;
			else if (line.EndsWith(" do", StringComparison.Ordinal))
				indent++;
			else if (line.IndexOf("function(", StringComparison.Ordinal) > -1)
				indent++;
			else if (line.StartsWith("function ", StringComparison.Ordinal))
				indent++;
			else if (line == "else")
				indent++;
			else if (bracketOpen > bracketClose)
				indent += bracketOpen - bracketClose;

			return indent < 0 ? 0 : indent;
		}
	}
}