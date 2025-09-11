using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GRF.FileFormats.ActFormat;
using GRF.Graphics;
using GRF.Image;
using GRF.Threading;
using Gif.Components;
using TokeiLibrary;
using Utilities.CommandLine;
using Color = System.Drawing.Color;
using Frame = GRF.FileFormats.ActFormat.Frame;
using Point = System.Windows.Point;

namespace ActImaging {
	public static class Imaging {
		private static Color _transparentPixel = Color.FromArgb(255, 255, 0, 255);

		/// <summary>
		/// Gets or sets the transparent pixel used for the background in palettes.
		/// </summary>
		/// <value>
		/// The transparent pixel.
		/// </value>
		public static Color TransparentPixel {
			get { return _transparentPixel; }
			set { _transparentPixel = value; }
		}

		/// <summary>
		/// Generates all frames of the action index for the Act with predefined parameters.
		/// </summary>
		/// <param name="act">The act.</param>
		/// <param name="actionIndex">Index of the action.</param>
		/// <returns></returns>
		public static List<ImageSource> GenerateImages(Act act, int actionIndex) {
			List<ImageSource> images = new List<ImageSource>();

			BoundingBox boundingBox = GenerateBoundingBox(act, actionIndex);

			for (int i = 0; i < act[actionIndex].Frames.Count; i++) {
				images.Add(_generateImage(act, actionIndex, i, true, Colors.Transparent, boundingBox, Configuration.BestAvailableScaleMode));
			}

			return images;
		}

		/// <summary>
		/// Generates the specified frame of the action index for the Act with predefined parameters.
		/// </summary>
		/// <param name="act">The act.</param>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <returns></returns>
		public static ImageSource GenerateImage(Act act, int actionIndex, int frameIndex) {
			BoundingBox boundingBox = GenerateBoundingBox(act, actionIndex);
			return _generateImage(act, actionIndex, frameIndex, true, Colors.Transparent, boundingBox, Configuration.BestAvailableScaleMode);
		}

		public static ImageSource GenerateImage(Act act, int actionIndex, int frameIndex, BitmapScalingMode scalingMode) {
			BoundingBox boundingBox = GenerateBoundingBox(act, actionIndex);
			return _generateImage(act, actionIndex, frameIndex, true, Colors.Transparent, boundingBox, scalingMode);
		}

		public static ImageSource GenerateFrameImage(Act act, Frame frame) {
			BoundingBox boundingBox = GenerateFrameBoundingBox(act, frame);
			return _generateImage(act, frame, false, Colors.Transparent, boundingBox, Configuration.BestAvailableScaleMode);
		}

		public static ImageSource GenerateFrameImage(Act act, int actionIndex, int frameIndex) {
			BoundingBox boundingBox = GenerateFrameBoundingBox(act, actionIndex, frameIndex);
			return _generateImage(act, actionIndex, frameIndex, true, Colors.Transparent, boundingBox, Configuration.BestAvailableScaleMode);
		}

		/// <summary>
		/// Generates all frames of the action index for the Act with advanced parameters.
		/// </summary>
		/// <param name="act">The act.</param>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="uniform">if set to <c>true</c> [uniform].</param>
		/// <param name="guideLineColors">The guide line colors.</param>
		/// <param name="margin">The margin.</param>
		/// <returns></returns>
		public static List<ImageSource> GenerateImages(Act act, int actionIndex, bool uniform, System.Windows.Media.Color guideLineColors, int margin, BitmapScalingMode scalingMode) {
			List<ImageSource> images = new List<ImageSource>();
			BoundingBox boundingBox = GenerateBoundingBox(act, actionIndex, margin);

			for (int i = 0; i < act[actionIndex].Frames.Count; i++) {
				images.Add(_generateImage(act, actionIndex, i, uniform, guideLineColors, boundingBox, scalingMode));
			}

			images.ForEach(p => p.SetValue(RenderOptions.BitmapScalingModeProperty, scalingMode));
			images.ForEach(p => p.Freeze());
			return images;
		}

