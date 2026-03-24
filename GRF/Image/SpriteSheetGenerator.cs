using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GRF.FileFormats.ActFormat;
using GRF.Graphics;
using Utilities;
using Utilities.IndexProviders;
using Action = GRF.FileFormats.ActFormat.Action;

namespace GRF.Image {
	public struct Margin {
		public double Left { get; set; }
		public double Right { get; set; }
		public double Top { get; set; }
		public double Bottom { get; set; }

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
			Act head = headSource == null ? null : new Act(headSource);
			Act shadowAct = new Act();

			shadowAct.Actions.Add(new Action());
			shadowAct[0].Frames.Add(new Frame());
			shadowAct[0, 0].Layers.Add(new Layer(new GRF.FileFormats.SprFormat.SpriteIndex(0, GrfImageType.Indexed8)));
			shadowAct.Sprite.InsertAny(settings.Shadow);

			if (head != null)
				head[actionIndex, 0].Layers = head[actionIndex, 0].Layers.Where(p => p.SpriteIndex >= 0).ToList();

			Layer headLayer = null;

			if (head != null) {
				headLayer = _getVisibleHead(head, settings.ActionIndex);

				var bodyFrame = body[settings.ActionIndex, 0];

				headLayer.OffsetX = headLayer.OffsetX + (bodyFrame.Anchors.Count > 0 ? bodyFrame.Anchors[0].OffsetX : 0) - head[settings.ActionIndex, 0].Anchors[0].OffsetX;
				headLayer.OffsetY = headLayer.OffsetY + (bodyFrame.Anchors.Count > 0 ? bodyFrame.Anchors[0].OffsetY : 0) - head[settings.ActionIndex, 0].Anchors[0].OffsetY;
			}

			Plane planeHead = (settings.ShowHead && head != null) ? head[settings.ActionIndex, 0, 0].ToPlane(head) : null;
			Plane planeBody = settings.ShowBody ? body[settings.ActionIndex, 0, 0].ToPlane(body) : null;
			Plane planeShadow = settings.ShowShadow ? shadowAct[0, 0, 0].ToPlane(shadowAct) : null;

			if (planeShadow != null) {
				for (int index = 0; index < planeShadow.Points.Length; index++) {
					planeShadow.Points[index].Y += 1;
				}
			}

			Margin borderHead = _calculateMargin(planeHead, planeBody, planeShadow);
			Margin borderBody = _calculateMargin(planeBody, planeHead, planeShadow);

			Layer bodyLayer = body[actionIndex, 0, 0];

			GrfImage bodyImage = bodyLayer.GetImage(body.Sprite);
			GrfImage headImage = head != null ? headLayer.GetImage(head.Sprite) : null;
			GrfImage font = settings.Font;
			GrfImage shadow = settings.Shadow;

			if (body[settings.ActionIndex, 0, 0].Mirror) {
				bodyImage.Flip(FlipDirection.Horizontal);
			}

