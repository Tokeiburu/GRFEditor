using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary {
	public static class WpfProperties {
		#region Dependency Properties
		public static DependencyProperty IsDraggableProperty;
		public static DependencyProperty IsMouseEffectOnProperty;
		public static DependencyProperty ImagePathProperty;
		public static DependencyProperty IconPathProperty;
		public static DependencyProperty ReverseMouseContextMenuProperty;
		public static DependencyProperty DisableChildrenWindowsProperty;
		#endregion

		static WpfProperties() {
			ReverseMouseContextMenuProperty = DependencyProperty.RegisterAttached(
				"ReverseMouseContextMenu",
				typeof(bool),
				typeof(WpfProperties),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterReverseMouseContextMenu)));
			ImagePathProperty = DependencyProperty.RegisterAttached(
				"ImagePath",
				typeof(string),
				typeof(WpfProperties),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisterImagePath)));
			IsDraggableProperty = DependencyProperty.RegisterAttached(
				"IsDraggable",
				typeof(bool),
				typeof(WpfProperties),
				new PropertyMetadata(new PropertyChangedCallback(OnRegisteIsDraggable)));
			DisableChildrenWindowsProperty = DependencyProperty.RegisterAttached(
				"DisableChildrenWindows",
				typeof(bool),
				typeof(Window),
				new PropertyMetadata());
		}

		#region Attached Property Setters/Getters
		public static Boolean GetReverseMouseContextMenu(DependencyObject obj) {
			return (Boolean)obj.GetValue(ReverseMouseContextMenuProperty);
		}

		public static void SetReverseMouseContextMenu(DependencyObject obj, Boolean value) {
			obj.SetValue(ReverseMouseContextMenuProperty, value);
		}

		public static Boolean GetIsMouseEffectOn(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsMouseEffectOnProperty);
		}

		public static void SetIsMouseEffectOn(DependencyObject obj, Boolean value) {
			obj.SetValue(IsMouseEffectOnProperty, value);
		}

		public static Boolean GetIsDraggable(DependencyObject obj) {
			return (Boolean)obj.GetValue(IsDraggableProperty);
		}

		public static void SetIsDraggable(DependencyObject obj, Boolean value) {
			obj.SetValue(IsDraggableProperty, value);
		}

		public static string GetImagePath(DependencyObject obj) {
			return (string)obj.GetValue(ImagePathProperty);
		}

		public static void SetImagePath(DependencyObject obj, string value) {
			obj.SetValue(ImagePathProperty, value);
		}

		public static Boolean GetDisableChildrenWindows(DependencyObject obj) {
			return (Boolean)obj.GetValue(DisableChildrenWindowsProperty);
		}

		public static void SetDisableChildrenWindows(DependencyObject obj, Boolean value) {
			obj.SetValue(DisableChildrenWindowsProperty, value);
		}
		#endregion

		#region PropertyChangedHandlers
		private static void OnRegisterReverseMouseContextMenu(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if (d is Button element) {
				Action action = delegate {
					element.ContextMenu.Placement = PlacementMode.Bottom;
					element.ContextMenu.PlacementTarget = element;
					element.PreviewMouseRightButtonUp += delegate (object sender, MouseButtonEventArgs e1) { e1.Handled = true; };

					element.Click += delegate {
						element.ContextMenu.IsOpen = true;
					};
				};

				if (element.ContextMenu == null) {
					element.Loaded += delegate {
						action();
					};
				}
				else {
					action();
				}
			}

			if (d is FancyButton fElement) {
				Action action = delegate {
					fElement.ContextMenu.Placement = PlacementMode.Bottom;
					fElement.ContextMenu.PlacementTarget = fElement;
					fElement.PreviewMouseRightButtonUp += delegate (object sender, MouseButtonEventArgs e1) { e1.Handled = true; };

					fElement.Click += delegate {
						fElement.ContextMenu.IsOpen = true;
					};
				};

				if (fElement.ContextMenu == null) {
					fElement.Loaded += delegate {
						action();
					};
				}
				else {
					action();
				}
			}
		}

		private static void OnRegisterImagePath(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			try {
				if (d is Image element) {
					element.Source = ApplicationManager.PreloadResourceImage(e.NewValue.ToString());
					element.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
					element.Stretch = Stretch.None;
				}

				if (d is MenuItem mi) {
					var imageSource = ApplicationManager.PreloadResourceImage(e.NewValue.ToString());
					Image image = new Image { Source = imageSource, Stretch = Stretch.None };
					image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
					image.Width = imageSource.PixelWidth;
					image.Height = imageSource.PixelHeight;
					mi.Icon = image;
				}

				if (d is FancyButton fb) {
					var imageSource = ApplicationManager.PreloadResourceImage(e.NewValue.ToString());
					fb._imageIcon.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
					fb._imageIcon.Stretch = Stretch.None;
					fb._imageIcon.Width = imageSource.PixelWidth;
					fb._imageIcon.Height = imageSource.PixelHeight;
					fb._imageIcon.Source = imageSource;
				}

				if (d is Button b) {
					Image image = new Image { Source = ApplicationManager.PreloadResourceImage(e.NewValue.ToString()), Stretch = Stretch.None };
					image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
					b.Content = image;
				}
			}
			catch { }
		}

		private static void OnRegisteIsDraggable(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			UIElement grid = d as UIElement;

			if (grid != null) {
				var active = (bool)e.NewValue == true;

				if (active)
					grid.MouseDown += _grid_MouseDown;
				else
					grid.MouseDown -= _grid_MouseDown;
			}
		}

		private static void _grid_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				Window window = WpfUtilities.FindParentControl<Window>((DependencyObject)sender);

				if (window != null) {
					window.DragMove();
				}
			}
		}
		#endregion
	}
}
