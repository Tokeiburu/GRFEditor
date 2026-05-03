using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using GrfToWpfBridge.ActRenderer;
using Frame = GRF.FileFormats.ActFormat.Frame;
using Point = System.Windows.Point;

namespace GrfToWpfBridge.DrawingComponents {
	/// <summary>
	/// Drawing component for a frame's layer.
	/// </summary>
	public class LayerDraw : DrawingComponent {
		private readonly RotateTransform _rotate = new RotateTransform();
		private readonly ScaleTransform _scale = new ScaleTransform();
		private readonly ScaleTransform _mirrorScale = new ScaleTransform();
		private readonly TransformGroup _transformGroup = new TransformGroup();
		private readonly TranslateTransform _translateFrame = new TranslateTransform();
		private readonly TranslateTransform _translateToCenter = new TranslateTransform();

		private readonly FrameRenderer _renderer;
		private Act _act;
		private DrawSlot _drawSlot;
		private Image _image;
		private Layer _layer;
		private GrfImage _lastImage;
		public int LastDrawIndex => _lastDrawIndex;

		public LayerDraw() {
			_transformGroup.Children.Add(_mirrorScale);
			_transformGroup.Children.Add(_translateToCenter);
			_transformGroup.Children.Add(_scale);
			_transformGroup.Children.Add(_rotate);
			_transformGroup.Children.Add(_translateFrame);
		}

		public LayerDraw(FrameRenderer frameRenderer, Act act, int layerIndex) : this() {
			_renderer = frameRenderer;
			_act = act;
			LayerIndex = layerIndex;
		}

		public int LayerIndex { get; private set; }

		public Layer Layer {
			get { return _act.TryGetLayer(_renderer.SelectedAction, _renderer.SelectedFrame, LayerIndex); }
		}

		private void _initDraw(FrameRenderer renderer) {
			if (renderer == null)
				return;

			if (renderer.DrawIndex > -1) {
				_lastDrawIndex = renderer.DrawIndex++;
			}
			
			_drawSlot = renderer.DrawSlotManager.GetDrawSlot(_lastDrawIndex);

			_image = _drawSlot.Image;

			// Image
			{
				var isHitTestVisible = false;

				if (_image.IsHitTestVisible != isHitTestVisible)
					_image.IsHitTestVisible = isHitTestVisible;

				if (_image.Visibility != Visibility.Visible)
					_image.Visibility = Visibility.Visible;
			}

			_drawSlot.IsConfigured = true;
		}

		private BitmapResourceManager.BitmapHandle _handle;
		private int _lastDrawIndex;

		public override void Render(FrameRenderer renderer) {
			_initDraw(renderer);

			Act act = _act ?? renderer.Act;

			int actionIndex = renderer.SelectedAction;
			int frameIndex = renderer.SelectedFrame;
			int? anchorFrameIndex = null;

			if (actionIndex >= act.NumberOfActions) return;
			if (act.Name == "Head" || act.Name == "Body") {
				bool handled = false;

				if (act[actionIndex].NumberOfFrames == 3 &&
				    (0 <= actionIndex && actionIndex < 8) ||
				    (16 <= actionIndex && actionIndex < 24)) {
					if (renderer.Act != null) {
						Act editorAct = renderer.Act;

						int group = editorAct[actionIndex].NumberOfFrames / 3;

						if (group != 0) {
							anchorFrameIndex = frameIndex;

							if (frameIndex < group) {
								frameIndex = 0;
								handled = true;
							}
							else if (frameIndex < 2 * group) {
								frameIndex = 1;
								handled = true;
							}
							else if (frameIndex < 3 * group) {
								frameIndex = 2;
								handled = true;
							}
							else {
								frameIndex = 2;
								handled = true;
							}
						}
					}
				}

				if (!handled) {
					if (frameIndex >= act[actionIndex].NumberOfFrames) {
						if (act[actionIndex].NumberOfFrames > 0)
							frameIndex = frameIndex % act[actionIndex].NumberOfFrames;
						else
							frameIndex = 0;
					}
				}
			}
			else {
				if (frameIndex >= act[actionIndex].NumberOfFrames) {
					if (act[actionIndex].NumberOfFrames > 0)
						frameIndex = frameIndex % act[actionIndex].NumberOfFrames;
					else
						frameIndex = 0;
				}
			}

			Frame frame = act[actionIndex, frameIndex];
			if (LayerIndex >= frame.NumberOfLayers) return;

			_layer = act[actionIndex, frameIndex, LayerIndex];

			if (_layer.SpriteIndex < 0) {
				_image.Source = null;
				return;
			}

			int index = _layer.GetAbsoluteSpriteId(_act.Sprite);

			if (index < 0 || index >= act.Sprite.Images.Count) {
				_image.Source = null;
				return;
			}

			GrfImage img = act.Sprite.Images[index];
			_lastImage = img;

			int diffX = 0;
			int diffY = 0;

			if (act.AnchoredTo != null && frame.Anchors.Count > 0) {
				Frame frameReference;

				if (anchorFrameIndex != null && act.Name != null && act.AnchoredTo.Name != null) {
					frameReference = act.AnchoredTo.TryGetFrame(actionIndex, frameIndex);

					if (frameReference == null) {
						frameReference = act.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex.Value);
					}
				}
				else {
					frameReference = act.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex ?? frameIndex);
				}

