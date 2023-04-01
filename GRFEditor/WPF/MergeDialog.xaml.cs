using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using GRF.Core;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for MergeDialog.xaml
	/// </summary>
	public partial class MergeDialogCustom : TkWindow, IDisposable {
		private readonly AsyncOperation _asyncOperation;
		private readonly EditorMainWindow _editor;
		private readonly GrfHolder _grfAdd = new GrfHolder();
		protected readonly GrfHolder _originalGrf;
		public bool RequiresReload = false;
		private bool _allowCancel;
		private bool _cancelThread;
		private string _fileAdd;

		protected string _fileNameOriginal;
		private string _fileSource;
		private GrfHolder _grfSource = new GrfHolder();

		public MergeDialogCustom(EditorMainWindow editor, GrfHolder grfSource) : base("Merge files", "convert.ico") {
			InitializeComponent();

			_asyncOperation = new AsyncOperation(_progressBarComponent);
			_editor = editor;
			_grfSource = grfSource;
			_originalGrf = grfSource;

			if (!grfSource.IsModified && !grfSource.IsNewGrf)
				_pathBrowserOldGrf.Text = grfSource.FileName;

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
					WindowProvider.ShowDialog("The current GRF contains errors and cannot be merged.");
					return;
				}
			}

			if (_grfSource.IsModified) {
				WindowProvider.ShowDialog("The current GRF has been modified, you must save it first.");
				return;
			}

			if (_grfSource.IsBusy) {
				WindowProvider.ShowDialog("The current GRF is saving, please wait for the operation to finish first.");
				return;
			}

			if (_grfSource.Header.FoundErrors) {
				WindowProvider.ShowDialog("The current GRF contains errors and cannot be merged.");
				return;
			}

			if (_fileAdd == null) {
				WindowProvider.ShowDialog("Drop a GRF in the right rectangle first.");
				return;
			}

			_grfAdd.Close();
			_grfAdd.Open(_fileAdd);

			if (_grfAdd.IsModified) {
				WindowProvider.ShowDialog("The added GRF has been modified, you must save it first.");
				return;
			}

			if (_grfAdd.IsBusy) {
				WindowProvider.ShowDialog("The added GRF is saving, please wait for the operation to finish first.");
				return;
			}

			if (_grfAdd.Header.FoundErrors) {
				WindowProvider.ShowDialog("The added GRF contains errors and cannot be merged.");
				return;
			}

			if (_grfAdd.FileName == _grfSource.FileName) {
				WindowProvider.ShowDialog("Trying to merge the same GRF into itself.");
				return;
			}

			_buttonOK.Dispatch(p => p.IsEnabled = false);
			_pathBrowserNewGrf.Dispatch(p => p.IsEnabled = false);
			_pathBrowserOldGrf.Dispatch(p => p.IsEnabled = false);
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _grfSource.QuickMerge(_grfAdd), _grfSource, 200, _grfSource), _syncFinished);
		}

		private void _syncFinished(object grfSource) {
			_pathBrowserNewGrf.Dispatch(p => p.IsEnabled = true);
			_pathBrowserOldGrf.Dispatch(p => p.IsEnabled = true);
			_buttonOK.Dispatch(p => p.IsEnabled = true);

			if (_fileNameOriginal == _grfSource.FileName) {
				_editor._grfLoadingSettings.FileName = _editor._grfHolder.FileName;
				_editor.Load();

				// This is necessary because the encrypted GRFs add commands
				((GrfHolder)grfSource).Commands.ClearCommands();
			}
			else {
				// This is necessary because the encrypted GRFs add commands
				((GrfHolder)grfSource).Commands.ClearCommands();
				((GrfHolder) grfSource).Close();
			}
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _pathBrowserOldGrf_TextChanged(object sender, EventArgs e) {
			if (File.Exists(_pathBrowserOldGrf.Text)) {
				GrfEditorConfiguration.AppLastPath = _pathBrowserOldGrf.Text;
				_fileSource = _pathBrowserOldGrf.Text;
			}
		}

		private void _pathBrowserNewGrf_TextChanged(object sender, EventArgs e) {
			if (File.Exists(_pathBrowserNewGrf.Text)) {
				GrfEditorConfiguration.AppLastPath = _pathBrowserNewGrf.Text;
				_fileAdd = _pathBrowserNewGrf.Text;
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