using System.Collections.Generic;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat {
	/// <summary>
	/// Helping class for the CodeAnalyser.
	/// This class checks for various properties on lines.
	/// </summary>
	public static class LineHelper {
		/// <summary>
		/// Determines whether the specified line is an if line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns>
		///   <c>true</c> if the specified line is if; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsIf(string line) {
			return IsStart(line, "if ");
		}

		public static bool IsControl(string line) {
			var l = NoIndent(line);
			return l.StartsWith("if ") || l.StartsWith("for ") || l.StartsWith("while ");
		}

		/// <summary>
		/// Removes the indent on the line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns>A line without indent</returns>
		public static string NoIndent(string line) {
			return line.TrimStart('\t');
		}

		/// <summary>
		/// Removes everything after the indent and puts the new value.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="newValue">The new value.</param>
		/// <returns></returns>
		public static string ReplaceAfterIndent(string line, string newValue) {
			int numOfTabs = line.Length - NoIndent(line).Length;
			return line.Remove(numOfTabs) + newValue;
		}

		/// <summary>
		/// Gets the indent of a line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns>The line indent.</returns>
		public static int GetIndent(string line) {
			int indent = 0;

			for (int j = 0; j < line.Length; j++) {
				if (line[j] == '\t')
					indent++;
				else
					break;
			}

			return indent;
		}

		public static int GetLineIndexContains(List<string> lines, string toFind, int startIndex) {
			for (int i = startIndex; i < lines.Count; i++) {
				if (lines[i].Contains(" function(")) {
					int end = 1;
					i++;

					for (; i < lines.Count; i++) {
						if (lines[i].Contains("\tend") || lines[i] == "end")
							end--;
						else if (
							lines[i].Contains("\tfor ") ||
							lines[i].Contains("\tif ") ||
							lines[i].Contains("\twhile "))
							end++;

						if (end <= 0)
							break;
					}
				}

				for (int j = 0, k = 0; j < lines[i].Length && k < toFind.Length; j++) {
					if (lines[i][j] == '\t')
						continue;
					if (lines[i][j] == toFind[k++]) {
						if (k == toFind.Length)
							return i;
						continue;
					}
					break;
				}
			}

			return -1;
		}

		public static int GetLineIndexEndsWith(List<string> lines, string toFind, int startIndex) {
			for (int i = startIndex; i < lines.Count; i++) {
				if (lines[i].EndsWith(toFind))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Determines whether the specified line is empty.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns>
		///   <c>true</c> if the specified line is empty; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsEmpty(string line) {
			return line == "" || line.EndsWith("\t");
		}

		/// <summary>
		/// Determines whether the specified line starts with the specified value (ignores indent).
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="value">The value to find.</param>
		/// <returns>
		///   <c>true</c> if the specified line starts with the value; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsStart(string line, string value) {
			return NoIndent(line).StartsWith(value);
		}

		/// <summary>
		/// Swaps the specified lines.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		public static void Swap(List<string> lines, int from, int to) {
			string old = lines[@from];
			lines[@from] = lines[to];
			lines[to] = old;
		}

		public static List<string> FixIndent(List<string> replacedLines, int codeIndent) {
			int gotoIndent = GetIndent(replacedLines[0]);
			string toReplaceFrom = "";
			string toReplaceTo = "";

			int toAdd = codeIndent - gotoIndent;

			if (toAdd != 0) {
				if (toAdd > 0) {
					for (int i = 0; i < toAdd; i++)
						toReplaceTo += "\t";
				}
				else {
					toAdd = -1 * toAdd;

					for (int i = 0; i < toAdd; i++)
						toReplaceFrom += "\t";
				}

				for (int i = 0; i < replacedLines.Count; i++) {
					replacedLines[i] = replacedLines[i].ReplaceOnce(toReplaceFrom, toReplaceTo);
				}
			}

			return replacedLines;
		}

		private static readonly Dictionary<int, string> _indents = new Dictionary<int, string>();

		public static string GenerateIndent(int indent) {
			if (indent > 0) {
				string toRet = "";

				if (_indents.TryGetValue(indent, out toRet)) {
					return toRet;
				}

				for (int i = 0; i < indent; i++)
					toRet += "\t";

				_indents[indent] = toRet;
				return toRet;
			}

			return "";
		}

		public static string GetLabelFromGoto(string line) {
			return NoIndent(line).Replace("goto ", "");
		}
	}
}