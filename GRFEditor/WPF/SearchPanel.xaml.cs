using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using TokeiLibrary;
using Utilities;

namespace GRFEditor.WPF {
	public partial class SearchPanel : UserControl {
		public static readonly DependencyProperty MatchCaseProperty =
			DependencyProperty.Register("MatchCase", typeof (bool), typeof (SearchPanel),
			                            new FrameworkPropertyMetadata(false, _searchPatternChangedCallback));

		public static readonly DependencyProperty SearchPatternProperty =
			DependencyProperty.Register("SearchPattern", typeof (string), typeof (SearchPanel),
			                            new FrameworkPropertyMetadata("", _searchPatternChangedCallback));

		public static readonly DependencyProperty WholeWordsProperty =
			DependencyProperty.Register("WholeWords", typeof (bool), typeof (SearchPanel),
			                            new FrameworkPropertyMetadata(false, _searchPatternChangedCallback));

		public static readonly DependencyProperty UseRegexProperty =
			DependencyProperty.Register("UseRegex", typeof (bool), typeof (SearchPanel),
			                            new FrameworkPropertyMetadata(false, _searchPatternChangedCallback));

		public static readonly RoutedCommand Find = new RoutedCommand(
			"Find", typeof (SearchPanel),
			new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control) }
			);

		private readonly ToolTip _messageView = new ToolTip { Placement = PlacementMode.Bottom, StaysOpen = false };
		private SearchPanelAdorner _adorner;

		private TextDocument _currentDocument;
		private TextEditor _editor;
		private SearchResultBackgroundRenderer _renderer;
		private ISearchStrategy _strategy;
		private TextArea _textArea;

		public SearchPanel() {
			InitializeComponent();

			_isSearch = true;

			CommandManager.AddPreviewCanExecuteHandler(
				this,
				new CanExecuteRoutedEventHandler(OnPreviewCanExecuteHandler));

			CommandManager.AddPreviewExecutedHandler(
				this,
				new ExecutedRoutedEventHandler(OnPreviewExecutedEvent));
		}

		public bool IsClosed { get; set; }

		public string SearchPattern {
			get { return (string) GetValue(SearchPatternProperty); }
			set { SetValue(SearchPatternProperty, value); }
		}

		public bool MatchCase {
			get { return (bool) GetValue(MatchCaseProperty); }
			set { SetValue(MatchCaseProperty, value); }
		}

		public bool WholeWords {
			get { return (bool) GetValue(WholeWordsProperty); }
			set { SetValue(WholeWordsProperty, value); }
		}

		public bool UseRegex {
			get { return (bool) GetValue(UseRegexProperty); }
			set { SetValue(UseRegexProperty, value); }
		}

		private bool _isSearch { get; set; }

		private static void _searchPatternChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			SearchPanel panel = d as SearchPanel;
			if (panel != null) {
				panel.ValidateSearchText();
				panel.UpdateSearch();
			}
		}

		public void OnPreviewCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e) {
			if (e.Command == ApplicationCommands.Undo) {
				e.CanExecute = true;
				e.Handled = true;
			}
			else if (e.Command == ApplicationCommands.Redo) {
				e.CanExecute = true;
				e.Handled = true;
			}
		}

		public void OnPreviewExecutedEvent(object sender, ExecutedRoutedEventArgs e) {
			if (e.Command == ApplicationCommands.Undo) {
				_editor.Undo();
				e.Handled = true;
			}
			else if (e.Command == ApplicationCommands.Redo) {
				_editor.Redo();
				e.Handled = true;
			}
		}

		public void ValidateSearchText() {
			if (_searchTextBox == null)
				return;
			var be = _searchTextBox.GetBindingExpression(TextBox.TextProperty);
			try {
				Validation.ClearInvalid(be);
				UpdateSearch();
			}
			catch (SearchPatternException ex) {
				var ve = new ValidationError(be.ParentBinding.ValidationRules[0], be, ex.Message, ex);
				Validation.MarkInvalid(be, ve);
			}
		}

		public void Attach(TextArea textArea, TextEditor editor) {
			if (textArea == null)
				throw new ArgumentNullException("textArea");

			_editor = editor;
			_textArea = textArea;
			_currentDocument = textArea.Document;
			_renderer = new SearchResultBackgroundRenderer { MarkerBrush = new SolidColorBrush(Colors.Yellow) };
			_searchTextBox.TextChanged += new TextChangedEventHandler(_searchTextBox_TextChanged);

			_adorner = new SearchPanelAdorner(textArea, this);

			if (_currentDocument != null)
				_currentDocument.TextChanged += new EventHandler(_currentDocument_TextChanged);

			_textArea.DocumentChanged += new EventHandler(_textArea_DocumentChanged);
			_textArea.PreviewKeyDown += new KeyEventHandler(_textArea_PreviewKeyDown);
			_searchTextBox.LostFocus += new RoutedEventHandler(_searchTextBox_LostFocus);
			_replaceTextBox.LostFocus += new RoutedEventHandler(_replaceTextBox_LostFocus);
			_searchTextBox.GotFocus += new RoutedEventHandler(_searchTextBox_GotFocus);
			_replaceTextBox.GotFocus += new RoutedEventHandler(_replaceTextBox_GotFocus);
			KeyDown += new KeyEventHandler(_searchPanel_KeyDown);

			CommandBindings.Add(new CommandBinding(Find, (sender, e) => Open()));
			CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
			CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
			CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));

			textArea.CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
			textArea.CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
			textArea.CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));
			IsClosed = true;
		}

		private void _replaceTextBox_GotFocus(object sender, RoutedEventArgs e) {
			_labelReplace.Visibility = Visibility.Hidden;
			_border2.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 186, 86, 0));
			_replaceTextBox.Foreground = Application.Current.Resources["TextForeground"] as Brush;
			_searchTextBox.Foreground = Application.Current.Resources["TextForeground"] as Brush;
		}

		private void _searchTextBox_GotFocus(object sender, RoutedEventArgs e) {
			_labelFind.Visibility = Visibility.Hidden;
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 186, 86, 0));
			_replaceTextBox.Foreground = Application.Current.Resources["TextForeground"] as Brush;
			_searchTextBox.Foreground = Application.Current.Resources["TextForeground"] as Brush;
		}

		private void _replaceTextBox_LostFocus(object sender, RoutedEventArgs e) {
			_border2.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_replaceTextBox.Text == "") {
				_labelReplace.Visibility = Visibility.Visible;
			}
			else {
				_labelReplace.Visibility = Visibility.Hidden;
				_searchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
				_replaceTextBox.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}

		private void _searchTextBox_LostFocus(object sender, RoutedEventArgs e) {
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_searchTextBox.Text == "") {
				_labelFind.Visibility = Visibility.Visible;
			}
			else {
				_labelFind.Visibility = Visibility.Hidden;
				_searchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
				_replaceTextBox.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}

		private void _searchTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			SearchPattern = _searchTextBox.Text;
			//UpdateSearch();
		}

		private void _textArea_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.F) {
				_isSearch = false;
				_buttonFancyMode_Click(sender, null);
				Open();
				_searchTextBox.SelectAll();
				_searchTextBox.Focus();
				e.Handled = true;
			}

			if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.H) {
				_isSearch = true;
				_buttonFancyMode_Click(sender, null);
				Open();
				_searchTextBox.SelectAll();
				_searchTextBox.Focus();
				e.Handled = true;
			}
		}

		public void Open() {
			string selectedText = _textArea.Selection.GetText();

			if (!String.IsNullOrEmpty(selectedText)) {
				_searchTextBox.Text = _cut(selectedText);
				_searchTextBox.SelectAll();
				_searchTextBox.Focus();
			}

			if (!IsClosed) return;

			var layer = AdornerLayer.GetAdornerLayer(_textArea);
			if (layer != null) {
				layer.Add(_adorner);
				_searchTextBox.SelectAll();
				_searchTextBox.Focus();

				new Thread(new ThreadStart(delegate {
					Thread.Sleep(100);
					_searchTextBox.Dispatch(p => p.SelectAll());
					_searchTextBox.Dispatch(p => p.Focus());
				})).Start();
			}

			_textArea.TextView.BackgroundRenderers.Add(_renderer);
			IsClosed = false;
			DoSearch(false);
		}

		private string _cut(string selectedText) {
			if (selectedText.Contains(Environment.NewLine)) {
				selectedText = selectedText.Substring(0, selectedText.IndexOf(Environment.NewLine, StringComparison.Ordinal));
			}

			if (selectedText.Contains('\r')) {
				selectedText = selectedText.Substring(0, selectedText.IndexOf('\r'));
			}

			if (selectedText.Contains('\n')) {
				selectedText = selectedText.Substring(0, selectedText.IndexOf('\n'));
			}

			return selectedText;
		}

		private void _currentDocument_TextChanged(object sender, EventArgs e) {
			DoSearch(false);
		}

		public void UpdateSearch() {
			// only reset as long as there are results
			// if no results are found, the "no matches found" message should not flicker.
			// if results are found by the next run, the message will be hidden inside DoSearch ...
			if (_renderer.CurrentResults.Any())
				_messageView.IsOpen = false;

			try {
				_strategy = SearchStrategyFactory.Create(SearchPattern ?? "", !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
				OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern, MatchCase, UseRegex, WholeWords));
				DoSearch(true);
			}
			catch {
			}
		}

		public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

		protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e) {
			if (SearchOptionsChanged != null) {
				SearchOptionsChanged(this, e);
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
			_textArea.Caret.Offset = result.StartOffset;
			_textArea.Selection = Selection.Create(_textArea, result.StartOffset, result.EndOffset);
			_textArea.Caret.BringCaretToView();
			// show caret even if the editor does not have the Keyboard Focus
			//_textArea.Caret.Show();
		}

		private void _searchPanel_KeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Enter:
					e.Handled = true;
					_messageView.IsOpen = false;
					if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
						FindPrevious();
					else
						FindNext();
					if (_searchTextBox != null) {
						var error = Validation.GetErrors(_searchTextBox).FirstOrDefault();
						if (error != null) {
							_messageView.Content = "Found errors : " + " " + error.ErrorContent;
							_messageView.PlacementTarget = _searchTextBox;
							_messageView.IsOpen = true;
						}
					}
					break;
				case Key.Escape:
					e.Handled = true;
					Close();
					break;
			}
		}

		public void CloseAndRemove() {
			Close();
			_textArea.DocumentChanged -= _textArea_DocumentChanged;
			if (_currentDocument != null)
				_currentDocument.TextChanged -= _currentDocument_TextChanged;
		}

		public void Close() {
			bool hasFocus = IsKeyboardFocusWithin;

			var layer = AdornerLayer.GetAdornerLayer(_textArea);
			if (layer != null)
				layer.Remove(_adorner);

			_messageView.IsOpen = false;
			_textArea.TextView.BackgroundRenderers.Remove(_renderer);
			if (hasFocus)
				_textArea.Focus();
			IsClosed = true;

			// Clear existing search results so that the segments don't have to be maintained
			_renderer.CurrentResults.Clear();
		}

		private void _textArea_DocumentChanged(object sender, EventArgs e) {
			if (_currentDocument != null)
				_currentDocument.TextChanged -= _currentDocument_TextChanged;

			_currentDocument = _textArea.Document;

			if (_currentDocument != null) {
				_currentDocument.TextChanged += _currentDocument_TextChanged;
				DoSearch(false);
			}
		}

		public void DoSearch(bool changeSelection) {
			if (IsClosed)
				return;

			_renderer.CurrentResults.Clear();

			if (!string.IsNullOrEmpty(SearchPattern)) {
				int offset = _textArea.Caret.Offset;
				if (changeSelection) {
					_textArea.ClearSelection();
				}
				// We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
				foreach (SearchResult result in _strategy.FindAll(_textArea.Document, 0, _textArea.Document.TextLength)) {
					if (changeSelection && result.StartOffset >= offset) {
						SelectResult(result);
						changeSelection = false;
					}
					_renderer.CurrentResults.Add(result);
				}
				if (!_renderer.CurrentResults.Any()) {
					_messageView.IsOpen = true;
					_messageView.Content = "No results found";
					_messageView.PlacementTarget = _searchTextBox;
				}
				else
					_messageView.IsOpen = false;
			}
			_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
		}

		private void _buttonPrev_Click(object sender, RoutedEventArgs e) {
			FindPrevious();
		}

		private void _buttonNext_Click(object sender, RoutedEventArgs e) {
			FindNext();
		}

		private void _buttonClose_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonFancyMode_Click(object sender, RoutedEventArgs e) {
			_isSearch = !_isSearch;

			if (_isSearch) {
				_buttonFancyMode.ImagePath = "replace.png";
				_replaceTextBox.Visibility = Visibility.Collapsed;
			}
			else {
				_buttonFancyMode.ImagePath = "search.png";
				_replaceTextBox.Visibility = Visibility.Visible;
			}
		}

		private void _buttonOpenSubMenu_Click(object sender, RoutedEventArgs e) {
			_cbSubMenu.IsDropDownOpen = true;
		}

		private void _buttonReplaceSingle_Click(object sender, RoutedEventArgs e) {
			SearchResult result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset) ?? _renderer.CurrentResults.FirstSegment;

			if (result != null) {
				_replace(result);
				FindNext();
			}
		}

		private void _replace(TextSegment result) {
			if (!_editor.IsReadOnly) {
				_textArea.Document.Replace(result.StartOffset, result.EndOffset - result.StartOffset, _replaceTextBox.Text);
				_textArea.Caret.Offset = result.EndOffset - ((result.EndOffset - result.StartOffset) - _replaceTextBox.Text.Length);
			}
			else {
				_messageView.Content = "The document is readonly.";
				_messageView.PlacementTarget = _searchTextBox;
				_messageView.IsOpen = true;
			}
		}

		private void _buttonReplaceAll_Click(object sender, RoutedEventArgs e) {
			if (_editor.IsReadOnly) {
				_messageView.Content = "The document is readonly.";
				_messageView.PlacementTarget = _searchTextBox;
				_messageView.IsOpen = true;
				return;
			}

			var items = _renderer.CurrentResults.OrderBy(p => p.StartOffset).ToList();
			int added = 0;

			if (items.Count > 0) {
				_textArea.Document.BeginUpdate();

				try {
					foreach (SearchResult result in items) {
						_textArea.Document.Replace(result.StartOffset + added, result.EndOffset - result.StartOffset, _replaceTextBox.Text);
						added += _replaceTextBox.Text.Length - (result.EndOffset - result.StartOffset);
					}
				}
				finally {
					_textArea.Document.EndUpdate();
				}
			}
		}

		private void _replaceTextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
			_textArea_PreviewKeyDown(sender, e);

			if (e.Key == Key.Enter) {
				_buttonReplaceSingle_Click(sender, null);
				e.Handled = true;
			}
		}

		#region Nested type: SearchPanelAdorner

		public class SearchPanelAdorner : Adorner {
			private readonly SearchPanel _panel;

			public SearchPanelAdorner(TextArea textArea, SearchPanel panel)
				: base(textArea) {
				_panel = panel;
				//_panel.HorizontalAlignment = HorizontalAlignment.Right;
				//_panel.VerticalAlignment = VerticalAlignment.Top;
				AddVisualChild(panel);
			}

			protected override int VisualChildrenCount {
				get { return 1; }
			}

			protected override Visual GetVisualChild(int index) {
				if (index != 0)
					throw new ArgumentOutOfRangeException();
				return _panel;
			}

			protected override Size ArrangeOverride(Size finalSize) {
				_panel.Arrange(new Rect(new Point(0, 0), finalSize));
				return new Size(_panel.ActualWidth < 0 ? 0 : _panel.ActualWidth, _panel.ActualHeight < 0 ? 0 : _panel.ActualHeight);
			}
		}

		#endregion

		#region Nested type: SearchResultBackgroundRenderer

		public class SearchResultBackgroundRenderer : IBackgroundRenderer {
			private readonly TextSegmentCollection<SearchResult> _currentResults = new TextSegmentCollection<SearchResult>();

			private Brush _markerBrush;
			private Pen _markerPen;

			public SearchResultBackgroundRenderer() {
				_markerBrush = Brushes.LightGreen;
				_markerPen = new Pen(_markerBrush, 1);
			}

			public TextSegmentCollection<SearchResult> CurrentResults {
				get { return _currentResults; }
			}

			public Brush MarkerBrush {
				get { return _markerBrush; }
				set {
					_markerBrush = value;
					_markerPen = new Pen(_markerBrush, 1);
				}
			}

			#region IBackgroundRenderer Members

			public KnownLayer Layer {
				get {
					// draw behind selection
					return KnownLayer.Selection;
				}
			}

			public void Draw(TextView textView, DrawingContext drawingContext) {
				if (textView == null)
					throw new ArgumentNullException("textView");
				if (drawingContext == null)
					throw new ArgumentNullException("drawingContext");

				if (_currentResults == null || !textView.VisualLinesValid)
					return;

				var visualLines = textView.VisualLines;
				if (visualLines.Count == 0)
					return;

				int viewStart = visualLines.First().FirstDocumentLine.Offset;
				int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

				foreach (SearchResult result in _currentResults.FindOverlappingSegments(viewStart, viewEnd - viewStart)) {
					BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
					geoBuilder.AlignToMiddleOfPixels = true;
					geoBuilder.AddSegment(textView, result);
					Geometry geometry = geoBuilder.CreateGeometry();
					if (geometry != null) {
						drawingContext.DrawGeometry(_markerBrush, _markerPen, geometry);
					}
				}
			}

			#endregion
		}

		#endregion
	}
}