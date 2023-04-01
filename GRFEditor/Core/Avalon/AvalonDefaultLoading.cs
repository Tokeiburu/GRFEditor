using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using TokeiLibrary;
using Utilities.Extension;
using SearchPanel = GRFEditor.WPF.SearchPanel;

namespace GRFEditor.Core.Avalon {
	public class AvalonDefaultLoading {
		private readonly object _lock = new object();
		private readonly List<string> _toIgnore = new List<string> { "\t", Environment.NewLine, "\n", "\r", " ", ",", ".", "!", "\"", "?" };
		private string _currentWord;
		private SearchPanel.SearchResultBackgroundRenderer _renderer;
		private TextArea _textArea;
		private TextEditor _textEditor;

		public void Attach(TextEditor editor) {
			_textEditor = editor;
			_loadAvalon();
		}

		private void _loadAvalon() {
			DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
			foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
			foldingUpdateTimer.Start();

			_textEditor.Foreground = Application.Current.Resources["TextForeground"] as Brush;
			_textEditor.Background = Application.Current.Resources["AvalonEditorBackground"] as Brush;
			_textEditor.Dispatch(p => p.TextArea.SelectionCornerRadius = 0);
			_textEditor.Dispatch(p => p.TextArea.SelectionBorder = new Pen(_textEditor.TextArea.SelectionBrush, 0));
			_textEditor.TextArea.SelectionBrush = Application.Current.Resources["AvalonEditorSelectionBrush"] as Brush;
			_textEditor.TextArea.SelectionBorder = new Pen(_textEditor.TextArea.SelectionBrush, 1);
			_textEditor.TextArea.SelectionForeground = new SolidColorBrush(Colors.Black);
			SearchPanel panel = new SearchPanel();
			panel.Attach(_textEditor.TextArea, _textEditor);

			FontFamily oldFamily = _textEditor.FontFamily;
			double oldSize = _textEditor.FontSize;

			_renderer = new SearchPanel.SearchResultBackgroundRenderer {
				MarkerBrush = Application.Current.Resources["AvalonEditorMarkerBrush"] as Brush
			};
			_textEditor.TextArea.Caret.PositionChanged += _caret_PositionChanged;

			try {
				_textEditor.FontFamily = new FontFamily("Consolas");
				_textEditor.FontSize = 12;

				if (_textEditor.FontFamily == null) {
					_textEditor.FontFamily = oldFamily;
					_textEditor.FontSize = oldSize;
				}
			}
			catch {
				_textEditor.FontFamily = oldFamily;
				_textEditor.FontSize = oldSize;
			}

			_textEditor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
			_textEditor.TextArea.KeyDown += new KeyEventHandler(_textArea_KeyDown);
			_textArea = _textEditor.TextArea;
		}

		private void _textArea_KeyDown(object sender, KeyEventArgs e) {
			if (Keyboard.Modifiers.HasFlags(ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Up) {
				FindPrevious();
				e.Handled = true;
			}

			if (Keyboard.Modifiers.HasFlags(ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Down) {
				FindNext();
				e.Handled = true;
			}
		}

		public void FindNext() {
			SearchResult result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset + 1) ?? _renderer.CurrentResults.FirstSegment;
			if (result != null) {
				SelectResult(result);
			}
		}

		public void FindPrevious() {
			SearchResult result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset);
			if (result != null)
				result = _renderer.CurrentResults.GetPreviousSegment(result);
			if (result == null)
				result = _renderer.CurrentResults.LastSegment;

			if (result != null) {
				if (result.StartOffset <= _textArea.Caret.Offset && _textArea.Caret.Offset <= result.EndOffset) {
					// We find the previous again, just to make sure

					result = _renderer.CurrentResults.GetPreviousSegment(result);

					if (result == null)
						result = _renderer.CurrentResults.LastSegment;
				}
			}

			if (result != null) {
				SelectResult(result);
			}
		}

		public void SelectResult(SearchResult result) {
			_textEditor.TextArea.Caret.PositionChanged -= _caret_PositionChanged;
			_textArea.Caret.Offset = result.StartOffset;
			_textArea.Selection = Selection.Create(_textArea, result.StartOffset, result.EndOffset);
			_textArea.Caret.BringCaretToView();
			_textEditor.TextArea.Caret.PositionChanged += _caret_PositionChanged;
		}

		private void _caret_PositionChanged(object sender, EventArgs e) {
			try {
				string currentWord = AvalonHelper.GetWholeWord(_textEditor.TextArea.Document, _textEditor);

				if (_textEditor.CaretOffset > 0) {
					if (currentWord.Length <= 0 || !char.IsLetterOrDigit(currentWord[0])) {
						foreach (char c in new char[] { '{', '}', '(', ')', '[', ']' }) {
							if (_isBefore(c) || _isAfter(c)) {
								currentWord = "" + c;
							}
						}
					}
				}

				if (_currentWord != currentWord) {
					_renderer.CurrentResults.Clear();
					_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
				}

				_currentWord = currentWord;
				_updateCurrentWord(currentWord);
			}
			catch {
			}
		}

