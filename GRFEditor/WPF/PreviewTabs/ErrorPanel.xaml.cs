using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TokeiLibrary;
using static GrfToWpfBridge.Application.DefaultErrorHandler;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for ErrorPanel.xaml
	/// </summary>
	public partial class ErrorPanel : UserControl {
		public DebuggerParameters LastDebugInfo = new DebuggerParameters();
		private Dictionary<WindowsFormsHost, Visibility> _hostVisibilities = new Dictionary<WindowsFormsHost, Visibility>();

		public ErrorPanel() {
			InitializeComponent();

			_imageHeader.Source = ApplicationManager.PreloadResourceImage("lock.png");
		}

		private bool Visible { get; set; } = false;

		public void ShowError(FilePreviewTab tab, FileEntry entry, Exception err) {
			LastDebugInfo.Exception = err;
			LastDebugInfo.StackTrace = new System.Diagnostics.StackTrace(true);

			this.Dispatch(delegate {
				var tbHeader = WpfUtilities.FindFirstChild<TextBlock>(tab);

				if (tbHeader != null)
					_labelHeader.Text = tbHeader.Text;
				else
					_labelHeader.Text = "File: " + entry.FileName;

				_tbError2.Text = err.GetType().ToString();

				StringBuilder b = new StringBuilder();

				b.AppendLine("Entry: " + entry.RelativePath);
				b.Append(err.Message);

				var visibility = Visibility.Collapsed;

				if (err is GrfException grfErr) {
					if (grfErr == GrfExceptions.__CorruptedOrEncryptedEntry || grfErr == GrfExceptions.__GravityEncryptedFile)
						visibility = Visibility.Visible;
				}

				if (visibility != _imageHeader.Visibility)
					_imageHeader.Visibility = visibility;

				_tbError.Text = b.ToString();

				var hosts = WpfUtilities.FindChildren<WindowsFormsHost>(tab);

				if (_hostVisibilities.Count == 0) {
					foreach (var host in hosts) {
						_hostVisibilities[host] = host.Visibility;
						host.Visibility = Visibility.Collapsed;
					}
				}

				if (this.Visibility == Visibility.Collapsed) {
					this.Visibility = Visibility.Visible;
					Visible = true;
				}
			});
		}

		public void ClearError() {
			if (!Visible)
				return;

			this.Dispatch(delegate {
				foreach (var host in _hostVisibilities) {
					host.Key.Visibility = host.Value;
				}

				_hostVisibilities.Clear();

				if (this.Visibility == Visibility.Visible) {
					this.Visibility = Visibility.Collapsed;
					Visible = false;
				}
			});
		}

		private void _buttonCopyException_Click(object sender, RoutedEventArgs e) {
			string rawInfo = ErrorHandler.GenerateOutput(LastDebugInfo.Exception, LastDebugInfo.StackTrace);

			Clipboard.SetDataObject(rawInfo);
			MessageBox.Show("Debug information has been copied to the clipboard.", "Information");
		}
	}
}
