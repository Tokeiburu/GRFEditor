using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TokeiLibrary.WPF.Styles.ListView {
	public class ListViewLayoutManager {
		private const double zeroWidthRange = 0.1;

		public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
			"Enabled",
			typeof (bool),
			typeof (ListViewLayoutManager),
			new FrameworkPropertyMetadata(new PropertyChangedCallback(OnLayoutManagerEnabledChanged)));

		private readonly System.Windows.Controls.ListView listView;
		private GridViewColumn autoSizedColumn;
		private bool loaded;
		private Cursor resizeCursor;
		private bool resizing;
		private ScrollViewer scrollViewer;
		private ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Auto;


		public ListViewLayoutManager(System.Windows.Controls.ListView listView) {
			if (listView == null) {
				throw new ArgumentNullException("listView");
			}

			this.listView = listView;
			this.listView.Loaded += new RoutedEventHandler(ListViewLoaded);
			this.listView.Unloaded += new RoutedEventHandler(ListViewUnloaded);
		}


		public System.Windows.Controls.ListView ListView {
			get { return listView; }
		}


		public ScrollBarVisibility VerticalScrollBarVisibility {
			get { return verticalScrollBarVisibility; }
			set { verticalScrollBarVisibility = value; }
		}


		public static void SetEnabled(DependencyObject dependencyObject, bool enabled) {
			dependencyObject.SetValue(EnabledProperty, enabled);
		}


		public void Refresh() {
			InitColumns();
			DoResizeColumns();
		}


		private void RegisterEvents(DependencyObject start) {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++) {
				Visual childVisual = VisualTreeHelper.GetChild(start, i) as Visual;
				if (childVisual is Thumb) {
					GridViewColumn gridViewColumn = FindParentColumn(childVisual);
					if (gridViewColumn != null) {
						Thumb thumb = childVisual as Thumb;
						if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
						    FixedColumn.IsFixedColumn(gridViewColumn) || IsFillColumn(gridViewColumn)) {
							thumb.IsHitTestVisible = false;
						}
						else {
							thumb.PreviewMouseMove += new MouseEventHandler(ThumbPreviewMouseMove);
							thumb.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ThumbPreviewMouseLeftButtonDown);
							DependencyPropertyDescriptor.FromProperty(
								GridViewColumn.WidthProperty,
								typeof (GridViewColumn)).AddValueChanged(gridViewColumn, GridColumnWidthChanged);
						}
					}
				}
				else if (childVisual is GridViewColumnHeader) {
					GridViewColumnHeader columnHeader = childVisual as GridViewColumnHeader;
					columnHeader.SizeChanged += new SizeChangedEventHandler(GridColumnHeaderSizeChanged);
				}
				else if (scrollViewer == null && childVisual is ScrollViewer) {
					scrollViewer = childVisual as ScrollViewer;
					scrollViewer.ScrollChanged += new ScrollChangedEventHandler(ScrollViewerScrollChanged);

					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
					scrollViewer.VerticalScrollBarVisibility = verticalScrollBarVisibility;
				}

				RegisterEvents(childVisual);
			}
		}


		private void UnregisterEvents(DependencyObject start) {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++) {
				Visual childVisual = VisualTreeHelper.GetChild(start, i) as Visual;
				if (childVisual is Thumb) {
					GridViewColumn gridViewColumn = FindParentColumn(childVisual);
					if (gridViewColumn != null) {
						Thumb thumb = childVisual as Thumb;
						if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
						    FixedColumn.IsFixedColumn(gridViewColumn) || IsFillColumn(gridViewColumn)) {
							thumb.IsHitTestVisible = true;
						}
						else {
							thumb.PreviewMouseMove -= new MouseEventHandler(ThumbPreviewMouseMove);
							thumb.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(ThumbPreviewMouseLeftButtonDown);
							DependencyPropertyDescriptor.FromProperty(
								GridViewColumn.WidthProperty,
								typeof (GridViewColumn)).RemoveValueChanged(gridViewColumn, GridColumnWidthChanged);
						}
					}
				}
				else if (childVisual is GridViewColumnHeader) {
					GridViewColumnHeader columnHeader = childVisual as GridViewColumnHeader;
					columnHeader.SizeChanged -= new SizeChangedEventHandler(GridColumnHeaderSizeChanged);
				}
				else if (scrollViewer == null && childVisual is ScrollViewer) {
					scrollViewer = childVisual as ScrollViewer;
					scrollViewer.ScrollChanged -= new ScrollChangedEventHandler(ScrollViewerScrollChanged);
				}

				UnregisterEvents(childVisual);
			}
		}


		private GridViewColumn FindParentColumn(DependencyObject element) {
			if (element == null) {
				return null;
			}

			while (element != null) {
				GridViewColumnHeader gridViewColumnHeader = element as GridViewColumnHeader;
				if (gridViewColumnHeader != null) {
					return (gridViewColumnHeader).Column;
				}
				element = VisualTreeHelper.GetParent(element);
			}

			return null;
		}


		private GridViewColumnHeader FindColumnHeader(DependencyObject start, GridViewColumn gridViewColumn) {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++) {
				Visual childVisual = VisualTreeHelper.GetChild(start, i) as Visual;
				if (childVisual is GridViewColumnHeader) {
					GridViewColumnHeader gridViewHeader = childVisual as GridViewColumnHeader;
					if (gridViewHeader.Column == gridViewColumn) {
						return gridViewHeader;
					}
				}
				GridViewColumnHeader childGridViewHeader = FindColumnHeader(childVisual, gridViewColumn);
				if (childGridViewHeader != null) {
					return childGridViewHeader;
				}
			}
			return null;
		}


		private void InitColumns() {
			GridView view = listView.View as GridView;
			if (view == null) {
				return;
			}

			foreach (GridViewColumn gridViewColumn in view.Columns) {
				if (!RangeColumn.IsRangeColumn(gridViewColumn)) {
					continue;
				}

				double? minWidth = RangeColumn.GetRangeMinWidth(gridViewColumn);
				double? maxWidth = RangeColumn.GetRangeMaxWidth(gridViewColumn);
				if (!minWidth.HasValue && !maxWidth.HasValue) {
					continue;
				}

				GridViewColumnHeader columnHeader = FindColumnHeader(listView, gridViewColumn);
				if (columnHeader == null) {
					continue;
				}

				double actualWidth = columnHeader.ActualWidth;
				if (minWidth.HasValue) {
					columnHeader.MinWidth = minWidth.Value;
					if (!double.IsInfinity(actualWidth) && actualWidth < columnHeader.MinWidth) {
						gridViewColumn.Width = columnHeader.MinWidth;
					}
				}
				if (maxWidth.HasValue) {
					columnHeader.MaxWidth = maxWidth.Value;
					if (!double.IsInfinity(actualWidth) && actualWidth > columnHeader.MaxWidth) {
						gridViewColumn.Width = columnHeader.MaxWidth;
					}
				}
			}
		}


		protected virtual void ResizeColumns() {
			GridView view = listView.View as GridView;
			if (view == null || view.Columns.Count == 0) {
				return;
			}


			double actualWidth = double.PositiveInfinity;
			if (scrollViewer != null) {
				actualWidth = scrollViewer.ViewportWidth;
			}
			if (double.IsInfinity(actualWidth)) {
				actualWidth = listView.ActualWidth;
			}
			if (double.IsInfinity(actualWidth) || actualWidth <= 0) {
				return;
			}

			double resizeableRegionCount = 0;
			double otherColumnsWidth = 0;

			foreach (GridViewColumn gridViewColumn in view.Columns) {
				if (ProportionalColumn.IsProportionalColumn(gridViewColumn)) {
					double? proportionalWidth = ProportionalColumn.GetProportionalWidth(gridViewColumn);
					if (proportionalWidth != null) {
						resizeableRegionCount += proportionalWidth.Value;
					}
				}
				else {
					otherColumnsWidth += gridViewColumn.ActualWidth;
				}
			}

			if (resizeableRegionCount <= 0) {
				if (scrollViewer != null) {
					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
				}


				GridViewColumn fillColumn = null;
				for (int i = 0; i < view.Columns.Count; i++) {
					GridViewColumn gridViewColumn = view.Columns[i];
					if (IsFillColumn(gridViewColumn)) {
						fillColumn = gridViewColumn;
						break;
					}
				}

				if (fillColumn != null) {
					double otherColumnsWithoutFillWidth = otherColumnsWidth - fillColumn.ActualWidth;
					double fillWidth = actualWidth - otherColumnsWithoutFillWidth;
					if (fillWidth > 0) {
						double? minWidth = RangeColumn.GetRangeMinWidth(fillColumn);
						double? maxWidth = RangeColumn.GetRangeMaxWidth(fillColumn);

						bool setWidth = !(minWidth.HasValue && fillWidth < minWidth.Value);
						if (maxWidth.HasValue && fillWidth > maxWidth.Value) {
							setWidth = false;
						}
						if (setWidth) {
							if (scrollViewer != null) {
								scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
							}
							fillColumn.Width = fillWidth;
						}
					}
				}
				return;
			}

			double resizeableColumnsWidth = actualWidth - otherColumnsWidth;
			if (resizeableColumnsWidth <= 0) {
				return;
			}


			double resizeableRegionWidth = resizeableColumnsWidth / resizeableRegionCount;
			foreach (GridViewColumn gridViewColumn in view.Columns) {
				if (ProportionalColumn.IsProportionalColumn(gridViewColumn)) {
					double? proportionalWidth = ProportionalColumn.GetProportionalWidth(gridViewColumn);
					if (proportionalWidth != null) {
						gridViewColumn.Width = proportionalWidth.Value * resizeableRegionWidth;
					}
				}
			}
		}


		private double SetRangeColumnToBounds(GridViewColumn gridViewColumn) {
			double startWidth = gridViewColumn.Width;

			double? minWidth = RangeColumn.GetRangeMinWidth(gridViewColumn);
			double? maxWidth = RangeColumn.GetRangeMaxWidth(gridViewColumn);

			if ((minWidth.HasValue && maxWidth.HasValue) && (minWidth > maxWidth)) {
				return 0;
			}

			if (minWidth.HasValue && gridViewColumn.Width < minWidth.Value) {
				gridViewColumn.Width = minWidth.Value;
			}
			else if (maxWidth.HasValue && gridViewColumn.Width > maxWidth.Value) {
				gridViewColumn.Width = maxWidth.Value;
			}

			return gridViewColumn.Width - startWidth;
		}


		private bool IsFillColumn(GridViewColumn gridViewColumn) {
			if (gridViewColumn == null) {
				return false;
			}

			GridView view = listView.View as GridView;
			if (view == null || view.Columns.Count == 0) {
				return false;
			}

			bool? isFillColumn = RangeColumn.GetRangeIsFillColumn(gridViewColumn);
			return isFillColumn.HasValue && isFillColumn.Value;
		}


		private void DoResizeColumns() {
			if (resizing) {
				return;
			}

			resizing = true;
			try {
				ResizeColumns();
			}
			finally {
				resizing = false;
			}
		}


		private void ListViewLoaded(object sender, RoutedEventArgs e) {
			RegisterEvents(listView);
			InitColumns();
			DoResizeColumns();
			loaded = true;
		}


		private void ListViewUnloaded(object sender, RoutedEventArgs e) {
			if (!loaded) {
				return;
			}
			UnregisterEvents(listView);
			loaded = false;
		}


		private void ThumbPreviewMouseMove(object sender, MouseEventArgs e) {
			Thumb thumb = sender as Thumb;
			if (thumb == null) {
				return;
			}
			GridViewColumn gridViewColumn = FindParentColumn(thumb);
			if (gridViewColumn == null) {
				return;
			}


			if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
			    FixedColumn.IsFixedColumn(gridViewColumn) ||
			    IsFillColumn(gridViewColumn)) {
				thumb.Cursor = null;
				return;
			}


			if (thumb.IsMouseCaptured && RangeColumn.IsRangeColumn(gridViewColumn)) {
				double? minWidth = RangeColumn.GetRangeMinWidth(gridViewColumn);
				double? maxWidth = RangeColumn.GetRangeMaxWidth(gridViewColumn);

				if ((minWidth.HasValue && maxWidth.HasValue) && (minWidth > maxWidth)) {
					return;
				}

				if (resizeCursor == null) {
					resizeCursor = thumb.Cursor;
				}

				if (minWidth.HasValue && gridViewColumn.Width <= minWidth.Value) {
					thumb.Cursor = Cursors.No;
				}
				else if (maxWidth.HasValue && gridViewColumn.Width >= maxWidth.Value) {
					thumb.Cursor = Cursors.No;
				}
				else {
					thumb.Cursor = resizeCursor;
				}
			}
		}


		private void ThumbPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			Thumb thumb = sender as Thumb;
			GridViewColumn gridViewColumn = FindParentColumn(thumb);


			if (ProportionalColumn.IsProportionalColumn(gridViewColumn) ||
			    FixedColumn.IsFixedColumn(gridViewColumn) ||
			    IsFillColumn(gridViewColumn)) {
				e.Handled = true;
			}
		}


		private void GridColumnWidthChanged(object sender, EventArgs e) {
			if (!loaded) {
				return;
			}

			GridViewColumn gridViewColumn = sender as GridViewColumn;


			if (ProportionalColumn.IsProportionalColumn(gridViewColumn) || FixedColumn.IsFixedColumn(gridViewColumn)) {
				return;
			}


			if (RangeColumn.IsRangeColumn(gridViewColumn)) {
				if (gridViewColumn != null && gridViewColumn.Width.Equals(double.NaN)) {
					autoSizedColumn = gridViewColumn;
					return;
				}


				if (Math.Abs(SetRangeColumnToBounds(gridViewColumn) - 0) > zeroWidthRange) {
					return;
				}
			}

			DoResizeColumns();
		}


		private void GridColumnHeaderSizeChanged(object sender, SizeChangedEventArgs e) {
			if (autoSizedColumn == null) {
				return;
			}

			GridViewColumnHeader gridViewColumnHeader = sender as GridViewColumnHeader;
			if (gridViewColumnHeader != null && gridViewColumnHeader.Column == autoSizedColumn) {
				if (gridViewColumnHeader.Width.Equals(double.NaN)) {
					gridViewColumnHeader.Column.Width = gridViewColumnHeader.ActualWidth;
					DoResizeColumns();
				}

				autoSizedColumn = null;
			}
		}


		private void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e) {
			if (loaded && Math.Abs(e.ViewportWidthChange - 0) > zeroWidthRange) {
				DoResizeColumns();
			}
		}


		private static void OnLayoutManagerEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
			System.Windows.Controls.ListView listView = dependencyObject as System.Windows.Controls.ListView;
			if (listView != null) {
				bool enabled = (bool) e.NewValue;
				if (enabled) {
					new ListViewLayoutManager(listView);
				}
			}
		}
	}
}