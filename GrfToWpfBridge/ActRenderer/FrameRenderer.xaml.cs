using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ErrorManager;
using GRF.FileFormats.ActFormat;
using GrfToWpfBridge.DrawingComponents;
using Utilities.Tools;

namespace GrfToWpfBridge.ActRenderer {
	/// <summary>
	/// Interaction logic for FrameRenderer.xaml
	/// </summary>
	public partial class FrameRenderer : UserControl, IDisposable {
		// Private fields
		protected List<DrawingComponent> _components = new List<DrawingComponent>();
		protected Point _relativeCenter = new Point(0.5, 0.8);
		protected ZoomEngine _zoomEngine = new ZoomEngine();
		protected List<IDrawingModule> _drawingModules = new List<IDrawingModule>();
		private DrawSlotManager _drawSlotManager;
		private BitmapResourceManager _bitmapResourceManager = new BitmapResourceManager();
		private int _drawIndex;
		private bool _disposed;

		public delegate void RenderUpdateEventHandler(object sender);

		public event RenderUpdateEventHandler RenderUpdate;

		public FrameRendererConfiguration ActRendererConfiguration;

		// Properties
		/// <summary>
		/// Gets the background grid for events.
		/// </summary>
		public Grid GridBackground => _gridBackground;

		public IFrameRendererEditor Editor { get; set; }

		/// <summary>
		/// Gets the drawing modules. The drawing modules are used to retrieve a list of all the DrawingComponents and draw them in order.
		/// </summary>
		/// <value>
		/// The drawing modules.
		/// </value>
		public List<IDrawingModule> DrawingModules => _drawingModules;

		/// <summary>
		/// Gets the main ActDraw drawing component, useful to ignore the ActDraw references.
		/// </summary>
		/// <value>
		/// The main drawing component.
		/// </value>
		public ActDraw MainDrawingComponent => _components.OfType<ActDraw>().FirstOrDefault(p => p.Primary);

		public FrameRendererEdit Edit { get; set; }

		// Events
		public delegate void ZoomChangedDelegate(object sender, double scale);
		public delegate void ViewerMovedDelegate(object sender, Point relativeCenter);
		
		public event MouseButtonEventHandler FrameMouseUp;
		public event ViewerMovedDelegate ViewerMoved;
		public event ZoomChangedDelegate ZoomChanged;

		public virtual void OnViewerMoved(Point relativecenter) {
			ViewerMovedDelegate handler = ViewerMoved;
			if (handler != null) handler(this, relativecenter);
		}

		public virtual void OnZoomChanged(double scale) {
			ZoomChangedDelegate handler = ZoomChanged;
			if (handler != null) handler(this, scale);
		}

		public virtual void OnFrameMouseUp(MouseButtonEventArgs e) {
			MouseButtonEventHandler handler = FrameMouseUp;
			if (handler != null) handler(this, e);
		}

		// Constructor
		public FrameRenderer() {
			InitializeComponent();

			SizeChanged += _renderer_SizeChanged;
			MouseWheel += _renderer_MouseWheel;
			
			Unloaded += _frameRenderer_Unloaded;
		}

		private void _frameRenderer_Unloaded(object sender, RoutedEventArgs e) {
			Editor?.IndexSelector?.Stop();
		}

		public virtual void Init(IFrameRendererEditor editor, FrameRendererConfiguration configuration) {
			ActRendererConfiguration = configuration;
			_drawSlotManager = new DrawSlotManager(Canvas, ActRendererConfiguration);

			//_primary.Background = new SolidColorBrush(ActRendererConfiguration.ActEditorBackgroundColor);

			Editor = editor;
			
			Editor.IndexSelector.FrameChanged += (e) => Update();
			Editor.IndexSelector.SpecialFrameChanged += (e) => Update();
			Editor.IndexSelector.ActionChanged += (e) => Update();
			
			Edit = new FrameRendererEdit(this, Editor);

			_components.Add(new LineDraw(Editor.FrameRenderer));
		}

		public void OnRenderUpdate() {
			RenderUpdateEventHandler handler = RenderUpdate;
			if (handler != null) handler(this);
		}

		public virtual void SizeUpdate() {
			View = ComputeViewMatrix();
			_updateBackground();
			
			foreach (var dc in _components) {
				dc.QuickRender(this);
			}
		}

		public virtual void Update() {
			_internalRequestUpdate();
		}

		private bool _updatePending = false;

		private void _internalRequestUpdate() {
			if (_updatePending)
				return;

			_updatePending = true;

			Dispatcher.BeginInvoke(new System.Action(delegate {
				_updatePending = false;
				_update();
				OnRenderUpdate();
			}), DispatcherPriority.Render);
		}

		public Matrix ComputeViewMatrix() {
			Matrix matrix = Matrix.Identity;
			matrix.ScaleAt(ZoomEngine.Scale, ZoomEngine.Scale, CenterX, CenterY);
			matrix.Translate(CenterX * ZoomEngine.Scale, CenterY * ZoomEngine.Scale);
			return matrix;
		}

		private void _update() {
			if (_components == null)
				return;
			
			_updateBackground();
			
			View = ComputeViewMatrix();
			
			DrawSlotManager.Begin();
			while (_components.Count > 1) {
				_components[1].Remove(this);
				_components.RemoveAt(1);
			}
			
			foreach (var components in _drawingModules.OrderBy(p => p.DrawingPriority)) {
				_components.AddRange(components.GetComponents());
			}
			
			DrawIndex = 0;
			foreach (var dc in _components) {
				dc.Render(this);
			}
			DrawIndex = -1;
			DrawSlotManager.End();
		}

