using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor {
	public class CodeFragment {
		public List<CodeFragment> ChildReferences = new List<CodeFragment>();
		public List<CodeFragment> ParentReferences = new List<CodeFragment>();
		public CodeFragment Break;
		public CodeReconstructorCommon Common = new CodeReconstructorCommon();
		private bool? _leads;

		public int Uid {
			get;
			private set;
		}

		private static int _uid;

		public CodeFragment(int lineStart, int lineEnd, IEnumerable<string> lines, CodeReconstructorCommon common) {
			_originalLineIndexStart = lineStart;
			_originalLineIndexEnd = lineEnd;
			Common = common;

			_setLines(lines);
			IsRoot = lineStart == 0;
			Label = IsRoot ? "root" : Lines[0].Replace("::", "");
			Common.Fragments[Label] = this;
			Uid = ++_uid;
		}

		#region Getter and setters

		public int TotalLinesIncludingLabel {
			get { return Lines.Count; }
		}

		public int TotalLinesExcludingLabel {
			get { return IsRoot ? Lines.Count : Lines.Count - 1; }
		}

		public bool IsPure { get; set; }
		public bool IsRoot { get; set; }

		public CodeFragment Else {
			get { return _elseReference(); }
		}

		public CodeFragment If {
			get { return _ifReference(); }
		}

		public CodeFragment Execution {
			get { return _executionReference(); }
		}

		public bool IsReturn {
			get { return ChildReferences.Count == 0 && TotalLinesExcludingLabel > 0 && LineHelper.IsStart(Lines[Lines.Count - 1], "return"); }
		}

		public string Label { get; set; }
		public List<string> Lines { get; set; }
		public FragmentType FragmentType { get; set; }

		private string _ifCondition {
			get { return Lines[_ifBranchIndex(0)]; }
			set { Lines[_ifBranchIndex(0)] = value; }
		}

		private string _elseCondition {
			get { return Lines[_elseBranchIndex(0)]; }
			set { Lines[_elseBranchIndex(0)] = value; }
		}

		// The line indexes are irrevelant, the items will be printed
		// (this avoids a LOT of issues...)
		private int _originalLineIndexStart { get; set; }
		private int _originalLineIndexEnd { get; set; }

		private int _printLineStart {
			get { return IsRoot ? 0 : 1; }
		}

		#endregion

		public override string ToString() {
			return Methods.Aggregate(Lines, "\r\n");
		}

		public bool ContainsLine(int line) {
			return _originalLineIndexStart <= line && line <= _originalLineIndexEnd;
		}

		public void RemoveEmpty() {
			if (TotalLinesExcludingLabel == 0 && ChildReferences.Count == 1) {
				for (int i = 0; i < ParentReferences.Count; i++) {
					ParentReferences[i]._changeGoto(this, ChildReferences[0]);
				}
			}
			else if (TotalLinesExcludingLabel == 1 && ChildReferences.Count == 1) {
				for (int i = 0; i < ParentReferences.Count; i++) {
					ParentReferences[i]._changeGoto(this, ChildReferences[0]);
				}
			}
		}

		#region Initialisation

		private void _setLines(IEnumerable<string> lines) {
			Lines = lines.Skip(_originalLineIndexStart).Take(_originalLineIndexEnd - _originalLineIndexStart + 1).ToList();

			_detectFragmentType();
		}

		private void _detectFragmentType() {
			FragmentType = FragmentType.NormalExecution;

			if (CodeLogic.IsIfElseBranch(this)) {
				int indexIf = _ifBranchIndex(0);
				int indexEnd = _endBranchIndex(1);

				FragmentType = FragmentType.IfElse;

				if (indexIf == _printLineStart && indexEnd == Lines.Count - 1) {
					IsPure = true;
				}
				else if (indexEnd != Lines.Count - 1) {
					FragmentType = FragmentType.NormalExecution;
				}
			}
			else if (CodeLogic.IsIfBranch(this)) {
				int indexIf = _ifBranchIndex(0);
				int indexEnd = _endBranchIndex(1);

				FragmentType = FragmentType.If;

				if (indexIf == _printLineStart && indexEnd == Lines.Count - 1) {
					IsPure = true;
				}
			}
			else if (CodeLogic.IsForLoopBranch(this)) {
				FragmentType = FragmentType.ForLoop;
			}

			if (IsReturn) {
				if (LineHelper.NoIndent(Lines[Lines.Count - 1]) == "return") {
					IsPure = true;
				}
			}
		}

		#endregion

		#region References

		public void SetReferences(List<CodeFragment> fragments) {
			List<string> tableLines = new List<string>();

			for (int i = 0; i < Lines.Count; i++) {
				// This is a fail table association, reset it
				if (LineHelper.IsStart(Lines[i], "{}.")) {
					tableLines.Add(LineHelper.NoIndent(Lines[i]));
					continue;
				}

				if (tableLines.Count > 0) {
					if (LineHelper.IsStart(Lines[i], "local")) {
						Lines[i] = Lines[i].Replace("{}", "{ " + Methods.Aggregate(tableLines.Select(p => p.Replace("{}.", "")).ToList(), ", ") + " }");
						Lines.RemoveRange(i - tableLines.Count, tableLines.Count);
						i -= tableLines.Count;
					}

					tableLines.Clear();
				}

				if (LineHelper.IsStart(Lines[i], "goto ")) {
					string label = LineHelper.GetLabelFromGoto(Lines[i]);
					CodeFragment linkedFragment = Common.Fragments[label];

					if (linkedFragment != null) {
						linkedFragment.AddParentReference(this);
						AddChildReference(linkedFragment);
					}
				}
			}

			string currentLine;

			for (int i = 1; i < Lines.Count; i++) {
				currentLine = LineHelper.NoIndent(Lines[i]);

				if (currentLine.StartsWith("if "))
					break;

				if (currentLine.StartsWith("return"))
					break;

				if (currentLine.StartsWith("goto "))
					break;

				if (i == Lines.Count - 1) {
					CodeFragment linkedFragment = CodeLogic.GetFragment(fragments, _originalLineIndexEnd + 1);

					if (linkedFragment != null) {
						linkedFragment.AddParentReference(this);
						_appendGoto(linkedFragment);
						AddChildReference(linkedFragment);
					}
				}
			}

			if (Lines.Count == 1) {
				// This reference should not be there at all, this is just a bug fix
				CodeFragment linkedFragment = CodeLogic.GetFragment(fragments, _originalLineIndexEnd + 1);

				if (linkedFragment != null) {
					linkedFragment.AddParentReference(this);
					AddChildReference(linkedFragment);
				}
			}

			if (FragmentType == FragmentType.ForLoop) {
				Break = ChildReferences[1];
			}
		}

		private void AddParentReference(CodeFragment fragment) {
			if (!ParentReferences.Contains(fragment)) {
				ParentReferences.Add(fragment);
			}
		}

		private void AddChildReference(IEnumerable<CodeFragment> fragments) {
			foreach (CodeFragment fragment in fragments) {
				AddChildReference(fragment);
			}
		}

		private void AddChildReference(CodeFragment fragment) {
			if (!ChildReferences.Contains(fragment))
				ChildReferences.Add(fragment);
		}

		public void RemoveChildReference(CodeFragment codeFragment) {
			ChildReferences.Remove(codeFragment);
			codeFragment.RemoveParentReference(this);
		}

		public void RemoveParentReference(CodeFragment codeFragment) {
			ParentReferences.Remove(codeFragment);
		}

		#endregion

		#region Utility methods

		public void Print(StringBuilder builder, int level, Dictionary<CodeFragment, int> printedFragments = null) {
			if (printedFragments == null)
				printedFragments = new Dictionary<CodeFragment, int>();

			if (printedFragments.ContainsKey(this)) {
				//if (printedFragments[this] > 1) {
				builder.AppendLine(LineHelper.GenerateIndent(level + 1) + "-- GRF Editor Decompiler : CodeReconstructor has failed to identify the usage of this goto " + Label);
				return;
				//}

				//builder.AppendLine(LineHelper.GenerateIndent(level + 1) + "-- GRF Editor Decompiler : CodeReconstructor has already printed this goto " + Label);
				//printedFragments[this]++;
			}

			printedFragments[this] = 1;

			Dictionary<int, CodeFragment> references = new Dictionary<int, CodeFragment>();

			for (int i = 0; i < ChildReferences.Count; i++) {
				try {
					references.Add(_getGotoLineIndex(ChildReferences[i]), ChildReferences[i]);
				}
				catch (Exception) {
					//ErrorHandler.HandleException(err);
				}
			}

			for (int i = _printLineStart; i < Lines.Count; i++) {
				if (references.ContainsKey(i)) {
					references[i].Print(builder, level + LineHelper.GetIndent(Lines[i]) - 1, printedFragments);
				}
				else {
					builder.AppendIndent(level);
					builder.AppendLine(Lines[i]);
				}
			}
		}

		public void GetAllFragments(List<CodeFragment> allFragments, bool enterLoops = true) {
			for (int i = 0; i < ChildReferences.Count; i++) {
				CodeFragment fragment = ChildReferences[i];

				if (!allFragments.Contains(fragment)) {
					allFragments.Add(fragment);

					if (!enterLoops && fragment.IsLoop())
						continue;

					fragment.GetAllFragments(allFragments);
				}
			}
		}

		public bool IsLoop() {
			return FragmentType == FragmentType.ForLoop || FragmentType == FragmentType.WhileLoop;
		}

		private List<string> _getContentLines() {
			return Lines.Skip(_printLineStart).ToList();
		}

		#endregion

		#region Code reconstruction

		private void _replaceGotoWithBreak(CodeFragment fragment) {
			if (ChildReferences.Contains(fragment)) {
				int lineIndex = _getGotoLineIndex(fragment);
				Lines[lineIndex] = LineHelper.ReplaceAfterIndent(Lines[lineIndex], "break");
			}
		}

		private void _appendGoto(CodeFragment fragment) {
			if (TotalLinesExcludingLabel == 0) {
				Lines.Add(LineHelper.GenerateIndent(1) + "goto " + fragment.Label);
			}
			else if (TotalLinesExcludingLabel > 0 && !LineHelper.IsStart(Lines[Lines.Count - 1], "goto ")) {
				Lines.Add(LineHelper.GenerateIndent(LineHelper.GetIndent(Lines[1])) + "goto " + fragment.Label);
			}
			else throw new Exception("Appending a goto without removing the previous one first.");
		}

		private void _changeGoto(CodeFragment oldFragment, CodeFragment newFragment) {
			int location = _getGotoLineIndex(oldFragment);
			Lines[location] = LineHelper.ReplaceAfterIndent(Lines[location], "goto " + newFragment.Label);
			ChildReferences[ChildReferences.IndexOf(oldFragment)] = newFragment;

			newFragment.RemoveParentReference(oldFragment);
			newFragment.AddParentReference(this);

			if (oldFragment.ParentReferences.Count == 1 && oldFragment.ParentReferences[0] == this) {
				oldFragment.ChildReferences.ForEach(p => p.RemoveParentReference(oldFragment));
				oldFragment.FragmentType = FragmentType.NotReferenced;
				oldFragment.ChildReferences.Clear();
			}

			oldFragment.RemoveParentReference(this);
		}

		private void _changeGoto(CodeFragment oldFragment, IEnumerable<string> lines) {
			int location = _getGotoLineIndex(oldFragment);
			Lines.RemoveAt(location);
			Lines.InsertRange(location, lines);
		}

		private void _removeElseBranch() {
			int indexIf = _ifBranchIndex(0);
			int indexElse = _elseBranchIndex(indexIf + 1);
			int indexEnd = _endBranchIndex(indexElse + 1);

			_removeLines(indexElse, indexEnd - indexElse);
		}

		public void RemoveElseBranch() {
			CodeFragment elseFragment = Else;

			if (elseFragment != null) {
				_removeElseBranch();
				RemoveChildReference(elseFragment);
			}
		}

		public void AppendGoto(CodeFragment fragment) {
			CodeFragment execution = Execution;

			if (execution != null) {
				RemoveExecutionLine();
			}

			_appendGoto(fragment);
			AddChildReference(fragment);
		}

		private void _removeLines(int start, int count) {
			Lines.RemoveRange(start, count);
		}

		#endregion

		#region CodeLogic getters and setters

		private int _ifBranchIndex(int startIndex) {
			return LineHelper.GetLineIndexContains(Lines, "\tif ", startIndex);
		}

		private int _whileBranchIndex(int startIndex) {
			return LineHelper.GetLineIndexContains(Lines, "while ", startIndex);
		}

		private int _elseBranchIndex(int startIndex) {
			return LineHelper.GetLineIndexEndsWith(Lines, "\telse", startIndex);
		}

		private int _endBranchIndex(int startIndex) {
			return LineHelper.GetLineIndexContains(Lines, "\tend", startIndex);
		}

		private int _executionBranchIndex() {
			return Lines.Count - 1;
		}

		private int _getGotoLineIndex(CodeFragment fragment) {
			return LineHelper.GetLineIndexContains(Lines, "goto " + fragment.Label, 0);
		}

		private string _getReferenceFromGoto(int i) {
			return LineHelper.NoIndent(Lines[i].Replace("goto ", ""));
		}

		private CodeFragment _ifReference() {
			return ChildReferences.FirstOrDefault(p => p.Label == _getReferenceFromGoto(_ifBranchIndex(0) + 1));
		}

		private CodeFragment _elseReference() {
			return ChildReferences.FirstOrDefault(p => p.Label == _getReferenceFromGoto(_elseBranchIndex(0) + 1));
		}

		private CodeFragment _executionReference() {
			return ChildReferences.FirstOrDefault(p => p.Label == _getReferenceFromGoto(_executionBranchIndex()));
		}

		#endregion

		#region CodeLogic

		public void ExtractExecution() {
			// Checks all the fragments for double references
			// which must ALL be dealt with;
			// In theory, the execution reference for the loops
			// is already handled.

			if (ParentReferences.Count > 1 && !IsLoop()) {
				_setCommonParent();
			}
		}

		private void _setCommonParent() {
			List<CodeFragment> concernedParents = new List<CodeFragment>();
			_getAllParents(concernedParents);
			concernedParents.Remove(this);
			List<CodeFragment> parents = new List<CodeFragment>(concernedParents);
			List<CodeFragment> commonParents = parents.Where(t => ParentReferences.All(t._anyLeadsToBreakLoop)).ToList();

			if (commonParents.Count == 0)
				return;

			CodeFragment closestParent;

			if (commonParents.Count > 1) {
				Dictionary<CodeFragment, int> distances = new Dictionary<CodeFragment, int>();

				for (int i = 0; i < commonParents.Count; i++) {
					distances.Add(commonParents[i], commonParents[i]._findClosestDistance(this, 0, new List<CodeFragment>()));
				}

				closestParent = distances.OrderBy(p => p.Value).ToList()[0].Key;
			}
			else {
				closestParent = commonParents[0];
			}

			concernedParents.Remove(closestParent);

			if (closestParent.Execution == null) {
				CodeFragment elseReference = closestParent.Else;

				if (elseReference != null && elseReference == this) {
					closestParent._removeElseBranch();
				}

				closestParent._appendGoto(this);
				closestParent.AddChildReference(this);
			}

			foreach (CodeFragment parent in concernedParents) {
				if (parent.Execution == this) {
					parent.RemoveChildReference(parent.Execution);
					parent._removeExecutionLine();
				}
			}
		}

		private int _findClosestDistance(CodeFragment fragment, int distance, List<CodeFragment> blockedPaths) {
			if (this == fragment)
				return distance;

			if (ChildReferences.Count == 0)
				return Int32.MaxValue;

			List<CodeFragment> blockedPathsCopy = new List<CodeFragment>(blockedPaths);

			if (IsLoop()) {
				if (blockedPaths.Contains(Break))
					return Int32.MaxValue;

				blockedPathsCopy.Add(Break);
				return Break._findClosestDistance(fragment, distance + 1, blockedPathsCopy);
			}

			List<CodeFragment> children = ChildReferences.Where(p => !blockedPaths.Contains(p)).ToList();

			if (children.Count == 0)
				return Int32.MaxValue;

			blockedPathsCopy.AddRange(children);
			return children.Min(p => p._findClosestDistance(fragment, distance + 1, blockedPathsCopy));
		}

		private void _getAllParents(List<CodeFragment> parents) {
			foreach (CodeFragment parent in ParentReferences) {
				if (parents.Contains(parent))
					continue;

				parents.Add(parent);
				parent._getAllParents(parents);
			}
		}

		public void RemoveElse() {
			try {
				if (FragmentType == FragmentType.IfElse) {
					// Handles pattern #1
					CodeFragment elseFragment = Else;
					CodeFragment ifFragment = If;

					if (ifFragment._allLeadsTo(elseFragment)) {// || ifFragment._allLeadsToOrReturn(elseFragment)) {
						if (ifFragment.ParentReferences.Count > 1)
							return;

						CodeFragment executingFragment = ifFragment.Execution;

						if (executingFragment == elseFragment) {
							ifFragment.RemoveChildReference(executingFragment);
							ifFragment._removeExecutionLine();
						}

						_removeElseBranch();
						_appendGoto(elseFragment);
						FragmentType = FragmentType.If;

						// Delete all parent references
						for (int i = 0; i < elseFragment.ParentReferences.Count; i++) {
							var parent = elseFragment.ParentReferences[i];

							if (parent == this)
								continue;

							if (parent.FragmentType == FragmentType.ForLoop && parent.RemoveExecutionLine()) {
								i--;
							}

							if (parent.FragmentType == FragmentType.IfElse) {
								if (parent.If == this) {
									LubErrorHandler.Handle("Failed to remove an if branch.", LubSourceError.CodeReconstructor);
								}
								else if (parent.Else == this) {
									parent.RemoveElseBranch();
									i--;
								}
							}
						}
					}
				}
			}
			catch {
				LubErrorHandler.Handle("Failed to remove an else branch.", LubSourceError.CodeReconstructor);
			}
		}

		public bool RemoveExecutionLine() {
			if (Execution == null)
				return false;

			RemoveChildReference(Execution);
			_removeExecutionLine();
			return true;
		}

		public void RemoveElseAfterLoop() {
			if (FragmentType == FragmentType.IfElse) {
				// Handles pattern #1
				if (If._allLeadsToBreakLoop(Else)) {
					CodeFragment elseFragment = Else;
					_removeElseBranch();
					_appendGoto(elseFragment);
					FragmentType = FragmentType.If;
				}
			}
		}

		public void RemoveReturnElseBranches() {
			if (FragmentType == FragmentType.IfElse) {
				if (If._allLeadsTo(null)) {
					CodeFragment elseReference = Else;

					if ((elseReference.FragmentType == FragmentType.IfElse || elseReference.FragmentType == FragmentType.If) && !elseReference.IsPure) {
						return;
					}

					if (IsMergeElseIf()) {
						return;
					}

					// Why does this restriction exist... ??
					//if (elseReference.FragmentType != FragmentType.IfElse &&
					//    elseReference.FragmentType != FragmentType.If) {
						CodeFragment elseFragment = Else;
						_removeElseBranch();
						_appendGoto(elseFragment);
						FragmentType = FragmentType.If;
					//}
				}
			}
		}

		public void RemoveIf() {
			if (FragmentType == FragmentType.IfElse) {
				// Handles pattern #2
				if (Else._allLeadsTo(If)) {
					// I very doubt this one works, seems fishy!
					int condition = _ifBranchIndex(0);
					Lines[condition] = CodeLogic.ReverseCondition(Lines[condition]);
					LineHelper.Swap(Lines, _ifBranchIndex(0) + 1, _elseBranchIndex(2) + 1);
					_removeElseBranch();
					_appendGoto(If);
					FragmentType = FragmentType.If;
				}
			}
		}

		public void MergeIfConditions() {
			try {
				// It's always the parent who merges
				// The else fragment must also be pure (but not the parent!)
				bool hasMerged = false;

				if (FragmentType == FragmentType.IfElse) {
					CodeFragment ifFragment = If;
					CodeFragment elseFragment = Else;

					if (ifFragment.IsPure) {
						if (ifFragment.FragmentType == FragmentType.IfElse) {
							// AND cases
							// Handles pattern #AND.1
							if (ifFragment.Else == elseFragment) {
								_ifCondition = CodeLogic.MergeAnd(_ifCondition, ifFragment._ifCondition);
								_changeGoto(ifFragment, ifFragment.If);
								hasMerged = true;
							}
							else if (ifFragment.If == elseFragment && elseFragment.FragmentType == FragmentType.NormalExecution && ifFragment.Else == elseFragment.Execution) {
								_ifCondition = CodeLogic.MergeOr(CodeLogic.ReverseCondition(_ifCondition), ifFragment._ifCondition);
								RemoveElseBranch();
								_changeGoto(ifFragment, elseFragment);
								AppendGoto(elseFragment.Execution);
								FragmentType = FragmentType.If;
								hasMerged = true;
							}
							// Handles pattern #AND.2
							else if (ifFragment.If == elseFragment) {
								_ifCondition = CodeLogic.MergeAnd(_ifCondition, CodeLogic.ReverseCondition(ifFragment._ifCondition));
								_changeGoto(ifFragment, ifFragment.Else);
								hasMerged = true;
							}
						}

						if (ifFragment.FragmentType == FragmentType.If) {
							// AND cases
							// Handles pattern #AND.3
							if (ifFragment.Execution == elseFragment) {
								_ifCondition = CodeLogic.MergeAnd(_ifCondition, ifFragment._ifCondition);
								_changeGoto(ifFragment, ifFragment.If);
								_removeElseBranch();
								_appendGoto(elseFragment);
								hasMerged = true;
							}
							//else if (elseFragment.Execution) {
							//	
							//}
						}
					}
					else {
						if (ifFragment.FragmentType == FragmentType.If) {
							// AND cases
							// Handles pattern #AND.3
							if (ifFragment.Execution == elseFragment) {
								// This is not pure, merge carefully
								ifFragment.RemoveChildReference(ifFragment.Execution);
								ifFragment._removeExecutionLine();
								
								_removeElseBranch();
								_appendGoto(elseFragment);
								FragmentType = FragmentType.If;

								// Do not go further
								MergeIfConditions();
								return;
							}
						}
					}

					if (elseFragment.IsPure) {
						if (elseFragment.FragmentType == FragmentType.If) {
							// Handles pattern #AND.4
							if (elseFragment.If == ifFragment) {
								_ifCondition = CodeLogic.MergeAnd(CodeLogic.ReverseCondition(_ifCondition), ifFragment._ifCondition);
								_changeGoto(ifFragment, elseFragment.If);
								_removeElseBranch();
								_appendGoto(ifFragment);
								hasMerged = true;
							}
						}

						if (elseFragment.FragmentType == FragmentType.IfElse) {
							// OR cases
							// Handles pattern #OR.1
							if (elseFragment.If == ifFragment) {
								_ifCondition = CodeLogic.MergeOr(_ifCondition, elseFragment._ifCondition);
								_changeGoto(elseFragment, elseFragment.Else);
								hasMerged = true;
							}

								// Handles pattern #OR.2
							else if (elseFragment.Else == ifFragment) {
								_ifCondition = CodeLogic.MergeOr(_ifCondition, CodeLogic.ReverseCondition(elseFragment._ifCondition));
								_changeGoto(elseFragment, elseFragment.If);
								hasMerged = true;
							}
						}
					}

					if (!ifFragment.IsPure && ifFragment.FragmentType == FragmentType.IfElse && !IsPure) {
						// Cannot merge, but check where they lead
						if (ifFragment.Else != elseFragment && ifFragment.Else._allLeadsTo(elseFragment)) {
							// That means we can remove the else branches
							_removeElseBranch();
							ifFragment._removeElseBranch();
							_appendGoto(elseFragment);
							ifFragment.FragmentType = FragmentType.If;
							FragmentType = FragmentType.If;
							hasMerged = true;
						}
					}

					if (hasMerged) {
						MergeIfConditions();
					}
				}
				else if (FragmentType == FragmentType.If) {
					CodeFragment ifFragment = If;
					CodeFragment executionFragment = Execution;

					if (ifFragment.IsPure) {
						// Handles pattern #AND.5
						if (ifFragment.FragmentType == FragmentType.IfElse) {
							if (ifFragment.Else == executionFragment) {
								_ifCondition = CodeLogic.MergeAnd(_ifCondition, ifFragment._ifCondition);
								_changeGoto(ifFragment, ifFragment.If);
							}
						}

							// Handles pattern #AND.6
						else if (ifFragment.FragmentType == FragmentType.If) {
							if (ifFragment.Execution == executionFragment) {
								_ifCondition = CodeLogic.MergeAnd(_ifCondition, ifFragment._ifCondition);
								_changeGoto(ifFragment, ifFragment.If);
							}
						}
					}
				}
			}
			catch (Exception) {
				//ErrorHandler.HandleException(err);
				MergeIfConditions();
			}
		}

		public bool IsMergeElseIf() {
			if (FragmentType == FragmentType.IfElse || FragmentType == FragmentType.ElseIf) {
				CodeFragment elseFragment = Else;

				if (elseFragment == null)
					return false;

				if (elseFragment.IsPure && elseFragment.ParentReferences.Count == 1) {
					if (elseFragment.FragmentType == FragmentType.IfElse) {
						return true;
					}

					if (elseFragment.FragmentType == FragmentType.If) {
						return true;
					}

					if (elseFragment.FragmentType == FragmentType.ElseIf) {
						return true;
					}
				}
			}

			return false;
		}

		public void MergeElseIf() {
			bool merged = false;

			if (FragmentType == FragmentType.IfElse || FragmentType == FragmentType.ElseIf) {
				CodeFragment elseFragment = Else;

				if (elseFragment == null)
					return;

				// ?? Major change, added  && elseFragment.ParentReferences.Count == 1
				if (elseFragment.IsPure && elseFragment.ParentReferences.Count == 1) {
					if (elseFragment.FragmentType == FragmentType.IfElse) {
						// Simple direct merge
						merged = true;
						_elseCondition = CodeLogic.ToElseIf(_elseCondition, elseFragment._ifCondition);

						List<string> linesToAppend = elseFragment._getContentLines();
						linesToAppend.RemoveAt(0);
						linesToAppend.RemoveAt(linesToAppend.Count - 1);

						_changeGoto(elseFragment, linesToAppend);
						AddChildReference(elseFragment.ChildReferences);
						RemoveChildReference(elseFragment);

						FragmentType = FragmentType.ElseIf;
					}

					if (elseFragment.FragmentType == FragmentType.If) {
						merged = true;
						_elseCondition = CodeLogic.ToElseIf(_elseCondition, elseFragment._ifCondition);

						List<string> linesToAppend = elseFragment._getContentLines();
						linesToAppend.RemoveAt(0);
						Lines.RemoveAt(Lines.Count - 1);

						_changeGoto(elseFragment, linesToAppend);
						AddChildReference(elseFragment.ChildReferences);
						RemoveChildReference(elseFragment);

						FragmentType = FragmentType.ElseIf;
					}

					if (elseFragment.FragmentType == FragmentType.ElseIf) {
						// There are two types of else ifs
						if (elseFragment.Execution == null) {
							// Simple direct merge
							merged = true;
							_elseCondition = CodeLogic.ToElseIf(_elseCondition, elseFragment._ifCondition);

							List<string> linesToAppend = elseFragment._getContentLines();
							linesToAppend.RemoveAt(0);
							linesToAppend.RemoveAt(linesToAppend.Count - 1);

							_changeGoto(elseFragment, linesToAppend);
							AddChildReference(elseFragment.ChildReferences);
							RemoveChildReference(elseFragment);

							FragmentType = FragmentType.ElseIf;
						}
						else {
							merged = true;
							_elseCondition = CodeLogic.ToElseIf(_elseCondition, elseFragment._ifCondition);

							List<string> linesToAppend = elseFragment._getContentLines();
							linesToAppend.RemoveAt(0);
							Lines.RemoveAt(Lines.Count - 1);

							_changeGoto(elseFragment, linesToAppend);
							AddChildReference(elseFragment.ChildReferences);
							RemoveChildReference(elseFragment);

							FragmentType = FragmentType.ElseIf;
						}
					}
				}
			}

			if (merged)
				MergeElseIf();
		}

		public void AnalyseLogicalExecutionLoops(List<CodeFragment> processedFragments = null) {
			try {
				// This process cuts off the fragments; they will no longer
				// be linked properly beyond this point.
				if (processedFragments == null) {
					processedFragments = new List<CodeFragment>();
				}

				if (processedFragments.Contains(this))
					return;

				processedFragments.Add(this);

				if (IsLoop()) {
					List<CodeFragment> fragments = new List<CodeFragment>();
					fragments.Add(this);
					GetAllFragments(fragments, false);

					fragments.Remove(this);

					CodeFragment fragment;
					CodeFragment executionFragment;

					for (int i = 0; i < fragments.Count; i++) {
						fragment = fragments[i];

						executionFragment = fragment.Execution;

						if (executionFragment != null) {
							if (executionFragment == this) {
								if (executionFragment.ParentReferences.Count > 1) {
									fragment.RemoveChildReference(executionFragment);
									fragment._removeExecutionLine();
								}
							}
						}

						if (!fragment.IsLoop()) {
							foreach (CodeFragment child in fragment.ChildReferences) {
								if (child == Break) {
									fragment._replaceGotoWithBreak(Break);
								}
								else if (child == this) {
									fragment._replaceGotoWithContinue(this);
								}
							}
						}
					}
				}

				ChildReferences.ForEach(p => p.AnalyseLogicalExecutionLoops(processedFragments));
			}
			catch {
				LubErrorHandler.Handle("Failed to analyse the logical execution of loops.", LubSourceError.CodeReconstructor);
			}
		}

		private void _replaceGotoWithContinue(CodeFragment fragment) {
			if (ChildReferences.Contains(fragment)) {
				int lineIndex = _getGotoLineIndex(fragment);
				Lines[lineIndex] = LineHelper.ReplaceAfterIndent(Lines[lineIndex], "continue");
			}
		}

		private void _removeExecutionLine() {
			try {
				if (LineHelper.IsStart(Lines[Lines.Count - 1], "goto ")) {
					Lines.RemoveAt(Lines.Count - 1);
				}
			}
			catch {
				LubErrorHandler.Handle("Failed to remove an execution line reference.", LubSourceError.CodeReconstructor);
			}
		}

		private void _removeReturnLine() {
			try {
				if (LineHelper.IsStart(Lines[Lines.Count - 1], "return")) {
					Lines.RemoveAt(Lines.Count - 1);
				}
			}
			catch {
				LubErrorHandler.Handle("Failed to remove a return line reference.", LubSourceError.CodeReconstructor);
			}
		}

		public void RemoveLogicalReturnExecution(CodeFragment root) {
			if (IsReturn && IsPure) {
				CodeFragment fragment = _getLogicalExecutionReference(this);

				if (fragment == null) {
					_removeReturnLine();
				}
				else {
					if (this != root && root._allLeadsToBreakLoop(this)) {
						_removeReturnLine();
					}
				}
			}
		}

		public void RemoveLogicalExecution() {
			// Basically : anyone with an execution reference fragment can remove code based on their parents
			if (ParentReferences.Count > 0 && (FragmentType != FragmentType.NotReferenced && FragmentType != FragmentType.None)) {
				CodeFragment executiongFragment = Execution;

				if (executiongFragment == null)
					return;

				if (ParentReferences.Count == 1) {
					CodeFragment parent = ParentReferences[0];

					if (parent._getLogicalExecutionReference(this) == executiongFragment) {
						RemoveChildReference(executiongFragment);
						_removeExecutionLine();
					}
				}
				else if (ParentReferences.Count == 2) {
					List<CodeFragment> children = new List<CodeFragment>();
					children.Add(this);
					GetAllFragments(children);

					List<CodeFragment> highParents = ParentReferences.Where(p => !children.Contains(p)).ToList();

					if (highParents.Count != 1)
						return;

					if (highParents[0]._getLogicalExecutionReference(this) == executiongFragment) {
						RemoveChildReference(executiongFragment);
						_removeExecutionLine();
					}
				}
			}
		}

		private CodeFragment _getLogicalExecutionReference(CodeFragment toSkip, List<CodeFragment> processedFragments = null) {
			if (processedFragments == null)
				processedFragments = new List<CodeFragment>();

			if (processedFragments.Contains(this))
				return null;

			processedFragments.Add(this);

			CodeFragment execution = Execution;

			if (execution != null && execution != toSkip)
				return execution;

			if (ParentReferences.Count > 1 || ParentReferences.Count == 0)
				return null;

			return ParentReferences[0]._getLogicalExecutionReference(toSkip, processedFragments);
		}

		public void SetWhileLoop(List<CodeFragment> processedFragments = null) {
			if (processedFragments == null) {
				processedFragments = new List<CodeFragment>();
			}

			if (processedFragments.Contains(this))
				return;

			processedFragments.Add(this);

			if (_isWhileLoop()) {
				_setWhileLoop();
			}

			ChildReferences.ForEach(p => p.SetWhileLoop(processedFragments));
		}

		private void _setWhileLoop() {
			if (If == Break) {
				// We reverse the loop condition
				_ifCondition = CodeLogic.ReverseCondition(_ifCondition);

				if (FragmentType == FragmentType.If) {
					int indexGotoIf = _ifBranchIndex(0) + 1;
					int indexGotoExecution = _executionBranchIndex();
					LineHelper.Swap(Lines, indexGotoIf, indexGotoExecution);
					Lines[indexGotoIf] = LineHelper.AddIndent(Lines[indexGotoIf], 1);
					Lines[indexGotoExecution] = LineHelper.RemoveIndent(Lines[indexGotoExecution], 1);
				}
				else {
					LineHelper.Swap(Lines, _ifBranchIndex(0) + 1, _elseBranchIndex(0) + 1);
				}
			}

			Lines[_ifBranchIndex(0)] = CodeLogic.ChangeToWhile(_ifCondition);

			if (FragmentType == FragmentType.IfElse) {
				CodeFragment elseFragment = Else;
				_removeElseBranch();
				_appendGoto(elseFragment);
			}

			FragmentType = FragmentType.WhileLoop;
			List<CodeFragment> fragments = new List<CodeFragment>();
			_whileReference().GetAllFragments(fragments);
			fragments.Remove(this);
			fragments.ForEach(p => p._replaceGotoWithBreak(Else ?? Execution));

			//AnalyseLogicalExecutionLoops();
		}

		private CodeFragment _whileReference() {
			return ChildReferences.FirstOrDefault(p => p.Label == _getReferenceFromGoto(_whileBranchIndex(0) + 1));
		}

		private bool _isWhileLoop() {
			// A while loop can also be an if node; this will be 
			// caused by negative conditional loops.
			if ((FragmentType == FragmentType.IfElse ||
			     FragmentType == FragmentType.If) && ParentReferences.Count > 1
			    && ChildReferences.Count > 1) {
				CodeFragment breakFragment = null;
				CodeFragment loopFragment = null;
				bool result = false;

				if (ParentReferences.Count == 2) {
					CodeFragment parent1 = ParentReferences[0];
					CodeFragment parent2 = ParentReferences[1];

					if (parent1.ParentReferences.Count == 1) {
						if (parent1.ParentReferences[0] == parent2)
							return false;
					}

					if (parent2.ParentReferences.Count == 1) {
						if (parent2.ParentReferences[0] == parent1)
							return false;
					}
				}

				if (FragmentType == FragmentType.IfElse) {
					// We need to validate the parents first

					CodeFragment ifFragment = If;
					CodeFragment elseFragment = Else;

					result = !ifFragment._anyLeadsToBreakLoop(this);

					if (result) {
						breakFragment = ifFragment;
						loopFragment = elseFragment;
					}
					else {
						result = !elseFragment._anyLeadsToBreakLoop(this);

						if (result) {
							breakFragment = elseFragment;
							loopFragment = ifFragment;
						}
					}
				}
				else if (FragmentType == FragmentType.If) {
					CodeFragment ifFragment = If;
					CodeFragment executionFragment = Execution;

					result = !ifFragment._anyLeadsToBreakLoop(this);

					if (result) {
						breakFragment = ifFragment;
						loopFragment = executionFragment;
					}
					else {
						result = !executionFragment._anyLeadsToBreakLoop(this);

						if (result) {
							breakFragment = executionFragment;
							loopFragment = ifFragment;
						}
					}
				}

				// Either the if fragment or the else fragment is recursive
				// We could be in an inner scope loop though, so we can't
				// confirm much yet.

				if (result && loopFragment != null) {
					// Possible while loop

					// At least one path must lead back to this fragment
					if (!loopFragment._anyLeadsTo(this)) {
						return false;
					}

					// We assume this is a while loop
					// All the other paths must lead to the breakFragment
					List<CodeFragment> fragments = new List<CodeFragment>();
					loopFragment.GetAllFragments(fragments);
					fragments.Remove(this);

					foreach (CodeFragment parent in ParentReferences) {
						fragments.Remove(parent);
					}

					// This is a while loop
					Break = breakFragment;
					return true;
				}
			}

			return false;
		}

		private bool _allLeadsTo(CodeFragment fragment) {
			return _findFragment(this, fragment, 0, new List<CodeFragment>(), false);
		}

		private bool _anyLeadsTo(CodeFragment fragment) {
			return _findFragment(this, fragment, 0, new List<CodeFragment>(), true);
		}

		private bool _anyLeadsToBreakLoop(CodeFragment fragment) {
			return _findFragment(this, fragment, 0, new List<CodeFragment>(), true, true);
		}

		private bool _allLeadsToBreakLoop(CodeFragment fragment) {
			return _findFragment(this, fragment, 0, new List<CodeFragment>(), false, true);
		}

		private bool _allLeadsToOrReturn(CodeFragment fragment) {
			return _findFragment(this, fragment, 0, new List<CodeFragment>(), false, false, true);
		}

		private bool _findFragment(CodeFragment currentFragment, CodeFragment toFind, int level, ICollection<CodeFragment> processedFragments, bool any, bool breakLoop = false, bool orReturn = false) {
			if (processedFragments.Contains(currentFragment))
				return _leads != null && _leads.Value;

			_leads = null;

			processedFragments.Add(currentFragment);

			if (level > 20) {
				_leads = false;
				return false;
			}

			if (currentFragment == toFind) {
				_leads = true;
				return true;
			}

			if (currentFragment.ChildReferences.Count == 0) {
				if (toFind == null || orReturn) {
					_leads = true;
					return true;
				}

				_leads = false;
				return false;
			}

			bool result;

			if (currentFragment.IsLoop()) {
				if (breakLoop) {
					result = Break._findFragment(Break, toFind, level + 1, processedFragments, any, true);
					_leads = result;
					return result;
				}

				if (!any) {
					if (currentFragment.FragmentType == FragmentType.ForLoop) {
						bool? back = _leads;
						bool allLead = currentFragment.ChildReferences[0]._findFragment(currentFragment.ChildReferences[0], currentFragment, level + 1, new List<CodeFragment>(), false);
						_leads = back;

						if (allLead) {
							result = Break._findFragment(Break, toFind, level + 1, processedFragments, false, true);
							_leads = result;
							return result;
						}
					}
				}
			}

			if (any) {
				result = currentFragment.ChildReferences.Any(p => p._findFragment(p, toFind, level + 1, processedFragments, true, breakLoop));
			}
			else {
				result = currentFragment.ChildReferences.All(p => p._findFragment(p, toFind, level + 1, processedFragments, false, breakLoop));
			}

			_leads = result;
			return result;
		}

		#endregion
	}
}