using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GRFEditor.OpenGL.MapRenderers;
using GrfToWpfBridge;
using OpenTK;
using Utilities;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for CloudEditDialog.xaml
	/// </summary>
	public partial class CloudEditDialog : Window {
		public CloudEditDialog() {
			InitializeComponent();
		}

		public void Init(OpenGLViewport viewport) {
			if (MapRenderer.SkyMap == null)
				MapRenderer.SkyMap = new CloudEffectSettings();

			_tbBg_Color.Color = Color.FromArgb((byte)(viewport.RenderOptions.SkymapBackgroundColor[3] * 255f), (byte)(viewport.RenderOptions.SkymapBackgroundColor[0] * 255f), (byte)(viewport.RenderOptions.SkymapBackgroundColor[1] * 255f), (byte)(viewport.RenderOptions.SkymapBackgroundColor[2] * 255f));
			_tbBg_Color.ColorChanged += (s, c) => {
				viewport.RenderOptions.SkymapBackgroundColor = new Vector4(c.R, c.G, c.B, c.A) / 255f;
				MapRenderer.SkyMap.IsChanged = true;
			};
			_tbBg_Color.PreviewColorChanged += (s, c) => {
				viewport.RenderOptions.SkymapBackgroundColor = new Vector4(c.R, c.G, c.B, c.A) / 255f;
				MapRenderer.SkyMap.IsChanged = true;
			};

			_cloudTab._tbColor.Color = Color.FromArgb(255, (byte)(MapRenderer.SkyMap.Color[0] * 255f), (byte)(MapRenderer.SkyMap.Color[1] * 255f), (byte)(MapRenderer.SkyMap.Color[2] * 255f));
			_cloudTab._tbColor.ColorChanged += (s, c) => {
				MapRenderer.SkyMap.Color = new Vector3(c.R, c.G, c.B) / 255f;
				MapRenderer.SkyMap.IsChanged = true;
			};
			_cloudTab._tbColor.PreviewColorChanged += (s, c) => {
				MapRenderer.SkyMap.Color = new Vector3(c.R, c.G, c.B) / 255f;
				MapRenderer.SkyMap.IsChanged = true;
			};

			_tbEnableStar.IsChecked = MapRenderer.SkyMap.StarEffect;
			_tbEnableStar.Checked += delegate {
				MapRenderer.SkyMap.StarEffect = true;
				MapRenderer.SkyMap.IsChanged = true;
			};
			_tbEnableStar.Unchecked += delegate {
				MapRenderer.SkyMap.StarEffect = false;
				MapRenderer.SkyMap.IsChanged = true;
			};

			_tbEnableCloud.IsChecked = MapRenderer.SkyMap.CloudEffect;
			_tbEnableCloud.Checked += delegate {
				MapRenderer.SkyMap.CloudEffect = true;
				MapRenderer.SkyMap.IsChanged = true;
			};
			_tbEnableCloud.Unchecked += delegate {
				MapRenderer.SkyMap.CloudEffect = false;
				MapRenderer.SkyMap.IsChanged = true;
			};

			Binder.Bind(_tbEnableSkyMap, () => viewport.RenderOptions.RenderSkymapDetected, v => viewport.RenderOptions.RenderSkymapDetected = v);

			_bind(_cloudTab._tbAmount, () => MapRenderer.SkyMap.Num.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Num = (int)FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbSize, () => MapRenderer.SkyMap.Size.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Size = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbSize_Extra, () => MapRenderer.SkyMap.Size_Extra.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Size_Extra = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbExpand_Rate, () => MapRenderer.SkyMap.Expand_Rate.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Expand_Rate = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbAlpha_Inc_Time, () => MapRenderer.SkyMap.Alpha_Inc_Time.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Alpha_Inc_Time = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbAlpha_Inc_Time_Extra, () => MapRenderer.SkyMap.Alpha_Inc_Time_Extra.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Alpha_Inc_Time_Extra = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbAlpha_Inc_Speed, () => MapRenderer.SkyMap.Alpha_Inc_Speed.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Alpha_Inc_Speed = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbAlpha_Dec_Time, () => MapRenderer.SkyMap.Alpha_Dec_Time.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Alpha_Dec_Time = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbAlpha_Dec_Time_Extra, () => MapRenderer.SkyMap.Alpha_Dec_Time_Extra.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Alpha_Dec_Time_Extra = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbAlpha_Dec_Speed, () => MapRenderer.SkyMap.Alpha_Dec_Speed.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Alpha_Dec_Speed = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbHeight, () => MapRenderer.SkyMap.Height.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Height = FormatConverters.SingleConverterNoThrow(v));
			_bind(_cloudTab._tbHeight_Extra, () => MapRenderer.SkyMap.Height_Extra.ToString(CultureInfo.InvariantCulture), v => MapRenderer.SkyMap.Height_Extra = FormatConverters.SingleConverterNoThrow(v));
		}

		private void _bind(TextBox tb, Func<string> get, Func<string, float> set) {
			tb.Text = get();

			tb.TextChanged += delegate {
				set(tb.Text);
				MapRenderer.SkyMap.IsChanged = true;
			};
		}
	}
}
