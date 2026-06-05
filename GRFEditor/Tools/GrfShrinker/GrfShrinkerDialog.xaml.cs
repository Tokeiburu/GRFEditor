using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;
using Utilities.Extension;
using OpeningService = Utilities.Services.OpeningService;

namespace GRFEditor.Tools.GrfShrinker {
	/// <summary>
	/// Interaction logic for GrfShrinkerDialog.xaml
	/// </summary>
	public partial class GrfShrinkerDialog : TkWindow {
		private readonly AsyncOperation _asyncOperation;
		private readonly GrfHolder _grfHolder;

		public GrfShrinkerDialog(GrfHolder grfHolder)
			: base("Grf validation", "validity.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			_grfHolder = grfHolder;
			InitializeComponent();

			_asyncOperation = new AsyncOperation(_progressBar);

			_progressBar.SetSpecialState(TkProgressBar.ProgressStatus.Finished);

			Owner = WpfUtilities.TopWindow;
		}

		protected override void OnClosing(CancelEventArgs e) {
			_asyncOperation.Cancel();
			base.OnClosing(e);
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonStart_Click(object sender, RoutedEventArgs e) {
			_asyncOperation.SetAndRunOperation(v => _startShrink(v));
		}

		private void _startShrink(ProgressObject progress) {
			try {
				progress.Init();

				if (_grfHolder.IsModified)
					throw new Exception("The GRF has been modified, you must save it first.");

				if (_grfHolder.IsBusy)
					throw new Exception("The GRF is saving, please wait for the operation to finish first.");

				if (_grfHolder.Header.FoundErrors)
					throw new Exception("The GRF contains errors and cannot be modified.");

				string output = _grfHolder.FileName;
				output = GrfPath.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "-compressed" + output.GetExtension());

				if (GrfEditorConfiguration.SH_UseLzmaCompression) {
					var oldCompression = Compression.CompressionAlgorithm;
					Compression.CompressionAlgorithm = Compression.LzmaCompression;

					try {
						_grfHolder.RepackAs(output);
					}
					finally {
						Compression.CompressionAlgorithm = oldCompression;
					}
				}

				if (GrfEditorConfiguration.SH_DowngradeTextures) {

				}
			}
			catch (OperationCanceledException) {
				progress.Cancel();
			}
			catch (Exception err) {
				progress.Cancel();
				ErrorHandler.HandleException(err);
			}
			finally {
				progress.Finish();
			}
		}
	}
}