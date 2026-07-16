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
	public static class ListViewExtensions {
		#region Dependency Properties
		public static readonly DependencyProperty IsGridSortableProperty;
		private static readonly DependencyPropertyKey LastSortedPropertyKey;
		private static readonly DependencyPropertyKey LastSortDirectionPropertyKey;
		public static readonly DependencyProperty TkVerticalAlignmentProperty;
		#endregion

		#region Static Constructor
		static ListViewExtensions() {
			IsGridSortableProperty = DependencyProperty.RegisterAttached(
				"IsGridSortable",
				typeof(Boolean),
				typeof(ListViewExtensions),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterSortableGrid)));
			LastSortDirectionPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
				"LastSortDirection",
				typeof(ListSortDirection),
				typeof(ListViewExtensions),
				new PropertyMetadata());
			LastSortedPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
				"LastSorted",
				typeof(GridViewColumnHeader),
				typeof(ListViewExtensions),
				new PropertyMetadata());
			TkVerticalAlignmentProperty = DependencyProperty.RegisterAttached(
				"TkVerticalAlignment",
				typeof(VerticalAlignment),
				typeof(ListViewExtensions),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterTkVerticalAlignment)));
		}
		#endregion

		#region Attached Property Setters/Getters
		public static Boolean GetIsGridSortable(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsGridSortableProperty);
		}

		public static void SetIsGridSortable(DependencyObject obj, Boolean value) {
			obj.SetValue(IsGridSortableProperty, value);
		}

		public static GridViewColumnHeader GetLastSorted(DependencyObject obj) {
			return obj.GetValue(LastSortedPropertyKey.DependencyProperty) as GridViewColumnHeader;
		}

		public static void SetLastSorted(DependencyObject obj, GridViewColumnHeader value) {
			obj.SetValue(LastSortedPropertyKey, value);
		}

		public static ListSortDirection GetLastSortDirection(DependencyObject obj) {
			return (ListSortDirection)obj.GetValue(LastSortDirectionPropertyKey.DependencyProperty);
		}

		public static void SetLastSortDirection(DependencyObject obj, ListSortDirection value) {
			obj.SetValue(LastSortDirectionPropertyKey, value);
		}

		public static VerticalAlignment GetTkVerticalAlignment(DependencyObject obj) {
			return (VerticalAlignment)obj.GetValue(TkVerticalAlignmentProperty);
		}

		public static void SetTkVerticalAlignment(DependencyObject obj, VerticalAlignment value) {
			obj.SetValue(TkVerticalAlignmentProperty, value);
		}
		#endregion

		#region PropertyChangedHandlers
		private static void OnRegisterSortableGrid(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			System.Windows.Controls.ListView grid = sender as System.Windows.Controls.ListView;
			if (grid != null) {
				RegisterSortableGridview(grid, args);
			}
		}

		private static void OnRegisterTkVerticalAlignment(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			FrameworkElement grid = sender as FrameworkElement;
			if (grid != null) {
				RegisterSortableGridview(grid, args);
			}
		}

		private static void OnRegisterUpdateOnSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
			System.Windows.Controls.ListView grid = sender as System.Windows.Controls.ListView;
			if (grid != null) {
				RegisterUpdateOnSizeChanged(grid, args);
			}
		}
		#endregion

		private static readonly RoutedEventHandler GridViewColumnHeaderClickHandler = new RoutedEventHandler(GridViewColumnHeaderClicked);

		public static DependencyProperty SortBindingMemberProperty = DependencyProperty.RegisterAttached(
			"SortBindingMember",
			typeof(BindingBase),
			typeof(ListViewExtensions));

		public readonly static DependencyProperty IsListviewSortableProperty = DependencyProperty.RegisterAttached(
			"IsListviewSortable",
			typeof(Boolean),
			typeof(ListViewExtensions),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnRegisterSortableGrid)));

		public static DependencyProperty CustomSorterProperty = DependencyProperty.RegisterAttached(
			"CustomSorter",
			typeof(IComparer),
			typeof(ListViewExtensions));
		
		public static DependencyProperty UpdateOnSizeChangedProperty = DependencyProperty.RegisterAttached(
			"UpdateOnSizeChanged",
			typeof(bool),
			typeof(ListViewExtensions),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnRegisterUpdateOnSizeChanged)));

		private static void RegisterSortableGridview(System.Windows.Controls.ListView grid, DependencyPropertyChangedEventArgs args) {
			if (args.NewValue is Boolean && (Boolean)args.NewValue) {
				grid.AddHandler(ButtonBase.ClickEvent, GridViewColumnHeaderClickHandler);
			}
			else {
				grid.RemoveHandler(ButtonBase.ClickEvent, GridViewColumnHeaderClickHandler);
			}
		}

		private static void RegisterUpdateOnSizeChanged(System.Windows.Controls.ListView list, DependencyPropertyChangedEventArgs args) {
			if (args.NewValue is Boolean value && value == true) {
				list.SizeChanged += (sender, e) => {
					double width = list.ActualWidth;
					double totalWidth;

					GridViewColumnCollection collection = ((GridView)list.View).Columns;

					totalWidth = collection.OfType<FixedWidthColumn>().Where(p => !p.IsFill).Sum(p => p.ActualWidth);

					List<FixedWidthColumn> toFillColumns = collection.OfType<FixedWidthColumn>().Where(p => p.IsFill).ToList();

					int numOfColumns = toFillColumns.Count;

					if (numOfColumns > 0) {
						double remainingWidth = width - totalWidth - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
						double widthPerColumn = remainingWidth / numOfColumns;

						if (widthPerColumn < 0)
							return;

						toFillColumns.ForEach(p => p.FixedWidth = widthPerColumn);
					}
				};
			}
		}

		private static void RegisterSortableGridview(FrameworkElement grid, DependencyPropertyChangedEventArgs args) {
			if (args.NewValue is VerticalAlignment) {
				grid.VerticalAlignment = VerticalAlignment.Top;

				VerticalAlignment alignment = (VerticalAlignment)args.NewValue;

				if (alignment == VerticalAlignment.Center) {
					var res = WpfUtilities.FindDirectParentControl<FrameworkElement>(grid);

					if (res != null) {
						int absoluteTopMargin = 0;

						if (res is TabItem) {
							res = WpfUtilities.FindDirectParentControl<TabControl>(res);
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
						((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).SetResourceReference(Path.FillProperty, "TextForeground");
					}
					else {
						ListSortDirection tmpDirection = GetLastSortDirection(lv);
						if (tmpDirection == ListSortDirection.Ascending) {
							sortDirection = ListSortDirection.Descending;
							((Grid) header.Content).Children[1].Visibility = Visibility.Visible;
							((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).Data = Geometry.Parse("M 1,-2 7,-2 4,2 1,-2 7,-2");
							((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).SetResourceReference(Path.FillProperty, "TextForeground");
						}
						else {
							sortDirection = ListSortDirection.Ascending;
							((Grid)header.Content).Children[1].Visibility = Visibility.Visible;
							((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).Data = Geometry.Parse("M 1,2 7,2 4,-2 1,2 7,2");
							((Path)((Canvas)((Grid)header.Content).Children[1]).Children[0]).SetResourceReference(Path.FillProperty, "TextForeground");
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

		public static void SetUpdateOnSizeChanged(DependencyObject obj, bool value) {
			obj.SetValue(UpdateOnSizeChangedProperty, value);
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
				headerProperty = lv.Dispatch(delegate {
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
				}) as string;
			}
			catch {
				headerProperty = null;
			}

			return headerProperty;
		}

		public static void SyncSort(System.Windows.Controls.ListView lv) {
			Cursor oldCursor = (Cursor) lv.Dispatch(() => lv.Cursor);
			lv.Dispatch(p => p.Cursor = Cursors.Wait);

			try {
				string headerProperty;

				try {
					headerProperty = lv.Dispatch(delegate {
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
					}) as string;

					//if (headerProperty == null)
					//    return;
				}
				catch {
					headerProperty = null;
				}

				lv.Dispatch(delegate {
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
				});
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

		public static void SetCustomSortOnItemsSourceChanged(System.Windows.Controls.ListView list) {
			_itemsSourceDescriptor.RemoveValueChanged(list, OnItemsSourceChanged);
			_itemsSourceDescriptor.AddValueChanged(list, OnItemsSourceChanged);

			ApplyCustomSort(list);
		}

		private static readonly DependencyPropertyDescriptor _itemsSourceDescriptor =
			DependencyPropertyDescriptor.FromProperty(
				ItemsControl.ItemsSourceProperty,
				typeof(System.Windows.Controls.ListView));

		private static void OnItemsSourceChanged(object sender, EventArgs e) {
			ApplyCustomSort((System.Windows.Controls.ListView)sender);
		}

		private static void ApplyCustomSort(System.Windows.Controls.ListView list) {
			if (list.ItemsSource == null)
				return;

			var view = CollectionViewSource.GetDefaultView(list.ItemsSource) as ListCollectionView;
			if (view == null)
				return;

			if (view.CustomSort != null)
				return;

			var sorter = GetCustomSorter(list);
			if (sorter == null)
				return;

			view.CustomSort = sorter;
			_itemsSourceDescriptor.RemoveValueChanged(list, OnItemsSourceChanged);
		}
	}
}