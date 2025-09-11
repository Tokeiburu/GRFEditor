using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.FileFormats.GatFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Tools.SpriteEditor;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using Utilities;

namespace GRFEditor {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public App() {
			Configuration.ConfigAsker = GrfEditorConfiguration.ConfigAsker;
			Configuration.ProgramDataPath = GrfEditorConfiguration.ProgramDataPath;
			SpriteEditorConfiguration.ConfigAsker = GrfEditorConfiguration.ConfigAsker;
			ErrorHandler.SetErrorHandler(new DefaultErrorHandler());
			SelfPatcher.SelfPatch();
		}

		protected override void OnStartup(StartupEventArgs e) {
			ApplicationManager.CrashReportEnabled = true;
			ImageConverterManager.AddConverter(new DefaultImageConverter());
			Gat.AutomaticallyFixNegativeGatTypes = true;

			Configuration.SetImageRendering(Resources);

			Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/GRFEditorStyles.xaml", UriKind.RelativeOrAbsolute) });
			//Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(@"C:\tktoolsuite\GRFEditor\GRFEditor\WPF\Styles\StyleRed.xaml", UriKind.Absolute) });

			if (GrfEditorConfiguration.ThemeIndex == 0) {
				Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/StyleLightBlue.xaml", UriKind.RelativeOrAbsolute) });
			}
			else if (GrfEditorConfiguration.ThemeIndex == 1) {
				Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/StyleDark.xaml", UriKind.RelativeOrAbsolute) });
				//Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(@"C:\tktoolsuite\GRFEditor\GRFEditor\WPF\Styles\StyleGilgamesh.xaml", UriKind.Absolute) });
				//
				//Methods.FileModified(@"C:\tktoolsuite\GRFEditor\GRFEditor\WPF\Styles", "StyleGilgamesh.xaml", delegate {
				//	Application.Current.Dispatch(delegate {
				//		try {
				//			Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count - 1);
				//
				//			//Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });
				//			Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(@"C:\tktoolsuite\GRFEditor\GRFEditor\WPF\Styles\StyleGilgamesh.xaml", UriKind.Absolute) });
				//		}
				//		catch {
				//			Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(@"C:\tktoolsuite\GRFEditor\GRFEditor\WPF\Styles\StyleDark.xaml", UriKind.Absolute) });
				//		}
				//	});
				//});

				ApplicationManager.ImageProcessing = delegate(string name, BitmapFrame img) {
					if (img == null) return null;

					if (name.Contains("reset.png")) {
						Func<byte, byte, byte, byte, Color> shader = delegate(byte A, byte R, byte G, byte B) {
							return Color.FromArgb(A, _clamp((R) * 1.8), _clamp(G / 3), _clamp(B / 3));
						};

						return _applyShader(img, shader);
					}
					else if (name.Contains("eye.png") || name.Contains("smallArrow.png")) {
						Func<byte, byte, byte, byte, Color> shader = delegate(byte A, byte R, byte G, byte B) {
							return Color.FromArgb(A, _clamp((255 - R) * 0.8), _clamp((255 - G) * 0.8), _clamp((255 - B) * 0.8));
						};

						return _applyShader(img, shader);
					}
					else if (name.Contains("arrow.png") ||
							 name.Contains("arrowoblique.png")) {
						Func<byte, byte, byte, byte, Color> shader = delegate(byte A, byte R, byte G, byte B) {
							return Color.FromArgb(A, _clamp(240 + R), _clamp(150 + G), 0);
							//FE9700
						};
						//F68D00
						return _applyShader(img, shader);
					}

					return img;
				};
			}

			base.OnStartup(e);
		}

		private byte _clamp(int val) {
			if (val < 0)
				return 0;
			if (val > 255)
				return 255;
			return (byte)val;
		}

		private byte _clamp(double val) {
			if (val < 0)
				return 0;
			if (val > 255)
				return 255;
			return (byte)val;
		}

		private void _darkTheme(byte[] pixels, PixelFormat format, List<Color> colors, Func<byte, byte, byte, byte, Color> shader) {
			if (format.BitsPerPixel / 8 == 1) {
				for (int i = 0; i < colors.Count; i++) {
					colors[i] = shader(colors[i].A, colors[i].R, colors[i].G, colors[i].B);
				}
			}
			else if (format.BitsPerPixel / 8 == 3) {
				for (int i = 0; i < pixels.Length; i += 3) {
					Color c = shader(255, pixels[i + 2], pixels[i + 1], pixels[i + 0]);

					pixels[i + 0] = c.B;
					pixels[i + 1] = c.G;
					pixels[i + 2] = c.R;
				}
			}
			else if (format.BitsPerPixel / 8 == 4) {
				for (int i = 0; i < pixels.Length; i += 4) {
					Color c = shader(pixels[i + 3], pixels[i + 2], pixels[i + 1], pixels[i + 0]);

					pixels[i + 0] = c.B;
					pixels[i + 1] = c.G;
					pixels[i + 2] = c.R;
					pixels[i + 3] = c.A;
				}
			}
		}

		private WriteableBitmap _applyShader(BitmapFrame img, Func<byte, byte, byte, byte, Color> shader) {
			const double DPI = 96;

			if (Methods.CanUseIndexed8 || img.Format != PixelFormats.Indexed8) {
				int width = img.PixelWidth;
				int height = img.PixelHeight;

				int stride = (int)Math.Ceiling(width * img.Format.BitsPerPixel / 8f);
				byte[] pixelData = new byte[stride * height];
				img.CopyPixels(pixelData, stride, 0);
				_darkTheme(pixelData, img.Format, null, shader);
				var wBitmap = new WriteableBitmap(BitmapSource.Create(width, height, DPI, DPI, img.Format, img.Palette, pixelData, stride));
				wBitmap.Freeze();
				return wBitmap;
			}
			else {
				List<Color> colors = new List<Color>(img.Palette.Colors);
				byte[] pixelData = new byte[img.PixelWidth * img.PixelHeight * img.Format.BitsPerPixel / 8];
				img.CopyPixels(pixelData, img.PixelWidth * img.Format.BitsPerPixel / 8, 0);
				_darkTheme(pixelData, img.Format, colors, shader);
				var wBitmap = WpfImaging.ToBgra32FromIndexed8(pixelData, colors, img.PixelWidth, img.PixelHeight);
				wBitmap.Freeze();
				return wBitmap;
			}
		}
	}
}