using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary;
using Utilities;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for AdvancedTextBox.xaml
	/// </summary>
	public partial class AdvancedTextBox : UserControl {
		private Color _color;
		private Color _colorEnter;
		private Color _colorHover;

		private SolidColorBrush _brushColor;
		private SolidColorBrush _brushEnter;
		private SolidColorBrush _brushHover;

		private bool _hasMoved;
		private bool _isMouseDown;
		private POINT _clickedPoint;
		private POINT _clickedRealPoint;
		private Action<string> _preview;
		public bool IsEditing { get; set; }
		public bool HasEdited { get; set; }

		public Thickness OuterBorderThickness {
			get { return (Thickness)GetValue(OuterBorderThicknessProperty); }
			set { SetValue(OuterBorderThicknessProperty, value); }
		}

		public static readonly DependencyProperty OuterBorderThicknessProperty =
			DependencyProperty.RegisterAttached("OuterBorderThickness",
				typeof(Thickness),
				typeof(AdvancedTextBox),
				new PropertyMetadata(new Thickness(), OuterBorderThicknessChanged));

		public static void OuterBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var advTb = d as AdvancedTextBox;

			if (advTb != null) {
				advTb._outerBorder.BorderThickness = (Thickness)e.NewValue;
			}
		}

		public CornerRadius OuterCornerRadius {
			get { return (CornerRadius)GetValue(OuterCornerRadiusProperty); }
			set { SetValue(OuterCornerRadiusProperty, value); }
		}

		public static readonly DependencyProperty OuterCornerRadiusProperty =
			DependencyProperty.RegisterAttached("OuterCornerRadius",
				typeof(CornerRadius),
				typeof(AdvancedTextBox),
				new PropertyMetadata(new CornerRadius(), OuterCornerRadiusChanged));

		public static void OuterCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var advTb = d as AdvancedTextBox;

			if (advTb != null) {
				advTb._outerBorder.CornerRadius = (CornerRadius)e.NewValue;
			}
		}

		public delegate void ValueChangedEventHandler(AdvancedTextBox sender, float deltaX, float deltaY, bool addCommand);
		public event ValueChangedEventHandler MouseValueChanged;

		protected virtual void OnMouseValueChanged(float deltax, float deltay, bool addCommand) {
			ValueChangedEventHandler handler = MouseValueChanged;
			if (handler != null) handler(this, deltax, deltay, addCommand);
		}

		public delegate void AdvancedTextChangedEventHandler(AdvancedTextBox sender, TextChangedEventArgs e, bool commands);
		public event AdvancedTextChangedEventHandler TextChanged;

		protected virtual void OnTextChanged(TextChangedEventArgs e, bool commands) {
			if (_disableEvents)
				return;

			if (_preview != null)
				_preview(Text);

			HasEdited = true;
			AdvancedTextChangedEventHandler handler = TextChanged;
			if (handler != null) handler(this, e, commands);
		}

		public string Text {
			get { return _tb.Text; }
			set { _tb.Text = value; }
		}

		public string TextNoEvent {
			set {
				try {
					_disableEvents = true;
					_tb.Text = value;
					_preview(Text);
				}
				finally {
					_disableEvents = false;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct POINT {
			public Int32 X;
			public Int32 Y;
		}

		[DllImport("User32.dll")]
		private static extern bool SetCursorPos(int X, int Y);

		[DllImport("User32.dll")]
		private static extern bool GetCursorPos(out POINT point);

		public bool AddCommand { get; set; }

		public AdvancedTextBox() {
			InitializeComponent();

			AddCommand = true;

			_tb.TextChanged += (s, e) => {
				OnTextChanged(e, false);
			};
			_tb.KeyDown += new KeyEventHandler(_tb_KeyDown);

			var color = (Color)Application.Current.Resources["UIThemeTextBoxBackgroundColor"];

			SetColor(color, Color.FromArgb(255, 88, 129, 195), Color.FromArgb(255, 255, 255, 255));

			Loaded += delegate {
				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				var window = WpfUtilities.FindDirectParentControl<Window>(this);

				window.PreviewMouseDown += (s, e) => {
					if (_tb.Opacity >= 1) {
						var position = e.GetPosition(this);

						if (position.X < 0 || position.X > ActualWidth || position.Y < 0 || position.Y > ActualHeight) {
							_endEdit();
						}
					}
				};
			};

			_tb.GotFocus += (s, e) => {
				_beginEdit();
			};

			_gridPrevious.MouseEnter += delegate {
				_enterComponent();
				_gridPrevious.Background = _brushEnter;
			};

			_gridPreview.MouseEnter += delegate {
				_isMouseDown = false;
				_hasMoved = false;
				Cursor = Cursors.SizeWE;
				_enterComponent();
				_gridPreview.Background = _brushEnter;
			};

			_gridPreview.MouseMove += _gridPreviewMove;

			_gridPreview.MouseLeftButtonDown += (s, e) => {
				_isMouseDown = true;
				_hasMoved = false;
				Cursor = Cursors.None;
				GetCursorPos(out _clickedPoint);
				_clickedRealPoint = _clickedPoint;
				((Grid)s).CaptureMouse();
			};

			_gridPreview.MouseLeftButtonUp += (s, e) => {
				if (_tb.Opacity >= 1)
					return;

				if (_tb.IsMouseCaptured)
					return;

				if (!_isMouseDown)
					return;
				
				UIElement element = (UIElement)s;
				element.ReleaseMouseCapture();
				Cursor = Cursors.SizeWE;
				SetCursorPos((int)_clickedRealPoint.X, (int)_clickedRealPoint.Y);

				if (!_hasMoved) {
					_beginEdit();
				}
				else {
					OnTextChanged(null, true);
				}
			};

			_tb.LostFocus += delegate {
				_endEdit();
			};

			_gridNext.MouseEnter += delegate {
				_enterComponent();
				_gridNext.Background = _brushEnter;
			};

			_gridPrevious.MouseLeftButtonDown += (s, e) => OnMouseValueChanged(-1f, 0, true);
			_gridNext.MouseLeftButtonDown += (s, e) => OnMouseValueChanged(1f, 0, true);

			_gridPrevious.MouseLeave += _leaveComponent;
			_gridPreview.MouseLeave += _leaveComponent;
			_gridNext.MouseLeave += _leaveComponent;

			_tb.PreviewKeyDown += (s, e) => {
				if (_tb.Opacity <= 0) {
					e.Handled = true;
					return;
				}
			};

			IsEnabledChanged += delegate {
				if (IsEnabled) {
					Opacity = 1;
				}
				else {
					Opacity = 0.5;
				}
			};

			_updateModeView(true);
		}

		private void _updateModeView(bool? current = null) {
			if (current != null)
				return;

			_tb.Opacity = 0;
			_tb.IsHitTestVisible = false;
			_outerBorder.Background = _brushColor;

			var color = (Color)Application.Current.Resources["AdvancedTextBoxBorderColor"];
			SetColor(color, Color.FromArgb(255, 88, 129, 195), Color.FromArgb(255, 255, 255, 255));
		}

		private void _beginEdit() {
			try {
				_tb.Opacity = 1;
				_tb.IsHitTestVisible = true;
				_outerBorder.Background = _tb.Background;
				Keyboard.Focus(_tb);
				_tb.SelectAll();
			}
			finally {
				HasEdited = false;
				IsEditing = true;
			}
		}

		private void _endEdit() {
			try {
				_tb.Opacity = 0;
				_tb.IsHitTestVisible = false;
				_outerBorder.Background = _brushColor;

				if (HasEdited)
					OnTextChanged(null, true);
			}
			finally {
				HasEdited = false;
				IsEditing = false;
			}
		}

		private void _tb_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter || e.Key == Key.Escape) {
				e.Handled = true;
				_endEdit();

				// Bring back the focus to its parent, otherwise shortcuts will stop working if we stay in textbox
				Keyboard.Focus(WpfUtilities.FindDirectParentControl<ScrollViewer>(this));
			}
		}

		public void Init(Action<string> preview) {
			_preview = preview;
		}

		private bool _enableMoveEvents = true;
		private bool _disableEvents;

		private void _gridPreviewMove(object sender, MouseEventArgs e) {
			UIElement element = (UIElement)sender;

			if (!element.IsMouseCaptured || !_enableMoveEvents)
				return;

			POINT current;

			GetCursorPos(out current);

			var deltaX = current.X - _clickedPoint.X;
			var deltaY = current.Y - _clickedPoint.Y;

			if (deltaX < 0 || deltaX > 0) {
				_hasMoved = true;
			}
			else {
				return;
			}

			if (current.X < _clickedPoint.X ||
				current.X > _clickedPoint.X) {
				_enableMoveEvents = false;
				SetCursorPos(_clickedRealPoint.X, _clickedRealPoint.Y);
				_enableMoveEvents = true;
			}

			OnMouseValueChanged((float)deltaX, (float)deltaY, false);
		}

		private void _enterComponent() {
			_pathPrevious.Visibility = Visibility.Visible;
			_pathNext.Visibility = Visibility.Visible;
			_gridPrevious.Background = _brushHover;
			_gridPreview.Background = _brushHover;
			_gridNext.Background = _brushHover;
		}

		public void _leaveComponent(object sender, MouseEventArgs e) {
			Cursor = null;
			_pathPrevious.Visibility = Visibility.Hidden;
			_pathNext.Visibility = Visibility.Hidden;
			_gridPrevious.Background = _brushColor;
			_gridPreview.Background = _brushColor;
			_gridNext.Background = _brushColor;
		}

		public void SetColor(Color color, Color boxBackground, Color boxForeground) {
			_color = color;
			_colorEnter = Color.FromArgb(color.A, _clamp(color.R + 40), _clamp(color.G + 40), _clamp(color.B + 40));
			_brushEnter = new SolidColorBrush(_colorEnter);

			_colorHover = Color.FromArgb(color.A, _clamp(color.R + 20), _clamp(color.G + 20), _clamp(color.B + 20));
			_brushHover = new SolidColorBrush(_colorHover);

			_brushColor = new SolidColorBrush(color);
			_outerBorder.Background = _brushColor;

			_tb.Background = new SolidColorBrush(boxBackground);
			_tb.Foreground = new SolidColorBrush(boxForeground);
			_tb.SelectionBrush = new SolidColorBrush(Color.FromArgb(255, 197, 219, 255));
		}

		private byte _clamp(int v) {
			if (v > 255)
				return 255;
			if (v < 0)
				return 0;
			return (byte)v;
		}

		public float GetRealFloat() {
			return FormatConverters.SingleConverterNoThrow(Text);
		}

		public float GetFloat() {
			if (InvertValue)
				return -FormatConverters.SingleConverterNoThrow(Text);

			return FormatConverters.SingleConverterNoThrow(Text);
		}

		public bool InvertValue { get; set; }

		public int GetInt() {
			int value;

			if (Int32.TryParse(Text, out value))
				return value;

			return 0;
		}
	}
}
