using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TokeiLibrary.WPF.Styles.ListView;

namespace TokeiLibrary.WPF.Styles {
	public class TkProgressBar : UserControl {
		#region Delegates

		public delegate void ProgressBarDisplayDelegate(float value);

		#endregion

		#region ProgressStatus enum

		public enum ProgressStatus {
			Finished,
			ErrorsDetected,
			FileLoaded,
			Cancelling
		}

		#endregion

		internal bool EventsEnabled {
			get { return _eventsEnabled; }
			set { _eventsEnabled = value; }
		}

		public float Progress {
			get { return _progress; }
			set {
				this.BeginDispatch(() => this.SetValue(ProgressProperty, value));
			}
		}

		public float RefreshUpdate { get; set; }

		public Image BackImage = new Image { IsHitTestVisible = false };
		public Image FrontImage = new Image { IsHitTestVisible = false };

		public ProgressBar __ProgressBar {
			get { return _bar; }
		}
		
		public BitmapSource BackBitmapSource {
			get { return (BitmapSource)GetValue(BackBitmapSourceProperty); }
			set { SetValue(BackBitmapSourceProperty, value); }
		}
		public static DependencyProperty BackBitmapSourceProperty = DependencyProperty.Register("BackBitmapSource", typeof(BitmapSource), typeof(TkProgressBar), new PropertyMetadata(new PropertyChangedCallback(OnBackBitmapSourceChanged)));

