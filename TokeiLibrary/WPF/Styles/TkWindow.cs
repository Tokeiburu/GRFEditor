using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ErrorManager;

namespace TokeiLibrary.WPF.Styles {
	public class TkWindow : Window {
		public TkWindow() {
		}

		public TkWindow(string title, string icon, SizeToContent sizeToContent = SizeToContent.WidthAndHeight, ResizeMode resizeMode = ResizeMode.NoResize, Assembly assembly = null) {
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ResizeMode = resizeMode;
			ShowInTaskbar = false;
			AllowDrop = true;
			KeyDown += new KeyEventHandler(GRFEditorWindowKeyDown);

			SnapsToDevicePixels = true;
			Title = title;
			SizeToContent = sizeToContent;

			Stream file;
			try {
				file = new MemoryStream(ApplicationManager.GetResource(icon, assembly));// currentAssembly.GetManifestResourceStream(names.First(p => p.EndsWith("." + icon)));
			}
			catch {
				file = null;
				MessageBox.Show("Couldn't find the icon file in the program's resources. The icon must be a .ico file and it must be placed within the application's resources (embedded resource).");

				if (ShutDownOnInvalidIcons)
					ApplicationManager.Shutdown();
			}

			if (file == null) {
				MessageBox.Show("Couldn't find the icon file in the program's resources.");

				if (ShutDownOnInvalidIcons)
					ApplicationManager.Shutdown();
			}
			else {
				try {
					IconBitmapDecoder dec = new IconBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
					Icon = dec.Frames[0];
				}
				catch (Exception) {
					try {
						Icon = ApplicationManager.GetResourceImage(icon);
						return;
					}
					catch { }

					MessageBox.Show("Invalid icon file.");

					if (ShutDownOnInvalidIcons)
						Application.Current.Shutdown();
				}
			}

			this.Closed += delegate {
				try {
					if (Owner == null)
						Owner = Application.Current.MainWindow;

					if (Owner != null) {
						Owner.Activate();
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};
		}

		public static bool ShutDownOnInvalidIcons { get; set; }

		protected void _buttonClose(object sender, EventArgs e) {
			Close();
		}

		protected virtual void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}
	}
}
