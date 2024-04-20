using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using ErrorManager;
using GRF.Graphics;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary.WPF.Styles;
using Color = System.Windows.Media.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for CloudEditDialog.xaml
	/// </summary>
	public partial class MinimapDialog : TkWindow {
		private GLControl _primary;
		private string _mapname;

		public MinimapDialog() : base("Minimap edit...", "app.ico") {
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}

		public void Init(OpenGLViewport viewport, string mapname) {
			_primary = viewport._primary;
			_mapname = mapname;

			_tbWater_Color.Color = Color.FromArgb((byte)(viewport.RenderOptions.MinimapWaterColor[3] * 255f), (byte)(viewport.RenderOptions.MinimapWaterColor[0] * 255f), (byte)(viewport.RenderOptions.MinimapWaterColor[1] * 255f), (byte)(viewport.RenderOptions.MinimapWaterColor[2] * 255f));
			_tbWater_Color.ColorChanged += (s, c) => {
				viewport.RenderOptions.MinimapWaterColor = new Vector4(c.R, c.G, c.B, c.A) / 255f;
			};
			_tbWater_Color.PreviewColorChanged += (s, c) => {
				viewport.RenderOptions.MinimapWaterColor = new Vector4(c.R, c.G, c.B, c.A) / 255f;
			};

			_tbWalk_Color.SetPosition(viewport.RenderOptions.MinimapWalkColor[0], true);
			_tbWalk_Color.ValueChanged += (s, value) => {
				viewport.RenderOptions.MinimapWalkColor[0] = (float)value;
				viewport.RenderOptions.MinimapWalkColor[1] = (float)value;
				viewport.RenderOptions.MinimapWalkColor[2] = (float)value;
			};

			_tbNonWalk_Color.SetPosition(viewport.RenderOptions.MinimapNonWalkColor[3], true);
			_tbNonWalk_Color.ValueChanged += (s, value) => {
				viewport.RenderOptions.MinimapNonWalkColor[3] = (float)value;
			};

			_tbBorderCut.SetPosition(GrfEditorConfiguration.MapRendererMinimapBorderCut / 20f, true);
			_tbBorderCut.ValueChanged += (s, value) => {
				GrfEditorConfiguration.MapRendererMinimapBorderCut = (int)Math.Round((value * 20f), MidpointRounding.AwayFromZero);
				_tbBorderCut.SetPosition(GrfEditorConfiguration.MapRendererMinimapBorderCut / 20f, true);

				int removeEdge = GrfEditorConfiguration.MapRendererMinimapBorderCut;
				var distance = Math.Max(viewport._request.Gnd.Header.Height - removeEdge, viewport._request.Gnd.Header.Width - removeEdge) * 10f;
				float ratio = viewport._request.Gnd.Height < viewport._request.Gnd.Width ? (float)viewport._request.Gnd.Height / viewport._request.Gnd.Width : 1f;
				distance *= ratio;
				viewport.Camera.Distance = distance;
			};

			Binder.Bind(_tbMargin, () => GrfEditorConfiguration.MapRendererMinimapMargin, v => GrfEditorConfiguration.MapRendererMinimapMargin = v);
			Binder.Bind(_cbEnableWater, () => GrfEditorConfiguration.MapRenderMinimapEnableWaterOverride, v => GrfEditorConfiguration.MapRenderMinimapEnableWaterOverride = v, delegate {
				viewport.RenderOptions.MinimapWaterOverride = GrfEditorConfiguration.MapRenderMinimapEnableWaterOverride;
			}, true);
		}

		private void _buttonSave_Click(object sender, RoutedEventArgs e) {
			var path = PathRequest.SaveFileExtract("filter", "Bmp Files|*.bmp", "fileName", _mapname + ".bmp");

			if (path == null)
				return;

			try {
				_primary.MakeCurrent();
				Bitmap bmp = new Bitmap(_primary.Width, _primary.Height);
				BitmapData data =
					bmp.LockBits(new Rectangle(0, 0, _primary.Width, _primary.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format24bppRgb);
				GL.ReadPixels(0, 0, _primary.Width, _primary.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
				bmp.UnlockBits(data);
				bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

				MemoryStream stream = new MemoryStream();
				bmp.Save(stream, ImageFormat.Bmp);

				int margin = Math.Min(Math.Max(0, GrfEditorConfiguration.MapRendererMinimapMargin), 512);
				GrfImage image = new GrfImage(stream);

				byte[] imPinkData = new byte[512 * 512 * 3];
				for (int i = 0; i < imPinkData.Length; i += 3) {
					imPinkData[i + 0] = 255;
					imPinkData[i + 1] = 0;
					imPinkData[i + 2] = 255;
				}

				GrfImage imPink = new GrfImage(imPinkData, 512, 512, GrfImageType.Bgr24);

				int left = (imPink.Width - image.Width) / 2;
				int top = (imPink.Height - image.Height) / 2;
				int right = left + image.Width;
				int bottom = top + image.Height;

				imPink.SetPixelsUnrestricted(left, top, image);

				if (margin > 0) {
					left -= margin;
					top -= margin;
					right += margin;
					bottom += margin;

					if (left < 0)
						left = 0;
					if (top < 0)
						top = 0;
					if (right > imPink.Width)
						right = imPink.Width;
					if (bottom > imPink.Height)
						bottom = imPink.Height;

					for (int y = 0; y < imPink.Height; y++) {
						for (int x = 0; x < imPink.Width; x++) {
							if ((
								((x >= left && x < left + margin) || (x >= right - margin && x < right)) && (y >= top && y < bottom)
								)
							    ||
							    (
								    ((y >= top && y < top + margin) || (y >= bottom - margin && y < bottom)) && (x >= left && x < right)
								    )) {
								imPink.Pixels[(y * imPink.Width + x) * 3 + 0] = 0;
								imPink.Pixels[(y * imPink.Width + x) * 3 + 1] = 0;
								imPink.Pixels[(y * imPink.Width + x) * 3 + 2] = 0;
							}
						}
					}
				}

				imPink.Save(path, PixelFormatInfo.BmpBgr24);
				Close();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
