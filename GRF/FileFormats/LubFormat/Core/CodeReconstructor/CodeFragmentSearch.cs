using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GRF.FileFormats.LubFormat.Core.CodeReconstructor {
	public partial class CodeFragment {
		public bool AllLeadsTo(CodeFragment fragment, int maxUid = -1) {
			var search = new FragmentSearch { ToFind = fragment, Start = this, OrReturn = false, MaxUid = maxUid };
			search.Result = this._findFragment(search, null, 0);
			return search.Result;
		}

		public bool AllLeadsToOrReturn(CodeFragment fragment, int maxUid = -1) {
			var search = new FragmentSearch { ToFind = fragment, Start = this, OrReturn = true, MaxUid = maxUid };
			search.Result = this._findFragment(search, null, 0);
			return search.Result;
		}

		public class FragmentSearch {
			public bool OrReturn { get; set; }
			public CodeFragment ToFind { get; set; }
			public CodeFragment Start { get; set; }
			public int MaxUid { get; set; }

			public ICollection<CodeFragment> ProcessedFragments = new List<CodeFragment>();
			public int MaxLevel { get; set; }
			public bool Result { get; set; }

			public FragmentSearch() {
				MaxUid = -1;
			}
		}

		private bool _findFragment(FragmentSearch search, CodeFragment parent, int level) {
			search.MaxLevel = Math.Max(level, search.MaxLevel);

			if (search.ProcessedFragments.Contains(this))
				return _leads != null && _leads.Value;

			_leads = null;

			search.ProcessedFragments.Add(this);

			if (level > 20) {
				_leads = false;
				return false;
			}

			if (this == search.ToFind) {
				_leads = true;
				return true;
			}

			if (search.MaxUid != -1 && Uid >= search.MaxUid) {
				// Break acts as a return if the search is within the break loop
				if (search.OrReturn && IsBreakTarget &&
					parent != null &&
					parent.LoopScope != null &&
					parent.LoopScope.Break == this &&
					parent.LoopScope == search.Start.LoopScope) {
					_leads = true;
					return true;
				}

				_leads = false;
				return false;
			}

			if (ChildReferences.Count == 0) {
				if (search.ToFind == null || search.OrReturn) {
					_leads = true;
					return true;
				}

				_leads = false;
				return false;
			}

			if (IsLoop) {
				// Only skip the loop if the parent was outside the loop
				if (parent != null && IsWithinLoop(this, parent)) {
					if (search.ToFind == Break) {
						_leads = false;
						return false;
					}
				}

				return Break._findFragment(search, this, level + 1);
			}

			var result = ChildReferences.All(p => p._findFragment(search, this, level + 1));
			_leads = result;
			return result;
		}
	}
}