		private static void OnBackBitmapSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				var source = e.NewValue as BitmapSource;
				bar.BackImage.Source = source;
				bar.BackImage.Width = source.PixelWidth;
				bar.BackImage.Height = source.PixelHeight;
				bar._bar.Visibility = Visibility.Collapsed;
				OnProgressChanged(d, new DependencyPropertyChangedEventArgs(ProgressProperty, bar.Progress, bar.Progress));
			}
		}
		
		public BitmapSource FrontBitmapSource {
			get { return (BitmapSource)GetValue(FrontBitmapSourceProperty); }
			set { SetValue(FrontBitmapSourceProperty, value); }
		}
		public static DependencyProperty FrontBitmapSourceProperty = DependencyProperty.Register("FrontBitmapSource", typeof(BitmapSource), typeof(TkProgressBar), new PropertyMetadata(new PropertyChangedCallback(OnFrontBitmapSourceChanged)));

		private static void OnFrontBitmapSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				var source = e.NewValue as BitmapSource;
				bar.FrontImage.Source = source;
				bar.FrontImage.Width = source.PixelWidth;
				bar.FrontImage.Height = source.PixelHeight;
				bar.Background = Brushes.Transparent;
				bar.FrontImage.SetValue(ClipProperty, new RectangleGeometry(new Rect(0, 0, 0, bar.FrontImage.Height)));
				bar._bar.Visibility = Visibility.Collapsed;
				OnProgressChanged(d, new DependencyPropertyChangedEventArgs(ProgressProperty, bar.Progress, bar.Progress));
			}
		}

		private static void _showBlurEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				var value = (bool)e.NewValue;

				if (!value) {
					if (bar._rect != null)
						bar._rect.Visibility = Visibility.Hidden;
				}
			}
		}

		private static void _enableBarTextEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				var value = (bool)e.NewValue;

				if (bar._lab1 != null) {
					if (value) {
						bar._lab1.Visibility = Visibility.Visible;
						bar._lab2.Visibility = Visibility.Visible;
					}
					else {
						bar._lab1.Visibility = Visibility.Collapsed;
						bar._lab2.Visibility = Visibility.Collapsed;
					}
				}
			}
		}

		public static DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(float), typeof(TkProgressBar), new PropertyMetadata(new PropertyChangedCallback(OnProgressChanged)));
		public static readonly DependencyProperty ShowCancelButtonProperty = DependencyProperty.Register("ShowCancelButton", typeof(bool), typeof(TkProgressBar), new FrameworkPropertyMetadata(true));
		public static readonly DependencyProperty ShowBlurEffectProperty = DependencyProperty.Register("ShowBlurEffect", typeof(bool), typeof(TkProgressBar), new FrameworkPropertyMetadata(true, _showBlurEffectChanged));
		public static readonly DependencyProperty EnableBarTextProperty = DependencyProperty.Register("EnableBarText", typeof(bool), typeof(TkProgressBar), new FrameworkPropertyMetadata(true, _enableBarTextEffectChanged));
		public static readonly DependencyProperty SmoothProgressProperty = DependencyProperty.Register("SmoothProgress", typeof(bool), typeof(TkProgressBar), new FrameworkPropertyMetadata(false));
		public static readonly DependencyProperty ForegroundColorProperty = DependencyProperty.Register("ForegroundColor", typeof(Brush), typeof(TkProgressBar), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 211, 40)), _foregroundChanged));
		public static readonly DependencyProperty CancelBrushProperty = DependencyProperty.Register("CancelBrush", typeof(Brush), typeof(TkProgressBar), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)), _normalBrushChanged));
		public static readonly DependencyProperty NormalBrushProperty = DependencyProperty.Register("NormalBrush", typeof(Brush), typeof(TkProgressBar), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 211, 40))));
		public static readonly DependencyProperty ErrorBrushProperty = DependencyProperty.Register("ErrorBrush", typeof(Brush), typeof(TkProgressBar), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 83, 83, 83))));
		public static readonly DependencyProperty ClippingWidthProperty = DependencyProperty.Register("ClippingWidth", typeof(double), typeof(TkProgressBar), new FrameworkPropertyMetadata((double)0, _clipWidthChanged));
		public ProgressBarDisplayDelegate DisplayProgress;
		private Button _buttonCancel;
		private Image _buttonError;
		private string _display = "";
		private bool _isIndeterminate;
		private Label _lab1;
		private Label _lab2;
		private float _progress;
		private ProgressBar _progressBar;
		private Rectangle _rect;
		private bool _eventsEnabled = true;

		public TkProgressBar() {
			DisplayProgress = _displayProgress;
		}

		public bool EnableBarText {
			get { return (bool)GetValue(EnableBarTextProperty); }
			set { SetValue(EnableBarTextProperty, value); }
		}

		public bool ShowCancelButton {
			get { return (bool)GetValue(ShowCancelButtonProperty); }
			set { SetValue(ShowCancelButtonProperty, value); }
		}

		public bool ShowBlurEffect {
			get { return (bool)GetValue(ShowBlurEffectProperty); }
			set { SetValue(ShowBlurEffectProperty, value); }
		}

		public double ClippingWidth {
			get { return (double)GetValue(ClippingWidthProperty); }
			set { SetValue(ClippingWidthProperty, value); }
		}
		
		public bool SmoothProgress {
			get { return (bool)GetValue(SmoothProgressProperty); }
			set { SetValue(SmoothProgressProperty, value); }
		}

		public Brush ForegroundColor {
			get { return (Brush)GetValue(ForegroundColorProperty); }
			set { SetValue(ForegroundColorProperty, value); }
		}
		
		public Brush CancelBrush {
			get { return (Brush)GetValue(CancelBrushProperty); }
			set { SetValue(CancelBrushProperty, value); }
		}

		public Brush NormalBrush {
			get { return (Brush)GetValue(NormalBrushProperty); }
			set { SetValue(NormalBrushProperty, value); }
		}

		public Brush ErrorBrush {
			get { return (Brush)GetValue(ErrorBrushProperty); }
			set { SetValue(ErrorBrushProperty, value); }
		}

		public bool IsIndeterminate {
			get { return _isIndeterminate; }
			set {
				if (_isIndeterminate != value) {
					_progressBar.Dispatch(p => p.IsIndeterminate = value);
					_isIndeterminate = value;
				}
			}
		}

		public string Display {
			get { return _display; }
			set {
				if (_display != value) {
					_lab1.Dispatch(p => p.Content = value);
					_lab2.Dispatch(p => p.Content = value);
					_display = value;
				}
			}
		}

		private static void _forceUpdate(TkProgressBar bar, float newValue) {
			if (bar.DisplayProgress == null)
				bar.DisplayProgress = bar._displayProgress;

			bar.DisplayProgress(newValue);
		}

		private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null && bar.EventsEnabled) {
				_forceUpdate(bar, (float) e.NewValue);
			}
		}

		public float ProgressOnly {
			get { return _progress; }
			set {
				if (value == _progress)
					return;

				_progress = value;

				if (_states.ContainsKey(value)) {
					Display = _states[value];
					_setIsIndterminate(true);
				}
				else if (_progress == -5) {
					Display = "Indexing content";
					_setIsIndterminate(true);
				}
				else if (_progress == -2) {
					Display = "Scanning encrypted content";
					_setIsIndterminate(true);
				}
				else if (_progress < 0) {
					Display = "Processing";
					_setIsIndterminate(true);
				}
				else if (_progress == 0) {
					_setIsIndterminate(false);
					_setProg(value);
				}
				else if (_progress >= 100.0f) {
					_setIsIndterminate(false);
					Display = "Finished";
					_setProg(value);
				}
				else {
					_setIsIndterminate(false);
					_setProg(value);
				}
			}
		}

		private void _setIsIndterminate(bool value) {
			this.Dispatch(delegate {
				if (FrontImage.Source != null) {
					FrontImage.Visibility = value ? Visibility.Hidden : Visibility.Visible;
				}
				else {
					IsIndeterminate = value;
				}
			});
		}

		private void _setClipWidth(double value) {
			FrontImage.SetValue(ClipProperty, new RectangleGeometry(new Rect(0, 0, value / 100f * FrontImage.Width, FrontImage.Height)));
		}

		private void _setProg(float value) {
			this.Dispatch(delegate {
				if (FrontImage.Source != null) {
					if (SmoothProgress && RefreshUpdate > 50 && value > 0 && _previousValue < value) {
						if (_story == null) {
							_story = new Storyboard();
							_animation = new DoubleAnimation();
							_story.Children.Add(_animation);
							Storyboard.SetTarget(_animation, this);
							Storyboard.SetTargetProperty(_animation, new PropertyPath(ClippingWidthProperty));
						}

						_animation.Duration = TimeSpan.FromMilliseconds(RefreshUpdate);
						_animation.From = _previousValue;
						_animation.To = value;
						_story.Begin();
					}
					else {
						if (_story != null)
							_story.Stop();

						FrontImage.SetValue(ClipProperty, new RectangleGeometry(new Rect(0, 0, value / 100f * FrontImage.Width, FrontImage.Height)));
					}

					_previousValue = value;
				}
				else {
					_progressBar.Value = value;
				}
			});
		}

		public event RoutedEventHandler Cancel;
		public event RoutedEventHandler ShowErrors;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);

			if (e.Property == ProgressProperty) {
				OnProgressChanged(this, e);
			}
			if (e.Property == NormalBrushProperty) {
				_normalBrushChanged(this, e);
			}
			if (e.Property == ForegroundColorProperty) {
				_foregroundChanged(this, e);
			}
		}

		static void _foregroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				if (bar._progressBar != null)
					bar._progressBar.Foreground = (Brush) e.NewValue;
			}
		}

		static void _normalBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				if (bar._progressBar != null)
					bar._progressBar.Foreground = (Brush)e.NewValue;

				bar.NormalBrush = (Brush)e.NewValue;
			}
		}

		static void _clipWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TkProgressBar bar = d as TkProgressBar;

			if (bar != null) {
				bar._setClipWidth((double)e.NewValue);
			}
		}

		private void _displayProgress(float value) {
			if (value == _progress)
				return;

			_progress = value;

			this.Dispatch(delegate {
				if (_progressBar == null)
					return;

				if (_states.ContainsKey(value)) {
					Display = _states[value];
					_setIsIndterminate(true);
				}
				else if (_progress == -5) {
					Display = "Indexing content";
					_setIsIndterminate(true);
				}
				else if (_progress == -2) {
					Display = "Scanning encrypted content";
					_setIsIndterminate(true);
				}
				else if (_progress < 0) {
					Display = "Processing";
					_setIsIndterminate(true);
				}
				else if (_progress == 0) {
					_setIsIndterminate(false);
					Display = "";
					_setProg(value);
				}
				else if (_progress >= 100.0f) {
					_setIsIndterminate(false);
					Display = "Finished";
					SetSpecialState(ProgressStatus.Finished);
					_setProg(value);
				}
				else {
					_setIsIndterminate(false);
					Display = String.Format("{0:0.00} %", _progress);
					_setProg(value);
				}
			});
		}

		public void SetSpecialState(ProgressStatus state) {
			this.Dispatch(p => p._buttonError.Visibility = Visibility.Collapsed);

			try {
				EventsEnabled = false;

				_setProg(100f);
				Progress = 100f;
				_progressBar.Dispatch(p => p.Value = 100f);

				switch (state) {
					case ProgressStatus.Finished:
						_setIsIndterminate(false);
						this.Dispatch(p => p.ForegroundColor = NormalBrush);
						Display = "Finished";
						break;
					case ProgressStatus.ErrorsDetected:
						_setIsIndterminate(false);
						Display = "Errors were detected";
						this.Dispatch(p => p._buttonError.Visibility = Visibility.Visible);
						this.Dispatch(p => p.ForegroundColor = ErrorBrush);
						break;
					case ProgressStatus.FileLoaded:
						_setIsIndterminate(false);
						Display = "File loaded successfully";
						this.Dispatch(p => p.ForegroundColor = NormalBrush);
						break;
					case ProgressStatus.Cancelling:
						_setIsIndterminate(true);
						Display = "Cancelling...";
						this.Dispatch(p => p.ForegroundColor = CancelBrush);
						break;
				}
			}
			finally {
				EventsEnabled = true;
			}
		}

		public void SetInternal(float value) {
			_progress = value;
			_progressBar.Dispatch(p => p.Value = value);
		}

		private readonly Dictionary<float, string> _states = new Dictionary<float, string>();
		private readonly Dictionary<string, float> _statesValue = new Dictionary<string, float>();
		private ProgressBar _bar;
		private Storyboard _story;
		private float _previousValue;
		private DoubleAnimation _animation;

		public void SetIntermediateState(string value) {
			if (_statesValue.ContainsKey(value)) {
				Progress = _statesValue[value];
			}
			else {
				var min = _statesValue.Values.Count == 0 ? -100 : _statesValue.Values.Min() - 1;
				_statesValue[value] = min - 1;
				_states[_statesValue[value]] = value;
				Progress = _statesValue[value];
			}
		}

		public float GetIntermediateState(string value) {
			if (_statesValue.ContainsKey(value)) {
				return _statesValue[value];
			}
			else {
				var min = _statesValue.Values.Count == 0 ? -100 : _statesValue.Values.Min() - 1;
				_statesValue[value] = min - 1;
				_states[_statesValue[value]] = value;
				return _statesValue[value];
			}
		}

		protected override void OnInitialized(EventArgs e) {
			Grid mainGrid = new Grid();
			mainGrid.ColumnDefinitions.Add(new ColumnDefinition());

			if (ShowCancelButton)
				mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20)});

			_rect = null;

			if (ShowBlurEffect) {
				_rect = new Rectangle();
				_rect.Fill = new SolidColorBrush(Colors.White);
				_rect.Opacity = 1;
				_rect.Height = 25f;
				_rect.Margin = new Thickness(1, 0, 1, 0);
				_rect.Effect = new BlurEffect();
				_rect.SetValue(Grid.ColumnSpanProperty, 2);
			}

			_bar = new ProgressBar();
			_bar.Opacity = 1;
			_bar.Height = 24f;

			if (ShowBlurEffect)
				_bar.Margin = new Thickness(2, 0, 0, 0);

			_bar.Foreground = NormalBrush;

			Button button = null;

			var frame = ApplicationManager.PreloadResourceImage("warning16.png");
			Image image = new Image { Source = frame, Stretch = Stretch.None, Width = 20, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 8, 0) };
			_buttonError = image;
			_buttonError.SetValue(Grid.ColumnProperty, 0);
			_buttonError.Visibility = Visibility.Collapsed;
			_buttonError.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(_buttonError_MouseLeftButtonUp);
			WpfUtils.AddMouseInOutEffects(image);

			if (ShowCancelButton) {
				Canvas canva = new Canvas();
				Line line1 = new Line {X1 = -4, X2 = 4, Y1 = -4, Y2 = 4, StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 73, 77, 79)), StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round};
				Line line2 = new Line {X1 = 4, X2 = -4, Y1 = -4, Y2 = 4, StrokeThickness = 2, Stroke = new SolidColorBrush(Color.FromArgb(255, 73, 77, 79)), StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round};
				canva.Children.Add(line1);
				canva.Children.Add(line2);

				button = new Button();
				button.Height = 24f;
				button.Margin = new Thickness(-1, 0, 0, 0);

				try {
					try {
						if (Environment.OSVersion.Version.Major < 6) {
							button.Style = (Style)Application.Current.TryFindResource("ButtonStyledXP");
						}
						else {
							button.Style = (Style)Application.Current.TryFindResource("ButtonStyled");
						}
					}
					catch {
						button.Style = (Style)Application.Current.TryFindResource("ButtonProgressBarStyled");
					}
				}
				catch {
					// ??
				}

				button.SetValue(Grid.ColumnProperty, 2);
				button.Content = canva;
				_buttonCancel = button;
				_buttonCancel.Click += new RoutedEventHandler(_buttonCancel_Click);
			}

			Label lab1 = new Label();
			lab1.Foreground = new SolidColorBrush(Colors.Black);
			lab1.FontSize = 13;
			lab1.FontWeight = FontWeights.Bold;
			lab1.HorizontalAlignment = HorizontalAlignment.Center;
			lab1.VerticalAlignment = VerticalAlignment.Center;
			lab1.Effect = new BlurEffect();

			Label lab2 = new Label();
			lab2.Foreground = new SolidColorBrush(Colors.White);
			lab2.FontSize = 13;
			lab2.FontWeight = FontWeights.Bold;
			lab2.HorizontalAlignment = HorizontalAlignment.Center;
			lab2.VerticalAlignment = VerticalAlignment.Center;

			if (!EnableBarText) {
				lab1.Visibility = System.Windows.Visibility.Collapsed;
				lab2.Visibility = System.Windows.Visibility.Collapsed;
			}

			if (ShowBlurEffect) mainGrid.Children.Add(_rect);
			mainGrid.Children.Add(_bar);
			if (ShowCancelButton) mainGrid.Children.Add(button);
			mainGrid.Children.Add(BackImage);
			mainGrid.Children.Add(FrontImage);
			mainGrid.Children.Add(lab1);
			mainGrid.Children.Add(lab2);
			mainGrid.Children.Add(image);
			Content = mainGrid;

			if (FrontImage.Source != null || BackImage.Source != null) {
				_bar.Visibility = Visibility.Collapsed;
			}

			_progressBar = _bar;
			_lab1 = lab1;
			_lab2 = lab2;

			Progress = 0;
			base.OnInitialized(e);
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			try {
				if (Cancel != null)
					Cancel(sender, e);
			}
			catch { }
		}

		private void _buttonError_MouseLeftButtonUp(object sender, RoutedEventArgs e) {
			try {
				if (ShowErrors != null)
					ShowErrors(sender, e);
			}
			catch { }
		}
	}
}
