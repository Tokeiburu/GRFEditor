using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.FileFormats.LubFormat.VM;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor {
	public partial class CodeFragment {
		public List<CodeFragment> ChildReferences = new List<CodeFragment>();
		public List<CodeFragment> ParentReferences = new List<CodeFragment>();
		public TkDictionary<string, CodeFragment> Common = new TkDictionary<string, CodeFragment>();
		private bool? _leads;
		private OpCodes.RelationalStatement _condition;

		public int Uid { get; private set; }

		private static int _uid;

		public CodeFragment(int lineStart, int lineEnd, IEnumerable<string> lines, TkDictionary<string, CodeFragment> common) {
			Common = common;
			Loop_PC_Start = -1;

			Content = new LineBlock(lines, lineStart, lineEnd);

			_detectFragmentType();

			Common[Content.Label] = this;
			Uid = ++_uid;

			PC_Index = Content.GetLabelId();
		}

		public void NewUid() {
			Uid = ++_uid;
		}

		#region Getter and setters
		public CodeFragment Else {
			get { return FindGotoFragment(LuaToken.ElseGoto); }
		}

		public CodeFragment If {
			get { return FindGotoFragment(LuaToken.IfGoto); }
		}

		public CodeFragment For {
			get { return FindGotoFragment(LuaToken.LoopGoto); }
		}

		public CodeFragment Execution {
			get { return FindGotoFragment(LuaToken.Execution); }
		}

		public CodeFragment Break { get; set; }

		public bool IsLoop {
			get { return FragmentType == FragmentType.Loop; }
		}

		public bool IsReturn {
			get { return ChildReferences.Count == 0 && Content.LastLineIsReturn(); }
		}

		public bool IsDeleted {
			get { return !Content.IsRoot && ParentReferences.Count == 0; }
		}

		public int PC_Index { get; set; }
		public int Loop_PC_Start { get; set; }
		public int Loop_PC_End { get; set; }
		public bool IsBreakTarget { get; set; }
		public LineBlock Content { get; set; }
		public FragmentType FragmentType { get; set; }
		public CodeFragment LoopScope { get; set; }

		public OpCodes.RelationalStatement IfCondition {
			get {
				if (_condition == null) {
					string rCond = Content.IfLineCondition;
					_condition = new OpCodes.RelationalStatement(rCond);
					_condition.Changed += delegate { Content.IfLineCondition = _condition.ToString(); };
				}

				return _condition;
			}
		}
		#endregion

		public override string ToString() {
			return Content.ToString();
		}

		public bool RemoveEmpty() {
			if (Content.TotalLinesExcludingLabel <= 1 && ChildReferences.Count == 1) {
				for (int i = 0; i < ParentReferences.Count; i++) {
					var child = ChildReferences[0];

					if (ParentReferences[i].RemoveEmptyGoto(this, child)) {
						child.IsBreakTarget = IsBreakTarget;
						i--;
					}
				}

				return true;
			}

			return false;
		}

		#region Initialization
		private void _detectFragmentType() {
			FragmentType = FragmentType.NormalExecution;

			if (Content.IsIfElse()) {
				FragmentType = FragmentType.IfElse;
			}
			else if (Content.IsLoop()) {
				var loop_pc_index = LineHelper.GetLineIndexContains(Content.Lines, Lub.String_LoopPcBounds, 0);

				if (loop_pc_index != -1) {
					string data = Content.Lines[loop_pc_index];
					Content.Lines.RemoveAt(loop_pc_index);

					data = LineHelper.NoIndent(data).Substring(Lub.String_LoopPcBounds.Length);
					var data2 = data.Split(' ');
					Loop_PC_Start = Int32.Parse(data2[0]);
					Loop_PC_End = Int32.Parse(data2[1]);
				}
				else {
					throw new Exception(Lub.String_LoopPcBounds + " not found");
				}

				FragmentType = FragmentType.Loop;
			}
		}
		#endregion

		#region References
		public void SetReferences(List<CodeFragment> fragments) {
			for (int i = 0; i < Content.Lines.Count; i++) {
				if (LineHelper.IsStart(Content.Lines[i], "goto ")) {
					string label = LineHelper.GetLabelFromGoto(Content.Lines[i]);
					CodeFragment linkedFragment = Common[label];

					if (linkedFragment != null) {
						Link(this, linkedFragment);
					}
				}
			}

			for (int i = 1; i < Content.Lines.Count; i++) {
				string line = LineHelper.NoIndent(Content.Lines[i]);

				if (line.StartsWith("if "))
					break;

				if (line.StartsWith("return"))
					break;

				if (line.StartsWith("goto "))
					break;

				if (i == Content.Lines.Count - 1) {
					CodeFragment linkedFragment = CodeLogic.GetFragment(fragments, Content._originalLineIndexEnd + 1);

					if (linkedFragment != null) {
						Link(this, linkedFragment);
						Content.AppendGoto(linkedFragment);
					}
				}
			}

			if (Content.Lines.Count == 1) {
				// This reference should not be there at all, this is just a bug fix
				CodeFragment linkedFragment = CodeLogic.GetFragment(fragments, Content._originalLineIndexEnd + 1);

				if (linkedFragment != null) {
					Link(this, linkedFragment);

					if (Content.IsRoot)
						Content.AppendGoto(linkedFragment);
				}
			}

			if (FragmentType == FragmentType.Loop) {
				Break = ChildReferences[1];
				Break.IsBreakTarget = true;
			}
		}
		#endregion

		#region Utility methods
		public class PrintData {
			public HashSet<CodeFragment> Fragments = new HashSet<CodeFragment>();
			public int BaseIndentDiff { get; set; }
		}

		// Indent rules:
		// - The code decompiler will either have an indent of 0 or 1, which is defined by function.BaseIndent
		// - The indent must be correct in the code decompiler (code fragments) structure.
		// - Do not use final indent for anything; it's either +0 or +1, depending on who called
		public void Print(StringBuilder builder, LubFunction function, PrintData data, int level) {
			if (data == null) {
				data = new PrintData();
				data.BaseIndentDiff = function.BaseIndent;
			}

			if (!data.Fragments.Add(this)) {
				builder.AppendLine(LineHelper.GenerateIndent(level) + "-- GRF Editor Decompiler : CodeReconstructor has failed to identify the usage of this goto " + Content.Label);
				return;
			}

			Dictionary<int, CodeFragment> references = new Dictionary<int, CodeFragment>();

			for (int i = 0; i < ChildReferences.Count; i++) {
				references.Add(Content.GetGotoLineIndex(ChildReferences[i]), ChildReferences[i]);
			}

			var lines = Content.Lines;

			for (int i = Content.PrintLineStart; i < lines.Count; i++) {
				if (references.ContainsKey(i)) {
					references[i].Print(builder, function, data, level + LineHelper.GetIndent(lines[i]) - data.BaseIndentDiff);
				}
				else {
					if (i != 0 || !lines[i].StartsWith("function(")) {
						builder.AppendIndent(level);
					}

					builder.AppendLine(lines[i]);
				}
			}
		}

		public List<CodeFragment> GetLoopFragments(List<CodeFragment> allFragments) {
			List<CodeFragment> l = new List<CodeFragment>();
			int i = 0;

			if (allFragments.Count > 1) {
				if (allFragments[0].Content.IsRoot && allFragments[1].PC_Index == 0)
					i = 1;
			}

			for (; i < allFragments.Count; i++) {
				if (IsWithinLoop(this, allFragments[i]))
					l.Add(allFragments[i]);
			}

			return l;
		}

		public void GetAllFragments(List<CodeFragment> allFragments, bool enterLoops = true) {
			for (int i = 0; i < ChildReferences.Count; i++) {
				CodeFragment fragment = ChildReferences[i];

				if (!allFragments.Contains(fragment)) {
					allFragments.Add(fragment);

					if (!enterLoops && fragment.IsLoop)
						continue;

					fragment.GetAllFragments(allFragments);
				}
			}
		}
		#endregion

		#region CodeLogic getters and setters
		public CodeFragment FindGotoFragment(LuaToken token) {
			string label = Content.GetGotoLabel(token);
			if (label == null)
				return null;
			return ChildReferences.FirstOrDefault(p => p.Content.Label == label);
		}
		#endregion

		#region CodeLogic
		public void ExtractExecution() {
			// Checks all the fragments for double references
			// which must ALL be dealt with;
			// In theory, the execution reference for the loops
			// is already handled.
			if (ParentReferences.Count > 1) {
				_setCommonParent();
			}
		}

		private void _setCommonParent() {
			List<CodeFragment> concernedParents = new List<CodeFragment>();
			_getAllParents(concernedParents);
			concernedParents.Remove(this);

			try {
				// The correct parent is the one common to all parent paths?
				List<List<CodeFragment>> paths = new List<List<CodeFragment>>();

				foreach (var parent in ParentReferences) {
					var allParents = new List<CodeFragment>();
					allParents.Add(parent);
					parent._getAllParents(allParents);
					allParents.Reverse();
					paths.Add(allParents);
				}

				CodeFragment closestParent = paths[0][0];
				int s = 0;

				while (true) {
					if (paths.All(p => s < p.Count && s < paths[0].Count && p[s] == paths[0][s])) {
						closestParent = paths[0][s];
						s++;
						continue;
					}

					break;
				}

				var parents = new List<CodeFragment>(ParentReferences);
				parents.Remove(closestParent);

				if (closestParent.Execution == null) {
					CodeFragment elseReference = closestParent.Else;

					if (elseReference != null && elseReference == this) {
						closestParent.Content.RemoveElseLines();
					}

					closestParent.AppendGoto(this);
				}

				foreach (CodeFragment parent in parents) {
					if (parent.Execution == this) {
						parent.RemoveExecutionLine();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _getAllParents(List<CodeFragment> parents) {
			foreach (CodeFragment parent in ParentReferences) {
				if (parents.Contains(parent))
					continue;

				// This fragment will be a break, so it's going to be resolved and can be safely ignored
				if (parent.LoopScope != null && parent.LoopScope.Break == this)
					continue;

				parents.Add(parent);
				parent._getAllParents(parents);
			}
		}

		public void RemoveElse() {
			try {
				// Cases with empty code
				if (FragmentType == FragmentType.IfElse && If == Else) {
					RemoveElseBranchWithGoto(Else);
					Content.RemoveLine(LuaToken.IfGoto);
					FragmentType = FragmentType.NormalExecution;
					return;
				}

				// Cases with empty code
				if (FragmentType == FragmentType.Loop && For == this) {
					Content.RemoveLine(LuaToken.LoopGoto);
					FragmentType = FragmentType.NormalExecution;
					return;
				}

				if (FragmentType == FragmentType.IfElse) {
					// Handles pattern #1
					CodeFragment elseFragment = Else;
					CodeFragment ifFragment = If;

					var leadsIf = ifFragment.AllLeadsToOrReturn(elseFragment, elseFragment.Uid);
					var leadsElse = elseFragment.AllLeadsToOrReturn(ifFragment, ifFragment.Uid);

					if (leadsIf || leadsElse) {
						// Both, take the node with the lowest Uid
						if ((leadsIf && leadsElse && elseFragment.Uid < ifFragment.Uid) || leadsIf == false) {
							IfCondition.Reverse();
							Content.SwapIfElseLines();
							elseFragment = Else;
							ifFragment = If;
						}

						CodeFragment executingFragment = ifFragment.Execution;

						if (executingFragment == elseFragment) {
							ifFragment.RemoveExecutionLine();
						}

						RemoveElseBranchWithGoto(elseFragment);
						FragmentType = FragmentType.If;
					}
				}
			}
			catch {
				LubErrorHandler.Handle("Failed to remove an else branch.", LubSourceError.CodeReconstructor);
			}
		}

		public void RemoveReturnElseBranches() {
			if (FragmentType == FragmentType.IfElse) {
				for (int i = 0; i < 2; i++) {
					CodeFragment ifFragment = i == 0 ? If : Else;
					CodeFragment elseFragment = i == 0 ? Else : If;

					if (ifFragment.AllLeadsTo(null, elseFragment.Uid)) {
						if (IsMergeElseIf()) {
							return;
						}

						if (i == 1) {
							ReverseIfElse();
						}

						RemoveElseBranchWithGoto(elseFragment);
						FragmentType = FragmentType.If;
						return;
					}
				}
			}
		}

		public bool MergeIfConditions2() {
			if (ParentReferences.Count != 1)
				return false;

			if (FragmentType == FragmentType.IfElse) {
				CodeFragment ifFragment = If;
				CodeFragment elseFragment = Else;

				var parent = ParentReferences[0];

				if (ParentReferences.Count == 1 && parent.FragmentType == FragmentType.IfElse) {
					if (parent.If == this && parent.Else == Else) {
						parent.IfCondition.Combine(IfCondition, OpCodes.ConditionToken.And);
						parent.SetIf(ifFragment);
						Unlink(this, elseFragment);
						Unlink(this, ifFragment);
						return true;
					}

					if (parent.If == this && parent.Else == If) {
						parent.ReverseIfElse();
						parent.IfCondition.Combine(IfCondition, OpCodes.ConditionToken.Or);
						parent.SetElse(elseFragment);
						Unlink(this, elseFragment);
						Unlink(this, ifFragment);
						return true;
					}

					if (parent.Else == this && parent.If == If) {
						parent.IfCondition.Combine(IfCondition, OpCodes.ConditionToken.Or);
						parent.SetElse(elseFragment);
						Unlink(this, elseFragment);
						Unlink(this, ifFragment);
						return true;
					}

					// This is never created by the compiler; so this must be an elseif block
					if (parent.Else == this && parent.If == Else) {
						parent.ReverseIfElse();
						parent.IfCondition.Combine(IfCondition, OpCodes.ConditionToken.And);
						parent.SetIf(ifFragment);
						Unlink(this, elseFragment);
						Unlink(this, ifFragment);
						//throw new Exception("Not tested");
						return true;
					}
				}
			}

			return false;
		}

		public bool IsMergeElseIf() {
			if (FragmentType == FragmentType.IfElse || FragmentType == FragmentType.ElseIf) {
				for (int i = 0; i < 2; i++) {
					CodeFragment fragment = i == 0 ? Else : If;

					if (fragment == null)
						continue;

					if (fragment.ParentReferences.Count == 1) {
						if (fragment.FragmentType == FragmentType.IfElse ||
						    fragment.FragmentType == FragmentType.If ||
						    fragment.FragmentType == FragmentType.ElseIf) {
							return true;
						}
					}
				}
			}

			return false;
		}

		public void MergeElseIf() {
			if (FragmentType == FragmentType.IfElse || FragmentType == FragmentType.ElseIf) {
				for (int i = 0; i < 2; i++) {
					CodeFragment fragment = i == 0 ? Else : If;

					if (fragment == null)
						continue;

					if (fragment.ParentReferences.Count == 1) {
						if (fragment.FragmentType == FragmentType.IfElse ||
						    fragment.FragmentType == FragmentType.If ||
						    fragment.FragmentType == FragmentType.ElseIf) {
							if (i == 1) {
								if (Else.Uid > If.Uid)
									return;

								ReverseIfElse();
							}

							ElseIfMerge(fragment);
							return;
						}
					}
				}
			}
		}

		private void ElseIfMerge(CodeFragment elseFragment) {
			var elseIndex = Content.IndexOf(LuaToken.Else);
			Content.ReplaceElseContent(elseFragment.Content);
			Content.Lines[elseIndex] = LineHelper.ReplaceAfterIndent(Content.Lines[elseIndex], "elseif " + elseFragment.IfCondition + " then");
			Unlink(this, elseFragment);

			var children = elseFragment.ChildReferences.ToList();

			foreach (var child in children) {
				Unlink(elseFragment, child);
				Link(this, child);
			}

			FragmentType = FragmentType.ElseIf;
		}

		public void AnalyseLogicalExecutionLoops(List<CodeFragment> allFragments, List<CodeFragment> processedFragments = null) {
			try {
				// This process cuts off the fragments; they will no longer
				// be linked properly beyond this point.
				if (processedFragments == null) {
					processedFragments = new List<CodeFragment>();
				}

				if (processedFragments.Contains(this))
					return;

				processedFragments.Add(this);

				ChildReferences.ForEach(p => p.AnalyseLogicalExecutionLoops(allFragments, processedFragments));

				if (IsLoop) {
					var fragments = GetLoopFragments(allFragments).Where(p => p.LoopScope == this).ToList();

					fragments.Remove(this);

					CodeFragment fragment;
					CodeFragment executionFragment;

					for (int i = 0; i < fragments.Count; i++) {
						fragment = fragments[i];
						executionFragment = fragment.Execution;

						if (executionFragment != null) {
							if (executionFragment == this) {
								if (executionFragment.ParentReferences.Count > 1) {
									fragment.RemoveExecutionLine();
								}
							}
						}

						if (!fragment.IsLoop) {
							if (fragment.FragmentType == FragmentType.IfElse) {
								if (fragment.Else == this) {
									fragment.RemoveElseBranch();
								}
							}
							foreach (CodeFragment child in fragment.ChildReferences) {
								if (child == Break) {
									fragment.Content.RepaceGotoWithBreak(Break);
								}
								else if (child == this) {
									// Continue isn't part of the language...
									//fragment.Content._replaceGotoWithContinue(this);
									throw new Exception("Attempting to continue a loop; should be handled.");
								}
							}
						}
					}
				}
			}
			catch {
				LubErrorHandler.Handle("Failed to analyse the logical execution of loops.", LubSourceError.CodeReconstructor);
			}
		}

		public bool IsPureReturn() {
			return IsReturn && LineHelper.NoIndent(Content.Lines[Content.Lines.Count - 1]) == "return";
		}

		public void RemoveLogicalReturnExecution() {
			if (IsPureReturn()) {
				CodeFragment fragment = _getLogicalExecutionReference(this);

				if (fragment == null) {
					Content.RemoveReturnLine();
				}
			}
		}

		public void RemoveLogicalExecution() {
			// Basically : anyone with an execution reference fragment can remove code based on their parents
			if (ParentReferences.Count > 0) {
				CodeFragment executiongFragment = Execution;

				if (executiongFragment == null)
					return;

				if (ParentReferences.Count == 1) {
					CodeFragment parent = ParentReferences[0];

					if (parent._getLogicalExecutionReference(this) == executiongFragment) {
						RemoveExecutionLine();
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
						RemoveExecutionLine();
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

			return ParentReferences[0]._getLogicalExecutionReference(this, processedFragments);
		}

		#endregion
	}
}