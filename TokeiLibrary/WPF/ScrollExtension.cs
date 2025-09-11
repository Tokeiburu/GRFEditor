using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokeiLibrary.WPF {
	public static class DragDropExtension {
		#region ScrollOnDragDropProperty

		public static readonly DependencyProperty ScrollOnDragDropProperty =
			DependencyProperty.RegisterAttached("ScrollOnDragDrop",
				typeof(bool),
				typeof(DragDropExtension),
				new PropertyMetadata(false, HandleScrollOnDragDropChanged));

		public static bool GetScrollOnDragDrop(DependencyObject element) {
			if (element == null) {
				throw new ArgumentNullException("element");
			}

			return (bool)element.GetValue(ScrollOnDragDropProperty);
		}

		public static void SetScrollOnDragDrop(DependencyObject element, bool value) {
			if (element == null) {
				throw new ArgumentNullException("element");
			}

			element.SetValue(ScrollOnDragDropProperty, value);
		}

		private static void HandleScrollOnDragDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			FrameworkElement container = d as FrameworkElement;

			if (d == null) {
				return;
			}

			Unsubscribe(container);

			if (true.Equals(e.NewValue)) {
				Subscribe(container);
			}
		}

		private static void Subscribe(FrameworkElement container) {
			container.PreviewDragOver += OnContainerPreviewDragOver;
		}

		public static void OnContainerScroll(FrameworkElement container, Point mouseContainerPosition, double increment = 10d) {
			if (container == null) {
				return;
			}

			ScrollViewer scrollViewer = container as ScrollViewer ?? GetFirstVisualChild<ScrollViewer>(container);

			if (scrollViewer == null) {
				return;
			}

			double tolerance = 30;
			double verticalPos = mouseContainerPosition.Y;

			if (verticalPos < tolerance) {
				scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - increment); //Scroll up. 
			}
			else if (verticalPos > container.ActualHeight - tolerance) {
				scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + increment); //Scroll down.     
			}

			double horizontalPos = mouseContainerPosition.X;

			if (horizontalPos < tolerance) {
				scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - increment); //Scroll left
			}
			else if (horizontalPos > container.ActualWidth - tolerance) {
				scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + increment); //Scroll right
			}
		}

		public static void OnContainerPreviewDragOver(object sender, DragEventArgs e) {
			FrameworkElement container = sender as FrameworkElement;

			if (container == null) {
				return;
			}

			OnContainerScroll(container, e.GetPosition(container));
		}

		private static void Unsubscribe(FrameworkElement container) {
			container.PreviewDragOver -= OnContainerPreviewDragOver;
		}

		public static T GetFirstVisualChild<T>(DependencyObject depObj) where T : DependencyObject {
			if (depObj != null) {
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
					DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
					if (child != null && child is T) {
						return (T)child;
					}

					T childItem = GetFirstVisualChild<T>(child);
					if (childItem != null) {
						return childItem;
					}
				}
			}

			return null;
		}

		#endregion
	}
}
