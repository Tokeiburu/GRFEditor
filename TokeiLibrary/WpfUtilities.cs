using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ErrorManager;
using Utilities;
using Utilities.Extension;

namespace TokeiLibrary {
	public static class WpfUtilities {
		public static Brush Ok = new SolidColorBrush(Colors.White);
		public static Brush Error = new SolidColorBrush(Colors.Pink);
		public static Brush Processing = new SolidColorBrush(Color.FromArgb(255, 220, 255, 204));
		public static Brush GotFocusBrush = new SolidColorBrush(Color.FromArgb(255, 193, 193, 242));
		public static Brush LostFocusBrush = new SolidColorBrush(Color.FromArgb(255, 149, 149, 149));
		public static Brush MouseOverFocusBrush = new SolidColorBrush(Color.FromArgb(255, 174, 174, 201));

		public static readonly DependencyProperty TkVerticalAlignmentProperty;

		public static VerticalAlignment GetTkVerticalAlignment(DependencyObject obj) {
			return (VerticalAlignment)obj.GetValue(TkVerticalAlignmentProperty);
		}

		public static void SetTkVerticalAlignment(DependencyObject obj, VerticalAlignment value) {
			obj.SetValue(TkVerticalAlignmentProperty, value);
		}

		static WpfUtilities() {
			Ok.Freeze();
			Error.Freeze();
			Processing.Freeze();
			GotFocusBrush.Freeze();
			LostFocusBrush.Freeze();
			MouseOverFocusBrush.Freeze();

			TkVerticalAlignmentProperty = DependencyProperty.RegisterAttached(
				"TkVerticalAlignment",
				typeof(VerticalAlignment),
				typeof(WpfUtilities),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterSortableGrid)));
		}

		private static void OnRegisterSortableGrid(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			FrameworkElement grid = sender as FrameworkElement;
			if (grid != null) {
				RegisterSortableGridview(grid, args);
			}
		}

		private static void RegisterSortableGridview(FrameworkElement grid, DependencyPropertyChangedEventArgs args) {
			if (args.NewValue is VerticalAlignment) {
				grid.VerticalAlignment = VerticalAlignment.Top;

				VerticalAlignment alignment = (VerticalAlignment) args.NewValue;

				if (alignment == VerticalAlignment.Center) {
					var res = FindDirectParentControl<FrameworkElement>(grid);

					if (res != null) {
						int absoluteTopMargin = 0;

						if (res is TabItem) {
							res = FindDirectParentControl<TabControl>(res);
							absoluteTopMargin = 20;
						}

						res.SizeChanged += delegate {
							int top = (int)((res.ActualHeight - grid.ActualHeight) / 2) + absoluteTopMargin;

							grid.Margin = new Thickness(grid.Margin.Left, top, grid.Margin.Right, grid.Margin.Bottom);
						};
					}
				}
				else if (alignment == VerticalAlignment.Top) {
					// Do nothing
				}
				else if (alignment == VerticalAlignment.Bottom) {
					grid.VerticalAlignment = VerticalAlignment.Bottom;
				}
			}
		}

		public static void RemoveKeyboardFocus() {
			FrameworkElement textBox = Keyboard.FocusedElement as FrameworkElement;
			FrameworkElement parent = textBox;

			while (parent != null && !((IInputElement) parent).Focusable) {
				parent = (FrameworkElement) parent.Parent;
			}

			DependencyObject scope = FocusManager.GetFocusScope(textBox);
			FocusManager.SetFocusedElement(scope, parent);
		}

		public static void AddFocus(FrameworkElement tb) {
			Border border = FindDirectParentControl<Border>(tb);

			if (border == null)
				return;

			tb.MouseEnter += delegate {
				if (!tb.IsFocused)
					border.BorderBrush = MouseOverFocusBrush;
			};

			tb.MouseLeave += delegate {
				if (!tb.IsFocused)
					border.BorderBrush = LostFocusBrush;
			};

			tb.GotFocus += delegate { border.BorderBrush = GotFocusBrush; };
			tb.LostFocus += delegate { border.BorderBrush = LostFocusBrush; };

			border.BorderBrush = LostFocusBrush;
		}
		public static void AddFocus(params FrameworkElement[] elems) {
			foreach (FrameworkElement elem in elems) {
				AddFocus(elem);
			}
		}

		public static void SetMinimalSize(Window window) {
			window.Loaded += delegate {
				window.MinHeight = window.ActualHeight;
				window.MinWidth = window.ActualWidth;
			};
		}

		public static void SetMinAndMaxSize(Window window) {
			window.Loaded += delegate {
				window.MinHeight = window.ActualHeight;
				window.MinWidth = window.ActualWidth;
				window.MaxHeight = window.ActualHeight;
				window.MaxWidth = window.ActualWidth;
			};
		}

		public static Window TopWindow {
			get {
				try {
					if (Application.Current == null)
						return null;

					return Application.Current.Dispatch(delegate {
						if (Application.Current.MainWindow == null)
							return null;

						IntPtr active = NativeMethods.GetActiveWindow();
						Window topWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsLoaded && new WindowInteropHelper(window).Handle == active);

						if (topWindow != null) {
							return topWindow;
						}
						return Application.Current.MainWindow.IsVisible ? Application.Current.MainWindow : Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window.IsVisible);
					});
				}
				catch {
					return null;
				}
			}
		}

		public static void Delete(this ItemCollection control, object obj) {
			IEditableCollectionView items = control;

			if (items.CanRemove) {
				try {
					items.Remove(obj);
				}
				catch { }
			}
		}

		public static T FindDirectParentControl<T>(FrameworkElement control) where T : DependencyObject {
			FrameworkElement parent = control.Parent as FrameworkElement;

			while (parent != null && !(parent is T)) {
				parent = parent.Parent as FrameworkElement;
			}

			return parent as T;
		}

		public static T FindDirectParentControl<T>(UIElement control) where T : DependencyObject {
			return FindParentControl<T>(control);
		}

		public static T FindParentControl<T>(DependencyObject control) where T : DependencyObject {
			if (control == null)
				return null;

			DependencyObject parent = VisualTreeHelper.GetParent(control);
			while (parent != null && !(parent is T)) {
				parent = VisualTreeHelper.GetParent(parent);
			}
			return parent as T;
		}

		public static TItemContainer GetObjectAtPoint<TItemContainer>(this Visual control, Point p) where TItemContainer : DependencyObject {
			// ItemContainer - can be ListViewItem, or TreeViewItem and so on(depends on control)
			TItemContainer obj = GetContainerAtPoint<TItemContainer>(control, p);
			return obj;
		}

		public static TItemContainer GetContainerAtPoint<TItemContainer>(this Visual control, Point p)
								 where TItemContainer : DependencyObject {
			HitTestResult result = VisualTreeHelper.HitTest(control, p);

			if (result == null)
				return null;

			DependencyObject obj = result.VisualHit;

			while (VisualTreeHelper.GetParent(obj) != null && !(obj is TItemContainer)) {
				obj = VisualTreeHelper.GetParent(obj);
			}

			// Will return null if not found
			return obj as TItemContainer;
		}

		public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject {
			// Confirm parent and childName are valid. 
			if (parent == null) return null;

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++) {
				var child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child
				T childType = child as T;
				if (childType == null) {
					// recursively drill down the tree
					foundChild = FindChild<T>(child, childName);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null) break;
				}
				else if (!String.IsNullOrEmpty(childName)) {
					var frameworkElement = child as FrameworkElement;
					// If the child's name is set for search
					if (frameworkElement != null && frameworkElement.Name == childName) {
						// if the child's name is of the request name
						foundChild = (T) child;
						break;
					}
				}
				else {
					// child element found.
					foundChild = (T) child;
					break;
				}
			}

			return foundChild;
		}

		public static T FindFirstChild<T>(DependencyObject parent) where T : DependencyObject {
			// Confirm parent and childName are valid. 
			if (parent == null) return null;

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++) {
				var child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child
				T childType = child as T;
				if (childType == null) {
					// recursively drill down the tree
					foundChild = FindFirstChild<T>(child);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null) break;
				}
				else {
					// child element found.
					foundChild = (T) child;
					break;
				}
			}

			return foundChild;
		}

		public static T[] FindChildren<T>(DependencyObject parent) where T : DependencyObject {
			if (parent == null) return null;
			List<T> items = new List<T>();
			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++) {
				var child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child
				T childType = child as T;
				if (childType == null) {
					// recursively drill down the tree
					foundChild = FindFirstChild<T>(child);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null) {
						items.Add(foundChild);
						break;
					}
				}
				else {
					// child element found.
					foundChild = (T)child;
					items.Add(foundChild);
				}
			}

			return items.ToArray();
		}

		public static TreeViewItem GetTreeViewItemClicked(FrameworkElement sender, TreeView treeView) {
			Point p = sender.TranslatePoint(new Point(0, 5), treeView);
			DependencyObject obj = treeView.InputHitTest(p) as DependencyObject;
			while (obj != null && !(obj is TreeViewItem))
				obj = VisualTreeHelper.GetParent(obj);
			return obj as TreeViewItem;
		}

		public static bool IsTab(TabItem tabItem, string tab) {
			return tabItem.Header.ToString() == tab;
		}
		public static void TextBoxError(TextBox box) {
			box.Dispatch(p => p.Background = Error);
		}
		public static void TextBoxOk(TextBox box) {
			box.Dispatch(p => p.Background = Ok);
		}
		public static void TextBoxProcessing(TextBox box) {
			box.Dispatch(p => p.Background = Processing);
		}
		public static void SetGridPosition(UIElement element, int? row, int? rowSpawn, int? column, int? columnSpan) {
			if (row != null) element.SetValue(Grid.RowProperty, row.Value);
			if (rowSpawn != null) element.SetValue(Grid.RowSpanProperty, rowSpawn.Value);
			if (column != null) element.SetValue(Grid.ColumnProperty, column.Value);
			if (columnSpan != null) element.SetValue(Grid.ColumnSpanProperty, columnSpan.Value);
		}
		public static void SetGridPosition(UIElement element, int? row, int? column) {
			if (row != null) element.SetValue(Grid.RowProperty, row.Value);
			if (column != null) element.SetValue(Grid.ColumnProperty, column.Value);
		}
		public static void UpdateRtb(RichTextBox rtb, string text, bool noBreakLines) {
			try {
				text = text.Replace("\r", "").Replace("\\\"", "\"");

				if (noBreakLines) {
					int count = -1;

					while (count != text.Length) {
						count = text.Length;
						text = text.Replace("\n\n", "\n");
					}
				}

				text = text.Trim(new char[] {'\r', '\n'});

				SolidColorBrush currentBrush = Brushes.Black;
				Paragraph par = new Paragraph();

				int selectionStart = 0;
				int selectionEnd = -1;
				char c;
				string color;

				for (int i = 0; i < text.Length; i++) {
					if (i + 1 < text.Length && text[i] == '^' && (
						((c = text[i + 1]) >= '0' && c <= '9') ||
						(c >= 'a' && c <= 'f') ||
						(c >= 'A' && c <= 'F') || c == 'u')) {
						selectionEnd = i;

						if (selectionEnd > 0) {
							Run run1 = new Run();
							run1.Foreground = currentBrush;
							run1.Text = text.Substring(selectionStart, selectionEnd - selectionStart);
							par.Inlines.Add(run1);
						}

						// read new color
						color = "";
						int j;

						for (j = 0; j + i + 1 < text.Length && j < 6; j++) {
							c = text[i + 1 + j];

							if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
								color += c;
							else if (c >= 'a' && c <= 'f')
								color += char.ToUpper(c);
							else if (c == 'u')
								color += 'F';
							else
								break;
						}

						for (int k = j; k < 6; k++) {
							color += '0';
						}

						selectionStart = j + i + 1;
						i += j;
						currentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + color));
					}
					else if (i == text.Length - 1) {
						if (selectionEnd < 0)
							selectionEnd = 0;

						if (selectionEnd > -1) {
							Run run1 = new Run();
							run1.Foreground = currentBrush;
							run1.Text = text.Substring(selectionStart, text.Length - selectionStart);
							par.Inlines.Add(run1);
						}
					}
				}

				rtb.Document.Blocks.Clear();
				rtb.Document.Blocks.Add(par);
			}
			catch {
			}
		}

		public static T GetPlacementFromContextMenu<T>(FrameworkElement sender) where T : DependencyObject {
			if (sender is T)
				return sender as T;

			ContextMenu pop = FindDirectParentControl<ContextMenu>(sender);

			if (pop != null)
				return pop.PlacementTarget as T;

			return null;
		}

		public static void PreviewLabel(TextBox element, string content) {
			Label label = new Label();
			label.Content = content;
			label.Foreground = Brushes.DarkGray;
			label.Margin = new Thickness(7, 0, 0, 0);
			label.VerticalAlignment = VerticalAlignment.Center;
			label.IsHitTestVisible = false;

			Grid grid = FindParentControl<Grid>(element);
			grid.Children.Add(label);

			element.GotFocus += delegate {
				label.Visibility = Visibility.Collapsed;
			};

			element.LostFocus += delegate {
				label.Visibility = element.Text == "" ? Visibility.Visible : Visibility.Collapsed;
			};
		}

		public static void MakeElementDraggable(UIElement element, Window window) {
			element.MouseDown += delegate(object sender, MouseButtonEventArgs e) {
				try {
					if (e.LeftButton == MouseButtonState.Pressed) {
						window.DragMove();
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};
		}

		public static TabItem GetTab(TabControl control, string name) {
			foreach (TabItem item in control.Items) {
				if (item.Header.ToString() == name)
					return item;
			}

			return null;
		}

		public static bool CanDrop(DragEventArgs e, params string[] extensions) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

				return files.ToList().Any(p => p.IsExtension(extensions));
			}

			return false;
		}

		public static string[] GetDroppedFiles(DragEventArgs e, params string[] extensions) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				return files.ToList().Where(p => p.IsExtension(extensions)).ToArray();
			}

			return new string[] { };
		}
	}
}