		private bool _isBefore(char c) {
			return _textEditor.Text[_textEditor.CaretOffset - 1] == c;
		}

		private bool _isAfter(char c) {
			if (_textEditor.CaretOffset >= _textEditor.Text.Length) return false;
			return _textEditor.Text[_textEditor.CaretOffset] == c;
		}

		private int _findBefore(char back, char forward, int indexStart, int current) {
			for (int i = indexStart; current != 0 && i > -1; i--) {
				if (_textEditor.Text[i] == forward)
					current++;
				else if (_textEditor.Text[i] == back)
					current--;

				if (current == 0)
					return i;
			}

			return -1;
		}

		private int _findAfter(char back, char forward, int indexStart, int current) {
			for (int i = indexStart; current != 0 && i < _textEditor.Text.Length; i++) {
				if (_textEditor.Text[i] == forward)
					current--;
				else if (_textEditor.Text[i] == back)
					current++;

				if (current == 0)
					return i;
			}

			return -1;
		}

		private bool _bracketMatch(char current, char back, char forward) {
			if (current == forward && (_isAfter(current) || _isBefore(current))) {
				_renderer.CurrentResults.Clear();

				if (_isAfter(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset, Length = 1 });

					int before = _findBefore(back, forward, _textEditor.CaretOffset - 1, 1);

					if (before >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = before, Length = 1 });
					}
				}

				if (_isBefore(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset - 1, Length = 1 });

					int before = _findBefore(back, forward, _textEditor.CaretOffset - 2, 1);

					if (before >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = before, Length = 1 });
					}
				}

				_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
				return true;
			}

			if (current == back && (_isAfter(current) || _isBefore(current))) {
				_renderer.CurrentResults.Clear();

				if (_isBefore(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset - 1, Length = 1 });

					int after = _findAfter(back, forward, _textEditor.CaretOffset, 1);

					if (after >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = after, Length = 1 });
					}
				}

				if (_isAfter(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset, Length = 1 });

					int after = _findAfter(back, forward, _textEditor.CaretOffset + 1, 1);

					if (after >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = after, Length = 1 });
					}
				}

				_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
				return true;
			}

			return false;
		}

		private void _updateCurrentWord(string currentWord) {
			_renderer.CurrentResults.Clear();

			if (currentWord == null || _textEditor.CaretOffset == 0 || _toIgnore.Any(p => currentWord.Contains(p))) {
				return;
			}

			if (currentWord.Length == 1) {
				char current = currentWord[0];

				if (_bracketMatch(current, '{', '}')) return;
				if (_bracketMatch(current, '(', ')')) return;
				if (_bracketMatch(current, '[', ']')) return;
			}

			if (currentWord == ">") {
				int offsetPrevious = _textEditor.Text.LastIndexOf('<', _textEditor.CaretOffset);
				int offsetNext = _textEditor.Text.IndexOf('>', offsetPrevious + 1) + 1;

				if (offsetNext != _textEditor.CaretOffset || offsetNext <= offsetPrevious) {
					_renderer.CurrentResults.Clear();
					return;
				}

				currentWord = _textEditor.Text.Substring(offsetPrevious, offsetNext - offsetPrevious);
				_currentWord = currentWord;
			}

			if (currentWord == "<") {
				int offsetNext = _textEditor.Text.IndexOf('>', _textEditor.CaretOffset);
				int offsetPrevious = _textEditor.Text.LastIndexOf('<', offsetNext - 1);

				if (offsetPrevious != _textEditor.CaretOffset || offsetNext <= offsetPrevious) {
					_renderer.CurrentResults.Clear();
					return;
				}

				offsetNext++;
				currentWord = _textEditor.Text.Substring(offsetPrevious, offsetNext - offsetPrevious);
				_currentWord = currentWord;
			}

			new Thread(new ThreadStart(delegate {
				lock (_lock) {
					try {
						if (currentWord != _currentWord)
							return;

						Thread.Sleep(300);

						if (currentWord != _currentWord)
							return;

						Regex pattern = new Regex(Regex.Escape(currentWord), RegexOptions.Compiled);
						RegexSearchStrategy strategy = new RegexSearchStrategy(pattern, true);

						_textEditor.Dispatch(delegate {
							try {
								_renderer.CurrentResults.Clear();

								if (!string.IsNullOrEmpty(currentWord)) {
									// We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
									foreach (SearchResult result in strategy.FindAll(_textArea.Document, 0, _textArea.Document.TextLength)) {
										_renderer.CurrentResults.Add(result);
									}
								}
								_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
							}
							catch {
							}
						});
					}
					catch (ArgumentException ex) {
						throw new SearchPatternException(ex.Message, ex);
					}
				}
			})).Start();
		}
	}
}