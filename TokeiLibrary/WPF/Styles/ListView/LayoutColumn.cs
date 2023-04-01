using System;
using System.Windows;
using System.Windows.Controls;

namespace TokeiLibrary.WPF.Styles.ListView {
	public abstract class LayoutColumn {
		public static readonly DependencyProperty GetAccessorBindingProperty =
			DependencyProperty.RegisterAttached(
				"GetAccessorBinding",
				typeof (string),
				typeof(LayoutColumn));

		public static readonly DependencyProperty IsClickableColumnProperty =
			DependencyProperty.RegisterAttached(
				"IsClickableColumn",
				typeof (bool),
				typeof (FixedColumn));

		public string SearchGetAccessor { get; set; }

		public static double GetGetAccessorBinding(DependencyObject obj) {
			return (double)obj.GetValue(GetAccessorBindingProperty);
		}

		public static void SetGetAccessorBinding(DependencyObject obj, string binding) {
			obj.SetValue(GetAccessorBindingProperty, binding);
		}

		public static bool GetIsClickableColumn(DependencyObject obj) {
			return (bool) (obj.GetValue(IsClickableColumnProperty) ?? false);
		}

		public static void SetIsClickableColumn(DependencyObject obj, bool value) {
			obj.SetValue(IsClickableColumnProperty, value);
		}

		protected static bool HasPropertyValue(GridViewColumn column, DependencyProperty dp) {
			if (column == null) {
				throw new ArgumentNullException("column");
			}
			object value = column.ReadLocalValue(dp);
			if (value != null && value.GetType() == dp.PropertyType) {
				return true;
			}

			return false;
		}

		protected static double? GetColumnWidth(GridViewColumn column, DependencyProperty dp) {
			if (column == null) {
				throw new ArgumentNullException("column");
			}
			object value = column.ReadLocalValue(dp);
			if (value != null && value.GetType() == dp.PropertyType) {
				return (double) value;
			}

			return null;
		}
	}
}