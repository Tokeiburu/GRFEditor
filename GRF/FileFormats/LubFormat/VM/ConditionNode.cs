using System;
using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.LubFormat.Types;
using Utilities;

namespace GRF.FileFormats.LubFormat.VM {
	public partial class OpCodes {
		public class ConditionNode {
			public RelationalStatement Statement;
			public int PC_Start { get; set; }
			public int TrueIndex { get; set; }
			public int FalseIndex { get; set; }
			public ILubObject Result { get; set; }
			public HashSet<ConditionNode> Parents = new HashSet<ConditionNode>();
			public ConditionalInstruction Ins_Cond;

			public bool IsTerminated {
				get { return (True == null && False == null) || (True != null && True.B() && False != null && False.B()); }
			}

			public ConditionNode True { get; set; }
			public ConditionNode False { get; set; }
			public bool Root { get; set; }

			public LubStack Stack = new LubStack();

			public ConditionNode(CodeAnalyser analyser, int pc, LubFunction function) {
				PC_Start = pc;

				try {
					function.Stack.Push();

					for (; pc < function.Instructions.Count; pc++) {
						if (PC_Start == pc && (pc == analyser.PC_End || pc == analyser.NonAssign_PC_End)) {
							if (!analyser.Assign) {
								Result = new LubBoolean(pc == analyser.NonAssign_PC_End);
								break;
							}

							Result = new LubNull();
							break;
						}

						var ins = function.Instructions[pc];
						VarPosition pos;

						if (ins is SetTable || ins is SetGlobal) {
							if (ins is SetGlobal) {
								Result = GetKey(RegOutput(ins.Registers[0], function));
								Statement = new RelationalStatement(Result.ToString());
							}
							else {
								Result = GetKey(RegOrKOutput(ins.Registers[2], function));
								Statement = new RelationalStatement(Result.ToString());
							}

							break;
						}

						if ((pos = ShouldAssign(function, pc)) != null) {
							if (analyser.Result.Var == null)
								analyser.Result.Var = pos;
							else if (analyser.Result.Var != pos)
								throw new Exception("ConditionNode: Assigning different variables for inline conditional statement");

							Result = GetKey(RegOutput(pos.StackIndex, function));
							Statement = new RelationalStatement(Result.ToString());
							break;
						}

						if (pc == analyser.PC_End) {
							Z.F();
						}

						ins.Execute(function);

						if (ins is LoadBool && ins.Registers[2] == 1) {
							pc++;
						}
						else if (ins is Jmp) {
							pc += ins.Registers[0];
						}
					}
				}
				finally {
					function.Stack.Pop();
				}
			}

			public ConditionNode(CodeAnalyser analyser, int pc_b, int pc, ConditionalInstruction ins_cond, Jmp ins_jmp, LubFunction function) {
				PC_Start = pc_b;
				Ins_Cond = ins_cond;
				Root = analyser.PC_Start == PC_Start;

				for (int pc_start = pc_b; pc_start < pc; pc_start++) {
					function.Instructions[pc_start].Execute(function);
				}

				Statement = new RelationalStatement(ins_cond.GetPositiveCondition(function));

				TrueIndex = pc + 2;
				FalseIndex = ins_jmp.Registers[0] + pc + 2;

				bool reverse = false;

				if (ins_cond is Test) {
					if (ins_cond.Registers[2] == 1)
						reverse = true;
				}
				else if (ins_cond is TestSet) {
					if (ins_cond.Registers[2] == 1)
						reverse = true;
				}
				else if (ins_cond.Registers[0] == 1)
					reverse = true;

				if (ins_cond is TestSet) {
					// Only conditional instruction that assigns at the same time
					//function.Stack[ins_cond.Registers[0]] = GetKey(RegOrKOutput(ins_cond.Registers[1], function));

					//int stackIndex = ins_cond.Registers[0];
					//if (analyser.PC_End == -1)
					//	analyser.PC_End = Math.Min(TrueIndex, FalseIndex);
					//Result = GetKey(RegOrKOutput(ins_cond.Registers[1], function));

					//analyser.Result.Previous = function.Stack.GetPopValue(stackIndex);
					//analyser.Result.Current = function.Stack[stackIndex];
				}

				if (reverse) {
					int t = TrueIndex;
					TrueIndex = FalseIndex;
					FalseIndex = t;
				}
			}