		/// <summary>
		/// Converts a BitmapSource image to the Gif format, and saves it.
		/// </summary>
		/// <param name="frame">The frame.</param>
		/// <param name="filepath">The filepath.</param>
		public static void SaveAsGif(BitmapSource frame, string filepath) {
			if (Path.GetDirectoryName(filepath) != null && !Directory.Exists(Path.GetDirectoryName(filepath)))
				Directory.CreateDirectory(Path.GetDirectoryName(filepath));

			AnimatedGifEncoder e = new AnimatedGifEncoder();
			e.Start(filepath);
			e.SetRepeat(0);

			Color transparent = TransparentPixel;

			byte[] pixels = new byte[frame.PixelWidth * frame.PixelHeight];
			byte[] palette = WpfImaging.GetBytePaletteRGB(frame.Palette);
			frame.CopyPixels(pixels, frame.PixelWidth, 0);

			e.SetTransparent(transparent);
			e.AddFrame(frame.PixelWidth, frame.PixelHeight, pixels, palette);

			e.Finish();
		}

		/// <summary>
		/// Converts an Act object to an animated Gif, and saves it. Consider using Act.SaveTo(path) from
		/// GrfToWpfBridge's extensions.
		/// </summary>
		/// <param name="filepath">The filepath.</param>
		/// <param name="act">The act.</param>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="prog">The prog.</param>
		/// <param name="extra">The extra.</param>
		/// <exception cref="System.Exception">Action index is greater than the number of actions!</exception>
		public static void SaveAsGif(string filepath, Act act, int actionIndex, IProgress prog, params string[] extra) {
			int indexFrom = 0;
			int indexTo = act[actionIndex].NumberOfFrames;
			bool uniform = true;
			int margin = 0;
			System.Windows.Media.Color background = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
			System.Windows.Media.Color guildeLinesColor = Colors.Transparent;
			BitmapScalingMode scaling = Configuration.BestAvailableScaleMode;
			int delay = (int) Math.Ceiling((act[actionIndex].AnimationSpeed * 25));

			for (int index = 0; index < extra.Length; index++) {
				string param = extra[index];
				if (extra[index + 1] != null) {
					if (param == "indexFrom") {
						indexFrom = Int32.Parse(extra[index + 1]);
					}
					else if (param == "indexTo") {
						indexTo = Int32.Parse(extra[index + 1]);
					}
					else if (param == "uniform") {
						uniform = Boolean.Parse(extra[index + 1]);
					}
					else if (param == "background") {
						var convertFromString = ColorConverter.ConvertFromString(extra[index + 1]);
						if (convertFromString != null) background = (System.Windows.Media.Color) convertFromString;
					}
					else if (param == "guideLinesColor") {
						var convertFromString = ColorConverter.ConvertFromString(extra[index + 1]);
						if (convertFromString != null) guildeLinesColor = (System.Windows.Media.Color) convertFromString;
					}
					else if (param == "scaling") {
						scaling = (BitmapScalingMode) Enum.Parse(typeof (BitmapScalingMode), (extra[index + 1]));
					}
					else if (param == "delay") {
						delay = Int32.Parse(extra[index + 1]);
					}
					else if (param == "delayFactor") {
						delay = (int) (delay * Single.Parse(extra[index + 1], CultureInfo.InvariantCulture));
					}
					else if (param == "margin") {
						margin = Int32.Parse(extra[index + 1], CultureInfo.InvariantCulture);
					}
				}
				index++;
			}

			if (actionIndex > act.NumberOfActions)
				throw new Exception("Action index is greater than the number of actions!");

			List<BitmapFrame> frames = new List<BitmapFrame>();
			List<ImageSource> images = GenerateImages(act, actionIndex, uniform, guildeLinesColor, margin, scaling);

			for (int i = indexFrom; i < indexTo; i++) {
				try {
					frames.Add(_getIndexed8BitmapFrame(images[i], background, scaling));
				}
				catch (Exception err) {
					CLHelper.Error = err.Message;
					throw;
				}

				if (prog != null)
					prog.Progress = (i + 1) / (float) (indexTo - indexFrom + 1) * 100f;

				if (prog != null && prog.IsCancelling) {
					prog.IsCancelled = true;
					return;
				}
			}

			if (frames.Count > 0) {
				if (Path.GetDirectoryName(filepath) != null && !Directory.Exists(Path.GetDirectoryName(filepath)))
					Directory.CreateDirectory(Path.GetDirectoryName(filepath));

				AnimatedGifEncoder e = new AnimatedGifEncoder();
				e.Start(filepath);
				e.SetDelay(delay);
				e.SetRepeat(0);

				Color transparent = TransparentPixel;

				foreach (BitmapFrame frame in frames) {
					byte[] pixels = new byte[frame.PixelWidth * frame.PixelHeight];
					byte[] palette = WpfImaging.GetBytePaletteRGB(frame.Palette);
					frame.CopyPixels(pixels, frame.PixelWidth, 0);

					e.SetTransparent(transparent);
					e.AddFrame(frame.PixelWidth, frame.PixelHeight, pixels, palette);
				}

				e.Finish();
			}

			if (prog != null)
				prog.Progress = 100f;
		}

