using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.LubFormat.Core.CodeReconstructor;

namespace GRF.FileFormats.LubFormat.Core {
	public static class CodeLogic {
		public static List<CodeFragment> Analyse(List<string> lines, int level) {
			RemoveConsecutiveGotos(lines);

			List<CodeFragment> fragments = new List<CodeFragment>();
			CodeReconstructorCommon common = new CodeReconstructorCommon();

			// The first iteration creates the fragments, the references
			// aren't set up yet.
			for (int i = 0; i < lines.Count; i++) {
				if (lines[i].StartsWith("::") || i == 0) {
					int fragmentEnd = lines.Count - 2;
					int fragmentStart = i;

					for (i = i + 1; i < lines.Count; i++) {
						if (lines[i].StartsWith("::")) {
							fragmentEnd = i - 1;
							i--;
							break;
						}
					}

					CodeFragment fragment = new CodeFragment(fragmentStart, fragmentEnd, lines, common);

					if (level == 0)
						fragment.FragmentType = FragmentType.NormalExecution;

					fragments.Add(fragment);
				}
			}

			fragments.ForEach(p => p.SetReferences(fragments));

			return fragments;
		}

		//public static List<CodeFragment> Analyse2(List<string> lines, int level) {
		//	RemoveConsecutiveGotos(lines);
		//
		//	CodeReconstructorCommon common = new CodeReconstructorCommon();
		//	var fragments = new List<CodeFragment>();
		//
		//	// The first iteration creates the fragments, the references
		//	// aren't set up yet.
		//	for (int i = 0; i < lines.Count; i++) {
		//		if (lines[i].StartsWith("::") || i == 0) {
		//			int fragmentEnd = lines.Count - 2;
		//			int fragmentStart = i;
		//
		//			for (i = i + 1; i < lines.Count; i++) {
		//				if (lines[i].StartsWith("::")) {
		//					fragmentEnd = i - 1;
		//					i--;
		//					break;
		//				}
		//			}
		//
		//			CodeFragment fragment = new CodeFragment(fragmentStart, fragmentEnd, lines, common);
		//
		//			if (level == 0)
		//				fragment.FragmentType = AdvFragmentType.NormalExecution;
		//
		//			fragments.Add(fragment);
		//		}
		//	}
		//
		//	fragments.ForEach(p => p.SetReferences(fragments));
		//
		//	return fragments;
		//}

		public static int NumberOfConditions(string line) {
			return
				_count(line, " == true") +
				_count(line, " >= ") +
				_count(line, " <= ") +
				_count(line, " == ") +
				_count(line, " ~= ") +
				_count(line, " < ") +
				_count(line, " > ") +
				_count(line, " and ") +
				_count(line, " or ") +
				_count(line, " == false");
		}

		private static int _count(string line, string value) {
			return (line.Length - line.Replace(value, "").Length) / value.Length;
		}

		public static string MergeOr(string line1, string line2) {
			string first;
			string second;

			if (NumberOfConditions(line1) == 1) {
				first = LineHelper.NoIndent(line1).Replace("if ", "").Replace(" then", "");
			}
			else {
				first = "(" + LineHelper.NoIndent(line1).Replace("if ", "").Replace(" then", "") + ")";
			}

			if (NumberOfConditions(line2) == 1) {
				second = LineHelper.NoIndent(line2).Replace("if ", "").Replace(" then", "");
			}
			else {
				second = "(" + LineHelper.NoIndent(line2).Replace("if ", "").Replace(" then", "") + ")";
			}

			return LineHelper.GenerateIndent(LineHelper.GetIndent(line1)) + "if " + first + " or " + second + " then";
		}

		public static string MergeAnd(string line1, string line2) {
			string first;
			string second;

			if (NumberOfConditions(line1) == 1) {
				first = LineHelper.NoIndent(line1).Replace("if ", "").Replace(" then", "");
			}
			else {
				first = "(" + LineHelper.NoIndent(line1).Replace("if ", "").Replace(" then", "") + ")";
			}

			if (NumberOfConditions(line2) == 1) {
				second = LineHelper.NoIndent(line2).Replace("if ", "").Replace(" then", "");
			}
			else {
				second = "(" + LineHelper.NoIndent(line2).Replace("if ", "").Replace(" then", "") + ")";
			}

			return LineHelper.GenerateIndent(LineHelper.GetIndent(line1)) + "if " + first + " and " + second + " then";
		}

