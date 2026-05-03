using ColorPicker.Sliders;
using GRF.Image;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities;
using TokeiLibrary;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;

namespace GRFEditor.WPF.PreviewTabs.Controls {
	public static class ImageHelper {
		public static void ApplyZoom(Image image, double zoom) {
			if (image.Source == null) return;
			BitmapSource bitmap = (BitmapSource)image.Source;

			image.Width = bitmap.PixelWidth * zoom;
			image.Height = bitmap.PixelHeight * zoom;
			image.Stretch = Stretch.Fill;
		}

		public static void SetupZoomUI(Image image, float maxZoom, SliderColor slider, TextBox tb, Func<float> get, Action<float> set) {
			bool eventsEnabled = true;

			slider.ValueChanged += delegate {
				if (!eventsEnabled) return;
				tb.Text = String.Format("{0:0.00}", slider.Position * (maxZoom - 1d) + 1d);
			};

			tb.TextChanged += delegate {
				eventsEnabled = false;

				try {
					var zoom = FormatConverters.DoubleConverter(tb.Text);
					set((float)zoom);
					slider.SetPosition((zoom - 1) / (maxZoom - 1d), false);
					ApplyZoom(image, zoom);
				}
				catch {
					ApplyZoom(image, 1d);
				}
				finally {
					eventsEnabled = true;
				}
			};

			tb.Text = String.Format("{0:0.00}", get());
		}

		public static void UpdateZoom(Image image, float zoom) {
			image.Dispatch(delegate {
				ApplyZoom(image, zoom);
			});
		}

		public static void ExportAs(GrfImageWrapper wrapper, string path) {
			string file = wrapper?.Image?.SaveTo(path, PathRequest.ExtractSetting);

			try {
				if (file != null)
					Utilities.Services.OpeningService.FileOrFolder(file);
			}
			catch {
			}
		}
	}
}
