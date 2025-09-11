using System;
using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.LubFormat.Types;
using Utilities;

namespace GRF.FileFormats.LubFormat.VM {
	public partial class OpCodes {
		public class CodeAnalyser {
			public int PC_Start { get; set; }
			public int PC_End { get; set; }
			public int NonAssign_PC_End { get; set; }
			public RelationalStatement Statement { get; set; }
			public bool Assign { get; set; }

			public class AnalyserResult {
				public int PC_End = -1;
				public int StackIndex = -1;
				public ILubObject Previous;
				public ILubObject Current;
				public VarPosition Var { get; set; }
			};

			public AnalyserResult Result = new AnalyserResult();

			private bool _isComparerInstruction(AbstractInstruction ins) {
				return ins is NewTable ||
				       ins is LoadBool ||
				       ins is LoadK ||
				       ins is GetGlobal ||
				       ins is GetTable ||
				       ins is Jmp ||
				       ins is Not ||
				       ins is MathInstruction ||
				       (ins is Call && ins.Registers[2] == 2);
			}

			public CodeAnalyser(LubFunction function) {
				//return;
				PC_Start = function.PC;
				PC_End = -1;
				Assign = true;
				bool whileLoop = false;

				Dictionary<int, ConditionNode> processed = new Dictionary<int, ConditionNode>();

				int pc = PC_Start;
				int pc_nextIndex = pc;

				var cur_ins = function.Instructions[pc] as ConditionalInstruction;

				if (cur_ins != null && cur_ins.WhileLoop_PC_Start > -1) {
					return;

					//PC_End = cur_ins.WhileLoop_PC_End;
					//Assign = false;
					//whileLoop = true;
				}

				for (; pc < function.Instructions.Count; pc++) {
					if (pc == PC_End)
						break;

					//if (ShouldAssign(this, function, pc) != null) {
					//	PC_End = pc;
					//	break;
					//}

					var ins = function.Instructions[pc];

					if (ins is ConditionalInstruction && !(ins is ILoopInstruction)) {
						var ins_cond = (ConditionalInstruction)ins;

						if (pc != PC_Start && ins_cond.WhileLoop_PC_Start > -1) {
							if (PC_End == -1)
								PC_End = pc;
							break;
						}

						ins_cond.IsInline = false;
						var cond = new ConditionNode(this, pc_nextIndex, pc, ins_cond, function.Instructions[pc + 1] as Jmp, function);
						processed[pc_nextIndex] = cond;
						pc_nextIndex = pc + 2;
						pc++;

						if (cond.Result != null) {
							//if (PC_End == -1)
							//	PC_End = pc + 1;
							break;
						}
					}
					else {
						// Check if it's assigning to a local variable

						if (ins is SetTable ||
						    ins is SetGlobal
							//ShouldAssign(this, function, pc) != null
							) {
							if (PC_End == -1)
								PC_End = pc;
							break;
						}

						if (ins is LoadBool && pc + 1 < function.Instructions.Count && function.Instructions[pc + 1] is LoadBool) {
							if (PC_End == -1)
								PC_End = pc + 2;
							break;
						}

						if (!_isComparerInstruction(ins))
							break;
					}
				}

				if (PC_End == -1) {
					return;
				}

				ConditionNode temporaryFailNode = null;

				foreach (var node in processed.Values) {
					if (node.TrueIndex > PC_End || node.FalseIndex > PC_End) {
						// Inconclusive
						//_setInline(processed, Math.Min(node.TrueIndex, node.FalseIndex), function, null);
						//return;
						temporaryFailNode = node;
						break;
					}
				}

				//_setInline(processed, Math.Min(node.TrueIndex, node.FalseIndex), function, null);
				//return;

				if (temporaryFailNode != null) {
					Assign = false;
					PC_End = -1;
				}

				if (!Assign) {
					NonAssign_PC_End = pc_nextIndex;

					// Valid all
					HashSet<int> validIndexes = new HashSet<int>();

					validIndexes.Add(PC_Start);

					Dictionary<int, ConditionNode> valid = new Dictionary<int, ConditionNode>();
					int ifIndex = -1;
					int elseIndex = -1;

					foreach (var entry in processed) {
						var node = entry.Value;

						int[] indexes = {
							Math.Min(node.TrueIndex, node.FalseIndex),
							Math.Max(node.TrueIndex, node.FalseIndex)
						};

						bool fail = false;
						for (int i = 0; i < 2; i++) {
							if (validIndexes.Contains(indexes[i]))
								continue;

							if (!processed.ContainsKey(indexes[i])) {
								if (ifIndex == -1)
									ifIndex = indexes[i];
								else if (elseIndex == -1)
									elseIndex = indexes[i];
								else {
									fail = true;
									break;
								}

								validIndexes.Add(indexes[i]);
							}
						}

						if (fail)
							break;

						valid[entry.Key] = entry.Value;
					}

					// while loops already have their if/else targets predefined, no need to reassign them
					if (whileLoop) {
						/*
						while tableLen ~= 0 do
							while exe ~= "" do
								key1, key2, des, exe = GetHotKey(tab, idx)
							end
						end
						*/
						// This happens on sub-loops, reverse the output
						if (ifIndex < PC_Start) {
							NonAssign_PC_End = elseIndex; // true
							PC_End = ifIndex; // false
						}
					}
					else {
						if (ifIndex < elseIndex) {
							NonAssign_PC_End = ifIndex; // true
							PC_End = elseIndex; // false
						}
						else {
							NonAssign_PC_End = elseIndex;
							PC_End = ifIndex;
						}
					}

					if (valid.Count != processed.Count) {
						processed = valid;
						var last = processed.Values.Last();
						NonAssign_PC_End = Math.Min(last.TrueIndex, last.FalseIndex);
					}
				}

				// Find parents and add end nodes
				foreach (var node in processed.Values.ToList()) {
					if (!processed.ContainsKey(node.TrueIndex))
						processed[node.TrueIndex] = new ConditionNode(this, node.TrueIndex, function);

					processed[node.TrueIndex].Parents.Add(node);
					node.True = processed[node.TrueIndex];

					if (!processed.ContainsKey(node.FalseIndex))
						processed[node.FalseIndex] = new ConditionNode(this, node.FalseIndex, function);

					processed[node.FalseIndex].Parents.Add(node);
					node.False = processed[node.FalseIndex];
				}

				var nodes = processed.OrderBy(p => p.Key).Select(p => p.Value).ToList();
				var root = processed.First().Value;
				nodes.Remove(root);
				nodes.Insert(0, root);
				Z.F();

				for (int i = 1; i < nodes.Count; i++) {
					while (i > 0 && nodes[i].Analyse()) {
						nodes.RemoveAt(i);
						i--;
					}
				}

				_setInline(processed, PC_Start, function, true);
				Statement = nodes[0].Statement;

				if (nodes[0].True != null && nodes[0].True.B() &&
				    nodes[0].False != null && nodes[0].False.B() &&
				    !nodes[0].True.T()) {
					//if (nodes[0].B() && nodes[0].T() == false) {
					Statement.Reverse();
				}

				if (!Assign)
					function.PC = NonAssign_PC_End - 2;
				else
					function.PC = PC_End - 1;
			}

			private void _setInline(Dictionary<int, ConditionNode> processed, int start, LubFunction function, bool? value) {
				foreach (var pc in processed.Keys) {
					if (pc < start)
						continue;

					var ins = processed[pc].Ins_Cond;

					if (ins != null)
						ins.IsInline = value;
				}
			}
		}
	}
}