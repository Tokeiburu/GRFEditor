using System;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WPF {
	public static class WindowProvider {
		public delegate void ShowWindowEventHandler(TkWindow window);

		public static event ShowWindowEventHandler WindowOpened;

		private static void OnWindowOpened(TkWindow window) {
			ShowWindowEventHandler handler = WindowOpened;
			if (handler != null) handler(window);
		}

		public static bool? ShowWindow(TkWindow window, Window owner = null) {
			if (owner != null)
				window.Owner = owner;

			OnWindowOpened(window);
			return window.ShowDialog();
		}

		public static void Show(TkWindow window, Control itemClicked, Window owner = null) {
			if (Configuration.EnableWindowsOwnership) {
				ShowWindow(window, owner);
			}
			else {
				if (owner != null)
					window.Owner = owner;

				itemClicked.IsEnabled = false;
				window.ShowInTaskbar = true;
				window.Closed += delegate {
					itemClicked.IsEnabled = true;

					if (window.Owner != null) {
						window.Owner.Focus();
						window.Owner.Activate();
					}
				};
				OnWindowOpened(window);
				window.Show();
			}
		}

		public static void Show(TkWindow window, Control[] buttons, Window owner = null) {
			if (Configuration.EnableWindowsOwnership) {
				ShowWindow(window, owner);
			}
			else {
				window.Owner = owner;

				foreach (Control item in buttons) {
					item.IsEnabled = false;
				}

				window.ShowInTaskbar = true;
				window.Closed += delegate {
					foreach (Control item in buttons) {
						item.IsEnabled = true;
					}

					if (window.Owner != null) {
						window.Owner.Focus();
						window.Owner.Activate();
					}
				};
				OnWindowOpened(window);
				window.Show();
			}
		}

		public static T ShowWindow<T>(TkWindow window, Window owner) where T : TkWindow {
			window.Owner = owner;
			OnWindowOpened(window);
			window.ShowDialog();
			return (T) window;
		}

		public static MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton buttons,
												  string yes = "Yes", string no = "No", string cancel = "Cancel") {

			if (Application.Current == null)
				return MessageBoxResult.Cancel;

			return (MessageBoxResult)Application.Current.Dispatch(() => {
				try {
					MessageDialog dialog = new MessageDialog(message, buttons, caption, yes, no, cancel);
					Window topWindow = WpfUtilities.TopWindow;

					dialog.Owner = topWindow;
					OnWindowOpened(dialog);
					dialog.ShowDialog();
					
					dialog.Closed += delegate {
						if (dialog.Owner != null) {
							dialog.Owner.Focus();
							dialog.Owner.Activate();
						}
					};

					return dialog.Result;
				}
				catch {
					return MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel);
				}
			});
		}

		public static MessageBoxResult ShowDialog(string message) {
			if (Application.Current == null)
				return MessageBoxResult.Cancel;

			return (MessageBoxResult)Application.Current.Dispatch(() => {
				MessageDialog dialog = new MessageDialog(message, MessageBoxButton.OK, "");

				if (Application.Current.MainWindow.IsVisible) {
					dialog.Owner = Application.Current.MainWindow;
				}
				else {
					foreach (Window window in Application.Current.Windows) {
						if (window.IsVisible) {
							dialog.Owner = window;
						}
					}
				}

				OnWindowOpened(dialog);
				dialog.ShowDialog();
				return dialog.Result;
			});
		}
	}
}
