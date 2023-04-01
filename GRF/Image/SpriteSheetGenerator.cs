using System;
using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.ActFormat;
using GRF.Graphics;

namespace GRF.Image {
	public class Margin {
		public double Left { get; set; }
		public double Right { get; set; }
		public double Top { get; set; }
		public double Bottom { get; set; }

		public Margin() {
		}

		public Margin(double value) {
			Left = value;
			Right = value;
			Bottom = value;
			Top = value;
		}

		public Margin(double left, double top, double right, double bottom) {
			Left = left;
			Right = right;
			Bottom = bottom;
			Top = top;
		}
	}

	public class GeneratorSettings {
		public int Start { get; set; }
		public int ActionIndex { get; set; }
		public int Range { get; set; }
		public int MaxPerLine { get; set; }
		public bool ShowPalIndex { get; set; }
		public bool ShowBody { get; set; }
		public bool ShowHead { get; set; }
		public bool HeadAffected { get; set; }
		public bool BodyAffected { get; set; }
		public GrfImage Font { get; set; }
	}

	/// <summary>
	/// Generates sheets for sprites using a range of palettes
	/// </summary>
	public class SpriteSheetGenerator {
		public static void GeneratePreviewSheet(Act bodySource, Act head, GeneratorSettings settings, Func<int, byte[]> palMethod) {
			int start = settings.Start;
			int to = start + settings.Range;
			int max = settings.MaxPerLine;
			int actionIndex = settings.ActionIndex;

			// Copy the inputs
			Act body = new Act(bodySource);

			GrfImage mega = GrfImage.Empty(GrfImageType.Bgr32);
			var x = 0;
			var y = 0;

			var headLayerBottom = _getVisibleHead(head, 0);
			var headLayerLeft = _getVisibleHead(head, 1);

			GrfImage headImageBottom = headLayerBottom.GetImage(head.Sprite);
			GrfImage headImageLeft = headLayerLeft.GetImage(head.Sprite);

			headImageBottom.SetPalette(body.Sprite.Palette.BytePalette);
			headImageLeft.SetPalette(body.Sprite.Palette.BytePalette);

			int id = body.Sprite.InsertAny(headImageBottom);
			body.Sprite.InsertAny(headImageLeft);

			body[actionIndex, 0].Layers.Add(new Layer(headLayerBottom));

			var layerBottom = body[actionIndex, 0].Layers.Last();

			layerBottom.SetAbsoluteSpriteId(id, body.Sprite);

			layerBottom.OffsetX = headLayerBottom.OffsetX + body[actionIndex, 0].Anchors[0].OffsetX - head[actionIndex, 0].Anchors[0].OffsetX;
			layerBottom.OffsetY = headLayerBottom.OffsetY + body[actionIndex, 0].Anchors[0].OffsetY - head[actionIndex, 0].Anchors[0].OffsetY;

			Margin borderHead = _calculateMargin(body, body[actionIndex, 0], 1, settings);
			Margin borderBody = _calculateMargin(body, body[actionIndex, 0], 0, settings);
			Layer bodyLayer = body[actionIndex, 0, 0];
			Layer headLayer = body[actionIndex, 0, 1];

			GrfImage bodyImage = bodyLayer.GetImage(body.Sprite);
			GrfImage headImage = headLayer.GetImage(body.Sprite);
			GrfImage font = settings.Font;

			int fSize = font.Width / 10;
			int fHeight = font.Height;

			const int Margin = 5;

			int height = (int) (settings.ShowBody ? borderBody.Top + borderBody.Bottom + bodyImage.Height : borderHead.Top + borderHead.Bottom + headImage.Height);
			int width = (int) (settings.ShowBody ? borderBody.Left + borderBody.Right + bodyImage.Width : borderHead.Left + borderHead.Right + headImage.Width);

			for (int i = start; i < to; i++) {
				if ((i - start) % max == 0 && i != 0) {
					x = 0;

					if (settings.ShowPalIndex) {
						y = mega.Height + 1;
					}
					else {
						y = mega.Height + Margin;
					}
				}

				byte[] p = palMethod(i);

				if (settings.BodyAffected)
					bodyImage.SetPalette(p);

				if (settings.HeadAffected)
					headImage.SetPalette(p);

				if (settings.ShowBody)
					mega.SetPixelsUnrestricted((int) (x + borderBody.Left), (int) (y + borderBody.Top), bodyImage, false);

				if (settings.ShowHead)
					mega.SetPixelsUnrestricted((int) (x + borderHead.Left), (int) (y + borderHead.Top), headImage, true);

				if (settings.ShowPalIndex) {
					string pid = String.Format("{0:000}", i);

					int startOffset = (width - pid.Length * fSize) / 2;

					for (int j = 0; j < pid.Length; j++) {
						mega.SetPixelsUnrestricted(x + j * fSize + startOffset, y + height, font.Extract(fSize * (pid[j] - '0'), 0, fSize, fHeight));
					}
				}

				x += (int) (borderHead.Left + headImage.Width + borderHead.Right + Margin);
			}
		}

