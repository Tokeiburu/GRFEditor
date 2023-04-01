using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TokeiLibrary.WPF.Styles.ListView {
	public class ListViewWidthConverter : IValueConverter {
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (targetType != typeof(double)) { return null; }
			//if (!(value is TextBlock)) { return 0; }


			return value;
			//TextBlock text = (TextBlock)value;

			//double dToAdjust = parameter == null ? 0 : double.Parse(parameter.ToString());
			//double dAdjustedWidth = dParentWidth + dToAdjust - 10;

			//if (_isScrollBarVisible(errorItemView.ListView)) {
			//    dAdjustedWidth -= SystemParameters.VerticalScrollBarWidth;
			//}

			//return (dAdjustedWidth < 0 ? 0 : dAdjustedWidth);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion

		private static bool _isScrollBarVisible(DependencyObject view) {
			ScrollViewer[] sv = WpfUtilities.FindChildren<ScrollViewer>(view);

			if (sv.Length > 0)
				return sv[0].ComputedVerticalScrollBarVisibility == Visibility.Visible;

			return false;
		}
	}
}
