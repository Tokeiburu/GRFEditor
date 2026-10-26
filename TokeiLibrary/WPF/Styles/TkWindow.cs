using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using ErrorManager;
using Utilities;

namespace TokeiLibrary.WPF.Styles {
	public class TkWindow : Window {
		public TkWindow() {
			SnapsToDevicePixels = true;
			//UseLayoutRounding = true;
		}

		public TkWindow(string title, string icon, SizeToContent sizeToContent = SizeToContent.WidthAndHeight, ResizeMode resizeMode = ResizeMode.NoResize, Assembly assembly = null) {
			Initialize(title, icon, sizeToContent, resizeMode, assembly);
		}

		public void Initialize(string title, string icon, SizeToContent sizeToContent = SizeToContent.WidthAndHeight, ResizeMode resizeMode = ResizeMode.NoResize, Assembly assembly = null) {
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ResizeMode = resizeMode;
			ShowInTaskbar = false;
			AllowDrop = true;
			KeyDown += new KeyEventHandler(GRFEditorWindowKeyDown);

			SnapsToDevicePixels = true;
			//UseLayoutRounding = true;
			Title = title;
			SizeToContent = sizeToContent;

			var image = ApplicationManager.PreloadResourceImage(icon);

			if (image == null) {
				MessageBox.Show("Couldn't find the icon file in the program's resources. The icon must be a .ico file and it must be placed within the application's resources (embedded resource).");

				if (ShutDownOnInvalidIcons)
					ApplicationManager.Shutdown();
			}
			else {
				Icon = image;
			}

			this.Closed += delegate {
				try {
					if (Owner == null)
						Owner = Application.Current.MainWindow;

					Owner?.Activate();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			this.Closing += delegate {
				FocusManager.SetFocusedElement(this, null);
				Keyboard.ClearFocus();
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

		protected override AutomationPeer OnCreateAutomationPeer() {
			return new FakeWindowAutomationPeer(this);
		}
	}

	public class FakeWindowAutomationPeer : WindowAutomationPeer {
		public FakeWindowAutomationPeer(Window window) : base(window) {
		}

		protected override List<AutomationPeer> GetChildrenCore() {
			return null;
		}
	}
}
