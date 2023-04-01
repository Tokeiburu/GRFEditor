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
				if (lines[i].Contains(toFind))
					return i;
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

		public static int FindNextEndsWith(List<string> lines, int indexStart, int ifIndent, string toFind) {
			for (int i = indexStart; i < lines.Count; i++) {
				if (lines[i].EndsWith(toFind)) {
					int indent = GetIndent(lines[i]);
					if (indent == ifIndent)
						return i;
				}
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
		/// Determines whether the specified line ends with the specified value.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="value">The value.</param>
		/// <returns>
		///   <c>true</c> if the specified line ends with the value; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsEnd(string line, string value) {
			return line.EndsWith(value);
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

		/// <summary>
		/// Removes a specified amount of indentation.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="toRemove">The amount of indent to remove per line.</param>
		/// <returns></returns>
		public static string RemoveIndent(string line, int toRemove) {
			int indent = GetIndent(line);

			if (indent == 0)
				return line;

			return line.Replace(GenerateIndent(indent), GenerateIndent(indent - toRemove));
		}

		public static string AddIndent(string line, int toAdd) {
			int indent = GetIndent(line);
			return line.Replace(GenerateIndent(indent), GenerateIndent(indent + toAdd));
		}

		public static string GenerateIndent(int indent) {
			if (indent > 0) {
				string toRet = "";

				for (int i = 0; i < indent; i++)
					toRet += "\t";

				return toRet;
			}

			return "";
		}

		public static void CleanUpLines(List<string> lines) {
			for (int i = 0; i < lines.Count; i++) {
				if (IsEmpty(lines[i])) {
					lines.RemoveAt(i);
					i--;
				}
				else if (lines[i].Contains(" == true")) {
					lines[i] = lines[i].Replace(" == true", "");
				}
				//else if (i + 1 < lines.Count && 
				//    lines[i].EndsWith("{") &&
				//    lines[i + 1].StartsWith("}")) {
				//    lines[i] = lines[i] + "}";
				//    lines.RemoveAt(i + 1);
				//    i--;
				//}
				//else if (i + 1 < lines.Count &&
				//    lines[i].EndsWith("\telse") &&
				//    lines[i + 1].EndsWith("\tend")) {
				//    lines.RemoveAt(i);
				//    i--;
				//}
			}

			//for (int i = 0; i < lines.Count; i++) {
			//    if (lines[i].Contains(" == true")) {
			//        lines[i] = lines[i].Replace(" == true", "");
			//    }
			//}

			if (lines.Count > 1) {
				if (GetIndent(lines[lines.Count - 2]) == 0 &&
				    lines[lines.Count - 1] == "return") {
					lines.RemoveAt(lines.Count - 1);
				}
			}

			// Removes useless else statements
			if (lines.Count > 4) {
				if (lines[lines.Count - 4] == "\telse" &&
				    lines[lines.Count - 3].StartsWith("\t\treturn") &&
				    lines[lines.Count - 2] == "\tend" &&
				    lines[lines.Count - 1] == "end") {
					lines[lines.Count - 4] = "\tend";
					lines[lines.Count - 3] = lines[lines.Count - 3].ReplaceOnce("\t\t", "\t");
					lines.RemoveAt(lines.Count - 2);
				}
			}

			if (lines.Count > 2) {
				if (GetIndent(lines[lines.Count - 3]) == 1 &&
				    lines[lines.Count - 2] == "\treturn") {
					lines.RemoveAt(lines.Count - 2);
				}
			}
		}

		public static string GetLabelFromGoto(string line) {
			return NoIndent(line).Replace("goto ", "");
		}
	}
}