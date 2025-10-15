using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ErrorManager;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.RswFormat;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.MapRenderers;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;
using Utilities;
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
		public ViewportStatistics Stats = new ViewportStatistics();
		
		public Camera Camera {
			get { return _camera; }
			set { _camera = value; }
		}

		// Viewport settings
		internal bool _glControlReady;
		private readonly List<Renderer> _renderers = new List<Renderer>();
		public Matrix4 View;
		public Matrix4 Projection;
		public Matrix4 ViewProjection;
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
		public readonly Shader Shader_gat;
		public readonly Shader Shader_skymap;

		// Camera settings
		public bool ResetCameraDistance { get; set; }
		public bool ResetCameraPosition { get; set; }
		public bool RotateCamera { get; set; }
		public bool IsRotatingCamera { get; set; }
		public double OpenGLVersion { get; private set; }

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
		private TextBlock _tbFps;

		public RenderMode RenderPass { get; set; } = RenderMode.OpaqueTextures;

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
					EnableRenderThread = IsVisible && Visibility == Visibility.Visible;
				};

				EnableRenderThread = true;
				new Thread(_renderRefreshThread) { Name = "GrfEditor - Map animation update thread" }.Start();

				Dispatcher.ShutdownStarted += delegate {
					UnloadAndStopViewport();
				};

				OpenGLMemoryManager.CreateInstance(this);
				_primary.MakeCurrent();

				GLHelper.VerifyError();
				Shader_rsm = new Shader("map.rsm.vert", "map.rsm.frag");
				Shader_gnd = new Shader("map.gnd.vert", "map.gnd.frag");
				Shader_water = new Shader("map.water.vert", "map.water.frag");
				Shader_lub = new Shader("map.lub.vert", "map.lub.frag");
				Shader_simple = new Shader("map.color.vert", "map.color.frag");
				Shader_gat = new Shader("map.gat.vert", "map.gat.frag");
				Shader_skymap = new Shader("map.skymap.vert", "map.skymap.frag");
				GLHelper.VerifyError();

				int majorVersion = GL.GetInteger(GetPName.MajorVersion);
				int minorVersion = GL.GetInteger(GetPName.MinorVersion);
				OpenGLVersion = FormatConverters.DoubleConverterNoThrow(majorVersion + "." + minorVersion);

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

			if (_request != null && !request.AlwaysReload && _request.Resource == request.Resource)
				return;

			//request.Resource = @"C:\Games\NovaRO - 4th - NewClient\data\morocc";

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
			Rsm.ForceShadeType = -1;
			GLHelper.OnLog(() => "Message: Loading map \"" + request.Resource + "\"");

			if (RotateCamera)
				IsRotatingCamera = true;

			Rsw rsw = request.Preloaded ? request.Rsw : new Rsw(ResourceManager.GetData(request.Resource + ".rsw"));
			Gnd gnd;
			if (request.Preloaded) {
				gnd = request.Gnd;
			}
			else {
				var entryGnd = ResourceManager.GetData(request.Resource + ".gnd");

				if (entryGnd == null)
					entryGnd = ResourceManager.GetData("data\\" + rsw.Header.GroundFile);

				gnd = new Gnd(entryGnd);
			}

			var glGnd = new GndRenderer(request, Shader_gnd, gnd, rsw);
			var glWater = new WaterRenderer(request, Shader_water, rsw, gnd);

			GatRenderer glGat = null;
			if (GrfEditorConfiguration.MapRenderRenderGat) {
				Gat gat = request.Preloaded ? request.Gat : new Gat(ResourceManager.GetData(request.Resource + ".gat"));
				request.Gat = gat;
				glGat = new GatRenderer(request, Shader_gat, gat, gnd);
			}

			if (request.CancelRequired())
				return;

			glGnd.Load(this);
			if (glGat != null)
				glGat.Load(this);
			glWater.Load(this);
			MapRenderer mapRenderer = new MapRenderer(request, Shader_rsm, rsw);
			mapRenderer.LoadModels(this, rsw, gnd, RenderOptions.AnimateMap);
			LubRenderer lubRenderer = null;
			SkyMapRenderer skyRenderer = null;

			try {	
				lubRenderer = new LubRenderer(request, Shader_lub, gnd, rsw, ResourceManager.GetData(@"data\luafiles514\lua files\effecttool\" + Path.GetFileName(request.Resource) + ".lub"), this);
				//lubRenderer = new LubRenderer(request, Shader_lub, gnd, rsw, ResourceManager.GetData(@"C:\Games\NovaRO - 4th - NewClient\data\luafiles514\lua files\effecttool\" + Path.GetFileName(request.Resource) + ".lub"), this);
				lubRenderer.Load(this);
			}
			catch {
				lubRenderer = null;
			}

			try {
				skyRenderer = new SkyMapRenderer(request, Shader_skymap, gnd, this);
			}
			catch {
			}

			request.Rsw = rsw;
			request.Gnd = gnd;
			request.GndRenderer = glGnd;
			request.MapRenderer = mapRenderer;
			request.SkyMapRenderer = skyRenderer;

			Loader.OnLoaded(request);

			_renderers.Add(glGnd);if (skyRenderer != null)
				_renderers.Add(skyRenderer);
			_renderers.Add(mapRenderer);
			
			if (glGat != null)
				_renderers.Add(glGat);
			_renderers.Add(glWater);

			if (lubRenderer != null)
				_renderers.Add(lubRenderer);

			if (ResetCameraPosition)
				_camera.LookAt = new Vector3(gnd.Width * 5f, 0, gnd.Height * 5f + 10f);

			if (ResetCameraDistance)
				_camera.Distance = Math.Max(gnd.Header.Height, gnd.Header.Width) * _camera.DistanceMultiplier_Map;

			var maxDistance = Math.Max(gnd.Header.Height, gnd.Header.Width) * 10f;
			Camera.MaxDistance = (float)Math.Max(Camera.MaxDistance, maxDistance);

			ResetCameraPosition = ResetCameraDistance = true;
		}

		private void _loadRsm(RendererLoadRequest request) {
			RenderOptions.ForceShader = 4;
			RenderOptions.RenderingMap = false;
			GLHelper.OnLog(() => "Message: Loading RSM \"" + request.Resource + "\"");
			Loader.OnLoaded(request);

			if (RotateCamera)
				IsRotatingCamera = true;

			var rsm = request.Rsm;
			_renderers.Add(new ModelRenderer(request, rsm, Shader_rsm));

			if (ResetCameraPosition)
				_camera.LookAt = new Vector3(0, 0, 0);

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
			GLHelper.VerifyError();
			_glControlReady = true;
			_watchRenderStart.Start();
		}

		private void _primary_Render() {
			if (_crashState || !_glControlReady || _primary.Width <= 0 || _primary.Height <= 0 || (_editorWindow != null && _editorWindow.WindowState == System.Windows.WindowState.Minimized))
				return;

			try {
#if DEBUG
				Stats = new ViewportStatistics();
#endif
				_currentTick = _watchRenderStart.ElapsedMilliseconds;
				FrameRenderTime = _currentTick - _previousTick;

				OpenGLMemoryManager.MakeCurrent(this);
				_primary.MakeCurrent();
				GLHelper.VerifyError();

				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				GL.Disable(EnableCap.CullFace);
				GL.Enable(EnableCap.DepthTest);
				GL.DepthFunc(DepthFunction.Less);
				GL.DepthMask(true);
				GL.Disable(EnableCap.Blend);
				
				_camera.Update();
				GLHelper.VerifyError();
				View = _camera.GetViewMatrix();
				Projection = _camera.GetProjectionMatrix();
				ViewProjection = View * Projection;
				
				SharedRsmRenderer.UpdateShader(Shader_rsm, this);

				var renderers = _renderers.ToList();

				// Draw opaque textures
				RenderPass = RenderMode.OpaqueTextures;
				foreach (var renderer in renderers) {
					renderer.Render(this);
				}

				GLHelper.VerifyError();
				// Draw opaque transparent textures
				RenderPass = RenderMode.OpaqueTransparentTextures;

				foreach (var renderer in renderers) {
					if (renderer is ModelRenderer || renderer is MapRenderer)
						renderer.Render(this);
				}
				GLHelper.VerifyError();
				// Draw transparent textures
				RenderPass = RenderMode.TransparentTextures;
				GLHelper.VerifyError();
				foreach (var renderer in renderers) {
					renderer.Render(this);
				}
				GLHelper.VerifyError();
				// Ignore that, it's merged with transparent textures, though it is technically accurate
				// Draw animated textures (always on top of transparent textures)
				RenderPass = RenderMode.AnimatedTransparentTextures;
				foreach (var renderer in renderers) {
					if (renderer is ModelRenderer || renderer is MapRenderer)
						renderer.Render(this);
				}
				GLHelper.VerifyError();
				// Draw lub effects
				RenderPass = RenderMode.LubTextures;
				foreach (var renderer in renderers) {
					renderer.Render(this);
				}
				GL.DepthMask(true);
				_selectionRender();
				GLHelper.VerifyError();
				// FPS handling
				_frameCount++;
				_fpsRefreshTimer -= FrameRenderTime;
				
				if (_fpsRefreshTimer <= 0) {
					if (RenderOptions.ShowFps && (_tbFps ?? MapRenderer.FpsTextBlock) != null) {
						int fps = (int)Math.Ceiling(_frameCount * 1000f / (_fpsUpdateFrequency - _fpsRefreshTimer));
						string output = fps + "" + (RenderOptions.FpsCap > 0 ? " (limited " + RenderOptions.FpsCap + ")" : "");

#if DEBUG
						output = "Draw calls: " + Stats.DrawArrays_Calls + ", Vertex count: " + Stats.DrawArrays_Calls_VertexLength + ", " + output;
#endif

						(_tbFps ?? MapRenderer.FpsTextBlock).Text = output;
					}
				
					_frameCount = 0;
					_fpsRefreshTimer = _fpsUpdateFrequency;
				}
				GLHelper.VerifyError();
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

		public void SetFpsTextBlock(TextBlock tbFps) {
			_tbFps = tbFps;
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
