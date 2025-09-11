using System;
using System.Collections.Generic;
using System.Linq;
using ErrorManager;
using GRF.FileFormats.LubFormat.Core.CodeReconstructor;
using Utilities;

namespace GRF.FileFormats.LubFormat.Core {
	public static class CodeLogic {
		public static List<CodeFragment> Analyse(List<string> lines, int level, LubFunction function) {
			_removeConsecutiveGotos(lines);

			List<CodeFragment> fragments = new List<CodeFragment>();
			var common = new TkDictionary<string, CodeFragment>();
			int splitCounter = 0;

			// The first iteration creates the fragments, the references
			// aren't set up yet.
			for (int i = 0; i < lines.Count; i++) {
				try {
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

						for (int j = fragmentStart + 1; j < fragmentEnd; j++) {
							// Not pure!
							if (lines[j].Contains(" function(")) {
								int end = 1;
								j++;

								for (; j < lines.Count; j++) {
									if (lines[j].Contains("\tend") || lines[j] == "end")
										end--;
									else if (
										lines[j].Contains("\tfor ") ||
										lines[j].Contains("\tif ") ||
										lines[j].Contains("\twhile "))
										end++;

									if (end <= 0)
										break;
								}

								j--;
								continue;
							}

							if ((j >= fragmentStart + 2 && LineHelper.IsIf(lines[j])) || (j > fragmentStart + 2 && LineHelper.IsControl(lines[j]))) {
								string newLabel = "e_[" + String.Format("9{0:0000}", splitCounter++) + "]";

								if (lines[fragmentStart].StartsWith("::")) {
									newLabel = "e_" + lines[fragmentStart].Replace("::", "").Split('_')[1];
								}

								lines.Insert(j, LineHelper.GenerateIndent(function.BaseIndent) + "goto " + newLabel);
								lines.Insert(j + 1, "::" + newLabel + "::");
								fragmentEnd = i = j;
								break;
							}
						}

						CodeFragment fragment = new CodeFragment(fragmentStart, fragmentEnd, lines, common);
						fragments.Add(fragment);
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			fragments.ForEach(p => p.SetReferences(fragments));
			_rearrangeWhileLoops(fragments, function);
			return fragments;
		}

		private static void _rearrangeWhileLoops(List<CodeFragment> fragments, LubFunction function) {
			HashSet<CodeFragment> processed = new HashSet<CodeFragment>();

			// Fix while loops
			for (int i = 0; i < fragments.Count; i++) {
				var fragment = fragments[i];

				if (processed.Add(fragment) && fragment.IsLoop) {
					int whileIndex = fragment.Content.IndexOf(LuaToken.While);

					if (whileIndex < 0)
						continue;

					var oldLabel = fragment.Content.Label;
					var newLabel = function.Label + "_[" + fragment.Loop_PC_End + "]";

					fragment.Content.Lines[0] = "::" + newLabel + "::";
					fragment.Content.Label = newLabel;
					fragment.PC_Index = fragment.Loop_PC_End;

					foreach (var frag in fragment.ParentReferences) {
						int location = frag.Content.GetGotoLineIndex(oldLabel);

						if (location > -1)
							frag.Content.Lines[location] = LineHelper.ReplaceAfterIndent(frag.Content.Lines[location], "goto " + newLabel);
					}

					if (function._decompiler.Header.Version >= 5.1)
						fragment.Loop_PC_Start = fragments[i + 1].PC_Index;

					fragments.Remove(fragment);

					int j;

					for (j = 0; j < fragments.Count; j++) {
						if (fragments[j].PC_Index >= 90000)
							continue;
						if (fragments[j].PC_Index > fragment.PC_Index) {
							fragments.Insert(j, fragment);
							break;
						}
					}

					if (j == fragments.Count)
						fragments.Add(fragment);

					for (j = 0; j < fragments.Count; j++) {
						fragments[j].NewUid();
					}

					i--;
				}
			}

			// Set fragments associated with loops
			for (int i = 0; i < fragments.Count; i++) {
				var fragmentLoop = fragments[i];

				if (fragmentLoop.IsLoop) {
					for (int j = i - 1; j >= 0; j--) {
						if (fragments[j].LoopScope != null)
							continue;

						if (CodeFragment.IsWithinLoop(fragmentLoop, fragments[j])) {
							fragments[j].LoopScope = fragmentLoop;
							continue;
						}

						break;
					}
				}
			}
		}

		private static void _removeConsecutiveGotos(List<string> lines) {
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
			return fragments.FirstOrDefault(p => p.Content.ContainsLineIndex(line));
		}
		#endregion
	}
}