			if (head != null && head[settings.ActionIndex, 0, 0].Mirror) {
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

			List<int> fSizes = new List<int>();
			int fHeight = font.Height;

			int height = (int)(settings.ShowBody ? borderBody.Top + borderBody.Bottom + bodyImage.Height : borderHead.Top + borderHead.Bottom + headImage?.Height);
			int width = (int)(settings.ShowBody ? borderBody.Left + borderBody.Right + bodyImage.Width : borderHead.Left + borderHead.Right + headImage?.Width);
			int total = 0;

			List<GrfImage> palIds = new List<GrfImage>();

			if (font.GetColor(0) == new GrfColor(255, 255, 0, 255)) {
				GrfColor lastColor = new GrfColor(255, 255, 0, 255);
				int startX = 0;

				for (int xx = 0; xx < font.Width; xx++) {
					var current = font.GetColor(xx);
					font.SetPixelTransparent(xx, 0);

					if (current != lastColor) {
						palIds.Add(font.Extract(startX, 0, xx - startX, fHeight));
						fSizes.Add(xx - startX);
						lastColor = current;
						startX = xx;
					}

					if (palIds.Count == 10)
						break;
				}
			}
			else {
				int fSize = font.Width / 10;

				for (int i = 0; i < 10; i++) {
					palIds.Add(font.Extract(fSize * i, 0, fSize, fHeight));
					fSizes.Add(fSize);
				}
			}

			int imWidth = 1;
			int imHeight = 1;
			int indexesCount = indexProvider.GetIndexes().Count;

			if (max > indexesCount)
				max = indexesCount;

			imWidth = max * (width + settings.Margin) - ((max > 1) ? settings.Margin : 0);

			int rows = (int)Math.Ceiling(indexesCount / (double)max);
			int palIdHeight = settings.ShowPalIndex ? Math.Max(fHeight + 1, settings.Margin) : settings.Margin;
			int palIdHeightLast = settings.ShowPalIndex ? fHeight : 0;
			imHeight = rows * height + (rows - 1) * palIdHeight + palIdHeightLast;

			byte[] image = new byte[imWidth * imHeight * 4];
			GrfImage mega = new GrfImage(image, imWidth, imHeight, GrfImageType.Bgra32);
			List<int> indexes = indexProvider.GetIndexes().ToList();

			Dictionary<int, byte[]> bytePalette = new Dictionary<int, byte[]>();

			for (int i = 0; i < indexes.Count; i++) {
				int idx = indexes[i];

				if (idx == 0)
					bytePalette[idx] = body.Sprite.Palette.BytePalette;
				else {
					bytePalette[idx] = palMethod(idx);
				}
			}


			if (!settings.TransparentBackground) {
				mega.Fill(255);
			}

			Parallel.For(0, indexes.Count, ii => {
				int x = (ii % max) * (width + settings.Margin);
				int y = (ii / max) * (height + palIdHeight);

				int palIndex = indexes[ii];
				byte[] p = bytePalette[palIndex];

				if (p == null)
					return;

				if (p.Length == 1024) {
					unsafe {
						fixed (byte* pPalette = p) {
							byte* pDst = pPalette + 7;
							byte* pEnd = pPalette + p.Length;

							while (pDst < pEnd) {
								*pDst = 255;
								pDst += 4;
							}
						}
					}
				}

				var cBodyImage = bodyImage;

				if (settings.BodyAffected) {
					cBodyImage = bodyImage.Copy();
					cBodyImage.SetPalette(p);
					cBodyImage.Convert(GrfImageType.Bgra32);
				}

				var cHeadImage = headImage;

				if (settings.HeadAffected && head != null) {
					cHeadImage = headImage.Copy();
					cHeadImage.SetPalette(p);
					cHeadImage.Convert(GrfImageType.Bgra32);
				}

				if (settings.ShowShadow)
					mega.SetPixelsUnrestricted((int)(x + borderBody.Left - bodyLayer.OffsetX + bodyImage.Width / 2f - shadow.Width / 2f), (int)(y + borderBody.Top - bodyLayer.OffsetY + bodyImage.Height / 2f - shadow.Height / 2f), shadow, true);

				if (settings.ShowBody)
					mega.SetPixelsUnrestricted((int)(x + borderBody.Left), (int)(y + borderBody.Top), cBodyImage, true);

				if (settings.ShowHead && cHeadImage != null)
					mega.SetPixelsUnrestricted((int)(x + borderHead.Left), (int)(y + borderHead.Top), cHeadImage, true);

				if (settings.ShowPalIndex) {
					string pid = String.Format(settings.PalIndexFormat, palIndex);
					int startOffset = (width - pid.Length * fSizes[0]) / 2;
					int offset = startOffset;

					for (int j = 0; j < pid.Length; j++) {
						mega.SetPixelsUnrestricted(x + offset, y + height, palIds[pid[j] - '0'], true);

						offset += fSizes[pid[j] - '0'];
					}
				}

				total++;
			});

			return mega;
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
