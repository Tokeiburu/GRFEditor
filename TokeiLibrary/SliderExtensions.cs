using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Utilities;

namespace TokeiLibrary {
	public static class SliderExtensions {
		public static readonly DependencyProperty EnableImmediateDragProperty =
			DependencyProperty.RegisterAttached("EnableImmediateDrag", typeof(bool), typeof(SliderExtensions), new PropertyMetadata(false, OnEnableImmediateDragChanged));

		public static void SetEnableImmediateDrag(DependencyObject element, bool value) {
			element.SetValue(EnableImmediateDragProperty, value);
		}

		public static bool GetEnableImmediateDrag(DependencyObject element) {
			return (bool)element.GetValue(EnableImmediateDragProperty);
		}

		private static void OnEnableImmediateDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if (d is Slider slider) {
				if ((bool)e.NewValue) {
					slider.IsMoveToPointEnabled = true;

					slider.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_PreviewMouseLeftButtonDown), true);
					slider.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(Slider_PreviewMouseMove), true);
					slider.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler(Slider_PreviewMouseUp), true);
				}
			}
		}

		private static void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (sender is Slider slider) {
				slider.CaptureMouse();

				UpdateSliderValue(slider, e.GetPosition(slider));
			}
		}

		private static void Slider_PreviewMouseUp(object sender, MouseEventArgs e) {
			if (sender is Slider slider && slider.IsMouseCaptured) {
				slider.ReleaseMouseCapture();
			}
		}

		private static void Slider_PreviewMouseMove(object sender, MouseEventArgs e) {
			if (sender is Slider slider && slider.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) {
				UpdateSliderValue(slider, e.GetPosition(slider));
			}
		}

		private static void UpdateSliderValue(Slider slider, Point mousePos) {
			double realWidth = slider.ActualWidth - 8;
			double realX = mousePos.X - 4;
			realX = Methods.Clamp(realX, 0, realWidth);

			double relativePosition = realX / realWidth;

			double value;

			if (slider.IsSnapToTickEnabled) {
				var distance = (slider.Maximum - slider.Minimum);
				double interval = slider.TickFrequency;
				relativePosition = Math.Round(relativePosition * distance) / distance;
			}

			value = slider.Minimum + relativePosition * (slider.Maximum - slider.Minimum);
			slider.Value = value;
		}
	}
}