		public void Update(int layerIndex) {
			var comp = _components.OfType<ActDraw>().FirstOrDefault(p => p.Primary);
			
			if (comp != null && layerIndex < comp.Components.Count) {
				var layerDraw = (LayerDraw)comp.Components[layerIndex];
				DrawSlotManager.Begin(layerDraw.LastDrawIndex);
				comp.Remove(this, layerIndex);
				comp.Render(this, layerIndex);
				DrawSlotManager.End(layerDraw.LastDrawIndex);
			}
		}

		private Rect _oldFrameDimensions;
		private double _oldZoom = -1;

		protected virtual void _updateBackground() {
			try {
				Rect frameDimensions = new Rect(_relativeCenter.X, _relativeCenter.Y, _gridBackground.ActualWidth, _gridBackground.ActualHeight);
			
				if (frameDimensions == _oldFrameDimensions && _oldZoom == ZoomEngine.Scale)
					return;
			
				const double imageSize = 16d;
				double xUnits = imageSize;
				double yUnits = imageSize;
			
				if (ZoomEngine.Scale >= 0.45) {
					xUnits *= ZoomEngine.Scale;
					yUnits *= ZoomEngine.Scale;
				}
			
				double x = SnapToDevicePixels(_relativeCenter.X * _gridBackground.ActualWidth);
				double y = SnapToDevicePixels(_relativeCenter.Y * _gridBackground.ActualHeight);
				
				_imBrush.Viewport = new Rect(x, y, xUnits, yUnits);
				_oldFrameDimensions = frameDimensions;
				_oldZoom = ZoomEngine.Scale;
			}
			catch {
			}
		}

		private double SnapToDevicePixels(double value) {
			double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
			return Math.Round(value * dpiScale) / dpiScale;
		}

		private void _renderer_SizeChanged(object sender, SizeChangedEventArgs e) {
			SizeUpdate();
		}

		private bool _dummy = false;

		private void _renderer_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) return;
			
			if (!_dummy) {
				ZoomEngine.ZoomFunction = ZoomEngine.DefaultLimitZoom;
				_dummy = true;
			}
			
			ZoomEngine.Zoom(e.Delta);
			
			Point mousePosition = e.GetPosition(_primary);
			
			// The relative center must be moved as well!
			double diffX = mousePosition.X / _primary.ActualWidth - _relativeCenter.X;
			double diffY = mousePosition.Y / _primary.ActualHeight - _relativeCenter.Y;
			
			_relativeCenter.X = mousePosition.X / _primary.ActualWidth - diffX / ZoomEngine.OldScale * ZoomEngine.Scale;
			_relativeCenter.Y = mousePosition.Y / _primary.ActualHeight - diffY / ZoomEngine.OldScale * ZoomEngine.Scale;
			
			_cbZoom.SelectedIndex = -1;
			_cbZoom.Text = _zoomEngine.ScaleText;
			OnZoomChanged(ZoomEngine.Scale);
			SizeUpdate();
		}

		private void _cbZoom_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_cbZoom.SelectedIndex < 0) return;
			
			_zoomEngine.SetZoom(double.Parse(((string)((ComboBoxItem)_cbZoom.SelectedItem).Content).Replace(" %", "")) / 100f);
			_cbZoom.Text = _zoomEngine.ScaleText;
			OnZoomChanged(_zoomEngine.Scale);
			SizeUpdate();
		}

		private void _cbZoom_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				try {
					string text = _cbZoom.Text;
			
					text = text.Replace(" ", "").Replace("%", "");
					_cbZoom.SelectedIndex = -1;
			
					double value = double.Parse(text);
			
					_zoomEngine.SetZoom(value / 100f);
					_cbZoom.Text = _zoomEngine.ScaleText;
					SizeUpdate();
					OnZoomChanged(_zoomEngine.Scale);
					e.Handled = true;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		private void _cbZoom_MouseEnter(object sender, MouseEventArgs e) {
			_cbZoom.Opacity = 1;
			_cbZoom.StaysOpenOnEdit = true;
		}

		private void _cbZoom_MouseLeave(object sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Released)
				_cbZoom.Opacity = 0.7;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (!_disposed) {
				if (disposing) {
					foreach (var dc in _components) {
						dc.Unload(this);
					}
			
					DrawSlotManager.Unload();
					_bitmapResourceManager.ClearCache();
					_components = null;
				}
			
				_disposed = true;
			}
		}

		public Act Act => Editor.Act;
		public int SelectedAction => Editor.SelectedAction;
		public int SelectedFrame => Editor.SelectedFrame;
		public int CenterX => (int)(_primary.ActualWidth * _relativeCenter.X);
		public int CenterY => (int)(_primary.ActualHeight * _relativeCenter.Y);
		public ZoomEngine ZoomEngine => _zoomEngine;
		public Canvas Canvas => _primary;
		public virtual List<DrawingComponent> Components => _components;
		public DrawSlotManager DrawSlotManager => _drawSlotManager;
		public int DrawIndex { get => _drawIndex; set => _drawIndex = value; }
		public BitmapResourceManager BitmapResourceManager => _bitmapResourceManager;
		public Point RelativeCenter { get => _relativeCenter; set => _relativeCenter = value; }
		public Matrix View;
	}
}
