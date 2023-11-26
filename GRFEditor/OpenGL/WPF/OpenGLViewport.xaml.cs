using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ErrorManager;
using GRF.FileFormats.RswFormat;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.MapGLGroup;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;
using UserControl = System.Windows.Controls.UserControl;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for OpenGLViewport.xaml
	/// </summary>
	public partial class OpenGLViewport : UserControl {
		// Camera settings
		private double _angleXRad;
		private Vector3 _lookAt = new Vector3(0, 0, 0);
		private Vector3 _cameraPosition = new Vector3(50, 20, 0f);
		private float _near = 10f;
		private double _angleYDegree = 20;
		private double _distance = 20;
		private const float _viewYDirection = 1f;

		// Viewport settings
		private bool _loaded;
		private readonly List<MapGLObject> _objects = new List<MapGLObject>();
		private readonly object _objectLock = new object();
		public Matrix4 View;
		public Matrix4 Projection;

		public List<MapGLObject> GLObjects {
			get { return _objects; }
		}

		// Mouse input settings
		private Point _oldPosition;

		#region Animation settings
		private readonly ManualResetEvent _animationThreadHandle = new ManualResetEvent(false);
		private bool _threadIsEnabled = true;
		private bool _isRunning = true;
		private readonly object _lockAnimation = new object();
		#endregion

		// Loading settings
		public readonly RendererLoader Loader = new RendererLoader();
		private readonly Shader _shader_rsm;
		private readonly Shader _shader_gnd;
		private readonly Shader _shader_water;
		private readonly Shader _shader_lub;
		private readonly BackgroundRenderer _background;

		// Camera settings
		public bool ResetCameraDistance { get; set; }
		public bool ResetCameraPosition { get; set; }
		public bool RotateCamera { get; set; }
		public bool IsRotatingCamera { get; set; }
		public Vector3 LightAmbient = new Vector3(0.5f);

		public Vector3 LightDirection {
			get { return _lookAt - _cameraPosition; }
		}

		public Vector3 CameraPosition {
			get { return _cameraPosition; }
		}

		internal GLControl _primary;

		private readonly Stopwatch _watchRender = new Stopwatch();
		private readonly Stopwatch _watchFps = new Stopwatch();
		private long _previousMillisecond;
		private readonly List<long> _elapsed = new List<long>();
		private RendererLoadRequest _request;
		private bool _crashState;

		public OpenGLViewport() {
			InitializeComponent();

			_primary = new GLControl(new GraphicsMode(32, 24, 0, 8));
			_host.Child = _primary;

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			ResetCameraDistance = true;
			ResetCameraPosition = true;

			_primary.Load += _primary_Load;
			_primary.Resize += _primary_Resize;
			_primary.Paint += _primary_Paint;

			_primary.MouseMove += new System.Windows.Forms.MouseEventHandler(_primary_MouseMove);
			_primary.MouseDown += new System.Windows.Forms.MouseEventHandler(_primary_MouseDown);
			_primary.MouseWheel += new System.Windows.Forms.MouseEventHandler(_primary_MouseWheel);

			Dispatcher.ShutdownStarted += delegate {
				_isRunning = false;
				EnableAnimationThread = true;
			};

			this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(_previewRsm_IsVisibleChanged);
			EnableAnimationThread = true;
			new Thread(_animationThread) { Name = "GrfEditor - Map animation update thread" }.Start();

			Dispatcher.ShutdownStarted += delegate {
				TextureManager.ExitTextureThreads();
				Loader.ExitThreads();
			};

			_primary.MakeCurrent();

			_shader_rsm = new Shader("shader_map_rsm.vert", "shader_map_rsm.frag");
			_shader_gnd = new Shader("shader_map_gnd.vert", "shader_map_gnd.frag");
			_shader_water = new Shader("shader_map_water.vert", "shader_map_water.frag");
			_shader_lub = new Shader("shader_map_lub.vert", "shader_map_lub.frag");

			_background = new BackgroundRenderer();

			Loader.ClearFunction = _clearGlObjects;
			Loader.LoadFunction = request => {
				if (request.CancelRequired())
					return;

				if (request.IsMap) {
					_loadMap(request);
				}
				else {
					_loadRsm(request);
				}
			};
		}

		private void _previewRsm_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (this.IsVisible) {
				EnableAnimationThread = true;
			}
			else {
				EnableAnimationThread = false;
			}
		}

		private void _primary_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e) {
			var mult = e.Delta < 0 ? 1.1f : 0.9f;
			_distance *= mult;
			_distance = GLHelper.Clamp(0f, 3500f, _distance);
			IsRotatingCamera = false;
			UpdateCamera();
		}

		private void _primary_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			_oldPosition = new Point(e.Location.X, e.Location.Y);

			if (e.Button == MouseButtons.Left) {
				_primary.Capture = true;
			}
			else if (e.Button == MouseButtons.Right) {
				_primary.Capture = true;
			}
		}

		private void _primary_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
			var newMousePosition = new Point(e.Location.X, e.Location.Y);

			if (e.Button == MouseButtons.Left && _primary.Capture) {
				double distanceDelta = 1f;

				if (_distance < 50) {
					distanceDelta = (1d - (200 - _distance) / 300f);
				}
				else if (_distance < 200) {
					distanceDelta = (1d - (200 - _distance) / 400f);
				}

				double deltaX = _viewYDirection * GLHelper.ToRad(newMousePosition.X - _oldPosition.X) / 1f;
				_angleXRad -= deltaX * distanceDelta;

				double deltaY = _viewYDirection * (newMousePosition.Y - _oldPosition.Y) / 2f;
				_angleYDegree += deltaY * distanceDelta;

				IsRotatingCamera = false;
				UpdateCamera();
			}
			else if (e.Button == MouseButtons.Right && _primary.Capture) {
				if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
					double deltaY = newMousePosition.Y - _oldPosition.Y;
					double distY = _distance * 0.0003 * deltaY;

					_lookAt.Y += (float)distY;
				}
				else {
					double deltaX = newMousePosition.X - _oldPosition.X;
					double deltaZ = newMousePosition.Y - _oldPosition.Y;

					double distX = _distance * 0.0013 * deltaX * (MapRenderer.RenderOptions.UseClientPov ? 0.4 : 1);
					double distZ = _distance * 0.0013 * deltaZ * (MapRenderer.RenderOptions.UseClientPov ? 0.4 : 1);

					_lookAt.X += (float)(-distX * Math.Cos(_angleXRad) - distZ * Math.Sin(_angleXRad));
					_lookAt.Z += (float)(distX * Math.Sin(_angleXRad) - distZ * Math.Cos(_angleXRad));

					if (_request != null && _request.Gnd != null && MapRenderer.RenderOptions.ViewStickToGround) {
						var x = _lookAt.X / 10f;
						var z = _lookAt.Z / 10f;

						var xi = (int)x;
						var yi = (int)z;

						var cube = _request.Gnd[xi, _request.Gnd.Height - yi];

						if (cube != null && cube.TileUp != -1) {
							var y2 = -cube[0];
							var dif = (y2 - _lookAt.Y) * 0.05 * (MapRenderer.RenderOptions.UseClientPov ? 0.2 : 1);
							_lookAt.Y += (float)dif;
						}
					}
				}

				IsRotatingCamera = false;
				UpdateCamera();
			}

			_oldPosition = new Point(e.Location.X, e.Location.Y);
		}

		private void UpdateCamera() {
			_angleYDegree = _angleYDegree > 89 ? 89 : _angleYDegree;
			_angleYDegree = _angleYDegree < -89 ? -89 : _angleYDegree;

			double mult = MapRenderer.RenderOptions.UseClientPov ? 2 : 1;
			double subDistance = mult * _distance * Math.Cos(GLHelper.ToRad(_angleYDegree));
			_cameraPosition.X = (float)(subDistance * Math.Sin(_angleXRad));
			_cameraPosition.Y = (float)(mult * _distance * Math.Sin(GLHelper.ToRad(_angleYDegree)));
			_cameraPosition.Z = (float)(subDistance * Math.Cos(_angleXRad));
			_cameraPosition += _lookAt;

			MapRenderer.LookAt = _lookAt;
		}

		private void _render() {
			if (_crashState)
				return;

			_primary_Resize(this, null);
			_primary_Paint(this, null);
		}

		private int _previousWidth;
		private int _previousHeigt;

		private void _primary_Resize(object sender, EventArgs e) {
			if (!_loaded)
				return;

			if (_previousWidth == _primary.Width && _previousHeigt == _primary.Height)
				return;

			_previousWidth = _primary.Width;
			_previousHeigt = _primary.Height;
			GL.Viewport(0, 0, _primary.Width, _primary.Height);
			_background.Resize(this);
		}

		private void _loadMap(RendererLoadRequest request) {
			SharedRsmRenderer.ForceShader = -1;
			MapRenderer.RenderOptions.RenderingMap = true;
			MapRenderer.RenderOptions.RenderSkymapDetected = false;
			Rsm.ForceShadeType = -1;

			if (RotateCamera) {
				IsRotatingCamera = true;
				_watchRender.Reset();
			}

			_request = request;
			Rsw rsw = new Rsw(ResourceManager.GetData(request.Resource + ".rsw"));
			Gnd gnd = new Gnd(ResourceManager.GetData(request.Resource + ".gnd"));

			var glGnd = new GndRenderer(request, _shader_gnd, gnd, rsw);
			var glWater = new WaterRenderer(request, _shader_water, rsw, gnd);

			if (request.CancelRequired())
				return;
			
			glGnd.Load(this);
			glWater.Load(this);

			MapRenderer mapRenderer = new MapRenderer(request, _shader_rsm, rsw);
			mapRenderer.LoadModels(rsw, gnd, MapRenderer.RenderOptions.AnimateMap);

			LubRenderer lubRenderer = null;

			try {
				lubRenderer = new LubRenderer(request, _shader_lub, gnd, rsw, ResourceManager.GetData(@"data\luafiles514\lua files\effecttool\" + Path.GetFileName(request.Resource) + ".lub"));
				lubRenderer.Load(this);
			}
			catch {
				lubRenderer = null;
			}

			request.Rsw = rsw;
			request.Gnd = gnd;
			request.GndRenderer = glGnd;
			request.MapRenderer = mapRenderer;
			Loader.OnLoaded(request);

			lock (_objectLock) {
				_objects.Add(glGnd);
				_objects.Add(mapRenderer);
				_objects.Add(glWater);

				if (lubRenderer != null)
					_objects.Add(lubRenderer);
			}

			if (ResetCameraPosition) {
				_lookAt.X = gnd.Width * 5f;
				_lookAt.Y = 0 * 5f;
				_lookAt.Z = gnd.Height * 5f;
			}

			_near = 10f;

			if (ResetCameraDistance)
				_distance = Math.Max(gnd.Header.Height, gnd.Header.Width) * 10f;

			ResetCameraPosition = ResetCameraDistance = true;
			UpdateCamera();
		}

		private void _loadRsm(RendererLoadRequest request) {
			SharedRsmRenderer.ForceShader = 4;
			MapRenderer.RenderOptions.RenderingMap = false;

			if (RotateCamera) {
				IsRotatingCamera = true;
				_watchRender.Reset();
			}

			var rsm = request.Rsm;
			var model = new ModelRenderer(request, rsm, _shader_rsm);

			lock (_objectLock) {
				_objects.Add(model);
			}

			if (ResetCameraPosition) {
				_lookAt = rsm.Version >= 2.0 ? new Vector3(-rsm.DrawnBox.Center) : new Vector3(0, rsm.DrawnBox.Center.Y, 0);
			}

			if (ResetCameraDistance)
				_distance = Math.Max(Math.Max(rsm.DrawnBox.Range[0], rsm.DrawnBox.Range[1]), rsm.DrawnBox.Range[2]) * 3;

			if (_distance < 50) {
				_near = 1f;
			}
			else {
				_near = 5f;
			}

			ResetCameraPosition = ResetCameraDistance = true;
		}

		private void _clearGlObjects() {
			this.Dispatch(delegate {
				lock (_objectLock) {
					for (int i = 0; i < _objects.Count; i++) {
						if (_objects[i].Permanent)
							continue;

						_objects[i].Unload();
						_objects.RemoveAt(i);
						i--;
					}
				}

				TextureManager.UnloadAllTextures();
				OpenGLMemoryManager.Clear();
			});
		}

		private void _primary_Load(object sender, EventArgs e) {
			_primary.MakeCurrent();
			_loaded = true;
		}

		public void StartRotatingCamera() {
			_watchRender.Reset();
			_watchRender.Start();
			IsRotatingCamera = true;
			RotateCamera = true;
		}

		private void _primary_Paint(object sender, PaintEventArgs e) {
			if (_crashState)
				return;

			if (e == null && !_loaded)
				return;

			try {
				_primary.MakeCurrent();
				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

				_background.Render(this);

				GL.Disable(EnableCap.CullFace);
				GL.Enable(EnableCap.DepthTest);

				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

				if (IsRotatingCamera && RotateCamera) {
					_watchRender.Stop();
					var ms = _watchRender.ElapsedMilliseconds;
					_angleXRad += 0.03 * (ms / 50f);
					_watchRender.Reset();
					_watchRender.Start();
				}

				UpdateCamera();

				View = Matrix4.LookAt(_cameraPosition, _lookAt, new Vector3(0, _viewYDirection, 0));
				Projection = Matrix4.CreatePerspectiveFieldOfView(GLHelper.ToRad(MapRenderer.RenderOptions.UseClientPov ? 10 : 45), _primary.Width / (float)(_primary.Height), _near, MapRenderer.RenderOptions.UseClientPov ? 7000f : 5000f);

				SharedRsmRenderer.UpdateShader(_shader_rsm, this);

				List<MapGLObject> objects;

				lock (_objectLock) {
					objects = _objects.ToList();
				}

				foreach (var obj in objects) {
					obj.Render(this);
				}

				_primary.SwapBuffers();

				if (MapRenderer.RenderOptions.ShowFps && MapRenderer.FpsTextBlock != null) {
					_watchFps.Stop();
					long elapsed = _watchFps.ElapsedMilliseconds;
					_watchFps.Reset();
					_watchFps.Start();

					_previousMillisecond -= elapsed;

					if (_previousMillisecond <= 0 && _elapsed.Count > 0) {
						if (MapRenderer.FpsTextBlock != null) {
							double average = _elapsed.Average();

							MapRenderer.FpsTextBlock.Dispatcher.BeginInvoke(new Action(delegate {
								MapRenderer.FpsTextBlock.Text = (int)(1000f / Math.Max(1, average)) + "";
							}), System.Windows.Threading.DispatcherPriority.Background);
						}

						_elapsed.Clear();
						_previousMillisecond = 200;
					}
					else {
						_elapsed.Add(elapsed);
					}
				}
			}
			catch (Exception err) {
				_crashState = true;
				try {
					_host.Visibility = Visibility.Collapsed;
					_crashGrid.Visibility = Visibility.Visible;
				}
				catch {
				}

				ErrorHandler.HandleException(err);
			}
		}

		#region Animation
		public bool EnableAnimationThread {
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

				lock (_lockAnimation) {
					this.Dispatch(p => _render());
				}

				Thread.Sleep(1);

				if (!_threadIsEnabled) {
					_animationThreadHandle.WaitOne();

					if (!_threadIsEnabled)
						_threadIsEnabled = true;
				}
			}
		}
		#endregion

		private void _btnResume_Click(object sender, RoutedEventArgs e) {
			_crashGrid.Visibility = Visibility.Collapsed;
			_host.Visibility = Visibility.Visible;
			_crashState = false;
		}
	}
}
