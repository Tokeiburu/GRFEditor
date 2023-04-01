using System.Collections.Generic;
using System.Linq;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor2 {
	//public class AdvCodeFragment {
	//	public bool IsRoot { get; set; }
	//	public string Label { get; set; }
	//	public List<string> Lines { get; private set; }
	//
	//	private int _originalLineIndexStart { get; set; }
	//	private int _originalLineIndexEnd { get; set; }
	//
	//	public AdvFragmentType FragmentType { get; set; }
	//	public CodeReconstructorCommon Common { get; private set; }
	//
	//	public List<AdvCodeFragment> ChildReferences = new List<AdvCodeFragment>();
	//	public List<AdvCodeFragment> ParentReferences = new List<AdvCodeFragment>();
	//
	//	public AdvCodeFragment Break { get; set; }
	//
	//	public AdvCodeFragment(int lineStart, int lineEnd, IEnumerable<string> lines, CodeReconstructorCommon common) {
	//		_originalLineIndexStart = lineStart;
	//		_originalLineIndexEnd = lineEnd;
	//		Common = common;
	//
	//		Lines = lines.Skip(_originalLineIndexStart).Take(_originalLineIndexEnd - _originalLineIndexStart + 1).ToList();
	//		_detectFragmentType();
	//		IsRoot = lineStart == 0;
	//		Label = IsRoot ? "root" : Lines[0].Replace("::", "");
	//		Common.Fragments[Label] = this;
	//	}
	//
	//	private void _detectFragmentType() {
	//		FragmentType = AdvFragmentType.NormalExecution;
	//
	//		if (CodeLogic.IsIfElseBranch(this)) {
	//			//int indexIf = _ifBranchIndex(0);
	//			//int indexEnd = _endBranchIndex(1);
	//
	//			FragmentType = AdvFragmentType.IfElse;
	//
	//			//if (indexIf == _printLineStart && indexEnd == Lines.Count - 1) {
	//			//	IsPure = true;
	//			//}
	//			//else if (indexEnd != Lines.Count - 1) {
	//			//	FragmentType = AdvFragmentType.NormalExecution;
	//			//}
	//		}
	//		else if (CodeLogic.IsIfBranch(this)) {
	//			//int indexIf = _ifBranchIndex(0);
	//			//int indexEnd = _endBranchIndex(1);
	//
	//			FragmentType = AdvFragmentType.If;
	//
	//			//if (indexIf == _printLineStart && indexEnd == Lines.Count - 1) {
	//			//	IsPure = true;
	//			//}
	//		}
	//		else if (CodeLogic.IsForLoopBranch(this)) {
	//			FragmentType = AdvFragmentType.ForLoop;
	//		}
	//
	//		//if (IsReturn) {
	//		//	if (LineHelper.NoIndent(Lines[Lines.Count - 1]) == "return") {
	//		//		IsPure = true;
	//		//	}
	//		//}
	//	}
	//
	//	public void SetReferences(List<AdvCodeFragment> fragments) {
	//		for (int i = 0; i < Lines.Count; i++) {
	//			if (LineHelper.IsStart(Lines[i], "goto ")) {
	//				string label = LineHelper.GetLabelFromGoto(Lines[i]);
	//				AdvCodeFragment linkedFragment = Common.Fragments[label];
	//
	//				if (linkedFragment != null) {
	//					linkedFragment._addParentReference(this);
	//
	//					if (!ChildReferences.Contains(linkedFragment)) {
	//						ChildReferences.Add(linkedFragment);
	//					}
	//				}
	//			}
	//		}
	//
	//		string currentLine;
	//
	//		for (int i = 1; i < Lines.Count; i++) {
	//			currentLine = LineHelper.NoIndent(Lines[i]);
	//
	//			if (currentLine.StartsWith("if "))
	//				break;
	//
	//			if (currentLine.StartsWith("return"))
	//				break;
	//
	//			if (currentLine.StartsWith("goto "))
	//				break;
	//
	//			if (i == Lines.Count - 1) {
	//				AdvCodeFragment linkedFragment = CodeLogic.GetFragment(fragments, _originalLineIndexEnd + 1);
	//
	//				if (linkedFragment != null) {
	//					linkedFragment._addParentReference(this);
	//					_appendGoto(linkedFragment);
	//					_addChildReference(linkedFragment);
	//				}
	//			}
	//		}
	//
	//		if (Lines.Count == 1) {
	//			// This reference should not be there at all, this is just a bug fix
	//			AdvCodeFragment linkedFragment = CodeLogic.GetFragment(fragments, _originalLineIndexEnd + 1);
	//
	//			if (linkedFragment != null) {
	//				linkedFragment._addParentReference(this);
	//				//_appendGoto(linkedFragment);
	//				_addChildReference(linkedFragment);
	//			}
	//		}
	//
	//		if (FragmentType == AdvFragmentType.ForLoop) {
	//			Break = ChildReferences[1];
	//		}
	//	}
	//
	//	private void _addParentReference(AdvCodeFragment fragment) {
	//		if (!ParentReferences.Contains(fragment)) {
	//			ParentReferences.Add(fragment);
	//		}
	//	}
	//}
}