using System;
using System.IO;
using System.Linq;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;
using Action = GRF.FileFormats.ActFormat.Action;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for PatcherDialog.xaml
	/// </summary>
	public partial class PatcherDialog : TkWindow, IDisposable {
		private readonly AsyncOperation _asyncOperation;
		private readonly GrfHolder _newerGrf = new GrfHolder();
		private readonly GrfHolder _olderGrf = new GrfHolder();
		private string _fileNewer;
		private string _fileOlder;

		public PatcherDialog() : base("Patch maker", "diff.ico", SizeToContent.WidthAndHeight, ResizeMode.NoResize) {
			InitializeComponent();
			_asyncOperation = new AsyncOperation(_progress);
			_textBoxOutputName.Text = String.Format("{0:0000}-{1:00}-{2:00}{3}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, "data.grf");

			WpfUtilities.SetMinAndMaxSize(this);
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private void _buttonOK_Click(object sender, RoutedEventArgs e) {
			if (_asyncOperation.IsRunning) {
				ErrorHandler.HandleException("An operation is already running, cancel it first.");
				return;
			}

			if (!File.Exists(_fileOlder) || !File.Exists(_fileNewer)) {
				ErrorHandler.HandleException("One of the GRF couldn't be located (file not found).");
				return;
			}

			if (_textBoxOutputName.Text == "" || _textBoxOutputName.Text.Any(p => Path.GetInvalidFileNameChars().Contains(p))) {
				ErrorHandler.HandleException("Invalid output file name.");
				return;
			}

			_olderGrf.Close();
			_olderGrf.Open(_fileOlder);

			_newerGrf.Close();
			_newerGrf.Open(_fileNewer);

			if (_olderGrf.Header.FoundErrors) {
				ErrorHandler.HandleException("The older GRF contains errors and cannot be used to make a patch.");
				return;
			}

			if (_newerGrf.Header.FoundErrors) {
				ErrorHandler.HandleException("The newer GRF contains errors and cannot be used to make a patch.");
				return;
			}

			FileInfo infoOld = new FileInfo(_fileOlder);
			FileInfo infoNew = new FileInfo(_fileNewer);
			string fileName = Path.Combine(Methods.ApplicationPath, _textBoxOutputName.Text);

			MessageBoxResult result = MessageBoxResult.None;

			if (infoOld.LastWriteTime > infoNew.LastWriteTime) {
				result = WindowProvider.ShowDialog("The old GRF is newer than the new GRF file, would you like to switch them?", "Unconsistent date", MessageBoxButton.YesNoCancel);
			}

			if (result == MessageBoxResult.Cancel) {
				return;
			}

			if (result == MessageBoxResult.Yes) {
				GrfThread thread = new GrfThread(() => _newerGrf.Patch(_olderGrf, Path.Combine(GrfEditorConfiguration.ProgramDataPath, Path.GetFileName(fileName))), _olderGrf, 200, fileName);
				thread.Finished += new GrfThread.GrfThreadEventHandler(_thread_Finished);
				_asyncOperation.SetAndRunOperation(thread);
			}
			else {
				GrfThread thread = new GrfThread(() => _olderGrf.Patch(_newerGrf, Path.Combine(GrfEditorConfiguration.ProgramDataPath, Path.GetFileName(fileName))), _newerGrf, 200, fileName);
				thread.Finished += new GrfThread.GrfThreadEventHandler(_thread_Finished);
				_asyncOperation.SetAndRunOperation(thread);
			}
		}

		private void _thread_Finished(object state) {
			try {
				OpeningService.FileOrFolder(Path.Combine(GrfEditorConfiguration.ProgramDataPath, Path.GetFileName((string) state)));
			}
			catch {
			}
		}

		private void _pathBrowserOldGrf_TextChanged(object sender, EventArgs e) {
			if (File.Exists(_pathBrowserOldGrf.Text)) {
				GrfEditorConfiguration.AppLastPath = _pathBrowserOldGrf.Text;
				_fileOlder = _pathBrowserOldGrf.Text;
			}
		}

		private void _pathBrowserNewGrf_TextChanged(object sender, EventArgs e) {
			if (File.Exists(_pathBrowserNewGrf.Text)) {
				GrfEditorConfiguration.AppLastPath = _pathBrowserNewGrf.Text;
				_fileNewer = _pathBrowserNewGrf.Text;
			}
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_olderGrf != null)
					_olderGrf.Dispose();

				if (_newerGrf != null)
					_newerGrf.Dispose();
			}
		}
	}
}