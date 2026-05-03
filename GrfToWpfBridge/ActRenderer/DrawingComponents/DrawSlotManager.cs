using GrfToWpfBridge.ActRenderer;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GrfToWpfBridge.DrawingComponents {
	public class DrawSlot {
		public Image Image = new Image();
		public bool InUse;
		public bool IsConfigured;
	}

	public class DrawSlotManager {
		private readonly Canvas _canvas;
		private readonly FrameRendererConfiguration _rendererConfiguration;

		public DrawSlotManager(Canvas canvas, FrameRendererConfiguration rendererConfiguration) {
			_canvas = canvas;
			_rendererConfiguration = rendererConfiguration;
		}

		public List<DrawSlot> DrawSlots = new List<DrawSlot>();

		public void Begin() {
			foreach (var drawSlot in DrawSlots) {
				drawSlot.InUse = false;
			}
		}

		public void Begin(int drawIndex) {
			var drawSlot = GetDrawSlot(drawIndex);
			drawSlot.InUse = false;
		}

		public void End() {
			foreach (var drawSlot in DrawSlots) {
				if (!drawSlot.InUse) {
					if (drawSlot.Image.Visibility != Visibility.Hidden)
						drawSlot.Image.Visibility = Visibility.Hidden;
				}
			}
		}

		public void End(int drawIndex) {
			var drawSlot = GetDrawSlot(drawIndex);
			
			if (!drawSlot.InUse) {
				if (drawSlot.Image.Visibility != Visibility.Hidden)
					drawSlot.Image.Visibility = Visibility.Hidden;
			}
		}

		public DrawSlot GetDrawSlot(int drawIndex) {
			if (drawIndex >= DrawSlots.Count) {
				var edgeMode = _rendererConfiguration.UseAliasing ? EdgeMode.Aliased : EdgeMode.Unspecified;
				
				while (drawIndex >= DrawSlots.Count) {
					DrawSlot drawSlot = new DrawSlot();
					DrawSlots.Add(drawSlot);
					drawSlot.Image.Visibility = Visibility.Hidden;
				
					drawSlot.Image.VerticalAlignment = VerticalAlignment.Top;
					drawSlot.Image.HorizontalAlignment = HorizontalAlignment.Left;
					drawSlot.Image.SnapsToDevicePixels = true;
					drawSlot.Image.SetValue(RenderOptions.BitmapScalingModeProperty, _rendererConfiguration.ActEditorScalingMode);
				
					_canvas.Children.Add(drawSlot.Image);
				}
			}

			{
				var drawSlot = DrawSlots[drawIndex];
				drawSlot.InUse = true;
				Panel.SetZIndex(drawSlot.Image, 4 * drawIndex + 10);
				return drawSlot;
			}
		}

		public void ImagesDirty() {
			var scalingMode = _rendererConfiguration.ActEditorScalingMode;

			foreach (var drawSlot in DrawSlots) {
				drawSlot.Image.SetValue(RenderOptions.BitmapScalingModeProperty, scalingMode);
			}
		}

		public void Unload() {
			DrawSlots.Clear();
			_canvas.Children.Clear();
		}
	}
}
