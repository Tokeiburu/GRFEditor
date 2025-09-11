using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utilities;

namespace GRFEditor.Tools.Map {
	/// <summary>
	/// Interaction logic for CellEditControl.xaml
	/// </summary>
	public partial class CellEditControl : UserControl {
		public string HeaderText {
			get { return (string)GetValue(HeaderTextProperty); }
			set { SetValue(HeaderTextProperty, value); }
		}

		public static DependencyProperty HeaderTextProperty = DependencyProperty.Register("HeaderText", typeof(string), typeof(CellEditControl), new PropertyMetadata("", new PropertyChangedCallback(OnHeaderTextChanged)));

		private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as CellEditControl;

			if (fb != null) {
				var value = e.NewValue.ToString();
				fb._tbHeader.Text = value;
			}
		}

		public object ToolTipText {
			get { return (object)GetValue(ToolTipTextProperty); }
			set { SetValue(ToolTipTextProperty, value); }
		}

		//public static DependencyProperty ToolTipTextProperty = DependencyProperty.Register("ToolTipText", typeof(object), typeof(CellEditControl), new PropertyMetadata("", new PropertyChangedCallback(OnToolTipTextChanged)));
		//public static readonly DependencyProperty ToolTipTextProperty = ToolTipService.ToolTipProperty.AddOwner(FrameworkElement._typeofThis);
		public static readonly DependencyProperty ToolTipTextProperty = DependencyProperty.RegisterAttached("ToolTipText", typeof(object), typeof(CellEditControl), new PropertyMetadata("", new PropertyChangedCallback(OnToolTipTextChanged)));

		private static void OnToolTipTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as CellEditControl;

			if (fb != null) {
				var value = e.NewValue as TextBlock;

				fb._tbHeader.MouseEnter += delegate {
					Mouse.OverrideCursor = Cursors.Hand;
					fb._tbHeader.Foreground = Application.Current.Resources["MouseOverTextBrush"] as SolidColorBrush;
					fb._tbHeader.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
				};

				ToolTip tooltip = new ToolTip { Content = value };
				fb._tbHeader.ToolTip = tooltip;
				ToolTipService.SetBetweenShowDelay(fb._tbHeader, 30000);

				fb._tbHeader.MouseLeave += delegate {
					Mouse.OverrideCursor = null;
					fb._tbHeader.Foreground = Application.Current.Resources["FancyButtonHeaderForeground"] as SolidColorBrush;
					fb._tbHeader.SetValue(TextBlock.TextDecorationsProperty, null);
					tooltip.IsOpen = false;
				};

				fb._tbHeader.MouseLeftButtonUp += delegate {
					tooltip.IsOpen = true;
				};
			}
		}

		public CellEditControl() {
			InitializeComponent();
		}
	}
}
