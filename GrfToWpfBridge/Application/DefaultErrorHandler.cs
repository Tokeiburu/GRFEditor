using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using ErrorManager;
using GRF.Threading;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;

namespace GrfToWpfBridge.Application {
	/// <summary>
	/// Class imported from GrfEditor
	/// </summary>
	public class DefaultErrorHandler : IErrorHandler {
		public class DebuggerParameters {
			public DateTime Time { get; set; }
			public Exception Exception { get; set; }
			public StackTrace StackTrace { get; set; }
			public string Message { get; set; }
		}

		private static readonly DebuggerParameters _recentDebugInfo = new DebuggerParameters();
		private string _latestException;
		public bool IgnoreNoMainWindow { get; set; }

		#region IErrorHandler Members

		public void Handle(Exception exception, ErrorLevel errorLevel) {
			_reportAnyManagedExceptions(exception.Message, exception, errorLevel);

			if (errorLevel < Configuration.WarningLevel) return;
			if (_exceptionAlreadyShown(exception.Message)) return;

			if (System.Windows.Application.Current != null) {
				_checkMainWindow();

				if (IgnoreNoMainWindow) {
					WindowProvider.ShowWindow(_addDebugButton(new ErrorDialog("Information", _getHeader(errorLevel) + GetMessage(exception), errorLevel)));
				}
				else {
					bool mainWindowVisible = (bool)System.Windows.Application.Current.Dispatch(() => System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow.IsLoaded);
					
					if (!mainWindowVisible) {
						Clipboard.SetDataObject(ErrorHandler.GenerateOutput(_recentDebugInfo.Exception, _recentDebugInfo.StackTrace));
						_showBasicError(_getHeader(errorLevel) + GetMessage(exception), "Information");
						return;
					}

					System.Windows.Application.Current.Dispatch(() => WindowProvider.ShowWindow(_addDebugButton(new ErrorDialog("Information", _getHeader(errorLevel) + GetMessage(exception), errorLevel)), WpfUtilities.TopWindow ?? System.Windows.Application.Current.MainWindow));
				}
			}
		}

		public bool YesNoRequest(string message, string caption) {
			return WindowProvider.ShowDialog(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}

		#endregion

		internal static string GetMessage(Exception ex) {
			if (ex == null)
				return "Null exception.";

			if (ex.InnerException == null)
				return _dotTerminate(ex.Message);

			if (ex.Message.EndsWith("\r\n\r\n"))
				return _dotTerminate(ex.Message) + "Inner exception: " + ex.InnerException.Message;

			if (ex.Message.EndsWith("\r\n"))
				return _dotTerminate(ex.Message) + "\r\nInner exception: " + ex.InnerException.Message;

			return _dotTerminate(ex.Message) + "\r\n\r\nInner exception: " + ex.InnerException.Message;
		}

		private static string _dotTerminate(string s) {
			if (!s.EndsWith("."))
				return s + ".";
			return s;
		}

		/// <summary>
		/// This method adds the "Copy exception" button by adding elements directly
		/// in the visual tree of the ErrorDialog from TokeiLibrary. This is necessary
		/// due to compatibility issues with other softwares.
		/// </summary>
		/// <param name="errorDialog">The error dialog.</param>
		/// <returns></returns>
		private static TkWindow _addDebugButton(ErrorDialog errorDialog) {
			errorDialog.Loaded += delegate {
				try {
					if (errorDialog.ButtonCopyException != null) {
						errorDialog.ButtonCopyException.Visibility = Visibility.Visible;
						string rawInfo = ErrorHandler.GenerateOutput(_recentDebugInfo.Exception, _recentDebugInfo.StackTrace);

						errorDialog.ButtonCopyException.Click += delegate {
							Clipboard.SetDataObject(rawInfo);
							MessageBox.Show("Debug information has been copied to the clipboard.", "Information");
						};
					}
				}
				catch {
				}
			};

			return errorDialog;
		}

		private bool _exceptionAlreadyShown(string exception) {
			try {
				if (_latestException == null) {
					_latestException = exception;
					return false;
				}

				if (_latestException != null) {
					if (exception == _latestException) {
						if (System.Windows.Application.Current != null) {
							bool res = (bool) System.Windows.Application.Current.Dispatch(delegate {
								try {
									return System.Windows.Application.Current.Windows.OfType<ErrorDialog>().Any();
								}
								catch {
									return false;
								}
							});

							if (!res) {
								_latestException = exception;
							}

							return res;
						}
					}
				}

				_latestException = exception;
				return false;
			}
			catch {
				return false;
			}
		}

		private static void _checkMainWindow() {
			System.Windows.Application.Current.Dispatch(delegate {
				if (System.Windows.Application.Current.MainWindow == null || !System.Windows.Application.Current.MainWindow.IsLoaded) {
					foreach (Window window in System.Windows.Application.Current.Windows) {
						if (window.Visibility == Visibility.Visible && window.IsLoaded) {
							System.Windows.Application.Current.MainWindow = window;
						}
					}
				}
			});
		}

		private void _reportAnyManagedExceptions(string message, Exception exception, ErrorLevel errorLevel) {
			if (Configuration.LogAnyExceptions) {
				try {
					string crash = "\r\n\r\n\r\n" + ApplicationManager.PrettyLine(DateTime.Now.ToString(CultureInfo.InvariantCulture)) + "\r\n";
					crash += ErrorHandler.GenerateOutput(exception, new StackTrace(true));
					File.AppendAllText(Path.Combine(Configuration.ProgramDataPath, "debug.log"), crash);
				}
				catch {
				}
			}

			StackTrace trace = new StackTrace(true);

			_recentDebugInfo.StackTrace = trace;
			_recentDebugInfo.Time = DateTime.Now;
			_recentDebugInfo.Exception = exception;
			_recentDebugInfo.Message = message;
		}

		private static void _showBasicError(string message, string caption) {
			MessageBox.Show("An error has been encountered before the application could load properly.\n" + message, caption);
			GrfThread.Start(delegate {
				int time = 5000;
				const int Check = 200;

				while (time > 0) {
					time -= Check;
					Thread.Sleep(Check);

					if (WpfUtilities.TopWindow != null && WpfUtilities.TopWindow.IsVisible)
						return;
				}

				if (WpfUtilities.TopWindow == null || !WpfUtilities.TopWindow.IsVisible) {
					ApplicationManager.Shutdown();
				}
			});
		}

		private static string _getHeader(ErrorLevel level) {
			string headerMessage = "";

			switch (level) {
				case ErrorLevel.Warning:
					headerMessage = "An unhandled exception has been thrown:\r\n\r\n";
					break;
				case ErrorLevel.Critical:
					headerMessage = "A critical error has been encountered:\r\n\r\n";
					break;
			}

			return headerMessage;
		}
	}
}