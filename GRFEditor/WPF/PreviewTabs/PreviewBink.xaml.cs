using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorPicker;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using GRF.GrfSystem;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.Core.Services;
using GRFEditor.Tools.SpriteEditor;
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using TokeiLibrary.Paths;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewImage.xaml
	/// </summary>
	public partial class PreviewBink : FilePreviewTab {
		private readonly GrfImageWrapper _primaryImage = new GrfImageWrapper();
		private readonly GrfImageWrapper _spriteImage = new GrfImageWrapper();
		private string _bikTempPath;
		private Bink _bink;
		private bool _threadIsEnabled;
		private bool _isRunning = true;
		private int _animationSpeed;
		private readonly ManualResetEvent _animationThreadHandle = new ManualResetEvent(false);
		private readonly Stopwatch _watch = new Stopwatch();
		private readonly object _lockAnimation = new object();

		public PreviewBink() {
			InitializeComponent();

			_bikTempPath = TemporaryFilesManager.GetTemporaryFilePath(Process.GetCurrentProcess().Id + "_vid.bik");

			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			VirtualFileDataObject.SetDraggable(_imagePreview, _primaryImage);
			WpfUtilities.AddFocus(_tbEase);
			ImageHelper.SetupZoomUI(_imagePreview, 6f, _gpEase, _tbEase, () => GrfEditorConfiguration.PreviewImageZoom, v => GrfEditorConfiguration.PreviewImageZoom = v);
			ErrorPanel = _errorPanel;
			_sliderAnimation.ValueChanged += _sliderAnimation_ValueChanged;
			IsVisibleChanged += _previewBik_IsVisibleChanged;
			new Thread(_animationThread) { Name = "GrfEditor - BIK animation update thread" }.Start();
		}

		private void _previewBik_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (IsVisible) {
				if (_playAnimation.IsPressed) {
					_enableAnimationThread = true;
				}
			}
		}

		private void _sliderAnimation_ValueChanged(object sender, ValueEventArgs args) {
			try {
				if (_bink.FrameCount > 0) {
					_enableAnimationThread = false;
					_playAnimation.IsPressed = false;

					int v = (int)Math.Round((args.Value * (_bink.FrameCount - 1)), MidpointRounding.AwayFromZero);
					_sliderAnimation.SetPosition((float)v / (_bink.FrameCount - 1), true);
					_sliderPosition.Text = v + "";

					if (v == _bink.CurrentFrame)
						return;

					var image = _bink.GetImage(v);
					_imagePreview.Source = image?.Cast<BitmapSource>();
				}
			}
			catch {
			}
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			var success = _tryLoadBik(entry);
			if (!success) return;

			_displayImage(entry, 0);
		}

		private void _displayImage(FileEntry entry, int startFrame) {
			this.Dispatch(delegate {
				_primaryImage.Image = _bink.GetImage(startFrame);
				_primaryImage.ExportFileName = Path.GetFileNameWithoutExtension(entry.RelativePath);
				_imagePreview.Source = _primaryImage.Image?.Cast<BitmapSource>();
				ImageHelper.UpdateZoom(_imagePreview, GrfEditorConfiguration.PreviewImageZoom);
			});
		}

		private bool _tryLoadBik(FileEntry entry) {
			try {
				_enableAnimationThread = false;

				var data = entry.GetDecompressedData();

				_bink?.Dispose();
				File.Delete(_bikTempPath);
				File.WriteAllBytes(_bikTempPath, data);

				_bink = new Bink(_bikTempPath);

				this.Dispatch(delegate {
					if (_playAnimation.IsPressed) {
						_enableAnimationThread = true;
					}

					_sliderPosition.Text = "0";
					_sliderPositionTotal.Text = (_bink.FrameCount - 1) + "";
					_sliderAnimation.SetPosition(0, true);
				});

				_animationSpeed = (int)Math.Round(_bink.Fps);

				if (_animationSpeed != 0) {
					_animationSpeed = 1000 / _animationSpeed;
				}
				else {
					_animationSpeed = 1000;
				}

				return true;
			}
			catch (GrfException err) {
				if (err == GrfExceptions.__CorruptedOrEncryptedEntry || err == GrfExceptions.__GravityEncryptedFile) {
					_imagePreview.Dispatch(p => p.Source = null);
					throw;
				}

				if (err == GrfExceptions.__ContainerBusy) {
					_imagePreview.Dispatch(p => p.Source = null);
					return false;
				}

				throw;
			}
		}

		private void _setupUI(FileEntry entry) {
			string fileName = entry.RelativePath;

			this.Dispatch(delegate {
				_labelHeader.Text = "Video preview: " + entry.DisplayRelativePath;
			});
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);

		private void _playAnimation_Click(object sender, RoutedEventArgs e) {
			if (_bink != null && _playAnimation.IsVisible) {
				_playAnimation.IsPressed = !_playAnimation.IsPressed;

				if (_playAnimation.IsPressed) {
					_bink.GetImage((int)(_sliderAnimation.Position * (_bink.FrameCount - 1)));
					_enableAnimationThread = true;
				}
				else {
					_enableAnimationThread = false;
				}
			}
		}

		private bool _enableAnimationThread {
			set {
				if (value) {
					if (!_threadIsEnabled)
						_animationThreadHandle.Set();
				}
				else {
					if (_threadIsEnabled) {
						_threadIsEnabled = false;
						_animationThreadHandle.Reset();
					}
				}
			}
		}

		private void _animationThread() {
			while (true) {
				if (!_isRunning)
					return;

				if (_bink != null && _bink.FrameCount > 0) {
					_watch.Reset();
					_watch.Start();

					lock (_lockAnimation) {
						var image = _bink.GetNextFrame(out _);
						
						_sliderPosition.Dispatch(delegate {
							_sliderAnimation.SetPosition((float)_bink.CurrentFrame / (_bink.FrameCount - 1), true);
							_sliderPosition.Text = _bink.CurrentFrame + "";
							_imagePreview.Source = image?.Cast<BitmapSource>();
						});

						this.Dispatch(delegate {
							try {
								if (!IsVisible)
									_enableAnimationThread = false;
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
								_enableAnimationThread = false;
							}
						});
					}

					_watch.Stop();

					int delay = (int)(_animationSpeed - _watch.ElapsedMilliseconds);
					delay = delay < 0 ? 0 : delay;

					if (delay < 20) {
						delay = 20; // Going any lower would freeze the computer
					}

					Thread.Sleep(delay);
				}
				else {
					_threadIsEnabled = false;
				}

				if (!_threadIsEnabled) {
					_animationThreadHandle.WaitOne();

					if (!_threadIsEnabled)
						_threadIsEnabled = true;
				}
			}
		}
	}
}