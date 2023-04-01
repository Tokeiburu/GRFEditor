using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TokeiLibrary.WPF.Styles.ListView {
	public static class WpfUtils {
		#region Dependency Properties
		public static readonly DependencyProperty IsGridSortableProperty;
		private static readonly DependencyPropertyKey LastSortedPropertyKey;
		private static readonly DependencyPropertyKey LastSortDirectionPropertyKey;
		public static DependencyProperty IsDraggableProperty;
		public static DependencyProperty IsMouseEffectOnProperty;
		public static DependencyProperty ImagePathProperty;
		public static DependencyProperty ReverseMouseContextMenuProperty;
		#endregion

		#region Static Constructor

		static WpfUtils() {
			ReverseMouseContextMenuProperty = DependencyProperty.RegisterAttached(
				"ReverseMouseContextMenu",
				typeof(bool),
				typeof(WpfUtils),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterReverseMouseContextMenu)));
			ImagePathProperty = DependencyProperty.RegisterAttached(
				"ImagePath",
				typeof(string),
				typeof(WpfUtils),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterImagePath)));
			IsDraggableProperty = DependencyProperty.RegisterAttached(
				"IsDraggable",
				typeof(bool),
				typeof(WpfUtils),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisteIsDraggable)));
			IsGridSortableProperty = DependencyProperty.RegisterAttached(
				"IsGridSortable",
				typeof(Boolean),
				typeof(WpfUtils),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterSortableGrid)));
			LastSortDirectionPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
				"LastSortDirection",
				typeof(ListSortDirection),
				typeof(WpfUtils),
				new PropertyMetadata());
			LastSortedPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
				"LastSorted",
				typeof(GridViewColumnHeader),
				typeof(WpfUtils),
				new PropertyMetadata());
		}

		private static void OnRegisterReverseMouseContextMenu(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			Button element = d as Button;

			if (element != null) {
				Action action = delegate {
					element.ContextMenu.Placement = PlacementMode.Bottom;
					element.ContextMenu.PlacementTarget = element;
					element.PreviewMouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e1) { e1.Handled = true; };

					element.Click += delegate {
						element.ContextMenu.IsOpen = true;
					};
				};

				if (element.ContextMenu == null) {
					element.Loaded += delegate {
						action();
					};
				}
				else {
					action();
				}
			}

			FancyButton fElement = d as FancyButton;

			if (fElement != null) {
				Action action = delegate {
					fElement.ContextMenu.Placement = PlacementMode.Bottom;
					fElement.ContextMenu.PlacementTarget = fElement;
					fElement.PreviewMouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e1) { e1.Handled = true; };

					fElement.Click += delegate {
						fElement.ContextMenu.IsOpen = true;
					};
				};

				if (fElement.ContextMenu == null) {
					fElement.Loaded += delegate {
						action();
					};
				}
				else {
					action();
				}
			}
		}

		private static void OnRegisterImagePath(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			Image element = d as Image;

			try {
				if (element != null) {
					element.Source = ApplicationManager.PreloadResourceImage(e.NewValue.ToString());
					element.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
					element.Stretch = Stretch.None;
				}

				MenuItem mi = d as MenuItem;
				if (mi != null) {
					Image image = new Image { Source = ApplicationManager.PreloadResourceImage(e.NewValue.ToString()), Stretch = Stretch.None };
					image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
					mi.Icon = image;
				}

				Button b = d as Button;
				if (b != null) {
					Image image = new Image { Source = ApplicationManager.PreloadResourceImage(e.NewValue.ToString()), Stretch = Stretch.None };
					image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
					b.Content = image;
				}
			}
			catch { }
		}

		#endregion

		#region Attached Property Setters/Getters

		public static Boolean GetReverseMouseContextMenu(DependencyObject obj) {
			return (Boolean)obj.GetValue(ReverseMouseContextMenuProperty);
		}

		public static void SetReverseMouseContextMenu(DependencyObject obj, Boolean value) {
			obj.SetValue(ReverseMouseContextMenuProperty, value);
		}

		public static Boolean GetIsMouseEffectOn(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsMouseEffectOnProperty);
		}

		public static void SetIsMouseEffectOn(DependencyObject obj, Boolean value) {
			obj.SetValue(IsMouseEffectOnProperty, value);
		}

		public static Boolean GetIsDraggable(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsDraggableProperty);
		}

		public static void SetIsDraggable(DependencyObject obj, Boolean value) {
			obj.SetValue(IsDraggableProperty, value);
		}

		public static string GetImagePath(DependencyObject obj) {
			return (string) obj.GetValue(ImagePathProperty);
		}

		public static void SetImagePath(DependencyObject obj, string value) {
			obj.SetValue(ImagePathProperty, value);
		}

		public static Boolean GetIsGridSortable(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsGridSortableProperty);
		}

		public static void SetIsGridSortable(DependencyObject obj, Boolean value) {
			obj.SetValue(IsGridSortableProperty, value);
		}

		public static GridViewColumnHeader GetLastSorted(DependencyObject obj) {
			return obj.GetValue(LastSortedPropertyKey.DependencyProperty) as GridViewColumnHeader;
		}

		private static void SetLastSorted(DependencyObject obj, GridViewColumnHeader value) {
			obj.SetValue(LastSortedPropertyKey, value);
		}

		public static ListSortDirection GetLastSortDirection(DependencyObject obj) {
			return (ListSortDirection)obj.GetValue(LastSortDirectionPropertyKey.DependencyProperty);
		}

		public static void SetLastSortDirection(DependencyObject obj, ListSortDirection value) {
			obj.SetValue(LastSortDirectionPropertyKey, value);
		}

		#endregion

		#region PropertyChangedHandlers

		private static void OnRegisterSortableGrid(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			System.Windows.Controls.ListView grid = sender as System.Windows.Controls.ListView;
			if (grid != null) {
				RegisterSortableGridview(grid, args);
			}
		}

		private static void OnRegisteIsDraggable(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			UIElement grid = sender as UIElement;
			if (grid != null) {
				if (true) {
					grid.MouseDown += new MouseButtonEventHandler(_grid_MouseDown);
				}
			}
		}

		private static void OnRegisterIsMouseEffectOn(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			UIElement grid = sender as UIElement;
			if (grid != null) {
				//if (true) {
				//    if (grid is TabItem) {
				//        var tabItem = (TabItem)grid;

				//        Action action = new Action(delegate {
				//            grid = tabItem.Header as UIElement;
				//        });

				//        if (tabItem.IsLoaded) {
				//            action();
				//        }
				//        else {
				//            tabItem.Loaded += delegate {
				//                action();
				//            };
				//        }
				//    }
				//    else {
				//        AddMouseInOutEffects(grid);
				//    }
				//}
			}
		}

		private static void _grid_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				Window window = WpfUtilities.FindParentControl<Window>((DependencyObject) sender);

				if (window != null) {
					window.DragMove();
				}
			}
		}

		#endregion

		private static readonly RoutedEventHandler GridViewColumnHeaderClickHandler = new RoutedEventHandler(GridViewColumnHeaderClicked);

		public static DependencyProperty SortBindingMemberProperty = DependencyProperty.RegisterAttached(
			"SortBindingMember",
			typeof(BindingBase),
			typeof(WpfUtils));

		public readonly static DependencyProperty IsListviewSortableProperty = DependencyProperty.RegisterAttached(
			"IsListviewSortable",
			typeof(Boolean),
			typeof(WpfUtils),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnRegisterSortableGrid)));

		public static DependencyProperty CustomSorterProperty = DependencyProperty.RegisterAttached(
			"CustomSorter",
			typeof(IComparer),
			typeof(WpfUtils));

		private static void RegisterSortableGridview(System.Windows.Controls.ListView grid, DependencyPropertyChangedEventArgs args) {
			if (args.NewValue is Boolean && (Boolean)args.NewValue) {
				grid.AddHandler(ButtonBase.ClickEvent, GridViewColumnHeaderClickHandler);
			}
			else {
				grid.RemoveHandler(ButtonBase.ClickEvent, GridViewColumnHeaderClickHandler);
			}
		}

		public static void AddDragDropEffects(Control control, Func<List<string>, bool> condition = null) {
			Brush oldBrush = control.Background;

			control.PreviewDragEnter += delegate(object sender, DragEventArgs e) {
				oldBrush = control.Background;

				if (condition != null) {
					List<string> files = _getFiles(e);

					if (files != null && condition(files)) {
						control.Background = Application.Current.Resources["UIDragDropBrush"] as Brush;
					}
				}
				else
					control.Background = Application.Current.Resources["UIDragDropBrush"] as Brush;
			};

			control.PreviewDragLeave += delegate {
				control.Background = oldBrush;
			};

			control.PreviewDrop += delegate {
				control.Background = oldBrush;
			};
		}

		private static List<string> _getFiles(DragEventArgs e) {
			string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

			if (files != null) {
				return files.ToList();
			}

			return null;
		}

		private static void GridViewColumnHeaderClicked(object sender, RoutedEventArgs e) {
			System.Windows.Controls.ListView lv = sender as System.Windows.Controls.ListView;
			if (lv != null) {
				GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;

				if (header != null) {
					bool isClickable = (bool) header.GetValue(LayoutColumn.IsClickableColumnProperty);

					if (!isClickable)
						return;

					ListSortDirection sortDirection;
					GridViewColumnHeader tmpHeader = GetLastSorted(lv);

					if (tmpHeader != null) {
						if (tmpHeader.Column == null)
							return;
						tmpHeader.Column.HeaderTemplate = null;
					}

					if (header != tmpHeader) {
						sortDirection = ListSortDirection.Ascending;

						if (tmpHeader != null) {
							((Path) ((Canvas) ((Grid) tmpHeader.Content).Children[1]).Children[0]).Data = Geometry.Parse("");
							((Grid)tmpHeader.Content).Children[1].Visibility = Visibility.Collapsed;
						}

						((Grid)header.Content).Children[1].Visibility = Visibility.Visible;
						((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).Data = Geometry.Parse("M 1,2 7,2 4,-2 1,2 7,2");
					}
					else {
						ListSortDirection tmpDirection = GetLastSortDirection(lv);
						if (tmpDirection == ListSortDirection.Ascending) {
							sortDirection = ListSortDirection.Descending;
							((Grid) header.Content).Children[1].Visibility = Visibility.Visible;
							((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).Data = Geometry.Parse("M 1,-2 7,-2 4,2 1,-2 7,-2");
						}
						else {
							sortDirection = ListSortDirection.Ascending;
							((Grid)header.Content).Children[1].Visibility = Visibility.Visible;
							((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).Data = Geometry.Parse("M 1,2 7,2 4,-2 1,2 7,2");
						}
					}
					SetLastSorted(lv, header);
					SetLastSortDirection(lv, sortDirection);
					string resourceTemplateName = "";
					switch (sortDirection) {
						case ListSortDirection.Ascending: resourceTemplateName = "HeaderTemplateSortAsc"; break;
						case ListSortDirection.Descending: resourceTemplateName = "HeaderTemplateSortDesc"; break;
					}
					DataTemplate tmpTemplate = lv.TryFindResource(resourceTemplateName) as DataTemplate;
					if (tmpTemplate != null) {
						header.Column.HeaderTemplate = tmpTemplate;
					}
					Sort(lv);
				}
			}
		}

		public static IComparer GetCustomSorter(DependencyObject obj) {
			return (IComparer)obj.GetValue(CustomSorterProperty);
		}

		public static void SetCustomSorter(DependencyObject obj, IComparer value) {
			obj.SetValue(CustomSorterProperty, value);
		}

		public static BindingBase GetSortBindingMember(DependencyObject obj) {
			return (BindingBase)obj.GetValue(SortBindingMemberProperty);
		}

		public static void SetSortBindingMember(DependencyObject obj, BindingBase value) {
			obj.SetValue(SortBindingMemberProperty, value);
		}

		public static Boolean GetIsListviewSortable(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsListviewSortableProperty);
		}

		public static void SetIsListviewSortable(DependencyObject obj, Boolean value) {
			obj.SetValue(IsListviewSortableProperty, value);
		}

		public static void Sort(System.Windows.Controls.ListView lv) {
			new Thread(() => SyncSort(lv)) { Name = "GrfEditor - ListView sort thread" }.Start();
		}

		public static string GetLastGetSearchAccessor(System.Windows.Controls.ListView lv) {
			string headerProperty;

			try {
				headerProperty = lv.Dispatcher.Invoke(new Func<object>(delegate {
					try {
						if (GetLastSorted(lv) == null)
							return null;

						return GetLastSorted(lv).GetValue(LayoutColumn.GetAccessorBindingProperty) ??
						       ((FixedWidthColumn) GetLastSorted(lv).Column).BindingExpression;
					}
					catch {
						try {
							return ((FixedWidthColumn)GetLastSorted(lv).Column).BindingExpression;
						}
						catch (Exception) {
							return null;
						}
					}
				})) as string;
			}
			catch {
				headerProperty = null;
			}

			return headerProperty;
		}

		public static void SyncSort(System.Windows.Controls.ListView lv) {
			Cursor oldCursor = (Cursor) lv.Dispatcher.Invoke(new Func<Cursor>(() => lv.Cursor));
			lv.Dispatch(p => p.Cursor = Cursors.Wait);

			try {
				string headerProperty;

				try {
					headerProperty = lv.Dispatcher.Invoke(new Func<object>(delegate {
						try {
							if (GetLastSorted(lv) == null)
								return null;

							return GetLastSorted(lv).GetValue(LayoutColumn.GetAccessorBindingProperty) ??
								((FixedWidthColumn)GetLastSorted(lv).Column).BindingExpression;
						}
						catch {
							try {
								return ((FixedWidthColumn)GetLastSorted(lv).Column).BindingExpression;
							}
							catch (Exception) {
								return null;
							}
						}
					})) as string;

					//if (headerProperty == null)
					//    return;
				}
				catch {
					headerProperty = null;
				}

				lv.Dispatcher.Invoke(new Action(delegate {
					if (lv.ItemsSource == null)
						return;

					ListCollectionView view = (ListCollectionView) CollectionViewSource.GetDefaultView(lv.ItemsSource);
					ListViewCustomComparer sorter = (ListViewCustomComparer) GetCustomSorter(lv);

					//if (sorter != null) {
					if (sorter != null && !String.IsNullOrEmpty(headerProperty)) {
						sorter.SetSort(headerProperty, GetLastSortDirection(lv));
						view.CustomSort = sorter;
						lv.Items.Refresh();
					}
				}));
			}
			catch { }
			finally {
				lv.Dispatch(p => p.Cursor = oldCursor);
			}
		}

		public static void CopyContent(System.Windows.Controls.ListView debugList) {
			StringBuilder builder = new StringBuilder();

			foreach (var obj in debugList.SelectedItems) {
				builder.AppendLine(obj.ToString());
			}

			Clipboard.SetDataObject(builder.ToString());
		}

		public static void DisableContextMenuIfEmpty(System.Windows.Controls.ListView list) {
			list.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e) {
				object lvi = list.GetObjectAtPoint<ListViewItem>(e.GetPosition(list));

				if (lvi == null) {
					e.Handled = true;
				}
			};
		}

		public static void AddMouseInOutEffects(UIElement image) {
			image.MouseEnter += delegate {
				Mouse.OverrideCursor = Cursors.Hand;
			};

			image.MouseLeave += delegate {
				Mouse.OverrideCursor = null;
			};
		}

		public static void AddMouseInOutEffectsBox(params CheckBox[] boxes) {
			foreach (var box in boxes) {
				AddMouseInOutEffectsBox(box);
			}
		}

		public static void AddMouseInOutEffectsBox(params RadioButton[] boxes) {
			foreach (var box in boxes) {
				AddMouseInOutEffectsBox(box);
			}
		}

		public static void AddMouseInOutEffectsBox(CheckBox box) {
			if (box.Content is string) {
				box.Content = new TextBlock { Text = box.Content.ToString(), TextWrapping = TextWrapping.Wrap };
			}

			var tb = box.Content as TextBlock;

			box.MouseEnter += delegate {
				Mouse.OverrideCursor = Cursors.Hand;
				box.Foreground = Application.Current.Resources["MouseOverTextBrush"] as SolidColorBrush;
				if (tb != null)
					tb.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
			};

			box.MouseLeave += delegate {
				Mouse.OverrideCursor = null;
				box.Foreground = Application.Current.Resources["TextForeground"] as SolidColorBrush;
				if (tb != null)
					tb.SetValue(TextBlock.TextDecorationsProperty, null);
			};
		}

		public static void AddMouseInOutEffectsBox(RadioButton box) {
			if (box.Content is string) {
				box.Content = new TextBlock { Text = box.Content.ToString(), TextWrapping = TextWrapping.Wrap };
			}

			var tb = box.Content as TextBlock;

			box.MouseEnter += delegate {
				Mouse.OverrideCursor = Cursors.Hand;
				box.Foreground = Application.Current.Resources["MouseOverTextBrush"] as SolidColorBrush;
				if (tb != null)
					tb.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
			};

			box.MouseLeave += delegate {
				Mouse.OverrideCursor = null;
				box.Foreground = Application.Current.Resources["TextForeground"] as SolidColorBrush;
				if (tb != null)
					tb.SetValue(TextBlock.TextDecorationsProperty, null);
			};
		}
	}
}