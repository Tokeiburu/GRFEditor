using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ColorPicker.Sliders;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.GatFormat;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.MapRenderers;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;
using Binder = GrfToWpfBridge.Binder;
using Button = System.Windows.Controls.Button;
using RenderOptions = GRFEditor.OpenGL.WPF.RenderOptions;
using WindowState = System.Windows.WindowState;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewRsm.xaml
	/// </summary>
	public partial class PreviewRsm : FilePreviewTab {
		private Rsm _rsm;
		private int _shading = -1;
		private readonly ManualResetEvent _animationThreadHandle = new ManualResetEvent(false);
		private readonly Stopwatch _watch = new Stopwatch();
		private bool _threadIsEnabled;
		private bool _isRunning = true;
		private int _animationPosition = 0;
		private readonly object _lockAnimation = new object();
		private float _animationSpeed = 30;
		private RendererLoadRequest _currentRequest;
		private readonly Stopwatch _rsm1Watch = new Stopwatch();
		private long _elapsedOffset;

		public PreviewRsm() {
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_reloadOptions();
			_shader2.IsChecked = true;
			_shading = 2;

			Binder.Bind(_checkBoxUseGlobalLighting, () => GrfEditorConfiguration.MapRendererGlobalLighting, v => GrfEditorConfiguration.MapRendererGlobalLighting = v, delegate {
				_viewport.LightAmbient = GrfEditorConfiguration.MapRendererGlobalLighting ? new Vector3(1f) : new Vector3(0.5f);
			}, true);

			ApplicationShortcut.Link(ApplicationShortcut.FromString("F11", "PreviewRsm.FullScreen"), _fullScreen, EditorMainWindow.Instance);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("Space", "PreviewRsm.PlayPause"), () => _playAnimation_Click(null, null), this);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("C", "PreviewRsm.DebugCopyCamera"), () => _viewport.Camera.Copy(), this);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("V", "PreviewRsm.DebugPasteCamera"), () => _viewport.Camera.Paste(), this);

			_viewport.RotateCamera = GrfEditorConfiguration.MapRendererRotateCamera;
			Binder.Bind(_checkBoxRotateCamera, () => GrfEditorConfiguration.MapRendererRotateCamera, v => GrfEditorConfiguration.MapRendererRotateCamera = v, delegate {
				if (GrfEditorConfiguration.MapRendererRotateCamera) {
					_viewport.RotateCamera = true;
					_viewport.IsRotatingCamera = true;
				}
				else {
					_viewport.RotateCamera = false;
				}
			});
			Binder.Bind(_checkBoxEnableMipmap, () => GrfEditorConfiguration.MapRendererMipmap, v => GrfEditorConfiguration.MapRendererMipmap = v, () => Reload(false, false, true));

			_qcsBackground.ColorBrush = GrfEditorConfiguration.UIPanelPreviewBackgroundMap;
			_qcsBackground.PreviewColorChanged += (s, value) => {
				GrfEditorConfiguration.UIPanelPreviewBackgroundMap = new SolidColorBrush(value);
			};
			_qcsBackground.ColorChanged += (s, value) => {
				GrfEditorConfiguration.UIPanelPreviewBackgroundMap = new SolidColorBrush(value);
			};

			_sliderAnimation.ValueChanged += new SliderGradient.GradientPickerEventHandler(_sliderAnimation_ValueChanged);
			WpfUtils.AddMouseInOutEffectsBox(_checkBoxRotateCamera, _checkBoxUseGlobalLighting, _checkBoxEnableMipmap);
			
			IsVisibleChanged += new DependencyPropertyChangedEventHandler(_previewRsm_IsVisibleChanged);
			new Thread(_animationThread) { Name = "GrfEditor - RSM2 animation update thread" }.Start();

			_viewport.Loader.Loaded += _dataLoaded;

			_buttonLighting.Click += delegate {
				var dialog = new LightingOptions();
				dialog.Load(_currentRequest);
				_openDialog(dialog, _buttonLighting);
			};

			_buttonRenderOptions.Click += delegate {
				var dialog = new RenderOptions(_viewport);
				dialog.Load(this);
				_openDialog(dialog, _buttonRenderOptions);
			};

			Dispatcher.ShutdownStarted += delegate {
				_isRunning = false;
				_enableAnimationThread = true;
			};

			_buttonShading.ContextMenu.PlacementTarget = _buttonShading;
			_buttonShading.ContextMenu.Placement = PlacementMode.Bottom;

			_buttonShading.Click += delegate {
				_buttonShading.IsEnabled = false;
				_buttonShading.ContextMenu.IsOpen = true;
			};

			_buttonShading.ContextMenu.Closed += delegate {
				_buttonShading.IsEnabled = true;
			};

			_shader1.StaysOpenOnClick = true;
			_shader2.StaysOpenOnClick = true;

			_buttonSkyMap.Click += delegate {
				var dialog = new CloudEditDialog();
				dialog.Init(_viewport);
				dialog.Show();
				dialog.Owner = EditorMainWindow.Instance;
				dialog.ShowInTaskbar = false;
				_buttonSkyMap.IsEnabled = false;
				dialog.Closed += delegate {
					_buttonSkyMap.IsEnabled = true;
				};
			};

			_buttonMinimap.Click += delegate {
				CreateMinimap();
			};
		}

		public void CreateMinimap() {
			try {
				OpenGL.WPF.OpenGLViewport nViewport = new OpenGL.WPF.OpenGLViewport(0);
				nViewport.RenderOptions = _viewport.RenderOptions;
				nViewport.Load(_viewport._request);

				var current = _viewport._host.Child;
				var newPrimary = nViewport._host.Child;
				nViewport._host.Child = null;
				_viewport._host.Child = newPrimary;

				var gnd = _viewport._request.Gnd;

				float w = 512.0f / Math.Max(gnd.Width, gnd.Height);

				nViewport._primary.Width = (int)(w * gnd.Width);
				nViewport._primary.Height = (int)(w * gnd.Height);
				_viewport.EnableRenderThread = false;

				nViewport.Camera.LookAt.X = gnd.Width * 5f;
				nViewport.Camera.LookAt.Y = 0 * 5f;
				nViewport.Camera.LookAt.Z = gnd.Height * 5f + 10f;

				nViewport.Camera.Position = new Vector3(0);
				int removeEdge = GrfEditorConfiguration.MapRendererMinimapBorderCut;
				nViewport.Camera.Distance = Math.Max(gnd.Header.Height - removeEdge, gnd.Header.Width - removeEdge) * 10f;
				float ratio = gnd.Height < gnd.Width ? (float)gnd.Height / gnd.Width : 1f;
				nViewport.Camera.Distance *= ratio;
				_viewport.RenderOptions.MinimapMode = true;

				nViewport.Camera.AngleX_Degree = 0;
				nViewport.Camera.AngleY_Degree = 90;

				if (_viewport._request.Gat == null) {
					_viewport._request.Gat = new Gat(ResourceManager.GetData(_viewport._request.Resource + ".gat"));
				}

				var gatRenderer = new GatRenderer(_viewport._request, nViewport.Shader_simple, _viewport._request.Gat, _viewport._request.Gnd);
				nViewport.Renderers.Add(gatRenderer);

				MinimapDialog diag = new MinimapDialog();
				diag.Owner = WpfUtilities.TopWindow;
				diag.Init(nViewport, Path.GetFileNameWithoutExtension(_viewport._request.Resource));
				diag.Closed += delegate {
					nViewport.UnloadAndStopViewport();
					nViewport = null;

					_viewport.RenderOptions.MinimapMode = false;
					_viewport._host.Child = current;
					newPrimary.Dispose();

					_viewport.EnableRenderThread = true;
				};
				diag.ShowDialog();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _fullScreen() {
			if (_viewportGrid.Children.Count == 0)
				return;

			Window window = new Window();
			_viewportGrid.Children.Clear();
			window.Content = _viewport;
			window.Loaded += delegate {
				window.Topmost = false;
			};
			window.Title = "Preview";
			window.ResizeMode = ResizeMode.NoResize;
			window.Topmost = true;
			window.WindowStyle = WindowStyle.None;
			window.WindowState = WindowState.Maximized;
			window.Show();
			window.Closed += delegate {
				window.Content = null;
				_viewportGrid.Children.Add(_viewport);
			};
			window.KeyDown += (s, e) => {
				if (e.Key == Key.Escape) {
					window.Close();
				}
				else if (e.Key == Key.F11) {
					window.Close();
				}
			};
			window.StateChanged += delegate {

			};
		}

		private void _openDialog(Window dialog, Button button) {
			dialog.WindowStyle = WindowStyle.None;
			var content = dialog.Content;

			Border border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
			dialog.Content = null;
			border.Child = content as UIElement;
			dialog.Content = border;
			dialog.Owner = null;
			dialog.ShowInTaskbar = false;
			dialog.ResizeMode = ResizeMode.NoResize;
			dialog.SnapsToDevicePixels = true;
			
			EditorMainWindow.Instance.Activated += delegate {
				dialog.Close();
			};

			Point p = button.PointToScreen(new Point(0, 0));
			var par = WpfUtilities.FindParentControl<Window>(button);

			dialog.Loaded += delegate {
				button.IsEnabled = false;
				dialog.WindowStartupLocation = WindowStartupLocation.Manual;

				int dpiXI = (int)typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
				double dpiX = dpiXI;
				double ratio = dpiX / 96;

				p.X /= ratio;
				p.Y /= ratio;

				// The dialog's position scales with the DPI
				dialog.Left = p.X;
				dialog.Top = p.Y + button.ActualHeight;

				if (dialog.Left < 0) {
					dialog.Left = 0;
				}

				if (dialog.Top + dialog.Height > SystemParameters.WorkArea.Bottom) {
					dialog.Top = p.Y - dialog.ActualHeight;
				}

				if (dialog.Left + dialog.Width > SystemParameters.WorkArea.Right) {
					dialog.Left = p.X - dialog.ActualWidth;
				}

				if (dialog.Top < 0) {
					dialog.Top = 0;
				}

				dialog.Owner = par;
			};

			dialog.Closed += delegate {
				button.IsEnabled = true;
			};

			dialog.Show();
		}

		private void _dataLoaded(RendererLoadRequest request) {
			this.Dispatch(delegate {
				_currentRequest = request;
			});
		}

		public override void Load(GrfHolder grfData, FileEntry entry) {
			base.Load(grfData, entry);

			if (entry.RelativePath.IsExtension(".rsm", ".rsm2")) {
				((TabItem)Parent).Header = "Model preview";
			}
			else {
				((TabItem)Parent).Header = "Map preview";
			}
		}

		public void ReloadViewport() {
			var camera = _viewport.Camera;
			var renderOptions = _viewport.RenderOptions;
			_viewport.UnloadAndStopViewport();
			_viewportGrid.Children.Clear();
			_viewport = new OpenGL.WPF.OpenGLViewport(GrfEditorConfiguration.MapRenderEnableFSAA ? 8 : 0);
			_viewport.Camera = camera;
			_viewport.RenderOptions = renderOptions;
			camera.Viewport = _viewport;
			_viewportGrid.Children.Add(_viewport);
			_load(_entry);
		}

		public void Reload(bool reloadAll = true, bool reloadGnd = false, bool reloadTexture = false) {
			_reloadOptions();

			if (reloadAll) {
				_oldEntry = null;
				_viewport.ResetCameraDistance = false;
				_viewport.ResetCameraPosition = false;
				Update();
			}
			else {
				if (reloadGnd) {
					foreach (var renderer in _viewport.Renderers.OfType<GndRenderer>()) {
						_viewport._primary.MakeCurrent();
						renderer.Shader.Use();
						renderer.Shader.SetFloat("showLightmap", _viewport.RenderOptions.Lightmap ? 1.0f : 0.0f);
						renderer.Shader.SetFloat("showShadowmap", _viewport.RenderOptions.Shadowmap ? 1.0f : 0.0f);
						renderer.Shader.SetFloat("enableCullFace", _viewport.RenderOptions.EnableFaceCulling ? 1.0f : 0.0f);
					}
				}

				if (reloadTexture) {
					_viewport._primary.MakeCurrent();

					foreach (var texture in TextureManager.BufferedTextures.Values) {
						texture.Item1.ReloadParameter();
					}
				}
			}
		}

		private void _reloadOptions() {
			Texture.EnableMipmap = GrfEditorConfiguration.MapRendererMipmap;
			_viewport.RenderOptions.Water = GrfEditorConfiguration.MapRendererWater;
			_viewport.RenderOptions.Ground = GrfEditorConfiguration.MapRendererGround;
			_viewport.RenderOptions.Objects = GrfEditorConfiguration.MapRendererObjects;
			_viewport.RenderOptions.AnimateMap = GrfEditorConfiguration.MapRendererAnimateMap;
			_viewport.RenderOptions.Lightmap = GrfEditorConfiguration.MapRendererLightmap;
			_viewport.RenderOptions.Shadowmap = GrfEditorConfiguration.MapRendererShadowmap;
			_viewport.RenderOptions.ShowFps = GrfEditorConfiguration.MapRendererShowFps;
			_viewport.RenderOptions.ShowBlackTiles = GrfEditorConfiguration.MapRendererTileUp;
			_viewport.RenderOptions.LubEffect = GrfEditorConfiguration.MapRendererRenderLub;
			_viewport.RenderOptions.ViewStickToGround = GrfEditorConfiguration.MapRendererStickToGround;
			_viewport.RenderOptions.RenderSkymapFeature = GrfEditorConfiguration.MapRenderSkyMap;
			_viewport.RenderOptions.SmoothCamera = GrfEditorConfiguration.MapRenderSmoothCamera;
			_viewport.RenderOptions.FpsCap = GrfEditorConfiguration.MapRenderUnlimitedFps ? -1 : GrfEditorConfiguration.MapRenderFpsCap;
			_viewport.RenderOptions.EnableFaceCulling = GrfEditorConfiguration.MapRenderEnableFaceCulling;
			//_viewport.RenderOptions.UseClientPov = GrfEditorConfiguration.MapRendererClientPov;

			if (MapRenderer.FpsTextBlock == null)
				MapRenderer.FpsTextBlock = _tbFps;

			_tbFps.Visibility = _viewport.RenderOptions.ShowFps ? Visibility.Visible : Visibility.Collapsed;
		}

		private void _previewRsm_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (IsVisible) {
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
					_sliderAnimation.SetPosition((float)v / _rsm.AnimationLength, true);
					_sliderPosition.Text = v + "";

					if (v == _animationPosition)
						return;

					_animationPosition = v;
					_rsm.SetAnimationIndex(v, v);
					_rsm.Dirty();
				}
			}
			catch {
			}
		}

		protected override void _load(FileEntry entry) {
			_enableAnimationThread = false;

			if (entry.RelativePath.IsExtension(".rsm", ".rsm2")) {
				Rsm.ForceShadeType = _shading;
				_rsm = new Rsm(entry.GetDecompressedData());
				_viewport.Loader.AddRequest(new RendererLoadRequest { IsMap = false, Rsm = _rsm, CancelRequired = _isCancelRequired, Resource = entry.RelativePath, Context = _viewport });

				this.Dispatch(delegate {
					_labelHeader.Text = "Model preview : " + entry.DisplayRelativePath;
					_buttonShading.Visibility = Visibility.Visible;
					_buttonLighting.Visibility = Visibility.Collapsed;
					_buttonSkyMap.Visibility = Visibility.Collapsed;
					_buttonMinimap.Visibility = Visibility.Collapsed;
					_checkBoxUseGlobalLighting.IsEnabled = true;

					if (_rsm.AnimationLength > 0) {
						_gridAnimation.Visibility = Visibility.Visible;
						_animationPosition = 0;
						_sliderPosition.Text = "0";
						_sliderPositionTotal.Text = _rsm.AnimationLength + "";
						_sliderAnimation.SetPosition(0, true);

						if (_playAnimation.IsPressed) {
							_enableAnimationThread = true;
						}

						_animationSpeed = _rsm.FramesPerSecond;

						if (_animationSpeed != 0) {
							_animationSpeed = 1000 / _animationSpeed;
						}
						else {
							_animationSpeed = 1000;
						}
					}
					else {
						_gridAnimation.Visibility = Visibility.Collapsed;
					}
				});
			}
			else {
				// Map loading!
				string mapName = GrfPath.Combine(Path.GetDirectoryName(entry.RelativePath), Path.GetFileNameWithoutExtension(entry.RelativePath));

				Rsm.ForceShadeType = -1;
				_viewport.Loader.AddRequest(new RendererLoadRequest { IsMap = true, Resource = mapName, CancelRequired = _isCancelRequired, Context = _viewport });

				this.Dispatch(delegate {
					_labelHeader.Text = "Map preview : " + entry.DisplayRelativePath;
					_buttonShading.Visibility = Visibility.Collapsed;
					_buttonLighting.Visibility = Visibility.Visible;
					_buttonSkyMap.Visibility = Visibility.Visible;
					_buttonMinimap.Visibility = Visibility.Visible;
					_checkBoxUseGlobalLighting.IsEnabled = false;

					_gridAnimation.Visibility = Visibility.Collapsed;
				});
			}
		}

		private void _shader1_Click(object sender, RoutedEventArgs e) {
			_shader2.IsChecked = false;

			if (_shader1.IsChecked) {
				_shading = 1;
			}
			else {
				_shading = -1;
			}

			_oldEntry = null;
			_viewport.ResetCameraDistance = false;
			_viewport.ResetCameraPosition = false;
			Update();
		}

		private void _shader2_Click(object sender, RoutedEventArgs e) {
			_shader1.IsChecked = false;

			if (_shader2.IsChecked) {
				_shading = 2;
			}
			else {
				_shading = -1;
			}

			_oldEntry = null;
			_viewport.ResetCameraDistance = false;
			_viewport.ResetCameraPosition = false;
			Update();
		}

		private void _playAnimation_Click(object sender, RoutedEventArgs e) {
			if (_rsm != null && _playAnimation.IsVisible) {
				_playAnimation.IsPressed = !_playAnimation.IsPressed;

				if (_playAnimation.IsPressed) {
					_elapsedOffset = (long)(_animationPosition * 1000d / Math.Max(1d, _rsm.FramesPerSecond));
					_rsm1Watch.Reset();
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

				if (_rsm != null && _rsm.AnimationLength > 0) {
					_watch.Reset();
					_watch.Start();

					lock (_lockAnimation) {
						if (!_rsm1Watch.IsRunning)
							_rsm1Watch.Start();
							
						_rsm.SetAnimationIndex(_rsm1Watch.ElapsedMilliseconds + _elapsedOffset, 1f);
						_animationPosition = _rsm.AnimationIndex;

						_sliderPosition.Dispatch(delegate {
							_sliderAnimation.SetPosition((float)_animationPosition / _rsm.AnimationLength, true);
							_sliderPosition.Text = _animationPosition + "";
						});

						_rsm.Dirty();

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
	}
}