			public bool N() {
				return Result is LubNull;
			}

			public bool C() {
				return Result != null && !(Result is LubNull) && !(Result is LubBoolean);
			}

			public bool B() {
				return Result is LubBoolean;
			}

			public bool T() {
				return Result is LubBoolean && ((LubBoolean)Result).Value;
			}

			public bool F() {
				return Result is LubBoolean && !((LubBoolean)Result).Value;
			}

			public bool Analyse() {
				if (Parents.Count == 0)
					return true;

				if (Parents.Count != 1)
					return false;

				// Test all possible combinations!
				var parent = Parents.First();

				if (B()) {
					var r = T();

					if (r && parent.True == this)
						return false;

					if (!r && parent.False == this)
						return false;

					// ?? Though isn't that always the case for booleans?
					if (this.Statement == null)
						return false;
				}

				if (parent.True == this && parent.False != null) {
					// a == 1 and b == 2
					if (parent.False == False && !B()) {
						_combine(parent, this, ConditionToken.And);
						Unlink(parent, this, True);
						return true;
					}

					if (IsTerminated) {
						// a == 1 and g
						if (parent.False.F()) {
							_combine(parent, this, ConditionToken.And);
							parent.SetTerminated();
							return true;
						}

						// not(a == 1 and b == 2) or g
						if (parent.False.T()) {
							parent.Statement.Reverse();
							_combine(parent, this, ConditionToken.Or);
							parent.SetTerminated();
							return true;
						}

						// a and b
						if (parent.False.N()) {
							_combine(parent, this, ConditionToken.And);
							parent.SetTerminated();
							return true;
						}
					}
					Z.F();
				}

				if (parent.False == this && parent.True != null) {
					// a == 1 or b == 2
					if (parent.True == True && !B()) {
						_combine(parent, this, ConditionToken.Or);
						Unlink(parent, this, False);
						return true;
					}

					if (IsTerminated) {
						// a == 1 or g
						if (parent.True.T()) {
							_combine(parent, this, ConditionToken.Or);
							parent.SetTerminated();
							return true;
						}

						// not(a == 1 and b == 2) and g
						if (parent.True.F()) {
							parent.Statement.Reverse();
							_combine(parent, this, ConditionToken.And);
							parent.SetTerminated();
							return true;
						}

						// a or b
						if (parent.True.N()) {
							_combine(parent, this, ConditionToken.Or);
							parent.SetTerminated();
							return true;
						}
					}
					Z.F();
				}

				return false;
			}

			private void SetTerminated() {
				if (True != null)
					True.Parents.Remove(this);
				if (False != null)
					False.Parents.Remove(this);

				True = null;
				False = null;
			}

			private void Unlink(ConditionNode parent, ConditionNode current, ConditionNode newLink) {
				if (True != null)
					True.Parents.Remove(current);
				if (False != null)
					False.Parents.Remove(current);
				if (current != null)
					current.Parents.Remove(parent);

				if (newLink != null)
					newLink.Parents.Add(parent);

				if (parent.True == current)
					parent.True = newLink;

				if (parent.False == current)
					parent.False = newLink;
			}

			private void _combine(ConditionNode left, ConditionNode right, ConditionToken token) {
				left.Statement.Combine(right.Statement, token);
			}

			public override string ToString() {
				if (IsTerminated)
					return "#" + PC_Start + "; " + Result + (Statement == null ? "" : "; " + Statement);

				return "#" + PC_Start + "; L> " + True.PC_Start + ", R> " + False.PC_Start + "; " + Statement;
			}
		}
	}
}