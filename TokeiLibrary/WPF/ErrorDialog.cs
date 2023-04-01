using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Services;

namespace TokeiLibrary.WPF {
	public class ErrorDialog : TkWindow {
		private readonly ErrorLevel _level;
		private readonly string _message;
		private Image _imageError;
		private Image _imageInformation;
		private Image _imageUnspecified;
		private Image _imageWarning;
		private TextBlock _textBoxError;

		public ErrorDialog() {
		}

		public ErrorDialog(string title, string message, ErrorLevel level) : base(title, "document.ico") {
			_message = message;
			_level = level;
		}

		public ErrorLevel ErrorLevel {
			get { return _level; }
		}

		private void _initialize() {
			_textBoxError.Text = _message;

			switch (_level) {
				case ErrorLevel.Critical:
					_imageError.Visibility = Visibility.Visible;
					break;
				case ErrorLevel.Warning:
					_imageWarning.Visibility = Visibility.Visible;
					break;
				case ErrorLevel.Low:
					_imageInformation.Visibility = Visibility.Visible;
					break;
				case ErrorLevel.NotSpecified:
					_imageUnspecified.Visibility = Visibility.Visible;
					break;
			}
		}

		protected override void OnInitialized(EventArgs e) {
			SnapsToDevicePixels = true;
			MinWidth = 535;
			MaxWidth = 535;

			Grid grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
			this.Background = Application.Current.Resources["UIBackgroundBrush"] as Brush;
			this.Foreground = Application.Current.Resources["TextForeground"] as Brush;

			ScrollViewer scroll = new ScrollViewer();
			scroll.Margin = new Thickness(73, 5, 4, 3);
			scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			scroll.MinHeight = 115;
			scroll.MaxHeight = 115;

			TextBlock textBoxError = new TextBlock();
			textBoxError.Background = new SolidColorBrush(Colors.Transparent);
			textBoxError.SetValue(Grid.RowProperty, 1);
			textBoxError.TextWrapping = TextWrapping.Wrap;
			textBoxError.MouseDown += _textBoxError_MouseDown;
			scroll.Content = textBoxError;

			Image imageInformation = new Image { Width = 64, Height = 64, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Visibility = Visibility.Hidden, Margin = new Thickness(5, 3, 0, 3) };
			imageInformation.Source = ApplicationManager.GetResourceImage("information.png");
			_imageInformation = imageInformation;

			Image imageError = new Image { Width = 64, Height = 64, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Visibility = Visibility.Hidden, Margin = new Thickness(5, 3, 0, 3) };
			imageError.Source = ApplicationManager.GetResourceImage("error.png");
			_imageError = imageError;

			Image imageWarning = new Image { Width = 64, Height = 64, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Visibility = Visibility.Hidden, Margin = new Thickness(5, 3, 0, 3) };
			imageWarning.Source = ApplicationManager.GetResourceImage("warning.png");
			_imageWarning = imageWarning;

			Image imageUnspecified = new Image { Width = 64, Height = 64, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Visibility = Visibility.Hidden, Margin = new Thickness(5, 3, 0, 3) };
			imageUnspecified.Source = ApplicationManager.GetResourceImage("unknown.png");
			_imageUnspecified = imageUnspecified;

			Grid footer = new Grid { Height = 40, Background = Application.Current.Resources["UIDialogBackground"] as Brush };
			footer.SetValue(Grid.RowProperty, 1);
			footer.SetValue(WpfUtils.IsDraggableProperty, true);
			Grid panel = new Grid { Margin = new Thickness(0, 0, 3, 0) };
			panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			panel.ColumnDefinitions.Add(new ColumnDefinition());
			panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			footer.Children.Add(panel);

			Button buttonCopy = new Button();
			buttonCopy.Content = "Copy exception";
			buttonCopy.Height = 24;
			buttonCopy.Margin = new Thickness(5, 5, 3, 3);
			buttonCopy.Width = 100;
			buttonCopy.Visibility = Visibility.Hidden;
			buttonCopy.SetValue(Grid.ColumnProperty, 0);
			ButtonCopyException = buttonCopy;

			Button buttonOk = new Button();
			buttonOk.Content = "Ok";
			buttonOk.Height = 24;
			buttonOk.Margin = new Thickness(3);
			buttonOk.Width = 100;
			buttonOk.Click += _buttonClose;
			buttonOk.SetValue(Grid.ColumnProperty, 2);
			panel.Children.Add(buttonOk);
			panel.Children.Add(buttonCopy);

			grid.Children.Add(scroll);
			grid.Children.Add(imageInformation);
			grid.Children.Add(imageError);
			grid.Children.Add(imageWarning);
			grid.Children.Add(imageUnspecified);
			grid.Children.Add(footer);

			//presenter.Content = grid;
			Content = grid;

			_textBoxError = textBoxError;

			_initialize();
			buttonOk.Focus();
			base.OnInitialized(e);
		}

		public Button ButtonCopyException { get; set; }

		private void _textBoxError_MouseDown(object sender, MouseButtonEventArgs e) {
			try {
				DragMove();
			}
			catch { }
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.C && (Keyboard.Modifiers & (ModifierKeys.Control)) == ModifierKeys.Control)
				Clipboard.SetDataObject(EncodingService.GetAnsiString(_textBoxError.Text));
			else if (e.Key != Key.Down && e.Key != Key.Up &&
			         e.Key != Key.Left && e.Key != Key.Right &&
			         e.Key != Key.LeftCtrl && e.Key != Key.C) {
				if (_level == ErrorLevel.Low || _level == ErrorLevel.NotSpecified) {
					e.Handled = true;
					Close();
				}
				else if (e.Key == Key.Escape) {
					e.Handled = true;
					Close();
				}
			}
		}
	}
}
