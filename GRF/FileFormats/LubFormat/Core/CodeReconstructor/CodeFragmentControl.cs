using System;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor {
	public partial class CodeFragment {
		public static void Unlink(CodeFragment parent, CodeFragment child) {
			child.ParentReferences.Remove(parent);
			parent.ChildReferences.Remove(child);
		}

		public static void Link(CodeFragment parent, CodeFragment child) {
			if (!child.ParentReferences.Contains(parent))
				child.ParentReferences.Add(parent);
			if (!parent.ChildReferences.Contains(child))
				parent.ChildReferences.Add(child);
		}

		public static bool IsWithinLoop(CodeFragment forLoop, CodeFragment needle) {
			return needle.PC_Index >= forLoop.Loop_PC_Start && needle.PC_Index <= forLoop.Loop_PC_End;
		}

		public void RemoveElseBranchWithGoto(CodeFragment execution) {
			var elseFragment = Else;

			RemoveElseBranch();
			AppendGoto(execution);

			if (elseFragment.IsDeleted) {
				Unlink(elseFragment, execution);
			}
		}

		public bool RemoveExecutionLine() {
			if (Execution == null)
				return false;

			Unlink(this, Execution);
			Content.RemoveExecutionLine();
			return true;
		}

		public void RemoveElseBranch() {
			var elseFragment = Else;

			if (elseFragment == null)
				return;

			Content.RemoveElseLines();

			Unlink(this, elseFragment);

			switch(FragmentType) {
				case FragmentType.If:
					break;
				case FragmentType.IfElse:
					FragmentType = FragmentType.If;
					break;
				default:
					throw new Exception("Undefined behavior");
			}
		}

		public bool SetIf(CodeFragment fragment) {
			var ifFragment = If;

			if (ifFragment == fragment)
				return false;

			Unlink(this, ifFragment);
			Link(this, fragment);

			Content.RepaceGotoLine(LuaToken.IfGoto, fragment);
			return true;
		}

		public bool SetElse(CodeFragment fragment) {
			var elseFragment = Else;

			if (elseFragment == fragment)
				return false;

			Unlink(this, elseFragment);
			Link(this, fragment);

			Content.RepaceGotoLine(LuaToken.ElseGoto, fragment);
			return true;
		}

		public void AppendGoto(CodeFragment fragment) {
			CodeFragment execution = Execution;

			if (execution != null) {
				RemoveExecutionLine();
			}

			Content.AppendGoto(fragment);
			Link(this, fragment);
		}

		public void ReverseIfElse() {
			IfCondition.Reverse();
			Content.SwapIfElseLines();
		}

		public bool RemoveEmptyGoto(CodeFragment oldFragment, CodeFragment newFragment) {
			Content.RepaceGotoLine(oldFragment, newFragment);

			Unlink(this, oldFragment);
			Link(this, newFragment);

			if (oldFragment.ParentReferences.Count == 0) {
				while (oldFragment.ChildReferences.Count > 0)
					Unlink(oldFragment, oldFragment.ChildReferences[0]);
			}

			if (Break == oldFragment)
				Break = newFragment;
			return true;
		}
	}
}