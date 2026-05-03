using System;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using TokeiLibrary;
using Point = System.Windows.Point;

namespace GrfToWpfBridge.ActRenderer {
	public class FrameRendererTransformInput {
		public bool KeyboardTranslated { get; set; }
		public bool Moved { get; set; }
		public bool Scaled { get; set; }
		public bool Rotated { get; set; }
		public bool Translated { get; set; }
		public bool TransformEnabled { get; set; }
		public bool AnyMouseDown { get; set; }
		public Point BeforeTransformMousePosition { get; set; }
		public Point LatestTransformMousePosition { get; set; }
		public ScaleDirection? FavoriteOrientation { get; set; }
	}

	public enum ScaleDirection {
		Horizontal,
		Vertical,
		Both
	}

	public class FrameRendererEdit {
		private readonly FrameRenderer _renderer;
		private readonly IFrameRendererEditor _editor;
		private readonly FrameRendererTransformInput _frti = new FrameRendererTransformInput();
		public bool EnableEdit { get; set; }
		
		public FrameRendererEdit(FrameRenderer renderer, IFrameRendererEditor editor) {
			EnableEdit = false;
			_renderer = renderer;
			_editor = editor;

			_renderer.MouseMove += new MouseEventHandler(_renderer_MouseMove);
			_renderer.MouseDown += new MouseButtonEventHandler(_renderer_MouseDown);
			_renderer.MouseUp += new MouseButtonEventHandler(_renderer_MouseUp);
		}

		private void _renderer_MouseDown(object sender, MouseButtonEventArgs e) {
			try {
				_frti.AnyMouseDown = true;
				_frti.Moved = false;

				if (Keyboard.FocusedElement != _renderer._cbZoom)
					Keyboard.Focus(_editor.GridPrimary);

				_frti.BeforeTransformMousePosition = e.GetPosition(_renderer);
				_frti.FavoriteOrientation = null;

				if (_frti.Translated || _frti.Scaled || _frti.Rotated) {
					e.Handled = true;
					return;
				}

				if (e.RightButton == MouseButtonState.Pressed) {
					_frti.TransformEnabled = true;
					_renderer.CaptureMouse();
				}

				_frti.LatestTransformMousePosition = _frti.BeforeTransformMousePosition;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _renderer_MouseMove(object sender, MouseEventArgs e) {
			try {
				if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
					return;

				if (_renderer.ContextMenu != null && _renderer.ContextMenu.IsOpen) return;

				Point current = e.GetPosition(_renderer);

				double deltaX = current.X - _frti.BeforeTransformMousePosition.X;
				double deltaY = current.Y - _frti.BeforeTransformMousePosition.Y;

				if (deltaX == 0 && deltaY == 0)
					return;

				if (_renderer.GetObjectAtPoint<ComboBox>(e.GetPosition(_renderer)) == _renderer._cbZoom && !_renderer.IsMouseCaptured)
					return;

				var distPoint = Point.Subtract(current, _frti.LatestTransformMousePosition);

				if (_frti.FavoriteOrientation == null) {
					_frti.FavoriteOrientation = distPoint.X * distPoint.X > distPoint.Y * distPoint.Y ? ScaleDirection.Horizontal : ScaleDirection.Vertical;
				}

				if (e.RightButton == MouseButtonState.Pressed && _frti.AnyMouseDown) {
					_renderer.RelativeCenter = new Point(
						_renderer.RelativeCenter.X + deltaX / _renderer.Canvas.ActualWidth,
						_renderer.RelativeCenter.Y + deltaY / _renderer.Canvas.ActualHeight);

					_frti.BeforeTransformMousePosition = current;
					_renderer.SizeUpdate();
					_renderer.OnViewerMoved(_renderer.RelativeCenter);
					_frti.Moved = true;
				}

				if (_editor.IndexSelector.IsPlaying) return;

				_frti.LatestTransformMousePosition = current;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _renderer_MouseUp(object sender, MouseButtonEventArgs e) {
			try {
				_frti.AnyMouseDown = false;

				if (_renderer.GetObjectAtPoint<ComboBox>(e.GetPosition(_renderer)) != _renderer._cbZoom)
					e.Handled = true;

				_frti.TransformEnabled = false;

				_renderer.OnFrameMouseUp(e);
				_renderer.ReleaseMouseCapture();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private double _getDistance(Point p1, Point p2) {
			return Point.Subtract(p2, p1).Length;
		}
	}
}
