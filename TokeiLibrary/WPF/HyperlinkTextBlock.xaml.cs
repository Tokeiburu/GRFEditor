using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for LinkControl.xaml
	/// </summary>
	public partial class HyperlinkTextBlock : UserControl {
		#region Delegates

		public delegate void LinkControlDelegate(object sender);

		#endregion

		public static DependencyProperty NormalBrushProperty = DependencyProperty.Register("NormalBrush", typeof (Brush), typeof (HyperlinkTextBlock), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 0, 254)), new PropertyChangedCallback(OnNormalBrushChanged)));
		public static DependencyProperty MouseOverBrushProperty = DependencyProperty.Register("MouseOverBrush", typeof (Brush), typeof (HyperlinkTextBlock), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 91, 255)), new PropertyChangedCallback(OnMouseOverBrushChanged)));
		public static DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof (string), typeof (HyperlinkTextBlock), new PropertyMetadata(new PropertyChangedCallback(OnHeaderChanged)));

		public HyperlinkTextBlock() {
			InitializeComponent();

			_label.Foreground = NormalBrush;
			MouseLeftButtonUp += (e, a) => OnClick();
		}

		public Brush NormalBrush {
			get { return (Brush) GetValue(NormalBrushProperty); }
			set { SetValue(NormalBrushProperty, value); }
		}

		public Brush MouseOverBrush {
			get { return (Brush) GetValue(MouseOverBrushProperty); }
			set { SetValue(MouseOverBrushProperty, value); }
		}

		public string Header {
			get { return (string) GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}

		public event LinkControlDelegate Click;

		public void OnClick() {
			LinkControlDelegate handler = Click;
			if (handler != null) handler(this);
		}

		private static void OnNormalBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			HyperlinkTextBlock link = d as HyperlinkTextBlock;

			if (link != null) {
				link._label.Foreground = (Brush) e.NewValue;
			}
		}

		private static void OnMouseOverBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
		}

		private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			HyperlinkTextBlock link = d as HyperlinkTextBlock;

			if (link != null) {
				link._label.Text = e.NewValue.ToString();
			}
		}

		protected override void OnMouseEnter(MouseEventArgs e) {
			Mouse.OverrideCursor = Cursors.Hand;
			_label.Foreground = MouseOverBrush;
			_label.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(MouseEventArgs e) {
			Mouse.OverrideCursor = null;
			_label.Foreground = NormalBrush;
			_label.SetValue(TextBlock.TextDecorationsProperty, null);
			base.OnMouseLeave(e);
		}
	}
}