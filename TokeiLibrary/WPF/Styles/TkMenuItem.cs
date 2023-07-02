using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.Shortcuts;
using Utilities;

namespace TokeiLibrary.WPF.Styles {
	public class TkMenuItem : MenuItem {
		public TkMenuItem() {
			// Ideally open we want the event to trigger from the parent's context menu
			var contextMenu = WpfUtilities.FindDirectParentControl<ContextMenu>(this);

			if (contextMenu != null) {
				contextMenu.Opened += _menuOpened;
			}
			else {
				this.Loaded += _menuOpened;
			}
		}

		private bool _assigned;

		private void _menuOpened(object sender, RoutedEventArgs e) {
			if (String.IsNullOrEmpty(Shortcut) && String.IsNullOrEmpty(ShortcutCmd))
				return;

			var parent = WpfUtilities.FindDirectParentControl<Window>(this) ?? WpfUtilities.TopWindow;

			if (parent != null) {
				var name = this.HeaderText;
				if (String.IsNullOrEmpty(name)) {
					name = (this.Header ?? "").ToString();
				}

				if (String.IsNullOrEmpty(name)) {
					name = this.Name;
				}

				string gestureCmd = String.IsNullOrEmpty(ShortcutCmd) ? (name ?? "") : ShortcutCmd;

				if (!_assigned && !String.IsNullOrEmpty(Shortcut)) {
					ApplicationShortcut.Link(ApplicationShortcut.FromString(String.IsNullOrEmpty(Shortcut) ? "NULL" : Shortcut, gestureCmd), () => {
						RoutedEventArgs arg = new RoutedEventArgs(ClickEvent, this);
						this.RaiseEvent(arg);
					}, parent);
					_assigned = true;
				}

				var gesture = ApplicationShortcut.GetGesture(gestureCmd);

				this.InputGestureText = ApplicationShortcut.FindDislayNameMenuItem(gesture);
			}

			if (!this._isInputStyleSet) {
				try {
					var grid = WpfUtilities.FindFirstChild<Grid>(this);

					if (grid != null) {
						var last = grid.Children.Cast<UIElement>().Last() as Grid;

						if (last != null) {
							var tb = last.Children.Cast<UIElement>().Last() as TextBlock;

							if (tb != null) {
								tb.VerticalAlignment = VerticalAlignment.Center;
								this._isInputStyleSet = true;
							}
						}
					}
				}
				catch {
					this._isInputStyleSet = true;
				}
			}

			//var contextMenu = WpfUtilities.FindDirectParentControl<ContextMenu>(this);
			//
			//if (contextMenu != null) {
			//	RoutedEventArgs arg = new RoutedEventArgs(SizeChangedEvent, contextMenu);
			//	contextMenu.RaiseEvent(arg);
			//}
		}

		public Func<bool> CanExecute {
			get { return (Func<bool>)GetValue(CanExecuteProperty); }
			set { SetValue(CanExecuteProperty, value); }
		}
		public static DependencyProperty CanExecuteProperty = DependencyProperty.Register("CanExecute", typeof(Func<bool>), typeof(TkMenuItem), new PropertyMetadata(new PropertyChangedCallback(OnCanExecuteChanged)));

		private static void OnCanExecuteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			MenuItem menuItem = d as MenuItem;

			if (menuItem != null) {
				menuItem.Loaded += delegate {
					var parent = WpfUtilities.FindDirectParentControl<MenuItem>(menuItem);

					if (parent != null) {
						var func = e.NewValue as Func<bool>;

						if (func != null) {
							parent.SubmenuOpened += delegate {
								menuItem.IsEnabled = func();
							};
						}
					}
				};
			}
		}

		public string Shortcut {
			get { return (string)GetValue(ShortcutProperty); }
			set { SetValue(ShortcutProperty, value); }
		}
		public static DependencyProperty ShortcutProperty = DependencyProperty.Register("Shortcut", typeof(string), typeof(TkMenuItem), null);

		public string ShortcutCmd {
			get { return (string)GetValue(ShortcutCmdProperty); }
			set { SetValue(ShortcutCmdProperty, value); }
		}
		public static DependencyProperty ShortcutCmdProperty = DependencyProperty.Register("ShortcutCmd", typeof(string), typeof(TkMenuItem), null);

		private bool _isInputStyleSet = false;

		public string IconPath {
			get { return (string)GetValue(IconPathProperty); }
			set { SetValue(IconPathProperty, value); }
		}
		public static DependencyProperty IconPathProperty = DependencyProperty.Register("IconPath", typeof(string), typeof(TkMenuItem), new PropertyMetadata("empty.png", new PropertyChangedCallback(OnIconPathChanged)));

		private static void OnIconPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			MenuItem menuItem = d as MenuItem;

			if (menuItem != null) {
				Image image = new Image();
				image.Source = ApplicationManager.GetResourceImage(e.NewValue.ToString());
				image.Width = 16;
				image.Height = 16;
				menuItem.Icon = image;
			}
		}

		public string HeaderText {
			get { return (string)GetValue(HeaderTextProperty); }
			set { SetValue(HeaderTextProperty, value); }
		}
		public static DependencyProperty HeaderTextProperty = DependencyProperty.Register("HeaderText", typeof(string), typeof(TkMenuItem), new PropertyMetadata(new PropertyChangedCallback(OnHeaderTextChanged)));

		private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			MenuItem menuItem = d as MenuItem;

			if (menuItem != null) {
				Grid grid = _getHeader(menuItem);
				grid.VerticalAlignment = VerticalAlignment.Center;

				//TextBlock block = new TextBlock { Padding = new Thickness(0, 1.5, 0, 0), VerticalAlignment = VerticalAlignment.Center, Text = e.NewValue.ToString() };
				TextBlock block = new TextBlock { Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center, Text = e.NewValue.ToString() };
				
				if (Methods.IsWinXP()) {
					block.Margin = new Thickness(2, 1, 0, 2);
					//block.Padding = new Thickness(30, 0, 0, 0);
				}

				block.SetValue(Grid.ColumnProperty, 0);
				grid.Children.Add(block);
			}
		}

		private static Grid _getHeader(MenuItem item) {
			if (item.Header == null) {
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(-1, GridUnitType.Auto) });

				item.Header = grid;
			}

			return item.Header as Grid;
		}
	}
}
