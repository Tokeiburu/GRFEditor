using System;
using System.Windows;
using System.Windows.Media;
using GRFEditor.OpenGL.MapComponents;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for LightningOptions.xaml
	/// </summary>
	public partial class GatOptions : Window {
		private bool _enableEvents = true;
		private RendererLoadRequest _currentRequest;
		private OpenGLViewport _viewport;

		public GatOptions() {
			InitializeComponent();

			_slider.ValueChanged += _slider_ValueChanged;
			_reset.Click += delegate {
				_viewport.RenderOptions.GatAlpha = 0.25f;
				_slider.SetPosition(_viewport.RenderOptions.GatAlpha, true);
				_tbAlpha.Text = String.Format("{0:0.00}", _viewport.RenderOptions.GatAlpha);
			};

			_sliderZBias.ValueChanged += _sliderZBias_ValueChanged;
			_resetZBias.Click += delegate {
				_viewport.RenderOptions.GatZBias = 0;
				_sliderZBias.SetPosition((_viewport.RenderOptions.GatZBias + 3f) / 6f, true);
				_tbZBias.Text = String.Format("{0:0.00}", _viewport.RenderOptions.GatZBias);
			};
		}

		public void Load(RendererLoadRequest request) {
			_currentRequest = request;
			_enableEvents = false;

			_viewport = _currentRequest.Context as OpenGLViewport;

			if (_viewport != null) {
				_slider.SetPosition(_viewport.RenderOptions.GatAlpha, true);
				_tbAlpha.Text = String.Format("{0:0.00}", _viewport.RenderOptions.GatAlpha);

				_sliderZBias.SetPosition((_viewport.RenderOptions.GatZBias + 3f) / 6f, true);
				_tbZBias.Text = String.Format("{0:0.00}", _viewport.RenderOptions.GatZBias);
			}

			_enableEvents = true;
		}

		private void _slider_ValueChanged(object sender, double value) {
			if (!_enableEvents)
				return;

			_viewport.RenderOptions.GatAlpha = (float)value;
			_slider.SetPosition(_viewport.RenderOptions.GatAlpha, true);
			_tbAlpha.Text = String.Format("{0:0.00}", _viewport.RenderOptions.GatAlpha);
		}

		private void _sliderZBias_ValueChanged(object sender, double value) {
			if (!_enableEvents)
				return;

			_viewport.RenderOptions.GatZBias = (float)value * 6f - 3f;
			_sliderZBias.SetPosition((_viewport.RenderOptions.GatZBias + 3f) / 6f, true);
			_tbZBias.Text = String.Format("{0:0.00}", _viewport.RenderOptions.GatZBias);
		}
	}
}
