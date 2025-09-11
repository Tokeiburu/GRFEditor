using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using GRF.Threading;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;

namespace GrfToWpfBridge {
	/// <summary>
	/// Interaction logic for TaskDialog.xaml
	/// </summary>
	public partial class TaskDialog : TkWindow, IProgress {
		private readonly AsyncOperation _async;
		private bool _enableClosing;

		public void ShowFooter(bool value) {
			_gridFooter.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
		}

		public TaskDialog() {
			InitializeComponent();
		}

		public TaskDialog(string title, string icon, string description) : base(title, icon) {
			InitializeComponent();
			_tbInfo.Text = description;
			_async = new AsyncOperation(_progressBar);
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;
		}

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		protected override void OnClosing(CancelEventArgs e) {
			if (!_enableClosing) {
				IsCancelling = true;
				e.Cancel = true;
				return;
			}

			base.OnClosing(e);
		}

		public void Start(Action action, Func<float> progress) {
			_async.SetAndRunOperation(new GrfThread(() => _start(action, progress), this, 200, null, true, true));
		}

		public void Start(Action<Func<bool>> action, Func<float> progress) {
			_async.SetAndRunOperation(new GrfThread(() => _start(action, progress), this, 200, null, true, true));
		}

		public void SetUpdate(string message) {
			this.Dispatch(p => p._tbInfo.Text = message);
		}

		private void _start(Action<Func<bool>> action, Func<float> progress) {
			Progress = -1;

			var grfThread = new GrfThread(() => action(() => IsCancelling), this, 200, null, true, true);

			grfThread.Finished += delegate {
				Progress = 100f;
				_enableClosing = true;
				this.Dispatch(Close);
			};
			grfThread.Start();

			while (progress() < 100f && grfThread.IsRunning) {
				Thread.Sleep(200);
				var prog = progress();
				Progress = prog <= 0 ? -1 : prog;

				if (IsCancelling) {
					grfThread.Cancel();
				}
			}

			Progress = 100f;
			_enableClosing = true;
			this.Dispatch(Close);
		}

		private void _start(Action action, Func<float> progress) {
			Progress = -1;

			var grfThread = new GrfThread(action, this, 200, null, true, true);

			grfThread.Finished += delegate {
				Progress = 100f;
				_enableClosing = true;
				this.Dispatch(Close);
			};
			grfThread.Start();

			while (progress() < 100f && grfThread.IsRunning) {
				Thread.Sleep(200);
				var prog = progress();
				Progress = prog <= 0 ? -1 : prog;

				if (IsCancelling) {
					grfThread.Cancel();
				}
			}

			Progress = 100f;
			_enableClosing = true;
			this.Dispatch(Close);
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}