using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;

namespace GRFEditor.Core.Avalon {
	public class RegexSearchStrategy : ISearchStrategy {
		private readonly bool _matchWholeWords;
		private readonly Regex _searchPattern;

		public RegexSearchStrategy(Regex searchPattern, bool matchWholeWords) {
			if (searchPattern == null)
				throw new ArgumentNullException("searchPattern");
			_searchPattern = searchPattern;
			_matchWholeWords = matchWholeWords;
		}

		#region ISearchStrategy Members

		public IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length) {
			int endOffset = offset + length;
			foreach (Match result in _searchPattern.Matches(document.Text)) {
				int resultEndOffset = result.Length + result.Index;
				if (offset > result.Index || endOffset < resultEndOffset)
					continue;
				if (_matchWholeWords && (!AvalonHelper.IsWordBorder(document, result.Index) || !AvalonHelper.IsWordBorder(document, resultEndOffset)))
					continue;
				yield return new SearchResult { StartOffset = result.Index, Length = result.Length, Data = result };
			}
		}

		public ISearchResult FindNext(ITextSource document, int offset, int length) {
			return FindAll(document, offset, length).FirstOrDefault();
		}

		public bool Equals(ISearchStrategy other) {
			return other == this;
		}

		#endregion
	}
}