		private static float _awayRounding(float max) {
			if (max < 0) {
				return (float) Math.Floor(max);
			}

			return (float) Math.Ceiling(max);
		}

		public static BoundingBox GenerateFrameBoundingBox(Act act, int actionIndex, int frameIndex) {
			return GenerateFrameBoundingBox(act, act[actionIndex, frameIndex]);
		}

		public static BoundingBox GenerateFrameBoundingBox(Act act, Frame frame) {
			List<Plane> planes = new List<Plane>();
			BoundingBox box = new BoundingBox();

			foreach (Layer layer in frame) {
				if (layer.SpriteIndex < 0)
					continue;

				GrfImage img = act.Sprite.Images[layer.SpriteTypeInt == 1 ? layer.SpriteIndex + act.Sprite.NumberOfIndexed8Images : layer.SpriteIndex];
				Plane plane = new Plane(img.Width, img.Height);

				plane.ScaleX(layer.ScaleX * (layer.Mirror ? -1f : 1f));
				plane.ScaleY(layer.ScaleY);
				plane.RotateZ(layer.Rotation);
				plane.Translate(layer.OffsetX + (layer.Mirror ? -(img.Width + 1) % 2 : 0), layer.OffsetY);

				planes.Add(plane);
			}

			if (planes.Count == 0) {
				box.Max[0] = 2;
				box.Max[1] = 2;
				box.Min[0] = 0;
				box.Min[1] = 0;

				box.Center[0] = (box.Max[0] - box.Min[0]) / 2f + box.Min[0];
				box.Center[1] = (box.Max[1] - box.Min[1]) / 2f + box.Min[1];

				box.Center[0] = _awayRounding((2 * box.Center[0] + 1) / 2);
				box.Center[1] = _awayRounding((2 * box.Center[1] + 1) / 2);

				box.Range[0] = box.Max[0] - box.Center[0];
				box.Range[1] = box.Max[1] - box.Center[1];

				return box;
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

			return box;
		}

		public static BoundingBox GenerateBoundingBox(Act act, int actionIndex, int margin = 0) {
			List<Plane> planes = new List<Plane>();
			BoundingBox box = new BoundingBox();

			foreach (Frame frame in act[actionIndex].Frames) {
				foreach (Layer layer in frame.Layers) {
					if (layer.SpriteIndex < 0)
						continue;

					GrfImage img = act.Sprite.Images[layer.SpriteTypeInt == 1 ? layer.SpriteIndex + act.Sprite.NumberOfIndexed8Images : layer.SpriteIndex];
					Plane plane = new Plane(img.Width, img.Height);

					plane.ScaleX(layer.ScaleX * (layer.Mirror ? -1f : 1f));
					plane.ScaleY(layer.ScaleY);
					plane.RotateZ(layer.Rotation);
					plane.Translate(layer.OffsetX + (layer.Mirror ? -(img.Width + 1) % 2 : 0), layer.OffsetY);

					planes.Add(plane);
				}
			}

			if (planes.Count == 0) {
				box.Max[0] = 2;
				box.Max[1] = 2;
				box.Min[0] = 0;
				box.Min[1] = 0;

				box.Center[0] = (box.Max[0] - box.Min[0]) / 2f + box.Min[0];
				box.Center[1] = (box.Max[1] - box.Min[1]) / 2f + box.Min[1];

				box.Center[0] = _awayRounding((2 * box.Center[0] + 1) / 2);
				box.Center[1] = _awayRounding((2 * box.Center[1] + 1) / 2);

				box.Range[0] = box.Max[0] - box.Center[0];
				box.Range[1] = box.Max[1] - box.Center[1];

				return box;
			}

			box.Max[0] = _awayRounding(planes.Max(p => p.Points.Max(q => q.X)));
			box.Max[1] = _awayRounding(planes.Max(p => p.Points.Max(q => q.Y)));
			box.Min[0] = _awayRounding(planes.Min(p => p.Points.Min(q => q.X)));
			box.Min[1] = _awayRounding(planes.Min(p => p.Points.Min(q => q.Y)));

			if (margin > 0) {
				box.Min[0] -= margin;
				box.Min[1] -= margin;
				box.Max[0] += margin;
				box.Max[1] += margin;
			}

			box.Center[0] = (box.Max[0] - box.Min[0]) / 2f + box.Min[0];
			box.Center[1] = (box.Max[1] - box.Min[1]) / 2f + box.Min[1];
			box.Center[0] = _awayRounding((2 * box.Center[0] + 1) / 2);
			box.Center[1] = _awayRounding((2 * box.Center[1] + 1) / 2);
			box.Range[0] = box.Max[0] - box.Center[0];
			box.Range[1] = box.Max[1] - box.Center[1];

			return box;
		}
		private static ImageSource _generateImage(Act act, int actionIndex, int frameIndex, bool uniform, System.Windows.Media.Color guideLineColors, BoundingBox box, BitmapScalingMode scalingMode) {
			return _generateImage(act, act[actionIndex, frameIndex], uniform, guideLineColors, box, scalingMode);
		}
		private static ImageSource _generateImage(Act act, Frame frame, bool uniform, System.Windows.Media.Color guideLineColors, BoundingBox box, BitmapScalingMode scalingMode) {
			List<Layer> layers = frame.Layers.ToList();

			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open()) {
				foreach (Layer layer in layers) {
					if (layer.SpriteIndex < 0)
						continue;

					GrfImage img = act.Sprite.Images[layer.SpriteTypeInt == 1 ? layer.SpriteIndex + act.Sprite.NumberOfIndexed8Images : layer.SpriteIndex];

					TransformGroup transformGroup = new TransformGroup();
					ScaleTransform scale = new ScaleTransform();
					TranslateTransform translate = new TranslateTransform();
					TranslateTransform translate2 = new TranslateTransform();
					RotateTransform rotate = new RotateTransform();

					translate2.X = -((img.Width + 1) / 2) + (layer.Mirror ? -(img.Width + 1) % 2 : 0);
					translate2.Y = -((img.Height + 1) / 2);
					translate.X = layer.OffsetX;
					translate.Y = layer.OffsetY;

					scale.ScaleX = layer.ScaleX * (layer.Mirror ? -1 : 1);
					scale.ScaleY = layer.ScaleY;

					rotate.Angle = layer.Rotation;

					transformGroup.Children.Add(translate2);
					transformGroup.Children.Add(scale);
					transformGroup.Children.Add(rotate);
					transformGroup.Children.Add(translate);
					dc.PushTransform(transformGroup);

					img = img.Copy();
					img.ApplyChannelColor(layer.Color);
					dc.DrawImage(img.Cast<BitmapSource>(), new Rect(0, 0, img.Width, img.Height));
					
					dc.Pop();
				}

				if (uniform) {
					Pen shapeOutlinePen = new Pen(new SolidColorBrush(guideLineColors), 1);
					shapeOutlinePen.Freeze();
					dc.DrawLine(shapeOutlinePen, new Point(0, box.Max.Y), new Point(0, box.Min.Y));
					dc.DrawLine(shapeOutlinePen, new Point(box.Min.X, 0), new Point(box.Max.X, 0));
				}
			}

			dGroup.SetValue(RenderOptions.BitmapScalingModeProperty, scalingMode);
			dGroup.Freeze();

			DrawingImage dImage = new DrawingImage(dGroup);
			dImage.SetValue(RenderOptions.BitmapScalingModeProperty, scalingMode);
			return dImage;
		}
		private static BitmapFrame _getIndexed8BitmapFrame(ImageSource source, System.Windows.Media.Color background, BitmapScalingMode mode) {
			BitmapFrame frame = ForceRender(source, mode);
			BitmapSource im = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
			return _reconstructImage(im, background);
		}
		public static BitmapFrame ForceRender(ImageSource dImage, BitmapScalingMode mode) {
			Viewbox viewbox = new Viewbox();
			Image im = new Image();
			im.Source = dImage;
			im.SetValue(RenderOptions.BitmapScalingModeProperty, mode);
			viewbox.Child = im;
			viewbox.SetValue(RenderOptions.BitmapScalingModeProperty, mode);
			viewbox.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
			viewbox.Measure(new Size(dImage.Width, dImage.Height));
			viewbox.Arrange(new Rect(0, 0, dImage.Width, dImage.Height));
			viewbox.UpdateLayout();

			RenderTargetBitmap targetBitmap =
				new RenderTargetBitmap((int) dImage.Width,
				                       (int) dImage.Height,
				                       96, 96,
				                       PixelFormats.Pbgra32);
			targetBitmap.Render(viewbox);
			return BitmapFrame.Create(targetBitmap);
		}
		public static BitmapFrame ForceRender(Image dImage, BitmapScalingMode mode) {
			Viewbox viewbox = new Viewbox();
			Image im = dImage;
			im.SetValue(RenderOptions.BitmapScalingModeProperty, mode);
			viewbox.Child = im;
			viewbox.SetValue(RenderOptions.BitmapScalingModeProperty, mode);
			viewbox.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
			viewbox.Measure(new Size(dImage.Width, dImage.Height));
			viewbox.Arrange(new Rect(0, 0, dImage.Width, dImage.Height));
			viewbox.UpdateLayout();

			RenderTargetBitmap targetBitmap =
				new RenderTargetBitmap((int)dImage.Width,
									   (int)dImage.Height,
									   96, 96,
									   PixelFormats.Pbgra32);
			targetBitmap.Render(viewbox);
			return BitmapFrame.Create(targetBitmap);
		}
		private static BitmapFrame _reconstructImage(BitmapSource im, System.Windows.Media.Color background) {
			byte[] pixels = new byte[im.PixelWidth * im.PixelHeight * im.Format.BitsPerPixel / 8];
			im.CopyPixels(pixels, im.PixelWidth * im.Format.BitsPerPixel / 8, 0);

			byte[] newPixels = new byte[im.PixelWidth * im.PixelHeight];

			List<int> colors = new List<int>();
			colors.Add(TransparentPixel.A << 24 | TransparentPixel.R << 16 | TransparentPixel.G << 8 | TransparentPixel.B);
			int color;
			int colorTransparent = colors[0];
			byte colorA;

			for (int i = 0, numPixels = pixels.Length / 4; i < numPixels; i++) {
				if (pixels[4 * i + 3] != 0) {
					colorA = pixels[4 * i + 3];

					color = 0xff << 24 |
							(byte)(((255 - colorA) * background.R + colorA * pixels[4 * i + 2]) / 255f) << 16 |
							(byte)(((255 - colorA) * background.G + colorA * pixels[4 * i + 1]) / 255f) << 8 |
							(byte)(((255 - colorA) * background.B + colorA * pixels[4 * i + 0]) / 255f);
				}
				else {
					color = colorTransparent;
				}

				if (colors.Contains(color)) {
					newPixels[i] = (byte) colors.IndexOf(color);
				}
				else {
					colors.Add(color);
					newPixels[i] = (byte) (colors.Count - 1);
				}
			}

			List<System.Windows.Media.Color> toWpfColors = new List<System.Windows.Media.Color>(256);

			for (int i = 0; i < colors.Count; i++) {
				color = colors[i];

				toWpfColors.Add(System.Windows.Media.Color.FromArgb(
									255, 
									(byte) ((color & 0x00ff0000) >> 16),
									(byte) ((color & 0x0000ff00) >> 8),
									(byte) ((color & 0x000000ff))
									));
			}

			if (toWpfColors.Count > 256)
				toWpfColors = _reduceImageQuality(out newPixels, pixels, im.PixelWidth, im.PixelHeight, toWpfColors, background);

			while (toWpfColors.Count < 256) {
				toWpfColors.Add(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
			}

			WriteableBitmap bit = new WriteableBitmap(im.PixelWidth, im.PixelHeight, 96, 96, PixelFormats.Indexed8, new BitmapPalette(toWpfColors));
			bit.WritePixels(new Int32Rect(0, 0, im.PixelWidth, im.PixelHeight), newPixels, im.PixelWidth, 0);
			bit.Freeze();
			return BitmapFrame.Create(bit);
		}
		private static List<System.Windows.Media.Color> _reduceImageQuality(out byte[] pixels, byte[] originalPixels, int pixelWidth, int pixelHeight, IList<System.Windows.Media.Color> colors, System.Windows.Media.Color background) {
			byte[] newPixels = new byte[pixelWidth * pixelHeight];

			int exceedingColors = colors.Count - 256;
			Dictionary<int, int> closestMatchingColors = new Dictionary<int, int>();

			int searchRadius = (int) (exceedingColors / 150f + 10);

			while (closestMatchingColors.Count < exceedingColors) {
				closestMatchingColors.Clear();

				for (int i = 1; i < colors.Count; i++) {
					if (closestMatchingColors.ContainsKey(i))
						continue;

					for (int j = 1; j < colors.Count; j++) {
						if (j == i || closestMatchingColors.ContainsKey(j))
							continue;

						if (Math.Abs(colors[i].R - colors[j].R) + Math.Abs(colors[i].G - colors[j].G) + Math.Abs(colors[i].B - colors[j].B) < searchRadius) {
							closestMatchingColors.Add(j, i);
						}
					}
				}

				searchRadius *= 2;
			}

			List<System.Windows.Media.Color> newColors = new List<System.Windows.Media.Color>(colors);
			foreach (KeyValuePair<int, int> tuple in closestMatchingColors) {
				newColors[tuple.Key] = colors[tuple.Value];
			}

			newColors = newColors.Distinct().ToList();

			for (int i = 0; i < originalPixels.Length / 4; i++) {
				System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(originalPixels[4 * i + 3], originalPixels[4 * i + 2], originalPixels[4 * i + 1], originalPixels[4 * i + 0]);
				if (color.A != 0) {
					color = System.Windows.Media.Color.FromArgb(255,
										   (byte)(((255 - color.A) * background.R + color.A * color.R) / 255f),
										   (byte)(((255 - color.A) * background.G + color.A * color.G) / 255f),
										   (byte)(((255 - color.A) * background.B + color.A * color.B) / 255f));

					int colorIndex = colors.IndexOf(color);
					if (closestMatchingColors.ContainsKey(colorIndex)) {
						newPixels[i] = (byte)newColors.IndexOf(colors[closestMatchingColors[colorIndex]]);
					}
					else {
						newPixels[i] = (byte)newColors.IndexOf(color);
					}
				}
				else {
					newPixels[i] = 0;
				}
			}

			pixels = newPixels;
			return newColors;
		}
	}
}
