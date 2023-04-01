using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.Graphics;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewRsm.xaml
	/// </summary>
	public partial class PreviewRsm : FilePreviewTab, IDisposable {
		private Matrix4 _modelRotation = Matrix4.Identity;
		private Rsm _rsm;
		private int _shader = -1;
		private readonly ManualResetEvent _animationThreadHandle = new ManualResetEvent(false);
		private readonly Stopwatch _watch = new Stopwatch();
		private bool _threadIsEnabled;
		private bool _isRunning = true;
		private int _animationPosition = 0;
		private readonly object _lockAnimation = new object();
		private float _animationSpeed = 30;

		public PreviewRsm() {
			InitializeComponent();
			_shader1.IsPressed = true;
			_shader = 1;

			_isInvisibleResult = () => _meshesDrawer.Dispatch(p => p.Visibility = Visibility.Hidden);

			_checkBoxRotateCamera.Checked += (e, a) => _meshesDrawer.SetCameraState(true, true, null);
			_checkBoxRotateCamera.Unchecked += (e, a) => _meshesDrawer.SetCameraState(false, null, null);
			_checkBoxUseGlobalLighting.Checked += (e, a) => _meshesDrawer.SetCameraState(null, null, true);
			_checkBoxUseGlobalLighting.Unchecked += (e, a) => _meshesDrawer.SetCameraState(null, null, false);
			_sliderAnimation.ValueChanged += new ColorPicker.Sliders.SliderGradient.GradientPickerEventHandler(_sliderAnimation_ValueChanged);
			WpfUtils.AddMouseInOutEffectsBox(_checkBoxRotateCamera, _checkBoxUseGlobalLighting);

			Dispatcher.ShutdownStarted += delegate {
				_isRunning = false;
				_enableAnimationThread = true;
			};

			this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(_previewRsm_IsVisibleChanged);
			new Thread(_animationThread) { Name = "GrfEditor - RSM2 animation update thread" }.Start();
		}

		private void _previewRsm_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (this.IsVisible) {
				if (_playAnimation.IsPressed) {
					_enableAnimationThread = true;
				}
			}
		}

		private void _sliderAnimation_ValueChanged(object sender, double value) {
			try {
				if (_rsm.AnimationLength > 0) {
					_enableAnimationThread = false;
					_playAnimation.IsPressed = false;

					int v = (int)Math.Round((value * _rsm.AnimationLength), MidpointRounding.AwayFromZero);

					if (v == _animationPosition)
						return;

					_sliderAnimation.SetPosition((float)v / _rsm.AnimationLength, true);
					_sliderPosition.Content = "Frame: " + v + " / " + _rsm.AnimationLength;
					_animationPosition = v;

					_meshesDrawer.Dispatch(delegate {
						_meshesDrawer.AddRsm2(_rsm, v, false);
					});
				}
			}
			catch {
			}
		}

		public Action<Brush> BackgroundBrushFunction {
			get {
				return v => _grid.Dispatch(p => _grid.Background = v);
			}
		}

		protected override void _load(FileEntry entry) {
			_meshesDrawer.Dispatch(p => p.Clear());
			_enableAnimationThread = false;

			_rsm = new Rsm(entry.GetDecompressedData());
			
			this.Dispatch(delegate {
				if (_rsm.Header.IsCompatibleWith(2, 0)) {
					_gridAnimation.Visibility = Visibility.Visible;
					_sliderPosition.Content = "Frame: 0 / " + _rsm.AnimationLength;
					_sliderAnimation.SetPosition(0, true);

					if (_playAnimation.IsPressed) {
						_enableAnimationThread = true;
					}
				}
				else {
					_gridAnimation.Visibility = Visibility.Collapsed;
				}
			});

			if (_rsm.Header.IsCompatibleWith(2, 0)) {
				if (_isCancelRequired()) return;

				_labelHeader.Dispatch(p => p.Content = "Model preview : " + Path.GetFileName(entry.RelativePath));

				_meshesDrawer.Dispatch(p => p.Update(_grfData, _isCancelRequired, 200));

				if (_isCancelRequired()) return;

				_animationSpeed = _rsm.FrameRatePerSecond;

				if (_animationSpeed != 0) {
					_animationSpeed = 1000 / _animationSpeed;
				}
				else {
					_animationSpeed = 1000;
				}

				//_modelRotation = Matrix4.Identity;
				//_modelRotation = Matrix4.RotateX(_modelRotation, ModelViewerHelper.ToRad(180));
				//_modelRotation = Matrix4.RotateY(_modelRotation, ModelViewerHelper.ToRad(180));

				//Dictionary<string, MeshRawData> allMeshData = _rsm.Compile(_modelRotation, _shader, 1);

				// Render meshes
				_meshesDrawer.Dispatch(p => p.AddRsm2(_rsm, 0, true));
			}
			else {
				_rsm.CalculateBoundingBox();

				if (_isCancelRequired()) return;

				_labelHeader.Dispatch(p => p.Content = "Model preview : " + Path.GetFileName(entry.RelativePath));

				_meshesDrawer.Dispatch(p => p.Update(_grfData, _isCancelRequired, Math.Max(Math.Max(_rsm.Box.Range[0], _rsm.Box.Range[1]), _rsm.Box.Range[2]) * 4));

				if (_isCancelRequired()) return;

				_modelRotation = Matrix4.Identity;
				//_modelRotation[0, 0] = -1;
				_modelRotation = Matrix4.RotateX(_modelRotation, ModelViewerHelper.ToRad(180));
				_modelRotation = Matrix4.RotateY(_modelRotation, ModelViewerHelper.ToRad(180));

				Dictionary<string, MeshRawData> allMeshData = _rsm.Compile(_modelRotation, _shader, 1);

				// Render meshes
				_meshesDrawer.Dispatch(p => p.AddObject(allMeshData.Values.ToList(), Matrix4.Identity));
			}

			if (_isCancelRequired()) return;

			_meshesDrawer.Dispatch(p => p.UpdateCamera());
			_meshesDrawer.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _shader1_Click(object sender, RoutedEventArgs e) {
			_shader1.IsPressed = !_shader1.IsPressed;
			_shader2.IsPressed = false;

			if (_shader1.IsPressed) {
				_shader = 1;
			}
			else {
				_shader = -1;
			}

			_oldEntry = null;
			_meshesDrawer.ResetCameraDistance(false);
			_meshesDrawer.ReactivateRotatingCamera(false);
			Update();
		}

		private void _shader2_Click(object sender, RoutedEventArgs e) {
			_shader1.IsPressed = false;
			_shader2.IsPressed = !_shader2.IsPressed;

			if (_shader2.IsPressed) {
				_shader = 2;
			}
			else {
				_shader = -1;
			}

			_oldEntry = null;
			_meshesDrawer.ResetCameraDistance(false);
			_meshesDrawer.ReactivateRotatingCamera(false);
			Update();
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

				if (_rsm != null && _rsm.Header.IsCompatibleWith(2, 0) && _rsm.AnimationLength > 0) {
					_watch.Reset();
					_watch.Start();

					lock (_lockAnimation) {
						_animationPosition++;

						if (_animationPosition >= _rsm.AnimationLength) {
							_animationPosition = 0;
						}

						Stopwatch watch = new Stopwatch();

						_sliderPosition.Dispatch(delegate {
							_sliderAnimation.SetPosition((float)_animationPosition / _rsm.AnimationLength, true);
							_sliderPosition.Content = "Frame: " + _animationPosition + " / " + _rsm.AnimationLength;
						});

						_meshesDrawer.Dispatch(delegate {
							try {
								watch.Reset();
								watch.Start();
								_meshesDrawer.AddRsm2(_rsm, _animationPosition, false);
								watch.Stop();

								if (!IsVisible)
									_enableAnimationThread = false;
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
								_enableAnimationThread = false;
							}
						});

						//Console.WriteLine("Time spent (" + _animationPosition + "): " + watch.ElapsedMilliseconds);
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

		#region IDisposable members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~PreviewRsm() {
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