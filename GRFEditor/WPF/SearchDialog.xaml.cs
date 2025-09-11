using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Services;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for PatcherDialog.xaml
	/// </summary>
	public partial class SearchDialog : TkWindow, IProgress {
		private readonly GrfHolder _grf;
		private RangeObservableCollection<ESearchResult> _results = new RangeObservableCollection<ESearchResult>();
		private SearchResultBackgroundRenderer _renderer;
		private AsyncOperation _async;

		public SearchDialog(GrfHolder grf)
			: base("Advanced search", "search.png", SizeToContent.Manual, ResizeMode.CanResize) {
			_grf = grf;
			InitializeComponent();
			_async = new AsyncOperation(_progressBar);

			_renderer = new SearchResultBackgroundRenderer { MarkerBrush = new SolidColorBrush(Colors.Yellow) };
			_tbPreview.TextArea.TextView.BackgroundRenderers.Add(_renderer);

			ShowInTaskbar = true;

			AvalonHelper.Load(_tbPreview);
			AvalonHelper.Load(_tbPreview);

			_tbSearchPattern.KeyDown += new KeyEventHandler(_tbSearchPattern_KeyUp);
			_tbSearchPattern.GotFocus += _tbSearchPattern_GotFocus;
			_tbSearchPattern.LostFocus += _tbSearchPattern_LostFocus;

			_tbFilePattern.KeyDown += new KeyEventHandler(_tbSearchPattern_KeyUp);
			_tbFilePattern.GotFocus += _tbFilePattern_GotFocus;
			_tbFilePattern.LostFocus += _tbFilePattern_LostFocus;

			Binder.Bind(_cbMatchCase);
			Binder.Bind(_cbRegex);
			Binder.Bind(_cbWholeWord);
			Binder.Bind(_tbSearchPattern);
			Binder.Bind(_tbFilePattern, "*.txt;*.lua;*.log;*.xml;*.ini");

			_tbSearchPattern_LostFocus(null, null);
			_tbFilePattern_LostFocus(null, null);

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listViewResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "FileType", FixedWidth = 20, MaxHeight = 16 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "FileName", SearchGetAccessor = "FileName", ToolTipBinding = "FullPath", TextAlignment = TextAlignment.Left, MinWidth = 160 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Path", DisplayExpression = "Path", SearchGetAccessor = "Path", TextAlignment = TextAlignment.Left, MinWidth = 100, IsFill = true },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Matches", DisplayExpression = "NumberOfMatches", SearchGetAccessor = "NumberOfMatches", TextAlignment = TextAlignment.Right, FixedWidth = 60 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", SearchGetAccessor = "FileType", TextAlignment = TextAlignment.Right, FixedWidth = 40 },
			}, new DefaultListViewComparer<ESearchResult>(), new string[] { "Normal", "{DynamicResource TextForeground}" });

			_listViewResults.ItemsSource = _results;
			_listViewResults.SelectionChanged += new SelectionChangedEventHandler(_listViewResults_SelectionChanged);
			_listViewResults.MouseLeftButtonUp += new MouseButtonEventHandler(_listViewResults_MouseLeftButtonUp);
		}

		private void _listViewResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			var item = _listViewResults.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listViewResults));

			if (item != null) {

			}
			else {
				e.Handled = true;
			}
		}

		private void _listViewResults_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				var item = _listViewResults.SelectedItem as ESearchResult;

				if (item != null) {
					_tbPreview.Text = _generatePreview(item);

					var strategy = SearchStrategyFactory.Create(_tbSearchPattern.Text, _cbMatchCase.IsChecked != true, _cbWholeWord.IsChecked == true, _cbRegex.IsChecked == true ? SearchMode.RegEx : SearchMode.Normal);
					_renderer.CurrentResults.Clear();

					foreach (SearchResult res in strategy.FindAll(_tbPreview.Document, 0, _tbPreview.Document.TextLength)) {
						_renderer.CurrentResults.Add(res);
					}
				}
				else {
					_tbPreview.Text = "";
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private string _generatePreview(ESearchResult item) {
			StringBuilder b = new StringBuilder();

			List<int> newLines = new List<int>();
			newLines.Add(0);

			for (int i = 0; i < item.Source.TextLength; i++) {
				if (item.Source.Text[i] == '\n') {
					newLines.Add(i + 1);
				}
			}

			HashSet<int> shown = new HashSet<int>();

			foreach (var res in item.Results) {
				int lineNumber = _findLineNumber(newLines, res);

				if (lineNumber < 0)
					lineNumber = 0;

				if (shown.Add(lineNumber)) {
					b.Append((lineNumber + 1));
					b.Append("\t");

					int length = (lineNumber + 1 < newLines.Count ? newLines[lineNumber + 1] : item.Source.TextLength) - newLines[lineNumber];
					int start = newLines[lineNumber];

					if (length > 100) {
						// Only get the revelant part
						var sub = res.Offset;
						var end = 100;

						sub = sub - 10;

						if (sub < start)
							sub = start;

						if (end > length)
							end = length;

						if (end + sub > start + length)
							end = start + length - sub;

						start = sub;
						length = end;
					}

					b.AppendLine(item.Source.GetText(start, length).Replace("\0", "\\0").TrimEnd('\r', '\n'));
				}
			}

			return b.ToString();
		}

		private int _findLineNumber(List<int> newLines, ISearchResult res) {
			var t = newLines.BinarySearch(res.Offset);

			if (t == -1 || t == 0) {
				return 0;
			}

			if (t < 0) {
				return -1 * t - 2;
			}
			
			return t;
			//Z.F(t);
			//for (int i = 0; i < newLines.Count; i++) {
			//	if (newLines[i] >= res.Offset)
			//		return (i - 1);
			//}
			//
			//return newLines.Count - 1;
		}

		private void _tbSearchPattern_KeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				_search();
			}
		}

		private void _tbFilePattern_GotFocus(object sender, RoutedEventArgs e) {
			_labelFilePattern.Visibility = Visibility.Hidden;
			_border2.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 186, 86, 0));
			_tbFilePattern.Foreground = Application.Current.Resources["TextForeground"] as Brush;
		}

		private void _tbFilePattern_LostFocus(object sender, RoutedEventArgs e) {
			_border2.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_tbFilePattern.Text == "") {
				_labelFilePattern.Visibility = Visibility.Visible;
			}
			else {
				_labelFilePattern.Visibility = Visibility.Hidden;
				_tbFilePattern.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}

		private void _tbSearchPattern_GotFocus(object sender, RoutedEventArgs e) {
			_labelFind.Visibility = Visibility.Hidden;
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 186, 86, 0));
			_tbSearchPattern.Foreground = Application.Current.Resources["TextForeground"] as Brush;
		}

		private void _tbSearchPattern_LostFocus(object sender, RoutedEventArgs e) {
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_tbSearchPattern.Text == "") {
				_labelFind.Visibility = Visibility.Visible;
			}
			else {
				_labelFind.Visibility = Visibility.Hidden;
				_tbSearchPattern.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}

		private string _fileFilter {
			get { return _tbFilePattern.Dispatch(p => p.Text).Trim(';'); }
		}

		private string _searchPattern {
			get { return _tbSearchPattern.Dispatch(p => p.Text); }
		}

		private bool _matchCase {
			get { return _cbMatchCase.Dispatch(p => p.IsChecked == true); }
		}

		private bool _wholeWord {
			get { return _cbWholeWord.Dispatch(p => p.IsChecked == true); }
		}

		private bool _regex {
			get { return _cbRegex.Dispatch(p => p.IsChecked == true); }
		}

		private void _searchSub() {
			try {
				AProgress.Init(this);
				
				Regex filePattern = new Regex((@"^(" + Methods.WildcardToRegexLine(_fileFilter) + ")$").Replace(";", "|"), RegexOptions.IgnoreCase);

				List<(string Directory, string Filename, FileEntry Entry)> result1 = _grf.FileTable.FastTupleAccessEntries.Where(p => filePattern.IsMatch(p.Item2)).ToList();

				if (result1.Count == 0) {
					ErrorHandler.HandleException("No file extension matches your file pattern.");
					return;
				}

				this.Dispatch(p => _results.Clear());

				var strategy = SearchStrategyFactory.Create(_searchPattern, !_matchCase, _wholeWord, _regex ? SearchMode.RegEx : SearchMode.Normal);

				for (int index = 0; index < result1.Count; index++) {
					var file = result1[index];
					AProgress.IsCancelling(this);

					string content;

					try {
						content = EncodingService.DisplayEncoding.GetString(file.Item3.GetDecompressedData());
					}
					catch {
						continue;
					}

					TextSource source = new TextSource(content);

					var output = strategy.FindAll(source, 0, source.TextLength).ToList();

					if (output.Count > 0) {
						this.Dispatch(p => _results.Add(new ESearchResult { Results = output, FileName = file.Item3.DisplayRelativePath, Source = source, FileType = file.Item3.FileType, FullPath = file.Item3.RelativePath, Entry = file.Item3 }));
					}

					Progress = (float) index / result1.Count * 100f;
				}
			}
			catch (OperationCanceledException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				AProgress.Finalize(this);
			}
		}

		private void _search() {
			_async.SetAndRunOperation(new GrfThread(_searchSub, this, 200));
		}

		private void _buttonOpenSubMenu_Click(object sender, RoutedEventArgs e) {
			_cbSubMenu.IsDropDownOpen = true;
		}

		private void _miSelect_Click(object sender, RoutedEventArgs e) {
			var item = _listViewResults.SelectedItem as ESearchResult;

			if (item != null) {
				PreviewService.Select(null, null, item.FullPath);
			}
		}

		private void _buttonSearch_Click(object sender, RoutedEventArgs e) {
			_search();
		}

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		private void _fbReset_Click(object sender, RoutedEventArgs e) {
			_tbFilePattern.Text = "*.txt;*.lua;*.log;*.xml;*.ini";
		}

		private void _miExtract_Click(object sender, RoutedEventArgs e) {
			try {
				var entries = _listViewResults.SelectedItems.Cast<ESearchResult>().Select(p => p.Entry).ToList();

				_async.SetAndRunOperation(
					new GrfThread(() => _grf.Extract(null, entries,
													 GrfEditorConfiguration.DefaultExtractingPath, 
													 (GrfEditorConfiguration.AlwaysOpenAfterExtraction ? ExtractOptions.OpenAfterExtraction : ExtractOptions.Normal) |
													 (GrfEditorConfiguration.OverrideExtractionPath ? ExtractOptions.UseAppDataPathToExtract : ExtractOptions.Normal),
													 SyncMode.Synchronous), this, 200, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miExtractTo_Click(object sender, RoutedEventArgs e) {
			try {
				var entries = _listViewResults.SelectedItems.Cast<ESearchResult>().Select(p => p.Entry).ToList();
				var dest = PathRequest.FolderExtract();

				if (dest != null) {
					_async.SetAndRunOperation(
						new GrfThread(() => _grf.Extract(dest, entries,
						                                 GrfEditorConfiguration.DefaultExtractingPath, 
														 (GrfEditorConfiguration.AlwaysOpenAfterExtraction ? ExtractOptions.OpenAfterExtraction : ExtractOptions.Normal) |
														 (GrfEditorConfiguration.OverrideExtractionPath ? ExtractOptions.UseAppDataPathToExtract : ExtractOptions.Normal) |
														 ExtractOptions.ExtractAllInSameFolder,
						                                 SyncMode.Synchronous), this, 200, null, true));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class TextSource : ITextSource {
		public TextSource(string content) {
			Text = content;
		}

		public char GetCharAt(int offset) {
			return Text[offset];
		}

		public int IndexOfAny(char[] anyOf, int startIndex, int count) {
			return Text.IndexOfAny(anyOf, startIndex, count);
		}

		public string GetText(int offset, int length) {
			return Text.Substring(offset, length);
		}

		public ITextSource CreateSnapshot() {
			throw new NotImplementedException();
		}

		public ITextSource CreateSnapshot(int offset, int length) {
			throw new NotImplementedException();
		}

		public TextReader CreateReader() {
			throw new NotImplementedException();
		}

		public string Text { get; private set; }
		public int TextLength { get { return Text.Length; } }
		public event EventHandler TextChanged;

		public void OnTextChanged() {
			EventHandler handler = TextChanged;
			if (handler != null) handler(this, null);
		}
	}
}