		private static Margin _calculateMargin(Act act, Frame frame, int layerIndex, GeneratorSettings settings) {
			List<Plane> planes = new List<Plane>();
			Plane layerPlane = null;
			BoundingBox box = new BoundingBox();

			for (int index = 0; index < frame.Layers.Count; index++) {
				if (!settings.ShowBody && index == 0)
					continue;

				if (!settings.ShowHead && index == 1)
					continue;

				Layer layer = frame.Layers[index];

				if (layer.SpriteIndex < 0)
					continue;

				GrfImage img = act.Sprite.Images[layer.SpriteTypeInt == 1 ? layer.SpriteIndex + act.Sprite.NumberOfIndexed8Images : layer.SpriteIndex];
				Plane plane = new Plane(img.Width, img.Height);

				plane.ScaleX(layer.ScaleX * (layer.Mirror ? -1f : 1f));
				plane.ScaleY(layer.ScaleY);
				plane.RotateZ(layer.Rotation);
				plane.Translate(layer.OffsetX + (layer.Mirror ? -(img.Width + 1) % 2 : 0), layer.OffsetY);

				planes.Add(plane);

				if (index == layerIndex)
					layerPlane = plane;
			}

			box.Max[0] = _awayRounding(planes.Max(p => p.Points.Max(q => q.X)));
			box.Max[1] = _awayRounding(planes.Max(p => p.Points.Max(q => q.Y)));
			box.Min[0] = _awayRounding(planes.Min(p => p.Points.Min(q => q.X)));
			box.Min[1] = _awayRounding(planes.Min(p => p.Points.Min(q => q.Y)));

			box.Center[0] = (box.Max[0] - box.Min[0]) / 2f + box.Min[0];
			box.Center[1] = (box.Max[1] - box.Min[1]) / 2f + box.Min[1];
			box.Center[0] = _awayRounding((2 * box.Center[0] + 1) / 2);
			box.Center[1] = _awayRounding((2 * box.Center[1] + 1) / 2);
			box.Range[0] = box.Max[0] - box.Center[0];
			box.Range[1] = box.Max[1] - box.Center[1];

			if (layerPlane != null) {
				return new Margin(
					Math.Abs(box.Min[0] - layerPlane.Points.Min(q => q.X)),
					Math.Abs(box.Min[1] - layerPlane.Points.Min(q => q.Y)),
					Math.Abs(box.Max[0] - layerPlane.Points.Max(q => q.X)),
					Math.Abs(box.Max[1] - layerPlane.Points.Max(q => q.Y))
				);
			}

			return new Margin();
		}

		private static float _awayRounding(float max) {
			return max < 0 ? (float) Math.Floor(max) : (float) Math.Ceiling(max);
		}

		private static Layer _getVisibleHead(Act head, int i) {
			return head[i, 0].FirstOrDefault(l => l.SpriteIndex >= 0);
		}
	}
}
