using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokeiLibrary.WPF {
	public class SimpleButton : Button {
		//public SimpleButton() {
		//	//Click += delegate {
		//	//	IsStatePressed = !IsStatePressed;
		//	//};
		//}

		public bool IsStatePressed {
			get { return (bool)GetValue(IsStatePressedProperty); }
			set { SetValue(IsStatePressedProperty, value); }
		}

		public static readonly DependencyProperty IsStatePressedProperty = DependencyProperty.Register("IsStatePressed", typeof(bool), typeof(SimpleButton), new PropertyMetadata(false));

		public string ImagePath {
			get { return (string)GetValue(ImagePathProperty); }
			set { SetValue(ImagePathProperty, value); }
		}

		public static DependencyProperty ImagePathProperty = DependencyProperty.Register("ImagePath", typeof(string), typeof(SimpleButton), new PropertyMetadata(new PropertyChangedCallback(OnImagePathChanged)));

		private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var fb = d as SimpleButton;

			if (fb != null) {
				var img = ApplicationManager.PreloadResourceImage(e.NewValue.ToString());
				Image image = new Image();
				image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
				image.Stretch = Stretch.None;
				image.Source = img;
				fb.Content = image;
			}
		}
	}
}
