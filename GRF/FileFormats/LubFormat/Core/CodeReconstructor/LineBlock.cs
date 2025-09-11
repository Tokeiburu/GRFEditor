using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor {
	public enum LuaToken {
		If,
		Else,
		Loop,
		End,
		While,
		Return,
		Execution,
		IfGoto,
		ElseGoto,
		LoopGoto,
	}

	public class LineBlock {
		private readonly List<string> _lines = new List<string>();

		public int _originalLineIndexStart { get; set; }
		public int _originalLineIndexEnd { get; set; }

		public bool IsRoot { get; set; }
		public string Label { get; set; }

		public int TotalLinesIncludingLabel {
			get { return _lines.Count; }
		}

		public int TotalLinesExcludingLabel {
			get { return IsRoot ? _lines.Count : _lines.Count - 1; }
		}

		public int PrintLineStart {
			get { return IsRoot ? 0 : 1; }
		}

		public List<string> Lines {
			get { return _lines; }
		}

		public LineBlock(IEnumerable<string> lines, int lineStart, int lineEnd) {
			_originalLineIndexStart = lineStart;
			_originalLineIndexEnd = lineEnd;

			_lines = lines.Skip(_originalLineIndexStart).Take(_originalLineIndexEnd - _originalLineIndexStart + 1).ToList();
			IsRoot = lineStart == 0;
			Label = GetLabel();
		}

		public string GetLabel() {
			return IsRoot ? "root" : _lines[0].Replace("::", "");
		}

		public int GetLabelId() {
			return IsRoot ? 0 : Int32.Parse(Label.Split('[', ']')[1].Split('.')[0]);
		}

		public bool LastLineIsReturn() {
			return TotalLinesExcludingLabel > 0 && LineHelper.IsStart(_lines.Last(), "return");
		}

		public string IfLineCondition {
			get { return _lines[IndexOf(LuaToken.If)].TrimStart('\t').ReplaceFirst("if ", "").Replace(" then", ""); }
			set {
				var ifIndex = IndexOf(LuaToken.If);
				_lines[ifIndex] = LineHelper.GenerateIndent(LineHelper.GetIndent(_lines[ifIndex])) + "if " + value + " then";
			}
		}

		private List<string> _getContentLines() {
			return _lines.Skip(PrintLineStart).ToList();
		}

		public void ReplaceElseContent(LineBlock content) {
			List<string> linesToAppend = content._getContentLines();
			linesToAppend.RemoveAt(0);

			if (content.IndexOf(LuaToken.Else, 0) > -1) {
				linesToAppend.RemoveAt(linesToAppend.Count - 1);
			}
			else {
				_lines.RemoveAt(_lines.Count - 1);
			}

			int location = IndexOf(LuaToken.Else) + 1;
			_lines.RemoveAt(location);
			_lines.InsertRange(location, linesToAppend);
		}

		public int GetGotoLineIndex(CodeFragment fragment) {
			return LineHelper.GetLineIndexContains(_lines, "goto " + fragment.Content.Label, 0);
		}

		public int GetGotoLineIndex(string label) {
			return LineHelper.GetLineIndexContains(_lines, "goto " + label, 0);
		}

		public void RemoveExecutionLine() {
			if (LineHelper.IsStart(_lines[_lines.Count - 1], "goto ")) {
				_lines.RemoveAt(_lines.Count - 1);
			}
		}

		public void RemoveReturnLine() {
			if (LineHelper.IsStart(_lines[_lines.Count - 1], "return")) {
				_lines.RemoveAt(_lines.Count - 1);
			}
		}

		public override string ToString() {
			return Methods.Aggregate(_lines, "\r\n");
		}

		public bool ContainsLineIndex(int line) {
			return _originalLineIndexStart <= line && line <= _originalLineIndexEnd;
		}

		public int IndexOf(LuaToken token, int startIndex = 0) {
			string[] search = new string[2];
			int toAdd = 0;

			switch(token) {
				case LuaToken.ElseGoto:
					toAdd = 1;
					search[0] = "else";
					return LineHelper.GetLineIndexEndsWith(_lines, search[0], startIndex) + toAdd;
				case LuaToken.Else:
					search[0] = "else";
					return LineHelper.GetLineIndexEndsWith(_lines, search[0], startIndex) + toAdd;
				case LuaToken.IfGoto:
					toAdd = 1;
					search[0] = "if ";
					break;
				case LuaToken.If:
					search[0] = "if ";
					break;
				case LuaToken.End:
					search[0] = "end";
					break;
				case LuaToken.Loop:
					search[0] = "for";
					search[1] = "while";
					break;
				case LuaToken.LoopGoto:
					toAdd = 1;
					search[0] = "for";
					search[1] = "while";
					break;
				case LuaToken.While:
					search[0] = "while";
					break;
				case LuaToken.Execution:
					return _lines.Count - 1;
				default:
					throw new Exception("Unknown index");
			}

			for (int i = 0; i < search.Length && search[i] != null; i++) {
				var r = LineHelper.GetLineIndexContains(_lines, search[i], startIndex) + toAdd;

				if (r > -1)
					return r;
			}

			return -1;
		}

		public bool IsIf() {
			int index;
			if ((index = IndexOf(LuaToken.If)) < 0) return false;
			if ((index = IndexOf(LuaToken.End, index + 1)) < 0) return false;
			return true;
		}

		public bool IsIfElse() {
			int index;
			if ((index = IndexOf(LuaToken.If)) < 0) return false;
			if ((index = IndexOf(LuaToken.Else, index + 1)) < 0) return false;
			if ((index = IndexOf(LuaToken.End, index + 1)) < 0) return false;
			if (index != _lines.Count - 1) return false;
			return true;
		}

		public bool IsLoop() {
			var index = IndexOf(LuaToken.Loop);
			return index == 1 || index == 2;
		}

		public void SwapIfElseLines() {
			int indexIf = IndexOf(LuaToken.IfGoto);
			int indexElse = IndexOf(LuaToken.ElseGoto, indexIf + 1);

			if (indexIf == -1 || indexElse == -1)
				throw new Exception("InvertIfElse failed: if or else index was not found.");

			LineHelper.Swap(_lines, indexIf, indexElse);
		}

		public string GetGotoLabel(LuaToken token) {
			int index = IndexOf(token);

			if (index < 0)
				return null;

			return LineHelper.NoIndent(_lines[index].Replace("goto ", ""));
		}

		public void RemoveLine(LuaToken token) {
			int index = IndexOf(token, 0);

			if (index > -1)
				_lines.RemoveAt(index);
		}

		public void RepaceGotoLine(CodeFragment oldFragment, CodeFragment newFragment) {
			int location = GetGotoLineIndex(oldFragment);
			_lines[location] = LineHelper.ReplaceAfterIndent(_lines[location], "goto " + newFragment.Content.Label);
		}

		public void RepaceGotoLine(LuaToken token, CodeFragment fragment) {
			int index = IndexOf(token);
			_lines[index] = LineHelper.ReplaceAfterIndent(_lines[index], "goto " + fragment.Content.Label);
		}

		public void RemoveElseLines() {
			int indexIf = IndexOf(LuaToken.If);
			int indexElse = IndexOf(LuaToken.Else, indexIf + 1);
			int indexEnd = IndexOf(LuaToken.End, indexElse);

			_lines.RemoveRange(indexElse, indexEnd - indexElse);
		}

		public void AppendGoto(CodeFragment fragment) {
			if (TotalLinesExcludingLabel == 0) {
				_lines.Add(LineHelper.GenerateIndent(1) + "goto " + fragment.Content.Label);
			}
			else if (TotalLinesExcludingLabel > 0 && !LineHelper.IsStart(_lines[_lines.Count - 1], "goto ")) {
				if (_lines.Count > 1)
					_lines.Add(LineHelper.GenerateIndent(LineHelper.GetIndent(_lines[1])) + "goto " + fragment.Content.Label);
				else
					_lines.Add(LineHelper.GenerateIndent(1) + "goto " + fragment.Content.Label);
			}
			else throw new Exception("Appending a goto without removing the previous one first.");
		}

		public void RepaceGotoWithBreak(CodeFragment fragment) {
			int index = GetGotoLineIndex(fragment);

			if (index > -1)
				_lines[index] = LineHelper.ReplaceAfterIndent(_lines[index], "break");
		}
	}
}