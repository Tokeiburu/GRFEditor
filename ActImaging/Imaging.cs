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
using GRF.Image.Decoders;

namespace ActImaging {
	public static class Imaging {
		private static BoundingBox _defaultBoundingBox;
		private static Color _transparentPixel = Color.FromArgb(255, 255, 0, 255);

		static Imaging() {
			_defaultBoundingBox = new BoundingBox();
			_defaultBoundingBox.Max[0] = 1;
			_defaultBoundingBox.Max[1] = 1;
			_defaultBoundingBox.Min[0] = -1;
			_defaultBoundingBox.Min[1] = -1;

			_defaultBoundingBox.Center[0] = 0;
			_defaultBoundingBox.Center[1] = 0;

			_defaultBoundingBox.Range[0] = 1;
			_defaultBoundingBox.Range[1] = 1;
		}

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

			GenerateImageConfig config = new GenerateImageConfig();
			config.Box = GenerateBoundingBox(act, actionIndex);
			
			for (int i = 0; i < act[actionIndex].Frames.Count; i++) {
				config.Layers = act[actionIndex, i].Layers;
				images.Add(_generateImage(act, config));
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
		public static ImageSource GenerateImage(Act act, List<Layer> layers, BitmapScalingMode scalingMode = BitmapScalingMode.NearestNeighbor) => _generateImage(act, new GenerateImageConfig(layers) { ScalingMode = scalingMode });
		public static ImageSource GenerateImage(Act act, int actionIndex, int frameIndex, BitmapScalingMode scalingMode = BitmapScalingMode.NearestNeighbor) => GenerateImage(act, act[actionIndex, frameIndex].Layers, scalingMode);
		public static ImageSource GenerateImage(Act act, Frame frame, BitmapScalingMode scalingMode = BitmapScalingMode.NearestNeighbor) => GenerateImage(act, frame.Layers, scalingMode);

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

			GenerateImageConfig config = new GenerateImageConfig();
			config.Box = GenerateBoundingBox(act, actionIndex);
			config.GuidelineColor = guideLineColors;
			config.ScalingMode = scalingMode;

			if (margin > 0) {
				config.Box.Min[0] -= margin;
				config.Box.Min[1] -= margin;
				config.Box.Max[0] += margin;
				config.Box.Max[1] += margin;
				config.Box.Range[0] = config.Box.Max[0] - config.Box.Center[0];
				config.Box.Range[1] = config.Box.Max[1] - config.Box.Center[1];
			}

			for (int i = 0; i < act[actionIndex].Frames.Count; i++) {
				config.Layers = act[actionIndex, i].Layers;
				images.Add(_generateImage(act, config));
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
			BitmapScalingMode scaling = BitmapScalingMode.NearestNeighbor;
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


		public static BoundingBox GenerateBoundingBox(Act act, int actionIndex, bool ceilingAwayFromZero = true, bool enableScaling = true) {
			List<Layer> layers = new List<Layer>();
			foreach (var frame in act[actionIndex])
				layers.AddRange(frame.Layers);
			return GenerateBoundingBox(act, layers, ceilingAwayFromZero, enableScaling);
		}
		public static BoundingBox GenerateBoundingBox(Act act, Frame frame, bool ceilingAwayFromZero = true, bool enableScaling = true) => GenerateBoundingBox(act, frame.Layers, ceilingAwayFromZero, enableScaling);
		public static BoundingBox GenerateBoundingBox(Act act, int actionIndex, int frameIndex, bool ceilingAwayFromZero = true, bool enableScaling = true) => GenerateBoundingBox(act, act[actionIndex, frameIndex].Layers, ceilingAwayFromZero, enableScaling);
		public static BoundingBox GenerateBoundingBox(Act act, Layer layer, bool ceilingAwayFromZero = true, bool enableScaling = true) => GenerateBoundingBox(act, new List<Layer>() { layer }, ceilingAwayFromZero, enableScaling);
		public static BoundingBox GenerateBoundingBox(Act act, List<Layer> layers, bool ceilingAwayFromZero = true, bool enableScaling = true) {
			List<Plane> planes = new List<Plane>();
			BoundingBox box = new BoundingBox();

			foreach (Layer layerF in layers) {
				var layer = layerF;

				if (layer.SpriteIndex < 0)
					continue;

				if (!enableScaling) {
					layer = new Layer(layer);
					layer.ScaleX = 1;
					layer.ScaleY = 1;
				}

				Plane plane = layer.ToPlane(act);
				planes.Add(plane);
			}

			if (planes.Count == 0) {
				return _defaultBoundingBox.Clone();
			}

			box = _calculateBox(box, planes, ceilingAwayFromZero);
			return box;
		}

		private static BoundingBox _calculateBox(BoundingBox box, List<Plane> planes, bool ceilingAwayFromZero) {
			if (ceilingAwayFromZero) {
				box.Max[0] = _awayRounding(planes.Max(p => p.Points.Max(q => q.X)));
				box.Max[1] = _awayRounding(planes.Max(p => p.Points.Max(q => q.Y)));
				box.Min[0] = _awayRounding(planes.Min(p => p.Points.Min(q => q.X)));
				box.Min[1] = _awayRounding(planes.Min(p => p.Points.Min(q => q.Y)));
			}
			else {
				box.Max[0] = planes.Max(p => p.Points.Max(q => q.X));
				box.Max[1] = planes.Max(p => p.Points.Max(q => q.Y));
				box.Min[0] = planes.Min(p => p.Points.Min(q => q.X));
				box.Min[1] = planes.Min(p => p.Points.Min(q => q.Y));
			}

			box.Center[0] = (box.Max[0] - box.Min[0]) / 2f + box.Min[0];
			box.Center[1] = (box.Max[1] - box.Min[1]) / 2f + box.Min[1];

			box.Range[0] = box.Max[0] - box.Center[0];
			box.Range[1] = box.Max[1] - box.Center[1];
			return box;
		}

		public class GenerateImageConfig {
			public BitmapScalingMode ScalingMode = BitmapScalingMode.NearestNeighbor;
			public List<Layer> Layers = new List<Layer>();
			public System.Windows.Media.Color? GuidelineColor;
			public BoundingBox Box;

			public GenerateImageConfig() {
			}

			public GenerateImageConfig(List<Layer> layers) {
				Layers = layers;
			}
		}

		private static ImageSource _generateImage(Act act, GenerateImageConfig config) {
			TransformGroup transformGroup = new TransformGroup();
			RotateTransform rotate = new RotateTransform();
			ScaleTransform scale = new ScaleTransform();
			ScaleTransform mirrorScale = new ScaleTransform();
			TranslateTransform translateFrame = new TranslateTransform();
			TranslateTransform translateToCenter = new TranslateTransform();

			transformGroup.Children.Add(mirrorScale);
			transformGroup.Children.Add(translateToCenter);
			transformGroup.Children.Add(scale);
			transformGroup.Children.Add(rotate);
			transformGroup.Children.Add(translateFrame);

			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open()) {
				foreach (Layer layer in config.Layers) {
					if (layer.SpriteIndex < 0)
						continue;

					GrfImage img = layer.GetImage(act.Sprite);

					mirrorScale.ScaleX = layer.Mirror ? -1d : 1d;
					mirrorScale.ScaleY = 1d;
					mirrorScale.CenterX = img.Width / 2d;

					translateToCenter.X = -(img.Width + 1) / 2;
					translateToCenter.Y = -(img.Height + 1) / 2;

					scale.ScaleX = layer.ScaleX;
					scale.ScaleY = layer.ScaleY;

					rotate.Angle = layer.Rotation;
					rotate.CenterX = img.Width % 2 == 1 ? -0.5f * layer.ScaleX : 0;
					rotate.CenterY = img.Height % 2 == 1 ? -0.5f * layer.ScaleY : 0;

					translateFrame.X = layer.OffsetX;
					translateFrame.Y = layer.OffsetY;

					dc.PushTransform(new MatrixTransform(transformGroup.Value));

					img = img.Copy();
					img.Multiply(layer.Color);
					dc.DrawImage(img.Cast<BitmapSource>(), new Rect(0, 0, img.Width, img.Height));
					dc.Pop();
				}

				if (config.GuidelineColor != null) {
					var box = config.Box;
					Pen shapeOutlinePen = new Pen(new SolidColorBrush(config.GuidelineColor.Value), 1);
					shapeOutlinePen.Freeze();
					dc.DrawLine(shapeOutlinePen, new Point(0, box.Max.Y), new Point(0, box.Min.Y));
					dc.DrawLine(shapeOutlinePen, new Point(box.Min.X, 0), new Point(box.Max.X, 0));
				}
			}

			dGroup.SetValue(RenderOptions.BitmapScalingModeProperty, config.ScalingMode);
			dGroup.Freeze();

			DrawingImage dImage = new DrawingImage(dGroup);
			dImage.SetValue(RenderOptions.BitmapScalingModeProperty, config.ScalingMode);
			return dImage;
		}
		private static BitmapFrame _getIndexed8BitmapFrame(ImageSource source, System.Windows.Media.Color background, BitmapScalingMode mode) {
			BitmapFrame frame = ForceRender(source, mode);
			BitmapSource im = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
			return _reconstructImage(im, background);
		}
		public static BitmapFrame ForceRender(ImageSource dImage, BitmapScalingMode mode) {
			return ForceRender(new Image { Source = dImage }, mode);
		}
		public static BitmapFrame ForceRender(Image dImage, BitmapScalingMode mode) {
			ImageSource source = dImage.Source;
			if (source == null) return null;

			double width = double.IsNaN(dImage.Width) ? dImage.Source.Width : dImage.Width;
			double height = double.IsNaN(dImage.Height) ? dImage.Source.Height : dImage.Height;

			DrawingVisual drawingVisual = new DrawingVisual();

			RenderOptions.SetBitmapScalingMode(drawingVisual, mode);
			RenderOptions.SetEdgeMode(drawingVisual, EdgeMode.Aliased);

			using (DrawingContext dc = drawingVisual.RenderOpen()) {
				dc.DrawImage(source, new Rect(0, 0, width, height));
			}

			RenderTargetBitmap targetBitmap = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
			targetBitmap.Render(drawingVisual);
			return BitmapFrame.Create(targetBitmap);
		}
		private static BitmapFrame _reconstructImage(BitmapSource im, System.Windows.Media.Color background) {
			byte[] pixels = new byte[im.PixelWidth * im.PixelHeight * im.Format.BitsPerPixel / 8];
			im.CopyPixels(pixels, im.PixelWidth * im.Format.BitsPerPixel / 8, 0);

			GrfImage image = new GrfImage(pixels, im.PixelWidth, im.PixelHeight, GrfImageType.Bgra32);
			var converter = new Indexed8FormatConverter();
			converter.UseBackgroundColor = true;
			converter.BackgroundColor = GrfColor.FromArgb(background.A, background.R, background.G, background.B);
			converter.Options |= Indexed8FormatConverter.PaletteOptions.AutomaticallyGeneratePalette;
			image.Convert(converter);
			return BitmapFrame.Create(image.Cast<BitmapSource>());
		}
	}
}
