using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TokeiLibrary.WPF.Styles {
	/// <summary>
	/// Interaction logic for FancyButton.xaml
	/// </summary>
	public partial class FancyButton : UserControl {
		public FancyButton() {
			InitializeComponent();

			Loaded += _fancyButton_Loaded;
		}
		
		public string TextHeader {
			get { return (string)GetValue(TextHeaderProperty); }
			set { SetValue(TextHeaderProperty, value); }
		}
		public static DependencyProperty TextHeaderProperty = DependencyProperty.Register("TextHeader", typeof(string), typeof(FancyButton), new PropertyMetadata(new PropertyChangedCallback(OnTextHeaderChanged)));

		private static void OnTextHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				var value = e.NewValue.ToString();
				fb._tbIdentifier.Text = value;
				fb._tbIdentifier.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		
		public bool IsPressed {
			get { return (bool)GetValue(IsPressedProperty); }
			set { SetValue(IsPressedProperty, value); }
		}
		public static DependencyProperty IsPressedProperty = DependencyProperty.Register("IsPressed", typeof(bool), typeof(FancyButton), new PropertyMetadata(new PropertyChangedCallback(OnIsPressedChanged)));

		private static void OnIsPressedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				var value = Boolean.Parse(e.NewValue.ToString());
				fb._borderOverlayPressed.Visibility = value ? Visibility.Visible : Visibility.Hidden;
			}
		}

		public ComboBox Elements {
			get { return (ComboBox)GetValue(ElementsProperty); }
			set { SetValue(ElementsProperty, value); }
		}
		public static DependencyProperty ElementsProperty = DependencyProperty.Register("Elements", typeof(ComboBox), typeof(FancyButton), new PropertyMetadata(new PropertyChangedCallback(OnElementsChanged)));

		private static void OnElementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				ComboBox box = (ComboBox) e.NewValue;

				box.SetValue(Grid.RowProperty, 1);
				//box.Visibility = Visibility.Hidden;
				//box.IsEnabled = false;
				box.Height = 0;
				box.Width = 0;
				box.HorizontalAlignment = HorizontalAlignment.Left;
				box.VerticalAlignment = VerticalAlignment.Bottom;
				box.Margin = new Thickness(0);

				fb._primaryGrid.Children.Add(box);

				box.DropDownOpened += delegate { fb.IsPressed = true; };

				box.DropDownClosed += delegate {
					fb.IsPressed = false;
					//Keyboard.Focus(actEditor._gridPrimary);
				};

				//box.SelectionChanged += delegate {
				//	if (box.SelectedItem != null) {
				//		foreach (object item in box.Items) {
				//			((UIElement) item).SetValue(FontWeightProperty, FontWeights.Normal);
				//		}
				//
				//		((UIElement) box.SelectedItem).SetValue(FontWeightProperty, FontWeights.Bold);
				//	}
				//};

				fb.Click += delegate {
					box.IsDropDownOpen = true;
				};

				//box.SelectionChanged += new SelectionChangedEventHandler(box_SelectionChanged);
				//box_SelectionChanged(null, null);
			}
		}
		
		public bool IsButtonEnabled {
			get { return (bool)GetValue(IsButtonEnabledProperty); }
			set { SetValue(IsButtonEnabledProperty, value); }
		}
		public static DependencyProperty IsButtonEnabledProperty = DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(FancyButton), new PropertyMetadata(true, new PropertyChangedCallback(OnIsButtonEnabledChanged)));

		private static void OnIsButtonEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				var value = Boolean.Parse(e.NewValue.ToString());
				fb.IsEnabled = value;
				fb._borderOverlayEnabled.Visibility = value ? Visibility.Hidden : Visibility.Visible;
			}
		}

		
		public string TextDescription {
			get { return (string)GetValue(TextDescriptionProperty); }
			set { SetValue(TextDescriptionProperty, value); }
		}
		public static DependencyProperty TextDescriptionProperty = DependencyProperty.Register("TextDescription", typeof(string), typeof(FancyButton), new PropertyMetadata(new PropertyChangedCallback(OnTextDescriptionChanged)));

		private static void OnTextDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				var value = e.NewValue.ToString();
				fb._tbDescription.Text = value;
				fb._tbDescription.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		
		public string TextSubDescription {
			get { return (string)GetValue(TextSubDescriptionProperty); }
			set { SetValue(TextSubDescriptionProperty, value); }
		}
		public static DependencyProperty TextSubDescriptionProperty = DependencyProperty.Register("TextSubDescription", typeof(string), typeof(FancyButton), new PropertyMetadata(new PropertyChangedCallback(OnTextSubDescriptionChanged)));

		private static void OnTextSubDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				var value = e.NewValue.ToString();
				fb._tbSubDescription.Text = value;
				fb._tbSubDescription.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		
		public string ImagePath {
			get { return (string)GetValue(ImagePathProperty); }
			set { SetValue(ImagePathProperty, value); }
		}
		public static DependencyProperty ImagePathProperty = DependencyProperty.Register("ImagePath", typeof(string), typeof(FancyButton), new PropertyMetadata(new PropertyChangedCallback(OnImagePathChanged)));

		private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as FancyButton;

			if (fb != null) {
				var img = ApplicationManager.PreloadResourceImage(e.NewValue.ToString());
				fb.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
				fb._imageIcon.Source = img;
			}
		}

		public Image ImageIcon {
			get { return _imageIcon; }
			set { _imageIcon = value; }
		}

		public event RoutedEventHandler Click;

		public void OnClick(RoutedEventArgs e) {
			RoutedEventHandler handler = Click;
			if (handler != null) handler(this, e);
		}

		private void _fancyButton_Loaded(object sender, RoutedEventArgs e) {
			//_tbIdentifier.Text = TextHeader;
			//_tbDescription.Text = TextDescription;
			//_tbSubDescription.Text = TextSubDescription;
			//
			//_tbIdentifier.Visibility = _tbIdentifier.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			//_tbDescription.Visibility = _tbDescription.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			//_tbSubDescription.Visibility = _tbSubDescription.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

			//if (!String.IsNullOrEmpty(_imagePath)) {
			//    try {
			//        _imageIcon.Source = ApplicationManager.PreloadResourceImage(_imagePath);
			//    }
			//    catch { }
			//}

			_borderOverlayEnabled.Visibility = IsEnabled ? Visibility.Hidden : Visibility.Visible;
		}

		public bool ShowMouseOver {
			set {
				_borderOverlay.Visibility = value ? Visibility.Visible : Visibility.Hidden;
			}
		}

		private void _border_MouseEnter(object sender, MouseEventArgs e) {
			_borderOverlay.Visibility = Visibility.Visible;
		}

		private void _border_MouseLeave(object sender, MouseEventArgs e) {
			_borderOverlay.Visibility = Visibility.Hidden;
		}

		private void _border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (IsEnabled) {
				if (_border.IsMouseCaptured) {
					_border.ReleaseMouseCapture();
					FancyButton button = this.GetObjectAtPoint<FancyButton>(e.GetPosition(this)) as FancyButton;

					if (button == this)
						OnClick(e);
				}
			}

			_border.ReleaseMouseCapture();
		}

		private void _border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			_border.CaptureMouse();
		}
	}
}
