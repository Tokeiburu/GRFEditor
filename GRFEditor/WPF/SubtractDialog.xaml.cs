using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for MergeDialog.xaml
	/// </summary>
	public partial class SubtractDialogCustom : TkWindow, IDisposable {
		private readonly AsyncOperation _asyncOperation;
		private readonly EditorMainWindow _editor;
		private readonly GrfHolder _grfAdd = new GrfHolder();
		protected readonly GrfHolder _originalGrf;
		public bool RequiresReload = false;
		private bool _allowCancel;
		private bool _cancelThread;
		private string _fileSub;

		protected string _fileNameOriginal;
		private string _fileSource;
		private GrfHolder _grfSource = new GrfHolder();

		public SubtractDialogCustom(EditorMainWindow editor, GrfHolder grfSource)
			: base("Subtract GRFs", "convert.ico") {
			InitializeComponent();

			_textBoxOutputName.Text = String.Format("{0:0000}-{1:00}-{2:00}{3}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, "subtract.grf");
			_asyncOperation = new AsyncOperation(_progressBarComponent);
			_editor = editor;
			_grfSource = grfSource;
			_originalGrf = grfSource;

			if (!grfSource.IsModified && !grfSource.IsNewGrf)
				_pathBrowserGrf1.Text = grfSource.FileName;

			if (grfSource.IsOpened) {
				_fileNameOriginal = _grfSource.FileName;
			}
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		protected override void OnClosing(CancelEventArgs e) {
			if (_cancelThread) {
				e.Cancel = !_allowCancel;
				return;
			}

			if (_grfSource != null && _grfSource.IsOpened)
				_grfSource.Cancel();

			_buttonCancel.Dispatch(p => p.IsEnabled = false);

			_cancelThread = true;

			GrfThread.Start(delegate {
				while (_grfSource != null && _grfSource.IsOpened && _grfSource.IsBusy) {
					Thread.Sleep(200);
				}

				_allowCancel = true;
				this.Dispatch(p => p.Close());
			});

			e.Cancel = !_allowCancel;
		}

		protected void _buttonOK_Click(object sender, RoutedEventArgs e) {
			if (_fileSource != null) {
				if (_fileSource == _fileNameOriginal)
					_grfSource = _originalGrf;
				else {
					_grfSource = new GrfHolder();
					_grfSource.Open(_fileSource);
				}

				if (_grfSource.Header.FoundErrors) {
					ErrorHandler.HandleException("The current GRF contains errors and cannot be subtracted.");
					return;
				}
			}

			if (_textBoxOutputName.Text == "" || _textBoxOutputName.Text.Any(p => Path.GetInvalidFileNameChars().Contains(p))) {
				ErrorHandler.HandleException("Invalid output file name.");
				return;
			}

			if (_grfSource.IsModified) {
				ErrorHandler.HandleException("The current GRF has been modified, you must save it first.");
				return;
			}

			if (_grfSource.IsBusy) {
				ErrorHandler.HandleException("The current GRF is saving, please wait for the operation to finish first.");
				return;
			}

			if (_grfSource.Header.FoundErrors) {
				ErrorHandler.HandleException("The current GRF contains errors and cannot be subtracted.");
				return;
			}

			if (_fileSub == null) {
				ErrorHandler.HandleException("Drop a GRF in the right rectangle first.");
				return;
			}

			_grfAdd.Close();
			_grfAdd.Open(_fileSub);

			if (_grfAdd.IsModified) {
				ErrorHandler.HandleException("The subtracting GRF has been modified, you must save it first.");
				return;
			}

			if (_grfAdd.IsBusy) {
				ErrorHandler.HandleException("The subtracting GRF is saving, please wait for the operation to finish first.");
				return;
			}

			if (_grfAdd.Header.FoundErrors) {
				ErrorHandler.HandleException("The subtracting GRF contains errors and cannot be merged.");
				return;
			}

			if (_grfAdd.FileName == _grfSource.FileName) {
				ErrorHandler.HandleException("Trying to subtract the same GRF into itself.");
				return;
			}

			string fileName = Path.Combine(Methods.ApplicationPath, _textBoxOutputName.Text);
			_buttonOK.Dispatch(p => p.IsEnabled = false);
			_pathBrowserGrf1.Dispatch(p => p.IsEnabled = false);
			_pathBrowserGrf2.Dispatch(p => p.IsEnabled = false);
			_textBoxOutputName.Dispatch(p => p.IsEnabled = false);

			foreach (var entry in _grfAdd.FileTable.Entries) {
				if (_grfSource.FileTable.ContainsFile(entry.RelativePath)) {
					_grfSource.Commands.RemoveFile(entry.RelativePath);
				}
			}

			_grfAdd.Close();

			_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfSource.Save(fileName), _grfSource, 200, _grfSource), _syncFinished);
		}

		private void _syncFinished(object grfSource) {
			try {
				_pathBrowserGrf1.Dispatch(p => p.IsEnabled = true);
				_pathBrowserGrf2.Dispatch(p => p.IsEnabled = true);
				_buttonOK.Dispatch(p => p.IsEnabled = true);
				_textBoxOutputName.Dispatch(p => p.IsEnabled = true);

				string fileName = Path.Combine(Methods.ApplicationPath, _textBoxOutputName.Dispatch(p => p.Text));
				OpeningService.FileOrFolder(fileName);

				//if (_fileNameOriginal == _grfSource.FileName) {
				//	_editor._grfLoadingSettings.FileName = _editor._grfHolder.FileName;
				//	_editor.Load();
				//
				//	// This is necessary because the encrypted GRFs add commands
				//	((GrfHolder) grfSource).Commands.ClearCommands();
				//}
				//else {
				//	// This is necessary because the encrypted GRFs add commands
				//	((GrfHolder) grfSource).Commands.ClearCommands();
				//	((GrfHolder) grfSource).Close();
				//}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _pathBrowserGrf1_TextChanged(object sender, EventArgs e) {
			if (File.Exists(_pathBrowserGrf1.Text)) {
				GrfEditorConfiguration.AppLastPath = _pathBrowserGrf1.Text;
				_fileSource = _pathBrowserGrf1.Text;
			}
		}

		private void _pathBrowserGrf2_TextChanged(object sender, EventArgs e) {
			if (File.Exists(_pathBrowserGrf2.Text)) {
				GrfEditorConfiguration.AppLastPath = _pathBrowserGrf2.Text;
				_fileSub = _pathBrowserGrf2.Text;
			}
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_originalGrf != _grfSource) {
					if (_grfSource != null)
						_grfSource.Dispose();
				}

				if (_grfAdd != null)
					_grfAdd.Dispose();
			}
		}
	}
}