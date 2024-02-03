using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ErrorManager;
using GRF.FileFormats.RswFormat;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.MapRenderers;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;
using Key = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for OpenGLViewport.xaml
	/// </summary>
	public partial class OpenGLViewport : UserControl {
		// Camera settings
		private Camera _camera;

		public Camera Camera {
			get { return _camera; }
			set { _camera = value; }
		}

		// Viewport settings
		internal bool _glControlReady;
		private readonly List<Renderer> _renderers = new List<Renderer>();
		public Matrix4 View;
		public Matrix4 Projection;
		public long FrameRenderTime;
		public int _frameCount = 0;

		public List<Renderer> Renderers {
			get { return _renderers; }
		}

		// Mouse input settings
		private Point _oldPosition;

		// Animation settings
		private readonly ManualResetEvent _renderThreadHandle = new ManualResetEvent(false);
		internal bool _renderThreadEnabled = true;
		internal bool _isRunning = true;

		// Loading settings
		public readonly RendererLoader Loader = new RendererLoader();
		public readonly Shader Shader_rsm;
		public readonly Shader Shader_gnd;
		public readonly Shader Shader_water;
		public readonly Shader Shader_lub;
		public readonly Shader Shader_simple;

		// Camera settings
		public bool ResetCameraDistance { get; set; }
		public bool ResetCameraPosition { get; set; }
		public bool RotateCamera { get; set; }
		public bool IsRotatingCamera { get; set; }

		public Vector3 LightAmbient = new Vector3(0.5f);
		public Vector3 LightDirection {
			get { return _camera.LookAt - _camera.Position; }
		}

		internal GLControl _primary;

		private long _previousTick;
		private long _currentTick;
		private readonly Stopwatch _watchRenderStart = new Stopwatch();
		private long _fpsRefreshTimer;
		private const long _fpsUpdateFrequency = 200;
		internal RendererLoadRequest _request;
		private bool _crashState;
		internal Window _editorWindow;
		public MapRendererOptions RenderOptions { get; set; }

		public OpenGLViewport()
			: this(GrfEditorConfiguration.MapRenderEnableFSAA ? 8 : 0) {
		}

		public OpenGLViewport(int antialias) {
			InitializeComponent();
			
			try {
				RenderOptions = new MapRendererOptions();
				_primary = new GLControl(new GraphicsMode(32, 24, 0, antialias));
				_camera = new Camera(this);
				_host.Child = _primary;

				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				ResetCameraDistance = true;
				ResetCameraPosition = true;

				_primary.Load += _primary_Load;
				_primary.Resize += _primary_Resize;

				_primary.MouseMove += new MouseEventHandler(_primary_MouseMove);
				_primary.MouseDown += new MouseEventHandler(_primary_MouseDown);
				_primary.MouseWheel += new MouseEventHandler(_primary_MouseWheel);
				_primary.KeyDown += new KeyEventHandler(_primary_KeyDown);

				IsVisibleChanged += delegate {
					EnableRenderThread = IsVisible;
				};

				EnableRenderThread = true;
				new Thread(_renderRefreshThread) { Name = "GrfEditor - Map animation update thread" }.Start();

				Dispatcher.ShutdownStarted += delegate {
					UnloadAndStopViewport();
				};

				OpenGLMemoryManager.CreateInstance(this);
				_primary.MakeCurrent();

				Shader_rsm = new Shader("shader_map_rsm.vert", "shader_map_rsm.frag");
				Shader_gnd = new Shader("shader_map_gnd.vert", "shader_map_gnd.frag");
				Shader_water = new Shader("shader_map_water.vert", "shader_map_water.frag");
				Shader_lub = new Shader("shader_map_lub.vert", "shader_map_lub.frag");
				Shader_simple = new Shader("shader_map_simple.vert", "shader_map_simple.frag");

				_renderers.Add(new BackgroundRenderer { Permanent = true });

				Loader.ClearFunction = _clearRenderers;
				Loader.LoadFunction = Load;

				Loaded += delegate {
					if (_editorWindow != null)
						return;

					_editorWindow = WpfUtilities.FindParentControl<Window>(this);

					if (_editorWindow != null) {
						_editorWindow.Closed += delegate {
							UnloadAndStopViewport();
						};
					}
				};
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void UnloadAndStopViewport() {
			if (!_isRunning)
				return;

			_isRunning = false;
			_glControlReady = false;
			EnableRenderThread = true;
			Loader.ExitThreads();

			OpenGLMemoryManager.MakeCurrent(this);
			_primary.MakeCurrent();

			try {
				for (int i = 0; i < _renderers.Count; i++) {
					_renderers[i].Unload();
					_renderers.RemoveAt(i);
					i--;
				}
			}
			catch {
			}

			TextureManager.UnloadAllTextures(this);
			OpenGLMemoryManager.Clear();
			OpenGLMemoryManager.Remove(this);
		}

		public void Load(RendererLoadRequest request) {
			if (request.CancelRequired())
				return;

			_request = request;

			if (request.IsMap) {
				_loadMap(request);
			}
			else {
				_loadRsm(request);
			}
		}

		private void _primary_MouseWheel(object sender, MouseEventArgs e) {
			_camera.Zoom(e.Delta < 0 ? 1.1f : 0.9f);
			IsRotatingCamera = false;
		}

		private void _primary_MouseDown(object sender, MouseEventArgs e) {
			_oldPosition = new Point(e.Location.X, e.Location.Y);

			if (e.Button == MouseButtons.Left) {
				_primary.Capture = true;
			}
			else if (e.Button == MouseButtons.Right) {
				_primary.Capture = true;
			}
		}

		private void _primary_MouseMove(object sender, MouseEventArgs e) {
			if (!_primary.Capture)
				return;

			var newMousePosition = new Point(e.Location.X, e.Location.Y);

			if (e.Button == MouseButtons.Left) {
				if (Keyboard.IsKeyDown(Key.LeftShift) && _request != null && _request.IsMap) {
					_camera.CancelMovement();
					return;
				}

				_selectingTiles = false;
				_camera.RotateXY(newMousePosition.X - _oldPosition.X, newMousePosition.Y - _oldPosition.Y);
			}
			else if (e.Button == MouseButtons.Right) {
				if (Keyboard.IsKeyDown(Key.LeftCtrl))
					_camera.TranslateY(newMousePosition.Y - _oldPosition.Y);
				else
					_camera.TranslateXZ(newMousePosition.X - _oldPosition.X, newMousePosition.Y - _oldPosition.Y);
			}

			_oldPosition = new Point(e.Location.X, e.Location.Y);
		}

		private void _render() {
			if (_crashState)
				return;
			
			_primary_Resize(this, null);
			_primary_Render();
		}

		private int _previousWidth;
		private int _previousHeight;

		private void _primary_Resize(object sender, EventArgs e) {
			if (!_glControlReady)
				return;

			if (_previousWidth == _primary.Width && _previousHeight == _primary.Height) {
				return;
			}

			if (RenderOptions.MinimapMode) {
				_primary.Width = _previousWidth;
				_primary.Height = _previousHeight;
			}
			else {
				_previousWidth = _primary.Width;
				_previousHeight = _primary.Height;
			}

			_primary.MakeCurrent();
			GL.Viewport(0, 0, _primary.Width, _primary.Height);
		}

		private void _loadMap(RendererLoadRequest request) {
			RenderOptions.ForceShader = -1;
			RenderOptions.RenderingMap = true;
			RenderOptions.RenderSkymapDetected = false;
			Rsm.ForceShadeType = -1;
			GLHelper.OnLog(() => "Message: Loading map \"" + request.Resource + "\"");

			if (RotateCamera)
				IsRotatingCamera = true;

			Rsw rsw = new Rsw(ResourceManager.GetData(request.Resource + ".rsw"));
			Gnd gnd = new Gnd(ResourceManager.GetData(request.Resource + ".gnd"));

			var glGnd = new GndRenderer(request, Shader_gnd, gnd, rsw);
			var glWater = new WaterRenderer(request, Shader_water, rsw, gnd);

			if (request.CancelRequired())
				return;

			glGnd.Load(this);
			glWater.Load(this);
			MapRenderer mapRenderer = new MapRenderer(request, Shader_rsm, rsw);
			mapRenderer.LoadModels(rsw, gnd, RenderOptions.AnimateMap);
			LubRenderer lubRenderer = null;

			try {
				lubRenderer = new LubRenderer(request, Shader_lub, gnd, rsw, ResourceManager.GetData(@"data\luafiles514\lua files\effecttool\" + Path.GetFileName(request.Resource) + ".lub"), this);
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

			_renderers.Add(glGnd);
			_renderers.Add(mapRenderer);
			_renderers.Add(glWater);

			if (lubRenderer != null)
				_renderers.Add(lubRenderer);

			if (ResetCameraPosition)
				_camera.LookAt = new Vector3(gnd.Width * 5f, 0, gnd.Height * 5f + 10f);

			if (ResetCameraDistance)
				_camera.Distance = Math.Max(gnd.Header.Height, gnd.Header.Width) * _camera.DistanceMultiplier_Map;

			ResetCameraPosition = ResetCameraDistance = true;
		}

		private void _loadRsm(RendererLoadRequest request) {
			RenderOptions.ForceShader = 4;
			RenderOptions.RenderingMap = false;
			GLHelper.OnLog(() => "Message: Loading RSM \"" + request.Resource + "\"");

			if (RotateCamera)
				IsRotatingCamera = true;

			var rsm = request.Rsm;
			_renderers.Add(new ModelRenderer(request, rsm, Shader_rsm));

			if (ResetCameraPosition)
				_camera.LookAt = rsm.Version >= 2.0 ? new Vector3(-rsm.DrawnBox.Center) : new Vector3(0, 0, 0);

			if (ResetCameraDistance)
				_camera.Distance = Math.Max(Math.Max(rsm.DrawnBox.Range[0], rsm.DrawnBox.Range[1]), rsm.DrawnBox.Range[2]) * _camera.DistanceMultiplier_Rsm;

			ResetCameraPosition = ResetCameraDistance = true;
		}

		public void Unload() {
			_clearRenderers();
		}

		private void _clearRenderers() {
			GLHelper.OnLog(() => null);
			GLHelper.OnLog(() => "Message: Clearing unused data...");

			this.Dispatch(delegate {
				OpenGLMemoryManager.MakeCurrent(this);
				_primary.MakeCurrent();

				for (int i = 0; i < _renderers.Count; i++) {
					if (_renderers[i].Permanent)
						continue;

					_renderers[i].Unload();
					_renderers.RemoveAt(i);
					i--;
				}

				TextureManager.UnloadAllTextures(this);
				OpenGLMemoryManager.Clear();
			});

			_selectionTiles.Clear();
		}

		private void _primary_Load(object sender, EventArgs e) {
			OpenGLMemoryManager.MakeCurrent(this);
			_primary.MakeCurrent();
			_glControlReady = true;
			_watchRenderStart.Start();
		}

		private void _primary_Render() {
			if (_crashState || !_glControlReady)
				return;

			try {
				_currentTick = _watchRenderStart.ElapsedMilliseconds;
				FrameRenderTime = _currentTick - _previousTick;

				OpenGLMemoryManager.MakeCurrent(this);
				_primary.MakeCurrent();

				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				//_background.Render(this);
				GL.Disable(EnableCap.CullFace);
				GL.Enable(EnableCap.DepthTest);
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
				
				_camera.Update();
				
				View = _camera.GetViewMatrix();
				Projection = _camera.GetProjectionMatrix();
				
				SharedRsmRenderer.UpdateShader(Shader_rsm, this);

				foreach (var renderer in _renderers) {
					renderer.Render(this);
				}
				
				_selectionRender();

				// FPS handling
				_frameCount++;
				_fpsRefreshTimer -= FrameRenderTime;
				
				if (_fpsRefreshTimer <= 0) {
					if (RenderOptions.ShowFps && MapRenderer.FpsTextBlock != null) {
						int fps = (int)Math.Ceiling(_frameCount * 1000f / (_fpsUpdateFrequency - _fpsRefreshTimer));
						MapRenderer.FpsTextBlock.Text = fps + "" + (RenderOptions.FpsCap > 0 ? " (limited " + RenderOptions.FpsCap + ")" : "");
					}
				
					_frameCount = 0;
					_fpsRefreshTimer = _fpsUpdateFrequency;
				}

				_primary.SwapBuffers();
				_previousTick = _currentTick;
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

		private void _btnResume_Click(object sender, RoutedEventArgs e) {
			_crashGrid.Visibility = Visibility.Collapsed;
			_host.Visibility = Visibility.Visible;
			_crashState = false;
		}

		#region Render Thread
		public bool EnableRenderThread {
			set {
				if (value) {
					if (!_renderThreadEnabled)
						_renderThreadHandle.Set();
				}
				else {
					if (_renderThreadEnabled) {
						_renderThreadEnabled = false;
						_renderThreadHandle.Reset();
					}
				}
			}
		}

		private void _renderRefreshThread() {
			while (_isRunning) {
				long renderTick = _watchRenderStart.ElapsedMilliseconds;
				this.Dispatch(p => _render());
				var interval = _watchRenderStart.ElapsedMilliseconds - renderTick;

				if (RenderOptions.FpsCap > 0) {
					Thread.Sleep((int)Math.Max(1, 1000 / RenderOptions.FpsCap - interval));
				}
				else {
					// SwapBuffers won't allow the FPS to go past the monitor refresh rate, but... it seems a bit buggy. Put a hard limit on this either way.
					Thread.Sleep((int)Math.Max(2, 1000 / 200 - interval));
				}

				if (!_renderThreadEnabled) {
					_renderThreadHandle.WaitOne();

					if (!_renderThreadEnabled)
						_renderThreadEnabled = true;
				}
			}
		}
		#endregion
	}
}
