using System;
using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.ActFormat;
using GRF.Graphics;
using Utilities.IndexProviders;

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
		private bool _showShadow;
		public int ActionIndex { get; set; }
		public int MaxPerLine { get; set; }
		public bool ShowPalIndex { get; set; }
		public bool ShowBody { get; set; }
		public bool ShowHead { get; set; }

		public bool ShowShadow {
			get {
				if (!ShowBody)
					return false;

				return _showShadow;
			}
			set { _showShadow = value; }
		}

		public bool HeadAffected { get; set; }
		public bool BodyAffected { get; set; }
		public bool TransparentBackground { get; set; }
		public GrfImage Font { get; set; }
		public GrfImage Shadow { get; set; }
		public int Margin { get; set; }
		public string PalIndexFormat { get; set; }

		public GeneratorSettings() {
			Margin = 5;
			PalIndexFormat = "{0:000}";
		}
	}

	/// <summary>
	/// Generates sheets for sprites using a range of palettes
	/// </summary>
	public class SpriteSheetGenerator {
		public static GrfImage GeneratePreviewSheet(Act bodySource, Act headSource, GeneratorSettings settings, string paletteRange, Func<int, byte[]> palMethod) {
			QueryIndexProvider indexProvider = new QueryIndexProvider(paletteRange);
			int max = settings.MaxPerLine;
			int actionIndex = settings.ActionIndex;

			// Copy the inputs
			Act body = new Act(bodySource);
			Act head = new Act(headSource);
			Act shadowAct = new Act();

			var x = 0;
			var y = 0;

			shadowAct.Actions.Add(new FileFormats.ActFormat.Action());
			shadowAct[0].Frames.Add(new Frame());
			shadowAct[0, 0].Layers.Add(new Layer());
			shadowAct[0, 0, 0].SpriteIndex = 0;
			shadowAct.Sprite.AddImage(settings.Shadow);

			head[actionIndex, 0].Layers = head[actionIndex, 0].Layers.Where(p => p.SpriteIndex >= 0).ToList();

			var headLayer = _getVisibleHead(head, settings.ActionIndex);
			headLayer.OffsetX = headLayer.OffsetX + body[settings.ActionIndex, 0].Anchors[0].OffsetX - head[settings.ActionIndex, 0].Anchors[0].OffsetX;
			headLayer.OffsetY = headLayer.OffsetY + body[settings.ActionIndex, 0].Anchors[0].OffsetY - head[settings.ActionIndex, 0].Anchors[0].OffsetY;

			Plane planeHead = settings.ShowHead ? _calculatePlane(head, head[settings.ActionIndex, 0]) : null;
			Plane planeBody = settings.ShowBody ? _calculatePlane(body, body[settings.ActionIndex, 0]) : null;
			Plane planeShadow = settings.ShowShadow ? _calculatePlane(shadowAct, shadowAct[0, 0]) : null;

			if (planeShadow != null) {
				for (int index = 0; index < planeShadow.Points.Length; index++) {
					planeShadow.Points[index].Y += 1;
				}
			}

			Margin borderHead = _calculateMargin(planeHead, planeBody, planeShadow);
			Margin borderBody = _calculateMargin(planeBody, planeHead, planeShadow);

			Layer bodyLayer = body[actionIndex, 0, 0];

			GrfImage bodyImage = bodyLayer.GetImage(body.Sprite);
			GrfImage headImage = headLayer.GetImage(head.Sprite);
			GrfImage font = settings.Font;
			GrfImage shadow = settings.Shadow;

			if (body[settings.ActionIndex, 0, 0].Mirror) {
				bodyImage.Flip(FlipDirection.Horizontal);
			}

			if (head[settings.ActionIndex, 0, 0].Mirror) {
				headImage.Flip(FlipDirection.Horizontal);
			}

			if (settings.ShowShadow) {
				shadow.Convert(GrfImageType.Bgra32);

				for (int i = 0; i < shadow.NumberOfPixels; i++) {
					if (shadow.Pixels[4 * i + 3] != 0) {
						shadow.Pixels[4 * i + 3] = 128;
					}
				}
			}

			int fSize = font.Width / 10;
			int fHeight = font.Height;

			int height = (int)(settings.ShowBody ? borderBody.Top + borderBody.Bottom + bodyImage.Height : borderHead.Top + borderHead.Bottom + headImage.Height);
			int width = (int)(settings.ShowBody ? borderBody.Left + borderBody.Right + bodyImage.Width : borderHead.Left + borderHead.Right + headImage.Width);
			int total = 0;

			List<GrfImage> palIds = new List<GrfImage>();

			for (int i = 0; i < 10; i++) {
				palIds.Add(font.Extract(fSize * i, 0, fSize, fHeight));
			}

			int imWidth = 1;
			int imHeight = 1;
			int indexesCount = indexProvider.GetIndexes().Count;

			{
				if (max > indexesCount)
					max = indexesCount;

				imWidth = max * (width + settings.Margin) - ((max > 1) ? settings.Margin : 0);

				int rows = (int)Math.Ceiling(indexesCount / (double)max);
				int heightGap = settings.ShowPalIndex ? fHeight + 1 + (settings.Margin - fHeight) : settings.Margin;
				imHeight = rows * height + (rows + (settings.ShowPalIndex ? 0 : -1)) * heightGap;
			}

			byte[] image = new byte[imWidth * imHeight * 4];
			GrfImage mega = new GrfImage(ref image, imWidth, imHeight, GrfImageType.Bgra32);

			foreach (var i in indexProvider.GetIndexes()) {
				if (total % max == 0 && total != 0) {
					x = 0;

					if (settings.ShowPalIndex) {
						//y = mega.Height + 1 + (settings.Margin - 5);
						y = y + height + fHeight + 1 + (settings.Margin - fHeight);
					}
					else {
						//y = mega.Height + settings.Margin;
						y = y + height + settings.Margin;
					}
				}

				byte[] p;

				if (i == 0) {
					p = body.Sprite.Palette.BytePalette;
				}
				else {
					p = palMethod(i);
				}

				if (settings.BodyAffected) {
					bodyImage.SetPalette(p);
					for (int k = 4; k < 1024; k += 4) {
						bodyImage.Palette[k + 3] = 255;
					}
				}

				if (settings.HeadAffected) {
					headImage.SetPalette(p);
					for (int k = 4; k < 1024; k += 4) {
						headImage.Palette[k + 3] = 255;
					}
				}

				if (settings.ShowShadow)
					mega.SetPixelsUnrestricted((int)(x + borderBody.Left - bodyLayer.OffsetX + bodyImage.Width / 2f - shadow.Width / 2f), (int)(y + borderBody.Top - bodyLayer.OffsetY + bodyImage.Height / 2f - shadow.Height / 2f), shadow, true);

				if (settings.ShowBody)
					mega.SetPixelsUnrestricted((int)(x + borderBody.Left), (int)(y + borderBody.Top), bodyImage, true);

				if (settings.ShowHead)
					mega.SetPixelsUnrestricted((int)(x + borderHead.Left), (int)(y + borderHead.Top), headImage, true);

				if (settings.ShowPalIndex) {
					string pid = String.Format(settings.PalIndexFormat, i);
					int startOffset = (width - pid.Length * fSize) / 2;

					for (int j = 0; j < pid.Length; j++) {
						mega.SetPixelsUnrestricted(x + j * fSize + startOffset, y + height, palIds[(pid[j] - '0')]);
					}
				}

				x += width + settings.Margin;
				total++;
			}

			if (!settings.TransparentBackground) {
				int bpp = mega.GetBpp();
				int index2 = 0;
				int length = mega.Pixels.Length;

				while (index2 < length) {
					if (mega.Pixels[index2 + 3] == 0) {
						mega.Pixels[index2] = byte.MaxValue;
						mega.Pixels[index2 + 1] = byte.MaxValue;
						mega.Pixels[index2 + 2] = byte.MaxValue;
						mega.Pixels[index2 + 3] = byte.MaxValue;
					}

					index2 += bpp;
				}
			}

			return mega;
		}

		private static Plane _calculatePlane(Act act, Frame frame) {
			Layer layer = frame.Layers[0];
			GrfImage img = layer.GetImage(act.Sprite);
			Plane plane = new Plane(img.Width, img.Height);

			plane.ScaleX(layer.ScaleX * (layer.Mirror ? -1f : 1f));
			plane.ScaleY(layer.ScaleY);
			plane.RotateZ(layer.Rotation);
			plane.Translate(layer.OffsetX + (layer.Mirror ? -(img.Width + 1) % 2 : 0), layer.OffsetY);
			return plane;
		}

		private static Margin _calculateMargin(Plane plane1, Plane plane2, Plane plane3) {
			List<Plane> planes = new List<Plane>();
			BoundingBox box = new BoundingBox();
			if (plane1 != null)
				planes.Add(plane1);
			if (plane2 != null)
				planes.Add(plane2);
			if (plane3 != null)
				planes.Add(plane3);

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

			if (plane1 != null) {
				return new Margin(
					Math.Abs(box.Min[0] - plane1.Points.Min(q => q.X)),
					Math.Abs(box.Min[1] - plane1.Points.Min(q => q.Y)),
					Math.Abs(box.Max[0] - plane1.Points.Max(q => q.X)),
					Math.Abs(box.Max[1] - plane1.Points.Max(q => q.Y))
				);
			}

			return new Margin(
				Math.Abs(box.Min[0]),
				Math.Abs(box.Min[1]),
				Math.Abs(box.Max[0]),
				Math.Abs(box.Max[1])
			);
		}

		private static float _awayRounding(float max) {
			return max < 0 ? (float)Math.Floor(max) : (float)Math.Ceiling(max);
		}

		private static Layer _getVisibleHead(Act head, int i) {
			return head[i, 0].FirstOrDefault(l => l.SpriteIndex >= 0);
		}
	}
}
