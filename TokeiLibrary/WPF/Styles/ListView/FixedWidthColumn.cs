using System.Windows;
using System.Windows.Controls;

namespace TokeiLibrary.WPF.Styles.ListView {
	public class FixedWidthColumn : GridViewColumn {
		public bool IsFill { get; set; }
		public bool IsAuto { get; set; }

		#region Constructor
		static FixedWidthColumn() {
			WidthProperty.OverrideMetadata(typeof(FixedWidthColumn),
			                               new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
		}

		static object OnCoerceWidth(DependencyObject o, object baseValue) {
			FixedWidthColumn fwc = o as FixedWidthColumn;
			if (fwc != null)
				return fwc.FixedWidth;
			return 0.0;
		}
		#endregion

		#region FixedWidth

		public static readonly DependencyProperty FixedWidthProperty =
			DependencyProperty.Register("FixedWidth", typeof(double), typeof(FixedWidthColumn),
			                            new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnFixedWidthChanged)));

		public double FixedWidth {
			get { return (double)GetValue(FixedWidthProperty); }
			set { SetValue(FixedWidthProperty, value); }
		}

		private static void OnFixedWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			FixedWidthColumn fwc = o as FixedWidthColumn;
			if (fwc != null)
				fwc.CoerceValue(WidthProperty);
		}
		#endregion

		#region BindingExpression

		public static readonly DependencyProperty BindingExpressionProperty =
			DependencyProperty.Register("BindingExpression", typeof(string), typeof(FixedWidthColumn),
			                            new FrameworkPropertyMetadata(null));

		public string BindingExpression {
			get { return (string)GetValue(BindingExpressionProperty); }
			set { SetValue(BindingExpressionProperty, value); }
		}

		#endregion

		#region GetAccessorBinding

		public static readonly DependencyProperty GetAccessorBindingProperty =
			DependencyProperty.Register("GetAccessorBinding", typeof(string), typeof(GridViewColumn),
			                            new FrameworkPropertyMetadata(null));

		public string GetAccessorBinding {
			get { return (string)GetValue(GetAccessorBindingProperty); }
			set { SetValue(GetAccessorBindingProperty, value); }
		}

		#endregion
	}
}