		public static string ChangeToWhile(string ifCondition) {
			return ifCondition.Replace("if ", "while ").Replace(" then", " do");
		}

		public static string ToElseIf(string elseCondition, string ifCondition) {
			return LineHelper.ReplaceAfterIndent(elseCondition, LineHelper.NoIndent(ifCondition).Replace("if ", "elseif "));
		}

		public static string ReverseCondition(string line) {
			if (NumberOfConditions(line) == 1) {
				if (line.Contains(" == false")) {
					return line.Replace(" == false", "");
				}

				if (line.Contains(" == true")) {
					return line.Replace(" == true", ")").Replace("if ", "if not (");
				}

				if (line.Contains(" == ")) {
					return line.Replace(" == ", " ~= ");
				}

				if (line.Contains(" ~= ")) {
					return line.Replace(" ~= ", " == ");
				}

				if (line.Contains(" <= ")) {
					return line.Replace(" <= ", " > ");
				}

				if (line.Contains(" >= ")) {
					return line.Replace(" >= ", " < ");
				}

				if (line.Contains(" < ")) {
					return line.Replace(" < ", " >= ");
				}

				if (line.Contains(" > ")) {
					return line.Replace(" > ", " <= ");
				}
			}

			return line.Replace("if ", "if not(").Replace(" then", ") then");
		}

		public static void RemoveConsecutiveGotos(List<string> lines) {
			for (int i = 0; i < lines.Count; i++) {
				if (i + 1 < lines.Count &&
				    LineHelper.IsStart(lines[i], "goto ") &&
				    LineHelper.IsStart(lines[i + 1], "goto ")
					) {
					lines.RemoveAt(i + 1);
					i--;
				}
			}
		}

		#region Utility methods

		public static CodeFragment GetFragment(IEnumerable<CodeFragment> fragments, int line) {
			return fragments.FirstOrDefault(p => p.ContainsLine(line));
		}

		public static CodeFragment GetFragment(IEnumerable<CodeFragment> fragments, string label) {
			return fragments.FirstOrDefault(p => p.Label == label);
		}

		public static bool IsIfElseBranch(CodeFragment fragment) {
			int indexIf = _ifBranchIndex(fragment, 0);
			if (indexIf < 0) return false;
			int indexElse = _elseBranchIndex(fragment, indexIf + 1);
			if (indexElse < 0) return false;
			int indexEnd = _endBranchIndex(fragment, indexElse + 1);
			if (indexEnd < 0) return false;
			if (indexEnd != fragment.Lines.Count - 1) return false;
			return true;
		}

		public static bool IsIfBranch(CodeFragment fragment) {
			int indexIf = _ifBranchIndex(fragment, 0);
			if (indexIf < 0) return false;
			int indexEnd = _endBranchIndex(fragment, indexIf + 1);
			if (indexEnd < 0) return false;
			return true;
		}

		private static int _ifBranchIndex(CodeFragment fragment, int startIndex) {
			return LineHelper.GetLineIndexContains(fragment.Lines, "if ", startIndex);
		}

		private static int _whileBranchIndex(CodeFragment fragment, int startIndex) {
			return LineHelper.GetLineIndexContains(fragment.Lines, "while ", startIndex);
		}

		private static int _elseBranchIndex(CodeFragment fragment, int startIndex) {
			return LineHelper.GetLineIndexEndsWith(fragment.Lines, "\telse", startIndex);
		}

		private static int _endBranchIndex(CodeFragment fragment, int startIndex) {
			return LineHelper.GetLineIndexContains(fragment.Lines, "end", startIndex);
		}

		public static bool IsForLoopBranch(CodeFragment fragment) {
			return fragment.Lines.Count > 2 && LineHelper.IsStart(fragment.Lines[1], "for ");
		}

		#endregion
	}
}