using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using ColorPicker.Sliders;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.StrFormat;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewStr.xaml
	/// </summary>
	public partial class PreviewStr : FilePreviewTab, IDisposable {
		private readonly ManualResetEvent _animationThreadHandle = new ManualResetEvent(false);
		private bool _isRunning = true;
		private bool _threadIsEnabled;
		private Str _str;
		private readonly Stopwatch _watch = new Stopwatch();
		private readonly object _lockAnimation = new object();
		private int _frameIndex;
		private float _animationSpeed = 30f;

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

		public Action<Brush> BackgroundBrushFunction {
			get {
				return v => this.Dispatch(delegate {
					if (!IsVisible)
						return;

					_viewport.Update();
				});
			}
		}

		public PreviewStr() {
			InitializeComponent();

			_sliderAnimation.ValueChanged += new SliderGradient.GradientPickerEventHandler(_sliderAnimation_ValueChanged);

			Dispatcher.ShutdownStarted += delegate {
				_isRunning = false;
				_enableAnimationThread = true;
			};

			IsVisibleChanged += new DependencyPropertyChangedEventHandler(_previewStr_IsVisibleChanged);
			UIPanelPreviewBackgroundStrPick(_qcsBackground);
			_qcsBackground.ColorBrush = GrfEditorConfiguration.UIPanelPreviewBackgroundStr;

			Binder.Bind(_checkBoxShowGrid, () => GrfEditorConfiguration.StrEditorShowGrid, () => _viewport.Update(), false);
			WpfUtils.AddMouseInOutEffectsBox(_checkBoxShowGrid);

			new Thread(_animationThread) { Name = "GrfEditor - Str animation update thread" }.Start();
		}

		private void UIPanelPreviewBackgroundStrPick(QuickColorSelector selector) {
			selector.PreviewColorChanged += _onSelectorOnPreviewStrColorChanged;
			selector.ColorChanged += _onSelectorOnPreviewStrColorChanged;
		}

		private void _onSelectorOnPreviewStrColorChanged(object sender, Color value) {
			if (GrfEditorConfiguration.UIPanelPreviewBackgroundStr is SolidColorBrush) {
				if (((SolidColorBrush)GrfEditorConfiguration.UIPanelPreviewBackgroundStr).Color == value)
					return;
			}

			GrfEditorConfiguration.UIPanelPreviewBackgroundStr = new SolidColorBrush(value);
			GrfEditorConfiguration.StrEditorBackgroundColorQuick.SetNull();
			BackgroundBrushFunction(GrfEditorConfiguration.UIPanelPreviewBackgroundStr);
		}

		private void _previewStr_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (IsVisible) {
				if (_playAnimation.IsPressed) {
					_enableAnimationThread = true;
				}
			}
		}

		private void _sliderAnimation_ValueChanged(object sender, double value) {
			try {
				if (_str.KeyFrameCount > 0) {
					_enableAnimationThread = false;
					_playAnimation.IsPressed = false;

					int v = (int)Math.Round((value * _str.MaxKeyFrame), MidpointRounding.AwayFromZero);

					_sliderAnimation.SetPosition((float)v / Math.Max(_str.MaxKeyFrame, 1), true);

					if (v == _frameIndex)
						return;

					_sliderPosition.Content = "Frame: " + v + " / " + _str.MaxKeyFrame;
					_frameIndex = v;

					this.Dispatch(delegate {
						_viewport.FrameIndex = _frameIndex;
						_viewport.Update();
					});
				}
			}
			catch {
			}
		}

		protected override void _load(FileEntry entry) {
			//_meshesDrawer.Dispatch(p => p.Clear());
			_enableAnimationThread = false;

			_str = new Str(entry.GetDecompressedData());

			this.Dispatch(delegate {
				try {
					_sliderPosition.Content = "Frame: 0 / " + _str.MaxKeyFrame;
					_sliderAnimation.SetPosition(0, true);
					_frameIndex = 0;

					if (_playAnimation.IsPressed) {
						_enableAnimationThread = true;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});

			if (_isCancelRequired()) return;

			_labelHeader.Dispatch(p => p.Content = "Str preview : " + Path.GetFileName(entry.RelativePath));

			if (_isCancelRequired()) return;

			_animationSpeed = _str.Fps;

			if (_animationSpeed != 0) {
				_animationSpeed = 1000 / _animationSpeed;
			}
			else {
				_animationSpeed = 1000;
			}

			// Render meshes
			this.Dispatch(delegate {
				_viewport.Load(_str, entry.RelativePath, _grfData);
			});
		}

		private void _animationThread() {
			while (true) {
				if (!_isRunning)
					return;

				if (_str != null && _str.KeyFrameCount > 0) {
					_watch.Reset();
					_watch.Start();

					lock (_lockAnimation) {
						_frameIndex++;

						if (_frameIndex >= _str.KeyFrameCount) {
							_frameIndex = 0;
						}

						Stopwatch watch = new Stopwatch();

						_sliderPosition.Dispatch(delegate {
							try {
								if (_frameIndex > _str.MaxKeyFrame) {
									return;
								}

								_sliderAnimation.SetPosition((float)_frameIndex / Math.Max(_str.MaxKeyFrame, 1), true);
								_sliderPosition.Content = "Frame: " + _frameIndex + " / " + _str.MaxKeyFrame;
							}
							catch {
								_enableAnimationThread = false;
							}
						});

						this.Dispatch(delegate {
							try {
								watch.Reset();
								watch.Start();
								_viewport.FrameIndex = _frameIndex;
								_viewport.Update();
								watch.Stop();

								if (!IsVisible)
									_enableAnimationThread = false;
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
								_enableAnimationThread = false;
							}
						});

						//Console.WriteLine("Time spent (" + _frameIndex + "): " + watch.ElapsedMilliseconds);
					}

					_watch.Stop();

					int delay = (int)(_animationSpeed - _watch.ElapsedMilliseconds);
					delay = delay < 0 ? 0 : delay;

					if (delay <= 0) {
						// Too fast! Decrease the animation speed
						//_animationSpeed *= 2;
					}

					if (delay < 20) {
						delay = 20;	// Going any lower would freeze the computer
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

		private void _playAnimation_Click(object sender, RoutedEventArgs e) {
			_playAnimation.IsPressed = !_playAnimation.IsPressed;

			if (_playAnimation.IsPressed) {
				_enableAnimationThread = true;
			}
			else {
				_enableAnimationThread = false;
			}
		}

		#region IDisposable members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~PreviewStr() {
			Dispose(false);
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				if (_animationThreadHandle != null) {
					_animationThreadHandle.Close();
				}
			}
		}

		#endregion
	}
}
