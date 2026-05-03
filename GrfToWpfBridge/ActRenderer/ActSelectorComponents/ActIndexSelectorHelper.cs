using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;

namespace GrfToWpfBridge.ActRenderer.ActSelectorComponents {
	public static class ActIndexSelectorHelper {
		public static void BuildDirectionalActionSelectorUI(List<FancyButton> fancyButtons, bool buttonsEnabled) {
			BitmapSource image = ApplicationManager.PreloadResourceImage("arrow.png");
			BitmapSource image2 = ApplicationManager.PreloadResourceImage("arrowoblique.png");

			for (int i = 0; i < fancyButtons.Count; i++) {
				var fb = fancyButtons[i];

				fb.ImageIcon.Stretch = Stretch.None;
				fb.ImageIcon.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
				fb.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				fb.ImageIcon.RenderTransform = new RotateTransform { Angle = i / 2 * 90 + 90 };
				fb.ImageIcon.Source = i % 2 == 0 ? image : image2;
			}

			if (!buttonsEnabled)
				fancyButtons.ForEach(p => p.IsButtonEnabled = false);
		}

		public static void BuildDirectionalActionSelectorUI(List<SimpleButton> fancyButtons, bool buttonsEnabled) {
			BitmapSource image = ApplicationManager.PreloadResourceImage("arrow.png");
			BitmapSource image2 = ApplicationManager.PreloadResourceImage("arrowoblique.png");

			for (int i = 0; i < fancyButtons.Count; i++) {
				var fb = fancyButtons[i];

				var imageUi = new Image();
				fb.Content = imageUi;
				imageUi.Stretch = Stretch.None;
				imageUi.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
				imageUi.RenderTransformOrigin = new Point(0.5, 0.5);
				imageUi.RenderTransform = new RotateTransform { Angle = i / 2 * 90 + 90 };
				imageUi.Source = i % 2 == 0 ? image : image2;
			}

			if (!buttonsEnabled)
				fancyButtons.ForEach(p => p.IsEnabled = false);
		}

		public static void UpdatePlayButtonUI(FancyButton playButton) {
			((TextBlock)playButton.FindName("_tbIdentifier")).Margin = new Thickness(3, 0, 0, 3);
			((Grid)((Grid)((Border)playButton.FindName("_border")).Child).Children[2]).HorizontalAlignment = HorizontalAlignment.Left;
			((Grid)((Grid)((Border)playButton.FindName("_border")).Child).Children[2]).Margin = new Thickness(2, 0, 0, 0);

			if (playButton.IsPressed) {
				playButton.ImagePath = "stop2.png";
				playButton.TextHeader = "Stop";
			}
			else {
				playButton.ImagePath = "play.png";
				playButton.TextHeader = "Play";
			}
		}
	}
}
