using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ErrorManager;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace TokeiLibrary.WPF {
	public class TkView : TreeView, IDisposable {
		#region Delegates

		public delegate void DoDragDropDelegate(List<TkTreeViewItem> items);

		public delegate void EncodingChangedEventHandler(object sender);

		#endregion

		private readonly DispatcherTimer _treeViewTimer = new DispatcherTimer();
		public DoDragDropDelegate DoDragDropCustomMethod;
		private TkTreeViewItem _previewItem;
		private SelectedItemsList _selectedItems = new SelectedItemsList();
		public event EncodingChangedEventHandler EncodingChanged;
		public Encoding DisplayEncoding { get; set; }
		public Action CopyMethod;

		public virtual void OnEncodingChanged() {
			EncodingChangedEventHandler handler = EncodingChanged;
			if (handler != null) handler(this);
		}

		public TkView() {
			AllowDrop = true;
			_treeViewTimer.Interval = new TimeSpan(0, 0, 0, 0, 650);
			_treeViewTimer.Tick += _treeViewTimer_Tick;

			FocusVisualStyle = null;
			Drop += _treeView_Drop;
			DragEnter += _treeView_DragEnter;
			DragOver += _treeView_DragOver;
			DragLeave += _treeView_DragLeave;
			PreviewMouseMove += _treeView_PreviewMouseMove;
			PreviewMouseLeftButtonDown += _treeView_PreviewMouseLeftButtonDown;
			KeyDown += new KeyEventHandler(_tKView_KeyDown);
			DisplayEncoding = EncodingService.DisplayEncoding;
			base.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(_base_SelectedItemChanged);
		}

		private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				var tItem = _getTreeViewMousePos(e);

				if (tItem != null)
					SelectedItem = tItem;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _selectNext(TkTreeViewItem item, Key select) {
			if (select == Key.Left) {
				if (item.IsExpanded) {
					item.IsExpanded = false;
					return;
				}

				if (!(item.Parent is TkTreeViewItem))
					return;

				SelectedItem = item.Parent;
				return;
			}

			if (select == Key.Right) {
				if (item.Items.Count == 0)
					return;

				if (!item.IsExpanded) {
					item.IsExpanded = true;
					return;
				}

				SelectedItem = item.Items[0];
				return;
			}

			var items = _flatten(null);
			var index = items.IndexOf(item);

			if (index < 0)
				return;

			if (select == Key.Down) {
				if (index + 1 >= items.Count)
					return;
				SelectedItem = items[index + 1];
			}
			else {
				if (index <= 0)
					return;

				SelectedItem = items[index - 1];
			}
		}

		private List<TkTreeViewItem> _flatten(TkTreeViewItem current, List<TkTreeViewItem> items = null) {
			if (items == null)
				items = new List<TkTreeViewItem>();

			foreach (var item in (current == null ? Items : current.Items).OfType<TkTreeViewItem>()) {
				items.Add(item);

				if (item.IsExpanded)
					_flatten(item, items);
			}

			return items;
		}

		private void _treeView_DragOver(object sender, DragEventArgs e) {
			DragDropEffects? effect = null;

			var itemUnderMouse = _getTreeViewItemClicked((FrameworkElement)e.OriginalSource, this);

			if (itemUnderMouse != _previewItem && _previewItem != null) {
				_previewItem.Reset();
			}

			if (itemUnderMouse != null) {
				if (!itemUnderMouse.CanBeDropped) {
					itemUnderMouse.UnableItem();
					effect = DragDropEffects.None;
				}
				else if (itemUnderMouse == _previewItem) {
					itemUnderMouse.DragEnterItem();
				}
			}

			if (effect == null) {
				effect = DragDropEffects.Move;
			}

			e.Effects = effect.Value;
			e.Handled = true;
		}

		private void _treeView_DragLeave(object sender, DragEventArgs e) {
			var itemUnderMouse = _getTreeViewItemClicked((FrameworkElement) e.OriginalSource, this);
			DragDropEffects? effect = null;

			if (itemUnderMouse != _previewItem && _previewItem != null) {
				_previewItem.Reset();
			}

			if ((e.KeyStates & DragDropKeyStates.LeftMouseButton) != DragDropKeyStates.LeftMouseButton) {
				if (_previewItem != null)
					_previewItem.Reset();
			}

			if (itemUnderMouse != null && !itemUnderMouse.CanBeDropped) {
				effect = DragDropEffects.None;
			}

			if (effect == null) {
				effect = DragDropEffects.Move;
			}

			e.Effects = effect.Value;
			e.Handled = true;
		}

		public bool MultiSelection {
			get { return _selectedItems.MultiSelection; }
			set { _selectedItems.MultiSelection = value; }
		}

		public new object SelectedItem {
			get { return SelectedItems.Last(); }
			set { _selectedItems.Set(value); }
		}

		public SelectedItemsList SelectedItems {
			get { return _selectedItems; }
			set { _selectedItems = value; }
		}

		#region IDisposable Members

		public void Dispose() {
			if (Items != null)
				Items.Clear();
		}

		#endregion

		public new event RoutedPropertyChangedEventHandler<object> SelectedItemChanged;

		public new void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e) {
			RoutedPropertyChangedEventHandler<object> handler = SelectedItemChanged;
			if (handler != null) handler(this, e);
		}

		private void _base_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			TkTreeViewItem item = e.NewValue as TkTreeViewItem;

			if (item != null && !ReferenceEquals(item, SelectedItem) && !SelectedItems.Items.Contains(item)) {
				item.IsSelected = true;
			}
		}

		private Stopwatch _lastKeyDown = new Stopwatch();
		private string _currentSearch = "";

		private void _tKView_KeyDown(object sender, KeyEventArgs e) {
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C) {
				if (CopyMethod == null)
					Clipboard.SetDataObject(GetSelectedPathExceptProject());
				else
					CopyMethod();
			}
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.A) {
				_selectAll();
			}
			if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == (ModifierKeys.Alt | ModifierKeys.Control) && e.Key == Key.C) {
				var item = SelectedItem as TkTreeViewItem;

				if (item != null) {
					Clipboard.SetDataObject(item.HeaderText);
				}
			}
			if (e.Key == Key.Down ||
			    e.Key == Key.Up ||
			    e.Key == Key.Left ||
			    e.Key == Key.Right) {
				var item = SelectedItem as TkTreeViewItem;

				if (item != null) {
					_selectNext(item, e.Key);
				}
				e.Handled = true;
			}
			else {
				try {
					switch(e.Key) {
						case Key.LeftAlt:
						case Key.LeftCtrl:
						case Key.LeftShift:
						case Key.RightAlt:
						case Key.RightCtrl:
						case Key.RightShift:
							return;
					}

					if (Keyboard.FocusedElement is TextBox)
						return;

					var str = KeyEventUtility.GetCharFromKey(e.Key);

					if (_lastKeyDown.IsRunning && _lastKeyDown.ElapsedMilliseconds > 800) {
						_currentSearch = "";
					}

					_lastKeyDown.Reset();
					_lastKeyDown.Start();

					_currentSearch += str;

					var item = SelectedItem as TkTreeViewItem;
					var items = _flatten(null);
					var index = item == null ? -1 : items.IndexOf(item);

					if (_currentSearch.Length == 1) {
						index += 1;
					}

					if (index < 0)
						index = 0;

					for (int i = index; i < items.Count; i++) {
						if ((items[i].HeaderText ?? "").IndexOf(_currentSearch, 0, StringComparison.OrdinalIgnoreCase) == 0 ||
							(items[i].TranslatedText ?? "").IndexOf(_currentSearch, 0, StringComparison.OrdinalIgnoreCase) == 2) {
							SelectedItem = items[i];
							return;
						}
					}

					for (int i = 0; i < index - 1; i++) {
						if ((items[i].HeaderText ?? "").IndexOf(_currentSearch, 0, StringComparison.OrdinalIgnoreCase) == 0 ||
							(items[i].TranslatedText ?? "").IndexOf(_currentSearch, 0, StringComparison.OrdinalIgnoreCase) == 2) {
							SelectedItem = items[i];
							return;
						}
					}
				}
				catch { }
			}
		}

		private void _selectAll() {
			if (MultiSelection) {
				if (Items.Count > 0) {
					SelectedItems.SelectNoEvent = true;
					foreach (TkTreeViewItem item in Items) {
						_selectAll(item);
					}
					SelectedItems.SelectNoEvent = false;
				}
			}
		}

		private void _selectAll(TkTreeViewItem parent) {
			parent.IsSelected = true;

			if (parent.IsExpanded) {
				foreach (TkTreeViewItem item in parent.Items) {
					_selectAll(item);
				}
			}
		}
		public static bool _control() {
			return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
		}

		private void _treeViewTimer_Tick(object sender, EventArgs e) {
			if (_previewItem != null && _previewItem.Items.Count > 0) {
				_previewItem.IsExpanded = true;
				_treeViewTimer.Stop();
			}
		}
		private void _treeView_Drop(object sender, DragEventArgs e) {
			var item = _getTreeViewItemClicked((FrameworkElement)e.OriginalSource, this);

			if (item != null) {
				item.Reset();
			}
		}
		private void _treeView_DragEnter(object sender, DragEventArgs e) {
			var itemUnderMouse = _getTreeViewItemClicked((FrameworkElement)e.OriginalSource, this);

			try {
				if (itemUnderMouse != null && !Equals(itemUnderMouse, _previewItem)) {
					if (_previewItem != null) {
						_previewItem.Reset();
					}

					if (!itemUnderMouse.CanBeDropped) {
						itemUnderMouse.UnableItem();
					}
					else
						itemUnderMouse.DragEnterItem();

					_previewItem = itemUnderMouse;

					if (!itemUnderMouse.IsExpanded && itemUnderMouse.Items.Count > 0) {
						_treeViewTimer.Stop();
						_treeViewTimer.Start();
					}
				}

				e.Handled = true;
			}
			finally {
				if (itemUnderMouse == null && _previewItem != null && !Equals(_previewItem, SelectedItem)) {
					_previewItem.IsSelected = _previewItem.IsSelected;
					_previewItem.Reset();
					_previewItem = null;
				}
			}
		}
		private void _treeView_PreviewMouseMove(object sender, MouseEventArgs e) {
			try {
				if (Configuration.TreeBehaviorUseAlt && (e.LeftButton == MouseButtonState.Pressed && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))) ||
					!Configuration.TreeBehaviorUseAlt && (e.LeftButton == MouseButtonState.Pressed)) {
					var tItem = _getTreeViewItemClicked((FrameworkElement) e.OriginalSource, this);
					
					if (tItem == null)
						return;

					if (e.GetPosition(tItem).X < 20)
						return;

					if (SelectedItem != null) {
						TkTreeViewItem item = (TkTreeViewItem) SelectedItem;

						if (DoDragDropCustomMethod == null) {
							DataObject data = new DataObject();
							data.SetData(DataFormats.StringFormat, GetPlainPath(item));
							DragDrop.DoDragDrop(this, data, item.CanBeDragged ? DragDropEffects.Move : DragDropEffects.None);
						}
						else {
							DoDragDropCustomMethod(SelectedItems.Items);
						}

						e.Handled = true;
						return;
					}

					e.Handled = false;
				}
				else if (Configuration.TreeBehaviorUseAlt && e.LeftButton == MouseButtonState.Pressed) {
					//if (!this.IsMouseCaptured) {
					//    this.CaptureMouse();
					//    this.MouseUp += delegate {
					//        this.ReleaseMouseCapture();
					//    };
					//}

					//var tItem = _getTreeViewItemClicked((FrameworkElement)e.OriginalSource, this);
					var tItem = _getTreeViewMousePos(e);

					if (tItem == null)
						return;

					if (Equals(SelectedItem, tItem))
						return;

					tItem.IsSelected = true;
					e.Handled = false;
				}
			}
			catch { }
		}

		private ScrollViewer _sv;

		public string GetSelectedPath() {
			string path = "";

			TkTreeViewItem node = (TkTreeViewItem) SelectedItem;

			while (node != null) {
				path = node.HeaderText + "\\" + path;
				node = node.Parent as TkTreeViewItem;
			}

			return path;
		}

		public string GetSelectedPathExceptProject() {
			string path = "";

			TkTreeViewItem node = (TkTreeViewItem)SelectedItem;

			while (node != null) {
				if (node.HeaderText.IsExtension(".grf", ".rgz", ".root", ".gpf", ".thor"))
					break;

				path = node.HeaderText + "\\" + path;
				node = node.Parent as TkTreeViewItem;
			}

			return path;
		}

		private TkTreeViewItem _getTreeViewMousePos(MouseEventArgs e) {
			var itemUnderMouse = _getTreeViewItemClicked((FrameworkElement)e.OriginalSource, this);

			if (itemUnderMouse != null)
				return itemUnderMouse;

			var tPos = e.GetPosition(this);

			if (Items.Count == 0)
				return null;

			if (_sv == null)
				_sv = WpfUtilities.FindChild<ScrollViewer>(this);

			if (_sv == null)
				return null;

			if (_sv.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible && tPos.X >= this.ActualWidth - SystemParameters.VerticalScrollBarWidth)
				return null;

			if (_sv.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible && tPos.Y >= this.ActualHeight - SystemParameters.HorizontalScrollBarHeight)
				return null;

			var items = _flatten(null);
			var itemHeight = items[0].ActualHeight / items.Count;

			var pos = e.GetPosition(items[0]);
			var index = (int)(pos.Y / itemHeight);

			if (index >= 0 && index < items.Count) {
				e.Handled = true;
				return items[index];
				//SelectedItem = items[index];
			}

			return null;
		}

		private static TkTreeViewItem _getTreeViewItemClicked(FrameworkElement sender, TreeView treeView) {
			Point p = sender.TranslatePoint(new Point(0, 5), treeView);
			DependencyObject obj = treeView.InputHitTest(p) as DependencyObject;
			while (obj != null && !(obj is TkTreeViewItem))
				obj = VisualTreeHelper.GetParent(obj);
			return obj as TkTreeViewItem;
		}
		public static string GetPlainPath(object selectedItem) {
			if (selectedItem == null) return null;

			TkTreeViewItem item = selectedItem as TkTreeViewItem;

			string currentPath = null;
			if (item != null) {
				currentPath = (string)item.Dispatcher.Invoke(new Func<string>(() => item.HeaderText));

				while (item.Parent != null && !(item.Parent is TreeView)) {
					item = ((TkTreeViewItem)item.Parent);
					currentPath = item.Dispatcher.Invoke(new Func<string>(() => item.HeaderText)) + "\\" + currentPath;
				}
			}

			if (currentPath == null)
				return null;

			string[] folders = currentPath.Split('\\');

			if (folders.Length == 1)
				return folders[0];

			return folders[0] + '?' + folders.Skip(1).Aggregate((a, b) => a + '\\' + b);
		}

		public void UpdateEncoding() {
			foreach (TkTreeViewItem item in Items) {
				_changeEncodingTVI(item);
			}

			DisplayEncoding = EncodingService.DisplayEncoding;
			OnEncodingChanged();
		}

		private void _changeEncodingTVI(TkTreeViewItem item) {
			item.HeaderText = EncodingService.DisplayEncoding.GetString(DisplayEncoding.GetBytes(item.HeaderText.ToString(CultureInfo.InvariantCulture)));

			foreach (TkTreeViewItem itemc in item.Items) {
				_changeEncodingTVI(itemc);
			}
		}

		public int FindPosition(TkTreeViewItem toFind) {
			int position = 0;

			foreach (TkTreeViewItem item in Items) {
				int? res = _findPosition(toFind, ref position, item);

				if (res != null)
					return res.Value;
			}

			return 0;
		}

		private int? _findPosition(TkTreeViewItem toFind, ref int position, TkTreeViewItem parent) {
			if (ReferenceEquals(toFind, parent))
				return position;

			position++;

			if (parent.IsExpanded) {
				foreach (TkTreeViewItem item in parent.Items) {
					int? res = _findPosition(toFind, ref position, item);

					if (res != null)
						return res.Value;
				}
			}

			return null;
		}

		public List<object> GetItems(int position1, int position2) {
			try {
				if (Items.Count == 0)
					return null;

				List<object> items = new List<object>();
				int currentCount = 0;

				foreach (TkTreeViewItem item in Items) {
					_getItems(position1, position2, ref currentCount, items, item);
				}

				return items;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return null;
			}
		}

		private void _getItems(int position1, int position2, ref int currentCount, List<object> items, TkTreeViewItem current) {
			if (position1 <= currentCount && currentCount <= position2) {
				items.Add(current);
			}

			currentCount++;

			if (currentCount > position2)
				return;

			if (current.IsExpanded) {
				foreach (TkTreeViewItem item in current.Items) {
					_getItems(position1, position2, ref currentCount, items, item);
				}
			}
		}
	}
}
