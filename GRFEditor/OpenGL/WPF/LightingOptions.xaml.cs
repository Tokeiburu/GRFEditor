using System.Windows;
using System.Windows.Media;
using GRFEditor.OpenGL.MapComponents;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for LightningOptions.xaml
	/// </summary>
	public partial class LightingOptions : Window {
		private bool _enableEvents = true;
		private RendererLoadRequest _currentRequest;

		public LightingOptions() {
			InitializeComponent();

			_qcsAmbient.PreviewColorChanged += _qcsAmbient_ColorChanged;
			_qcsAmbient.ColorChanged += _qcsAmbient_ColorChanged;

			_qcsDiffuse.PreviewColorChanged += _qcsDiffuse_ColorChanged;
			_qcsDiffuse.ColorChanged += _qcsDiffuse_ColorChanged;
			_sliderLatitude.ValueChanged += _sliderLatitude_ValueChanged;
			_sliderLongitude.ValueChanged += _sliderLongitude_ValueChanged;
		}

		public void Load(RendererLoadRequest request) {
			_currentRequest = request;
			_enableEvents = false;
			_qcsAmbient.Color = Color.FromArgb(255, (byte)(request.Rsw.Light.AmbientRed * 255f), (byte)(request.Rsw.Light.AmbientGreen * 255f), (byte)(request.Rsw.Light.AmbientBlue * 255f));
			_qcsDiffuse.Color = Color.FromArgb(255, (byte)(request.Rsw.Light.DiffuseRed * 255f), (byte)(request.Rsw.Light.DiffuseGreen * 255f), (byte)(request.Rsw.Light.DiffuseBlue * 255f));

			_sliderLatitude.SetPosition(request.Rsw.Light.Latitude / 90f, true);
			_sliderLongitude.SetPosition(request.Rsw.Light.Longitude / 360f, true);
			_tbLongitude.Text = request.Rsw.Light.Longitude + "";
			_tbLatitude.Text = request.Rsw.Light.Latitude + "";

			_enableEvents = true;
		}

		private void _sliderLatitude_ValueChanged(object sender, double value) {
			if (!_enableEvents)
				return;

			_currentRequest.Rsw.Light.Latitude = (int)(value * 90f);
			_tbLatitude.Text = _currentRequest.Rsw.Light.Latitude + "";
			_currentRequest.GndRenderer.ReloadLight = true;
			_currentRequest.MapRenderer.ReloadLight = true;
		}

		private void _sliderLongitude_ValueChanged(object sender, double value) {
			if (!_enableEvents)
				return;

			_currentRequest.Rsw.Light.Longitude = (int)(value * 360f);
			_tbLongitude.Text = _currentRequest.Rsw.Light.Longitude + "";
			_currentRequest.GndRenderer.ReloadLight = true;
			_currentRequest.MapRenderer.ReloadLight = true;
		}

		private void _qcsAmbient_ColorChanged(object sender, Color value) {
			if (!_enableEvents)
				return;

			_currentRequest.Rsw.Light.AmbientRed = value.R / 255f;
			_currentRequest.Rsw.Light.AmbientGreen = value.G / 255f;
			_currentRequest.Rsw.Light.AmbientBlue = value.B / 255f;
			_currentRequest.GndRenderer.ReloadLight = true;
			_currentRequest.MapRenderer.ReloadLight = true;
		}

		private void _qcsDiffuse_ColorChanged(object sender, Color value) {
			if (!_enableEvents)
				return;

			_currentRequest.Rsw.Light.DiffuseRed = value.R / 255f;
			_currentRequest.Rsw.Light.DiffuseGreen = value.G / 255f;
			_currentRequest.Rsw.Light.DiffuseBlue = value.B / 255f;
			_currentRequest.GndRenderer.ReloadLight = true;
			_currentRequest.MapRenderer.ReloadLight = true;
		}
	}
}