				if (frameReference != null && frameReference.Anchors.Count > 0) {
					diffX = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
					diffY = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;

					if (act.AnchoredTo.AnchoredTo != null) {
						frameReference = act.AnchoredTo.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex ?? frameIndex);

						if (frameReference != null && frameReference.Anchors.Count > 0) {
							diffX = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
							diffY = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;
						}
					}
				}
			}

			// UV flip
			_mirrorScale.ScaleX = _layer.Mirror ? -1d : 1d;
			_mirrorScale.ScaleY = 1d;
			_mirrorScale.CenterX = img.Width / 2d;

			_translateToCenter.X = -(img.Width + 1) / 2;
			_translateToCenter.Y = -(img.Height + 1) / 2;

			_scale.ScaleX = _layer.ScaleX;
			_scale.ScaleY = _layer.ScaleY;

			_rotate.Angle = _layer.Rotation;
			_rotate.CenterX = img.Width % 2 == 1 ? -0.5f * _layer.ScaleX : 0;
			_rotate.CenterY = img.Height % 2 == 1 ? -0.5f * _layer.ScaleY : 0;

			_translateFrame.X = _layer.OffsetX + diffX;
			_translateFrame.Y = _layer.OffsetY + diffY;

			_handle = renderer.BitmapResourceManager.GetBitmapHandle(_layer.SprSpriteIndex, act, img, _layer.Color);
			_image.Source = _handle.Bitmap;

			QuickRender(renderer);
		}

		public override void QuickRender(FrameRenderer renderer) {
			if (_image.Visibility != Visibility.Visible)
				return;

			_image.RenderTransform = new MatrixTransform(_transformGroup.Value * renderer.View);
		}

		public void UpdateScaleAndRotationMatrices() {
			if (_lastImage == null)
				return;

			_rotate.Angle = _layer.Rotation;
			_rotate.CenterX = _lastImage.Width % 2 == 1 ? -0.5f * _layer.ScaleX : 0;
			_rotate.CenterY = _lastImage.Height % 2 == 1 ? -0.5f * _layer.ScaleY : 0;

			_scale.ScaleX = _layer.ScaleX;
			_scale.ScaleY = _layer.ScaleY;
		}

		public override void Remove(FrameRenderer renderer) {
		}

		public bool IsMouseUnder(Point point) {
			try {
				if (_scale.ScaleX == 0 || _scale.ScaleY == 0) return false;
				if (_image == null || _image.Parent == null) return false;

				return ReferenceEquals(_image.InputHitTest(_image.PointFromScreen(point)), _image);
			}
			catch {
				return false;
			}
		}
	}
}