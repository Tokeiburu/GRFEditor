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

		private bool _hasMoved = false;
		private POINT _clickedPoint;
		private POINT _clickedRealPoint;
		private Action<string> _preview;

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

			_tb.TextChanged += (s, e) => OnTextChanged(e, AddCommand);
			_tb.KeyDown += new KeyEventHandler(_tb_KeyDown);

			var color = (Color)Application.Current.Resources["UIThemeTextBoxBackgroundColor"];

			SetColor(color, Color.FromArgb(255, 88, 129, 195), Color.FromArgb(255, 255, 255, 255));

			this.Loaded += delegate {
				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				var window = WpfUtilities.FindDirectParentControl<Window>(this);

				window.PreviewMouseDown += (s, e) => {
					if (_tb.Opacity >= 1) {
						var position = e.GetPosition(this);

						if (position.X < 0 || position.X > this.ActualWidth || position.Y < 0 || position.Y > this.ActualHeight) {
							_tb.Opacity = 0;
							_tb.IsHitTestVisible = false;
							_outerBorder.Background = _brushColor;
						}
					}
				};
			};

			_tb.GotFocus += (s, e) => {
				_focusTb();
			};

			_gridPrevious.MouseEnter += delegate {
				_enterComponent();
				_gridPrevious.Background = _brushEnter;
			};

			_gridPreview.MouseEnter += delegate {
				_hasMoved = false;
				this.Cursor = Cursors.SizeWE;
				_enterComponent();
				_gridPreview.Background = _brushEnter;
			};

			_gridPreview.MouseMove += _gridPreviewMove;

			_gridPreview.MouseLeftButtonDown += (s, e) => {
				_hasMoved = false;
				this.Cursor = Cursors.None;
				GetCursorPos(out _clickedPoint);
				_clickedRealPoint = _clickedPoint;
				((Grid)s).CaptureMouse();
			};

			_gridPreview.MouseLeftButtonUp += (s, e) => {
				//if (_tb.Visibility == Visibility.Visible)
				if (_tb.Opacity >= 1)
					return;

				if (_tb.IsMouseCaptured)
					return;
				
				UIElement element = (UIElement)s;
				element.ReleaseMouseCapture();
				this.Cursor = Cursors.SizeWE;
				SetCursorPos((int)_clickedRealPoint.X, (int)_clickedRealPoint.Y);

				if (!_hasMoved) {
					_focusTb();
				}
				else {
					OnTextChanged(null, true);
				}
			};

			_tb.LostFocus += delegate {
				_tb.Opacity = 0;
				_tb.IsHitTestVisible = false;
				//_tb.Visibility = Visibility.Collapsed;
				_outerBorder.Background = _brushColor;
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
				//if (_tb.Visibility == Visibility.Collapsed) {
					e.Handled = true;
					return;
				}
			};
		}

		private void _focusTb() {
			//_tb.Visibility = Visibility.Visible;
			_tb.Opacity = 1;
			_tb.IsHitTestVisible = true;
			_outerBorder.Background = _tb.Background;
			Keyboard.Focus(_tb);
			_tb.SelectAll();
		}

		private void _tb_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter || e.Key == Key.Escape) {
				//_tb.Visibility = Visibility.Collapsed;
				_tb.Opacity = 0;
				_tb.IsHitTestVisible = false;
				_outerBorder.Background = _brushColor;
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

		private void _leaveComponent(object sender, MouseEventArgs e) {
			this.Cursor = null;
			_pathPrevious.Visibility = Visibility.Hidden;
			_pathNext.Visibility = Visibility.Hidden;
			_gridPrevious.Background = _brushColor;
			_gridPreview.Background = _brushColor;
			_gridNext.Background = _brushColor;
		}

		public void SetColor(Color color, Color boxBackground, Color boxForeground) {
			int diff1 = 40;
			int diff2 = 20;

			if (color.R == 255 && color.G == 255 && color.B == 255) {
				diff2 = -20;
			}

			_color = color;
			_colorEnter = Color.FromArgb(color.A, _clamp(color.R + diff1), _clamp(color.G + diff1), _clamp(color.B + diff1));
			_brushEnter = new SolidColorBrush(_colorEnter);

			_colorHover = Color.FromArgb(color.A, _clamp(color.R + diff2), _clamp(color.G + diff2), _clamp(color.B + diff2));
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

		public float GetFloat() {
			return FormatConverters.SingleConverterNoThrow(Text);
		}

		public int GetInt() {
			int value;

			if (Int32.TryParse(Text, out value))
				return value;

			return 0;
		}
	}
}
